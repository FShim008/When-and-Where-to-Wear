using System.Collections.Generic;
using UnityEngine;

namespace CollisionFeedback.Core
{
    /// <summary>
    /// Orchestrates one block: ticks the opportunity scheduler, the condition (feedback), and the
    /// collision detector in lockstep, attributing each detected collision to the opportunity open for
    /// that limb. Produces the analysis-ready <see cref="BlockResult"/>.
    ///
    /// PRIMARY OUTCOME [PAPER1_STUDY_DESIGN §5]: alongside the block summary it now builds one
    /// <see cref="OpportunityOutcome"/> per scheduled opportunity. A violation is recorded only when the
    /// **designated target limb** enters the **designated target hazard** inside that opportunity's window,
    /// using the same contact rule as <see cref="CollisionDetector"/> so the two can never disagree. Contacts
    /// during the window by another limb, or with another hazard, are kept separately as unattributed safety
    /// events rather than inflating the primary numerator.
    ///
    /// Hardware-free and deterministic. The Runtime must pass BLOCK-RELATIVE frame timestamps (reset to
    /// ~0 at the start of each block) so scheduler time and data time share one clock.
    /// </summary>
    public sealed class BlockRunner
    {
        private readonly BlockContext _ctx;
        private readonly ConditionManager _conditionManager;
        private readonly CollisionDetector _detector;
        private readonly OpportunityScheduler _scheduler;
        private readonly CountingSink _sink;

        private readonly AvoidanceLatencyDetector _latency = new();
        private readonly bool _genericCue;
        private int _processedAlerts;

        private int _processedEvents;
        private readonly HashSet<string> _hitOpportunityIds = new();
        private int _collisionsAttributed;
        private int _collisionsUnattributed;
        private float _minClearance = float.PositiveInfinity;
        private double _firstTime = double.NaN;
        private double _lastTime;
        private bool _finished;

        // ── Per-opportunity primary outcome ────────────────────────────────────────────────────
        private readonly IReadOnlyList<Obstacle> _obstacles;
        private readonly DetectorParams _detectorParams;
        private readonly Opportunity[] _schedule;
        private readonly OpportunityOutcome[] _outcomes;
        private readonly bool[] _insideNow;          // edge state, for counting separate entry episodes
        private bool _aborted;

        // --- Continuous-cue bookkeeping (Condition.PBC only) [H4'] ---
        // A continuous cue emits EVERY frame while engaged. For alert burden and avoidance latency the
        // meaningful unit is an ENGAGEMENT EPISODE - a rising edge out of silence - not a frame. Counting
        // frames would report ~1000 "alerts" per block against ~12 for the discrete conditions, and would
        // re-arm the latency detector on every frame so PBC's avoidance latency came out near zero by
        // construction rather than by measurement.
        private bool _continuousEngaged;
        private Joint _continuousLimb;
        private int _alertEpisodes;
        private double _cueDoseSeconds;
        private double _prevTickTime = double.NaN;

        /// <summary>
        /// Engagement onsets, not raw commands. For the discrete conditions this equals
        /// <see cref="Alerts"/>.Count; for <see cref="Condition.PBC"/> it is the number of times the cue
        /// rose out of silence, which is the quantity comparable across conditions.
        /// </summary>
        public int AlertEpisodes => _alertEpisodes;

        /// <summary>
        /// Integrated intensity-seconds delivered over the block — the continuous analogue of an alert
        /// count, and the only burden measure on which PB and PBC can be compared honestly. Zero-length
        /// for discrete cues, whose burden is already captured by <see cref="AlertEpisodes"/>.
        /// </summary>
        public double CueDoseSeconds => _cueDoseSeconds;

        // Counterfactual timing check: what BOTH policies would have done, in every condition [§6].
        private readonly PolicyTriggerProbe _policyProbe;

        public BlockRunner(BlockContext ctx, IReadOnlyList<Obstacle> obstacles, IReadOnlyList<Joint> limbs,
                           IReadOnlyList<Opportunity> schedule, OracleParams oracleParams,
                           DetectorParams detectorParams, IFeedbackSink deviceSink = null)
        {
            _ctx = ctx;
            _sink = new CountingSink(deviceSink);
            _conditionManager = new ConditionManager(ctx.Condition, oracleParams, obstacles, limbs, _sink,
                                                     schedule);   // designated pair only [§6]
            _detector = new CollisionDetector(obstacles, limbs, detectorParams);
            _scheduler = new OpportunityScheduler(schedule);
            _genericCue = ctx.Condition == Condition.RG || ctx.Condition == Condition.PG;

            _obstacles = obstacles;
            _detectorParams = detectorParams;
            // Runs in EVERY condition, including None: the counterfactual is what makes the timing
            // manipulation measurable rather than assumed [§6].
            _policyProbe = new PolicyTriggerProbe(obstacles, limbs, schedule, oracleParams);

            _schedule = new Opportunity[schedule.Count];
            for (int i = 0; i < schedule.Count; i++) _schedule[i] = schedule[i];

            _outcomes = new OpportunityOutcome[_schedule.Length];
            _insideNow = new bool[_schedule.Length];
            for (int i = 0; i < _schedule.Length; i++)
            {
                Opportunity op = _schedule[i];
                _outcomes[i] = new OpportunityOutcome
                {
                    OpportunityId = op.Id,
                    TargetLimb = op.TargetLimb,
                    TargetObstacleId = op.TargetObstacleId,
                    PlannedOnset = op.OnsetTime,
                    PlannedClose = op.CloseTime,
                    Presented = false,
                    Valid = false,
                    InvalidReason = "not_presented",
                    Violation = 0,
                    EntryEpisodes = 0,
                    FirstEntryTime = double.NaN,
                    MinClearance = float.PositiveInfinity,
                    MaxPenetration = 0f,
                    UnattributedContacts = 0,
                };
            }
        }

