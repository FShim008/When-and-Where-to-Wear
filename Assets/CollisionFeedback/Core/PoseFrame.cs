using UnityEngine;

namespace CollisionFeedback.Core
{
    /// <summary>
    /// One timestamped frame of 3D joint positions, expressed in the VR world frame
    /// (i.e. AFTER the camera-rig to OpenXR calibration has been applied).
    /// </summary>
    public struct PoseFrame
    {
        /// <summary>Seconds on the data clock (monotonic; supplied by the tracking source).</summary>
        public double Timestamp;

        /// <summary>Joint positions indexed by <c>(int)</c><see cref="Joint"/>; length == <see cref="JointInfo.Count"/>.</summary>
        public Vector3[] Joints;

        public readonly Vector3 Get(Joint j) => Joints[(int)j];

        /// <summary>Allocates an empty frame with a zeroed joint array.</summary>
        public static PoseFrame Create(double timestamp)
        {
            return new PoseFrame { Timestamp = timestamp, Joints = new Vector3[JointInfo.Count] };
        }
    }
}
