using System.Collections.Generic;
using UnityEngine;

namespace CollisionFeedback.Core.E2
{
    /// <summary>Tunables for one E2 session. Freeze before confirmatory collection [PAPER2 §8].</summary>
    public sealed class E2Params
    {
        /// <summary>
        /// Measured command-to-first-vibration latency (s). **Must come from the bench measurement**
        /// (§5: accelerometer on the tactor, ≥100 activations); the default of 0 is a placeholder that makes
        /// every delivered lead time wrong by the true latency.
        /// </summary>
        public float PipelineLatencySeconds = 0f;

        /// <summary>Radial closing speed above which the limb counts as moving toward the hazard (m/s).</summary>
        public float MovementOnsetSpeed = 0.15f;
        /// <summary>Consecutive frames above <see cref="MovementOnsetSpeed"/> required to confirm onset.</summary>
        public int OnsetConfirmFrames = 3;

        /// <summary>Limb radius applied to contact tests (m). Matches the Paper 1 convention.</summary>
        public float ContactRadius = 0.08f;
        /// <summary>Effective surface distance at or below which contact is recorded (m).</summary>
        public float ContactDistance = 0.03f;

        /// <summary>Distance to the visible target at which it counts as reached (m).</summary>
        public float TargetReachDistance = 0.08f;

        /// <summary>
        /// **The tempo manipulation** [PAPER2 §8: "Two target-response windows create normal and urgent
        /// reaches"]. Movement time is measured from confirmed movement onset to target contact, and the
        /// participant is asked to land inside the window for the trial's <see cref="Tempo"/>.
        ///
        /// Tempo is the independent variable behind **H3a**, one of only two confirmatory hypotheses. Without
        /// a window that actually differs, `tempo` in the log is noise and H3a estimates nothing — so these
        /// two numbers carry a confirmatory hypothesis between them.
        ///
        /// ⚠ **Both values are placeholders and must be set at pilot** (§15 gate 7). They have to satisfy two
        /// competing constraints, and only measured reaches can say whether they do:
        ///
        /// - **Far enough apart** that pre-cue closing speed genuinely differs — that is the manipulation
        ///   check, and §8 requires it to be confirmed with velocity and movement time.
        /// - **Both achievable**, or the urgent window becomes an instruction-failure generator rather than a
        ///   tempo manipulation, and the trials it spoils are not missing at random.
        ///
        /// A 0.70 m reach at a comfortable pace runs roughly 0.8–1.0 s, so these bracket that: `Normal` is
        /// unhurried, `Urgent` demands a genuinely fast reach without being impossible.
        /// </summary>
        public float NormalWindowSeconds = 1.20f;
        public float UrgentWindowSeconds = 0.70f;

        /// <summary>
        /// **Lower bounds, and Normal's is the one that matters.** An upper bound alone does not manipulate
        /// tempo: a participant who reaches at 0.6 s on every trial satisfies a "≤ 1.20 s" Normal window and a
        /// "≤ 0.70 s" Urgent window simultaneously, and the two conditions become behaviourally identical
        /// while every compliance check still reads green. H3a would then compare Urgent against Urgent.
        ///
        /// So Normal is a **band** — too fast is a miss, exactly as the instruction script says ("not slower,
        /// not faster"). Urgent keeps a lower bound of 0: faster than asked is never a problem there.
        ///
        /// ⚠ Pilot-set, like the upper bounds. `NormalWindowMinSeconds` must sit comfortably above the
        /// Urgent upper bound or the two bands touch and the separation is not enforced.
        /// </summary>
        public float NormalWindowMinSeconds = 0.85f;
        public float UrgentWindowMinSeconds = 0.0f;

        /// <summary>Longest movement time this trial's tempo accepts (s).</summary>
        public float WindowFor(Tempo tempo) =>
            tempo == Tempo.Urgent ? UrgentWindowSeconds : NormalWindowSeconds;

        /// <summary>Shortest movement time this trial's tempo accepts (s).</summary>
        public float WindowMinFor(Tempo tempo) =>
            tempo == Tempo.Urgent ? UrgentWindowMinSeconds : NormalWindowMinSeconds;

