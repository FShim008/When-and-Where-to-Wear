using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using CollisionFeedback.Core;
using CollisionFeedback.Runtime;

namespace CollisionFeedback.Integration
{
    /// <summary>
    /// Runs the per-site cue-intensity perceptual match [Plan Task 3.1 / E2] and writes the gains the study
    /// applies. It closes the localization confound (chest X40 = 40 motors vs a 3-motor hand/foot Tactosy at a
    /// flat drive): one site is the fixed REFERENCE, every other site is matched to its perceived magnitude, and
    /// the resulting per-site drives are saved to <c>cue_intensity.csv</c> — the same file
    /// <see cref="SessionRunner"/> loads via <see cref="CueIntensityFile"/>.
    ///
    /// Two procedures (the psychophysics + orchestration is the tested Core <see cref="CueIntensityCalibration"/>
    /// / <see cref="Staircase"/>; this MonoBehaviour is the device playback + operator UI + logging):
    ///  • <b>Staircase</b> (default, rigorous): a 2AFC "which pulse felt stronger?" adaptive staircase per site,
    ///    converging on the equal-magnitude point (PSE). Interval order is randomized to cancel order bias.
    ///  • <b>Method of adjustment</b>: the operator nudges the test drive until the participant reports it
    ///    matches the reference, then accepts. Faster, coarser.
    ///
    /// Lives in Integration (Assembly-CSharp) so it can drive the bHaptics SDK through
    /// <see cref="HapticDeviceBinding.PlayThreePulse"/> (the same 3-pulse study waveform). Drop it on a
    /// GameObject in a simple scene with the <c>[bHaptics]</c> prefab + Player running, wear the suit, press the
    /// on-screen Begin. Run it ONCE per participant before their session; point
    /// <c>SessionRunner.cueIntensityFile</c> at the same <see cref="outputFile"/>.
    /// </summary>
    public sealed class CueIntensityCalibrationRunner : MonoBehaviour
    {
        public enum Mode { Staircase, MethodOfAdjustment }

        [Header("Participant / output")]
        [SerializeField] private int participantId = 0;
        [Tooltip("Per-site gains destination (relative → persistentDataPath). MUST match SessionRunner.cueIntensityFile.")]
        [SerializeField] private string outputFile = CueIntensityFile.DefaultName; // "cue_intensity.csv"

        [Header("Reference — the standard every site is matched to (decision D3)")]
        [Tooltip("Pick a site EVERY other site can reach: a 3-motor Tactosy (hand/foot), so the stronger chest is attenuated down to it.")]
        [SerializeField] private HapticSite referenceSite = HapticSite.LeftHand;
        [Range(0f, 1f)] [SerializeField] private float referenceIntensity = 0.8f;

        [Header("Procedure")]
        [SerializeField] private Mode mode = Mode.Staircase;
        [SerializeField] private bool startOnPlay = false;
        [Tooltip("Silent gap between the two intervals of a 2AFC trial (s).")]
        [SerializeField] private float interStimulusGap = 0.6f;

        [Header("Staircase")]
        [SerializeField] private float startLevel = 0.5f;
        [SerializeField] private float initialStep = 0.2f;
        [SerializeField] private float minStep = 0.02f;
        [SerializeField] private int reversalsToStop = 10;
        [SerializeField] private int reversalsToAverage = 6;

        [Header("Method of adjustment")]
        [Range(0.01f, 0.2f)] [SerializeField] private float adjustStep = 0.05f;

        // The study cue waveform (must match HapticDeviceBinding's live cue so the match is on the real stimulus).
        private const int PulseMs = 100, GapMs = 60, Pulses = 3;
        private static readonly CultureInfo CI = CultureInfo.InvariantCulture;

        private enum Phase { Idle, Playing, Await, Done }
        private Phase _phase = Phase.Idle;

        // Staircase run state.
        private CueIntensityCalibration _cal;
        private System.Random _rng;
        private bool _testFirst;
        private int _choice;      // 0 none, 1 first, 2 second
        private bool _replay;

