namespace CollisionFeedback.Core
{
    /// <summary>
    /// Shape of the Visual-condition cue [PAPER1_STUDY_DESIGN §2; CODE_GAP #2].
    ///
    /// ── WHAT H4 NEEDS, AND WHY THE OLD COMPONENT BROKE IT ─────────────────────────────────────────────
    /// H4 compares **PB against Visual** and is meant to isolate **modality** — haptic versus visual, with
    /// timing and information held constant. That only works if the visual cue carries *exactly* what the
    /// haptic cue carries: **which limb is at risk, and nothing else.**
    ///
    /// `Runtime/VisualObstacleAlert` does the opposite. It renders a proximity-graded glow **on the hazard
    /// volume**, so the Visual condition would reveal
    ///   - *where the hazard is* (the haptic conditions never disclose this), and
    ///   - *how close the limb is* (a continuous distance readout the haptic cue does not provide).
    ///
    /// A Visual advantage measured that way would say nothing about modality. It would say that showing
    /// someone the hazard helps — which is not in doubt and is not the paper's question. **That is the H4
    /// invalidity**, and it is more damaging than the trigger mismatch already fixed in
    /// <see cref="ConditionManager"/>, because it inflates the benchmark instead of merely mistiming it.
    ///
    /// ── THE GUARANTEE THIS TYPE ENFORCES ──────────────────────────────────────────────────────────────
    /// <see cref="Intensity"/> takes **only time since cue onset**. It has no distance argument, no hazard
    /// argument, and no position argument, so it is not *able* to leak hazard geometry or proximity. The
    /// information content is fixed by the type signature rather than by a comment asking future code to
    /// behave. Keep it that way: adding a distance parameter here re-breaks H4.
    ///
    /// Timing is matched to the haptic pulse train so the two modalities have the same onset and the same
    /// duration [Protocol 2.1].
    /// </summary>
    public readonly struct LimbCueVisualParams
    {
        public readonly int Pulses;
        public readonly float PulseSeconds;
        public readonly float GapSeconds;
        /// <summary>Rise/fall inside each pulse, to avoid a hard flicker edge. Kept small.</summary>
        public readonly float EdgeSeconds;

        public LimbCueVisualParams(int pulses, float pulseSeconds, float gapSeconds, float edgeSeconds)
        {
            Pulses = pulses;
            PulseSeconds = pulseSeconds;
            GapSeconds = gapSeconds;
            EdgeSeconds = edgeSeconds;
        }

        /// <summary>
        /// Matches <c>HapticDeviceBinding.CreateThreePulseSink</c>: 3 pulses of 100 ms with 60 ms gaps.
        /// **If the haptic waveform changes, change this too** — H4 depends on the two being duration-matched.
        /// </summary>
        public static LimbCueVisualParams MatchedToThreePulse =>
            new LimbCueVisualParams(pulses: 3, pulseSeconds: 0.100f, gapSeconds: 0.060f, edgeSeconds: 0.015f);

        /// <summary>Total cue duration (s). 3 pulses of 100 ms with 60 ms gaps = 420 ms.</summary>
        public float TotalSeconds => Pulses * PulseSeconds + (Pulses > 0 ? (Pulses - 1) * GapSeconds : 0f);
    }

    /// <summary>
    /// The visual cue's envelope over time. Pure, deterministic, and deliberately blind to geometry.
    /// </summary>
    public static class LimbCueVisual
    {
        /// <summary>
        /// Cue brightness at <paramref name="secondsSinceOnset"/>, in 0..1.
        ///
        /// Zero before onset and after the train completes, so the cue is **transient** — it never becomes a
        /// persistent state display the participant can read continuously.
        /// </summary>
        public static float Intensity(float secondsSinceOnset, in LimbCueVisualParams p)
        {
            if (p.Pulses <= 0 || p.PulseSeconds <= 0f) return 0f;
            if (secondsSinceOnset < 0f || secondsSinceOnset >= p.TotalSeconds) return 0f;

            float period = p.PulseSeconds + p.GapSeconds;
            int index = period > 0f ? (int)(secondsSinceOnset / period) : 0;
            if (index >= p.Pulses) return 0f;

            float withinPulse = secondsSinceOnset - index * period;
            if (withinPulse >= p.PulseSeconds) return 0f;          // inside a gap

            float edge = p.EdgeSeconds;
            if (edge <= 0f) return 1f;
            if (withinPulse < edge) return withinPulse / edge;                       // rise
            if (withinPulse > p.PulseSeconds - edge)
                return (p.PulseSeconds - withinPulse) / edge;                        // fall
            return 1f;
        }

        /// <summary>True while the cue is still running. False once the train has finished.</summary>
        public static bool IsActive(float secondsSinceOnset, in LimbCueVisualParams p) =>
            secondsSinceOnset >= 0f && secondsSinceOnset < p.TotalSeconds;

        /// <summary>
        /// Which body site the indicator is drawn on. Identical routing to the haptic cue, so the two
        /// modalities name the same limb — that identity is what makes H4 a modality comparison.
        /// </summary>
        public static HapticSite SiteFor(Joint limb) => SiteRouting.For(limb);
    }
}