        /// <summary>
        /// Preregistered timing-tolerance band (s). When |realized - assigned| exceeds this, the trial is
        /// flagged as a **protocol deviation** and kept, per §8: "Trials outside the preregistered
        /// timing-tolerance band are not silently deleted. Mark them as protocol deviations and handle them
        /// using the preregistered intention-to-deliver and per-protocol sensitivity analyses."
        ///
        /// Deleting them would bias the curve, because they are not missing at random — they are
        /// concentrated on the reaches that were too fast or started too close for the assigned lead to be
        /// deliverable, which is exactly the tail that matters.
        ///
        /// **⚠ The default is a placeholder and will flag almost everything.** Delivery is systematically
        /// short by ~15–20% at longer leads (see <see cref="E2SessionPlan"/>), so a naive ±50 ms band around
        /// the *assigned* lead marks nearly every trial as a deviation. Measure the bias at pilot and set the
        /// band around *expected* delivery before it means anything.
        /// </summary>
        public float TimingToleranceSeconds = 0.05f;

        /// <summary>Fraction of peak closing speed below which the limb counts as arrested.</summary>
        public float ArrestSpeedFraction = 0.15f;
        /// <summary>Consecutive decelerating frames required to call a response onset.</summary>
        public int ResponseConfirmFrames = 3;

        public OracleParams Oracle = new OracleParams();
    }

    /// <summary>
    /// Drives ONE E2 trial [PAPER2_STUDY_DESIGN §8]: watches the reaching limb, fires the warning so that the
    /// **first physical vibration** lands at the assigned lead time before predicted contact, and records what
    /// was actually delivered.
    ///
    /// ── THE CENTRAL IDEA ───────────────────────────────────────────────────────────────────────────────
    /// You cannot deliver a lead time precisely. Frames quantise at ~11 ms, the haptic pipeline adds tens of
    /// milliseconds with jitter, and the constant-velocity contact prediction is biased during a decelerating
    /// reach. **So do not try.** Aim, then measure what landed, and fit the psychometric curve on the measured
    /// value. Scatter in delivered lead times is harmless — it fills the x-axis more evenly than hitting five
    /// exact levels would.
    ///
    /// Firing rule: the cue command goes out when predicted TTC drops to `assignedLead + pipelineLatency`, so
    /// the vibration *arrives* one lead time before predicted contact.
    ///
    /// Freezing rule: at physical onset the pre-cue radial speed is frozen and the contact prediction is made
    /// from it. It is **never** revised using post-warning movement — otherwise a participant who successfully
    /// stops would appear to have been warned infinitely early (§8).
    ///
    /// Hardware-free and deterministic: driven entirely by <see cref="PoseFrame.Timestamp"/>, so it runs and is
    /// tested with no headset, no trackers and no bHaptics.
    /// </summary>
    public sealed class E2TrialRunner
    {
        private readonly E2Trial _trial;
        private readonly E2Params _p;
        private readonly Obstacle _hazard;
        private readonly bool _hazardFound;
        private readonly CollisionOracle _oracle;
        private readonly IFeedbackSink _sink;
        private readonly List<Joint> _limbs;

        private E2TrialOutcome _o;
        private bool _finished;

        private int _onsetRun;
        private bool _onsetConfirmed;
        private bool _commandSent;
        private bool _physicalOnsetPassed;
        private float _frozenPreCueSpeed = float.NaN;

        private double _firstTime = double.NaN;
        private double _lastTime = double.NaN;
        private double _frameInterval = double.NaN;
        private float _peakSpeed;
        private float _distanceAtPhysicalOnset = float.NaN;
        private int _decelRun;

        public E2TrialRunner(E2Trial trial, IReadOnlyList<Obstacle> obstacles, E2Params p, IFeedbackSink sink)
        {
            _trial = trial;
            _p = p;
            _sink = sink;
            _limbs = new List<Joint> { trial.TargetLimb };
            _oracle = new CollisionOracle(obstacles, p.Oracle);

            _hazardFound = false;
            for (int i = 0; i < obstacles.Count; i++)
            {
                if (obstacles[i].Id != trial.HazardId) continue;
                _hazard = obstacles[i];
                _hazardFound = true;
                break;
            }

            _o = new E2TrialOutcome
            {
                TrialId = trial.Id,
                TargetLimb = trial.TargetLimb,
                HazardId = trial.HazardId,
                Tempo = trial.Tempo,
                IsWarningTrial = trial.IsWarningTrial,
                Valid = false,
                InvalidReason = "no_movement_onset",
                Crossed = 0,
                AssignedLeadSeconds = trial.AssignedLeadSeconds,
                CommandTime = double.NaN,
                PhysicalOnsetTime = double.NaN,
                RealizedLeadSeconds = double.NaN,
                PreCueSpeed = float.NaN,
                DistanceAtCue = float.NaN,
                PredictedContactTime = double.NaN,
                ActualContactTime = double.NaN,
                MinClearance = float.PositiveInfinity,
                MaxPenetration = 0f,
                PostCueTravel = 0f,
                ResponseOnsetTime = double.NaN,
                ArrestTime = double.NaN,
                ReachedTarget = false,
                MovementOnsetTime = double.NaN,
                TargetContactTime = double.NaN,
                ResponseWindowSeconds = p.WindowFor(trial.Tempo),
                ResponseWindowMinSeconds = p.WindowMinFor(trial.Tempo),
            };

            if (!_hazardFound) { _o.InvalidReason = "hazard_id_not_in_scene"; }
        }

