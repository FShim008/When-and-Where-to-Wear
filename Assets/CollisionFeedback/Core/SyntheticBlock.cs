using System.Collections.Generic;
using UnityEngine;

namespace CollisionFeedback.Core
{
    /// <summary>
    /// Builds a small, fully synthetic demo "block" (obstacles + limbs + opportunity schedule + a frame
    /// stream of scripted limb approaches) so the WHOLE pipeline can run end-to-end with no hardware and
    /// emit a real CSV. Each approach moves one limb neutral -> target -> neutral over its window.
    /// </summary>
    public static class SyntheticBlock
    {
        public readonly struct Approach
        {
            public readonly Joint Limb;
            public readonly string ObstacleId;
            public readonly Vector3 Target;
            public readonly double Onset;
            public readonly double Duration;

            public Approach(Joint limb, string obstacleId, Vector3 target, double onset, double duration)
            {
                Limb = limb; ObstacleId = obstacleId; Target = target; Onset = onset; Duration = duration;
            }
        }

        public sealed class Data
        {
            public List<Obstacle> Obstacles;
            public List<Joint> Limbs;
            public List<Opportunity> Schedule;
            public List<PoseFrame> Frames;
        }

        public static Data Demo()
        {
            var obstacles = new List<Obstacle>
            {
                new Obstacle("O1_low",   new Vector3(0.0f, 0.40f, 1.20f), new Vector3(0.25f, 0.40f, 0.25f)),
                new Obstacle("O2_hand",  new Vector3(0.40f, 1.00f, 1.20f), new Vector3(0.20f, 0.50f, 0.20f)),
                new Obstacle("O3_torso", new Vector3(-0.40f, 1.30f, 1.20f), new Vector3(0.25f, 0.50f, 0.20f)),
            };

            var limbs = new List<Joint> { Joint.LeftHand, Joint.RightHand, Joint.LeftFoot, Joint.RightFoot };

            var approaches = new List<Approach>
            {
                // Right hand drives INTO O2 -> a collision.
                new Approach(Joint.RightHand, "O2_hand",  new Vector3(0.40f, 1.00f, 1.20f), onset: 1.0, duration: 1.5),
                // Left hand reaches toward O3 but stops just inside the near-miss band -> near-miss / avoidance.
                new Approach(Joint.LeftHand,  "O3_torso", new Vector3(-0.35f, 1.30f, 0.95f), onset: 3.5, duration: 1.5),
                // Right foot toward O1, also stops within the near-miss band -> near-miss / avoidance.
                new Approach(Joint.RightFoot, "O1_low",   new Vector3(0.00f, 0.40f, 0.90f), onset: 6.0, duration: 1.5),
            };

            var schedule = new List<Opportunity>();
            foreach (var a in approaches)
                schedule.Add(new Opportunity($"OP_{a.Limb}", a.Onset, a.Duration, a.Limb, a.ObstacleId));

            const double dt = 1.0 / 30.0;
            const double blockSeconds = 8.0;
            var frames = new List<PoseFrame>();
            int n = (int)(blockSeconds / dt);
            for (int i = 0; i <= n; i++)
            {
                double t = i * dt;
                Vector3[] j = SyntheticTrajectory.NeutralPose();
                foreach (var a in approaches)
                {
                    if (t >= a.Onset && t <= a.Onset + a.Duration)
                    {
                        double u = (t - a.Onset) / a.Duration;            // 0..1 across the window
                        float s = (float)(u <= 0.5 ? u * 2.0 : (1.0 - u) * 2.0); // triangle 0 -> 1 -> 0
                        j[(int)a.Limb] = Vector3.Lerp(j[(int)a.Limb], a.Target, s);
                    }
                }
                frames.Add(new PoseFrame { Timestamp = t, Joints = j });
            }

            return new Data { Obstacles = obstacles, Limbs = limbs, Schedule = schedule, Frames = frames };
        }
    }
}
