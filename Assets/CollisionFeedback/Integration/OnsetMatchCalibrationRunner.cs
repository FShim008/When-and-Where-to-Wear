using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using CollisionFeedback.Core;
using CollisionFeedback.Runtime;

namespace CollisionFeedback.Integration
{
    /// <summary>
    /// Runs the H4' ONSET-MATCH calibration and writes the value that freezes
    /// <see cref="OracleParams.ContinuousCueMinIntensity"/>
    /// [Core/ContinuousOnsetCalibration.cs; docs/PAPER1_READINESS.md §1].
    ///
    /// WHAT IT IS FOR. PB has a sharp onset; PBC fades in from a floor. A sharp onset is more detectable
    /// than a gradual one, so without this the two conditions differ in EFFECTIVE onset as well as in cue
    /// form — and H4' would confound the thing it exists to isolate. Each trial presents PB's onset and
    /// PBC's onset in random order; the participant says which felt stronger; a PSE staircase converges on
    /// the floor at which they match.
    ///
    /// RUN IT BEFORE ANY H4' DATA, once per participant, after the E1 per-site intensity calibration
    /// (this measures a floor that is applied ON TOP of those per-site gains).
    ///
    /// Drop on a GameObject with the bHaptics SDK initialised. Operator keys, IMGUI so it works with either
    /// input backend (matching <c>OperatorEStop</c> and <c>E2SessionRunner</c>):
    ///   SPACE  begin
    ///   1 / 2  "the FIRST / SECOND interval felt stronger"
    ///   R      replay the current trial
    ///   Esc    abort and write whatever converged
    /// </summary>
    public sealed class OnsetMatchCalibrationRunner : MonoBehaviour
    {
        [Header("Session")]
        [SerializeField] private int participantId = 0;
        [SerializeField] private string outputFile = "onset_match.csv";
        [SerializeField] private bool startOnPlay = false;

        [Header("Reference = PB's onset")]
        [Tooltip("Per-site calibrated drives from E1. Leave blank for a uniform reference drive.")]
        [SerializeField] private string cueIntensityFile = "";
        [Range(0f, 1f)] [SerializeField] private float uniformReferenceIntensity = 0.8f;

        [Header("Timing")]
        [SerializeField] private float interStimulusGap = 0.6f;
        [SerializeField] private float interTrialGap = 0.8f;

        private const int PulseMs = 100, GapMs = 60, Pulses = 3;

        private ContinuousOnsetCalibration _cal;
        private CueIntensityTable _reference;
        private bool _running, _awaitingResponse, _testWasFirst, _abort;
        private string _status = "SPACE to begin.";
        private int _trials;

        private void Start() { if (startOnPlay) Begin(); }

        public void Begin()
        {
            if (_running) return;
            _cal = new ContinuousOnsetCalibration();
            _reference = string.IsNullOrWhiteSpace(cueIntensityFile)
                ? CueIntensityTable.Uniform(uniformReferenceIntensity)
                : CueIntensityFile.Load(cueIntensityFile);
            _running = true; _abort = false; _trials = 0;
            StartCoroutine(RunAll());
        }

        private IEnumerator RunAll()
        {
            while (!_cal.IsComplete && !_abort)
            {
                yield return PresentTrial();
                _awaitingResponse = true;
                while (_awaitingResponse && !_abort) yield return null;
                yield return new WaitForSeconds(interTrialGap);
            }
            _running = false;
            Write();
        }

        private IEnumerator PresentTrial()
        {
            HapticSite site = _cal.CurrentSite;
            float testLevel = _cal.CurrentTestLevel;

            // Order randomised per trial so the participant cannot answer from position alone.
            _testWasFirst = UnityEngine.Random.value < 0.5f;
            _status = $"{site} · trial {_trials + 1} · listen, then 1 or 2";

            if (_testWasFirst)
            {
                yield return HapticDeviceBinding.PlayHeldOnset(site, testLevel, _cal.OnsetWindowMillis);
                yield return new WaitForSeconds(interStimulusGap);
                yield return HapticDeviceBinding.PlayThreePulse(site, _reference.For(site), PulseMs, GapMs, Pulses);
            }
            else
            {
                yield return HapticDeviceBinding.PlayThreePulse(site, _reference.For(site), PulseMs, GapMs, Pulses);
                yield return new WaitForSeconds(interStimulusGap);
                yield return HapticDeviceBinding.PlayHeldOnset(site, testLevel, _cal.OnsetWindowMillis);
            }
        }