        public E2TrialOutcome Outcome => _o;

        public void Tick(in PoseFrame frame)
        {
            if (_finished || !_hazardFound) return;

            double t = frame.Timestamp;
            if (double.IsNaN(_firstTime)) _firstTime = t;
            // Real inter-frame interval, for back-dating a confirmed detector run. Replaces a guess of
            // (elapsed / 100), which was wrong by whatever the true frame rate was not.
            if (!double.IsNaN(_lastTime) && t > _lastTime) _frameInterval = t - _lastTime;
            _lastTime = t;

            _oracle.UpdateVelocities(frame, _limbs);
            TargetReading r = _oracle.ReadAgainst(_trial.TargetLimb, _hazard, frame);
            Vector3 p = frame.Get(_trial.TargetLimb);

            // ── Contact bookkeeping, identical in shape to Paper 1's so the two never disagree ──────────
            float effective = Mathf.Max(0f, r.Distance - _p.ContactRadius);
            if (effective < _o.MinClearance) _o.MinClearance = effective;
            float depth = PenetrationDepth(_hazard, p);
            if (depth > _o.MaxPenetration) _o.MaxPenetration = depth;

            if (effective <= _p.ContactDistance && _o.Crossed == 0)
            {
                _o.Crossed = 1;
                _o.ActualContactTime = t;       // the ground truth the counterfactual is validated against
            }

            if (Vector3.Distance(p, _trial.TargetPosition) <= _p.TargetReachDistance)
            {
                if (!_o.ReachedTarget) _o.TargetContactTime = t;   // first contact only; movement time needs it
                _o.ReachedTarget = true;
            }

            // ── Movement onset: the cue is eligible only after the reach has genuinely begun (§8) ───────
            if (!_onsetConfirmed)
            {
                _onsetRun = r.ClosingSpeed > _p.MovementOnsetSpeed ? _onsetRun + 1 : 0;
                if (_onsetRun >= _p.OnsetConfirmFrames)
                {
                    _onsetConfirmed = true;
                    // Back-date to the first frame of the confirmed run — that is when the reach actually
                    // began, not when the confirmation threshold happened to be met.
                    _o.MovementOnsetTime = t - (_p.OnsetConfirmFrames - 1) * FrameInterval();
                    _o.Valid = true;
                    _o.InvalidReason = "";
                    if (_trial.IsWarningTrial) _o.InvalidReason = "cue_never_eligible";
                }
            }

            if (r.ClosingSpeed > _peakSpeed) _peakSpeed = r.ClosingSpeed;

            // ── Fire so the VIBRATION lands at the assigned lead, not the command ───────────────────────
            if (_trial.IsWarningTrial && _onsetConfirmed && !_commandSent && r.Closing)
            {
                float fireAtTtc = _trial.AssignedLeadSeconds + _p.PipelineLatencySeconds;
                if (r.Ttc <= fireAtTtc)
                {
                    _commandSent = true;
                    _o.CommandTime = t;
                    _o.InvalidReason = "";
                    _frozenPreCueSpeed = r.ClosingSpeed;   // frozen here; never revised after the response
                    _sink.Fire(new FeedbackCommand(SiteRouting.For(_trial.TargetLimb), Modality.Haptic,
                                                   TriggerKind.Predictive, _trial.TargetLimb, _trial.HazardId,
                                                   t, r.Distance, r.Ttc));
                }
            }

            // ── At physical onset, freeze the counterfactual and compute the REALIZED lead ─────────────
            if (_commandSent && !_physicalOnsetPassed && t >= _o.CommandTime + _p.PipelineLatencySeconds)
            {
                _physicalOnsetPassed = true;
                _o.PhysicalOnsetTime = t;
                _o.PreCueSpeed = _frozenPreCueSpeed;
                _o.DistanceAtCue = r.Distance;
                _distanceAtPhysicalOnset = r.Distance;

                // Predicted contact uses the position NOW (still pre-response, since the vibration has only
                // just begun) and the speed frozen at command time. Realized lead is what remains.
                if (_frozenPreCueSpeed > _p.Oracle.MinClosingSpeed)
                {
                    double remaining = r.Distance / _frozenPreCueSpeed;
                    _o.PredictedContactTime = t + remaining;
                    _o.RealizedLeadSeconds = remaining;
                }
            }

            // ── Post-cue kinematics ────────────────────────────────────────────────────────────────────
            if (_physicalOnsetPassed)
            {
                float travelled = _distanceAtPhysicalOnset - r.Distance;
                if (travelled > _o.PostCueTravel) _o.PostCueTravel = travelled;

                if (double.IsNaN(_o.ResponseOnsetTime))
                {
                    bool decelerating = r.ClosingSpeed < _peakSpeed * (1f - _p.ArrestSpeedFraction);
                    _decelRun = decelerating ? _decelRun + 1 : 0;
                    if (_decelRun >= _p.ResponseConfirmFrames)
                        _o.ResponseOnsetTime = t - (_p.ResponseConfirmFrames - 1) * FrameInterval();
                }

                if (double.IsNaN(_o.ArrestTime) && r.ClosingSpeed <= _peakSpeed * _p.ArrestSpeedFraction)
                    _o.ArrestTime = t - _o.PhysicalOnsetTime;
            }
        }

