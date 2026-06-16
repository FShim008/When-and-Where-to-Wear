using System.Collections.Generic;

namespace CollisionFeedback.Core
{
    /// <summary>
    /// Arbitrates between SAFETY cues and in-game (gameplay) haptics so a safety cue is never masked
    /// (-> [Protocol 3E.5]). Policy: a safety cue always fires; an in-game haptic on the SAME site within
    /// the safety cue's hold window is suppressed (the safety meaning wins that site). In-game haptics on
    /// OTHER sites pass through. Pure decision helper; the Runtime feeds it the two streams.
    /// </summary>
    public sealed class TactorArbiter
    {
        private readonly double _safetyHoldSeconds;
        private readonly Dictionary<HapticSite, double> _safetyUntil = new();

        public TactorArbiter(double safetyHoldSeconds = 0.3) { _safetyHoldSeconds = safetyHoldSeconds; }

        /// <summary>Record that a safety cue fired on a site at time t (call when a safety command fires).</summary>
        public void NoteSafetyCue(HapticSite site, double t) => _safetyUntil[site] = t + _safetyHoldSeconds;

        /// <summary>True if an in-game haptic on <paramref name="site"/> at time <paramref name="t"/> may
        /// fire; false while a safety cue still holds that site.</summary>
        public bool AllowGameHaptic(HapticSite site, double t)
            => !(_safetyUntil.TryGetValue(site, out double until) && t < until);
    }
}
