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
            Assert.That(BHapticsTactorMap.For(HapticSite.Chest).Device,        Is.EqualTo(BHapticsDevice.VestFront));
            Assert.That(BHapticsTactorMap.For(HapticSite.LeftHand).Device,  Is.EqualTo(BHapticsDevice.HandLeft));
            Assert.That(BHapticsTactorMap.For(HapticSite.RightHand).Device, Is.EqualTo(BHapticsDevice.HandRight));
            Assert.That(BHapticsTactorMap.For(HapticSite.LeftShin).Device,     Is.EqualTo(BHapticsDevice.FootLeft));
            Assert.That(BHapticsTactorMap.For(HapticSite.RightShin).Device,    Is.EqualTo(BHapticsDevice.FootRight));

            foreach (HapticSite s in Enum.GetValues(typeof(HapticSite)))
                Assert.That(BHapticsTactorMap.For(s).Motors.Length, Is.GreaterThan(0), $"site {s} has no motors");
        }

        [Test]
        public void Intensity_is_carried_through()
        {
            Assert.That(BHapticsTactorMap.For(HapticSite.Chest, 0.7f).Intensity, Is.EqualTo(0.7f).Within(1e-6f));
        }
    }
}
