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
    }
}
