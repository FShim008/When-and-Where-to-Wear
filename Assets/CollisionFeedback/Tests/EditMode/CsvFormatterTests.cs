using NUnit.Framework;
using CollisionFeedback.Core;

namespace CollisionFeedback.Tests
{
    public class CsvFormatterTests
    {
        [Test]
        public void Header_and_row_have_the_same_number_of_columns()
        {
            var r = new BlockResult
            {
                Context = new BlockContext { ParticipantId = 7, BlockIndex = 2, Condition = Condition.PB, LayoutId = "L3" },
                Opportunities = 12, Collisions = 3, OpportunitiesHit = 3, OpportunitiesAvoided = 9,
                CollisionsAttributed = 3, CollisionsUnattributed = 0, NearMisses = 5, Alerts = 11,
                MinClearance = 0.04f, DurationSeconds = 180.0,
            };

            int headerCols = CsvFormatter.Header().Split(',').Length;
            int rowCols = CsvFormatter.Row(r).Split(',').Length;
            Assert.That(rowCols, Is.EqualTo(headerCols));
        }

        [Test]
        public void Row_uses_an_invariant_decimal_point()
        {
            var r = new BlockResult
            {
                Context = new BlockContext { ParticipantId = 1, BlockIndex = 0, Condition = Condition.RB, LayoutId = "L1" },
                Opportunities = 8, Collisions = 3,
            };

            // 3 / 8 = 0.375 -> must be written with a '.' (R-friendly), never a locale comma.
            Assert.That(CsvFormatter.Row(r), Does.Contain("0.3750"));
        }

        [Test]
        public void Infinite_min_clearance_serializes_as_NA()
        {
            var r = new BlockResult
            {
                Context = new BlockContext { Condition = Condition.None, LayoutId = "L1" },
                MinClearance = float.PositiveInfinity,
            };

            Assert.That(CsvFormatter.Row(r), Does.Contain("NA"));
        }
    }
}
