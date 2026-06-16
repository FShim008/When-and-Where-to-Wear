using System.Collections.Generic;
using System.Linq;

namespace CollisionFeedback.Core
{
    public enum OpportunityPhase
    {
        Pending,
        Open,
        Closed,
    }

    /// <summary>
    /// A scripted, behavior-INDEPENDENT collision-inducing event. There are a fixed number per block
    /// (~12), each nudging a specific limb toward a specific obstacle. This is the denominator of the
    /// primary DV (collisions-per-opportunity) and the fix for the opportunity-circularity threat:
    /// opportunities come from the script and the clock, never from the participant's own movements.
    /// </summary>
    public readonly struct Opportunity
    {
        public readonly string Id;
        public readonly double OnsetTime;      // s (block-relative): when the inducing stimulus appears
        public readonly double WindowSeconds;  // s: how long the opportunity stays open for attribution
        public readonly Joint TargetLimb;
        public readonly string TargetObstacleId;

        public Opportunity(string id, double onsetTime, double windowSeconds, Joint targetLimb, string targetObstacleId)
        {
            Id = id;
            OnsetTime = onsetTime;
            WindowSeconds = windowSeconds;
            TargetLimb = targetLimb;
            TargetObstacleId = targetObstacleId;
        }

        public double CloseTime => OnsetTime + WindowSeconds;
    }

    /// <summary>Record of an opportunity opening or closing (for the log and to drive the game's stimulus).</summary>
    public readonly struct OpportunityActivation
    {
        public readonly string OpportunityId;
        public readonly OpportunityPhase Phase;   // Open or Closed
        public readonly double DataTime;
        public readonly Joint TargetLimb;
        public readonly string TargetObstacleId;

        public OpportunityActivation(string opportunityId, OpportunityPhase phase, double dataTime,
                                     Joint targetLimb, string targetObstacleId)
        {
            OpportunityId = opportunityId;
            Phase = phase;
            DataTime = dataTime;
            TargetLimb = targetLimb;
            TargetObstacleId = targetObstacleId;
        }
    }

    /// <summary>
    /// Fires a fixed, scripted schedule of opportunities by block time. Each opportunity transitions
    /// Pending -> Open (at onset) -> Closed (at onset + window), exactly once. The Runtime polls the
    /// activation log to spawn the inducing orb/projectile; the metrics layer uses <see cref="ActiveFor"/>
    /// to attribute a detected collision to the opportunity that was open for that limb.
    /// Hardware-free and deterministic (driven by the block time you pass to <see cref="Tick"/>).
    /// </summary>
    public sealed class OpportunityScheduler
    {
        private readonly Opportunity[] _ordered;
        private readonly OpportunityPhase[] _phase;
        private readonly List<OpportunityActivation> _log = new();

        public int Total => _ordered.Length;
        public int Opened { get; private set; }
        public int Closed { get; private set; }
        public IReadOnlyList<OpportunityActivation> Log => _log;

        public OpportunityScheduler(IReadOnlyList<Opportunity> schedule)
        {
            // Stable sort by onset (LINQ OrderBy is a stable sort) so equal-onset events keep schedule order.
            _ordered = schedule.OrderBy(o => o.OnsetTime).ToArray();
            _phase = new OpportunityPhase[_ordered.Length]; // defaults to Pending (0)
        }

        public void Tick(double blockTime)
        {
            for (int i = 0; i < _ordered.Length; i++)
            {
                Opportunity op = _ordered[i];

                if (_phase[i] == OpportunityPhase.Pending && blockTime >= op.OnsetTime)
                {
                    _phase[i] = OpportunityPhase.Open;
                    Opened++;
                    _log.Add(new OpportunityActivation(op.Id, OpportunityPhase.Open, blockTime, op.TargetLimb, op.TargetObstacleId));
                }

                if (_phase[i] == OpportunityPhase.Open && blockTime >= op.CloseTime)
                {
                    _phase[i] = OpportunityPhase.Closed;
                    Closed++;
                    _log.Add(new OpportunityActivation(op.Id, OpportunityPhase.Closed, blockTime, op.TargetLimb, op.TargetObstacleId));
                }
            }
        }