        /// <summary>Raw per-event streams for the detailed event log (read after the block runs).</summary>
        public IReadOnlyList<FeedbackCommand> Alerts => _sink.Commands;
        public IReadOnlyList<OutcomeEvent> Outcomes => _detector.Events;
        public IReadOnlyList<OpportunityActivation> Opportunities => _scheduler.Log;

        /// <summary>The primary dataset for this block: one row per scheduled opportunity.</summary>
        public IReadOnlyList<OpportunityOutcome> OpportunityOutcomes => _outcomes;

        /// <summary>
        /// Record that the inducing stimulus for <paramref name="opportunityId"/> actually spawned. The Runtime
        /// spawner calls this. An opportunity that never presents is invalid and must not sit in the primary
        /// denominator [§5].
        /// </summary>
        public void MarkPresented(string opportunityId)
        {
            for (int i = 0; i < _outcomes.Length; i++)
            {
                if (_outcomes[i].OpportunityId != opportunityId) continue;
                _outcomes[i].Presented = true;
                return;
            }
        }

        /// <summary>Flag that the block ended early, so opportunities that never closed are marked invalid.</summary>
        public void MarkAborted() => _aborted = true;

        public void Tick(in PoseFrame frame)
        {
            if (double.IsNaN(_firstTime)) _firstTime = frame.Timestamp;
            _lastTime = frame.Timestamp;

            _scheduler.Tick(frame.Timestamp);   // open/close opportunities at this block time
            _conditionManager.Tick(frame);      // fire feedback (counted by _sink)
            _detector.Tick(frame);              // detect collisions / near-misses
            _policyProbe.Tick(frame);           // what BOTH policies would have done, regardless of condition
            SampleOpenOpportunities(frame);     // designated limb vs designated hazard, this frame
            ProcessNewOutcomes();               // attribute any new collisions to the open opportunity

            // Avoidance latency: register new alert ONSETS, then advance with current per-limb distances.
            // A discrete command is always an onset. A continuous command is an onset only when the cue was
            // silent on the previous frame, or has moved to a different limb.
            IReadOnlyList<FeedbackCommand> cmds = _sink.Commands;
            double dt = double.IsNaN(_prevTickTime) ? 0.0 : frame.Timestamp - _prevTickTime;
            for (; _processedAlerts < cmds.Count; _processedAlerts++)
            {
                FeedbackCommand c = cmds[_processedAlerts];
                bool onset;
                if (c.Form == CueForm.Continuous)
                {
                    onset = !_continuousEngaged || c.Limb != _continuousLimb;
                    _continuousEngaged = true;
                    _continuousLimb = c.Limb;
                    if (dt > 0.0) _cueDoseSeconds += c.Intensity * dt;
                }
                else onset = true;

                if (onset)
                {
                    _latency.RegisterAlert(c.DataTime, c.Limb, _genericCue);
                    _alertEpisodes++;
                }
            }

            // Disengagement: the continuous cue went silent if the newest command is not from this frame.
            if (_continuousEngaged &&
                (cmds.Count == 0 || cmds[cmds.Count - 1].DataTime < frame.Timestamp))
                _continuousEngaged = false;

            _prevTickTime = frame.Timestamp;
            _latency.Tick(frame.Timestamp, _detector.CurrentDistances);
        }

        // Track the designated pair directly, every frame the opportunity is open. Using the SAME contact rule
        // as CollisionDetector keeps the primary outcome and the event log consistent by construction.
        private void SampleOpenOpportunities(in PoseFrame frame)
        {
            double t = frame.Timestamp;
            for (int i = 0; i < _schedule.Length; i++)
            {
                Opportunity op = _schedule[i];
                if (t < op.OnsetTime || t > op.CloseTime) { _insideNow[i] = false; continue; }

                if (!TryFindObstacle(op.TargetObstacleId, out Obstacle target)) continue;

                Vector3 p = frame.Get(op.TargetLimb);
                float reach = _detectorParams.RadiusFor(op.TargetLimb);
                float effective = Mathf.Max(0f, target.DistanceTo(p) - reach);

                if (effective < _outcomes[i].MinClearance) _outcomes[i].MinClearance = effective;

                float depth = PenetrationDepth(target, p);
                if (depth > _outcomes[i].MaxPenetration) _outcomes[i].MaxPenetration = depth;

                bool inside = effective <= _detectorParams.ContactDistance;
                if (inside && !_insideNow[i])          // rising edge = a new entry episode
                {
                    _outcomes[i].EntryEpisodes++;
                    if (_outcomes[i].Violation == 0)
                    {
                        _outcomes[i].Violation = 1;    // repeats within one opportunity stay one primary event
                        _outcomes[i].FirstEntryTime = t;
                    }
                }
                _insideNow[i] = inside;
            }
        }

