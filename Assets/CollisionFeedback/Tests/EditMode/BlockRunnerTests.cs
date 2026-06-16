using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CollisionFeedback.Core;
using Joint = CollisionFeedback.Core.Joint; // disambiguate from UnityEngine.Joint (physics component)

namespace CollisionFeedback.Tests
{
    public class BlockRunnerTests
    {
        private static Obstacle BigBox(string id, float z) =>
            new Obstacle(id, new Vector3(0f, 1f, z), new Vector3(0.30f, 1f, 0.30f));

        private static List<PoseFrame> RightHandSweep() =>
            SyntheticTrajectory.LinearApproach(Joint.RightHand, new Vector3(0f, 1f, 0f),
                                               new Vector3(0f, 0f, 2f), 1.0f, 1f / 90f);

        private static BlockResult RunBlock(Condition condition, List<Obstacle> obstacles, List<Joint> limbs,
                                            List<Opportunity> schedule, List<PoseFrame> frames)
        {
            var ctx = new BlockContext { ParticipantId = 1, BlockIndex = 0, Condition = condition, LayoutId = "L1" };
            var runner = new BlockRunner(ctx, obstacles, limbs, schedule, new OracleParams(), new DetectorParams());
            foreach (var f in frames) runner.Tick(f);
            return runner.Finish();
        }

        [Test]
        public void Collision_inside_its_opportunity_window_is_attributed()
        {
            var obstacles = new List<Obstacle> { BigBox("O", 1.0f) };
            var limbs = new List<Joint> { Joint.RightHand };
            var schedule = new List<Opportunity> { new Opportunity("OP01", 0.0, 2.0, Joint.RightHand, "O") };

            var r = RunBlock(Condition.None, obstacles, limbs, schedule, RightHandSweep());

            Assert.That(r.Collisions, Is.EqualTo(1));
            Assert.That(r.CollisionsAttributed, Is.EqualTo(1));
            Assert.That(r.OpportunitiesHit, Is.EqualTo(1));
            Assert.That(r.OpportunitiesAvoided, Is.EqualTo(0));
            Assert.That(r.CollisionsPerOpportunity, Is.EqualTo(1.0f).Within(1e-4f));
        }

        [Test]
        public void Collision_with_no_opportunity_for_that_limb_is_unattributed()
        {
            var obstacles = new List<Obstacle> { BigBox("O", 1.0f) };
            var limbs = new List<Joint> { Joint.RightHand, Joint.LeftFoot };
            // Opportunity targets a DIFFERENT limb, so the right-hand collision is spontaneous.
            var schedule = new List<Opportunity> { new Opportunity("OP01", 0.0, 2.0, Joint.LeftFoot, "O") };

            var r = RunBlock(Condition.None, obstacles, limbs, schedule, RightHandSweep());

            Assert.That(r.Collisions, Is.EqualTo(1));
            Assert.That(r.CollisionsAttributed, Is.EqualTo(0));
            Assert.That(r.CollisionsUnattributed, Is.EqualTo(1));
            Assert.That(r.OpportunitiesHit, Is.EqualTo(0));
        }

        [Test]
        public void An_opportunity_with_no_collision_is_avoided()
        {
            // Obstacle 1 m to the side of the hand's straight path -> no collision.
            var obstacles = new List<Obstacle> { new Obstacle("O", new Vector3(1.0f, 1f, 1.0f), new Vector3(0.1f, 1f, 0.1f)) };
            var limbs = new List<Joint> { Joint.RightHand };
            var schedule = new List<Opportunity> { new Opportunity("OP01", 0.0, 2.0, Joint.RightHand, "O") };

            var r = RunBlock(Condition.None, obstacles, limbs, schedule, RightHandSweep());

            Assert.That(r.Collisions, Is.EqualTo(0));
            Assert.That(r.OpportunitiesHit, Is.EqualTo(0));
            Assert.That(r.OpportunitiesAvoided, Is.EqualTo(1));
            Assert.That(r.CollisionsPerOpportunity, Is.EqualTo(0f));
        }

        [Test]
        public void Feedback_alerts_are_counted()
        {
            var obstacles = new List<Obstacle> { new Obstacle("O", new Vector3(0f, 1f, 2f), new Vector3(0.15f, 1f, 0.15f)) };
            var limbs = new List<Joint> { Joint.RightHand };
            var schedule = new List<Opportunity>(); // empty: focus on the alert covariate

            var r = RunBlock(Condition.PB, obstacles, limbs, schedule, RightHandSweep());

            Assert.That(r.Alerts, Is.GreaterThanOrEqualTo(1));
            Assert.That(r.Opportunities, Is.EqualTo(0));
        }
    }
}
