using System.Collections.Generic;

namespace CollisionFeedback.Core
{
    /// <summary>
    /// Block-level roll-up of the timing manipulation check [PAPER1_STUDY_DESIGN §6].
    ///
    /// Pure and testable so the Runtime only formats it. The operator needs this per block during piloting:
    /// an opportunity whose timing manipulation ran backwards is indistinguishable from a working one in the
    /// violation column, so it has to be surfaced deliberately or it is never seen.
    /// </summary>
    public readonly struct PolicyTriggerSummary
    {
        /// <summary>Valid opportunities where BOTH policies triggered, so the lead time is defined.</summary>
        public readonly int Measurable;
        /// <summary>Of those, how many had the predictive policy fire LATER than the proximity policy.</summary>
        public readonly int Inverted;
        /// <summary>Valid opportunities whose approach distance was under the floor the timing rule needs.</summary>
        public readonly int BelowApproachFloor;
        /// <summary>Mean of <see cref="PolicyTriggerRecord.PolicyLeadTime"/> over measurable opportunities (s).</summary>
        public readonly double MeanLeadSeconds;

        public PolicyTriggerSummary(int measurable, int inverted, int belowApproachFloor, double meanLeadSeconds)
        {
            Measurable = measurable;
            Inverted = inverted;
            BelowApproachFloor = belowApproachFloor;
            MeanLeadSeconds = meanLeadSeconds;
        }

        /// <summary>
        /// True when nothing looks wrong. An inversion is not noise: those opportunities push H1 in the
        /// opposite direction to the one it assumes, so any inversion is worth a warning.
        /// </summary>
        public bool Clean => Measurable > 0 && Inverted == 0 && BelowApproachFloor == 0;

        /// <summary>
        /// Summarize the VALID opportunities of one block. Invalid rows are excluded for the same reason they
        /// are excluded from the primary denominator — they are not part of the experiment.
        /// </summary>
        public static PolicyTriggerSummary Summarize(IReadOnlyList<OpportunityOutcome> outcomes,
                                                     float approachFloor = OracleParams.MinApproachDistanceForValidTiming)
        {
            int measurable = 0, inverted = 0, belowFloor = 0;
            double leadSum = 0d;

            for (int i = 0; i < outcomes.Count; i++)
            {
                OpportunityOutcome o = outcomes[i];
                if (!o.Valid) continue;

                float approach = o.Timing.ApproachDistanceAtOnset;
                if (!float.IsNaN(approach) && approach < approachFloor) belowFloor++;

                double lead = o.Timing.PolicyLeadTime;
                if (double.IsNaN(lead)) continue;
                measurable++;
                leadSum += lead;
                if (o.Timing.TimingInverted) inverted++;
            }

            return new PolicyTriggerSummary(measurable, inverted, belowFloor,
                                            measurable > 0 ? leadSum / measurable : double.NaN);
        }

        /// <summary>One console line for the operator. Shaped here so Runtime and tests cannot drift apart.</summary>
        public string Describe(float approachFloor = OracleParams.MinApproachDistanceForValidTiming)
        {
            if (Measurable == 0)
                return "TIMING: no opportunity had both policy triggers — the timing manipulation is " +
                       "unmeasurable for this block. Check hazard geometry and that limbs actually approached " +
                       "their targets.";

            return $"TIMING: mean policy lead {MeanLeadSeconds:F3}s over {Measurable} measurable " +
                   $"opportunit{(Measurable == 1 ? "y" : "ies")}; inverted={Inverted}; " +
                   $"below approach floor ({approachFloor:F2} m)={BelowApproachFloor}." +
                   (Clean ? "" : " Inspect before treating H1 as valid.");
        }
    }
}
