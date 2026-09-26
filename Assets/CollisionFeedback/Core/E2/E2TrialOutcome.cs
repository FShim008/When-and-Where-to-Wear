using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace CollisionFeedback.Core.E2
{
    /// <summary>
    /// One row of the E2 primary dataset [PAPER2_STUDY_DESIGN §8, §9].
    ///
    /// The primary outcome is <see cref="Crossed"/> — did the target hand enter its designated hazard volume.
    /// The psychometric fit regresses that on <see cref="RealizedLeadSeconds"/>, **never** on the trial's
    /// assigned lead: H1 is about *physically realized* warning time, and delivery is imprecise by construction
    /// (frame quantisation, pipeline latency jitter, and error in the constant-velocity contact prediction).
    /// Precision comes from measuring what was delivered, not from delivering precisely.
    /// </summary>
    public struct E2TrialOutcome
    {
        public string TrialId;
        public Joint TargetLimb;
        public string HazardId;
        public Tempo Tempo;
        public bool IsWarningTrial;

        /// <summary>Eligible for analysis. False when the trial never produced a usable reach.</summary>
        public bool Valid;
        /// <summary>Empty when valid; otherwise why (e.g. "no_movement_onset", "cue_never_eligible").</summary>
        public string InvalidReason;

        // ── PRIMARY ────────────────────────────────────────────────────────────────────────
        /// <summary>THE PRIMARY OUTCOME: the target limb entered its designated hazard (0/1).</summary>
        public int Crossed;

        // ── Timing ─────────────────────────────────────────────────────────────────────────
        public float AssignedLeadSeconds;     // requested; NaN on go trials
        public double CommandTime;            // when the cue command was issued; NaN if never
        public double PhysicalOnsetTime;      // command + measured pipeline latency; NaN if never

        /// <summary>
        /// **The x-axis of the psychometric fit.** Predicted contact time minus physical cue onset, using the
        /// pre-cue trajectory frozen at onset and never revised by the participant's response (§8).
        /// NaN on go trials and when the cue never fired.
        /// </summary>
        public double RealizedLeadSeconds;

        /// <summary>
        /// The delivered lead missed the assigned lead by more than the preregistered tolerance. **The trial
        /// stays valid** (§8) — the psychometric fit uses the realized lead, so a mistimed trial is still a
        /// legitimate point on the curve. The flag exists for the intention-to-deliver analysis and for
        /// reporting how often delivery missed.
        /// </summary>
        public bool TimingDeviation;

        public float PreCueSpeed;             // m/s radial closing speed at physical onset
        public float DistanceAtCue;           // m to the hazard surface at physical onset
        public double PredictedContactTime;   // the frozen counterfactual

        // ── Counterfactual validation (added 2026-09-14) ───────────────────────────────────
        /// <summary>Block time the limb actually entered the hazard, NaN when it never did.</summary>
        public double ActualContactTime;
        /// <summary>
        /// Predicted minus actual contact time, on trials where contact actually happened. **Positive means
        /// the prediction was late.** NaN when either is unavailable. This is the direct, empirical measure of
        /// how good the constant-velocity counterfactual was — the study's largest unquantified assumption,
        /// turned into a reported number at the cost of one column.
        /// </summary>
        public double PredictionErrorSeconds => double.IsNaN(PredictedContactTime) || double.IsNaN(ActualContactTime)
            ? double.NaN
            : PredictedContactTime - ActualContactTime;

        // ── Kinematics and secondary outcomes ──────────────────────────────────────────────
        public float MinClearance;            // closest approach to the hazard surface (m)
        public float MaxPenetration;          // deepest entry (m, 0 if never inside)
        public float PostCueTravel;           // m travelled toward the hazard after physical onset
        public double ResponseOnsetTime;      // first sustained deceleration after the cue; NaN if none
        public double ArrestTime;             // physical cue onset to minimum outward velocity; NaN if none
        public bool ReachedTarget;

        // ── Tempo manipulation and its check (added 2026-09-14) ────────────────────────────────
        /// <summary>Block time at confirmed movement onset. NaN when the reach never began.</summary>
        public double MovementOnsetTime;
        /// <summary>Block time the limb first reached the visible target. NaN when it never did.</summary>
        public double TargetContactTime;
        /// <summary>Longest movement time this trial's tempo accepted (s).</summary>
        public float ResponseWindowSeconds;
        /// <summary>Shortest movement time this trial's tempo accepted (s). 0 when only an upper bound applies.</summary>
        public float ResponseWindowMinSeconds;

        /// <summary>
        /// Movement onset to target contact. **This is the tempo manipulation check** (§8: "confirm the
        /// manipulation with pre-cue hand velocity and movement time"), and with `precue_speed_ms` it is the
        /// evidence that §15 gate 7 asks for.
        ///
        /// NaN on trials where the target was never reached — which includes every successful stop, by
        /// design. So the manipulation check runs on **go trials**, where the reach completes; warning trials
        /// contribute pre-cue speed instead.
        /// </summary>
        public double MovementTimeSeconds => double.IsNaN(MovementOnsetTime) || double.IsNaN(TargetContactTime)
            ? double.NaN
            : TargetContactTime - MovementOnsetTime;

        /// <summary>
        /// The reach landed inside its tempo's window. False when the target was never reached, so read it
        /// together with <see cref="ReachedTarget"/>: a high out-of-window rate on go trials is the
        /// instruction failure §15 gate 7 warns about, not a tempo effect.
        /// </summary>
        public bool WithinResponseWindow => ReachedTarget && !double.IsNaN(MovementTimeSeconds) &&
                                            MovementTimeSeconds <= ResponseWindowSeconds &&
                                            MovementTimeSeconds >= ResponseWindowMinSeconds;
    }

    /// <summary>CSV shape for the E2 primary dataset. Pure and testable; the Runtime writer persists it.</summary>
    public static class E2TrialOutcomeFormatter
    {
        public const string HeaderLine =
            "participant,session,trial_id,target_limb,hazard,tempo,warning_trial,valid,invalid_reason," +
            "crossed," +
            "assigned_lead_s,realized_lead_s,timing_deviation,command_time_s,physical_onset_s," +
            "precue_speed_ms,distance_at_cue_m,predicted_contact_s,actual_contact_s,prediction_error_s," +
            "min_clearance_m,max_penetration_m,post_cue_travel_m,response_onset_s,arrest_s,reached_target," +
            // Tempo manipulation check (§8, §15 gate 7). Appended rather than inserted so readers that index
            // by position against an earlier file do not silently shift.
            "movement_onset_s,target_contact_s,movement_time_s,response_window_s,response_window_min_s,within_window";

        public static string Header() => HeaderLine;

        public static IEnumerable<string> Rows(int participant, int session,
                                               IReadOnlyList<E2TrialOutcome> outcomes)
        {
            for (int i = 0; i < outcomes.Count; i++) yield return Row(participant, session, outcomes[i]);
        }

        public static string Row(int participant, int session, in E2TrialOutcome o)
        {
            var inv = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();
            sb.Append(participant.ToString(inv)).Append(',')
              .Append(session.ToString(inv)).Append(',')
              .Append(Csv(o.TrialId)).Append(',')
              .Append(o.TargetLimb).Append(',')
              .Append(Csv(o.HazardId)).Append(',')
              .Append(o.Tempo).Append(',')
              .Append(o.IsWarningTrial ? 1 : 0).Append(',')
              .Append(o.Valid ? 1 : 0).Append(',')
              .Append(Csv(o.InvalidReason)).Append(',')
              .Append(o.Crossed.ToString(inv)).Append(',')
              .Append(Num(o.AssignedLeadSeconds, inv)).Append(',')
              .Append(Num(o.RealizedLeadSeconds, inv)).Append(',')
              .Append(o.TimingDeviation ? 1 : 0).Append(',')
              .Append(Num(o.CommandTime, inv)).Append(',')
              .Append(Num(o.PhysicalOnsetTime, inv)).Append(',')
              .Append(Num(o.PreCueSpeed, inv)).Append(',')
              .Append(Num(o.DistanceAtCue, inv)).Append(',')
              .Append(Num(o.PredictedContactTime, inv)).Append(',')
              .Append(Num(o.ActualContactTime, inv)).Append(',')
              .Append(Num(o.PredictionErrorSeconds, inv)).Append(',')
              .Append(Num(o.MinClearance, inv)).Append(',')
              .Append(Num(o.MaxPenetration, inv)).Append(',')
              .Append(Num(o.PostCueTravel, inv)).Append(',')
              .Append(Num(o.ResponseOnsetTime, inv)).Append(',')
              .Append(Num(o.ArrestTime, inv)).Append(',')
              .Append(o.ReachedTarget ? 1 : 0).Append(',')
              .Append(Num(o.MovementOnsetTime, inv)).Append(',')
              .Append(Num(o.TargetContactTime, inv)).Append(',')
              .Append(Num(o.MovementTimeSeconds, inv)).Append(',')
              .Append(Num(o.ResponseWindowSeconds, inv)).Append(',')
              .Append(Num(o.ResponseWindowMinSeconds, inv)).Append(',')
              .Append(o.WithinResponseWindow ? 1 : 0);
            return sb.ToString();
        }

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
