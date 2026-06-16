using System.Collections.Generic;
using NUnit.Framework;
using CollisionFeedback.Core;
using Joint = CollisionFeedback.Core.Joint; // disambiguate from UnityEngine.Joint (physics component)

namespace CollisionFeedback.Tests
{
    public class OpportunitySchedulerTests
    {
        private static List<Opportunity> ThreeEvents() => new()
        {
            new Opportunity("OP01", 1.0, 0.5, Joint.RightHand, "O2"),
            new Opportunity("OP02", 2.0, 0.5, Joint.LeftFoot,  "O1"),
            new Opportunity("OP03", 3.0, 0.5, Joint.Chest,     "O3"),
        };

        private static void RunTo(OpportunityScheduler s, double end, double step = 1.0 / 30.0)
        {
            for (double t = 0; t <= end + 1e-9; t += step) s.Tick(t);
        }

        [Test]
        public void Total_is_the_fixed_denominator_regardless_of_ticking()
        {
            var s = new OpportunityScheduler(ThreeEvents());
            Assert.That(s.Total, Is.EqualTo(3));
            RunTo(s, 5.0);
            Assert.That(s.Total, Is.EqualTo(3));
        }

        [Test]
        public void Every_opportunity_opens_then_closes_exactly_once()
        {
            var s = new OpportunityScheduler(ThreeEvents());
            RunTo(s, 5.0);
            Assert.That(s.Opened, Is.EqualTo(3));
            Assert.That(s.Closed, Is.EqualTo(3));
        }

        [Test]
        public void Pending_before_onset_open_during_window_closed_after()
        {
            var s = new OpportunityScheduler(ThreeEvents());

            s.Tick(0.5); // before OP01 onset (1.0)
            Assert.That(s.ActiveFor(Joint.RightHand).HasValue, Is.False);

            s.Tick(1.2); // inside OP01 window [1.0, 1.5]
            var open = s.ActiveFor(Joint.RightHand);
            Assert.That(open.HasValue, Is.True);
            Assert.That(open.Value.Id, Is.EqualTo("OP01"));

            s.Tick(1.6); // past OP01 close (1.5)
            Assert.That(s.ActiveFor(Joint.RightHand).HasValue, Is.False);
        }

        [Test]
        public void ActiveFor_only_matches_the_target_limb()
        {
            var s = new OpportunityScheduler(ThreeEvents());
            s.Tick(1.2); // OP01 open, targets RightHand
            Assert.That(s.ActiveFor(Joint.RightHand).HasValue, Is.True);
            Assert.That(s.ActiveFor(Joint.LeftHand).HasValue, Is.False);
        }

        [Test]
        public void EvenlySpaced_builds_the_requested_count_inside_the_block()
        {
            var targets = new List<(Joint, string)>
            {
                (Joint.RightHand, "O2"), (Joint.LeftHand, "O2"),
                (Joint.RightFoot, "O1"), (Joint.LeftFoot, "O1"),
            };
            var schedule = OpportunitySchedules.EvenlySpaced(12, blockSeconds: 180, windowSeconds: 2.5, targets);

            Assert.That(schedule.Count, Is.EqualTo(12));
            Assert.That(schedule[0].OnsetTime, Is.GreaterThan(0.0));
            Assert.That(schedule[schedule.Count - 1].CloseTime, Is.LessThan(180.0));
        }

        [Test]
        public void Layout1_is_the_storyboard_twelve_event_schedule()
        {
            var s = OpportunitySchedules.Layout1();
            Assert.That(s.Count, Is.EqualTo(12));

            // Onsets strictly increasing and the whole schedule fits inside the 180 s block.
            for (int i = 1; i < s.Count; i++)
                Assert.That(s[i].OnsetTime, Is.GreaterThan(s[i - 1].OnsetTime));
            Assert.That(s[s.Count - 1].CloseTime, Is.LessThan(180.0));

            // Obstacle balance per the storyboard: O2×4 · O3×4 · O1×2 · boundary×2.
            Assert.That(s.FindAll(o => o.TargetObstacleId == "O2").Count, Is.EqualTo(4));
            Assert.That(s.FindAll(o => o.TargetObstacleId == "O3").Count, Is.EqualTo(4));
            Assert.That(s.FindAll(o => o.TargetObstacleId == "O1").Count, Is.EqualTo(2));
            Assert.That(s.FindAll(o => o.TargetObstacleId == "BOUNDARY").Count, Is.EqualTo(2));

            // Limb balance: 4 right-arm, 4 left-arm, 1 each foot, 2 chest.
            Assert.That(s.FindAll(o => o.TargetLimb == Joint.RightHand).Count, Is.EqualTo(4));
            Assert.That(s.FindAll(o => o.TargetLimb == Joint.LeftHand).Count, Is.EqualTo(4));
            Assert.That(s.FindAll(o => o.TargetLimb == Joint.RightFoot).Count, Is.EqualTo(1));
            Assert.That(s.FindAll(o => o.TargetLimb == Joint.LeftFoot).Count, Is.EqualTo(1));
            Assert.That(s.FindAll(o => o.TargetLimb == Joint.Chest).Count, Is.EqualTo(2));
        }
    }
}
