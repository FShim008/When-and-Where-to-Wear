using NUnit.Framework;
using CollisionFeedback.Core;
using Joint = CollisionFeedback.Core.Joint; // disambiguate from UnityEngine.Joint (physics component)

namespace CollisionFeedback.Tests
{
    public class LogFormatterTests
    {
        private static BlockContext Ctx() =>
            new BlockContext { ParticipantId = 2, BlockIndex = 1, Condition = Condition.PB, LayoutId = "L1" };

        [Test]
        public void Alert_row_matches_header_and_uses_dot_decimals()
        {
            var cmd = new FeedbackCommand(HapticSite.RightHand, Modality.Haptic, TriggerKind.Predictive,
                                          Joint.RightHand, "O2", 1.25, 0.85f, 0.42f);
            int headerCols = EventLogFormatter.Header().Split(',').Length;
            string row = EventLogFormatter.AlertRow(Ctx(), cmd);

            Assert.That(row.Split(',').Length, Is.EqualTo(headerCols));
            Assert.That(row, Does.Contain("0.8500")); // distance, with a '.'
        }

        [Test]
        public void Outcome_and_opportunity_rows_match_header_columns()
        {
            int headerCols = EventLogFormatter.Header().Split(',').Length;
            var outcome = new OutcomeEvent(OutcomeKind.Collision, Joint.LeftFoot, "O1", 2.0, 0.01f);
            var act = new OpportunityActivation("OP03", OpportunityPhase.Open, 3.0, Joint.Chest, "O3");

            Assert.That(EventLogFormatter.OutcomeRow(Ctx(), outcome).Split(',').Length, Is.EqualTo(headerCols));
            Assert.That(EventLogFormatter.OpportunityRow(Ctx(), act).Split(',').Length, Is.EqualTo(headerCols));
        }

        [Test]
        public void Keypoint_log_header_and_row_have_matching_columns()
        {
            var frame = new PoseFrame { Timestamp = 1.0, Joints = SyntheticTrajectory.NeutralPose() };
            int headerCols = KeypointLogFormatter.Header().Split(',').Length;
            int rowCols = KeypointLogFormatter.Row(1, 0, frame).Split(',').Length;

            Assert.That(rowCols, Is.EqualTo(headerCols));
        }

        [Test]
        public void Keypoint_serialize_round_trips_through_the_deserializer()
        {
            var frame = new PoseFrame { Timestamp = 12.3456, Joints = SyntheticTrajectory.NeutralPose() };
            string line = KeypointDeserializer.Serialize(frame);

            Assert.That(KeypointDeserializer.TryParse(line, out var parsed), Is.True);
            Assert.That(parsed.Timestamp, Is.EqualTo(12.3456).Within(1e-9));
            for (int j = 0; j < JointInfo.Count; j++)
                Assert.That((parsed.Joints[j] - frame.Joints[j]).magnitude, Is.LessThan(1e-5f));
        }

        [Test]
        public void Malformed_keypoint_lines_are_rejected()
        {
            Assert.That(KeypointDeserializer.TryParse("1,2,3", out _), Is.False);
            Assert.That(KeypointDeserializer.TryParse("", out _), Is.False);
            Assert.That(KeypointDeserializer.TryParse(null, out _), Is.False);
        }
    }
}
