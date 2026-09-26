using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using CollisionFeedback.Core;

namespace CollisionFeedback.Tests
{
    /// <summary>
    /// Guards `events.csv` — the NON-TRIAL session chronology [PAPER2_STUDY_DESIGN §14].
    ///
    /// Scope is the point of this file. §14 lists per-trial quantities (command onset, physical onset,
    /// movement onset, boundary outcome) in its event-stream description, but those are already columns of
    /// `e2_trials.csv`. Re-emitting them would give two records of one measurement that could silently
    /// disagree after an edit. What belongs here is everything with no trial row to live in — the events
    /// that explain a gap in the timeline.
    /// </summary>
    public class SessionEventLogTests
    {
        private static string Row(SessionEvent e) => SessionEventFormatter.Row("Paper2_E2", 3, 1, e);

        private static int FieldCount(string row)
        {
            int n = 1; bool q = false;
            for (int i = 0; i < row.Length; i++)
            {
                char c = row[i];
                if (c == '"')
                {
                    if (q && i + 1 < row.Length && row[i + 1] == '"') { i++; continue; }
                    q = !q;
                }
                else if (c == ',' && !q) n++;
            }
            return n;
        }

        [Test]
        public void Every_row_matches_the_header_width()
        {
            int expected = SessionEventFormatter.Header().Split(',').Length;
            var events = new List<SessionEvent>
            {
                new(SessionEventKind.SessionStart, "plan=360 trials", "SETUP"),
                new(SessionEventKind.BreakStart, "", "TRIALS"),
                new(SessionEventKind.OperatorStop, "after 41 logged trials", "TRIALS"),
            };
            foreach (string r in SessionEventFormatter.Rows("Paper2_E2", 3, 1, events))
                Assert.That(FieldCount(r), Is.EqualTo(expected), r);
        }

        [Test]
        public void A_missing_session_time_writes_NA_not_zero()
        {
            // Zero is a real moment in the session. Writing it for "unknown" would put a fabricated event
            // at the start of every timeline.
            string row = Row(new SessionEvent(SessionEventKind.TrackingLost, "no clock"));
            Assert.That(row.Split(',')[4], Is.EqualTo("NA"));
        }

        [Test]
        public void Free_text_with_commas_and_quotes_is_escaped()
        {
            string row = Row(new SessionEvent(SessionEventKind.OperatorStop,
                "Participant said \"too fast\", paused 2 min", "TRIALS"));

            Assert.That(FieldCount(row), Is.EqualTo(SessionEventFormatter.Header().Split(',').Length));
            Assert.That(row.Split(',').Length, Is.GreaterThan(FieldCount(row)),
                "Sanity: this row must contain a quoted comma, or the escaping is not being exercised.");
            Assert.That(row, Does.Contain("\"\"too fast\"\""));
        }

        [Test]
        public void The_phase_is_carried_on_every_row()
        {
            // Without it, a reader has to reconstruct which part of the session an event belongs to by
            // scanning backwards for the last boundary marker.
            string row = Row(new SessionEvent(SessionEventKind.HomeWaitTimeout, "10 s", "PRACTICE"));
            Assert.That(row, Does.Contain(",PRACTICE,home_wait_timeout,"));
        }

        [Test]
        public void No_event_kind_duplicates_a_per_trial_measurement()
        {
            // The scoping rule, enforced rather than described. If someone adds "warning_onset" or
            // "boundary_crossed" here, e2_trials.csv stops being the single source of truth for it.
            var kinds = typeof(SessionEventKind)
                .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                .Select(f => (string)f.GetValue(null)).ToList();

            foreach (string banned in new[] { "warning", "onset", "boundary", "movement", "lead", "response" })
                Assert.That(kinds.Any(k => k.Contains(banned)), Is.False,
                    $"Event kind containing '{banned}' duplicates a per-trial column. Per-trial timing " +
                    "belongs in e2_trials.csv; this file carries only what has no trial row.");
        }

        [Test]
        public void The_kind_list_stays_small_and_every_kind_is_one_a_runner_emits()
        {
            // Four constants (home_wait, device, calibration, operator_note) were declared with no call
            // site. That is worse than omitting them: a reader seeing `tracking_lost` declared
            // reasonably assumes a dropout WOULD appear in the file, and it would not have.
            //
            // This test cannot see call sites from Core, so it pins the agreed list instead. Adding a
            // kind here without an emitter makes it fail, which is the prompt to wire one.
            var kinds = typeof(SessionEventKind)
                .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                .Select(f => (string)f.GetValue(null)).ToList();

            Assert.That(kinds, Is.EquivalentTo(new[]
            {
                "session_start", "session_end",
                "practice_start", "practice_end",
                "break_start", "break_end",
                "home_wait_timeout",
                "tracking_lost", "tracking_regained",
                "operator_stop",
            }), "Every kind must correspond to something a session runner actually emits.");
        }

        [Test]
        public void Session_and_practice_boundaries_are_representable()
        {
            // The practice/measured boundary is invisible in e2_trials.csv, which never receives practice
            // rows. Without these kinds the log cannot say when the measured session began.
            var kinds = typeof(SessionEventKind)
                .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                .Select(f => (string)f.GetValue(null)).ToList();

            foreach (string needed in new[] { "session_start", "session_end", "practice_end", "break_start" })
                Assert.That(kinds, Contains.Item(needed));
        }
    }
}
