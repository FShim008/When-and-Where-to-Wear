using System.Collections.Generic;

namespace CollisionFeedback.Core
{
    /// <summary>
    /// Orchestrates one block: ticks the opportunity scheduler, the condition (feedback), and the
    /// collision detector in lockstep, attributing each detected collision to the opportunity open for
    /// that limb. Produces the analysis-ready <see cref="BlockResult"/>.
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

        public BlockRunner(BlockContext ctx, IReadOnlyList<Obstacle> obstacles, IReadOnlyList<Joint> limbs,
                           IReadOnlyList<Opportunity> schedule, OracleParams oracleParams,
                           DetectorParams detectorParams, IFeedbackSink deviceSink = null)
        {
            _ctx = ctx;
            _sink = new CountingSink(deviceSink);
            _conditionManager = new ConditionManager(ctx.Condition, oracleParams, obstacles, limbs, _sink);
            _detector = new CollisionDetector(obstacles, limbs, detectorParams);
            _scheduler = new OpportunityScheduler(schedule);
            _genericCue = ctx.Condition == Condition.RG || ctx.Condition == Condition.PG;
        }

        /// <summary>Raw per-event streams for the detailed event log (read after the block runs).</summary>
        public IReadOnlyList<FeedbackCommand> Alerts => _sink.Commands;
        public IReadOnlyList<OutcomeEvent> Outcomes => _detector.Events;
        public IReadOnlyList<OpportunityActivation> Opportunities => _scheduler.Log;

        public void Tick(in PoseFrame frame)
        {
            if (double.IsNaN(_firstTime)) _firstTime = frame.Timestamp;
            _lastTime = frame.Timestamp;

            _scheduler.Tick(frame.Timestamp);   // open/close opportunities at this block time
            _conditionManager.Tick(frame);      // fire feedback (counted by _sink)
            _detector.Tick(frame);              // detect collisions / near-misses
            ProcessNewOutcomes();               // attribute any new collisions to the open opportunity

            // Avoidance latency: register new alerts, then advance with current per-limb distances.
            IReadOnlyList<FeedbackCommand> cmds = _sink.Commands;
            for (; _processedAlerts < cmds.Count; _processedAlerts++)
                _latency.RegisterAlert(cmds[_processedAlerts].DataTime, cmds[_processedAlerts].Limb, _genericCue);
            _latency.Tick(frame.Timestamp, _detector.CurrentDistances);
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
    }
}
