using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CollisionFeedback.Core;
using Joint = CollisionFeedback.Core.Joint; // disambiguate from UnityEngine.Joint (physics component)

namespace CollisionFeedback.Tests
{
    /// <summary>
    /// The PRIMARY outcome [PAPER1_STUDY_DESIGN §5]: a violation is recorded only when the DESIGNATED target
    /// limb enters the DESIGNATED target hazard inside that opportunity's window. These tests pin the three
    /// ways that can go wrong (wrong limb, wrong hazard, wrong time), the one-event-per-opportunity rule, and
    /// the validity gating that keeps the denominator honest.
    /// </summary>
    public class OpportunityOutcomeTests
    {
        private static readonly Vector3 Far = new(50f, 50f, 50f);

        // O1 is the designated hazard; O2 exists so "right limb, wrong hazard" can be tested.
        private static List<Obstacle> Obstacles() => new()
        {
            new Obstacle("O1", new Vector3(0f, 1f, 0f), new Vector3(0.1f, 0.1f, 0.1f)),
            new Obstacle("O2", new Vector3(3f, 1f, 0f), new Vector3(0.1f, 0.1f, 0.1f)),
        };

        private static List<Joint> Limbs() => new()
        {
            Joint.Chest, Joint.LeftHand, Joint.RightHand, Joint.LeftFoot, Joint.RightFoot,
        };

        // One opportunity: right hand should stay out of O1 between t = 1 and t = 3.
        private static List<Opportunity> Schedule() => new()
        {
            new Opportunity("E1", onsetTime: 1.0, windowSeconds: 2.0, targetLimb: Joint.RightHand,
                            targetObstacleId: "O1"),
        };

        private static PoseFrame Frame(double t, Joint placed, Vector3 where)
        {
            var joints = new Vector3[JointInfo.Count];
            for (int j = 0; j < joints.Length; j++) joints[j] = Far;
            joints[(int)placed] = where;
            return new PoseFrame { Timestamp = t, Joints = joints };
        }

        private static BlockContext Ctx() => new()
        {
            ParticipantId = 1, BlockIndex = 0, Condition = Condition.None, LayoutId = "L1",
        };

        /// <summary>Run frames 0..5 s at 20 Hz, putting <paramref name="placed"/> at the hazard between
        /// <paramref name="fromT"/> and <paramref name="toT"/>. Returns the single opportunity outcome.</summary>
        private static OpportunityOutcome Run(Joint placed, Vector3 hazardPos, double fromT, double toT,
                                              bool present = true, bool abort = false, double endT = 5.0)
        {
            var block = new BlockRunner(Ctx(), Obstacles(), Limbs(), Schedule(),
                                        new OracleParams(), new DetectorParams());
            if (present) block.MarkPresented("E1");

            for (double t = 0; t <= endT + 1e-9; t += 0.05)
            {
                bool atHazard = t >= fromT && t <= toT;
                block.Tick(Frame(t, placed, atHazard ? hazardPos : Far));
            }
            if (abort) block.MarkAborted();
            block.Finish();
            return block.OpportunityOutcomes[0];
        }

        [Test]
        public void Designated_limb_in_designated_hazard_inside_window_is_a_violation()
        {
            OpportunityOutcome o = Run(Joint.RightHand, new Vector3(0f, 1f, 0f), 1.5, 2.0);

            Assert.That(o.Violation, Is.EqualTo(1));
            Assert.That(o.Valid, Is.True);
            Assert.That(o.Presented, Is.True);
            Assert.That(o.FirstEntryTime, Is.GreaterThanOrEqualTo(1.0).And.LessThanOrEqualTo(3.0));
            Assert.That(o.MaxPenetration, Is.GreaterThan(0f));
            Assert.That(o.MinClearance, Is.EqualTo(0f).Within(1e-4f));
        }

        [Test]
        public void Wrong_limb_is_not_a_violation_but_is_counted_as_unattributed()
        {
            OpportunityOutcome o = Run(Joint.LeftHand, new Vector3(0f, 1f, 0f), 1.5, 2.0);

            Assert.That(o.Violation, Is.EqualTo(0), "a contact by a non-designated limb must not count");
            Assert.That(o.UnattributedContacts, Is.GreaterThan(0),
                        "it must still be retained as a secondary safety event");
        }

        [Test]
        public void Right_limb_in_the_wrong_hazard_is_not_a_violation()
        {
            // Right hand enters O2, not the designated O1.
            OpportunityOutcome o = Run(Joint.RightHand, new Vector3(3f, 1f, 0f), 1.5, 2.0);

            Assert.That(o.Violation, Is.EqualTo(0));
            Assert.That(o.UnattributedContacts, Is.GreaterThan(0));
        }