        // Method-of-adjustment run state.
        private List<HapticSite> _moaSites;
        private int _moaIndex;
        private float _moaLevel;
        private readonly Dictionary<HapticSite, float> _moaResult = new();
        private bool _playing;

        // Presentation / results.
        private string _nowPlaying = "";
        private string _trialLabel = "";
        private CueIntensityTable _result;
        private string _summary = "";
        private StreamWriter _log;
        private string _logPath;

        private GUIStyle _h, _p, _small;

        private void Start()
        {
            if (startOnPlay) Begin();
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            CloseLog();
            HapticDeviceBinding.StopAll();
        }

        /// <summary>Start a fresh calibration run in the selected <see cref="mode"/>.</summary>
        public void Begin()
        {
            StopAllCoroutines();
            _choice = 0; _replay = false; _playing = false; _nowPlaying = ""; _result = null; _summary = "";
            OpenLog();

            if (mode == Mode.Staircase)
            {
                _rng = new System.Random(participantId == 0 ? Environment.TickCount : participantId);
                _cal = new CueIntensityCalibration(new CueIntensityCalibration.Options
                {
                    ReferenceSite = referenceSite,
                    ReferenceIntensity = referenceIntensity,
                    StartLevel = startLevel,
                    InitialStep = initialStep,
                    MinStep = minStep,
                    ReversalsToStop = reversalsToStop,
                    ReversalsToAverage = reversalsToAverage,
                });
                _phase = Phase.Playing; // the coroutine flips to Await once the first trial has played
                StartCoroutine(RunStaircase());
            }
            else
            {
                _moaSites = new List<HapticSite>();
                foreach (HapticSite s in Enum.GetValues(typeof(HapticSite)))
                    if (s != referenceSite) _moaSites.Add(s);
                _moaResult.Clear();
                _moaIndex = 0;
                _moaLevel = Mathf.Clamp01(startLevel);
                _phase = Phase.Await;
            }
        }

        // ── Staircase (2AFC) ────────────────────────────────────────────────────────────────────
        private IEnumerator RunStaircase()
        {
            while (!_cal.IsComplete)
            {
                HapticSite site = _cal.CurrentSite;
                float testLevel = _cal.CurrentTestLevel;
                Staircase sc = _cal.CurrentStaircase;
                _trialLabel = $"Matching {site}  ·  site {_cal.SiteIndex + 1}/{_cal.TestSites.Count}  ·  " +
                              $"trial {sc.Trials + 1}  ·  reversals {sc.Reversals}/{reversalsToStop}  ·  test drive {testLevel:F2}";

                _testFirst = _rng.Next(2) == 0;
                _choice = 0;
                do
                {
                    _replay = false;
                    yield return PlayTrial(site, testLevel, _testFirst);
                    _phase = Phase.Await;
                    yield return new WaitUntil(() => _choice != 0 || _replay);
                } while (_replay);

                bool choseTest = _choice == (_testFirst ? 1 : 2);
                LogStaircaseTrial(site, sc.Trials + 1, testLevel, _testFirst, _choice, choseTest);
                _cal.Respond(choseTest);
            }
            FinishWith(_cal.BuildTable());
        }

        // Play interval 1 then (after the gap) interval 2; one is the reference, the other the test.
        private IEnumerator PlayTrial(HapticSite site, float testLevel, bool testFirst)
        {
            _phase = Phase.Playing;
            HapticSite s1 = testFirst ? site : referenceSite;
            float i1 = testFirst ? testLevel : referenceIntensity;
            HapticSite s2 = testFirst ? referenceSite : site;
            float i2 = testFirst ? referenceIntensity : testLevel;

            _nowPlaying = "① …";
            yield return HapticDeviceBinding.PlayThreePulse(s1, i1, PulseMs, GapMs, Pulses);
            _nowPlaying = "(gap)";
            yield return new WaitForSeconds(Mathf.Max(0.1f, interStimulusGap));
            _nowPlaying = "② …";
            yield return HapticDeviceBinding.PlayThreePulse(s2, i2, PulseMs, GapMs, Pulses);
            _nowPlaying = "";
        }

