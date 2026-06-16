using NUnit.Framework;
using UnityEngine;
using CollisionFeedback.Core;

namespace CollisionFeedback.Tests
{
    public class Layout1StimuliTests
    {
        [Test]
        public void Twelve_events_with_strictly_increasing_onsets()
        {
            var s = Layout1Stimuli.All();
            Assert.That(s.Count, Is.EqualTo(12));
            for (int i = 1; i < s.Count; i++)
                Assert.That(s[i].Onset, Is.GreaterThan(s[i - 1].Onset));
        }

        [Test]
        public void Kind_balance_matches_the_storyboard()
        {
            var s = Layout1Stimuli.All();
            // Storyboard: 7 orb-only, 4 projectile-only, 1 compound (E12).
            Assert.That(s.FindAll(e => e.Kind == StimulusKind.Orb).Count, Is.EqualTo(7));
            Assert.That(s.FindAll(e => e.Kind == StimulusKind.Projectile).Count, Is.EqualTo(4));
            Assert.That(s.FindAll(e => e.Kind == StimulusKind.Both).Count, Is.EqualTo(1));
            // 8 orbs and 5 projectiles total (each counts the compound).
            Assert.That(s.FindAll(e => e.HasOrb).Count, Is.EqualTo(8));
            Assert.That(s.FindAll(e => e.HasProjectile).Count, Is.EqualTo(5));
        }

        [Test]
        public void Ids_and_onsets_match_the_opportunity_schedule()
        {
            var stim = Layout1Stimuli.All();
            var opp = OpportunitySchedules.Layout1();
            Assert.That(stim.Count, Is.EqualTo(opp.Count));
            for (int i = 0; i < stim.Count; i++)
            {
                Assert.That(stim[i].Id, Is.EqualTo(opp[i].Id));
                Assert.That(stim[i].Onset, Is.EqualTo(opp[i].OnsetTime).Within(1e-9));
            }
        }

        [Test]
        public void Spawn_positions_are_inside_the_play_area()
        {
            foreach (var e in Layout1Stimuli.All())
            {
                if (e.HasOrb) AssertInArena(e.OrbPosition, e.Id + " orb");
                if (e.HasProjectile) AssertInArena(e.ProjectileOrigin, e.Id + " projectile");
            }
        }

        private static void AssertInArena(Vector3 p, string what)
        {
            Assert.That(Mathf.Abs(p.x), Is.LessThanOrEqualTo(1.75f), $"{what}: x outside the 3.5 m arena");
            Assert.That(Mathf.Abs(p.z), Is.LessThanOrEqualTo(1.75f), $"{what}: z outside the 3.5 m arena");
            Assert.That(p.y, Is.InRange(0f, 2.0f), $"{what}: y outside [0, 2] m");
        }
    }
}
