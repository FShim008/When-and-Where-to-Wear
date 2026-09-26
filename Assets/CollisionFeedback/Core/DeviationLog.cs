using System.Collections.Generic;
using System.Globalization;

namespace CollisionFeedback.Core
{
    /// <summary>What kind of departure from protocol this row records.</summary>
    public static class DeviationKind
    {
        /// <summary>A trial or opportunity excluded from the denominator, with its reason.</summary>
        public const string InvalidTrial = "invalid_trial";

        /// <summary>Delivered, but outside the preregistered timing-tolerance band. KEPT, not deleted.</summary>
        public const string TimingDeviation = "timing_deviation";

        /// <summary>Operator pressed the emergency stop.</summary>
        public const string OperatorStop = "operator_stop";

        /// <summary>Tracker dropout, tactor disconnect, SDK failure — anything that broke mid-session.</summary>
        public const string EquipmentFailure = "equipment_failure";

        /// <summary>Discomfort, sickness, skin reaction, or anything else reportable to the ethics board.</summary>
        public const string AdverseEvent = "adverse_event";

        /// <summary>A departure the operator made deliberately and recorded at the time.</summary>
        public const string ProtocolDeparture = "protocol_departure";
    }

    /// <summary>One departure from protocol.</summary>
    public readonly struct DeviationRecord
    {
        public readonly string Scope;        // "trial" | "block" | "session"
        public readonly string Id;           // trial/opportunity id, block index, or ""
        public readonly string Kind;         // DeviationKind.*
        public readonly string Reason;       // machine-readable
        public readonly string Detail;       // free text for a human
        public readonly string Utc;          // ISO-8601, or "" when not wall-clock anchored
        public readonly double DataTimeSeconds;  // block/session time, NaN when not applicable

        public DeviationRecord(string scope, string id, string kind, string reason,
                               string detail = "", string utc = "", double dataTimeSeconds = double.NaN)
        {
            Scope = scope; Id = id; Kind = kind; Reason = reason ?? "";
            Detail = detail ?? ""; Utc = utc ?? ""; DataTimeSeconds = dataTimeSeconds;
        }
    }

    /// <summary>
    /// `deviations.csv` — "every invalid/aborted trial, equipment failure, operator stop, and adverse event
    /// with reason" [PAPER2_STUDY_DESIGN §14; PAPER1_STUDY_DESIGN §12 e-stop/adverse-event log].
    ///
    /// WHY A SEPARATE FILE WHEN THE TRIAL CSV ALREADY HAS `valid` AND `invalid_reason`.
    /// Because the trial CSV only knows about trials. An operator stop between trials, a tactor that
    /// disconnected during a break, a participant who reported discomfort — none of these have a row to
    /// live in, and today they exist only in an operator's memory. Paper 2's **gate 10** asks that the logs
    /// "reconstruct every trial and preserve invalid-trial reasons"; a reconstruction that cannot say *why*
    /// the session stopped for four minutes is not a reconstruction.
    ///
    /// It is also the file an ethics board asks for. Adverse events scattered across a trial table and a
    /// console log are not a safety record.
    ///
    /// **Deviations are kept, never deleted.** A timing deviation is a trial that ran outside tolerance and
    /// is still analysable with the realized lead; silently dropping it would bias the very tail that
    /// determines the threshold.
    /// </summary>
    public static class DeviationFormatter
    {
        public const string HeaderLine =
            "study,participant,session,scope,id,kind,reason,detail,utc,data_time_s";

        public static string Header() => HeaderLine;

        public static string Row(string study, int participant, int session, in DeviationRecord d)
        {
            var inv = CultureInfo.InvariantCulture;
            return string.Join(",",
                Csv(study),
                participant.ToString(inv),
                session.ToString(inv),
                Csv(d.Scope),
                Csv(d.Id),
                Csv(d.Kind),
                Csv(d.Reason),
                Csv(d.Detail),
                Csv(d.Utc),
                double.IsNaN(d.DataTimeSeconds) ? "NA" : d.DataTimeSeconds.ToString("F4", inv));
        }

        public static IEnumerable<string> Rows(string study, int participant, int session,
                                               IReadOnlyList<DeviationRecord> records)
        {
            for (int i = 0; i < records.Count; i++)
                yield return Row(study, participant, session, records[i]);
        }

        /// <summary>
        /// Derive trial-level deviations from outcomes already computed. A trial can produce TWO rows — it
        /// may be both invalid and outside tolerance — and both are emitted, because collapsing them would
        /// hide one reason behind the other.
        /// </summary>
        public static IEnumerable<DeviationRecord> FromTrials<T>(
            IReadOnlyList<T> trials,
            System.Func<T, string> id,
            System.Func<T, bool> isValid,
            System.Func<T, string> invalidReason,
            System.Func<T, bool> timingDeviation)
        {
            for (int i = 0; i < trials.Count; i++)
            {
                T t = trials[i];
                if (!isValid(t))
                    yield return new DeviationRecord("trial", id(t), DeviationKind.InvalidTrial,
                        string.IsNullOrEmpty(invalidReason(t)) ? "unspecified" : invalidReason(t));

                if (timingDeviation(t))
                    yield return new DeviationRecord("trial", id(t), DeviationKind.TimingDeviation,
                        "outside_timing_tolerance",
                        "Kept for analysis against the realized lead, not deleted.");
            }
        }

        private static string Csv(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return (s.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0)
                ? "\"" + s.Replace("\"", "\"\"") + "\"" : s;
        }
    }
}
