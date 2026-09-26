using System;
using NUnit.Framework;
using CollisionFeedback.Core;

namespace CollisionFeedback.Tests
{
    public class BHapticsTactorMapTests
    {
        [Test]
        public void Each_site_maps_to_its_own_device_with_at_least_one_motor()
        {
            // Chest is a sternum TACTOSY, not the vest. Changed 2026-09-21 with the H2 device-confound fix:
            // this assertion previously encoded the bug (PAPER1_STUDY_DESIGN §2, DECISION 2026-09-07).
            Assert.That(BHapticsTactorMap.For(HapticSite.Chest).Device,        Is.EqualTo(BHapticsDevice.TactosyTorso));
            Assert.That(BHapticsTactorMap.For(HapticSite.LeftHand).Device,  Is.EqualTo(BHapticsDevice.HandLeft));
            Assert.That(BHapticsTactorMap.For(HapticSite.RightHand).Device, Is.EqualTo(BHapticsDevice.HandRight));
            Assert.That(BHapticsTactorMap.For(HapticSite.LeftShin).Device,     Is.EqualTo(BHapticsDevice.FootLeft));
            Assert.That(BHapticsTactorMap.For(HapticSite.RightShin).Device,    Is.EqualTo(BHapticsDevice.FootRight));

            foreach (HapticSite s in Enum.GetValues(typeof(HapticSite)))
                Assert.That(BHapticsTactorMap.For(s).Motors.Length, Is.GreaterThan(0), $"site {s} has no motors");
            // Every defined HapticSite must map; an unmapped one now throws rather than silently
            // falling back to the vest. See H2H4FixTests for that guard.
        }

        [Test]
        public void Intensity_is_carried_through()
        {
            Assert.That(BHapticsTactorMap.For(HapticSite.Chest, 0.7f).Intensity, Is.EqualTo(0.7f).Within(1e-6f));
        }
    }
}
