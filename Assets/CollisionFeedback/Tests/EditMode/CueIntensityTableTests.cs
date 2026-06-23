using NUnit.Framework;
using CollisionFeedback.Core;

namespace CollisionFeedback.Tests
{
    public class CueIntensityTableTests
    {
        [Test]
        public void Default_is_uniform_one_for_every_site()
        {
            var t = new CueIntensityTable();
            Assert.That(CueIntensityTable.SiteCount, Is.EqualTo(5));
            foreach (HapticSite s in System.Enum.GetValues(typeof(HapticSite)))
                Assert.That(t.For(s), Is.EqualTo(1f));
        }

        [Test]
        public void Uniform_sets_all_sites_and_clamps()
        {
            Assert.That(CueIntensityTable.Uniform(0.4f).For(HapticSite.Chest), Is.EqualTo(0.4f).Within(1e-6f));
            Assert.That(CueIntensityTable.Uniform(2f).For(HapticSite.LeftHand), Is.EqualTo(1f));
            Assert.That(CueIntensityTable.Uniform(-1f).For(HapticSite.RightShin), Is.EqualTo(0f));
        }

        [Test]
        public void Set_clamps_to_unit_range_and_leaves_other_sites_default()
        {
            var t = new CueIntensityTable().Set(HapticSite.Chest, 0.45f).Set(HapticSite.LeftHand, 9f);
            Assert.That(t.For(HapticSite.Chest), Is.EqualTo(0.45f).Within(1e-6f));
            Assert.That(t.For(HapticSite.LeftHand), Is.EqualTo(1f));   // clamped
            Assert.That(t.For(HapticSite.RightHand), Is.EqualTo(1f));  // untouched default
        }

        [Test]
        public void Parse_reads_per_site_gains_skipping_header_comments_and_junk()
        {
            string csv =
                "# cue calibration\n" +
                "site,intensity\n" +
                "Chest,0.4\n" +
                "leftshin,0.8\n" +        // case-insensitive
                "Bogus,0.5\n" +           // unknown site ignored
                "RightHand,notanumber\n" + // bad number ignored
                "\n";                      // blank ignored
            var t = CueIntensityTable.Parse(csv);

            Assert.That(t.For(HapticSite.Chest), Is.EqualTo(0.4f).Within(1e-6f));
            Assert.That(t.For(HapticSite.LeftShin), Is.EqualTo(0.8f).Within(1e-6f));
            Assert.That(t.For(HapticSite.RightHand), Is.EqualTo(1f)); // unparsed → default
            Assert.That(t.For(HapticSite.LeftHand), Is.EqualTo(1f));  // unlisted → default
        }

        [Test]
        public void Parse_never_throws_on_garbage_and_handles_crlf()
        {
            Assert.DoesNotThrow(() => CueIntensityTable.Parse(null));
            Assert.DoesNotThrow(() => CueIntensityTable.Parse(""));
            var t = CueIntensityTable.Parse("Chest,0.3\r\nRightShin,0.7\r\n"); // Windows line endings
            Assert.That(t.For(HapticSite.Chest), Is.EqualTo(0.3f).Within(1e-6f));
            Assert.That(t.For(HapticSite.RightShin), Is.EqualTo(0.7f).Within(1e-6f));
        }

        [Test]
        public void ToCsv_round_trips_through_Parse()
        {
            var original = new CueIntensityTable()
                .Set(HapticSite.Chest, 0.42f)
                .Set(HapticSite.LeftHand, 0.9f)
                .Set(HapticSite.RightShin, 0.05f);
            var reparsed = CueIntensityTable.Parse(original.ToCsv());

            foreach (HapticSite s in System.Enum.GetValues(typeof(HapticSite)))
                Assert.That(reparsed.For(s), Is.EqualTo(original.For(s)).Within(1e-3f), $"site {s} mismatch");
        }
    }
}
