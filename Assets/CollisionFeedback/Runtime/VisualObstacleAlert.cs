using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using CollisionFeedback.Core;
using Joint = CollisionFeedback.Core.Joint; // disambiguate from UnityEngine.Joint (physics component)

namespace CollisionFeedback.Runtime
{
    /// <summary>
    /// ⛔ **NOT THE CONFIRMATORY VISUAL BENCHMARK. DO NOT USE FOR H4.** Superseded 2026-09-21 by
    /// <see cref="LimbCueIndicator"/>.
    ///
    /// This component glows the HAZARD. The Visual condition would therefore reveal where the hazard is
    /// and how close the limb is to it — two things the haptic conditions never disclose. H4 compares PB
    /// against Visual to isolate MODALITY, so any information difference destroys it: a Visual advantage
    /// would show that revealing hazards helps, which is true, uninteresting, and not the question
    /// [PAPER1_STUDY_DESIGN §2; docs/PAPER1_CODE_GAP.md #2].
    ///
    /// Retained for pilot demonstrations and debugging only. It logs an error if driven during a
    /// confirmatory block.
    ///
    /// Renders the Visual-condition obstacle alert [Protocol 2.3]: a proximity-graded, depth-coloured glow on
    /// the at-risk obstacle, driven by the SAME tracking/distance the haptic conditions use (detection-matched).
    /// The grading math is <see cref="VisualAlertModel"/> (Core, tested); this class only drives the rendering.
    ///
    /// OBSTACLES ARE HIDDEN. The obstacle volumes stand for real-world hazards the participant cannot see
    /// through the HMD — if their meshes rendered as scene content, every condition (including None) would get
    /// a free visual channel and the manipulation would collapse. So on Awake this component disables every
    /// Renderer under <see cref="SceneObstacles"/>; in the Visual condition the at-risk obstacle fades in as a
    /// translucent unlit veil (green→red with proximity, pulse rate rising as the limb nears — SafeXR-style)
    /// and fades back to fully invisible as the limb clears. Every other condition never shows anything.
    /// Set <see cref="hideObstacles"/> = false to restore the legacy always-visible tinted boxes (debug only).
    ///
    /// Drive it from the session loop: <see cref="Configure"/> once per block (obstacles + tracked limbs + the
    /// reactive distance D), then <see cref="UpdatePose"/> every frame with <c>active = true</c> only in the
    /// Visual condition. Obstacle Ids map to scene GameObjects of the SAME name (the SceneObstacles convention).
    /// </summary>
    public sealed class VisualObstacleAlert : MonoBehaviour
    {
        [SerializeField] private float reactiveDistanceOverride = 0f;            // 0 = use the D from Configure
        [SerializeField] private float fadePerSecond = 6f;                       // presence-aware smoothing rate
        [SerializeField] private Color farColor = new(0.2f, 1f, 0.2f);           // green near the alert edge
        [SerializeField] private Color nearColor = new(1f, 0.2f, 0.1f);          // red at contact
        [Tooltip("Study mode: obstacle meshes stay invisible except the Visual-condition glow. Untick for the " +
                 "legacy always-visible tinted boxes (debugging only — never for a participant).")]
        [SerializeField] private bool hideObstacles = true;
        [Tooltip("Peak opacity of the glow veil at contact (hidden mode).")]
        [Range(0f, 1f)] [SerializeField] private float maxAlpha = 0.6f;

        private sealed class Target
        {
            public Obstacle Obstacle;
            public Renderer Renderer;
            public MaterialPropertyBlock Mpb;
            public Color BaseColor;
            public float Display;   // smoothed 0..1 brightness (presence-aware)
        }

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private readonly List<Target> _targets = new();
        private IReadOnlyList<Joint> _limbs;
        private float _alertDistance;
        private Material _glowMaterial; // shared transparent unlit veil (created once; per-target colour via MPB)

        // Hide the obstacle meshes from the VERY FIRST frame — before any block starts and in every condition —
        // so the participant never sees the hazard volumes while sitting at an operator gate.
        private void Awake()
        {
            if (!hideObstacles) return;
            SceneObstacles root = FindFirstObjectByType<SceneObstacles>();
            if (root == null) return;
            foreach (Renderer r in root.GetComponentsInChildren<Renderer>())
                r.enabled = false;
        }

        /// <summary>Map each obstacle Id to its scene Renderer (by GameObject name) and record the D used.</summary>
        public void Configure(IReadOnlyList<Obstacle> obstacles, IReadOnlyList<Joint> limbs, float alertDistance)
        {
            _limbs = limbs;
            _alertDistance = reactiveDistanceOverride > 0f ? reactiveDistanceOverride : alertDistance;
            _targets.Clear();

            foreach (Obstacle o in obstacles)
            {
                GameObject go = GameObject.Find(o.Id);
                Renderer rend = go != null ? go.GetComponentInChildren<Renderer>() : null;
                if (rend == null)
                {
                    Debug.LogWarning($"[VisualObstacleAlert] No scene Renderer named '{o.Id}' to highlight.");
                    continue;
                }

                if (hideObstacles)
                {
                    // Swap in the translucent veil and stay invisible until the glow needs to show.
                    if (_glowMaterial == null) _glowMaterial = CreateGlowMaterial();
                    if (_glowMaterial != null) rend.sharedMaterial = _glowMaterial;
                    rend.enabled = false;
                }

                _targets.Add(new Target
                {
                    Obstacle = o,
                    Renderer = rend,
                    Mpb = new MaterialPropertyBlock(),
                    BaseColor = ReadBaseColor(rend),
                });
            }
        }

