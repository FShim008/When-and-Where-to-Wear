using System.Collections.Generic;
using NUnit.Framework;
using CollisionFeedback.Core;

namespace CollisionFeedback.Tests
{
    /// <summary>
    /// End-to-end test of the per-site matching procedure [E2] with a synthetic observer whose PERCEIVED
    /// magnitude is a per-site gain × device drive (P = k_site · drive). The chest has a large k (40 motors),
    /// the Tactosys smaller k — exactly the confound the procedure exists to remove. The staircases should drive
    /// every site to the drive that equalizes P against the reference, i.e. gain_site ≈ (k_ref · refDrive) / k_site.
    /// </summary>
    public class CueIntensityCalibrationTests
    {
        // Perceived magnitude model: linear in drive, per-site slope k.
        private static Dictionary<HapticSite, float> Gains() => new()
        {
            { HapticSite.Chest,     2.0f },  // strong (many motors) → must be ATTENUATED to match
            { HapticSite.LeftHand,  1.0f },
            { HapticSite.RightHand, 1.0f },
            { HapticSite.LeftShin,  0.8f },
            { HapticSite.RightShin, 1.25f },
        };

        // Run a calibration to completion against the observer; returns the built table.
        private static CueIntensityTable RunMatch(CueIntensityCalibration cal, Dictionary<HapticSite, float> k,
                                                  float refPerceived)
        {
            int guard = 0;
            while (!cal.IsComplete && guard++ < 100000)
            {
                float testP = k[cal.CurrentSite] * cal.CurrentTestLevel;
                cal.Respond(testP > refPerceived);
            }
            Assert.That(cal.IsComplete, Is.True, "calibration never completed");
            return cal.BuildTable();
        }

        [Test]
        public void Reference_is_excluded_from_the_matched_sites()
        {
            var cal = new CueIntensityCalibration(new CueIntensityCalibration.Options
            {
                ReferenceSite = HapticSite.LeftHand,
            });
            Assert.That(cal.TestSites, Has.Count.EqualTo(CueIntensityTable.SiteCount - 1));
            foreach (HapticSite s in cal.TestSites)
                Assert.That(s, Is.Not.EqualTo(HapticSite.LeftHand)); // the reference is never one of the matched sites
        }

        [Test]
        public void Matches_every_site_to_the_reference_perceived_magnitude()
        {
            var k = Gains();
            var opt = new CueIntensityCalibration.Options
            {
                ReferenceSite = HapticSite.LeftHand,
                ReferenceIntensity = 0.5f,
                MinStep = 0.01f,
                ReversalsToStop = 16,
                ReversalsToAverage = 8,
            };
            var cal = new CueIntensityCalibration(opt);
            float refP = k[opt.ReferenceSite] * opt.ReferenceIntensity; // = 0.5

            CueIntensityTable table = RunMatch(cal, k, refP);

            // Reference keeps its chosen drive exactly.
            Assert.That(table.For(HapticSite.LeftHand), Is.EqualTo(0.5f).Within(1e-4f));
            // Each other site is driven to (refP / k_site).
            Assert.That(table.For(HapticSite.Chest),     Is.EqualTo(0.25f).Within(0.04f));  // strong → attenuated
            Assert.That(table.For(HapticSite.RightHand), Is.EqualTo(0.50f).Within(0.04f));
            Assert.That(table.For(HapticSite.LeftShin),  Is.EqualTo(0.625f).Within(0.04f)); // weaker → boosted
            Assert.That(table.For(HapticSite.RightShin), Is.EqualTo(0.40f).Within(0.04f));

            foreach (HapticSite s in System.Enum.GetValues(typeof(HapticSite)))
                Assert.That(cal.PinnedAtBound(s), Is.False, $"{s} should reach the reference");
        }

        [Test]
        public void Flags_a_site_that_cannot_reach_a_too_strong_reference()
        {
            // Reference = chest at a high drive → perceived magnitude beyond what a 1.0-gain hand can produce
            // even at full drive → that site pins at the ceiling and is flagged (equalization incomplete there).
            var k = Gains();
            var opt = new CueIntensityCalibration.Options
            {
                ReferenceSite = HapticSite.Chest,
                ReferenceIntensity = 0.8f, // refP = 2.0 * 0.8 = 1.6; a k=1.0 site maxes at 1.0
            };
            var cal = new CueIntensityCalibration(opt);
            float refP = k[opt.ReferenceSite] * opt.ReferenceIntensity;

            CueIntensityTable table = RunMatch(cal, k, refP);

            Assert.That(table.For(HapticSite.Chest), Is.EqualTo(0.8f).Within(1e-4f)); // reference fixed
            Assert.That(cal.PinnedAtBound(HapticSite.LeftHand), Is.True);
            Assert.That(table.For(HapticSite.LeftHand), Is.EqualTo(1f).Within(1e-2f)); // best-effort ceiling
        }
    }
}
