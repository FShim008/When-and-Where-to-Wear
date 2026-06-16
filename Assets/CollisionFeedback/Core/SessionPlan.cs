using System.Collections.Generic;

namespace CollisionFeedback.Core
{
    /// <summary>One participant's block at a given session position: which condition + which layout.</summary>
    public readonly struct BlockAssignment
    {
        public readonly int BlockIndex;     // 0-based position in the session
        public readonly Condition Condition;
        public readonly string LayoutId;

        public BlockAssignment(int blockIndex, Condition condition, string layoutId)
        {
            BlockIndex = blockIndex;
            Condition = condition;
            LayoutId = layoutId;
        }
    }

    /// <summary>
    /// Counterbalancing (-> [Design section 8]). Condition ORDER via a balanced 6x6 Williams Latin square
    /// (balances position AND first-order carryover). Condition->layout via a per-participant rotation so
    /// layout is decorrelated from condition (layout = a counterbalanced nuisance factor).
    /// </summary>
    public static class SessionPlan
    {
        /// <summary>The 6 conditions in canonical index order (must match the square's treatment indices).</summary>
        public static readonly Condition[] Conditions =
        {
            Condition.None, Condition.RG, Condition.RB, Condition.PG, Condition.PB, Condition.Visual,
        };

        /// <summary>Plan for one participant (0-based id). Order row = id mod n; layouts rotated by id.</summary>
        public static List<BlockAssignment> For(int participantId, IReadOnlyList<string> layoutIds)
        {
            int n = Conditions.Length;
            int[][] square = WilliamsSquare.Generate(n);
            int row = ((participantId % n) + n) % n;

            var plan = new List<BlockAssignment>(n);
            for (int pos = 0; pos < n; pos++)
            {
                Condition c = Conditions[square[row][pos]];
                string layout = layoutIds[((pos + participantId) % layoutIds.Count + layoutIds.Count) % layoutIds.Count];
                plan.Add(new BlockAssignment(pos, c, layout));
            }
            return plan;
        }
    }

    /// <summary>
    /// Generates a balanced Williams Latin square for EVEN n by cyclic development of the zig-zag
    /// starting row [0, 1, n-1, 2, n-2, ...]. For even n this single square is row-complete: every
    /// ordered pair of treatments is adjacent exactly once. square[row][pos] = treatment index 0..n-1.
    /// </summary>
    public static class WilliamsSquare
    {
        public static int[][] Generate(int n)
        {
            var start = new int[n];
            start[0] = 0;
            for (int j = 1; j < n; j++)
                start[j] = (j % 2 == 1) ? (j + 1) / 2 : n - j / 2;

            var square = new int[n][];
            for (int i = 0; i < n; i++)
            {
                square[i] = new int[n];
                for (int j = 0; j < n; j++)
                    square[i][j] = (start[j] + i) % n;
            }
            return square;
        }
    }
}
