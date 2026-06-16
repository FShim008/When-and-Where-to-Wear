using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CollisionFeedback.Core;
using Joint = CollisionFeedback.Core.Joint; // disambiguate from UnityEngine.Joint (physics component)

namespace CollisionFeedback.Tests
{
    public class CollisionOracleTests
    {
        [Test]
        public void Obstacle_distance_is_zero_inside_and_positive_outside()
        {
            var o = new Obstacle("o", Vector3.zero, new Vector3(0.5f, 0.5f, 0.5f));

            Assert.That(o.DistanceTo(Vector3.zero), Is.EqualTo(0f));
            Assert.That(o.DistanceTo(new Vector3(0f, 0f, 1.0f)), Is.EqualTo(0.5f).Within(1e-4f));
        }

        [Test]
        public void Ttc_is_finite_and_non_increasing_while_a_limb_closes()
        {
            var obstacles = new List<Obstacle>
            {
                new Obstacle("o", new Vector3(0f, 1.0f, 2.0f), new Vector3(0.15f, 1.0f, 0.15f)),
            };
            var limbs = new List<Joint> { Joint.RightHand };
            var oracle = new CollisionOracle(obstacles, new OracleParams());

            // Aligned head-on approach (x = 0) at 2 m/s.
            var frames = SyntheticTrajectory.LinearApproach(
                Joint.RightHand,
                start: new Vector3(0f, 1.0f, 0.0f),
                velocity: new Vector3(0f, 0f, 2.0f),
                seconds: 0.7f,
                dt: 1f / 90f);

            float prevTtc = float.PositiveInfinity;
            int closingReadings = 0;

            foreach (var f in frames)
            {
                oracle.UpdateVelocities(f, limbs);
                RiskReading r = oracle.Read(Joint.RightHand, f);

                if (r.Closing && !float.IsInfinity(r.MinTtc))
                {
                    closingReadings++;
                    // Allow a small tolerance for the EMA velocity ramp-in transient.
                    Assert.That(r.MinTtc, Is.LessThanOrEqualTo(prevTtc + 0.05f));
                    prevTtc = r.MinTtc;
                }
            }

            Assert.That(closingReadings, Is.GreaterThan(0));
            Assert.That(prevTtc, Is.LessThan(0.5f)); // ends up well inside the predictive horizon
        }

        [Test]
        public void A_static_limb_never_reports_closing()
        {
            var obstacles = new List<Obstacle>
            {
                new Obstacle("o", new Vector3(0f, 1.0f, 0.30f), new Vector3(0.15f, 1.0f, 0.15f)),
            };
            var limbs = new List<Joint> { Joint.RightHand };
            var oracle = new CollisionOracle(obstacles, new OracleParams());

            // Hand sits still right next to the obstacle (within reactive distance) but is NOT closing.
            var frames = SyntheticTrajectory.LinearApproach(
                Joint.RightHand,
                start: new Vector3(0f, 1.0f, 0.0f),
                velocity: Vector3.zero,
                seconds: 0.5f,
                dt: 1f / 90f);

            bool everClosing = false;
            foreach (var f in frames)
            {
                oracle.UpdateVelocities(f, limbs);
                if (oracle.Read(Joint.RightHand, f).Closing) everClosing = true;
            }

            Assert.That(everClosing, Is.False);
        }
    }
}
