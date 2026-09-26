using System.Collections.Generic;
using UnityEngine;
using CollisionFeedback.Core;
using Joint = CollisionFeedback.Core.Joint; // disambiguate from UnityEngine.Joint (physics component)

namespace CollisionFeedback.Runtime
{
    /// <summary>
    /// The Visual-condition benchmark cue [PAPER1_STUDY_DESIGN §2; CODE_GAP #2].
    ///
    /// A transient marker on the **at-risk limb**. It names which limb, and nothing else.
    ///
    /// ── WHY THIS REPLACES VisualObstacleAlert ─────────────────────────────────────────────────────────
    /// H4 compares PB against Visual to isolate **modality**. That requires the visual cue to carry exactly
    /// what the haptic cue carries. <see cref="VisualObstacleAlert"/> instead glows the hazard volume with a
    /// proximity-graded veil, which hands the Visual condition two things the haptic conditions never get:
    /// **where the hazard is**, and **how close the limb is to it**. A Visual advantage measured that way
    /// would show that revealing hazards helps — true, uninteresting, and not the paper's question.
    ///
    /// So this component:
    /// <list type="bullet">
    ///   <item>anchors to the limb, never to the hazard;</item>
    ///   <item>runs a fixed envelope from <see cref="LimbCueVisual"/>, duration-matched to the haptic pulse
    ///         train, so onset and duration are identical across modality;</item>
    ///   <item>takes no distance input, so it cannot grade with proximity even by accident.</item>
    /// </list>
    ///
    /// ── SEAM ──────────────────────────────────────────────────────────────────────────────────────────
    /// It is an <see cref="IFeedbackSink"/>, so `ConditionManager` drives it through the same path as the
    /// haptic sink and cannot tell them apart. Only commands with <see cref="Modality.Visual"/> are drawn;
    /// a haptic command passed here is ignored rather than silently rendered, so a mis-wired scene fails
    /// loudly instead of turning PB into a visual condition.
    ///
    /// Scene setup: one child marker per tracked limb, assigned below. Markers should be small, unlit,
    /// clearly visible in peripheral vision, and attached to the limb so they move with it.
    /// </summary>
    [DefaultExecutionOrder(-30)]
    public sealed class LimbCueIndicator : MonoBehaviour, IFeedbackSink
    {
        [Header("Limb markers — one per tracked limb, parented to that limb")]
        public Renderer leftHand;
        public Renderer rightHand;
        public Renderer leftFoot;
        public Renderer rightFoot;
        [Tooltip("Optional. Torso marker, used only if a generic visual arm is ever added. " +
                 "The confirmatory Visual condition is LOCALIZED and never uses this.")]
        public Renderer torso;

        [Header("Appearance")]
        [Tooltip("Cue colour. Keep it constant — a colour that varies with proximity would leak distance.")]
        public Color cueColor = new Color(1f, 0.82f, 0.25f);
        [Range(0f, 8f)] public float emission = 3.0f;

        [Header("Timing — must match the haptic waveform")]
        [Tooltip("Pulses in the train. Match HapticDeviceBinding.CreateThreePulseSink.")]
        public int pulses = 3;
        public float pulseSeconds = 0.100f;
        public float gapSeconds = 0.060f;
        [Tooltip("Rise/fall inside each pulse, to avoid a hard flicker edge.")]
        public float edgeSeconds = 0.015f;

        private static readonly int ColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        private readonly Dictionary<Joint, Renderer> _markers = new Dictionary<Joint, Renderer>();
        private MaterialPropertyBlock _mpb;
        private Joint _activeLimb;
        private float _onsetTime = float.NegativeInfinity;
        private bool _active;

        /// <summary>The envelope this instance runs. Pure Core; unit-tested without a scene.</summary>
        public LimbCueVisualParams Envelope =>
            new LimbCueVisualParams(pulses, pulseSeconds, gapSeconds, edgeSeconds);

        private void Awake()
        {
            _mpb = new MaterialPropertyBlock();
            Register(Joint.LeftHand, leftHand);
            Register(Joint.RightHand, rightHand);
            Register(Joint.LeftFoot, leftFoot);
            Register(Joint.RightFoot, rightFoot);

            if (_markers.Count == 0)
                Debug.LogError("[LimbCueIndicator] No limb markers assigned. The Visual condition will " +
                               "show nothing and H4 cannot be collected.");

            HideAll();

            if (!Mathf.Approximately(Envelope.TotalSeconds, 0.420f))
                Debug.LogWarning($"[LimbCueIndicator] Cue train is {Envelope.TotalSeconds * 1000f:F0} ms. " +
                                 "The haptic train is 420 ms. H4 isolates modality only if the two are " +
                                 "duration-matched — change both or neither.");
        }

        private void Register(Joint j, Renderer r)
        {
            if (r != null) _markers[j] = r;
        }

        /// <summary>
        /// Driven by `ConditionManager` through the ordinary sink path. Visual commands only.
        /// </summary>
        public void Fire(in FeedbackCommand command)
        {
            if (command.Modality != Modality.Visual)
            {
                // A haptic command reaching the visual sink means the scene is wired wrong. Surfacing it
                // matters: silently drawing it would turn a haptic condition into a visual one.
                Debug.LogWarning($"[LimbCueIndicator] Ignoring a {command.Modality} command. This sink " +
                                 "renders the Visual benchmark only.");
                return;
            }

            _activeLimb = command.Limb;
            _onsetTime = Time.time;
            _active = true;
        }

        private void Update()
        {
            if (!_active) return;

            float since = Time.time - _onsetTime;
            if (!LimbCueVisual.IsActive(since, Envelope))
            {
                HideAll();
                _active = false;
                return;
            }

            // NOTE the argument list: time only. No distance, no hazard. That is the H4 guarantee.
            float intensity = LimbCueVisual.Intensity(since, Envelope);

            foreach (KeyValuePair<Joint, Renderer> kv in _markers)
                Paint(kv.Value, kv.Key == _activeLimb ? intensity : 0f);
        }

        private void Paint(Renderer r, float intensity)
        {
            if (r == null) return;
            r.enabled = intensity > 0.001f;
            if (!r.enabled) return;

            Color c = cueColor * intensity;
            c.a = intensity;
            r.GetPropertyBlock(_mpb);
            _mpb.SetColor(ColorId, c);
            _mpb.SetColor(EmissionId, cueColor * (emission * intensity));
            r.SetPropertyBlock(_mpb);
        }

        private void HideAll()
        {
            foreach (KeyValuePair<Joint, Renderer> kv in _markers)
                if (kv.Value != null) kv.Value.enabled = false;
        }

        private void OnDisable() => HideAll();
    }
}
