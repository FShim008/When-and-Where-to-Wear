using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using CollisionFeedback.Core;
using CollisionFeedback.Runtime;
using Joint = CollisionFeedback.Core.Joint; // disambiguate from UnityEngine.Joint (physics component)

namespace CollisionFeedback.Integration
{
    /// <summary>
    /// Bench harness for the haptic pipeline latency [PAPER2_STUDY_DESIGN §5; §15 gate 2 — also Paper 1
    /// gate 23, so one afternoon closes a gate on both papers].
    ///
    /// ── WHAT IT MEASURES, AND WHY IT GATES EVERYTHING ──────────────────────────────────────────────────
    /// <see cref="E2TrialRunner"/> fires the cue when predicted TTC reaches `assignedLead + pipelineLatency`,
    /// so the *vibration* lands one lead time before predicted contact. With the latency left at 0 the command
    /// goes out too late by exactly the true hardware delay, **every realized lead time is short by that
    /// amount, and the whole psychometric curve shifts** — taking LT80, the number the paper delivers, with it.
    ///
    /// Two numbers come out and they are not interchangeable. The **mean** is compensated away. The **SD**
    /// cannot be: it is an error bar on the x-axis of the fit.
    ///
    /// ── THE MARKER PROBLEM, STATED HONESTLY ────────────────────────────────────────────────────────────
    /// The hard part is putting "Unity issued the command" and "the tactor moved" on ONE clock.
    ///
    /// A serial marker would be ideal, but this project's API compatibility level is .NET Standard, where
    /// `System.IO.Ports` is unavailable — so this harness does not use one. Instead it **flashes the screen
    /// white on the exact frame it fires**, giving an external sensor (photodiode, or a 240 fps camera) a
    /// visible command marker.
    ///
    /// ⚠ **A screen flash carries the display's own latency.** The flash appears at
    /// `command + display_latency`, so a flash-to-vibration measurement reads
    /// `haptic_latency − display_latency` and **UNDERSTATES the answer**. Handle it one of three ways:
    ///
    /// 1. Measure display latency once and put it in <see cref="displayLatencySeconds"/> — it is added back.
    /// 2. Use a marker that does not go through the display (a GPIO/photodiode rig driven some other way).
    /// 3. For the **daily re-check only**, ignore it: the offset is constant, so drift is still detectable
    ///    even when the absolute value is not.
    ///
    /// Leaving it at 0 is a defensible choice *if* you record that you did — the CSV writes the correction
    /// used into its provenance block either way.
    ///
    /// ── USE ────────────────────────────────────────────────────────────────────────────────────────────
    /// Put this on an empty GameObject in a bare scene with the `[bhaptics]` prefab. No HMD, no trackers, no
    /// participant. Press Play, let it run, then paste the sensed onset times into `sensed_onsets.csv` and
    /// press the "Pair &amp; summarise" button (or re-enter Play with <see cref="pairOnStart"/> ticked).
    /// </summary>
    [DefaultExecutionOrder(-40)]
    public sealed class LatencyBenchRunner : MonoBehaviour
    {
        public enum BenchMode
        {
            /// <summary>Full characterisation. §5 requires ≥100 activations before freezing a tolerance.</summary>
            FullCharacterisation,
            /// <summary>Start-of-day drift check (§5). Short; confirms the mean has not moved.</summary>
            DailyCheck,
        }

        [Header("Mode")]
        public BenchMode mode = BenchMode.FullCharacterisation;
        [Tooltip("Activations in FullCharacterisation. §5 requires at least 100.")]
        public int activations = 120;
        [Tooltip("Activations in DailyCheck.")]
        public int dailyCheckActivations = 20;

        [Header("Firing")]
        [Tooltip("Minimum gap between activations (s). Randomised gaps are what make command/onset pairing " +
                 "unambiguous — a fixed period would let a dropped activation re-pair silently.")]
        public float minGapSeconds = 2.0f;
        public float maxGapSeconds = 4.0f;
        [Tooltip("Which tactor to drive. Use the site the study actually cues.")]
        public HapticSite site = HapticSite.RightHand;
        [Range(0f, 1f)] public float intensity = 1.0f;
        [Tooltip("Settle time before the first activation (s).")]
        public float warmupSeconds = 3f;

