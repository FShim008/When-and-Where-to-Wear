namespace CollisionFeedback.Core
{
    /// <summary>
    /// Tracked body joints streamed by the keypoint source.
    /// Enum values double as indices into <see cref="PoseFrame.Joints"/>.
    /// </summary>
    public enum Joint
    {
        Head = 0,
        Chest = 1,
        LeftHand = 2,
        RightHand = 3,
        LeftFoot = 4,
        RightFoot = 5,
    }

    /// <summary>
    /// Physical tactor sites we own: TactSuit X40 (chest) + 2× Tactosy for Hands (back of the hand)
    /// + 2× Tactosy for Feet (shins). 5 sites, 0 extra hardware.
    /// </summary>
    public enum HapticSite
    {
        Chest = 0,
        LeftHand = 1,
        RightHand = 2,
        LeftShin = 3,
        RightShin = 4,
    }

    /// <summary>The 6 experimental conditions (StudyDesign section 5).</summary>
    public enum Condition
    {
        None,    // no-feedback floor (anchors absolute collision reduction)
        RG,      // Reactive, Generic (chest cue)
        RB,      // Reactive, Body-localized
        PG,      // Predictive, Generic (chest cue)
        PB,      // Predictive, Body-localized (full technique)
        Visual,  // best-practice visual reference (reactive, localized, detection-matched)
    }

    /// <summary>Feedback channel a condition emits on.</summary>
    public enum Modality
    {
        Haptic,
        Visual,
    }

    /// <summary>Which timing rule fired an alert (for logging / manipulation checks).</summary>
    public enum TriggerKind
    {
        Reactive,
        Predictive,
    }

    /// <summary>Shared joint metadata.</summary>
    public static class JointInfo
    {
        /// <summary>Number of <see cref="Joint"/> values; length of a <see cref="PoseFrame.Joints"/> array.</summary>
        public const int Count = 6;
    }
}
