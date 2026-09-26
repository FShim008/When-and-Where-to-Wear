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
        private readonly IReadOnlyList<Obstacle> _obstacles;
        private readonly Opportunity[] _schedule;        // null = hazard-agnostic (bench/tests only)
        private readonly Dictionary<Joint, bool> _armed = new();
        private double _nextFireAllowedTime = double.NegativeInfinity;   // global re-fire debounce gate [Protocol 2.1]

        public Condition Condition => _condition;

        /// <param name="schedule">
        /// The block's scheduled opportunities. When supplied, cues fire ONLY for the designated
        /// target limb × target hazard pair whose window is open [PAPER1_STUDY_DESIGN §6]. Pass null for the
        /// hazard-agnostic behaviour used by bench tools and unit tests — that path is still self-consistent
        /// per obstacle, it simply considers every hazard rather than the designated one.
        /// </param>
        public ConditionManager(Condition condition, OracleParams p, IReadOnlyList<Obstacle> obstacles,
                                IReadOnlyList<Joint> limbs, IFeedbackSink sink,
                                IReadOnlyList<Opportunity> schedule = null)
        {
            _condition = condition;
            _p = p;
            _limbs = limbs;
            _sink = sink;
            _obstacles = obstacles;
            _oracle = new CollisionOracle(obstacles, p);
            if (schedule != null && schedule.Count > 0)
            {
                _schedule = new Opportunity[schedule.Count];
                for (int i = 0; i < schedule.Count; i++) _schedule[i] = schedule[i];
            }
            for (int i = 0; i < limbs.Count; i++) _armed[limbs[i]] = true;
        }

        /// <summary>
        /// The designated hazard for <paramref name="limb"/> at time <paramref name="t"/>, if an opportunity
        /// for that limb is open. Stateless — driven purely by the frame timestamp — so it cannot drift out of
        /// step with <see cref="OpportunityScheduler"/> or <see cref="PolicyTriggerProbe"/>.
        /// </summary>
        private bool TryGetDesignated(Joint limb, double t, out Obstacle target)
        {
            target = default;
            if (_schedule == null) return false;
            for (int i = 0; i < _schedule.Length; i++)
            {
                Opportunity op = _schedule[i];
                if (op.TargetLimb != limb || t < op.OnsetTime || t > op.CloseTime) continue;
                for (int k = 0; k < _obstacles.Count; k++)
                {
                    if (_obstacles[k].Id != op.TargetObstacleId) continue;
                    target = _obstacles[k];
                    return true;
                }
                return false;   // designated hazard id not in the scene: fire nothing rather than something wrong
            }
            return false;
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

                // DESIGNATED PAIR [PAPER1_STUDY_DESIGN §6]. When a schedule is supplied, a cue may only ever
                // describe the target limb × target hazard of an OPEN opportunity. Warning about whatever
                // happens to be nearest is not merely unrealistic here: the global re-fire debounce means one
                // alert about an undesignated hazard can SWALLOW the designated warning, silently failing to
                // deliver the manipulation on that opportunity. Hazards are invisible (§5), so a suppressed
                // cue for an undesignated hazard is undetectable to the participant.
                bool haveDesignated = TryGetDesignated(limb, frame.Timestamp, out Obstacle designated);
                if (_schedule != null && !haveDesignated)
                {
                    _armed[limb] = true;   // nothing designated is open: disengaged, so re-arm for next time
                    continue;
                }

                // Candidates, each a SELF-CONSISTENT (obstacle, distance, ttc, closing-on-that-obstacle)
                // tuple. Previously the reactive branch combined "closing toward ANY obstacle" with the
                // distance to the NEAREST one, so a limb closing on a distant hazard could fire an alert
                // about a different hazard it merely happened to be near.
                string reactiveId, predictiveId;
                float reactiveDistance, reactiveTtc, predictiveDistance, predictiveTtc;
                bool reactiveClosing, predictiveClosing;

                if (haveDesignated)
                {
                    TargetReading d = _oracle.ReadAgainst(limb, designated, frame);
                    reactiveId = predictiveId = designated.Id;
                    reactiveDistance = predictiveDistance = d.Distance;
                    reactiveTtc = predictiveTtc = d.Ttc;
                    reactiveClosing = predictiveClosing = d.Closing;
                }
                else
                {
                    RiskReading r = _oracle.Read(limb, frame);
                    reactiveId = r.NearestObstacleId;
                    reactiveDistance = r.MinDistance;
                    reactiveTtc = r.NearestTtc;
                    // NearestTtc is finite only while the limb is closing on THAT obstacle.
                    reactiveClosing = !float.IsPositiveInfinity(r.NearestTtc);
                    predictiveId = r.SoonestObstacleId;
                    predictiveDistance = r.SoonestDistance;
                    predictiveTtc = r.MinTtc;
                    predictiveClosing = r.SoonestObstacleId != null;
                }

                bool fire = false;
                bool clear = true;   // re-arm gate; None has nothing to fire, so it stays armed
                TriggerKind trigger = TriggerKind.Reactive;

                // The alert describes the obstacle that DROVE it, plus that obstacle's own distance + TTC.
                string obstacleId = reactiveId;
                float distance = reactiveDistance;
                float ttc = reactiveTtc;
                float urgency = float.PositiveInfinity;   // the arbitration key

                switch (_condition)
                {
                    case Condition.None:
                        break;

                    case Condition.RG:
                    case Condition.RB:
                        trigger = TriggerKind.Reactive;
                        obstacleId = reactiveId;
                        distance = reactiveDistance;
                        ttc = reactiveTtc;
                        fire = reactiveClosing && reactiveDistance < _p.ReactiveDistance;
                        // Re-arm only when the limb genuinely disengages: stops closing, or backs off
                        // past D + margin. During a steady approach distance only shrinks, so this stays
                        // false after the first alert => exactly one alert per approach.
                        clear = !reactiveClosing || reactiveDistance > _p.ReactiveDistance + _p.ReactiveReleaseMargin;
                        urgency = reactiveDistance;   // reactive arbitration: cue the NEAREST limb
                        break;

                    // Every predictive condition shares ONE trigger rule, so each comparison isolates exactly
                    // one variable [PAPER1_STUDY_DESIGN §2, §3]:
                    //   PB  vs PG   -> localization  (H2)
                    //   PB  vs RB   -> trigger policy, cue form constant  (H1)
                    //   PB  vs PBC  -> CUE FORM, trigger constant  (H4')
                    //   PB  vs Visual -> modality (H4, implemented but NOT scheduled for Paper 1)
                    // PBC diverges only AFTER this point, in Pass 2, where it emits continuously instead of
                    // once. Grouping it here is what guarantees its onset matches PB's exactly.
                    case Condition.PG:
                    case Condition.PB:
                    case Condition.PBC:
                    case Condition.Visual:
                        trigger = TriggerKind.Predictive;
                        obstacleId = predictiveId;
                        distance = predictiveDistance;
                        ttc = predictiveTtc;
                        // Effective threshold forecasts ahead by the pipeline latency, so the cue ARRIVES
                        // ~T before contact despite the motion->tactor delay [3D.3].
                        float predictiveThreshold = _p.PredictiveTtc + _p.PipelineLatencySeconds;
                        fire = predictiveClosing && predictiveTtc < predictiveThreshold;
                        // The release MUST use the same metric as the trigger. A predictive alert fires
                        // while still FAR away, so a distance-based release would re-arm immediately and
                        // re-fire every frame. Hysteresis is on the TTC axis instead.
                        clear = !predictiveClosing || predictiveTtc > predictiveThreshold + _p.PredictiveReleaseMargin;
                        urgency = predictiveTtc;   // predictive arbitration: cue the lowest-TTC limb
                        break;
                }

                // Edge-trigger hysteresis: re-arm a previously-fired limb the moment it disengages.
                // (clear and fire are mutually exclusive, so this never re-arms a limb about to fire.)
                if (!_armed[limb] && clear) _armed[limb] = true;

                // A continuous cue has no edge to trigger on: it is engaged for as long as the approach
                // lasts, which is the manipulation. Gating it on _armed would emit one frame and then fall
                // silent, turning PBC into PB and making H4' a comparison of a condition with itself.
                if (fire && (IsContinuous(_condition) || _armed[limb]) && urgency < bestUrgency)
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
            if (IsContinuous(_condition))
            {
                // PBC [H4']: emit EVERY frame while engaged, with intensity tracking current TTC. No edge
                // trigger and no debounce - both exist to make discrete alert COUNTS meaningful, and a
                // continuous cue is a level, not a count. Alert burden for PBC is measured as engagement
                // episodes and dose, not as a frame tally (see BlockRunner).
                //
                // Because TTC = distance / closing speed, slowing down raises TTC and lowers intensity.
                // That feedback loop is the behaviour Valkov and Linsen identified as the cause of their
                // result, and reproducing it faithfully is the entire point of this condition.
                if (haveBest)
                {
                    float level = ContinuousCueMapping.Intensity(bestTtc, ContinuousParams());
                    if (level > 0f)
                        _sink.Fire(new FeedbackCommand(bestSite, Modality.Haptic, bestTrigger, bestLimb,
                                                       bestObstacleId, frame.Timestamp, bestDistance, bestTtc,
                                                       CueForm.Continuous, level));
                }
            }
            else if (haveBest && frame.Timestamp >= _nextFireAllowedTime)
            {
                Modality modality = _condition == Condition.Visual ? Modality.Visual : Modality.Haptic;
                _sink.Fire(new FeedbackCommand(bestSite, modality, bestTrigger, bestLimb, bestObstacleId,
                                               frame.Timestamp, bestDistance, bestTtc));
                _armed[bestLimb] = false;
                _nextFireAllowedTime = frame.Timestamp + _p.RefireDebounceSeconds;
            }
        }

        /// <summary>
        /// The continuous-cue band, built from the SAME effective threshold the predictive trigger uses.
        /// Deriving it here rather than storing it separately is what guarantees PB and PBC share an onset:
        /// the cue becomes perceptible at exactly the TTC where PB would have fired. If these two could
        /// drift apart, H4' would confound cue form with onset time.
        /// </summary>
        private ContinuousCueParams ContinuousParams() =>
            new(_p.PredictiveTtc + _p.PipelineLatencySeconds, 0f,
                _p.ContinuousCueGamma, _p.ContinuousCueMinIntensity);

        /// <summary>Body-localized and the (localized) Visual reference cue WHERE; Generic cues the chest.</summary>
        /// <summary>
        /// Which trigger rule a condition uses, or null for <see cref="Condition.None"/> which never fires.
        /// Kept beside the switch in <see cref="Tick"/> that defines it so the two cannot drift — logging and
        /// analysis need this mapping without re-deriving it. Note `Visual` is PREDICTIVE: it shares PB's
        /// trigger so H4 isolates modality [PAPER1_STUDY_DESIGN §2].
        /// </summary>
        public static TriggerKind? TriggerFor(Condition c) => c switch
        {
            Condition.RG or Condition.RB => TriggerKind.Reactive,
            Condition.PG or Condition.PB or Condition.PBC or Condition.Visual => TriggerKind.Predictive,
            _ => null,
        };

        /// <summary>
        /// Whether a condition delivers a continuous, intensity-modulated cue rather than a discrete
        /// edge-triggered one. This is the H4' manipulation [docs/PAPER1_PARAMETER_JUSTIFICATION.md §5B].
        /// Exactly one condition is continuous; if a second is ever added, every "PB vs PBC" contrast in
        /// the analysis must be revisited, because H4' assumes a single pair.
        /// </summary>
        public static bool IsContinuous(Condition c) => c == Condition.PBC;

        /// <summary>The cue form a condition delivers — the logged counterpart of <see cref="IsContinuous"/>.</summary>
        public static CueForm FormFor(Condition c) =>
            IsContinuous(c) ? CueForm.Continuous : CueForm.Discrete;

        /// <summary>
        /// Whether a condition cues the at-risk limb's own site (localized) or the chest (generic).
        ///
        /// Public for the same reason as <see cref="TriggerFor"/>: logging and analysis need the mapping
        /// without re-deriving it, and it is a design fact worth asserting in tests. `Visual` is localized
        /// because H4 requires it to be **information-matched to PB** — an undifferentiated visual cue
        /// would confound modality with information content [PAPER1_STUDY_DESIGN §2].
        /// </summary>
        public static bool IsLocalized(Condition c) =>
            c == Condition.RB || c == Condition.PB || c == Condition.PBC || c == Condition.Visual;
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
