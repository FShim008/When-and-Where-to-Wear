using System.Collections.Generic;
using UnityEngine;

namespace CollisionFeedback.Core
{
    /// <summary>
    /// Tunable thresholds. Pilot-tuned toward COMPARABLE alert rates across timing (StudyDesign section 5),
    /// so the manipulated variable is timing, not alert frequency.
    /// </summary>
    public sealed class OracleParams
    {
        public float ReactiveDistance = 0.30f;        // D: fire when within this distance (m) and closing
        public float PredictiveTtc = 0.50f;            // T: fire when TTC (s) drops below this and closing
        public float ReactiveReleaseMargin = 0.15f;    // re-arm once distance exceeds D + this (hysteresis)
        public float PredictiveReleaseMargin = 0.20f;  // re-arm once TTC exceeds T + this (hysteresis)
        public float PipelineLatencySeconds = 0f;      // forecast-ahead: fire when TTC < T + this, so the cue
                                                       // ARRIVES ~T before contact despite the motion->tactor delay [3D.3]
        public float VelocitySmoothing = 0.5f;         // EMA factor for the constant-velocity estimator [0..1]
        public float MinClosingSpeed = 0.05f;          // m/s; below this the limb is treated as "not closing"
        public float RefireDebounceSeconds = 1.0f;     // global: at most one safety cue per this interval [Protocol 2.1];
                                                       // also makes multi-limb arbitration cue ONE limb per engagement
    }

    /// <summary>
    /// Per-limb risk for the current frame. We expose the NEAREST obstacle (by distance) and the
    /// SOONEST obstacle (by TTC) as fully self-consistent triples — each obstacle id carries ITS OWN
    /// distance and TTC, so a consumer never pairs one obstacle's id with another's distance.
    /// </summary>
    public readonly struct RiskReading
    {
        public readonly Joint Limb;

        public readonly float MinDistance;          // m to the NEAREST obstacle surface
        public readonly string NearestObstacleId;
        public readonly float NearestTtc;           // s to the nearest obstacle (+Inf if not closing toward it)

        public readonly float MinTtc;               // s to the SOONEST obstacle (+Inf if none closing)
        public readonly string SoonestObstacleId;   // null if nothing is closing
        public readonly float SoonestDistance;      // m to the soonest obstacle (+Inf if none closing)

        public readonly bool Closing;               // is the limb closing on at least one obstacle?

        public RiskReading(Joint limb, float minDistance, string nearestObstacleId, float nearestTtc,
                           float minTtc, string soonestObstacleId, float soonestDistance, bool closing)
        {
            Limb = limb;
            MinDistance = minDistance;
            NearestObstacleId = nearestObstacleId;
            NearestTtc = nearestTtc;
            MinTtc = minTtc;
            SoonestObstacleId = soonestObstacleId;
            SoonestDistance = soonestDistance;
            Closing = closing;
        }
    }

    /// <summary>
    /// Deliberately-simple constant-velocity TTC estimator (NOT a learned / SOTA motion predictor).
    /// This keeps the manipulated variable *timing*, not predictor sophistication (StudyDesign section 5).
    /// The EMA velocity below can be swapped for a Kalman filter behind the same shape later.
    /// </summary>
    public sealed class CollisionOracle
    {
        private sealed class LimbTracker
        {
            private Vector3 _prevPos;
            private double _prevT;
            private bool _has;
            public Vector3 Velocity { get; private set; }

            public void Update(Vector3 pos, double t, float smoothing)
            {
                if (_has && t > _prevT)
                {
                    Vector3 instV = (pos - _prevPos) / (float)(t - _prevT);
                    Velocity = Vector3.Lerp(Velocity, instV, smoothing);
                }
                _prevPos = pos;
                _prevT = t;
                _has = true;
            }
        }

        private readonly IReadOnlyList<Obstacle> _obstacles;
        private readonly OracleParams _p;
        private readonly Dictionary<Joint, LimbTracker> _trackers = new();

        public CollisionOracle(IReadOnlyList<Obstacle> obstacles, OracleParams p)
        {
            _obstacles = obstacles;
            _p = p;
        }

        /// <summary>Advances the per-limb velocity estimate for every tracked limb. Call once per frame.</summary>
        public void UpdateVelocities(in PoseFrame frame, IReadOnlyList<Joint> limbs)
        {
            for (int i = 0; i < limbs.Count; i++)
            {
                Joint j = limbs[i];
                if (!_trackers.TryGetValue(j, out var tr))
                {
                    tr = new LimbTracker();
                    _trackers[j] = tr;
                }
                tr.Update(frame.Get(j), frame.Timestamp, _p.VelocitySmoothing);
            }
        }

        /// <summary>Reads risk for one limb against all obstacles. Call <see cref="UpdateVelocities"/> first.</summary>
        public RiskReading Read(Joint limb, in PoseFrame frame)
        {
            Vector3 p = frame.Get(limb);
            Vector3 v = _trackers.TryGetValue(limb, out var tr) ? tr.Velocity : Vector3.zero;

            float minDist = float.PositiveInfinity;
            string nearestId = null;
            float nearestTtc = float.PositiveInfinity;

            float minTtc = float.PositiveInfinity;
            string soonestId = null;
            float soonestDist = float.PositiveInfinity;

            bool anyClosing = false;

            for (int i = 0; i < _obstacles.Count; i++)
            {
                Obstacle o = _obstacles[i];
                Vector3 closest = o.ClosestPoint(p);
                float dist = Vector3.Distance(p, closest);

                // TTC toward THIS obstacle (Inf unless the limb is closing on it).
                float ttc = float.PositiveInfinity;
                Vector3 dir = closest - p;
                if (dir.sqrMagnitude > 1e-8f)
                {
                    float closingSpeed = Vector3.Dot(v, dir.normalized);
                    if (closingSpeed > _p.MinClosingSpeed)
                    {
                        anyClosing = true;
                        ttc = dist / closingSpeed;
                    }
                }

                if (dist < minDist)
                {
                    minDist = dist;
                    nearestId = o.Id;
                    nearestTtc = ttc;
                }

                if (!float.IsPositiveInfinity(ttc) && ttc < minTtc)
                {
                    minTtc = ttc;
                    soonestId = o.Id;
                    soonestDist = dist;
                }
            }

            return new RiskReading(limb, minDist, nearestId, nearestTtc, minTtc, soonestId, soonestDist, anyClosing);
        }
    }
}