        [Header("Command marker")]
        [Tooltip("Flash the screen white on the frame the command is issued, for a photodiode or camera.")]
        public bool flashOnFire = true;
        [Tooltip("Flash duration (s). Long enough for a 240 fps camera to catch, short enough not to blur.")]
        public float flashSeconds = 0.05f;
        [Tooltip("Measured display latency (s), ADDED BACK to each delay. 0 = uncorrected; record that you " +
                 "left it at 0. See the class summary — a screen flash otherwise understates the answer.")]
        public float displayLatencySeconds = 0f;

        [Header("Pairing")]
        [Tooltip("Widest delay still treated as belonging to a command (s). Generous on purpose: too tight " +
                 "silently discards the slow tail, which is the part that matters most.")]
        public float maxDelaySeconds = 0.5f;
        [Tooltip("Read sensed_onsets.csv and summarise on Play instead of firing. Use after the bench run.")]
        public bool pairOnStart = false;

        [Header("Provenance — §5 wants this, not just a number")]
        public string deviceModel = "bHaptics TactSuit";
        public string firmwareVersion = "";
        [Tooltip("USB or Bluetooth. Latency is a property of the whole chain.")]
        public string transport = "USB";

        private readonly List<double> _commandTimes = new List<double>();
        private BHapticsSink _sink;
        private string _dir;
        private float _flashUntil;
        private string _status = "idle";
        private bool _running;

        private int TargetActivations =>
            mode == BenchMode.DailyCheck ? dailyCheckActivations : activations;

        private void OnEnable()
        {
            _dir = Path.Combine(Application.persistentDataPath, "bench");
            Directory.CreateDirectory(_dir);

            if (pairOnStart) { PairAndSummarise(); return; }

            if (mode == BenchMode.FullCharacterisation && activations < 100)
                Debug.LogWarning($"[LatencyBench] {activations} activations. §5 requires at least 100 before " +
                                 "freezing a jitter tolerance — the SD estimate is not stable below that.");

            if (Mathf.Approximately(displayLatencySeconds, 0f) && flashOnFire)
                Debug.LogWarning("[LatencyBench] displayLatencySeconds is 0 while using the screen flash as " +
                                 "the command marker. The measured delay will UNDERSTATE the true haptic " +
                                 "latency by the display latency. Fine for a daily drift check; record it if " +
                                 "you use it as the absolute value.");

            _sink = HapticDeviceBinding.CreateThreePulseSink(this, intensity);
            if (_sink == null)
            {
                Debug.LogError("[LatencyBench] No haptic sink — is the [bhaptics] prefab in the scene and the " +
                               "device connected? Cannot run.");
                enabled = false;
                return;
            }

            Debug.Log($"[LatencyBench] {mode}: {TargetActivations} activations on {site}, " +
                      $"gaps {minGapSeconds:F1}-{maxGapSeconds:F1}s. Output → {_dir}");
            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            _running = true;
            _status = $"warmup {warmupSeconds:F0}s";
            yield return new WaitForSeconds(warmupSeconds);

            for (int i = 0; i < TargetActivations; i++)
            {
                // Timestamp as close to the Fire() call as possible, on the same clock the flash renders on.
                double t = Time.realtimeSinceStartupAsDouble;
                _commandTimes.Add(t);
                if (flashOnFire) _flashUntil = Time.time + flashSeconds;

                _sink.Fire(new FeedbackCommand(site, Modality.Haptic, TriggerKind.Predictive,
                                               Joint.RightHand, "BENCH", t, 0f, 0f));

                _status = $"{i + 1}/{TargetActivations}";
                yield return new WaitForSeconds(Random.Range(minGapSeconds, maxGapSeconds));
            }

            WriteCommandTimes();
            _running = false;
            _status = $"done — {_commandTimes.Count} activations → {_dir}";
            Debug.Log("[LatencyBench] " + _status);
            Debug.Log("[LatencyBench] NEXT: put your sensor's onset times (one per line, seconds, same " +
                      $"timebase) in {Path.Combine(_dir, "sensed_onsets.csv")}, then tick pairOnStart and " +
                      "press Play to get the mean, SD and verdict.");
        }

        private void WriteCommandTimes()
        {
            string path = Path.Combine(_dir, "command_times.csv");
            using var w = new StreamWriter(path, false);
            w.WriteLine("# LatencyBenchRunner command times — PAPER2 §5");
            w.WriteLine($"# mode={mode} site={site} intensity={intensity.ToString("F2", CultureInfo.InvariantCulture)}");
            w.WriteLine($"# device={deviceModel} firmware={firmwareVersion} transport={transport}");
            w.WriteLine($"# unity={Application.unityVersion} utc={System.DateTime.UtcNow:o}");
            w.WriteLine("index,command_time_s");
            for (int i = 0; i < _commandTimes.Count; i++)
                w.WriteLine($"{i},{_commandTimes[i].ToString("F6", CultureInfo.InvariantCulture)}");
        }

