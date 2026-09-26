using System.Collections.Generic;
using NUnit.Framework;
using CollisionFeedback.Core;

namespace CollisionFeedback.Tests
{
    /// <summary>
    /// Guards `analysis_manifest.json` [PAPER1 §12; PAPER2 §14 — Paper 2's gate 17 "freeze and archive"
    /// cannot pass without it].
    ///
    /// The manifest is what lets someone who was not in the room check that the CSVs the analysis ran on
    /// are the CSVs that were collected. Its two failure modes are both silent: an unstable ordering makes
    /// every diff meaningless, and a missing hash makes a dataset look verified when it is not.
    /// </summary>
    public class AnalysisManifestTests
    {
        private static readonly Dictionary<string, string> Versions = new()
        {
            { "unity", "6000.3.16f1" }, { "application", "1.0" },
        };

        private static IReadOnlyList<ManifestEntry> Three() => new List<ManifestEntry>
        {
            new("zebra.csv", 30, "cccc"),
            new("alpha.csv", 10, "aaaa"),
            new("middle.csv", 20, "bbbb"),
        };

        [Test]
        public void Files_are_emitted_in_sorted_order_so_the_manifest_is_diffable()
        {
            string json = AnalysisManifest.Build("E2", 3, "2026-09-25T00:00:00Z", Versions, Three(), null);

            int a = json.IndexOf("alpha.csv");
            int m = json.IndexOf("middle.csv");
            int z = json.IndexOf("zebra.csv");

            Assert.That(a, Is.LessThan(m), "Entries must be sorted; unsorted output makes every diff noise.");
            Assert.That(m, Is.LessThan(z));
        }

        [Test]
        public void The_same_inputs_produce_byte_identical_output()
        {
            // If two runs over an unchanged directory differed, a diff would stop meaning "something
            // changed" and the manifest would lose the only property that makes it useful.
            string a = AnalysisManifest.Build("E2", 3, "2026-09-25T00:00:00Z", Versions, Three(), null);
            string b = AnalysisManifest.Build("E2", 3, "2026-09-25T00:00:00Z", Versions, Three(), null);
            Assert.That(a, Is.EqualTo(b));
        }

        [Test]
        public void A_missing_hash_is_detectable()
        {
            var withGap = new List<ManifestEntry>
            {
                new("ok.csv", 10, "aaaa"),
                new("unreadable.csv", 0, ""),     // could not be read at manifest time
            };

            Assert.That(AnalysisManifest.AllHashed(Three()), Is.True);
            Assert.That(AnalysisManifest.AllHashed(withGap), Is.False,
                "An unhashed file must be detectable. A dataset that looks verified but is not is worse " +
                "than one that is openly unverified.");
        }

        [Test]
        public void Exclusions_carry_scope_id_and_reason()
        {
            var ex = new List<ManifestExclusion>
            {
                new("trial", "P03-B2-T17", "cue_never_eligible"),
                new("participant", "P07", "withdrew"),
            };
            string json = AnalysisManifest.Build("E2", 3, "t", Versions, Three(), ex);

            Assert.That(json, Does.Contain("\"scope\": \"trial\""));
            Assert.That(json, Does.Contain("\"id\": \"P03-B2-T17\""));
            Assert.That(json, Does.Contain("\"reason\": \"cue_never_eligible\""));
            Assert.That(json, Does.Contain("\"reason\": \"withdrew\""));
        }

        [Test]
        public void Empty_collections_still_produce_valid_json_shape()
        {
            string json = AnalysisManifest.Build("P1", 0, "t", null,
                                                 new List<ManifestEntry>(), new List<ManifestExclusion>());

            Assert.That(json, Does.Contain("\"raw_files\": []"));
            Assert.That(json, Does.Contain("\"exclusions\": []"));
            Assert.That(json, Does.Contain("\"versions\": {}"));
            Assert.That(Balanced(json), Is.True, "Braces and brackets must balance even when empty.");
        }

        [Test]
        public void Quotes_and_backslashes_in_paths_are_escaped()
        {
            var awkward = new List<ManifestEntry> { new("od\"d\\name.csv", 1, "ff") };
            string json = AnalysisManifest.Build("P1", 1, "t", Versions, awkward, null);

            Assert.That(json, Does.Contain("od\\\"d\\\\name.csv"));
            Assert.That(Balanced(json), Is.True,
                "An unescaped quote would terminate the string early and produce unparseable JSON — which " +
                "would be discovered only when someone tried to read the archive.");
        }

        [Test]
        public void The_manifest_excludes_itself_by_name()
        {
            // The writer skips this name when walking the directory; it cannot contain its own hash.
            Assert.That(AnalysisManifest.FileName, Is.EqualTo("analysis_manifest.json"));
        }

        private static bool Balanced(string s)
        {
            int braces = 0, brackets = 0; bool inStr = false, esc = false;
            foreach (char c in s)
            {
                if (esc) { esc = false; continue; }
                if (c == '\\' && inStr) { esc = true; continue; }
                if (c == '"') { inStr = !inStr; continue; }
                if (inStr) continue;
                if (c == '{') braces++;
                else if (c == '}') braces--;
                else if (c == '[') brackets++;
                else if (c == ']') brackets--;
                if (braces < 0 || brackets < 0) return false;
            }
            return braces == 0 && brackets == 0 && !inStr;
        }
    }
}
