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

    /// <summary>
    /// The experimental conditions. The STUDY runs six — <see cref="SessionPlan"/> is the single authority
    /// on what is scheduled. <see cref="Visual"/> stays implemented but is NOT in the Paper 1 protocol.
    /// </summary>
    public enum Condition
    {
        None,    // no-feedback floor (anchors absolute collision reduction)
        RG,      // Reactive, Generic (chest cue)
        RB,      // Reactive, Body-localized
        PG,      // Predictive, Generic (chest cue)
        PB,      // Predictive, Body-localized, DISCRETE cue (full technique)
        Visual,  // predictive, localized visual benchmark. IMPLEMENTED BUT NOT SCHEDULED — see PBC.

        /// <summary>
        /// Predictive, Body-localized, **CONTINUOUS** cue. Same trigger, same site, same information as
        /// <see cref="PB"/>; only the CUE FORM differs — intensity ramps with TTC instead of firing once.
        ///
        /// WHY THIS EXISTS [PAPER1_STUDY_DESIGN §3; docs/PAPER1_PARAMETER_JUSTIFICATION.md §5B].
        /// Valkov and Linsen (IEEE VR 2019) compared fixed-distance against speed-scaled triggering of
        /// vibrotactile collision warning and found the speed-scaled policy produced SIGNIFICANTLY MORE
        /// collisions (Z = -2.23, p = 0.024, N = 40). Their explanation was not the trigger but the
        /// mapping: intensity was a continuous function of distance and speed, so slowing down reduced the
        /// vibration and participants "continued walking slowly forward while constantly decreasing the
        /// speed to adjust the vibration level." Trigger policy and cue form were confounded.
        ///
        /// PB vs PBC isolates the half of that confound H1 cannot reach:
        ///   H1  varies the TRIGGER,  cue form held constant  -> was it the timing?
        ///   H4' varies the CUE FORM, trigger held constant   -> was it the mapping?
        ///
        /// PBC deliberately RE-CREATES the feedback loop: since TTC = d/v, slowing raises TTC and therefore
        /// lowers intensity, exactly as in the original. That loop is the thing under test — do not "fix" it.
        /// </summary>
        PBC,
    }

    /// <summary>
    /// How a cue is delivered over time — the variable H4' manipulates with the trigger held constant.
    /// <see cref="Discrete"/> is edge-triggered: one pulse train per approach, fixed intensity, nothing the
    /// participant does changes it. <see cref="Continuous"/> is emitted every frame while engaged with
    /// intensity a function of current TTC, so the participant CAN modulate it by changing speed.
    /// </summary>
    public enum CueForm
    {
        Discrete,
        Continuous,
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
