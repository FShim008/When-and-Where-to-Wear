using System.Collections.Generic;
using NUnit.Framework;
using CollisionFeedback.Core;

namespace CollisionFeedback.Tests
{
    /// <summary>
    /// Guards `session.json` [PAPER1 §12 "session and block metadata"; PAPER2 §14].
    ///
    /// Every other file in a session directory records the participant's behaviour. This one records the
    /// apparatus — which code, which devices, which parameter values were in force. A result that cannot
    /// say what `D`, `T` and the cue floor were at collection time cannot be reproduced once those defaults
    /// move, and the pilot exists to move them.
    /// </summary>
    public class SessionMetadataTests
    {
        private static IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Sections(
            params (string name, Dictionary<string, string> body)[] s)
        {
            var d = new Dictionary<string, IReadOnlyDictionary<string, string>>();
            foreach (var (name, body) in s) d[name] = body;
            return d;
        }

        private static string Sample() => SessionMetadata.Build(
            "Paper1", 7, "2026-09-25T10:00:00Z", "2026-09-25T11:50:00Z", "complete",
            Sections(
                ("parameters", SessionMetadata.Section(
                    ("predictive_ttc_s", "1"), ("proximity_distance_m", "0.3"))),
                ("software", SessionMetadata.Section(("unity", "6000.3.16f1")))));

        [Test]
        public void It_records_the_parameters_a_reviewer_will_ask_about()
        {
            string j = Sample();
            Assert.That(j, Does.Contain("\"predictive_ttc_s\": \"1\""));
            Assert.That(j, Does.Contain("\"proximity_distance_m\": \"0.3\""));
        }

        [Test]
        public void Output_is_deterministic_and_sorted()
        {
            // Two runs of one session must differ only where the session differed, or a diff between
            // archived datasets stops meaning anything.
            Assert.That(Sample(), Is.EqualTo(Sample()));

            string j = Sample();
            Assert.That(j.IndexOf("\"parameters\""), Is.LessThan(j.IndexOf("\"software\"")),
                "Sections must be sorted.");
            Assert.That(j.IndexOf("\"predictive_ttc_s\""), Is.LessThan(j.IndexOf("\"proximity_distance_m\"")),
                "Keys within a section must be sorted.");
        }

        [Test]
        public void An_empty_section_is_omitted_rather_than_written_as_an_empty_object()
        {
            // "devices": {} reads as "no devices were used", which is a claim. Omitting it reads as
            // "not recorded", which is the truth.
            string j = SessionMetadata.Build("Paper1", 1, "a", "b", "complete",
                Sections(("devices", new Dictionary<string, string>()),
                         ("software", SessionMetadata.Section(("unity", "6000")))));

            Assert.That(j, Does.Not.Contain("devices"));
            Assert.That(j, Does.Contain("software"));
        }

        [Test]
        public void Completion_status_distinguishes_a_stopped_session()
        {
            string stopped = SessionMetadata.Build("Paper1", 1, "a", "b", "stopped", null);
            Assert.That(stopped, Does.Contain("\"completion\": \"stopped\""),
                "A session halted by the e-stop must not be indistinguishable from one that finished.");
        }

        [Test]
        public void Quotes_and_backslashes_survive_as_valid_json()
        {
            string j = SessionMetadata.Build("Paper1", 1, "a", "b", "complete",
                Sections(("devices", SessionMetadata.Section(("path", "C:\\rigs\\\"main\"")))));

            Assert.That(j, Does.Contain("C:\\\\rigs\\\\\\\"main\\\""));
            Assert.That(Balanced(j), Is.True,
                "An unescaped quote produces unparseable JSON, discovered only when someone reads the archive.");
        }

        [Test]
        public void No_sections_still_produces_valid_json()
        {
            string j = SessionMetadata.Build("Paper2_E2", 3, "a", "b", "complete", null);
            Assert.That(Balanced(j), Is.True);
            Assert.That(j, Does.Contain("\"participant\": 3"));
        }

        [Test]
        public void Numbers_use_invariant_culture()
        {
            // A machine with a comma decimal separator would otherwise write 0,3 and break every reader.
            Assert.That(SessionMetadata.Num(0.3f), Is.EqualTo("0.3"));
            Assert.That(SessionMetadata.Num(1f), Is.EqualTo("1"));
        }

        private static bool Balanced(string s)
        {
            int braces = 0; bool inStr = false, esc = false;
            foreach (char c in s)
            {
                if (esc) { esc = false; continue; }
                if (c == '\\' && inStr) { esc = true; continue; }
                if (c == '"') { inStr = !inStr; continue; }
                if (inStr) continue;
                if (c == '{') braces++;
                else if (c == '}') braces--;
                if (braces < 0) return false;
            }
            return braces == 0 && !inStr;
        }
    }
}
