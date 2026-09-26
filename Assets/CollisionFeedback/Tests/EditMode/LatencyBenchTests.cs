using System.Collections.Generic;
using NUnit.Framework;
using CollisionFeedback.Core;

namespace CollisionFeedback.Tests
{
    /// <summary>
    /// The latency bench [PAPER2_STUDY_DESIGN §5; §15 gate 2].
    ///
    /// This measurement decides `pipelineLatencySeconds`, which shifts every realized lead time and therefore
    /// LT80 itself. A pairing bug here would not announce itself — it would produce a plausible-looking mean
    /// that is wrong by one activation's gap. Hence the tests.
    /// </summary>
    public class LatencyBenchTests
    {
        private static List<double> Commands(int n, double gap = 3.0, double t0 = 10.0)
        {
            var v = new List<double>(n);
            for (int i = 0; i < n; i++) v.Add(t0 + i * gap);
            return v;
        }

        [Test]
        public void Pairs_each_command_with_the_onset_that_followed_it()
        {
            var cmds = Commands(5);
            var sensed = new List<double>();
            foreach (double c in cmds) sensed.Add(c + 0.058);

            var pairs = LatencyPairing.Pair(cmds, sensed, 0.5, out int unmatched, out int spurious);

            Assert.That(pairs.Count, Is.EqualTo(5));
            Assert.That(unmatched, Is.Zero);
            Assert.That(spurious, Is.Zero);
            foreach (var p in pairs) Assert.That(p.DelaySeconds, Is.EqualTo(0.058).Within(1e-9));
        }

        [Test]
        public void A_dropped_cue_is_reported_and_does_not_shift_every_later_pair()
        {
            // THE FAILURE THIS GUARDS AGAINST. If a dropped activation silently re-paired, every subsequent
            // command would take the NEXT command's onset and the mean would come out near one full gap —
            // wrong by ~3 s, but perfectly self-consistent and easy to miss.
            var cmds = Commands(5);
            var sensed = new List<double>();
            for (int i = 0; i < cmds.Count; i++)
            {
                if (i == 2) continue;                 // the tactor never fired on this one
                sensed.Add(cmds[i] + 0.058);
            }

            var pairs = LatencyPairing.Pair(cmds, sensed, 0.5, out int unmatched, out int spurious);

            Assert.That(pairs.Count, Is.EqualTo(4));
            Assert.That(unmatched, Is.EqualTo(1), "the dropped cue must be counted, not absorbed");
            Assert.That(spurious, Is.Zero);
            foreach (var p in pairs)
                Assert.That(p.DelaySeconds, Is.EqualTo(0.058).Within(1e-9),
                    "a dropped cue must not drag later pairs onto the wrong onset");
        }

        [Test]
        public void Sensor_noise_between_activations_is_reported_as_spurious()
        {
            var cmds = Commands(3);
            var sensed = new List<double> { cmds[0] + 0.05, cmds[0] + 1.5, cmds[1] + 0.05, cmds[2] + 0.05 };

            var pairs = LatencyPairing.Pair(cmds, sensed, 0.5, out int unmatched, out int spurious);

            Assert.That(pairs.Count, Is.EqualTo(3));
            Assert.That(unmatched, Is.Zero);
            Assert.That(spurious, Is.EqualTo(1), "an onset belonging to no command means the threshold is too low");
        }

        [Test]
        public void An_onset_beyond_the_window_counts_as_a_drop_not_a_slow_trial()
        {
            var cmds = Commands(2);
            var sensed = new List<double> { cmds[0] + 0.9, cmds[1] + 0.05 };   // 0.9 s is not latency

            var pairs = LatencyPairing.Pair(cmds, sensed, 0.5, out int unmatched, out int spurious);

            Assert.That(pairs.Count, Is.EqualTo(1));
            Assert.That(unmatched, Is.EqualTo(1));
            Assert.That(spurious, Is.EqualTo(1));
        }

