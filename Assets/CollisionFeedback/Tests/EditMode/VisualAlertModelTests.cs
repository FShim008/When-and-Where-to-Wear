using NUnit.Framework;
using CollisionFeedback.Core;

namespace CollisionFeedback.Tests
{
    public class VisualAlertModelTests
    {
        [Test]
        public void Beyond_the_alert_distance_is_off()
        {
            var off = VisualAlertModel.Evaluate(distance: 0.4f, alertDistance: 0.3f);
            Assert.That(off.Active, Is.False);
            Assert.That(off.Intensity, Is.EqualTo(0f));
            // The edge itself is exclusive.
            Assert.That(VisualAlertModel.Evaluate(0.3f, 0.3f).Active, Is.False);
        }

        [Test]
        public void Contact_is_full_intensity_and_fastest_pulse()
        {
            var c = VisualAlertModel.Evaluate(0f, 0.3f, minPulseHz: 1.5f, maxPulseHz: 6f);
            Assert.That(c.Active, Is.True);
            Assert.That(c.Intensity, Is.EqualTo(1f).Within(1e-5f));
            Assert.That(c.PulseHz, Is.EqualTo(6f).Within(1e-5f));
        }

        [Test]
        public void Halfway_is_about_half_intensity()
        {
            var m = VisualAlertModel.Evaluate(0.15f, 0.3f);
            Assert.That(m.Intensity, Is.EqualTo(0.5f).Within(1e-4f));
        }

        [Test]
        public void Closer_is_brighter_and_pulses_faster()
        {
            var near = VisualAlertModel.Evaluate(0.05f, 0.3f);
            var far = VisualAlertModel.Evaluate(0.20f, 0.3f);
            Assert.That(near.Intensity, Is.GreaterThan(far.Intensity));
            Assert.That(near.PulseHz, Is.GreaterThan(far.PulseHz));
        }

        [Test]
        public void Non_positive_alert_distance_is_off()
        {
            Assert.That(VisualAlertModel.Evaluate(0.1f, 0f).Active, Is.False);
        }
    }
}
