using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CollisionFeedback.Core;
using Joint = CollisionFeedback.Core.Joint; // disambiguate from UnityEngine.Joint (physics component)

namespace CollisionFeedback.Tests
{
    /// <summary>
    /// Covers the offline robustness analysis: the noise injector must be deterministic and correctly scaled,
    /// and the fidelity measure must show warning quality degrading as tracking error grows (the empirical
    /// answer to "your oracle was idealized").
    /// </summary>
    public class RobustnessTests
    {
        private static PoseFrame Frame(double t, float x) => new()
        {
            Timestamp = t,
            Joints = new[]
            {
                new Vector3(x, 1.6f, 0f), new Vector3(x, 1.2f, 0f), new Vector3(x, 1.0f, 0f),
                new Vector3(x, 1.0f, 0.2f), new Vector3(x, 0.1f, 0f), new Vector3(x, 0.1f, 0.2f),
            },
        };

        [Test]
        public void Zero_noise_is_a_no_op()
        {
            var noise = new TrackingNoise(0f, 1, 0f);
            PoseFrame src = Frame(1.25, 0.5f);
            PoseFrame outF = noise.Apply(src);

            Assert.That(outF.Timestamp, Is.EqualTo(src.Timestamp));
            for (int j = 0; j < JointInfo.Count; j++)
                Assert.That(Vector3.Distance(outF.Joints[j], src.Joints[j]), Is.LessThan(1e-6f));
        }

        [Test]
        public void Same_seed_reproduces_the_same_noise()
        {
            PoseFrame src = Frame(0.0, 0f);
            var a = new TrackingNoise(0.02f, 4242, 0.01f);
            var b = new TrackingNoise(0.02f, 4242, 0.01f);

            for (int i = 0; i < 20; i++)
            {
                PoseFrame fa = a.Apply(src), fb = b.Apply(src);
                for (int j = 0; j < JointInfo.Count; j++)
                    Assert.That(Vector3.Distance(fa.Joints[j], fb.Joints[j]), Is.LessThan(1e-6f),
                                $"frame {i} joint {j} diverged — analysis would not be reproducible");
            }
        }

        [Test]
        public void Different_seeds_produce_different_noise()
        {
            PoseFrame src = Frame(0.0, 0f);
            PoseFrame fa = new TrackingNoise(0.02f, 1).Apply(src);
            PoseFrame fb = new TrackingNoise(0.02f, 2).Apply(src);

            float diff = 0f;
            for (int j = 0; j < JointInfo.Count; j++) diff += Vector3.Distance(fa.Joints[j], fb.Joints[j]);
            Assert.That(diff, Is.GreaterThan(1e-4f));
        }

        [Test]
        public void Jitter_magnitude_scales_with_sigma()
        {
            PoseFrame src = Frame(0.0, 0f);
            foreach (float sigma in new[] { 0.01f, 0.05f })
            {
                var noise = new TrackingNoise(sigma, 99);
                double sum = 0; int n = 0;
                for (int i = 0; i < 400; i++)
                {
                    PoseFrame f = noise.Apply(src);
                    for (int j = 0; j < JointInfo.Count; j++)
                    {
                        sum += Vector3.Distance(f.Joints[j], src.Joints[j]);
                        n++;
                    }
                }
                double meanErr = sum / n;
                // Mean magnitude of a 3D Gaussian ≈ 1.6σ; allow a generous band.
                Assert.That(meanErr, Is.GreaterThan(sigma * 1.0).And.LessThan(sigma * 2.4),
                            $"sigma {sigma}: mean displacement {meanErr:F4} m out of range");
            }
        }

        [Test]
        public void Bias_offsets_every_frame_by_a_constant()
        {
            PoseFrame src = Frame(0.0, 0f);
            var noise = new TrackingNoise(0f, 7, 0.05f);   // bias only, no jitter

            PoseFrame f1 = noise.Apply(src);
            PoseFrame f2 = noise.Apply(src);
            for (int j = 0; j < JointInfo.Count; j++)
            {
                // Same offset every frame...
                Assert.That(Vector3.Distance(f1.Joints[j], f2.Joints[j]), Is.LessThan(1e-6f));
                // ...of the requested magnitude.
                Assert.That(Vector3.Distance(f1.Joints[j], src.Joints[j]), Is.EqualTo(0.05f).Within(1e-3f));
            }
        }