        [Test]
        public void Summary_recovers_a_known_mean_and_sd()
        {
            // Delays 50, 55, 60, 65, 70 ms -> mean 60, sample SD 7.906 ms.
            var samples = new List<LatencySample>();
            double[] delays = { 0.050, 0.055, 0.060, 0.065, 0.070 };
            for (int i = 0; i < delays.Length; i++)
                samples.Add(new LatencySample(i, i * 3.0, i * 3.0 + delays[i]));

            LatencySummary s = LatencySummary.From(samples);

            Assert.That(s.Count, Is.EqualTo(5));
            Assert.That(s.MeanSeconds, Is.EqualTo(0.060).Within(1e-9));
            Assert.That(s.SdSeconds, Is.EqualTo(0.0079057).Within(1e-6));
            Assert.That(s.MedianSeconds, Is.EqualTo(0.060).Within(1e-9));
            Assert.That(s.MinSeconds, Is.EqualTo(0.050).Within(1e-9));
            Assert.That(s.MaxSeconds, Is.EqualTo(0.070).Within(1e-9));
        }

        [Test]
        public void Verdict_follows_the_jitter_tolerance()
        {
            Assert.That(Verdict(0.005), Is.EqualTo(JitterVerdict.Good));
            Assert.That(Verdict(0.020), Is.EqualTo(JitterVerdict.Acceptable));
            Assert.That(Verdict(0.040), Is.EqualTo(JitterVerdict.Marginal));
            Assert.That(Verdict(0.080), Is.EqualTo(JitterVerdict.Unusable));
        }

        /// <summary>Builds a run whose delays have the requested SD, then reads the verdict back.</summary>
        private static JitterVerdict Verdict(double targetSd)
        {
            // Two points at mean ± sd give a sample SD of exactly sd*sqrt(2)/sqrt(1)... use a symmetric
            // 3-point set instead: {m-s*k, m, m+s*k} with k chosen so the sample SD equals s.
            double m = 0.060, k = System.Math.Sqrt(1.0);   // sample SD of {m-s, m, m+s} is s
            var samples = new List<LatencySample>
            {
                new LatencySample(0, 0, m - targetSd * k),
                new LatencySample(1, 3, 3 + m),
                new LatencySample(2, 6, 6 + m + targetSd * k),
            };
            return LatencySummary.From(samples).Verdict;
        }

        [Test]
        public void Unusable_jitter_refuses_to_hand_back_a_latency_value()
        {
            var samples = new List<LatencySample>
            {
                new LatencySample(0, 0, 0.000),
                new LatencySample(1, 3, 3 + 0.060),
                new LatencySample(2, 6, 6 + 0.120),
            };
            LatencySummary s = LatencySummary.From(samples);

            Assert.That(s.Verdict, Is.EqualTo(JitterVerdict.Unusable));
            Assert.That(s.Recommendation(), Does.Contain("DO NOT COLLECT"),
                "a bench run this noisy must not quietly produce a number to paste in");
            Assert.That(s.Recommendation(), Does.Not.Contain("Set pipelineLatencySeconds"));
        }

        [Test]
        public void A_long_right_tail_is_flagged_even_when_the_SD_looks_acceptable()
        {
            // Bluetooth retries: most activations tight, a few dragged badly late. The SD alone understates
            // how damaging this is, because it drags individual trials rather than scattering them evenly.
            // Realistic bulk: tight but not identical, plus a handful dragged badly late.
            var samples = new List<LatencySample>();
            for (int i = 0; i < 40; i++)
                samples.Add(new LatencySample(i, i * 3.0, i * 3.0 + 0.050 + (i % 5) * 0.0005));
            for (int i = 40; i < 45; i++)
                samples.Add(new LatencySample(i, i * 3.0, i * 3.0 + 0.110));

            LatencySummary s = LatencySummary.From(samples);

            Assert.That(s.TailAsymmetry, Is.GreaterThan(2.0), "the right tail must be detectable");
            Assert.That(s.Describe(), Does.Contain("right tail"));
        }

