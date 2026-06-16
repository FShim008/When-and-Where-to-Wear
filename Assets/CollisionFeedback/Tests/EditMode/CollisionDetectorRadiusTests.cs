using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CollisionFeedback.Core;
using Joint = CollisionFeedback.Core.Joint; // disambiguate from UnityEngine.Joint (physics component)

namespace CollisionFeedback.Tests
{
    /// <summary>Plan Task 7.3: the per-limb effective contact radius (wrist→hand / ankle→foot reach).</summary>
    public class CollisionDetectorRadiusTests
    {
        private static readonly List<Joint> Limbs = new() { Joint.RightHand };

        // Right hand sweeps straight along +z at x=0; box surface sits ~0.10 m off that path (a near-miss
        // for the bare wrist, identical to CollisionDetectorTests' grazing case).
        private static List<PoseFrame> Sweep() =>
            SyntheticTrajectory.LinearApproach(Joint.RightHand, new Vector3(0f, 1f, 0f), new Vector3(0f, 0f, 2f), 1.0f, 1f / 90f);

        private static List<Obstacle> GrazingBox() => new()
        {
            new Obstacle("o", new Vector3(0.15f, 1f, 1.0f), new Vector3(0.05f, 1f, 0.05f)),
        };

        private static void Run(CollisionDetector d, List<PoseFrame> frames)
        {
            foreach (var f in frames) d.Tick(f);
        }

        [Test]
        public void Default_no_radius_is_a_no_op_grazing_stays_a_near_miss()
        {
            var d = new CollisionDetector(GrazingBox(), Limbs, new DetectorParams()); // LimbContactRadius == null
            Run(d, Sweep());
            Assert.That(d.Collisions, Is.EqualTo(0));
            Assert.That(d.NearMisses, Is.EqualTo(1));
        }

        [Test]
        public void Hand_contact_radius_promotes_a_grazing_near_miss_to_a_collision()
        {
            var p = new DetectorParams
            {
                LimbContactRadius = new Dictionary<Joint, float> { { Joint.RightHand, 0.12f } },
            };
            var d = new CollisionDetector(GrazingBox(), Limbs, p);
            Run(d, Sweep());

            // Wrist stays ~0.10 m from the foam, but the +0.12 m hand reach brings the effective distance
            // to ~0 ⇒ a real hand contact the bare-wrist detector would have missed.
            Assert.That(d.Collisions, Is.EqualTo(1));
            Assert.That(d.NearMisses, Is.EqualTo(0));
        }
    }
}
