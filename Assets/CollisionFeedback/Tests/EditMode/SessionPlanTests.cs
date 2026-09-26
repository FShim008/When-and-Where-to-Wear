using System.Collections.Generic;
using NUnit.Framework;
using CollisionFeedback.Core;

namespace CollisionFeedback.Tests
{
    public class SessionPlanTests
    {
        [Test]
        public void Every_row_and_column_is_a_permutation_latin_square()
        {
            const int n = 6;
            int[][] sq = WilliamsSquare.Generate(n);

            for (int i = 0; i < n; i++)
            {
                var rowSet = new HashSet<int>();
                var colSet = new HashSet<int>();
                for (int j = 0; j < n; j++)
                {
                    rowSet.Add(sq[i][j]);
                    colSet.Add(sq[j][i]);
                }
                Assert.That(rowSet.Count, Is.EqualTo(n), $"row {i} not a permutation");
                Assert.That(colSet.Count, Is.EqualTo(n), $"column {i} not a permutation");
            }
        }

        [Test]
        public void Square_is_balanced_for_first_order_carryover()
        {
            const int n = 6;
            int[][] sq = WilliamsSquare.Generate(n);

            var pairCount = new Dictionary<(int, int), int>();
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n - 1; j++)
                {
                    var key = (sq[i][j], sq[i][j + 1]);
                    pairCount[key] = pairCount.TryGetValue(key, out int c) ? c + 1 : 1;
                }

            // Every ordered pair of distinct treatments must be adjacent exactly once.
            Assert.That(pairCount.Count, Is.EqualTo(n * (n - 1)));
            foreach (var kv in pairCount)
                Assert.That(kv.Value, Is.EqualTo(1), $"pair {kv.Key} occurred {kv.Value} times");
        }

        [Test]
        public void A_participant_sees_all_six_conditions_and_six_distinct_layouts()
        {
            var layouts = new List<string> { "L1", "L2", "L3", "L4", "L5", "L6" };
            var plan = SessionPlan.For(participantId: 3, layoutIds: layouts);

            Assert.That(plan.Count, Is.EqualTo(6));

            var conds = new HashSet<Condition>();
            var lays = new HashSet<string>();
            foreach (var b in plan) { conds.Add(b.Condition); lays.Add(b.LayoutId); }

            Assert.That(conds.Count, Is.EqualTo(6));
            Assert.That(lays.Count, Is.EqualTo(6));
        }

        [Test]
        public void Across_six_participants_each_condition_hits_each_position_once()
        {
            var layouts = new List<string> { "L1", "L2", "L3", "L4", "L5", "L6" };

            // position -> set of conditions seen there across participants 0..5
            var perPosition = new Dictionary<int, HashSet<Condition>>();
            for (int p = 0; p < 6; p++)
                foreach (var b in SessionPlan.For(p, layouts))
                {
                    if (!perPosition.TryGetValue(b.BlockIndex, out var set))
                        perPosition[b.BlockIndex] = set = new HashSet<Condition>();
                    set.Add(b.Condition);
                }

            foreach (var kv in perPosition)
                Assert.That(kv.Value.Count, Is.EqualTo(6), $"position {kv.Key} not balanced across conditions");
        }
        // -- Condition x layout independence [PAPER1_STUDY_DESIGN section 7] -----------------------

        private static readonly List<string> SixLayouts =
            new() { "L1", "L2", "L3", "L4", "L5", "L6" };

        [Test]
        public void Condition_is_not_confounded_with_layout()
        {
            // REGRESSION GUARD, 2026-09-25. Both orders used to key off participantId directly, so they
            // were perfectly correlated: 6 distinct plans instead of 36, and 12 of the 36 condition x
            // layout cells NEVER occurred. paper1_analysis.R fits (1 | layout) assuming layout is a
            // decorrelated nuisance factor, so a layout-difficulty effect would have loaded straight onto
            // the condition estimates and biased H1, H2 and H4'.
            var counts = new Dictionary<Condition, Dictionary<string, int>>();
            int cycle = SessionPlan.Conditions.Length * SessionPlan.Conditions.Length;   // 36

            for (int pid = 0; pid < cycle; pid++)
                foreach (BlockAssignment b in SessionPlan.For(pid, SixLayouts))
                {
                    if (!counts.TryGetValue(b.Condition, out var row))
                        counts[b.Condition] = row = new Dictionary<string, int>();
                    row[b.LayoutId] = row.TryGetValue(b.LayoutId, out int c) ? c + 1 : 1;
                }

            foreach (Condition cond in SessionPlan.Conditions)
                foreach (string lay in SixLayouts)
                {
                    Assert.That(counts[cond].ContainsKey(lay), Is.True,
                        $"{cond} never occurs with {lay}. Condition is confounded with layout.");
                    Assert.That(counts[cond][lay], Is.EqualTo(SessionPlan.Conditions.Length),
                        $"{cond} x {lay} is unbalanced over one full allocation of {cycle} participants.");
                }
        }

        [Test]
        public void One_full_allocation_produces_distinct_plans()
        {
            var seen = new HashSet<string>();
            int cycle = SessionPlan.Conditions.Length * SessionPlan.Conditions.Length;

            for (int pid = 0; pid < cycle; pid++)
                seen.Add(string.Join(",", SessionPlan.For(pid, SixLayouts)
                                                     .ConvertAll(b => b.Condition + "@" + b.LayoutId)));

            Assert.That(seen.Count, Is.EqualTo(cycle),
                "The 6x6 crossing of condition-order row and layout-order row must give 36 distinct " +
                "plans. Fewer means the two orders are correlated and some cells are never visited.");
        }

        [Test]
        public void Complete_allocations_are_multiples_of_thirty_six()
        {
            // Section 4 is explicit: if power needs more than 36, round UP to 72 rather than accept an
            // incomplete crossing. 48 divides by 6 (the Williams width) but NOT by 36, so it leaves some
            // condition-order x layout-order combinations over-represented.
            Assert.That(SessionPlan.IsCompleteAllocation(36), Is.True);
            Assert.That(SessionPlan.IsCompleteAllocation(72), Is.True);
            Assert.That(SessionPlan.IsCompleteAllocation(48), Is.False,
                "48 is NOT a complete allocation. Divisibility by 6 balances condition order alone; the " +
                "design requires the full 6x6 crossing with layout order.");
        }
    }
}