        [Test]
        public void A_perfectly_tight_bulk_with_late_outliers_reads_as_maximally_asymmetric()
        {
            // The degenerate version of the case above: the lower spread is exactly zero. Reporting NaN here
            // would blind the metric in precisely the situation it exists to catch.
            var samples = new List<LatencySample>();
            for (int i = 0; i < 40; i++) samples.Add(new LatencySample(i, i * 3.0, i * 3.0 + 0.050));
            for (int i = 40; i < 45; i++) samples.Add(new LatencySample(i, i * 3.0, i * 3.0 + 0.110));

            LatencySummary s = LatencySummary.From(samples);

            Assert.That(double.IsPositiveInfinity(s.TailAsymmetry), Is.True);
            Assert.That(s.Describe(), Does.Contain("right tail"));
        }

        [Test]
        public void A_constant_delay_has_no_asymmetry_to_report()
        {
            var samples = new List<LatencySample>();
            for (int i = 0; i < 10; i++) samples.Add(new LatencySample(i, i * 3.0, i * 3.0 + 0.050));

            LatencySummary s = LatencySummary.From(samples);

            Assert.That(double.IsNaN(s.TailAsymmetry), Is.True, "no spread either side means the ratio is undefined");
            Assert.That(s.Describe(), Does.Not.Contain("right tail"));
            Assert.That(s.Verdict, Is.EqualTo(JitterVerdict.Good));
        }

        [Test]
        public void Drop_rate_is_reported_as_a_fraction_of_attempted_activations()
        {
            var samples = new List<LatencySample>();
            for (int i = 0; i < 9; i++) samples.Add(new LatencySample(i, i * 3.0, i * 3.0 + 0.05));
            LatencySummary s = LatencySummary.From(samples, unmatchedCommands: 1);

            Assert.That(s.DropRate, Is.EqualTo(0.10).Within(1e-9));
            Assert.That(s.Describe(), Does.Contain("DROPPED"));
        }

        [Test]
        public void Empty_run_summarises_without_throwing()
        {
            LatencySummary s = LatencySummary.From(new List<LatencySample>());
            Assert.That(s.Count, Is.Zero);
            Assert.That(s.Describe(), Does.Contain("nothing was measured"));
            Assert.That(s.Recommendation(), Does.Contain("No data"));
        }

        [Test]
        public void Csv_rows_carry_the_delay_in_both_units()
        {
            var s = new LatencySample(7, 12.5, 12.558);
            string row = LatencyCalibrationFormatter.Row(s);

            Assert.That(row, Does.StartWith("7,"));
            Assert.That(row, Does.Contain("0.058000"));
            Assert.That(row, Does.Contain("58.00"), "milliseconds are the units the operator reads");
            Assert.That(LatencyCalibrationFormatter.Header().Split(',').Length,
                        Is.EqualTo(row.Split(',').Length), "header and row must have equal width");
        }

        [Test]
        public void Provenance_block_records_what_the_measurement_was_made_on()
        {
            var samples = new List<LatencySample> { new LatencySample(0, 0, 0.058) };
            LatencySummary s = LatencySummary.From(samples);

            string p = LatencyCalibrationFormatter.ProvenanceBlock(
                s, "TactSuit X40", "fw-2.1.3", "6000.3.16f1", "USB", "2026-09-15T10:00:00Z", 0.012);

            // §5 wants provenance, not just a number: a latency without its device cannot be checked
            // against the day it was used, which is the point of the daily re-check.
            Assert.That(p, Does.Contain("TactSuit X40"));
            Assert.That(p, Does.Contain("fw-2.1.3"));
            Assert.That(p, Does.Contain("USB"));
            Assert.That(p, Does.Contain("6000.3.16f1"));
            Assert.That(p, Does.Contain("display_latency_correction_s=0.0120"));
            foreach (string line in p.Split('\n'))
                if (line.Trim().Length > 0)
                    Assert.That(line.TrimStart()[0], Is.EqualTo('#'), "every provenance line must be a comment");
        }
    }
}
