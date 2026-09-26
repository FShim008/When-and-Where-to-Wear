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
        /// <summary>
        /// The 6 SCHEDULED conditions in canonical index order (must match the square's treatment indices).
        /// This array — not the <see cref="Condition"/> enum — is the authority on what the study runs.
        ///
        /// CHANGED 2026-09-23: <see cref="Condition.Visual"/> was replaced by <see cref="Condition.PBC"/>.
        /// Visual answered "is haptic better than visual?", a generic modality question. PBC answers why
        /// the only prior in-venue comparison of these trigger policies (Valkov and Linsen, IEEE VR 2019)
        /// found the opposite of our H1: their trigger was confounded with a continuous intensity mapping
        /// participants could switch off by slowing down. PB vs PBC isolates that mapping with the trigger
        /// held constant [PAPER1_STUDY_DESIGN §3; docs/PAPER1_PARAMETER_JUSTIFICATION.md §5B].
        ///
        /// Visual remains implemented and tested, and can be scheduled again by editing this array. It is
        /// simply not part of the Paper 1 protocol. Length must stay 6 — the Williams square is 6x6.
        /// </summary>
        public static readonly Condition[] Conditions =
        {
            Condition.None, Condition.RG, Condition.RB, Condition.PG, Condition.PB, Condition.PBC,
        };

        /// <summary>Plan for one participant (0-based id). Order row = id mod n; layouts rotated by id.</summary>
        /// <summary>
        /// One participant's plan: condition order from a Williams row, layout order from an INDEPENDENT
        /// Williams row [PAPER1_STUDY_DESIGN §7 — "an independent six-sequence layout order… the complete
        /// 6 × 6 crossing of condition-order row and layout-order row"].
        ///
        /// FIXED 2026-09-25 — CONDITION WAS CONFOUNDED WITH LAYOUT.
        /// The previous version keyed BOTH orders to <c>participantId</c> directly: the condition row was
        /// <c>participantId % 6</c> and the layout was <c>(pos + participantId) % 6</c>, which also depends
        /// only on <c>participantId % 6</c>. The two orders were therefore perfectly correlated. Measured
        /// over 72 simulated participants: only **6 distinct plans** existed instead of 36, participants
        /// 0/6/12/18/… were identical, and **12 of the 36 condition × layout cells never occurred** — each
        /// condition met one "home" layout three times as often as any other, and two layouts not at all.
        ///
        /// That is a confound, not an inefficiency: <c>paper1_analysis.R</c> fits <c>(1 | layout)</c> on the
        /// assumption that layout is a decorrelated nuisance factor, so any layout difficulty effect would
        /// have loaded onto the condition estimates and biased H1, H2 and H4'.
        ///
        /// Indexing the layout row by <c>participantId / n</c> makes the two orders independent and walks
        /// the full crossing: participants 0–35 visit all 36 cells, then it repeats. **Complete allocations
        /// are therefore multiples of 36** — see the note on <see cref="IsCompleteAllocation"/>.
        /// </summary>
        public static List<BlockAssignment> For(int participantId, IReadOnlyList<string> layoutIds)
        {
            int n = Conditions.Length;
            int[][] square = WilliamsSquare.Generate(n);

            int p = participantId < 0 ? -participantId : participantId;
            int condRow = p % n;
            int layoutRow = (p / n) % n;   // advances only after a full cycle of condition rows

            // The layout order is its own Williams sequence over the supplied ids. When fewer than n layouts
            // are supplied (bench/debug use), the sequence wraps — the crossing is then incomplete by
            // construction and the caller is responsible for not treating that data as confirmatory.
            int m = layoutIds.Count;
            int[][] layoutSquare = WilliamsSquare.Generate(m >= 2 ? m : 2);
            int layoutRowM = m >= 2 ? layoutRow % m : 0;

            var plan = new List<BlockAssignment>(n);
            for (int pos = 0; pos < n; pos++)
            {
                Condition c = Conditions[square[condRow][pos]];
                string layout = m >= 2 ? layoutIds[layoutSquare[layoutRowM][pos % m]] : layoutIds[0];
                plan.Add(new BlockAssignment(pos, c, layout));
            }
            return plan;
        }

        /// <summary>
        /// Whether <paramref name="analysableN"/> completes the 6 × 6 crossing of condition-order row and
        /// layout-order row that §7 requires. Complete allocations are multiples of <c>Conditions.Length²</c>
        /// — i.e. **36, 72, 108**. An incomplete allocation leaves some condition-order × layout-order
        /// combinations over-represented, which is the imbalance this design exists to avoid.
        ///
        /// §4 of the design doc is explicit: if power requires more than 36, round **up to 72** rather than
        /// accept an incomplete crossing.
        /// </summary>
        public static bool IsCompleteAllocation(int analysableN) =>
            analysableN > 0 && analysableN % (Conditions.Length * Conditions.Length) == 0;
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