        [Test]
        public void Warning_fidelity_degrades_as_tracking_error_grows()
        {
            SyntheticBlock.Data demo = SyntheticBlock.Demo();
            var clean = new List<PoseFrame>(demo.Frames);
            var oracle = new OracleParams();
            var detector = new DetectorParams();

            FidelityResult perfect = WarningFidelity.Evaluate(
                Condition.PB, clean, clean, demo.Obstacles, demo.Limbs, oracle, detector, 0f);

            // Perfect tracking must warn about the approaches that actually happened.
            Assert.That(perfect.Events, Is.GreaterThan(0), "the synthetic block should contain approaches");
            Assert.That(perfect.Detected, Is.GreaterThan(0), "perfect tracking should detect approaches");

            // Gross tracking error (0.5 m) must measurably damage the warning signal: fewer approaches
            // correctly warned about, and/or more alerts that correspond to nothing.
            var noise = new TrackingNoise(0.5f, 11);
            List<PoseFrame> wrecked = noise.Apply(clean);
            FidelityResult degraded = WarningFidelity.Evaluate(
                Condition.PB, clean, wrecked, demo.Obstacles, demo.Limbs, oracle, detector, 0.5f);

            Assert.That(degraded.Detected <= perfect.Detected || degraded.FalseAlarms > perfect.FalseAlarms,
                        Is.True, "0.5 m of tracking error should degrade detection or inflate false alarms");
            Assert.That(degraded.Events, Is.EqualTo(perfect.Events),
                        "ground truth comes from the CLEAN stream and must not change with injected noise");
        }

        [Test]
        public void Sweep_returns_one_row_per_level_and_repeat()
        {
            SyntheticBlock.Data demo = SyntheticBlock.Demo();
            var sigmas = new[] { 0f, 0.02f, 0.05f };
            List<FidelityResult> rows = WarningFidelity.Sweep(
                Condition.RB, new List<PoseFrame>(demo.Frames), demo.Obstacles, demo.Limbs,
                new OracleParams(), new DetectorParams(), sigmas, repeats: 3);

            Assert.That(rows, Has.Count.EqualTo(sigmas.Length * 3));
            Assert.That(FidelityResult.CsvHeader(), Does.StartWith("condition,sigma_m"));
            foreach (FidelityResult r in rows) Assert.That(r.ToCsvRow(), Is.Not.Empty);
        }
    }

    /// <summary>The new warning-acceptability instrument [change 0.4].</summary>
    public class CueAcceptabilityTests
    {
        [Test]
        public void Has_four_items_and_reverse_keys_annoyance()
        {
            Questionnaire q = Questionnaire.CueAcceptability();
            Assert.That(q.Instrument, Is.EqualTo("CUE"));
            Assert.That(q.Items, Has.Count.EqualTo(4));

            var responses = new Dictionary<string, int>
            {
                { "HELPFUL", 6 }, { "TIMELY", 6 }, { "TRUST", 6 }, { "ANNOYING", 0 },
            };
            Dictionary<string, float> best = q.Score(responses);

            // ANNOYING is reverse-keyed, so "not at all annoying" (0) scores 6 — everything points the same way.
            Assert.That(best["annoying"], Is.EqualTo(6f).Within(1e-4f));
            Assert.That(best["acceptability"], Is.EqualTo(6f).Within(1e-4f));
        }

        [Test]
        public void Worst_case_scores_zero_and_composite_averages()
        {
            Questionnaire q = Questionnaire.CueAcceptability();

            var worst = new Dictionary<string, int>
            {
                { "HELPFUL", 0 }, { "TIMELY", 0 }, { "TRUST", 0 }, { "ANNOYING", 6 },
            };
            Assert.That(q.Score(worst)["acceptability"], Is.EqualTo(0f).Within(1e-4f));

            var mixed = new Dictionary<string, int>
            {
                { "HELPFUL", 6 }, { "TIMELY", 0 }, { "TRUST", 6 }, { "ANNOYING", 3 },
            };
            // (6 + 0 + 6 + 3) / 4 = 3.75
            Assert.That(q.Score(mixed)["acceptability"], Is.EqualTo(3.75f).Within(1e-3f));
            Assert.That(q.AllAnswered(mixed), Is.True);
        }
    }
}
