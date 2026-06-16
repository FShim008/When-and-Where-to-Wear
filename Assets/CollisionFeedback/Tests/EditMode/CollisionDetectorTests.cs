using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CollisionFeedback.Core;
using Joint = CollisionFeedback.Core.Joint; // disambiguate from UnityEngine.Joint (physics component)

namespace CollisionFeedback.Tests
{
    public class CollisionDetectorTests
    {
        private static readonly List<Joint> Limbs = new() { Joint.RightHand };

        private static List<PoseFrame> RightHandSweep(Vector3 start, Vector3 velocity, float seconds = 1.0f) =>
            SyntheticTrajectory.LinearApproach(Joint.RightHand, start, velocity, seconds, 1f / 90f);

        private static void Run(CollisionDetector d, List<PoseFrame> frames)
        {
            foreach (var f in frames) d.Tick(f);
        }

        [Test]
        public void Passing_through_an_obstacle_counts_one_collision_no_near_miss()
        {
            var obstacles = new List<Obstacle>
            {
                new Obstacle("o", new Vector3(0f, 1f, 1.0f), new Vector3(0.30f, 1f, 0.30f)),
            };
            var d = new CollisionDetector(obstacles, Limbs, new DetectorParams());

            Run(d, RightHandSweep(new Vector3(0f, 1f, 0f), new Vector3(0f, 0f, 2f))); // straight through the box

            Assert.That(d.Collisions, Is.EqualTo(1));
            Assert.That(d.NearMisses, Is.EqualTo(0));
            Assert.That(d.Events[0].Kind, Is.EqualTo(OutcomeKind.Collision));
        }

        [Test]
        public void Grazing_past_an_obstacle_counts_one_near_miss_no_collision()
        {
            // Box just off the hand's straight path: nearest surface ~0.10 m at closest approach.
            var obstacles = new List<Obstacle>
            {
                new Obstacle("o", new Vector3(0.15f, 1f, 1.0f), new Vector3(0.05f, 1f, 0.05f)),
            };
            var d = new CollisionDetector(obstacles, Limbs, new DetectorParams());

            Run(d, RightHandSweep(new Vector3(0f, 1f, 0f), new Vector3(0f, 0f, 2f)));

            Assert.That(d.Collisions, Is.EqualTo(0));
            Assert.That(d.NearMisses, Is.EqualTo(1));
            Assert.That(d.Events[0].Kind, Is.EqualTo(OutcomeKind.NearMiss));
            Assert.That(d.Events[0].Clearance, Is.EqualTo(0.10f).Within(0.02f)); // min clearance captured
        }

        [Test]
        public void A_wide_pass_counts_nothing()
        {
            var obstacles = new List<Obstacle>
            {
                new Obstacle("o", new Vector3(0.6f, 1f, 1.0f), new Vector3(0.05f, 1f, 0.05f)),
            };
            var d = new CollisionDetector(obstacles, Limbs, new DetectorParams());

            Run(d, RightHandSweep(new Vector3(0f, 1f, 0f), new Vector3(0f, 0f, 2f)));

            Assert.That(d.Collisions, Is.EqualTo(0));
            Assert.That(d.NearMisses, Is.EqualTo(0));
        }

        [Test]
        public void Flush_emits_a_near_miss_for_an_engagement_still_open_at_block_end()
        {
            // Box just off the path; the sweep STOPS while the hand is still inside the near-miss band.
            var obstacles = new List<Obstacle>
            {
                new Obstacle("o", new Vector3(0.15f, 1f, 1.0f), new Vector3(0.05f, 1f, 0.05f)),
            };
            var d = new CollisionDetector(obstacles, Limbs, new DetectorParams());

            var frames = RightHandSweep(new Vector3(0f, 1f, 0f), new Vector3(0f, 0f, 2f), seconds: 0.5f); // ends at z=1.0, mid-engagement
            Run(d, frames);
            Assert.That(d.NearMisses, Is.EqualTo(0)); // engagement never disengaged on its own

            d.Flush(frames[frames.Count - 1].Timestamp);
            Assert.That(d.NearMisses, Is.EqualTo(1)); // flush must not drop the still-open engagement
            Assert.That(d.Events[d.Events.Count - 1].Kind, Is.EqualTo(OutcomeKind.NearMiss));
            Assert.That(d.Events[d.Events.Count - 1].Clearance, Is.EqualTo(0.10f).Within(0.02f));
        }
    }
}
