using System;
using System.Collections.Generic;
using NUnit.Framework;
using CollisionFeedback.Core;

namespace CollisionFeedback.Tests
{
    /// <summary>
    /// Guards the onset-matching calibration that closes the H4' onset confound
    /// [docs/PAPER1_READINESS.md §1; Core/ContinuousOnsetCalibration.cs].
    ///
    /// The calibration's job is to find the continuous-cue floor at which PBC's FIRST MOMENT feels as
    /// strong as PB's. If it silently returns the wrong number, PBC starts perceptibly later than PB and
    /// H4' confounds cue form with timing — while every log still looks correct. So the recovery test
    /// below drives it with a synthetic observer whose true match point is known.
    /// </summary>
    public class ContinuousOnsetCalibrationTests
    {
        private static ContinuousOnsetCalibration.Options OneSite(float start = 0.15f) =>
            new()
            {
                TestSites = new[] { HapticSite.RightHand },
                StartLevel = start,
                InitialStep = 0.10f,
                MinStep = 0.005f,
                ReversalsToStop = 14,
                ReversalsToAverage = 8,
            };

        /// <summary>
        /// Deterministic observer: reports the continuous onset as stronger whenever its level exceeds the
        /// observer's true match point. Noiseless, so a correct 1-up/1-down staircase must bracket it.
        /// </summary>
        private static float RecoverWithObserver(float trueMatch, ContinuousOnsetCalibration.Options o)
        {
            var cal = new ContinuousOnsetCalibration(o);
            int guard = 0;
            while (!cal.IsComplete && guard++ < 5000)
                cal.Respond(cal.CurrentTestLevel > trueMatch);
            Assert.That(cal.IsComplete, Is.True, "Calibration did not terminate.");
            return cal.EstimateFor(HapticSite.RightHand);
        }

        [Test]
        public void It_recovers_a_known_match_point()
        {
            foreach (float truth in new[] { 0.12f, 0.25f, 0.40f, 0.60f })
            {
                float est = RecoverWithObserver(truth, OneSite());
                Assert.That(est, Is.EqualTo(truth).Within(0.03f),
                    $"Failed to recover a match point of {truth:F2} (got {est:F3}). If this drifts, the " +
                    "frozen ContinuousCueMinIntensity is wrong and PBC's onset no longer matches PB's.");
            }
        }

        [Test]
        public void It_recovers_the_same_value_from_either_side()
        {
            // Starting above and below the truth must converge to the same place. A staircase that only
            // works from one side would bias the floor in whichever direction the default happens to sit.
            float fromBelow = RecoverWithObserver(0.35f, OneSite(start: 0.05f));
            float fromAbove = RecoverWithObserver(0.35f, OneSite(start: 0.80f));

            Assert.That(fromBelow, Is.EqualTo(0.35f).Within(0.04f));
            Assert.That(fromAbove, Is.EqualTo(0.35f).Within(0.04f));
        }

        [Test]
        public void An_unmatchable_onset_is_flagged_rather_than_silently_estimated()
        {
            // Observer that NEVER finds the continuous onset stronger — i.e. no level makes PBC's first
            // moment feel like PB's. The staircase runs to its ceiling.
            var cal = new ContinuousOnsetCalibration(OneSite());
            int guard = 0;
            while (!cal.IsComplete && guard++ < 5000) cal.Respond(false);

            Assert.That(cal.PinnedAtBound(HapticSite.RightHand), Is.True,
                "A staircase that ran to its bound must SAY so. Reporting a bound as an estimate would " +
                "freeze a floor that never matched, and H4' would claim an isolation it does not have. " +
                "When this fires, report H4' with the onset caveat instead of claiming cue form was isolated.");
        }

        // ── The multi-site assumption: one floor only works if the sites agree ──────────────────────

        [Test]
        public void Chest_is_rejected_because_pbc_cannot_cue_it()
        {
            var o = new ContinuousOnsetCalibration.Options
            {
                TestSites = new[] { HapticSite.RightHand, HapticSite.Chest },
            };
            Assert.Throws<ArgumentException>(() => new ContinuousOnsetCalibration(o),
                "PBC is body-localized. Calibrating a chest onset would tune a cue the condition never " +
                "delivers, and the resulting floor would be measured on the wrong device entirely.");
        }

        [Test]
        public void Agreeing_sites_pool_to_one_defensible_floor()
        {
            var cal = new ContinuousOnsetCalibration(new ContinuousOnsetCalibration.Options
            {
                TestSites = new[] { HapticSite.LeftHand, HapticSite.RightHand, HapticSite.LeftShin },
                MinStep = 0.005f, ReversalsToStop = 14, ReversalsToAverage = 8,
            });

            // Same truth on every site — what the E1 per-site gains are supposed to produce.
            var truth = new Dictionary<HapticSite, float>
            {
                [HapticSite.LeftHand] = 0.30f, [HapticSite.RightHand] = 0.31f, [HapticSite.LeftShin] = 0.29f,
            };
            int guard = 0;
            while (!cal.IsComplete && guard++ < 20000)
                cal.Respond(cal.CurrentTestLevel > truth[cal.CurrentSite]);

            Assert.That(cal.PooledEstimate, Is.EqualTo(0.30f).Within(0.04f));
            Assert.That(cal.Spread, Is.LessThan(ContinuousOnsetCalibration.MaxDefensibleSpread));
            Assert.That(cal.SpreadIsAcceptable, Is.True);
        }

        [Test]
        public void Disagreeing_sites_fail_the_spread_check()
        {
            var cal = new ContinuousOnsetCalibration(new ContinuousOnsetCalibration.Options
            {
                TestSites = new[] { HapticSite.LeftHand, HapticSite.LeftShin },
                MinStep = 0.005f, ReversalsToStop = 14, ReversalsToAverage = 8,
            });

            // Shin needs far more drive than hand — i.e. the E1 gains did NOT equalise onset salience.
            var truth = new Dictionary<HapticSite, float>
            {
                [HapticSite.LeftHand] = 0.15f, [HapticSite.LeftShin] = 0.55f,
            };
            int guard = 0;
            while (!cal.IsComplete && guard++ < 20000)
                cal.Respond(cal.CurrentTestLevel > truth[cal.CurrentSite]);

            Assert.That(cal.SpreadIsAcceptable, Is.False,
                "When sites disagree, a single frozen floor is the wrong model: the cue would start " +
                "perceptibly later on some limbs than others, reintroducing the onset confound for a " +
                "subset of opportunities. The runner must surface this rather than average it away.");
            Assert.That(cal.Spread, Is.GreaterThan(ContinuousOnsetCalibration.MaxDefensibleSpread));
        }

        // ── The value this produces is the one the engine uses ──────────────────────────────────────

        [Test]
        public void The_calibrated_floor_is_what_the_cue_mapping_consumes()
        {
            float calibrated = RecoverWithObserver(0.28f, OneSite());

            var p = new ContinuousCueParams(ttcMax: 1.0f, ttcMin: 0f, gamma: 2f,
                                            minPerceptibleIntensity: calibrated);

            // Just inside the band the gamma curve is ~0; the floor is what makes the onset perceptible,
            // so the calibration result must actually reach the skin at that moment.
            float atOnset = ContinuousCueMapping.Intensity(0.999f, p);
            Assert.That(atOnset, Is.EqualTo(calibrated).Within(1e-3f),
                "The calibrated floor must be exactly what the cue delivers at onset. If ContinuousCueMapping " +
                "ever stops honouring MinPerceptibleIntensity, the calibration becomes decorative and the " +
                "onset confound returns silently.");
        }
    }
}
