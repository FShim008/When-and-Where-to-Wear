using UnityEngine;

namespace CollisionFeedback.Runtime
{
    /// <summary>
    /// Scene wiring for the VIVE Ultimate Tracker body rig: drag each tracked Transform into its slot — the
    /// XR camera for the head, and one Ultimate Tracker each for chest, wrists, ankles. The session drivers
    /// (LiveSessionController / SessionRunner) and the M4 TrackingBench find this and build a
    /// <see cref="TrackerKeypointSource"/> from it. All six must be assigned (<see cref="IsComplete"/>).
    /// </summary>
    public sealed class BodyTrackerRig : MonoBehaviour
    {
        [Tooltip("HMD / XR camera Transform.")]      public Transform head;
        [Tooltip("Chest Ultimate Tracker.")]         public Transform chest;
        [Tooltip("Left-wrist Ultimate Tracker.")]    public Transform leftHand;
        [Tooltip("Right-wrist Ultimate Tracker.")]   public Transform rightHand;
        [Tooltip("Left-ankle Ultimate Tracker.")]    public Transform leftFoot;
        [Tooltip("Right-ankle Ultimate Tracker.")]   public Transform rightFoot;

        public bool IsComplete => head && chest && leftHand && rightHand && leftFoot && rightFoot;

        public TrackerKeypointSource CreateSource() =>
            new TrackerKeypointSource(head, chest, leftHand, rightHand, leftFoot, rightFoot);
    }
}
