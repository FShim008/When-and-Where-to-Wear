using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using CollisionFeedback.Core;

namespace CollisionFeedback.Tests
{
    /// <summary>
    /// Guards `deviations.csv` [PAPER2 §14; PAPER1 §12].
    ///
    /// Paper 2's gate 10 asks that the logs "reconstruct every trial and preserve invalid-trial reasons."
    /// The trial CSV alone cannot: an operator stop between trials, a tactor that dropped during a break,
    /// or a participant reporting discomfort have no trial row to live in, and existed only in the
    /// operator's memory before this file.
    /// </summary>
    public class DeviationLogTests
    {
        private sealed class FakeTrial
        {
            public string Id; public bool Valid; public string Reason; public bool Timing;
        }

        private static List<FakeTrial> Trials() => new()
        {
            new FakeTrial { Id = "T01", Valid = true,  Reason = "",                    Timing = false },
            new FakeTrial { Id = "T02", Valid = false, Reason = "cue_never_eligible",  Timing = false },
            new FakeTrial { Id = "T03", Valid = true,  Reason = "",                    Timing = true  },
            new FakeTrial { Id = "T04", Valid = false, Reason = "",                    Timing = true  },
        };

        private static List<DeviationRecord> Derive() =>
            DeviationFormatter.FromTrials(Trials(), t => t.Id, t => t.Valid, t => t.Reason, t => t.Timing)
                              .ToList();

        [Test]
        public void Clean_trials_produce_no_rows()
        {
            Assert.That(Derive().Any(d => d.Id == "T01"), Is.False);
        }

        [Test]
        public void An_invalid_trial_carries_its_reason()
        {
            DeviationRecord d = Derive().Single(x => x.Id == "T02");
            Assert.That(d.Kind, Is.EqualTo(DeviationKind.InvalidTrial));
            Assert.That(d.Reason, Is.EqualTo("cue_never_eligible"));
        }

        [Test]
        public void A_blank_reason_becomes_unspecified_rather_than_empty()
        {
            // An empty reason column reads as "no reason recorded", which is indistinguishable from a bug
            // in the writer. Naming it makes the gap visible in the data itself.
            DeviationRecord d = Derive().First(x => x.Id == "T04" && x.Kind == DeviationKind.InvalidTrial);
            Assert.That(d.Reason, Is.EqualTo("unspecified"));
        }

        [Test]
        public void A_trial_that_is_both_invalid_and_out_of_tolerance_produces_both_rows()
        {
            var rows = Derive().Where(x => x.Id == "T04").ToList();
            Assert.That(rows.Count, Is.EqualTo(2),
                "Collapsing the two would hide one reason behind the other, and which one survived would " +
                "depend on evaluation order rather than on the data.");
            Assert.That(rows.Select(r => r.Kind),
                Is.EquivalentTo(new[] { DeviationKind.InvalidTrial, DeviationKind.TimingDeviation }));
        }

        [Test]
        public void A_timing_deviation_says_it_was_kept()
        {
            // §8: trials outside the tolerance band are marked, not deleted. Dropping them would bias the
            // long-lead tail, which is the part that determines the threshold.
            DeviationRecord d = Derive().First(x => x.Id == "T03");
            Assert.That(d.Kind, Is.EqualTo(DeviationKind.TimingDeviation));
            Assert.That(d.Detail.ToLowerInvariant(), Does.Contain("not deleted"));
        }

        /// <summary>
        /// Count CSV fields respecting RFC-4180 quoting. A naive Split(',') counts the commas INSIDE a
        /// quoted field, so it reports a correctly-escaped row as malformed - which is what the first
        /// version of these tests did.
        /// </summary>
        private static int FieldCount(string row)
        {
            int n = 1; bool inQuotes = false;
            for (int i = 0; i < row.Length; i++)
            {
                char c = row[i];
                if (c == '"')
                {
                    if (inQuotes && i + 1 < row.Length && row[i + 1] == '"') { i++; continue; }  // "" escape
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes) n++;
            }
            return n;
        }

        [Test]
        public void Every_row_has_the_same_column_count_as_the_header()
        {
            int expected = DeviationFormatter.Header().Split(',').Length;   // header has no quoted fields
            foreach (string row in DeviationFormatter.Rows("Paper2_E2", 3, 1, Derive()))
                Assert.That(FieldCount(row), Is.EqualTo(expected), row);
        }

        [Test]
        public void Commas_and_quotes_in_free_text_are_escaped()
        {
            var d = new DeviationRecord("session", "", DeviationKind.AdverseEvent, "discomfort",
                                        "Participant said \"my wrist aches\", session paused.");
            string row = DeviationFormatter.Row("Paper2_E2", 1, 0, d);

            Assert.That(FieldCount(row), Is.EqualTo(DeviationFormatter.Header().Split(',').Length),
                "Unescaped free text would shift every later column, and the corruption would look like " +
                "data rather than like a bug.");
            Assert.That(row.Split(',').Length, Is.GreaterThan(FieldCount(row)),
                "Sanity: this row SHOULD contain a quoted comma, so a naive split must over-count. If " +
                "it does not, the escaping under test is not being exercised.");
            Assert.That(row, Does.Contain("\"\"my wrist aches\"\""));
        }

        [Test]
        public void Session_scoped_events_need_no_trial_id()
        {
            var d = new DeviationRecord("session", "", DeviationKind.EquipmentFailure, "tactor_disconnected");
            string row = DeviationFormatter.Row("Paper2_E2", 3, 1, d);

            Assert.That(row, Does.Contain(",session,,equipment_failure,tactor_disconnected,"));
            Assert.That(row, Does.EndWith(",NA"), "A non-timestamped event writes NA, not 0.");
        }
    }
}