        /// <summary>Refresh the highlights. <paramref name="active"/> is true only in the Visual condition;
        /// otherwise everything fades to (and stays) fully invisible.</summary>
        public void UpdatePose(in PoseFrame frame, bool active, float deltaTime)
        {
            for (int i = 0; i < _targets.Count; i++)
            {
                Target t = _targets[i];

                float target = 0f, pulseHz = 0f;
                if (active && _limbs != null)
                {
                    VisualAlertLevel level = VisualAlertModel.Evaluate(MinLimbDistance(t.Obstacle, frame), _alertDistance);
                    if (level.Active) { target = level.Intensity; pulseHz = level.PulseHz; }
                }

                t.Display = Mathf.MoveTowards(t.Display, target, fadePerSecond * deltaTime); // presence-aware fade

                float shown = t.Display;
                if (shown > 0.001f && pulseHz > 0f)
                    shown *= 0.55f + 0.45f * Mathf.Sin(2f * Mathf.PI * pulseHz * Time.unscaledTime);

                ApplyGlow(t, t.Display, shown);
            }
        }

        // Render one obstacle's glow at the given proximity level (hue) and instantaneous brightness (alpha).
        // Shared by the live per-frame path and the cue-tour demo so both look identical.
        private void ApplyGlow(Target t, float display, float shown)
        {
            if (hideObstacles)
            {
                // Invisible until the glow is needed; a translucent veil whose hue = proximity, alpha = level.
                bool visible = shown > 0.004f;
                if (t.Renderer.enabled != visible) t.Renderer.enabled = visible;
                if (!visible) return;

                Color glow = Color.Lerp(farColor, nearColor, display);
                glow.a = Mathf.Clamp01(shown) * maxAlpha;
                t.Mpb.SetColor(BaseColorId, glow);
                t.Mpb.SetColor(ColorId, glow);
                t.Renderer.SetPropertyBlock(t.Mpb);
            }
            else
            {
                // Legacy debug view: box always visible, glow = tint toward the proximity colour.
                Color glowC = Color.Lerp(farColor, nearColor, display);
                Color c = Color.Lerp(t.BaseColor, glowC, Mathf.Clamp01(shown));
                t.Mpb.SetColor(BaseColorId, c);
                t.Mpb.SetColor(ColorId, c);
                t.Renderer.SetPropertyBlock(t.Mpb);
            }
        }

        /// <summary>
        /// Cue-tour demo [Design v2 §7]: ramp the glow in and out on the first configured obstacle so every
        /// participant SEES the Visual alert once before any measured block (removes first-block novelty
        /// without privileging a condition). Call <see cref="Configure"/> first; run as a coroutine.
        /// </summary>
        public System.Collections.IEnumerator DemoGlow(float seconds = 3f)
        {
            if (_targets.Count == 0) yield break;
            Target t = _targets[0];
            float duration = Mathf.Max(0.5f, seconds);
            float half = duration * 0.5f;

            for (float e = 0f; e < duration; e += Time.deltaTime)
            {
                float level = 1f - Mathf.Abs(e - half) / half;                         // triangle 0 → 1 → 0
                float pulse = 0.55f + 0.45f * Mathf.Sin(2f * Mathf.PI * 4f * Time.unscaledTime);
                ApplyGlow(t, level, level * pulse);
                yield return null;
            }
            t.Display = 0f;
            ApplyGlow(t, 0f, 0f); // back to fully invisible
        }

        private float MinLimbDistance(Obstacle o, in PoseFrame frame)
        {
            float min = float.PositiveInfinity;
            for (int i = 0; i < _limbs.Count; i++)
            {
                Vector3 p = frame.Get(_limbs[i]);
                float d = Vector3.Distance(p, o.ClosestPoint(p));
                if (d < min) min = d;
            }
            return min;
        }

        // A transparent, unlit, double-sided material for the glow veil. URP Unlit configured for alpha
        // blending; falls back to Sprites/Default (always transparent-capable), and finally to null — in which
        // case the obstacle's own material is kept and the glow becomes an opaque pop-in (still invisible when
        // idle, just less pretty when alerting).
        private static Material CreateGlowMaterial()
        {
            Shader s = Shader.Find("Universal Render Pipeline/Unlit");
            if (s != null)
            {
                var m = new Material(s);
                m.SetFloat("_Surface", 1f);                                  // 1 = Transparent
                m.SetFloat("_Blend", 0f);                                    // 0 = Alpha blend
                m.SetOverrideTag("RenderType", "Transparent");
                m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                m.SetFloat("_ZWrite", 0f);
                m.SetFloat("_Cull", (float)CullMode.Off);                    // visible even with the limb inside
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.renderQueue = (int)RenderQueue.Transparent;
                return m;
            }

            s = Shader.Find("Sprites/Default");
            if (s != null) return new Material(s);

            Debug.LogWarning("[VisualObstacleAlert] No transparent shader found — the glow will re-use the " +
                             "obstacle's own material (opaque pop-in while alerting).");
            return null;
        }

        private static Color ReadBaseColor(Renderer rend)
        {
            Material m = rend.sharedMaterial;
            if (m == null) return Color.white;
            if (m.HasProperty(BaseColorId)) return m.GetColor(BaseColorId);
            if (m.HasProperty(ColorId)) return m.GetColor(ColorId);
            return Color.white;
        }
    }
}