        /// <param name="firstFeltStronger">the participant chose interval 1.</param>
        private void Respond(bool firstFeltStronger)
        {
            if (!_awaitingResponse) return;
            // The staircase wants "was the CONTINUOUS onset stronger?", which depends on the order used.
            bool continuousStronger = firstFeltStronger == _testWasFirst;
            _cal.Respond(continuousStronger);
            _trials++;
            _awaitingResponse = false;
        }

        private void OnGUI()
        {
            // IMGUI events, not UnityEngine.Input: backend-agnostic under the new Input System package.
            Event e = Event.current;
            if (e != null && e.type == EventType.KeyDown)
            {
                if (!_running && e.keyCode == KeyCode.Space) { Begin(); e.Use(); }
                else if (_running && e.keyCode == KeyCode.Alpha1) { Respond(true); e.Use(); }
                else if (_running && e.keyCode == KeyCode.Alpha2) { Respond(false); e.Use(); }
                else if (_running && e.keyCode == KeyCode.R) { StartCoroutine(PresentTrial()); e.Use(); }
                else if (_running && e.keyCode == KeyCode.Escape) { _abort = true; _awaitingResponse = false; e.Use(); }
            }

            GUILayout.BeginArea(new Rect(12, 12, 560, 190), GUI.skin.box);
            GUILayout.Label("H4' ONSET MATCH — PB onset vs PBC onset");
            GUILayout.Label(_status);
            if (_cal != null && !_cal.IsComplete)
                GUILayout.Label($"site {_cal.CurrentSite} ({_cal.SiteIndex + 1}/{_cal.TestSites.Count})  " +
                                $"test level {_cal.CurrentTestLevel:F3}  reversals {_cal.CurrentStaircase?.Reversals}");
            if (_cal != null && _cal.IsComplete)
                GUILayout.Label($"DONE — floor {_cal.PooledEstimate:F3}, spread {_cal.Spread:F3} " +
                                $"({(_cal.SpreadIsAcceptable ? "acceptable" : "TOO WIDE")})");
            GUILayout.Label("SPACE begin · 1/2 which felt stronger · R replay · Esc abort");
            GUILayout.EndArea();
        }

        private void Write()
        {
            var inv = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();
            sb.AppendLine("participant,site,onset_floor,pinned_at_bound,trials,reversals");
            foreach (HapticSite s in _cal.TestSites)
                sb.AppendLine(string.Join(",", participantId, s,
                    _cal.EstimateFor(s).ToString("F4", inv), _cal.PinnedAtBound(s) ? 1 : 0,
                    _trials, _cal.CurrentStaircase?.Reversals ?? 0));

            string dir = Path.Combine(Application.persistentDataPath, $"P{participantId:D3}");
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, outputFile);
            File.WriteAllText(path, sb.ToString());

            float pooled = _cal.PooledEstimate;
            _status = $"written: {path}";
            Debug.Log($"[OnsetMatch] P{participantId}: floor = {pooled:F3}, spread = {_cal.Spread:F3} " +
                      $"over {_cal.TestSites.Count} sites, {_trials} trials -> {path}");

            // The two ways this calibration fails are both study-invalidating, and both are silent unless
            // said out loud here. Neither is a reason to stop collecting — they are reasons to change what
            // H4' is allowed to claim.
            if (!_cal.SpreadIsAcceptable)
                Debug.LogWarning($"[OnsetMatch] Site spread {_cal.Spread:F3} exceeds " +
                    $"{ContinuousOnsetCalibration.MaxDefensibleSpread:F2}. A single frozen floor is the wrong " +
                    "model: PBC would start perceptibly later on some limbs than others, reintroducing the " +
                    "onset confound for a subset of opportunities. Use per-site floors or report the caveat.");

            foreach (HapticSite s in _cal.TestSites)
                if (_cal.PinnedAtBound(s))
                    Debug.LogWarning($"[OnsetMatch] {s} ran to its bound — PBC's onset could NOT be matched to " +
                        "PB's at any drive. H4' must then be reported as comparing cue forms that differ in " +
                        "onset salience, NOT as isolating cue form.");

            if (_abort)
                Debug.LogWarning("[OnsetMatch] Aborted before all sites converged. Unconverged sites carry a " +
                                 "NaN estimate; do not pool them into a frozen value.");
        }
    }
}
