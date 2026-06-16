using System.Collections.Generic;
using NUnit.Framework;
using CollisionFeedback.Core;
using Joint = CollisionFeedback.Core.Joint; // disambiguate from UnityEngine.Joint (physics component)

namespace CollisionFeedback.Tests
{
    public class AvoidanceLatencyDetectorTests
    {
        private static Dictionary<Joint, float> D(Joint j, float d) => new() { { j, d } };

        [Test]
        public void Latency_is_time_from_alert_to_the_deceleration_onset()
        {
            var det = new AvoidanceLatencyDetector();
            det.RegisterAlert(t: 1.0, atRiskLimb: Joint.RightHand, anyLimb: false);

            // Approach speed rises to a distinct peak of 1.0 m/s at t=1.2 (0.6 -> 1.0 -> 0.4 m/s), then the
            // limb brakes -> avoidance onset is the peak-approach time t=1.2 (deceleration begins there).
            var series = new (double t, float d)[]
            {
                (1.0, 0.40f), (1.1, 0.34f), (1.2, 0.24f), (1.3, 0.20f),
            };
            foreach (var (t, d) in series) det.Tick(t, D(Joint.RightHand, d));

            Assert.That(det.Events.Count, Is.EqualTo(1));
            Assert.That(det.Events[0].AvoidanceTime, Is.EqualTo(1.2).Within(1e-9));
            Assert.That(det.Events[0].LatencySeconds, Is.EqualTo(0.2).Within(1e-9));
        }

        [Test]
        public void Slows_but_keeps_closing_still_counts_as_avoidance()
        {
            var det = new AvoidanceLatencyDetector();
            det.RegisterAlert(0.0, Joint.RightHand, anyLimb: false);

            // Distance strictly decreases the whole time (the limb never turns around), but the approach
            // brakes from a distinct 1.0 m/s peak (t=0.2) to 0.3 m/s -> a slowdown a distance-minimum test
            // would miss. The single peak avoids any plateau tie, so the onset time is unambiguous.
            var series = new (double t, float d)[]
            {
                (0.0, 0.40f), (0.1, 0.34f), (0.2, 0.24f), (0.3, 0.21f), (0.4, 0.19f),
            };
            foreach (var (t, d) in series) det.Tick(t, D(Joint.RightHand, d));

            Assert.That(det.Events.Count, Is.EqualTo(1));
            Assert.That(det.Events[0].AvoidanceTime, Is.EqualTo(0.2).Within(1e-9)); // peak-approach time
        }

        [Test]
        public void No_event_if_the_limb_keeps_approaching_at_full_speed()
        {
            var det = new AvoidanceLatencyDetector();
            det.RegisterAlert(0.0, Joint.RightHand, anyLimb: false);

            var dists = new[] { 0.30f, 0.22f, 0.14f, 0.07f, 0.01f };
            for (int i = 0; i < dists.Length; i++) det.Tick(i * 0.1, D(Joint.RightHand, dists[i]));

            Assert.That(det.Events, Is.Empty);
        }

        [Test]
        public void Generic_cue_takes_the_first_limb_to_decelerate()
        {
            var det = new AvoidanceLatencyDetector();
            det.RegisterAlert(0.0, atRiskLimb: Joint.RightHand, anyLimb: true);

            // LeftFoot peaks at 0.8 m/s (t=0.1) then brakes (detected at t=0.2); RightHand only creeps at
            // 0.2 m/s and never decelerates -> LeftFoot is the first limb to show avoidance.
            var frames = new (double t, float rh, float lf)[]
            {
                (0.0, 0.30f, 0.20f),
                (0.1, 0.28f, 0.12f),
                (0.2, 0.26f, 0.10f),
                (0.3, 0.24f, 0.18f),
            };
            foreach (var (t, rh, lf) in frames)
                det.Tick(t, new Dictionary<Joint, float> { { Joint.RightHand, rh }, { Joint.LeftFoot, lf } });

            Assert.That(det.Events.Count, Is.EqualTo(1));
            Assert.That(det.Events[0].Limb, Is.EqualTo(Joint.LeftFoot));
            Assert.That(det.Events[0].AvoidanceTime, Is.EqualTo(0.1).Within(1e-9));
        }
    }
}
