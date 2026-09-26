using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace CollisionFeedback.Core
{
    /// <summary>
    /// One row of the PRIMARY dataset [PAPER1_STUDY_DESIGN §5, §9]. The unit of analysis is the
    /// **opportunity**, not the block: <c>Violation = 1</c> only when the designated target limb entered the
    /// designated target hazard inside that opportunity's window. Repeated penetration within one opportunity
    /// stays a single primary event and is counted separately as <see cref="EntryEpisodes"/>.
    ///
    /// WHY THIS REPLACED BLOCK COUNTS: a block-level collision count mixes together contacts by the wrong limb,
    /// contacts with the wrong hazard, and contacts outside any attribution window, and it forces the
    /// denominator to a fixed 12 even when an opportunity never presented or tracking was invalid. The
    /// opportunity row keeps the numerator and the denominator honest, and it is what the logistic mixed model
    /// consumes. Block counts remain available as a preregistered sensitivity analysis.
    ///
    /// <see cref="Valid"/> gates inclusion in the denominator. An opportunity that never presented, or that was
    /// cut off by an aborted block, is recorded with a reason rather than silently dropped or silently counted.
    /// </summary>
    public struct OpportunityOutcome
    {
        public string OpportunityId;
        public Joint TargetLimb;
        public string TargetObstacleId;
        public double PlannedOnset;
        public double PlannedClose;

        /// <summary>The inducing stimulus actually spawned for this opportunity.</summary>
        public bool Presented;
        /// <summary>Eligible for the primary denominator.</summary>
        public bool Valid;
        /// <summary>Empty when valid; otherwise why it was excluded (e.g. "not_presented", "block_aborted").</summary>
        public string InvalidReason;

        /// <summary>THE PRIMARY OUTCOME: target limb entered target hazard within the window (0 or 1).</summary>
        public int Violation;
        /// <summary>Separate entries into the target hazard during the window (secondary).</summary>
        public int EntryEpisodes;
        /// <summary>Block-relative time of the first entry, NaN when none.</summary>
        public double FirstEntryTime;
        /// <summary>Closest the target limb came to the TARGET hazard during the window (m). +Inf if never sampled.</summary>
        public float MinClearance;
        /// <summary>Deepest penetration into the target hazard during the window (m, 0 when never inside).</summary>
        public float MaxPenetration;

        /// <summary>Contacts during the window that were NOT the designated limb/hazard pair (secondary safety).</summary>
        public int UnattributedContacts;

        /// <summary>
        /// THE TIMING MANIPULATION CHECK [PAPER1_STUDY_DESIGN §6]. What BOTH warning policies would have done
        /// on this opportunity, recorded in every condition including `None` by
        /// <see cref="PolicyTriggerProbe"/>. Without it, an opportunity whose timing manipulation ran backwards
        /// is indistinguishable from one that worked, and a null H1 cannot be told apart from a broken
        /// manipulation.
        /// </summary>
        public PolicyTriggerRecord Timing;
    }

    /// <summary>
    /// CSV shape for the primary dataset: one row per planned opportunity, per block, per participant.
    /// Pure and testable; the Runtime writer persists it.
    /// </summary>
    public static class OpportunityOutcomeFormatter
    {
        public const string HeaderLine =
            "participant,block,condition,layout,opportunity_id,target_limb,target_obstacle," +
            "planned_onset_s,planned_close_s,presented,valid,invalid_reason," +
            "violation,entry_episodes,first_entry_s,min_clearance_m,max_penetration_m,unattributed_contacts," +
            // Timing manipulation check [§6]. delivered_trigger_s is whichever of the two the assigned
            // condition actually used (NA for None); policy_lead_s > 0 means the predictive policy genuinely
            // led, which is what H1 assumes. timing_inverted flags the opportunities where it did not.
            "trigger_prox_s,trigger_pred_s,policy_lead_s,timing_inverted,delivered_trigger_s," +
            "approach_at_onset_m,closing_speed_prox_ms,closing_speed_pred_ms";

        public static string Header() => HeaderLine;

        public static IEnumerable<string> Rows(BlockContext ctx, IReadOnlyList<OpportunityOutcome> outcomes)
        {
            for (int i = 0; i < outcomes.Count; i++) yield return Row(ctx, outcomes[i]);
        }

        public static string Row(BlockContext ctx, in OpportunityOutcome o)
        {
            var inv = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();
            sb.Append(ctx.ParticipantId.ToString(inv)).Append(',')
              .Append(ctx.BlockIndex.ToString(inv)).Append(',')
              .Append(ctx.Condition).Append(',')
              .Append(Csv(ctx.LayoutId)).Append(',')
              .Append(Csv(o.OpportunityId)).Append(',')
              .Append(o.TargetLimb).Append(',')
              .Append(Csv(o.TargetObstacleId)).Append(',')
              .Append(o.PlannedOnset.ToString("F3", inv)).Append(',')
              .Append(o.PlannedClose.ToString("F3", inv)).Append(',')
              .Append(o.Presented ? 1 : 0).Append(',')
              .Append(o.Valid ? 1 : 0).Append(',')
              .Append(Csv(o.InvalidReason)).Append(',')
              .Append(o.Violation.ToString(inv)).Append(',')
              .Append(o.EntryEpisodes.ToString(inv)).Append(',')
              .Append(Num(o.FirstEntryTime, inv)).Append(',')
              .Append(Num(o.MinClearance, inv)).Append(',')
              .Append(o.MaxPenetration.ToString("F4", inv)).Append(',')
              .Append(o.UnattributedContacts.ToString(inv)).Append(',')
              // ── timing manipulation check ──────────────────────────────────────────────────
              .Append(Num(o.Timing.TriggerTimeProximity, inv)).Append(',')
              .Append(Num(o.Timing.TriggerTimePredictive, inv)).Append(',')
              .Append(Num(o.Timing.PolicyLeadTime, inv)).Append(',')
              .Append(double.IsNaN(o.Timing.PolicyLeadTime) ? "NA" : (o.Timing.TimingInverted ? "1" : "0")).Append(',')
              .Append(Num(DeliveredTrigger(ctx.Condition, o.Timing), inv)).Append(',')
              .Append(Num(o.Timing.ApproachDistanceAtOnset, inv)).Append(',')
              .Append(Num(o.Timing.ClosingSpeedAtProximity, inv)).Append(',')
              .Append(Num(o.Timing.ClosingSpeedAtPredictive, inv));
            return sb.ToString();
        }

        /// <summary>
        /// The trigger the ASSIGNED condition actually used, so analysis does not have to re-derive the
        /// condition→policy mapping. NaN for `None`, which never fires.
        /// </summary>
        public static double DeliveredTrigger(Condition condition, in PolicyTriggerRecord t) =>
            ConditionManager.TriggerFor(condition) switch
            {
                TriggerKind.Reactive => t.TriggerTimeProximity,
                TriggerKind.Predictive => t.TriggerTimePredictive,
                _ => double.NaN,
            };

        // NA rather than NaN/Infinity, so R reads the column as numeric without a coercion warning.
        private static string Num(double v, CultureInfo inv) =>
            double.IsNaN(v) || double.IsInfinity(v) ? "NA" : v.ToString("F4", inv);

        private static string Csv(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return (s.IndexOf(',') >= 0 || s.IndexOf('"') >= 0 || s.IndexOf('\n') >= 0)
                ? "\"" + s.Replace("\"", "\"\"") + "\"" : s;
        }
    }
}
