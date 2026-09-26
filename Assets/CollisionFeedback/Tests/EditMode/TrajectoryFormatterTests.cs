using NUnit.Framework;
using UnityEngine;
using CollisionFeedback.Core;
using Joint = CollisionFeedback.Core.Joint;

namespace CollisionFeedback.Tests
{
    /// <summary>
    /// Guards `trajectories_&lt;block&gt;.csv` [PAPER2 §14; gate 10 "logs reconstruct every trial"].
    ///
    /// The file's job is to let a disputed trial be recomputed from the same input the measurement used.
    /// That requires the frames to be identifiable and ordered, and it requires a dropout to be
    /// distinguishable from a logger fault — which is what `seq` paired with `gap_s` provides.
    /// </summary>
    public class TrajectoryFormatterTests
    {
        private static PoseFrame Frame(double t)
        {
            PoseFrame f = PoseFrame.Create(t);
            f.Joints[(int)Joint.RightHand] = new Vector3(0.1f, 1.0f, 0.5f);
            return f;
        }

        private static string Row(long seq, double t, double gap, string trial = "T01") =>
            TrajectoryFormatter.Row(3, 1, 0, trial, seq, Frame(t), "2026-09-25T10:00:00Z", gap);

        [Test]
        public void Header_and_rows_have_the_same_width()
        {
            int expected = TrajectoryFormatter.Header().Split(',').Length;
            Assert.That(Row(0, 1.0, double.NaN).Split(',').Length, Is.EqualTo(expected));
            Assert.That(Row(7, 1.08, 0.011).Split(',').Length, Is.EqualTo(expected));
        }

        [Test]
        public void Header_carries_all_six_joints_in_three_axes()
        {
            string h = TrajectoryFormatter.Header();
            foreach (string j in new[] { "head", "chest", "lhand", "rhand", "lfoot", "rfoot" })
                foreach (string ax in new[] { "_x", "_y", "_z" })
                    Assert.That(h, Does.Contain(j + ax));
        }

        [Test]
        public void The_first_frame_has_no_gap_and_writes_NA()
        {
            // Zero would claim the previous frame arrived simultaneously — a measurement, where none exists.
            Assert.That(Row(0, 1.0, double.NaN), Does.Contain(",NA,"));
        }

        [Test]
        public void Sequence_and_gap_together_distinguish_a_dropout_from_a_logger_fault()
        {
            // Contiguous seq + large gap  => the tracker produced nothing (a real dropout).
            // Jumping seq                 => the logger lost frames it had been given.
            // The analysis must not confuse the two, so both fields have to be present and independent.
            string realDropout = Row(41, 5.30, 0.42);
            string next = Row(42, 5.31, 0.01);

            Assert.That(realDropout.Split(',')[4], Is.EqualTo("41"));
            Assert.That(next.Split(',')[4], Is.EqualTo("42"));
            Assert.That(realDropout.Split(',')[7], Is.EqualTo("0.4200"));
        }

        [Test]
        public void There_is_no_tracking_quality_column()
        {
            // PoseFrame carries no confidence field and TrackerKeypointSource surfaces none. A constant
            // "good" column would be read as a measurement, which is worse than an absent one.
            Assert.That(TrajectoryFormatter.Header().ToLowerInvariant(), Does.Not.Contain("quality"));
            Assert.That(TrajectoryFormatter.Header(), Does.Contain("gap_s"),
                "gap_s is the honest substitute: a property of delivery, not of tracker confidence.");
        }

        [Test]
        public void Trial_id_is_on_every_row_so_frames_attribute_to_a_trial()
        {
            Assert.That(Row(0, 1.0, double.NaN, "P03-T117"), Does.Contain(",P03-T117,"));
        }

        [Test]
        public void Positions_use_invariant_decimals()
        {
            // A comma decimal separator would inject extra columns and corrupt every row after it.
            Assert.That(Row(0, 1.0, double.NaN), Does.Contain("0.1000,1.0000,0.5000"));
        }

        [Test]
        public void A_trial_id_containing_a_comma_is_escaped()
        {
            string row = Row(0, 1.0, double.NaN, "odd,id");
            int fields = 1; bool q = false;
            foreach (char c in row) { if (c == '"') q = !q; else if (c == ',' && !q) fields++; }
            Assert.That(fields, Is.EqualTo(TrajectoryFormatter.Header().Split(',').Length));
        }
    }
}
