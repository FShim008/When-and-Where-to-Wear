namespace CollisionFeedback.Core
{
    /// <summary>
    /// One feedback event a condition decided to emit. The sink (bHaptics, in-HMD visual, or a test
    /// recorder) decides how to realize it.
    ///
    /// Across the DISCRETE haptic conditions the SIGNAL is identical — only <see cref="Site"/>,
    /// <see cref="Modality"/> and timing differ. That is the design: we manipulate WHEN and WHERE, never
    /// the cue itself.
    ///
    /// <see cref="Condition.PBC"/> is the one deliberate exception, and it is a manipulation rather than a
    /// leak: it holds trigger and site constant with PB and varies only <see cref="Form"/>, so that H4' can
    /// separate cue form from trigger policy [docs/PAPER1_PARAMETER_JUSTIFICATION.md §5B].
    /// </summary>
    public readonly struct FeedbackCommand
    {
        public readonly HapticSite Site;       // Chest for Generic; the at-risk limb's site for Body-localized
        public readonly Modality Modality;     // Haptic or Visual
        public readonly TriggerKind Trigger;   // Reactive or Predictive
        public readonly Joint Limb;            // the at-risk limb that caused the alert
        public readonly string ObstacleId;     // which obstacle drove the alert
        public readonly double DataTime;       // PoseFrame.Timestamp at the moment of firing
        public readonly float Distance;        // m, limb -> obstacle surface at fire
        public readonly float Ttc;             // s, time-to-collision at fire (+Inf when not applicable)

        /// <summary>
        /// Cue delivery form. <see cref="CueForm.Discrete"/> for every condition except
        /// <see cref="Condition.PBC"/>. A sink that cannot render a continuous cue must reject
        /// <see cref="CueForm.Continuous"/> loudly rather than downgrading it to a discrete pulse — a
        /// silent downgrade would turn PBC into PB and H4' would compare a condition against itself.
        /// </summary>
        public readonly CueForm Form;

        /// <summary>
        /// Intensity in [0, 1]. Always 1 for discrete cues, whose amplitude is fixed by design and set on
        /// the device. For <see cref="CueForm.Continuous"/> this is the current level from
        /// <see cref="ContinuousCueMapping"/> and changes frame to frame.
        /// </summary>
        public readonly float Intensity;

        /// <summary>
        /// Discrete cue — the existing five-argument-shape constructor. Kept so every current call site and
        /// test compiles unchanged, and so a discrete cue cannot accidentally be constructed with a
        /// partial intensity.
        /// </summary>
        public FeedbackCommand(HapticSite site, Modality modality, TriggerKind trigger, Joint limb,
                               string obstacleId, double dataTime, float distance, float ttc)
            : this(site, modality, trigger, limb, obstacleId, dataTime, distance, ttc,
                   CueForm.Discrete, 1f)
        {
        }

        public FeedbackCommand(HapticSite site, Modality modality, TriggerKind trigger, Joint limb,
                               string obstacleId, double dataTime, float distance, float ttc,
                               CueForm form, float intensity)
        {
            Site = site;
            Modality = modality;
            Trigger = trigger;
            Limb = limb;
            ObstacleId = obstacleId;
            DataTime = dataTime;
            Distance = distance;
            Ttc = ttc;
            Form = form;
            Intensity = intensity;
        }
    }
}