        /// <summary>
        /// Reads `sensed_onsets.csv`, pairs it against the command times, and writes `timing_calibration.csv`
        /// (§14). All the statistics live in <see cref="LatencySummary"/> in Core, which is unit-tested —
        /// this method only moves bytes.
        /// </summary>
        [ContextMenu("Pair & summarise")]
        public void PairAndSummarise()
        {
            string cmdPath = Path.Combine(_dir ?? "", "command_times.csv");
            string sensedPath = Path.Combine(_dir ?? "", "sensed_onsets.csv");

            if (!File.Exists(cmdPath)) { Debug.LogError($"[LatencyBench] No {cmdPath}. Run the bench first."); return; }
            if (!File.Exists(sensedPath))
            {
                Debug.LogError($"[LatencyBench] No {sensedPath}. Put your sensor's onset times there — one " +
                               "per line, in seconds, on the same timebase as command_times.csv.");
                return;
            }

            List<double> commands = ReadNumbers(cmdPath, column: 1);
            List<double> sensed = ReadNumbers(sensedPath, column: 0);
            commands.Sort(); sensed.Sort();

            if (commands.Count == 0 || sensed.Count == 0)
            {
                Debug.LogError("[LatencyBench] One of the files parsed to zero rows. Check the format.");
                return;
            }

            var samples = LatencyPairing.Pair(commands, sensed, maxDelaySeconds,
                                              out int unmatched, out int spurious);

            // Add the display latency back: a screen-flash marker appears that much AFTER the command, so the
            // raw flash-to-vibration gap understates the true haptic latency by exactly this.
            if (!Mathf.Approximately(displayLatencySeconds, 0f))
            {
                var corrected = new List<LatencySample>(samples.Count);
                foreach (var s in samples)
                    corrected.Add(new LatencySample(s.Index, s.CommandTime, s.SensedTime + displayLatencySeconds));
                samples = corrected;
            }

            LatencySummary summary = LatencySummary.From(samples, unmatched, spurious);

            string outPath = Path.Combine(_dir, "timing_calibration.csv");
            using (var w = new StreamWriter(outPath, false))
            {
                w.Write(LatencyCalibrationFormatter.ProvenanceBlock(
                    summary, deviceModel, firmwareVersion, Application.unityVersion, transport,
                    System.DateTime.UtcNow.ToString("o"), displayLatencySeconds));
                w.WriteLine(LatencyCalibrationFormatter.Header());
                foreach (string row in LatencyCalibrationFormatter.Rows(samples)) w.WriteLine(row);
            }

            Debug.Log("[LatencyBench] " + summary.Describe());
            if (summary.Verdict == JitterVerdict.Unusable)
                Debug.LogError("[LatencyBench] " + summary.Recommendation());
            else if (summary.Verdict == JitterVerdict.Marginal)
                Debug.LogWarning("[LatencyBench] " + summary.Recommendation());
            else
                Debug.Log("[LatencyBench] " + summary.Recommendation());

            if (summary.DropRate > 0.02)
                Debug.LogWarning($"[LatencyBench] {summary.DropRate * 100:F1}% of activations produced no " +
                                 "vibration. A dropped cue is a missing trial, not a slow one — investigate " +
                                 "before collecting.");

            Debug.Log($"[LatencyBench] Wrote {outPath}");
            _status = summary.Describe();
        }

        private static List<double> ReadNumbers(string path, int column)
        {
            var v = new List<double>();
            foreach (string raw in File.ReadAllLines(path))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;
                string[] parts = line.Split(',');
                if (column >= parts.Length) continue;
                string cell = parts[column].Trim();
                if (double.TryParse(cell, NumberStyles.Float, CultureInfo.InvariantCulture, out double d))
                    v.Add(d);
                // Non-numeric cells are header rows; skipping silently is correct here.
            }
            return v;
        }

        private void OnGUI()
        {
            GUI.Label(new Rect(12, 12, 1200, 24), $"[LatencyBench] {_status}");

            // The command marker. Drawn in OnGUI so it lands on the same frame the command was issued.
            if (flashOnFire && _running && Time.time <= _flashUntil)
            {
                Color prev = GUI.color;
                GUI.color = Color.white;
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
                GUI.color = prev;
            }
        }

        private void OnDisable() => HapticDeviceBinding.StopAll();
    }
}
