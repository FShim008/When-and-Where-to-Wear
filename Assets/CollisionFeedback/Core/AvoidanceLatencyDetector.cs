using System.Collections.Generic;

namespace CollisionFeedback.Core
{
    /// <summary>One measured avoidance response: the latency from an alert to the limb beginning to avoid.</summary>
    public readonly struct AvoidanceEvent
    {
        public readonly Joint Limb;
        public readonly double AlertTime;
        public readonly double AvoidanceTime;    // onset of avoidance = peak approach speed (deceleration begins)
        public readonly double LatencySeconds;   // AvoidanceTime - AlertTime (>= 0)

        public AvoidanceEvent(Joint limb, double alertTime, double avoidanceTime, double latencySeconds)
        {
            Limb = limb;
            AlertTime = alertTime;
            AvoidanceTime = avoidanceTime;
            LatencySeconds = latencySeconds;
        }
    }

    /// <summary>
    /// Avoidance latency [Protocol 9]: time from an alert onset to the participant's FIRST avoidance
    /// movement of the at-risk limb. "First avoidance movement" is operationalized as the onset of
    /// deceleration of the approach — the moment the limb's closing speed peaks and then drops (the
    /// participant stops driving toward the obstacle), or the distance outright reverses. This fires even
    /// when the limb "slows but keeps closing" (a braking response short of a full turnaround), which a
    /// distance-minimum test would miss, and it timestamps the response at the deceleration onset (peak
    /// approach speed) rather than at the later distance minimum.
    ///
    /// Body-localized cue -> watch the cued limb; Generic cue -> watch ALL limbs and take the first to
    /// decelerate. Hardware-free and deterministic. If a limb keeps approaching at ~full speed (no braking,
    /// no reversal) before the data ends, no event is recorded for that alert.
    /// </summary>
    public sealed class AvoidanceLatencyDetector
    {
        private sealed class LimbState
        {
            public float PrevDist;
            public double PrevTime;
            public float MinDist;
            public float PeakClosingSpeed;   // m/s, max approach speed seen since the alert
            public double PeakTime;          // time of that peak = deceleration onset
        }

        private sealed class Pending
        {
            public Joint AtRiskLimb;
            public bool AnyLimb;
            public double AlertTime;
            public bool Resolved;
            public readonly Dictionary<Joint, LimbState> States = new();
        }

        private readonly float _decelFraction;    // avoidance once closing speed falls to <= this * peak
        private readonly float _minClosingSpeed;  // ignore peaks below this (no real approach to brake from) [m/s]
        private readonly float _riseEpsilon;      // distance-reversal fallback [m]
        private readonly List<Pending> _pending = new();
        private readonly List<AvoidanceEvent> _events = new();

        public IReadOnlyList<AvoidanceEvent> Events => _events;

        public AvoidanceLatencyDetector(float decelFraction = 0.5f, float minClosingSpeed = 0.05f,
                                        float riseEpsilon = 0.02f)
        {
            _decelFraction = decelFraction;
            _minClosingSpeed = minClosingSpeed;
            _riseEpsilon = riseEpsilon;
        }

        public void RegisterAlert(double t, Joint atRiskLimb, bool anyLimb)
        {
            _pending.Add(new Pending { AtRiskLimb = atRiskLimb, AnyLimb = anyLimb, AlertTime = t });
        }

        /// <summary>Advance with the current per-limb distance to nearest obstacle.</summary>
        public void Tick(double t, IReadOnlyDictionary<Joint, float> distanceByLimb)
        {
            for (int i = 0; i < _pending.Count; i++)
            {
                Pending p = _pending[i];
                if (p.Resolved || t < p.AlertTime) continue;

                if (p.AnyLimb)
                {
                    foreach (var kv in distanceByLimb)
                        if (Consider(p, kv.Key, kv.Value, t)) break;
                }
                else if (distanceByLimb.TryGetValue(p.AtRiskLimb, out float d))
                {
                    Consider(p, p.AtRiskLimb, d, t);
                }
            }
        }

        /// <summary>Updates a limb's approach profile; emits an event at the first sign of avoidance —
        /// deceleration past <see cref="_decelFraction"/> of the peak approach speed, or a distance reversal.</summary>
        private bool Consider(Pending p, Joint limb, float d, double t)
        {
            if (!p.States.TryGetValue(limb, out var s))
            {
                p.States[limb] = new LimbState
                {
                    PrevDist = d, PrevTime = t, MinDist = d, PeakClosingSpeed = 0f, PeakTime = t,
                };
                return false;
            }

            double dt = t - s.PrevTime;
            if (dt <= 0) return false;   // ignore out-of-order / duplicate timestamps

            float closingSpeed = (float)((s.PrevDist - d) / dt);   // + = approaching the obstacle
            s.PrevDist = d;
            s.PrevTime = t;

            if (d < s.MinDist) s.MinDist = d;
            // >= so a plateau at peak speed attributes the deceleration onset to its LAST sample.
            if (closingSpeed >= s.PeakClosingSpeed) { s.PeakClosingSpeed = closingSpeed; s.PeakTime = t; }

            // Need a real approach first — there must be something to decelerate from.
            if (s.PeakClosingSpeed < _minClosingSpeed) return false;

            bool decelerating = closingSpeed <= _decelFraction * s.PeakClosingSpeed;
            bool reversed = d > s.MinDist + _riseEpsilon;
            if (decelerating || reversed)
            {
                _events.Add(new AvoidanceEvent(limb, p.AlertTime, s.PeakTime, s.PeakTime - p.AlertTime));
                p.Resolved = true;
                return true;
            }

            return false;
        }
    }
}
