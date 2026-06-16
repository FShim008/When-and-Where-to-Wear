using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CollisionFeedback.Core;
using Joint = CollisionFeedback.Core.Joint; // disambiguate from UnityEngine.Joint (physics component)

namespace CollisionFeedback.Tests
{
    public class TrackingQualityMonitorTests
    {
        private static readonly List<Joint> All = new()
        {
            Joint.Head, Joint.Chest, Joint.LeftHand, Joint.RightHand, Joint.LeftFoot, Joint.RightFoot,
        };

        // A frame with every joint at the same position (so individual joints can then be perturbed).
        private static PoseFrame Uniform(double t, Vector3 p)
        {
            var j = new Vector3[JointInfo.Count];
            for (int i = 0; i < j.Length; i++) j[i] = p;
            return new PoseFrame { Timestamp = t, Joints = j };
        }

        private static JointTrackingStats Find(TrackingQualityReport r, Joint j)
        {
            foreach (var s in r.Joints) if (s.Joint == j) return s;
            return default;
        }

        [Test]
        public void Steady_delivery_meets_the_rate_bar_and_passes()
        {
            var m = new TrackingQualityMonitor(All);
            // 60 frames at 30 Hz; nudge every joint each frame so nothing reads as frozen.
            for (int i = 0; i < 60; i++)
            {
                double t = i / 30.0;
                m.Observe(Uniform(t, new Vector3(0f, 1f + i * 0.001f, 0f)), wallTime: t);
            }
            var r = m.Report(59 / 30.0);

            Assert.That(r.DeliveryRateHz, Is.EqualTo(30f).Within(1.5f));
            Assert.That(r.MaxFrameGapSeconds, Is.LessThan(0.05f));
            Assert.That(r.Pass, Is.True);
        }

        [Test]
        public void A_delivery_gap_is_flagged_and_fails()
        {
            var m = new TrackingQualityMonitor(All);
            m.Observe(Uniform(0.0, new Vector3(0f, 1f, 0f)), 0.0);
            m.Observe(Uniform(0.5, new Vector3(0f, 1.01f, 0f)), 0.5); // 0.5 s wall gap
            var r = m.Report(0.5);

            Assert.That(r.MaxFrameGapSeconds, Is.GreaterThanOrEqualTo(0.5f));
            Assert.That(r.Pass, Is.False);
        }

        [Test]
        public void A_frozen_joint_is_caught_while_the_others_move()
        {
            var m = new TrackingQualityMonitor(All);
            for (int i = 0; i < 30; i++)
            {
                double t = i / 30.0;
                var j = new Vector3[JointInfo.Count];
                for (int k = 0; k < j.Length; k++) j[k] = new Vector3(0f, 1f + i * 0.01f, 0f); // everything climbs
                j[(int)Joint.LeftFoot] = new Vector3(0f, 0.05f, 0f);                            // ...except LeftFoot
                m.Observe(new PoseFrame { Timestamp = t, Joints = j }, t);
            }
            var r = m.Report(29 / 30.0);

            Assert.That(Find(r, Joint.LeftFoot).MaxFreezeSeconds, Is.GreaterThan(0.25f));
            Assert.That(Find(r, Joint.LeftFoot).Pass, Is.False);
            Assert.That(Find(r, Joint.LeftHand).Pass, Is.True);
        }

        [Test]
        public void Jitter_is_the_small_noise_floor_of_a_still_joint()
        {
            var m = new TrackingQualityMonitor(All);
            var rng = new System.Random(1234);
            for (int i = 0; i < 100; i++)
            {
                double t = i / 50.0;
                var j = new Vector3[JointInfo.Count];
                // Everything dithers a hair so no joint is "frozen".
                for (int k = 0; k < j.Length; k++) j[k] = new Vector3(i % 2 == 0 ? 0.0005f : -0.0005f, 1f, 0f);
                float dither = (float)(rng.NextDouble() - 0.5) * 0.004f;         // +/- 2 mm noise on RightHand
                j[(int)Joint.RightHand] = new Vector3(0.2f + dither, 1f, 0f);
                m.Observe(new PoseFrame { Timestamp = t, Joints = j }, t);
            }
            var r = m.Report(99 / 50.0);

            float jitter = Find(r, Joint.RightHand).JitterMeters;
            Assert.That(jitter, Is.GreaterThan(0f));
            Assert.That(jitter, Is.LessThan(0.01f)); // under the 1 cm bar
        }

        [Test]
        public void A_teleport_jump_is_counted()
        {
            var m = new TrackingQualityMonitor(All);
            m.Observe(Uniform(0.0, new Vector3(0f, 1f, 0f)), 0.0);

            var j = new Vector3[JointInfo.Count];
            for (int k = 0; k < j.Length; k++) j[k] = new Vector3(0f, 1.01f, 0f);
            j[(int)Joint.RightHand] = new Vector3(1.2f, 1f, 0f); // 1.2 m in one frame
            m.Observe(new PoseFrame { Timestamp = 1 / 30.0, Joints = j }, 1 / 30.0);
            var r = m.Report(1 / 30.0);

            Assert.That(Find(r, Joint.RightHand).Jumps, Is.EqualTo(1));
            Assert.That(Find(r, Joint.RightHand).Pass, Is.False);
        }

        [Test]
        public void NaN_positions_are_counted_invalid_and_fail()
        {
            var m = new TrackingQualityMonitor(All);
            var j = new Vector3[JointInfo.Count];
            for (int k = 0; k < j.Length; k++) j[k] = new Vector3(0f, 1f, 0f);
            j[(int)Joint.LeftHand] = new Vector3(float.NaN, float.NaN, float.NaN);
            m.Observe(new PoseFrame { Timestamp = 0.0, Joints = j }, 0.0);
            var r = m.Report(0.0);

            Assert.That(Find(r, Joint.LeftHand).InvalidSamples, Is.EqualTo(1));
            Assert.That(Find(r, Joint.LeftHand).Pass, Is.False);
        }
    }
}
