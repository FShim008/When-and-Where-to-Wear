using System;
using System.Collections.Generic;

namespace CollisionFeedback.Core
{
    /// <summary>How well a condition's warning signal survived a given level of tracking error.</summary>
    public readonly struct FidelityResult
    {
        public readonly Condition Condition;
        public readonly float Sigma;              // jitter σ (m) the warnings were generated under
        public readonly int Alerts;               // alerts fired from the DEGRADED tracking
        public readonly int Events;               // ground-truth hazard approaches (from CLEAN tracking)
        public readonly int Detected;             // approaches preceded by an alert on the same limb
        public readonly int Missed;               // approaches with no qualifying alert
        public readonly int FalseAlarms;          // alerts matched to no approach
        public readonly double MeanLeadSeconds;   // mean (approach time − alert time) over detected approaches
        public readonly double MinLeadSeconds;

        public FidelityResult(Condition condition, float sigma, int alerts, int events, int detected,
                              int missed, int falseAlarms, double meanLead, double minLead)
        {
            Condition = condition; Sigma = sigma; Alerts = alerts; Events = events; Detected = detected;
            Missed = missed; FalseAlarms = falseAlarms; MeanLeadSeconds = meanLead; MinLeadSeconds = minLead;
        }

        /// <summary>Fraction of real approaches that got a warning (0..1). The headline robustness number.</summary>
        public double DetectionRate => Events > 0 ? (double)Detected / Events : double.NaN;

        /// <summary>Fraction of alerts that corresponded to no real approach (0..1).</summary>
        public double FalseAlarmRate => Alerts > 0 ? (double)FalseAlarms / Alerts : double.NaN;

        public static string CsvHeader() =>
            "condition,sigma_m,alerts,events,detected,missed,false_alarms,detection_rate,false_alarm_rate,mean_lead_s,min_lead_s";

        public string ToCsvRow()
        {
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            return string.Join(",",
                Condition, Sigma.ToString("0.####", inv), Alerts, Events, Detected, Missed, FalseAlarms,
                DetectionRate.ToString("0.####", inv), FalseAlarmRate.ToString("0.####", inv),
                MeanLeadSeconds.ToString("0.####", inv), MinLeadSeconds.ToString("0.####", inv));
        }
    }

    /// <summary>
    /// Offline robustness analysis: how does a condition's WARNING SIGNAL degrade as tracking error grows?
    ///
    /// THE DESIGN, and its honest limit. Warnings are generated from <b>degraded</b> tracking (what a real
    /// system would have seen); ground truth comes from the <b>clean</b> recording (what the body actually did).
    /// That asymmetry is the whole point — it asks "would this system still have warned in time?" without
    /// pretending to know how the participant would have moved differently. Replay cannot simulate
    /// counterfactual behaviour, so this measures <b>warning fidelity, not collision reduction</b>. Report it
    /// that way: *"the predictive lead time and detection rate hold up to X cm of tracking error"* — never
    /// *"collisions would still have dropped."*
    ///
    /// Matching rule: a ground-truth approach is DETECTED if an alert fired on the same limb within
    /// <c>maxLeadSeconds</c> before it. Lead time = approach time − alert time. Alerts matched to nothing are
    /// false alarms. Pure Core, deterministic, hardware-free.
    /// </summary>
    public static class WarningFidelity
    {
        /// <summary>
        /// Evaluate one condition at one noise level.
        /// </summary>
        /// <param name="clean">the recorded frames (ground truth for what the body did)</param>
        /// <param name="noisy">the same frames with tracking error injected (what the warning logic sees)</param>
        /// <param name="maxLeadSeconds">how far before an approach an alert still counts as warning about it</param>
        public static FidelityResult Evaluate(Condition condition,
                                              IReadOnlyList<PoseFrame> clean,
                                              IReadOnlyList<PoseFrame> noisy,
                                              IReadOnlyList<Obstacle> obstacles,
                                              IReadOnlyList<Joint> limbs,
                                              OracleParams oracleParams,
                                              DetectorParams detectorParams,
                                              float sigma,
                                              double maxLeadSeconds = 3.0)
        {
            // 1) Warnings, from the DEGRADED stream.
            var sink = new CountingSink(null);
            var conditions = new ConditionManager(condition, oracleParams, obstacles, limbs, sink);
            for (int i = 0; i < noisy.Count; i++) conditions.Tick(noisy[i]);
            IReadOnlyList<FeedbackCommand> alerts = sink.Commands;

            // 2) Ground-truth approaches, from the CLEAN stream.
            var detector = new CollisionDetector(obstacles, limbs, detectorParams);
            for (int i = 0; i < clean.Count; i++) detector.Tick(clean[i]);
            if (clean.Count > 0) detector.Flush(clean[clean.Count - 1].Timestamp);
            IReadOnlyList<OutcomeEvent> events = detector.Events;

            // 3) Match each approach to the latest qualifying alert on the same limb.
            var usedAlert = new bool[alerts.Count];
            int detected = 0;
            double leadSum = 0;
            double minLead = double.PositiveInfinity;

            for (int e = 0; e < events.Count; e++)
            {
                OutcomeEvent ev = events[e];
                int best = -1;
                double bestLead = 0;
                for (int a = 0; a < alerts.Count; a++)
                {
                    if (usedAlert[a] || alerts[a].Limb != ev.Limb) continue;
                    double lead = ev.DataTime - alerts[a].DataTime;
                    if (lead < 0 || lead > maxLeadSeconds) continue;
                    if (best < 0 || lead < bestLead) { best = a; bestLead = lead; } // the most recent warning
                }
                if (best >= 0)
                {
                    usedAlert[best] = true;
                    detected++;
                    leadSum += bestLead;
                    if (bestLead < minLead) minLead = bestLead;
                }
            }

            int falseAlarms = 0;
            for (int a = 0; a < alerts.Count; a++) if (!usedAlert[a]) falseAlarms++;

            return new FidelityResult(
                condition, sigma, alerts.Count, events.Count, detected, events.Count - detected, falseAlarms,
                detected > 0 ? leadSum / detected : double.NaN,
                detected > 0 ? minLead : double.NaN);
        }

        /// <summary>
        /// Sweep a set of noise levels for one condition, averaging over <paramref name="repeats"/> noise seeds
        /// so a single unlucky draw cannot drive the curve. Returns one row per (level × repeat).
        /// </summary>
        public static List<FidelityResult> Sweep(Condition condition,
                                                 IReadOnlyList<PoseFrame> clean,
                                                 IReadOnlyList<Obstacle> obstacles,
                                                 IReadOnlyList<Joint> limbs,
                                                 OracleParams oracleParams,
                                                 DetectorParams detectorParams,
                                                 IReadOnlyList<float> sigmas,
                                                 int repeats = 5,
                                                 int seed = 20260811,
                                                 float biasMagnitude = 0f,
                                                 double maxLeadSeconds = 3.0)
        {
            var results = new List<FidelityResult>();
            for (int s = 0; s < sigmas.Count; s++)
            {
                for (int r = 0; r < Math.Max(1, repeats); r++)
                {
                    var noise = new TrackingNoise(sigmas[s], seed + s * 1000 + r, biasMagnitude);
                    List<PoseFrame> noisy = noise.Apply(clean);
                    results.Add(Evaluate(condition, clean, noisy, obstacles, limbs,
                                         oracleParams, detectorParams, sigmas[s], maxLeadSeconds));
                }
            }
            return results;
        }
    }
}