        // ── Method of adjustment ────────────────────────────────────────────────────────────────
        private void PlayOnceAsync(HapticSite site, float intensity)
        {
            if (_playing) return;
            StartCoroutine(PlayOnce(site, intensity));
        }

        private IEnumerator PlayOnce(HapticSite site, float intensity)
        {
            _playing = true;
            _nowPlaying = $"{site} @ {intensity:F2}";
            yield return HapticDeviceBinding.PlayThreePulse(site, intensity, PulseMs, GapMs, Pulses);
            _nowPlaying = "";
            _playing = false;
        }

        private void AcceptMoa()
        {
            HapticSite site = _moaSites[_moaIndex];
            float level = Mathf.Clamp01(_moaLevel);
            _moaResult[site] = level;
            LogMoa(site, level);
            _moaIndex++;
            if (_moaIndex >= _moaSites.Count)
            {
                var t = new CueIntensityTable().Set(referenceSite, Mathf.Clamp01(referenceIntensity));
                foreach (var kv in _moaResult) t.Set(kv.Key, kv.Value);
                FinishWith(t);
            }
            else
            {
                _moaLevel = Mathf.Clamp01(startLevel);
            }
        }

        // ── Finish + persist ────────────────────────────────────────────────────────────────────
        private void FinishWith(CueIntensityTable table)
        {
            _result = table;
            CueIntensityFile.Save(table, outputFile);
            _summary = BuildSummary(table);
            _phase = Phase.Done;
            CloseLog();
            HapticDeviceBinding.StopAll();
            Debug.Log($"[CueCalibration] complete — {CueIntensityTable.SiteCount} site gains → " +
                      $"{CueIntensityFile.DefaultPath} (or the configured path). Point SessionRunner.cueIntensityFile at it.");
        }

        private string BuildSummary(CueIntensityTable table)
        {
            var sb = new System.Text.StringBuilder();
            foreach (HapticSite s in Enum.GetValues(typeof(HapticSite)))
            {
                string tag = s == referenceSite ? "  (reference)" : "";
                if (mode == Mode.Staircase && _cal != null && _cal.PinnedAtBound(s))
                    tag = "  ⚠ could not reach reference — equalization incomplete for this site";
                sb.Append($"{s,-10} {table.For(s):F2}{tag}\n");
            }
            return sb.ToString();
        }

        // ── Logging ─────────────────────────────────────────────────────────────────────────────
        private void OpenLog()
        {
            CloseLog();
            try
            {
                _logPath = Path.Combine(Application.persistentDataPath, $"cue_calibration_log_P{participantId:D3}.csv");
                bool header = !File.Exists(_logPath);
                _log = new StreamWriter(_logPath, append: true);
                if (header)
                    _log.WriteLine("utc,participant,mode,reference_site,reference_intensity,site,trial,test_level,test_interval,chosen_interval,test_stronger");
                _log.Flush();
            }
            catch (Exception e) { Debug.LogWarning($"[CueCalibration] log open failed: {e.Message}"); _log = null; }
        }

        private void LogStaircaseTrial(HapticSite site, int trial, float testLevel, bool testFirst, int choice, bool choseTest)
        {
            _log?.WriteLine(string.Join(",",
                DateTime.UtcNow.ToString("o"), participantId, mode, referenceSite,
                referenceIntensity.ToString("0.###", CI), site, trial, testLevel.ToString("0.###", CI),
                testFirst ? 1 : 2, choice, choseTest ? 1 : 0));
            _log?.Flush();
        }

        private void LogMoa(HapticSite site, float level)
        {
            _log?.WriteLine(string.Join(",",
                DateTime.UtcNow.ToString("o"), participantId, mode, referenceSite,
                referenceIntensity.ToString("0.###", CI), site, "accepted", level.ToString("0.###", CI), "", "", ""));
            _log?.Flush();
        }

        private void CloseLog()
        {
            try { _log?.Flush(); _log?.Dispose(); } catch { /* ignore */ }
            _log = null;
        }