        /// <summary>Closes the trial. Idempotent.</summary>
        public E2TrialOutcome Finish()
        {
            if (_finished) return _o;
            _finished = true;

            if (!_hazardFound) { _o.Valid = false; _o.InvalidReason = "hazard_id_not_in_scene"; }
            else if (!_onsetConfirmed) { _o.Valid = false; _o.InvalidReason = "no_movement_onset"; }
            else if (_trial.IsWarningTrial && !_commandSent)
            {
                // The cue genuinely never fired — the reach never came close enough for the TTC threshold to
                // be crossed at all. There is no lead time to analyse, so the trial leaves the denominator.
                _o.Valid = false;
                _o.InvalidReason = "cue_never_eligible";
            }

            // A cue that fired but landed outside the tolerance band stays VALID and is flagged. §8 is
            // explicit that these are protocol deviations, not deletions — and since the psychometric fit
            // uses the REALIZED lead, a mistimed trial still contributes a legitimate (x, y) point. It is
            // the intention-to-deliver analysis that needs the flag, not the curve.
            if (!double.IsNaN(_o.RealizedLeadSeconds) && !float.IsNaN(_o.AssignedLeadSeconds))
            {
                double miss = System.Math.Abs(_o.RealizedLeadSeconds - _o.AssignedLeadSeconds);
                _o.TimingDeviation = miss > _p.TimingToleranceSeconds;
            }

            if (float.IsPositiveInfinity(_o.MinClearance)) _o.MinClearance = float.NaN;
            return _o;
        }

        // Depth from the point to the nearest face of the box; 0 when outside. Mirrors BlockRunner.
        private static float PenetrationDepth(in Obstacle o, Vector3 p)
        {
            float dx = o.HalfExtents.x - Mathf.Abs(p.x - o.Center.x);
            float dy = o.HalfExtents.y - Mathf.Abs(p.y - o.Center.y);
            float dz = o.HalfExtents.z - Mathf.Abs(p.z - o.Center.z);
            if (dx <= 0f || dy <= 0f || dz <= 0f) return 0f;
            return Mathf.Min(dx, Mathf.Min(dy, dz));
        }

        /// <summary>
        /// The most recent inter-frame interval, for back-dating a confirmed detector run to the frame where
        /// it started. Falls back to 90 Hz only before a second frame has arrived.
        ///
        /// This replaced `(t - firstTime) / 100.0`, which back-dated by a *guess* that grew with elapsed trial
        /// time — so the same 3-frame run was back-dated by 0.3 ms early in a trial and 30 ms late in one.
        /// The detector still requires blinded validation against hand-labelled pilot trials before
        /// confirmatory use (§8, §15 gate 8); this only removes a known error it was carrying.
        /// </summary>
        private double FrameInterval() =>
            double.IsNaN(_frameInterval) || _frameInterval <= 0 ? 1.0 / 90.0 : _frameInterval;
    }
}
