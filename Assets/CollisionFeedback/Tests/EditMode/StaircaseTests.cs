using NUnit.Framework;
using CollisionFeedback.Core;

namespace CollisionFeedback.Tests
{
    /// <summary>
    /// Drives the adaptive staircase [E2] with a SYNTHETIC OBSERVER — a noiseless step psychometric function
    /// that reports "test stronger" iff the presented level exceeds a hidden boundary — and checks the staircase
    /// recovers that boundary (the PSE), shrinks its step at reversals, and degrades gracefully when the match is
    /// unreachable (pins to a clamp instead of looping forever).
    /// </summary>
    public class StaircaseTests
    {
        // Runs a staircase against "test stronger := currentLevel > boundary" until it stops; returns it.
        private static Staircase RunToBoundary(Staircase sc, float boundary)
        {
            int guard = 0;
            while (!sc.Done && guard++ < 5000) sc.Respond(sc.CurrentLevel > boundary);
            Assert.That(sc.Done, Is.True, "staircase never terminated");
            return sc;
        }

        [Test]
        public void Converges_to_a_known_PSE_from_above_and_below()
        {
            foreach (float boundary in new[] { 0.37f, 0.62f })
            {
                var fromAbove = RunToBoundary(
                    new Staircase(startLevel: 0.9f, initialStep: 0.2f, minStep: 0.01f,
                                  reversalsToStop: 16, reversalsToAverage: 8), boundary);
                var fromBelow = RunToBoundary(
                    new Staircase(startLevel: 0.1f, initialStep: 0.2f, minStep: 0.01f,
                                  reversalsToStop: 16, reversalsToAverage: 8), boundary);

                Assert.That(fromAbove.Estimate, Is.EqualTo(boundary).Within(0.03f), $"from above @ {boundary}");
                Assert.That(fromBelow.Estimate, Is.EqualTo(boundary).Within(0.03f), $"from below @ {boundary}");
            }
        }

        [Test]
        public void Records_reversals_and_shrinks_the_step()
        {
            var sc = RunToBoundary(
                new Staircase(startLevel: 0.5f, initialStep: 0.2f, minStep: 0.01f, reversalsToStop: 12), 0.4f);
            Assert.That(sc.Reversals, Is.GreaterThanOrEqualTo(12));
            // Every recorded reversal level sits inside the search range.
            foreach (float r in sc.ReversalLevels) Assert.That(r, Is.InRange(0f, 1f));
            Assert.That(sc.PinnedAtBound, Is.False);
        }

        [Test]
        public void Pins_at_ceiling_when_the_match_is_above_the_range()
        {
            // Boundary 1.5 is unreachable (max drive 1.0) → observer never says "test stronger" → level climbs
            // to the ceiling and stays; the trial cap must end the run rather than spinning forever.
            var sc = RunToBoundary(new Staircase(startLevel: 0.5f, maxTrials: 40), 1.5f);
            Assert.That(sc.CurrentLevel, Is.EqualTo(1f).Within(1e-4f));
            Assert.That(sc.Estimate, Is.EqualTo(1f).Within(1e-4f));
            Assert.That(sc.PinnedAtBound, Is.True);
        }

        [Test]
        public void Pins_at_floor_when_the_match_is_below_the_range()
        {
            // Boundary -0.5 → observer always says "test stronger" → level driven to 0 and held.
            var sc = RunToBoundary(new Staircase(startLevel: 0.5f, maxTrials: 40), -0.5f);
            Assert.That(sc.CurrentLevel, Is.EqualTo(0f).Within(1e-4f));
            Assert.That(sc.PinnedAtBound, Is.True);
        }

        [Test]
        public void Respond_is_a_noop_after_completion()
        {
            var sc = RunToBoundary(new Staircase(reversalsToStop: 10, reversalsToAverage: 6), 0.5f);
            int trials = sc.Trials;
            float est = sc.Estimate;
            sc.Respond(true);
            sc.Respond(false);
            Assert.That(sc.Trials, Is.EqualTo(trials), "responses after Done must be ignored");
            Assert.That(sc.Estimate, Is.EqualTo(est));
        }
    }
}