        /// <summary>The opportunity currently open for <paramref name="limb"/> (for outcome attribution), or null.</summary>
        public Opportunity? ActiveFor(Joint limb)
        {
            for (int i = 0; i < _ordered.Length; i++)
                if (_phase[i] == OpportunityPhase.Open && _ordered[i].TargetLimb == limb)
                    return _ordered[i];
            return null;
        }

        public OpportunityPhase PhaseOf(int index) => _phase[index];
    }

    /// <summary>Convenience builders. Real per-layout schedules come from the storyboard
    /// (e.g. IEEEVR2027_Layout1_Storyboard.md); this is for the demo and for scheduling tests.</summary>
    public static class OpportunitySchedules
    {
        /// <summary>Evenly spaces <paramref name="count"/> opportunities across a block, round-robining
        /// the supplied (limb, obstacle) targets.</summary>
        public static List<Opportunity> EvenlySpaced(int count, double blockSeconds, double windowSeconds,
                                                     IReadOnlyList<(Joint limb, string obstacleId)> targets)
        {
            var list = new List<Opportunity>(count);
            double spacing = blockSeconds / (count + 1);
            for (int i = 0; i < count; i++)
            {
                var tgt = targets[i % targets.Count];
                double onset = spacing * (i + 1);
                list.Add(new Opportunity($"OP{i + 1:D2}", onset, windowSeconds, tgt.limb, tgt.obstacleId));
            }
            return list;
        }

        /// <summary>
        /// The concrete Layout-L1 schedule from IEEEVR2027_Layout1_Storyboard.md: the 12 scripted
        /// collision-opportunity events (E1..E12) on a 180 s block, balanced by obstacle (O2×4 · O3×4 ·
        /// O1×2 · boundary×2) and by limb/side (R-arm×4, L-arm×4, R-foot, L-foot, chest×2). Each onset is
        /// the storyboard time; <paramref name="windowSeconds"/> is how long the opportunity stays open for
        /// outcome attribution (pilot-tunable; default sits well inside the ~13 s inter-event gap). The other
        /// 5 layouts mirror this same 12-event structure with rotated geometry.
        ///
        /// Arm-level routing (current hardware): hand/forearm/upper-arm risks map to the L/R hand unit;
        /// torso/hip/boundary map to the chest — so each event's <see cref="Opportunity.TargetLimb"/> is one
        /// of the 6 tracked joints used for collision attribution.
        /// </summary>
        public static List<Opportunity> Layout1(double windowSeconds = 6.0)
        {
            (string id, double t, Joint limb, string obstacle)[] events =
            {
                ("E1",    8.0, Joint.RightHand, "O2"),       // reach forward-right past the pillar
                ("E2",   22.0, Joint.LeftHand,  "O3"),       // lean/step left, L upper arm toward the panel
                ("E3",   35.0, Joint.RightFoot, "O1"),       // crouch/reach down, R knee/shin at the low block
                ("E4",   48.0, Joint.LeftHand,  "O3"),       // reach left around the panel edge
                ("E5",   61.0, Joint.Chest,     "BOUNDARY"), // step to the front edge (torso / lead hand)
                ("E6",   74.0, Joint.RightHand, "O2"),       // duck/step right, R hip/forearm toward the pillar
                ("E7",   87.0, Joint.RightHand, "O2"),       // wide right-arm sweep, R upper arm
                ("E8",  100.0, Joint.LeftFoot,  "O1"),       // step laterally clear of the low block (L shin)
                ("E9",  113.0, Joint.LeftHand,  "O3"),       // reach up-and-over the panel (L upper arm)
                ("E10", 126.0, Joint.LeftHand,  "O3"),       // big left lunge (L torso/upper arm)
                ("E11", 139.0, Joint.Chest,     "BOUNDARY"), // backward/diagonal step toward the corner
                ("E12", 152.0, Joint.RightHand, "O2"),       // compound dodge+reach: cue the lowest-TTC limb (R forearm)
            };

            var list = new List<Opportunity>(events.Length);
            foreach (var e in events)
                list.Add(new Opportunity(e.id, e.t, windowSeconds, e.limb, e.obstacle));
            return list;
        }
    }
}
