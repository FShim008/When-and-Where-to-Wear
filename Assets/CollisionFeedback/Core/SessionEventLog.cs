using System.Collections.Generic;
using System.Globalization;

namespace CollisionFeedback.Core
{
    /// <summary>
    /// Kinds of NON-TRIAL session event. Per-trial timing (command onset, physical onset, movement onset,
    /// boundary outcome) is deliberately absent — it already lives in the per-trial CSV, and duplicating it
    /// would create two sources of truth for the same number.
    ///
    /// EVERY KIND HERE HAS A CALL SITE. Constants for events nothing emits are worse than no constants:
    /// a reader seeing `tracking_lost` declared reasonably assumes a dropout would appear in the file.
    /// Four such placeholders (home_wait, device, calibration, operator_note) were removed 2026-09-25
    /// rather than left as promises the code did not keep. Add one back only together with its emitter.
    /// </summary>
    public static class SessionEventKind
    {
        public const string SessionStart    = "session_start";
        public const string SessionEnd      = "session_end";
        public const string PracticeStart   = "practice_start";
        public const string PracticeEnd     = "practice_end";
        public const string BreakStart      = "break_start";
        public const string BreakEnd        = "break_end";
        public const string HomeWaitTimeout = "home_wait_timeout";
        /// <summary>No tracking frame for longer than the runner's dropout threshold.</summary>
        public const string TrackingLost    = "tracking_lost";
        /// <summary>Frames resumed. Detail carries the gap duration.</summary>
        public const string TrackingRegained= "tracking_regained";
        public const string OperatorStop    = "operator_stop";
    }

    public readonly struct SessionEvent
    {
        public readonly string Utc;
        public readonly double DataTimeSeconds;   // session clock; NaN when not applicable
        public readonly string Phase;             // e.g. "PRACTICE", "TRIALS", "BREAK"
        public readonly string Kind;
        public readonly string Detail;

        public SessionEvent(string kind, string detail = "", string phase = "",
                            string utc = "", double dataTimeSeconds = double.NaN)
        {
            Kind = kind; Detail = detail ?? ""; Phase = phase ?? "";
            Utc = utc ?? ""; DataTimeSeconds = dataTimeSeconds;
        }
    }

    /// <summary>
    /// `events.csv` — the **non-trial** session chronology [PAPER2_STUDY_DESIGN §14].
    ///
    /// SCOPE, AND WHY IT IS NARROW. §14 asks for "one chronological event stream for target, movement,
    /// warning, physical-onset estimate, boundary, response, tracking, and operator events." The first six
    /// of those are already columns of `e2_trials.csv`, recorded once per trial with full precision.
    /// Re-emitting them here would produce two records of the same measurement that could disagree after an
    /// edit, and the disagreement would be silent.
    ///
    /// So this file carries what has **no trial row to live in**: session and practice boundaries, breaks,
    /// waits for the home region, tracking dropouts, device state, and operator actions. Those are the
    /// events that explain a gap in the timeline — why four minutes passed between trial 40 and trial 41 —
    /// and today they exist only in an operator's memory.
    ///
    /// RELATIONSHIP TO `deviations.csv`. They overlap by design, and the overlap is one-directional: an
    /// operator stop is both a moment in the chronology and a fact the analysis and the ethics board need.
    /// This file answers *"what happened, in order?"*; `deviations.csv` answers *"what went wrong, and what
    /// does it exclude?"*. Neither is derivable from the other — most events are not deviations, and a
    /// deviation derived from a trial outcome has no timeline moment of its own.
    /// </summary>
    public static class SessionEventFormatter
    {
        public const string HeaderLine = "study,participant,session,utc,data_time_s,phase,kind,detail";

        public static string Header() => HeaderLine;

        public static string Row(string study, int participant, int session, in SessionEvent e)
        {
            var inv = CultureInfo.InvariantCulture;
            return string.Join(",",
                Csv(study),
                participant.ToString(inv),
                session.ToString(inv),
                Csv(e.Utc),
                double.IsNaN(e.DataTimeSeconds) ? "NA" : e.DataTimeSeconds.ToString("F4", inv),
                Csv(e.Phase),
                Csv(e.Kind),
                Csv(e.Detail));
        }

        public static IEnumerable<string> Rows(string study, int participant, int session,
                                               IReadOnlyList<SessionEvent> events)
        {
            for (int i = 0; i < events.Count; i++)
                yield return Row(study, participant, session, events[i]);
        }

        private static string Csv(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return (s.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0)
                ? "\"" + s.Replace("\"", "\"\"") + "\"" : s;
        }
    }
}
