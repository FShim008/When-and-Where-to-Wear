using UnityEngine;
using CollisionFeedback.Core;
using Joint = CollisionFeedback.Core.Joint; // disambiguate from UnityEngine.Joint (physics component)

namespace CollisionFeedback.Runtime
{
    /// <summary>
    /// Real keypoint source backed by VIVE Ultimate Trackers (+ the HMD for the head), read directly in the
    /// VR world frame. Samples the assigned Transforms once per Unity frame and emits a PoseFrame — no network,
    /// no camera→VR calibration (the trackers share the headset's SteamVR tracking space). Drop-in for the old
    /// UDP source behind <see cref="IKeypointSource"/>. It only reads world positions, so it doesn't care how
    /// each Transform is driven (an OpenXR TrackedPoseDriver bound to the tracker's role, a SteamVR pose
    /// component, etc.).
    /// </summary>
    public sealed class TrackerKeypointSource : IKeypointSource
    {
        private readonly Transform[] _joints  = new Transform[JointInfo.Count]; // indexed by (int)Joint
        private readonly Vector3[]   _scratch = new Vector3[JointInfo.Count];
        private int _lastFrame = -1;

        public TrackerKeypointSource(Transform head, Transform chest, Transform leftHand,
                                     Transform rightHand, Transform leftFoot, Transform rightFoot)
        {
            _joints[(int)Joint.Head]      = head;
            _joints[(int)Joint.Chest]     = chest;
            _joints[(int)Joint.LeftHand]  = leftHand;
            _joints[(int)Joint.RightHand] = rightHand;
            _joints[(int)Joint.LeftFoot]  = leftFoot;
            _joints[(int)Joint.RightFoot] = rightFoot;
        }

        // One fresh sample per Unity frame; the driver drains with a while-loop, so the 2nd call returns false.
        public bool TryGetFrame(out PoseFrame frame)
        {
            frame = default;
            if (Time.frameCount == _lastFrame) return false;
            _lastFrame = Time.frameCount;

            for (int j = 0; j < _scratch.Length; j++)
                _scratch[j] = _joints[j] != null ? _joints[j].position : Vector3.zero; // world space == VR frame
            frame = new PoseFrame { Timestamp = Time.unscaledTimeAsDouble, Joints = (Vector3[])_scratch.Clone() };
            return true;
        }
    }
}
