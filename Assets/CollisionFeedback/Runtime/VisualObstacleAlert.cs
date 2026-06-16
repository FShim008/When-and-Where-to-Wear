using System.Collections.Generic;
using UnityEngine;
using CollisionFeedback.Core;
using Joint = CollisionFeedback.Core.Joint; // disambiguate from UnityEngine.Joint (physics component)

namespace CollisionFeedback.Runtime
{
    /// <summary>
    /// Renders the Visual-condition obstacle alert [Protocol 2.3]: a proximity-graded, depth-coloured pulse on
    /// the at-risk obstacle, driven by the SAME tracking/distance the haptic conditions use (detection-matched).
    /// The grading math is <see cref="VisualAlertModel"/> (Core, tested); this only drives the scene highlight.
    ///
    /// Drive it from the session loop: <see cref="Configure"/> once (obstacles + tracked limbs + the reactive
    /// distance D), then <see cref="UpdatePose"/> every frame with <c>active = true</c> only in the Visual
    /// condition. It maps each obstacle Id to the scene GameObject of the SAME name (the SceneObstacles
    /// convention) and tints/pulses that renderer; presence-aware (smooth fades, never a hard spike).
    ///
    /// First-cut highlight = a depth-coloured material tint + pulse (no audio, per Protocol §2.3). A dedicated
    /// outline shader can replace the tint later without touching the grading.
    /// </summary>
    public sealed class VisualObstacleAlert : MonoBehaviour
    {
        [SerializeField] private float reactiveDistanceOverride = 0f;            // 0 = use the D from Configure
        [SerializeField] private float fadePerSecond = 6f;                       // presence-aware smoothing rate
        [SerializeField] private Color farColor = new(0.2f, 1f, 0.2f);           // green near the alert edge
        [SerializeField] private Color nearColor = new(1f, 0.2f, 0.1f);          // red at contact

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
        /// otherwise everything fades to off.</summary>
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

                Color glow = Color.Lerp(farColor, nearColor, t.Display);   // hue = proximity
                Color c = Color.Lerp(t.BaseColor, glow, Mathf.Clamp01(shown));
                t.Mpb.SetColor(BaseColorId, c);
                t.Mpb.SetColor(ColorId, c);
                t.Renderer.SetPropertyBlock(t.Mpb);
            }
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