        [Test]
        public void Right_pair_outside_the_window_is_not_a_violation()
        {
            // Entry at t = 4.0, after the window closes at 3.0.
            OpportunityOutcome o = Run(Joint.RightHand, new Vector3(0f, 1f, 0f), 4.0, 4.5);

            Assert.That(o.Violation, Is.EqualTo(0));
            Assert.That(o.Valid, Is.True, "the opportunity itself still presented and completed");
        }

        [Test]
        public void Repeated_entries_stay_one_violation_but_increment_episodes()
        {
            var block = new BlockRunner(Ctx(), Obstacles(), Limbs(), Schedule(),
                                        new OracleParams(), new DetectorParams());
            block.MarkPresented("E1");

            var hazard = new Vector3(0f, 1f, 0f);
            for (double t = 0; t <= 5.0 + 1e-9; t += 0.05)
            {
                // Two separate dips into the hazard inside the window, with a clear exit between them.
                bool inside = (t >= 1.2 && t <= 1.4) || (t >= 2.2 && t <= 2.4);
                block.Tick(Frame(t, Joint.RightHand, inside ? hazard : Far));
            }
            block.Finish();
            OpportunityOutcome o = block.OpportunityOutcomes[0];

            Assert.That(o.Violation, Is.EqualTo(1), "the primary outcome is binary per opportunity");
            Assert.That(o.EntryEpisodes, Is.EqualTo(2), "separate entries are counted separately");
        }

        [Test]
        public void An_unpresented_opportunity_is_invalid_and_excluded()
        {
            OpportunityOutcome o = Run(Joint.RightHand, new Vector3(0f, 1f, 0f), 1.5, 2.0, present: false);

            Assert.That(o.Presented, Is.False);
            Assert.That(o.Valid, Is.False, "it cannot sit in the denominator");
            Assert.That(o.InvalidReason, Is.EqualTo("not_presented"));
        }

        [Test]
        public void A_block_that_ends_before_the_window_closes_is_invalid()
        {
            // Stop at t = 2.0; the window closes at 3.0.
            OpportunityOutcome o = Run(Joint.RightHand, Far, 99, 99, present: true, abort: true, endT: 2.0);

            Assert.That(o.Valid, Is.False);
            Assert.That(o.InvalidReason, Is.EqualTo("block_aborted"));
        }

        [Test]
        public void Never_approached_opportunity_is_valid_with_no_violation()
        {
            OpportunityOutcome o = Run(Joint.RightHand, Far, 99, 99);

            Assert.That(o.Valid, Is.True);
            Assert.That(o.Violation, Is.EqualTo(0));
            Assert.That(o.EntryEpisodes, Is.EqualTo(0));
            Assert.That(o.MaxPenetration, Is.EqualTo(0f));
        }

        [Test]
        public void Csv_row_has_one_field_per_header_column()
        {
            OpportunityOutcome o = Run(Joint.RightHand, new Vector3(0f, 1f, 0f), 1.5, 2.0);

            string[] header = OpportunityOutcomeFormatter.Header().Split(',');
            string[] row = OpportunityOutcomeFormatter.Row(Ctx(), o).Split(',');

            Assert.That(row, Has.Length.EqualTo(header.Length));
            Assert.That(OpportunityOutcomeFormatter.Header(), Does.StartWith("participant,block,condition,layout"));
            Assert.That(row[0], Is.EqualTo("1"));
        }

        [Test]
        public void Missing_values_are_written_as_NA_not_NaN_or_Infinity()
        {
            // Never entered the hazard, so FirstEntryTime is NaN.
            string row = OpportunityOutcomeFormatter.Row(Ctx(), Run(Joint.RightHand, Far, 99, 99));
            Assert.That(row, Does.Contain("NA"));
            Assert.That(row, Does.Not.Contain("NaN"), "R would coerce the column to character");
            Assert.That(row, Does.Not.Contain("Infinity"));

            // Never sampled at all, so MinClearance is still +Infinity.
            var unsampled = new OpportunityOutcome
            {
                OpportunityId = "E9",
                TargetLimb = Joint.LeftFoot,
                TargetObstacleId = "O1",
                InvalidReason = "not_presented",
                FirstEntryTime = double.NaN,
                MinClearance = float.PositiveInfinity,
            };
            string infRow = OpportunityOutcomeFormatter.Row(Ctx(), unsampled);
            Assert.That(infRow, Does.Contain("NA"));
            Assert.That(infRow, Does.Not.Contain("NaN"));
            Assert.That(infRow, Does.Not.Contain("Infinity"));
        }
    }
}
