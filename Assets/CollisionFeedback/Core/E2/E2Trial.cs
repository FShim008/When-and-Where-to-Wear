using UnityEngine;

namespace CollisionFeedback.Core.E2
{
    /// <summary>Normal or urgent reach, set by the target-response window [PAPER2 §8].</summary>
    public enum Tempo
    {
        Normal,
        Urgent,
    }

    /// <summary>
    /// One scheduled E2 trial [PAPER2_STUDY_DESIGN §8]. Everything here is fixed before the trial begins —
    /// nothing is chosen in response to what the participant does, which is what keeps the assigned lead time
    /// an independent variable rather than a consequence of behaviour.
    ///
    /// 75% of trials are ordinary go trials (<see cref="IsWarningTrial"/> false). They still carry a hazard
    /// intersecting the direct reach: **their crossing rate is the avoidance probability at zero warning**,
    /// the measured lower asymptote of the psychometric curve (§8).
    /// </summary>
    public readonly struct E2Trial
    {
        public readonly string Id;
        /// <summary>The reaching limb under test. One limb per participant (§4, dominant by default).</summary>
        public readonly Joint TargetLimb;
        /// <summary>Id of the invisible hazard volume the direct reach intersects.</summary>
        public readonly string HazardId;
        /// <summary>Visible reach target, placed beyond the hazard.</summary>
        public readonly Vector3 TargetPosition;
        public readonly Tempo Tempo;

        /// <summary>True on the 25% of trials that carry a warning.</summary>
        public readonly bool IsWarningTrial;

        /// <summary>
        /// Seconds of warning the participant is *assigned* — the intended interval between the first physical
        /// vibration and predicted contact. One of the 5–7 preregistered levels. NaN on go trials.
        ///
        /// This is the **requested** lead. What the participant actually received is
        /// <see cref="E2TrialOutcome.RealizedLeadSeconds"/>, and the psychometric fit uses that one, never this
        /// (§9 H1: "physically realized warning lead time").
        /// </summary>
        public readonly float AssignedLeadSeconds;

        public E2Trial(string id, Joint targetLimb, string hazardId, Vector3 targetPosition, Tempo tempo,
                       bool isWarningTrial, float assignedLeadSeconds)
        {
            Id = id;
            TargetLimb = targetLimb;
            HazardId = hazardId;
            TargetPosition = targetPosition;
            Tempo = tempo;
            IsWarningTrial = isWarningTrial;
            AssignedLeadSeconds = assignedLeadSeconds;
        }

        public static E2Trial Go(string id, Joint limb, string hazardId, Vector3 target, Tempo tempo) =>
            new E2Trial(id, limb, hazardId, target, tempo, false, float.NaN);

        public static E2Trial Warning(string id, Joint limb, string hazardId, Vector3 target, Tempo tempo,
                                      float leadSeconds) =>
            new E2Trial(id, limb, hazardId, target, tempo, true, leadSeconds);
    }
}
