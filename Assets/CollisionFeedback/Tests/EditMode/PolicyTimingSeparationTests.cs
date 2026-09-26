using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CollisionFeedback.Core;
using Joint = CollisionFeedback.Core.Joint; // disambiguate from UnityEngine.Joint (physics component)

namespace CollisionFeedback.Tests
{
    /// <summary>
    /// Guards the H1 timing manipulation [PAPER1_STUDY_DESIGN §6, gate §14.27].
    ///
    /// Proximity fires at a fixed DISTANCE D; predictive fires at a fixed TIME T, i.e. at distance v·T.
    /// So predictive leads proximity only while v > D/T, and — more restrictively — only when the limb has
    /// enough clear approach distance that the proximity cue is not already firing at movement onset.
    /// If that fails, the "predictive" condition delivers LESS lead time than the "proximity" condition and
    /// H1 silently averages over opportunities whose manipulation ran in opposite directions.
    ///
    /// These tests pin the ordering at the documented operating point, and pin the dead zone that motivates
    /// the opportunity-geometry requirement, so neither can regress unnoticed.
    /// </summary>
    public class PolicyTimingSeparationTests
    {
        private const float Hz = 90f;
        private const float Dt = 1f / Hz;
        private static readonly Joint Limb = Joint.RightHand;
        private static readonly List<Joint> Limbs = new() { Limb };

        private static List<Obstacle> Hazard() => new()
        { new Obstacle("O1", new Vector3(0f, 1.0f, 3.0f), new Vector3(0.2f, 0.2f, 0.2f)) };

        // Minimum-jerk displacement fraction — the standard model of a human reach: slow, fast, slow.
        private static float MinJerk(float u) => 10f * u * u * u - 15f * u * u * u * u + 6f * u * u * u * u * u;

        /// <summary>
        /// Runs one minimum-jerk reach that starts <paramref name="approach"/> m clear of the hazard surface
        /// and ends at it, and returns the time the given condition first fired (NaN if it never did).
        /// </summary>
        private static double FirstFireTime(Condition condition, OracleParams p, float approach, float movementTime)
        {
            var sink = new RecordingSink();
            var cm = new ConditionManager(condition, p, Hazard(), Limbs, sink);
            Obstacle obs = Hazard()[0];
            float startZ = obs.Center.z - obs.HalfExtents.z - approach;

            PoseFrame frame = PoseFrame.Create(0);
            int n = Math.Max(8, (int)(movementTime * Hz));
            for (int i = 0; i <= n; i++)
            {
                double t = i * Dt;
                frame.Timestamp = t;
                frame.Joints[(int)Limb] = new Vector3(0f, 1.0f, startZ + approach * MinJerk((float)i / n));
                cm.Tick(frame);
                if (sink.Fired.Count > 0) return t;
            }
            return double.NaN;
        }

        private static readonly float[] MovementTimes = { 0.4f, 0.6f, 0.9f, 1.4f };

        [Test]
        public void Predictive_leads_proximity_at_the_documented_minimum_approach_distance()
        {
            var p = new OracleParams();   // the frozen operating point
            float approach = OracleParams.MinApproachDistanceForValidTiming;

            foreach (float mt in MovementTimes)
            {
                double reactive = FirstFireTime(Condition.RB, p, approach, mt);
                double predictive = FirstFireTime(Condition.PB, p, approach, mt);

                Assert.That(reactive, Is.Not.NaN, $"proximity never fired (movement time {mt}s)");
                Assert.That(predictive, Is.Not.NaN, $"predictive never fired (movement time {mt}s)");
                Assert.That(reactive - predictive, Is.GreaterThan(0.05),
                    $"predictive must lead proximity by >50 ms at the documented minimum approach " +
                    $"distance ({approach} m), movement time {mt}s. If this fails, D/T or " +
                    $"{nameof(OracleParams.MinApproachDistanceForValidTiming)} are out of step.");
            }
        }

        [Test]
        public void Lead_grows_with_approach_distance_at_the_recommended_geometry()
        {
            var p = new OracleParams();
            foreach (float mt in MovementTimes)
            {
                double nearLead = FirstFireTime(Condition.RB, p, OracleParams.MinApproachDistanceForValidTiming, mt)
                                - FirstFireTime(Condition.PB, p, OracleParams.MinApproachDistanceForValidTiming, mt);
                double farLead = FirstFireTime(Condition.RB, p, OracleParams.RecommendedApproachDistance, mt)
                               - FirstFireTime(Condition.PB, p, OracleParams.RecommendedApproachDistance, mt);

                Assert.That(farLead, Is.GreaterThan(nearLead),
                    $"the recommended approach distance should buy more lead than the floor (mt {mt}s)");
            }
        }

        [Test]
        public void Timing_manipulation_INVERTS_when_the_limb_starts_inside_the_proximity_boundary()
        {
            // Documents the failure mode rather than hiding it: with less clear approach than D, the
            // proximity cue fires at movement onset and predictive cannot possibly lead it. This is why
            // opportunity geometry is audited (gate §14.27) and not left to the parameters alone.
            var p = new OracleParams();
            float tooClose = p.ReactiveDistance * 0.6f;

            double reactive = FirstFireTime(Condition.RB, p, tooClose, 0.9f);
            double predictive = FirstFireTime(Condition.PB, p, tooClose, 0.9f);

            Assert.That(reactive, Is.Not.NaN);
            Assert.That(predictive, Is.Not.NaN);
            Assert.That(reactive - predictive, Is.LessThan(0d),
                "expected the known inversion when the limb starts inside D — if this now passes, the " +
                "geometry requirement may have been solved another way and this test should be revisited");
        }

        [Test]
        public void Crossover_speed_is_D_over_T_and_stays_below_normal_limb_speeds()
        {
            var p = new OracleParams();
            float crossover = p.ReactiveDistance / p.PredictiveTtc;

            // Must sit below the closing speeds this task actually produces, or the factor inverts on
            // ordinary movements. 0.50 m/s is the documented ceiling for an acceptable crossover.
            Assert.That(crossover, Is.LessThanOrEqualTo(0.50f),
                $"D/T = {crossover:F2} m/s puts the policy crossover inside the range of real limb speeds; " +
                "below that speed the predictive cue fires LATER than the proximity cue (§6).");
        }

        [Test]
        public void Predictive_release_margin_stays_proportional_to_T()
        {
            // Hysteresis is on the TTC axis for the predictive policy, so the margin has to scale with T.
            // Left at an absolute value after T changed, the cue would re-arm at a different effective point.
            var p = new OracleParams();
            Assert.That(p.PredictiveReleaseMargin / p.PredictiveTtc, Is.EqualTo(0.40f).Within(0.05f),
                "predictive release margin should stay ~40% of T");
        }
    }
}
