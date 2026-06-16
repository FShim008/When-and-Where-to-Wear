using System.Collections.Generic;

namespace CollisionFeedback.Core
{
    /// <summary>
    /// Applies one condition's fire rule + routing each frame and emits <see cref="FeedbackCommand"/>s.
    /// Edge-triggered with hysteresis: at most one alert per approach (re-arms only once the limb clears),
    /// so alert COUNTS stay meaningful as a covariate / manipulation check rather than firing every frame.
    /// Multi-limb arbitration: when several limbs are at risk in the same frame, only the single most-urgent
    /// one is cued (lowest TTC for predictive, nearest for reactive) [Protocol 2.2 / storyboard E12],
    /// throttled by a global re-fire debounce so a compound event yields one cue, not a burst.
    /// </summary>
    public sealed class ConditionManager
    {
        private readonly Condition _condition;
        private readonly OracleParams _p;
        private readonly IReadOnlyList<Joint> _limbs;
        private readonly IFeedbackSink _sink;
        private readonly CollisionOracle _oracle;
        private readonly Dictionary<Joint, bool> _armed = new();
        private double _nextFireAllowedTime = double.NegativeInfinity;   // global re-fire debounce gate [Protocol 2.1]

        public Condition Condition => _condition;

        public ConditionManager(Condition condition, OracleParams p, IReadOnlyList<Obstacle> obstacles,
                                IReadOnlyList<Joint> limbs, IFeedbackSink sink)
        {
            _condition = condition;
            _p = p;
            _limbs = limbs;
            _sink = sink;
            _oracle = new CollisionOracle(obstacles, p);
            for (int i = 0; i < limbs.Count; i++) _armed[limbs[i]] = true;
        }

        public void Tick(in PoseFrame frame)
        {
            _oracle.UpdateVelocities(frame, _limbs);

            // Pass 1: evaluate every limb, keep the edge-trigger arming current, and pick the single
            // most-urgent armed limb that wants to fire this frame (multi-limb arbitration).
            bool haveBest = false;
            float bestUrgency = float.PositiveInfinity;   // lower = more urgent (TTC predictive / distance reactive)
            Joint bestLimb = default;
            HapticSite bestSite = HapticSite.Chest;
            TriggerKind bestTrigger = TriggerKind.Reactive;
            string bestObstacleId = null;
            float bestDistance = 0f;
            float bestTtc = 0f;

            for (int i = 0; i < _limbs.Count; i++)
            {
                Joint limb = _limbs[i];
                RiskReading r = _oracle.Read(limb, frame);

                bool fire = false;
                bool clear = true;   // re-arm gate; None has nothing to fire, so it stays armed
                TriggerKind trigger = TriggerKind.Reactive;

                // The alert describes the obstacle that DROVE it, plus that obstacle's own distance + TTC
                // (reactive -> nearest; predictive -> soonest), so the logged triple is self-consistent.
                string obstacleId = r.NearestObstacleId;
                float distance = r.MinDistance;
                float ttc = r.NearestTtc;
                float urgency = float.PositiveInfinity;   // the arbitration key

                switch (_condition)
                {
                    case Condition.None:
                        break;

                    case Condition.RG:
                    case Condition.RB:
                    case Condition.Visual:
                        trigger = TriggerKind.Reactive;
                        obstacleId = r.NearestObstacleId;
                        distance = r.MinDistance;
                        ttc = r.NearestTtc;
                        fire = r.Closing && r.MinDistance < _p.ReactiveDistance;
                        // Re-arm only when the limb genuinely disengages: stops closing, or backs off
                        // past D + margin. During a steady approach distance only shrinks, so this stays
                        // false after the first alert => exactly one alert per approach.
                        clear = !r.Closing || r.MinDistance > _p.ReactiveDistance + _p.ReactiveReleaseMargin;
                        urgency = r.MinDistance;   // reactive arbitration: cue the NEAREST limb
                        break;

                    case Condition.PG:
                    case Condition.PB:
                        trigger = TriggerKind.Predictive;
                        obstacleId = r.SoonestObstacleId;
                        distance = r.SoonestDistance;
                        ttc = r.MinTtc;
                        // Effective threshold forecasts ahead by the pipeline latency, so the cue ARRIVES
                        // ~T before contact despite the motion->tactor delay [3D.3].
                        float predictiveThreshold = _p.PredictiveTtc + _p.PipelineLatencySeconds;
                        fire = r.Closing && r.MinTtc < predictiveThreshold;
                        // The release MUST use the same metric as the trigger. A predictive alert fires
                        // while still FAR away, so a distance-based release would re-arm immediately and
                        // re-fire every frame. Hysteresis is on the TTC axis instead.
                        clear = !r.Closing || r.MinTtc > predictiveThreshold + _p.PredictiveReleaseMargin;
                        urgency = r.MinTtc;        // predictive arbitration: cue the lowest-TTC limb
                        break;
                }

                // Edge-trigger hysteresis: re-arm a previously-fired limb the moment it disengages.
                // (clear and fire are mutually exclusive, so this never re-arms a limb about to fire.)
                if (!_armed[limb] && clear) _armed[limb] = true;

                if (fire && _armed[limb] && urgency < bestUrgency)
                {
                    haveBest = true;
                    bestUrgency = urgency;
                    bestLimb = limb;
                    bestSite = IsLocalized(_condition) ? SiteRouting.For(limb) : HapticSite.Chest;
                    bestTrigger = trigger;
                    bestObstacleId = obstacleId;
                    bestDistance = distance;
                    bestTtc = ttc;
                }
            }

            // Pass 2: fire only the winner, throttled to at most one cue per debounce window [Protocol 2.1].
            // Losing at-risk limbs stay armed but are suppressed by the debounce, so the uncued risk in a
            // compound event (E12) is deliberately left uncued — exactly what the arbitration measures.
            if (haveBest && frame.Timestamp >= _nextFireAllowedTime)
            {
                Modality modality = _condition == Condition.Visual ? Modality.Visual : Modality.Haptic;
                _sink.Fire(new FeedbackCommand(bestSite, modality, bestTrigger, bestLimb, bestObstacleId,
                                               frame.Timestamp, bestDistance, bestTtc));
                _armed[bestLimb] = false;
                _nextFireAllowedTime = frame.Timestamp + _p.RefireDebounceSeconds;
            }
        }

        /// <summary>Body-localized and the (localized) Visual reference cue WHERE; Generic cues the chest.</summary>
        private static bool IsLocalized(Condition c) =>
            c == Condition.RB || c == Condition.PB || c == Condition.Visual;
    }

    /// <summary>Maps an at-risk limb to the tactor site on that limb (Body-localized routing).</summary>
    public static class SiteRouting
    {
        public static HapticSite For(Joint limb) => limb switch
        {
            Joint.LeftHand => HapticSite.LeftHand,
            Joint.RightHand => HapticSite.RightHand,
            Joint.LeftFoot => HapticSite.LeftShin,
            Joint.RightFoot => HapticSite.RightShin,
            _ => HapticSite.Chest,
        };
    }
}
