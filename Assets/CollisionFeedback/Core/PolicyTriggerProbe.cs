using System.Collections.Generic;

namespace CollisionFeedback.Core
{
    /// <summary>What each policy would have done on one scheduled opportunity.</summary>
    public struct PolicyTriggerRecord
    {
        /// <summary>Block time the PROXIMITY rule would first have fired. NaN if it never would have.</summary>
        public double TriggerTimeProximity;
        /// <summary>Block time the PREDICTIVE rule would first have fired. NaN if it never would have.</summary>
        public double TriggerTimePredictive;

        /// <summary>
        /// Clear limb→hazard surface distance at the first frame the opportunity was open (m), NaN if never
        /// sampled. This is the per-trial, measured version of the static geometry check in
        /// `docs/OPPORTUNITY_GEOMETRY_AUDIT.md` — below <see cref="OracleParams.MinApproachDistanceForValidTiming"/>
        /// the proximity cue fires at movement onset and the predictive cue cannot lead it.
        /// </summary>
        public float ApproachDistanceAtOnset;

        public float ClosingSpeedAtProximity;    // m/s at the proximity trigger, NaN if it never fired
        public float ClosingSpeedAtPredictive;   // m/s at the predictive trigger, NaN if it never fired

        /// <summary>
        /// THE MANIPULATION CHECK: proximity trigger minus predictive trigger. **Positive means the predictive
        /// policy genuinely led**, which is what H1 assumes. Negative means the timing factor ran BACKWARDS on
        /// this opportunity. NaN when either policy never triggered.
        /// </summary>
        public double PolicyLeadTime =>
            double.IsNaN(TriggerTimeProximity) || double.IsNaN(TriggerTimePredictive)
                ? double.NaN
                : TriggerTimeProximity - TriggerTimePredictive;

        /// <summary>True when the predictive policy would have fired LATER than the proximity policy.</summary>
        public bool TimingInverted => PolicyLeadTime < 0d;
    }

    /// <summary>
    /// Records, for every scheduled opportunity, when **both** warning policies would have fired — regardless
    /// of which condition is actually running [PAPER1_STUDY_DESIGN §6].
    ///
    /// WHY THIS EXISTS. The timing factor is only a manipulation while the predictive policy actually fires
    /// earlier than the proximity policy. It does not always: proximity fires at a fixed DISTANCE `D` and
    /// prediction at a fixed TIME `T`, so prediction leads only while closing speed exceeds `D`/`T` — and, more
    /// restrictively, only when the limb starts far enough away that the proximity cue is not already firing at
    /// movement onset. Without this probe, an opportunity whose manipulation ran backwards is indistinguishable
    /// in the data from one that worked, and a null H1 cannot be told apart from a broken manipulation.
    ///
    /// It runs in **every** condition, including `None`, because the counterfactual is what makes opportunities
    /// comparable across conditions. It is a pure observer: it never emits feedback and never touches the
    /// condition logic.
    ///
    /// TARGETED, NOT NEAREST. The probe evaluates the designated **target limb × target hazard** pair that §6
    /// specifies, via <see cref="CollisionOracle.ReadAgainst"/>. Note that the live
    /// <see cref="ConditionManager"/> still drives cues from the nearest/soonest obstacle across all obstacles,
    /// which §6 "Required corrections" asks to be changed; until it is, probe timings are the idealized
    /// targeted policy and may differ slightly from the cue the participant received. Compare
    /// <see cref="TriggerTimeProximity"/>/<see cref="TriggerTimePredictive"/> against the logged alert times to
    /// quantify that gap rather than assuming it is zero.
    /// </summary>
    public sealed class PolicyTriggerProbe
    {
        private readonly CollisionOracle _oracle;
        private readonly IReadOnlyList<Joint> _limbs;
        private readonly IReadOnlyList<Obstacle> _obstacles;
        private readonly OracleParams _p;
        private readonly Opportunity[] _schedule;
        private readonly PolicyTriggerRecord[] _records;

        public PolicyTriggerProbe(IReadOnlyList<Obstacle> obstacles, IReadOnlyList<Joint> limbs,
                                  IReadOnlyList<Opportunity> schedule, OracleParams oracleParams)
        {
            _obstacles = obstacles;
            _limbs = limbs;
            _p = oracleParams;
            // Its own oracle, so the probe is completely decoupled from whichever condition is running — the
            // velocity estimator is the same class with the same parameters, so the two never diverge.
            _oracle = new CollisionOracle(obstacles, oracleParams);

            _schedule = new Opportunity[schedule.Count];
            _records = new PolicyTriggerRecord[schedule.Count];
            for (int i = 0; i < schedule.Count; i++)
            {
                _schedule[i] = schedule[i];
                _records[i] = new PolicyTriggerRecord
                {
                    TriggerTimeProximity = double.NaN,
                    TriggerTimePredictive = double.NaN,
                    ApproachDistanceAtOnset = float.NaN,
                    ClosingSpeedAtProximity = float.NaN,
                    ClosingSpeedAtPredictive = float.NaN,
                };
            }
        }

        public IReadOnlyList<PolicyTriggerRecord> Records => _records;

        public void Tick(in PoseFrame frame)
        {
            _oracle.UpdateVelocities(frame, _limbs);
            double t = frame.Timestamp;

            for (int i = 0; i < _schedule.Length; i++)
            {
                Opportunity op = _schedule[i];
                if (t < op.OnsetTime || t > op.CloseTime) continue;
                if (!TryFindObstacle(op.TargetObstacleId, out Obstacle target)) continue;

                TargetReading r = _oracle.ReadAgainst(op.TargetLimb, target, frame);

                // First open frame: the approach distance this opportunity actually offered.
                if (float.IsNaN(_records[i].ApproachDistanceAtOnset))
                    _records[i].ApproachDistanceAtOnset = r.Distance;

                if (!r.Closing) continue;

                // Same rules as ConditionManager, evaluated against the designated pair.
                if (double.IsNaN(_records[i].TriggerTimeProximity) && r.Distance < _p.ReactiveDistance)
                {
                    _records[i].TriggerTimeProximity = t;
                    _records[i].ClosingSpeedAtProximity = r.ClosingSpeed;
                }

                // The predictive threshold forecasts ahead by the pipeline latency, exactly as the live rule
                // does, so the counterfactual stays comparable to the cue the participant actually received.
                float predictiveThreshold = _p.PredictiveTtc + _p.PipelineLatencySeconds;
                if (double.IsNaN(_records[i].TriggerTimePredictive) && r.Ttc < predictiveThreshold)
                {
                    _records[i].TriggerTimePredictive = t;
                    _records[i].ClosingSpeedAtPredictive = r.ClosingSpeed;
                }
            }
        }

        private bool TryFindObstacle(string id, out Obstacle found)
        {
            for (int i = 0; i < _obstacles.Count; i++)
            {
                if (_obstacles[i].Id != id) continue;
                found = _obstacles[i];
                return true;
            }
            found = default;
            return false;
        }
    }
}
