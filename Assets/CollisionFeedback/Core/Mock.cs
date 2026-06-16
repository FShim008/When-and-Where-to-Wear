using System.Collections.Generic;
using UnityEngine;

namespace CollisionFeedback.Core
{
    /// <summary>
    /// Replays a precomputed list of <see cref="PoseFrame"/>s. The key decoupling tool: it lets the
    /// whole brain (oracle -> conditions -> routing -> logging) run and be tested with NO cameras,
    /// NO headset, NO bHaptics device. Swap for the real UDP/LSL source behind <see cref="IKeypointSource"/>.
    /// </summary>
    public sealed class MockKeypointSource : IKeypointSource
    {
        private readonly IReadOnlyList<PoseFrame> _frames;
        private int _i;

        public MockKeypointSource(IReadOnlyList<PoseFrame> frames) { _frames = frames; }

        public bool TryGetFrame(out PoseFrame frame)
        {
            if (_i < _frames.Count)
            {
                frame = _frames[_i++];
                return true;
            }
            frame = default;
            return false;
        }

        public void Reset() => _i = 0;
    }

    /// <summary>Builds synthetic body-tracking trajectories for testing the oracle/conditions without hardware.</summary>
    public static class SyntheticTrajectory
    {
        /// <summary>A neutral standing pose (rough adult proportions, facing +Z).</summary>
        public static Vector3[] NeutralPose()
        {
            var j = new Vector3[JointInfo.Count];
            j[(int)Joint.Head]      = new Vector3(0f, 1.70f, 0f);
            j[(int)Joint.Chest]     = new Vector3(0f, 1.30f, 0f);
            j[(int)Joint.LeftHand]  = new Vector3(-0.20f, 1.00f, 0f);
            j[(int)Joint.RightHand] = new Vector3(0.20f, 1.00f, 0f);
            j[(int)Joint.LeftFoot]  = new Vector3(-0.15f, 0.05f, 0f);
            j[(int)Joint.RightFoot] = new Vector3(0.15f, 0.05f, 0f);
            return j;
        }

        /// <summary>One limb moves in a straight line from <paramref name="start"/> at constant
        /// <paramref name="velocity"/>; all other joints hold the neutral pose.</summary>
        public static List<PoseFrame> LinearApproach(Joint limb, Vector3 start, Vector3 velocity,
                                                     float seconds, float dt)
        {
            var frames = new List<PoseFrame>();
            int n = Mathf.Max(1, Mathf.RoundToInt(seconds / dt));
            for (int i = 0; i <= n; i++)
            {
                float t = i * dt;
                var j = NeutralPose();
                j[(int)limb] = start + velocity * t;
                frames.Add(new PoseFrame { Timestamp = t, Joints = j });
            }
            return frames;
        }
    }
}
