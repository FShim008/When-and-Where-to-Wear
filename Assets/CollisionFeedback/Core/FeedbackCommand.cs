namespace CollisionFeedback.Core
{
    /// <summary>
    /// One feedback event a condition decided to emit. The sink (bHaptics, in-HMD visual, or a test
    /// recorder) decides how to realize it. Across the haptic conditions the SIGNAL is identical -
    /// only <see cref="Site"/>, <see cref="Modality"/> and timing differ. That is the whole design:
    /// we manipulate WHEN and WHERE, never the cue itself.
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

        public FeedbackCommand(HapticSite site, Modality modality, TriggerKind trigger, Joint limb,
                               string obstacleId, double dataTime, float distance, float ttc)
        {
            Site = site;
            Modality = modality;
            Trigger = trigger;
            Limb = limb;
            ObstacleId = obstacleId;
            DataTime = dataTime;
            Distance = distance;
            Ttc = ttc;
        }
    }
}
