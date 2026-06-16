using NUnit.Framework;
using CollisionFeedback.Core;

namespace CollisionFeedback.Tests
{
    public class TactorArbiterTests
    {
        [Test]
        public void Game_haptic_is_suppressed_on_a_site_a_safety_cue_is_holding()
        {
            var arb = new TactorArbiter(safetyHoldSeconds: 0.3);
            arb.NoteSafetyCue(HapticSite.LeftHand, t: 0.0);

            Assert.That(arb.AllowGameHaptic(HapticSite.LeftHand, 0.1), Is.False); // within hold
            Assert.That(arb.AllowGameHaptic(HapticSite.LeftHand, 0.4), Is.True);  // after hold
        }

        [Test]
        public void Game_haptic_passes_on_an_uncontested_site()
        {
            var arb = new TactorArbiter(safetyHoldSeconds: 0.3);
            arb.NoteSafetyCue(HapticSite.LeftHand, t: 0.0);

            Assert.That(arb.AllowGameHaptic(HapticSite.RightHand, 0.1), Is.True);
        }
    }
}
