using System.Collections.Generic;
using UnityEngine;

namespace CollisionFeedback.Core
{
    /// <summary>
    /// Tunable thresholds. Pilot-tuned toward COMPARABLE alert rates across timing (StudyDesign section 5),
    /// so the manipulated variable is timing, not alert frequency.
    ///
    /// ── WHY D AND T ARE WHAT THEY ARE [PAPER1_STUDY_DESIGN §6, revised 2026-09-14] ───────────────────────
    /// Proximity fires at a fixed DISTANCE D. Predictive fires at a fixed TIME T, i.e. at distance v·T for a
    /// limb closing at speed v. So predictive leads proximity only while v > D/T. **T was 0.50 s, which put
    /// that crossover at 0.60 m/s — inside the range of real limb speeds, so the timing factor INVERTED on
    /// slow approaches.**
    ///
    /// Simulation against this oracle (minimum-jerk reaches, 0.4–1.4 s movement times) showed the dominant
    /// driver is not speed but **clear approach distance**: when a limb starts closer to the hazard than D,
    /// the proximity cue fires at movement onset and predictive cannot possibly lead it. Raising T shrinks
    /// that dead zone but cannot remove it.
    ///
    /// T = 1.00 s is the chosen operating point. Measured minimum approach distance for predictive to lead by
    /// ≥ 50 ms at every movement time tested:
    ///
    ///     D=0.30 T=0.50 → 0.55 m      D=0.30 T=0.80 → 0.45 m
    ///     D=0.30 T=1.00 → 0.40 m      D=0.25 T=1.00 → 0.35 m
    ///
    /// D stays at 0.30 m: dropping it to 0.25 buys only 0.05 m while making the reactive cue less actionable,
    /// and the reactive condition has to remain a plausible warning rather than a token one.
    ///
    /// **This parameter change is necessary but NOT sufficient.** Every scheduled opportunity must also give
    /// the target limb at least <see cref="MinApproachDistanceForValidTiming"/> of clear approach to its
    /// target hazard. Auditing the opportunity schedule against that is launch gate §14.27.
    /// ─────────────────────────────────────────────────────────────────────────────────────────────────────
    /// </summary>
    public sealed class OracleParams
    {
        /// <summary>
        /// Clear limb-to-hazard distance an opportunity must provide at onset for the timing manipulation to
        /// be valid at the current D and T. Below this the proximity cue fires at movement onset and the
        /// predictive cue cannot lead it, so the opportunity silently contributes a reversed or null
        /// manipulation to H1. Derived by simulation; re-derive if D or T change.
        /// </summary>
        public const float MinApproachDistanceForValidTiming = 0.40f;

        /// <summary>Design target for opportunity geometry, with margin over the hard floor above.</summary>
        public const float RecommendedApproachDistance = 0.60f;

        public float ReactiveDistance = 0.30f;        // D: fire when within this distance (m) and closing
        public float PredictiveTtc = 1.00f;            // T: fire when TTC (s) drops below this and closing.
                                                       // Raised from 0.50 on 2026-09-14 — see the class note.
        public float ReactiveReleaseMargin = 0.15f;    // re-arm once distance exceeds D + this (hysteresis)
        public float PredictiveReleaseMargin = 0.40f;  // re-arm once TTC exceeds T + this (hysteresis).
                                                       // Held at 40% of T, as when T was 0.50/0.20.
        public float PipelineLatencySeconds = 0f;      // forecast-ahead: fire when TTC < T + this, so the cue
                                                       // ARRIVES ~T before contact despite the motion->tactor delay [3D.3]
        public float VelocitySmoothing = 0.5f;         // EMA factor for the constant-velocity estimator [0..1]
        public float MinClosingSpeed = 0.05f;          // m/s; below this the limb is treated as "not closing"
        public float RefireDebounceSeconds = 1.0f;     // global: at most one safety cue per this interval [Protocol 2.1];
                                                       // also makes multi-limb arbitration cue ONE limb per engagement

        // --- Continuous cue (Condition.PBC only) [H4'; docs/PAPER1_PARAMETER_JUSTIFICATION.md §5B] ---
        // Preregistered here rather than left as literals, because they define a manipulated condition and
        // must be frozen and reportable like D and T. The TTC band itself is NOT a separate parameter: it
        // is derived from PredictiveTtc + PipelineLatencySeconds so PB and PBC cannot drift apart in onset.

        /// <summary>
        /// Curve exponent for the continuous cue. 2 reproduces the gamma function f3 of Valkov and Linsen
        /// (IEEE VR 2019), one of the two forms that performed best in their evaluation. Using their best
        /// function rather than their worst keeps H4' a fair test instead of a straw man.
        /// </summary>
        public float ContinuousCueGamma = 2.0f;

        /// <summary>
        /// Intensity floor once engaged, so the continuous cue is perceptible the moment it begins instead
        /// of fading in from silence. Their f4/f5 introduce a discontinuity at d_min for the same reason.
        /// Freeze from the pilot: too low and PBC's onset is effectively later than PB's, which would
        /// confound cue form with timing.
        /// </summary>
        public float ContinuousCueMinIntensity = 0.15f;
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
    /// One limb measured against ONE designated obstacle, for counterfactual policy evaluation.
    /// </summary>
    public readonly struct TargetReading
    {
        /// <summary>Metres from the limb point to the obstacle surface (no limb radius applied).</summary>
        public readonly float Distance;
        /// <summary>Seconds to contact at the current closing speed; +Inf when not closing on this obstacle.</summary>
        public readonly float Ttc;
        /// <summary>Component of limb velocity along the limb→obstacle direction (m/s; negative = retreating).</summary>
        public readonly float ClosingSpeed;
        /// <summary>Closing on THIS obstacle faster than <see cref="OracleParams.MinClosingSpeed"/>.</summary>
        public readonly bool Closing;

        public TargetReading(float distance, float ttc, float closingSpeed, bool closing)
        {
            Distance = distance;
            Ttc = ttc;
            ClosingSpeed = closingSpeed;
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

        /// <summary>
        /// Reads one limb against ONE designated obstacle — the target limb × target hazard pair the study
        /// actually schedules [PAPER1_STUDY_DESIGN §6]. <see cref="Read"/> answers "nearest / soonest of all
        /// obstacles", which is the right question for driving a cue but the wrong one for measuring whether a
        /// specific scheduled opportunity's manipulation behaved as designed.
        ///
        /// Used by <see cref="PolicyTriggerProbe"/> to evaluate both warning policies counterfactually.
        /// Call <see cref="UpdateVelocities"/> first.
        /// </summary>
        public TargetReading ReadAgainst(Joint limb, in Obstacle target, in PoseFrame frame)
        {
            Vector3 p = frame.Get(limb);
            Vector3 v = _trackers.TryGetValue(limb, out var tr) ? tr.Velocity : Vector3.zero;

            Vector3 closest = target.ClosestPoint(p);
            float dist = Vector3.Distance(p, closest);

            Vector3 dir = closest - p;
            float closingSpeed = 0f;
            float ttc = float.PositiveInfinity;
            if (dir.sqrMagnitude > 1e-8f)
            {
                closingSpeed = Vector3.Dot(v, dir.normalized);
                if (closingSpeed > _p.MinClosingSpeed) ttc = dist / closingSpeed;
            }

            return new TargetReading(dist, ttc, closingSpeed, closingSpeed > _p.MinClosingSpeed);
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