        // ── Operator UI (IMGUI, desktop mirror) ───────────────────────────────────────────────────
        private void OnGUI()
        {
            EnsureStyles();
            var area = new Rect(24, 24, Mathf.Min(760f, Screen.width - 48f), Screen.height - 48f);
            GUILayout.BeginArea(area, GUI.skin.box);

            GUILayout.Label("Cue-intensity calibration (E2)", _h);
            GUILayout.Label($"Reference: {referenceSite} @ {referenceIntensity:F2}   ·   mode: {mode}   ·   " +
                            $"out: {outputFile}", _small);
            GUILayout.Label("Wear the suit; bHaptics Player running + devices paired. Judge which pulse feels STRONGER.", _small);
            GUILayout.Space(8);

            switch (_phase)
            {
                case Phase.Idle: DrawIdle(); break;
                case Phase.Playing:
                case Phase.Await:
                    if (mode == Mode.Staircase) DrawStaircase(); else DrawMoa();
                    break;
                case Phase.Done: DrawDone(); break;
            }

            GUILayout.EndArea();
        }

        private void DrawIdle()
        {
            GUILayout.Label("Press Begin, then have the participant judge each trial. ~2–4 min.", _p);
            if (GUILayout.Button($"▶ Begin ({mode})", GUILayout.Height(40), GUILayout.Width(280))) Begin();
        }

        private void DrawStaircase()
        {
            GUILayout.Label(_trialLabel, _p);
            if (_phase == Phase.Playing)
            {
                GUILayout.Label($"playing {_nowPlaying}", _p);
                return;
            }
            GUILayout.Label("Which pulse felt STRONGER?", _p);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("① First", GUILayout.Height(48), GUILayout.Width(180))) _choice = 1;
            if (GUILayout.Button("② Second", GUILayout.Height(48), GUILayout.Width(180))) _choice = 2;
            if (GUILayout.Button("↻ Replay", GUILayout.Height(48), GUILayout.Width(140))) _replay = true;
            GUILayout.EndHorizontal();
        }

        private void DrawMoa()
        {
            HapticSite site = _moaSites[_moaIndex];
            GUILayout.Label($"Adjusting {site}  ·  site {_moaIndex + 1}/{_moaSites.Count}  ·  test drive {_moaLevel:F2}", _p);
            GUILayout.Label("Play both; nudge the TEST until it feels equal to the reference, then Accept.", _small);

            GUILayout.BeginHorizontal();
            GUI.enabled = !_playing;
            if (GUILayout.Button($"▶ Reference ({referenceIntensity:F2})", GUILayout.Height(40), GUILayout.Width(220)))
                PlayOnceAsync(referenceSite, referenceIntensity);
            if (GUILayout.Button($"▶ Test ({_moaLevel:F2})", GUILayout.Height(40), GUILayout.Width(180)))
                PlayOnceAsync(site, _moaLevel);
            GUI.enabled = true;
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button($"− {adjustStep:F2}", GUILayout.Height(36), GUILayout.Width(110)))
                _moaLevel = Mathf.Clamp01(_moaLevel - adjustStep);
            if (GUILayout.Button($"+ {adjustStep:F2}", GUILayout.Height(36), GUILayout.Width(110)))
                _moaLevel = Mathf.Clamp01(_moaLevel + adjustStep);
            if (GUILayout.Button("✓ Accept → next", GUILayout.Height(36), GUILayout.Width(200)))
                AcceptMoa();
            GUILayout.EndHorizontal();
        }

        private void DrawDone()
        {
            GUILayout.Label("Done — per-site gains saved.", _p);
            GUILayout.Label(_summary, _p);
            GUILayout.Label($"Saved to: {outputFile} (under persistentDataPath). Log: {_logPath}", _small);
            GUILayout.Space(6);
            if (GUILayout.Button("▶ Run again", GUILayout.Height(36), GUILayout.Width(200))) { _phase = Phase.Idle; }
        }

        private void EnsureStyles()
        {
            if (_h != null) return;
            _h = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold, wordWrap = true };
            _p = new GUIStyle(GUI.skin.label) { fontSize = 15, wordWrap = true, richText = true };
            _small = new GUIStyle(GUI.skin.label) { fontSize = 11, wordWrap = true, normal = { textColor = Color.gray } };
        }
    }
}
