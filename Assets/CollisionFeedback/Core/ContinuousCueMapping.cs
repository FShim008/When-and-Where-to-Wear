namespace CollisionFeedback.Core
{
    /// <summary>
    /// Parameters of the continuous cue used by <see cref="Condition.PBC"/>.
    ///
    /// Shape follows Valkov and Linsen (IEEE VR 2019), who tested five transfer functions and found the
    /// gamma function f3 and the linear-step f4 performed best. We use the gamma form on the TTC axis with
    /// their gamma = 2. Using their BEST function rather than their worst is deliberate: H4' is a fair test
    /// of continuous mapping, not a straw man.
    /// </summary>
    public readonly struct ContinuousCueParams
    {
        /// <summary>TTC (s) at or above which the cue is silent. Mirrors their d_max.</summary>
        public readonly float TtcMax;

        /// <summary>TTC (s) at or below which the cue is at full intensity. Mirrors their d_min.</summary>
        public readonly float TtcMin;

        /// <summary>Curve exponent. 2 reproduces their f3 (gamma) function.</summary>
        public readonly float Gamma;

        /// <summary>
        /// Floor applied once engaged, so the cue is perceptible the instant it starts rather than fading
        /// in from zero. Their f4/f5 introduce a discontinuity at d_min for the same reason.
        /// </summary>
        public readonly float MinPerceptibleIntensity;

        public ContinuousCueParams(float ttcMax, float ttcMin, float gamma, float minPerceptibleIntensity)
        {
            TtcMax = ttcMax;
            TtcMin = ttcMin;
            Gamma = gamma;
            MinPerceptibleIntensity = minPerceptibleIntensity;
        }

        /// <summary>
        /// Defaults matched to the frozen predictive threshold: the cue becomes audible exactly when the
        /// PB trigger would have fired, so PB and PBC start at the same moment and differ only afterwards.
        /// <paramref name="predictiveThreshold"/> must be the SAME effective threshold the trigger uses
        /// (T + pipeline latency), or H4' silently confounds cue form with onset time.
        /// </summary>
        public static ContinuousCueParams ForThreshold(float predictiveThreshold) =>
            new(predictiveThreshold, 0f, 2f, 0.15f);
    }

    /// <summary>
    /// The continuous cue's intensity as a function of time-to-contact. Pure, deterministic, allocation-free.
    ///
    /// THE GUARANTEE IS THE SIGNATURE, as with <see cref="LimbCueVisual"/>. This function takes a TTC and
    /// nothing else. It cannot see which condition is running, which hazard is involved, where the limb is,
    /// or how fast it is moving, so it cannot leak information that PB does not also carry. PB and PBC
    /// therefore differ in exactly one respect — whether intensity varies over the approach.
    ///
    /// THE FEEDBACK LOOP IS INTENTIONAL. Because TTC = distance / closing speed, a participant who slows
    /// down raises TTC and so lowers intensity. That is precisely the behaviour Valkov and Linsen observed
    /// and blamed for their result, and reproducing it is the point of H4'. Removing it would destroy the
    /// manipulation.
    /// </summary>
    public static class ContinuousCueMapping
    {
        /// <summary>
        /// Intensity in [0, 1] for the given time-to-contact.
        /// Returns 0 at or above <see cref="ContinuousCueParams.TtcMax"/> (silent, disengaged) and 1 at or
        /// below <see cref="ContinuousCueParams.TtcMin"/>. Monotonically non-increasing in <paramref name="ttcSeconds"/>.
        /// </summary>
        public static float Intensity(float ttcSeconds, in ContinuousCueParams p)
        {
            // Not closing / no forecast: silent. NaN is treated as silent rather than full, because a
            // degraded estimate must never produce a maximum-intensity cue.
            if (float.IsNaN(ttcSeconds) || float.IsPositiveInfinity(ttcSeconds)) return 0f;
            if (ttcSeconds >= p.TtcMax) return 0f;
            if (ttcSeconds <= p.TtcMin) return 1f;

            float span = p.TtcMax - p.TtcMin;
            if (span <= 0f) return 1f;   // degenerate band: engaged means full

            // r = 0 at TtcMin (closest), 1 at TtcMax (furthest out in time).
            float r = (ttcSeconds - p.TtcMin) / span;
            float level = Pow(1f - r, p.Gamma);

            // Engaged cues never sit below the perceptible floor.
            float floor = p.MinPerceptibleIntensity;
            if (level < floor) level = floor;
            return level > 1f ? 1f : level;
        }

        /// <summary>
        /// Whether the continuous cue is engaged (non-silent) at this TTC. Equivalent to
        /// <see cref="Intensity"/> &gt; 0, expressed separately so callers do not compare floats.
        /// </summary>
        public static bool IsEngaged(float ttcSeconds, in ContinuousCueParams p) =>
            !float.IsNaN(ttcSeconds) && !float.IsPositiveInfinity(ttcSeconds) && ttcSeconds < p.TtcMax;

        // Core references nothing outside itself, so no System.MathF. Gamma is a small positive number in
        // practice; this handles the integer cases exactly and falls back to exp/log otherwise.
        private static float Pow(float b, float e)
        {
            if (b <= 0f) return 0f;
            if (e == 1f) return b;
            if (e == 2f) return b * b;
            if (e == 3f) return b * b * b;
            return (float)System.Math.Pow(b, e);
        }
    }
}