        // Fold the counterfactual policy timings into the primary rows. The probe keeps its own array so it
        // stays a pure observer; this is the single point where the two meet.
        private void CopyPolicyTriggerRecords()
        {
            IReadOnlyList<PolicyTriggerRecord> recs = _policyProbe.Records;
            for (int i = 0; i < _outcomes.Length && i < recs.Count; i++) _outcomes[i].Timing = recs[i];
        }

        // Depth from the point to the nearest face of the box, 0 when the point is outside.
        private static float PenetrationDepth(in Obstacle o, Vector3 p)
        {
            float dx = o.HalfExtents.x - Mathf.Abs(p.x - o.Center.x);
            float dy = o.HalfExtents.y - Mathf.Abs(p.y - o.Center.y);
            float dz = o.HalfExtents.z - Mathf.Abs(p.z - o.Center.z);
            if (dx <= 0f || dy <= 0f || dz <= 0f) return 0f;
            return Mathf.Min(dx, Mathf.Min(dy, dz));
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

        private void ProcessNewOutcomes()
        {
            IReadOnlyList<OutcomeEvent> events = _detector.Events;
            for (; _processedEvents < events.Count; _processedEvents++)
            {
                OutcomeEvent e = events[_processedEvents];
                if (e.Clearance < _minClearance) _minClearance = e.Clearance;

                if (e.Kind == OutcomeKind.Collision)
                {
                    Opportunity? op = _scheduler.ActiveFor(e.Limb);
                    if (op.HasValue)
                    {
                        _hitOpportunityIds.Add(op.Value.Id);
                        _collisionsAttributed++;
                    }
                    else
                    {
                        _collisionsUnattributed++;
                    }

                    // Secondary safety stream: a contact inside an open window that is NOT the designated pair.
                    for (int i = 0; i < _schedule.Length; i++)
                    {
                        Opportunity s = _schedule[i];
                        if (e.DataTime < s.OnsetTime || e.DataTime > s.CloseTime) continue;
                        bool designated = e.Limb == s.TargetLimb && e.ObstacleId == s.TargetObstacleId;
                        if (!designated) _outcomes[i].UnattributedContacts++;
                    }
                }
            }
        }

        /// <summary>Finalizes the block (flushes any still-open engagement) and returns the result row.
        /// Idempotent: the flush runs only once.</summary>
        public BlockResult Finish()
        {
            if (!_finished)
            {
                _detector.Flush(_lastTime);
                ProcessNewOutcomes();
                CopyPolicyTriggerRecords();
                FinalizeOpportunityValidity();
                _finished = true;
            }

            int avoidanceCount = _latency.Events.Count;
            double meanLatency = double.NaN;
            if (avoidanceCount > 0)
            {
                double sum = 0;
                for (int i = 0; i < _latency.Events.Count; i++) sum += _latency.Events[i].LatencySeconds;
                meanLatency = sum / avoidanceCount;
            }

            return new BlockResult
            {
                Context = _ctx,
                Opportunities = _scheduler.Total,
                Collisions = _detector.Collisions,
                OpportunitiesHit = _hitOpportunityIds.Count,
                OpportunitiesAvoided = _scheduler.Total - _hitOpportunityIds.Count,
                CollisionsAttributed = _collisionsAttributed,
                CollisionsUnattributed = _collisionsUnattributed,
                NearMisses = _detector.NearMisses,
                Alerts = _sink.Count,
                MinClearance = _minClearance,
                DurationSeconds = double.IsNaN(_firstTime) ? 0.0 : _lastTime - _firstTime,
                AvoidanceCount = avoidanceCount,
                MeanAvoidanceLatencySeconds = meanLatency,
            };
        }

        // An opportunity counts toward the primary denominator only if its stimulus presented AND the block
        // actually ran through its window. Everything else is recorded with a reason [§5].
        private void FinalizeOpportunityValidity()
        {
            for (int i = 0; i < _outcomes.Length; i++)
            {
                bool ranPastClose = !double.IsNaN(_firstTime) && _lastTime >= _outcomes[i].PlannedClose;

                if (!_outcomes[i].Presented)
                {
                    _outcomes[i].Valid = false;
                    _outcomes[i].InvalidReason = "not_presented";
                }
                else if (!ranPastClose)
                {
                    _outcomes[i].Valid = false;
                    _outcomes[i].InvalidReason = _aborted ? "block_aborted" : "window_not_completed";
                }
                else
                {
                    _outcomes[i].Valid = true;
                    _outcomes[i].InvalidReason = "";
                }

                if (float.IsPositiveInfinity(_outcomes[i].MinClearance))
                    _outcomes[i].MinClearance = float.NaN;   // never sampled -> NA in the CSV
            }
        }
    }
}
