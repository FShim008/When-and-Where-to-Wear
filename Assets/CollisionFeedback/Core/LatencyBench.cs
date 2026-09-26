using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace CollisionFeedback.Core
{
    /// <summary>One paired activation: a cue command and the vibration it produced.</summary>
    public readonly struct LatencySample
    {
        /// <summary>When the haptic command was issued, on the Unity clock.</summary>
        public readonly double CommandTime;
        /// <summary>When the sensor saw the tactor move, on the sensor clock after alignment.</summary>
        public readonly double SensedTime;
        public readonly int Index;

        public LatencySample(int index, double commandTime, double sensedTime)
        {
            Index = index; CommandTime = commandTime; SensedTime = sensedTime;
        }

        public double DelaySeconds => SensedTime - CommandTime;
    }

    /// <summary>
    /// Pairs issued cue commands with sensed vibration onsets [PAPER2_STUDY_DESIGN §5].
    ///
    /// **Why pairing is unambiguous here, and why that is by design.** The bench fires with *randomised* gaps
    /// of seconds while the latency it measures is tens of milliseconds. So the correct partner for a command
    /// is simply the first onset after it, and no onset can plausibly belong to two commands. A fixed firing
    /// period would not have this property — a dropped activation would silently shift every later pair by
    /// one and the mean would look fine. The randomised gap is a correctness property, not a nicety.
    /// </summary>
    public static class LatencyPairing
    {
        /// <summary>
        /// Greedy nearest-after pairing. Each sensed onset is consumed at most once.
        /// </summary>
        /// <param name="commandTimes">Command times, ascending.</param>
        /// <param name="sensedTimes">Sensed onsets on the same timebase, ascending.</param>
        /// <param name="maxDelaySeconds">
        /// Widest delay still treated as belonging to a command. Generous by default: too tight silently
        /// discards the slow tail, which is the part that matters most.
        /// </param>
        /// <param name="unmatchedCommands">Commands that produced no onset — dropped cues.</param>
        /// <param name="spuriousOnsets">Onsets belonging to no command — sensor noise, or a threshold set too low.</param>
        public static List<LatencySample> Pair(IReadOnlyList<double> commandTimes,
                                               IReadOnlyList<double> sensedTimes,
                                               double maxDelaySeconds,
                                               out int unmatchedCommands,
                                               out int spuriousOnsets)
        {
            var samples = new List<LatencySample>();
            unmatchedCommands = 0;
            int consumed = 0;
            int s = 0;

            for (int c = 0; c < commandTimes.Count; c++)
            {
                double t = commandTimes[c];
                while (s < sensedTimes.Count && sensedTimes[s] <= t) s++;   // onsets before this command

                if (s < sensedTimes.Count && sensedTimes[s] - t <= maxDelaySeconds)
                {
                    samples.Add(new LatencySample(c, t, sensedTimes[s]));
                    s++; consumed++;
                }
                else unmatchedCommands++;
            }

            spuriousOnsets = Math.Max(0, sensedTimes.Count - consumed);
            return samples;
        }
    }

    /// <summary>Verdict against the §5 jitter tolerance, mirrored in docs/PAPER2_RUN_BOOK.md §2.5.</summary>
    public enum JitterVerdict
    {
        /// <summary>SD ≤ 10 ms — roughly 5% of the shortest lead. Invisible in the fit.</summary>
        Good,
        /// <summary>SD ≤ 30 ms — usable, but report it and widen the reported interval.</summary>
        Acceptable,
        /// <summary>SD &gt; 30 ms — report and widen; approaching the point where the curve flattens.</summary>
        Marginal,
        /// <summary>SD &gt; 50 ms — do not collect. Fix the hardware path first.</summary>
        Unusable,
    }

    /// <summary>
    /// Latency and jitter over a bench run [PAPER2_STUDY_DESIGN §5; §15 gate 2].
    ///
    /// **Two numbers, and they are not interchangeable.** <see cref="MeanSeconds"/> is compensated away by
    /// `E2Params.PipelineLatencySeconds` — the runner fires that much early so the vibration lands on time.
    /// <see cref="SdSeconds"/> cannot be compensated at all: it is an error bar on the x-axis of the
    /// psychometric fit, and large jitter smears the curve into a falsely shallow slope.
    /// </summary>
    public readonly struct LatencySummary
    {
        public readonly int Count;
        public readonly int UnmatchedCommands;
        public readonly int SpuriousOnsets;
        public readonly double MeanSeconds;
        public readonly double SdSeconds;
        public readonly double MedianSeconds;
        public readonly double P05Seconds;
        public readonly double P95Seconds;
        public readonly double MinSeconds;
        public readonly double MaxSeconds;

        public LatencySummary(int count, int unmatched, int spurious, double mean, double sd,
                              double median, double p05, double p95, double min, double max)
        {
            Count = count; UnmatchedCommands = unmatched; SpuriousOnsets = spurious;
            MeanSeconds = mean; SdSeconds = sd; MedianSeconds = median;
            P05Seconds = p05; P95Seconds = p95; MinSeconds = min; MaxSeconds = max;
        }

        /// <summary>
        /// Right-tail asymmetry: (P95 − median) / (median − P05). Near 1 is symmetric.
        ///
        /// **Read this as well as the SD.** A long right tail — Bluetooth retries are the usual cause — is
        /// worse than a symmetric spread of the same width, because it drags individual trials badly late
        /// rather than scattering them evenly. Above ~2, prefer a wired path.
        /// </summary>
        public double TailAsymmetry
        {
            get
            {
                double lower = MedianSeconds - P05Seconds;
                double upper = P95Seconds - MedianSeconds;
                // A tight bulk with a few late outliers — the exact signature of Bluetooth retries — drives
                // the lower spread to zero. Returning NaN there would blind the metric in the one case it
                // exists to catch, so a one-sided spread reports as maximally asymmetric instead.
                if (lower <= 1e-9) return upper <= 1e-9 ? double.NaN : double.PositiveInfinity;
                return upper / lower;
            }
        }

        public JitterVerdict Verdict =>
            SdSeconds > 0.050 ? JitterVerdict.Unusable :
            SdSeconds > 0.030 ? JitterVerdict.Marginal :
            SdSeconds > 0.010 ? JitterVerdict.Acceptable : JitterVerdict.Good;

        /// <summary>Dropped-cue rate. A cue that never fired is a missing trial, not a slow one.</summary>
        public double DropRate =>
            Count + UnmatchedCommands == 0 ? 0.0 : (double)UnmatchedCommands / (Count + UnmatchedCommands);

        public static LatencySummary From(IReadOnlyList<LatencySample> samples,
                                          int unmatchedCommands = 0, int spuriousOnsets = 0)
        {
            int n = samples.Count;
            if (n == 0) return new LatencySummary(0, unmatchedCommands, spuriousOnsets,
                                                  double.NaN, double.NaN, double.NaN,
                                                  double.NaN, double.NaN, double.NaN, double.NaN);

            var d = new double[n];
            for (int i = 0; i < n; i++) d[i] = samples[i].DelaySeconds;

            double sum = 0;
            for (int i = 0; i < n; i++) sum += d[i];
            double mean = sum / n;

            double ss = 0;
            for (int i = 0; i < n; i++) { double e = d[i] - mean; ss += e * e; }
            double sd = n > 1 ? Math.Sqrt(ss / (n - 1)) : 0.0;

            Array.Sort(d);
            return new LatencySummary(n, unmatchedCommands, spuriousOnsets, mean, sd,
                                      Percentile(d, 0.50), Percentile(d, 0.05), Percentile(d, 0.95),
                                      d[0], d[n - 1]);
        }

        /// <summary>Linear-interpolated percentile on an ascending array.</summary>
        internal static double Percentile(double[] sorted, double p)
        {
            if (sorted.Length == 0) return double.NaN;
            if (sorted.Length == 1) return sorted[0];
            double pos = p * (sorted.Length - 1);
            int lo = (int)Math.Floor(pos);
            int hi = (int)Math.Ceiling(pos);
            if (lo == hi) return sorted[lo];
            return sorted[lo] + (pos - lo) * (sorted[hi] - sorted[lo]);
        }

        public string Describe()
        {
            if (Count == 0) return "No paired activations — nothing was measured.";
            var sb = new StringBuilder();
            sb.Append($"n={Count}  mean={MeanSeconds * 1000:F1} ms  SD={SdSeconds * 1000:F1} ms  ")
              .Append($"median={MedianSeconds * 1000:F1}  P05–P95={P05Seconds * 1000:F1}–{P95Seconds * 1000:F1}  ")
              .Append($"range={MinSeconds * 1000:F1}–{MaxSeconds * 1000:F1} ms  [{Verdict}]");
            if (UnmatchedCommands > 0) sb.Append($"  DROPPED={UnmatchedCommands} ({DropRate * 100:F1}%)");
            if (SpuriousOnsets > 0) sb.Append($"  spurious onsets={SpuriousOnsets}");
            double tail = TailAsymmetry;
            if (!double.IsNaN(tail) && tail > 2.0)
                sb.Append($"  ⚠ right tail ×{tail:F1} — prefer a wired path");
            return sb.ToString();
        }

        /// <summary>The value to paste into <c>E2SessionRunner.pipelineLatencySeconds</c>.</summary>
        public string Recommendation()
        {
            if (Count == 0) return "No data.";
            switch (Verdict)
            {
                case JitterVerdict.Unusable:
                    return $"DO NOT COLLECT. Jitter SD {SdSeconds * 1000:F0} ms exceeds 50 ms — the curve " +
                           "would be smeared into a falsely shallow slope. Fix the hardware path first.";
                case JitterVerdict.Marginal:
                    return $"Set pipelineLatencySeconds = {MeanSeconds:F4}. Jitter SD {SdSeconds * 1000:F0} ms " +
                           "is high: report it and widen the reported interval (§16.2).";
                default:
                    return $"Set pipelineLatencySeconds = {MeanSeconds:F4}  (mean {MeanSeconds * 1000:F1} ms, " +
                           $"jitter SD {SdSeconds * 1000:F1} ms).";
            }
        }
    }

    /// <summary>
    /// `timing_calibration.csv` [PAPER2_STUDY_DESIGN §14]: "command and sensed physical onset for bench/day
    /// checks; latency and jitter summary". Pure and testable; a Runtime/Integration writer persists it.
    /// </summary>
    public static class LatencyCalibrationFormatter
    {
        public const string HeaderLine =
            "index,command_time_s,sensed_time_s,delay_s,delay_ms";

        public static string Header() => HeaderLine;

        public static string Row(in LatencySample s)
        {
            var inv = CultureInfo.InvariantCulture;
            return string.Join(",",
                s.Index.ToString(inv),
                s.CommandTime.ToString("F6", inv),
                s.SensedTime.ToString("F6", inv),
                s.DelaySeconds.ToString("F6", inv),
                (s.DelaySeconds * 1000.0).ToString("F2", inv));
        }

        public static IEnumerable<string> Rows(IReadOnlyList<LatencySample> samples)
        {
            for (int i = 0; i < samples.Count; i++) yield return Row(samples[i]);
        }

        /// <summary>
        /// Comment block written above the rows. §5 requires the provenance, not just the number: a latency
        /// value without the device and firmware it was measured on cannot be checked against the day it was
        /// used, which is the whole point of the daily re-check.
        /// </summary>
        public static string ProvenanceBlock(in LatencySummary summary, string device, string firmware,
                                             string unityVersion, string transport, string utcTimestamp,
                                             double displayLatencySeconds)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# timing_calibration.csv — PAPER2_STUDY_DESIGN §5, §14; §15 gate 2");
            sb.AppendLine("# " + summary.Describe());
            sb.AppendLine("# " + summary.Recommendation());
            sb.AppendLine($"# measured_utc={utcTimestamp}");
            sb.AppendLine($"# device={device}  firmware={firmware}  transport={transport}");
            sb.AppendLine($"# unity={unityVersion}");
            sb.AppendLine($"# display_latency_correction_s={displayLatencySeconds.ToString("F4", CultureInfo.InvariantCulture)}");
            sb.AppendLine("# NOTE: delay_s is command -> sensed vibration onset, already corrected for the");
            sb.AppendLine("#       display latency above when a screen flash was used as the command marker.");
            return sb.ToString();
        }
    }
}
