using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using CollisionFeedback.Core;
using CollisionFeedback.Core.E2;
using CollisionFeedback.Runtime;
using Joint = CollisionFeedback.Core.Joint; // disambiguate from UnityEngine.Joint (physics component)

namespace CollisionFeedback.Integration
{
    /// <summary>
    /// Session driver for Paper 2 E2 — the warning lead-time experiment [PAPER2_STUDY_DESIGN §6, §8].
    ///
    /// **This is glue, deliberately.** Every decision that matters — the trial schedule, the geometry, when the
    /// cue fires, what a realized lead time is, what counts as a crossing — lives in
    /// <see cref="E2SessionPlan"/> and <see cref="E2TrialRunner"/>, which are pure Core and covered by 27
    /// EditMode tests that run with no hardware. This class spawns a sphere, feeds frames in, and writes rows
    /// out. Keep it that way: logic added here is logic that cannot be tested without a headset.
    ///
    /// Scene requirements: a <see cref="BodyTrackerRig"/> (HMD + tracked hands), and bHaptics for the cue.
    /// With <see cref="useLiveHaptics"/> off the session still runs and logs — the cue simply is not felt,
    /// which is useful for a dry run but obviously invalid for data.
    ///
    /// **It lives in Integration, not Runtime, for the same reason <see cref="SessionRunner"/> does:** it needs
    /// the bHaptics SDK, and the `CollisionFeedback.Runtime` assembly references only Core. Anything touching
    /// a device SDK belongs on this side of the seam.
    ///
    /// ⚠ <see cref="pipelineLatencySeconds"/> defaults to 0 and **must** be set from the bench measurement
    /// (§5: accelerometer on the tactor, ≥100 activations) before any real session. Left at 0, every delivered
    /// lead time is wrong by the true hardware latency and the whole psychometric curve shifts.
    /// </summary>
    [DefaultExecutionOrder(-40)]
    public sealed class E2SessionRunner : MonoBehaviour
    {
        [Header("Participant")]
        public int participantId = 901;
        public int sessionIndex = 1;
        [Tooltip("Refuse to start if the session folder already holds data.")]
        public bool allowOverwrite = false;

        [Header("Timing — SET FROM THE BENCH MEASUREMENT")]
        [Tooltip("Measured command-to-first-vibration latency (s). 0 is a placeholder and invalidates the study.")]
        public float pipelineLatencySeconds = 0f;

        [Header("Schedule")]
        [Tooltip("Total trials. §8 plans 320–400 for 80–100 warning trials at 25% prevalence.")]
        public int totalTrials = 360;
        [Tooltip("Practice trials before measurement. Run at the longest lead and never logged.")]
        public int practiceTrials = 16;
        public int planSeed = 20260914;

        [Header("Lead levels — SET FROM PILOT (§8)")]
        [Tooltip("Assigned lead times (s). §8: 5–7 levels whose ACHIEVED spread covers ~10–90% avoidance. " +
                 "Delivery runs systematically short, so tune these against measured delivery, not intuition.")]
        public float[] leadLevels = { 0.08f, 0.15f, 0.25f, 0.35f, 0.50f };
        [Tooltip("Trials per mini-block, each holding exactly one warning trial. 4 = 25% prevalence, locked " +
                 "locally so a participant cannot detect a run of warnings and brake pre-emptively.")]
        public int miniBlockSize = 4;

        [Header("Geometry — SET FROM PILOT (§8)")]
        [Tooltip("Home-to-target reach amplitude (m).")]
        public float reachDistance = 0.70f;
        [Tooltip("Half-extents of every hazard volume (m).")]
        public Vector3 hazardHalfExtents = new Vector3(0.10f, 0.10f, 0.10f);
        [Tooltip("Where the hazard sits along the reach, as a fraction of it. Varied per trial so the " +
                 "boundary cannot be memorised.")]
        public float hazardFractionMin = 0.60f;
        public float hazardFractionMax = 0.80f;
        public float azimuthSpreadDeg = 35f;
        public float elevationSpreadDeg = 18f;
        [Tooltip("Home position of the reaching limb, in the tracking frame.")]
        public Vector3 homePosition = new Vector3(0.20f, 1.10f, 0.20f);

        [Tooltip("A tracking gap longer than this is logged as a dropout. Must exceed the normal " +
                 "inter-frame interval with margin, or ordinary jitter is reported as equipment failure.")]
        [SerializeField] private float trackingDropoutSeconds = 0.25f;

        [Header("Tempo — SET FROM PILOT (§15 gate 7)")]
        [Tooltip("Longest accepted movement time on Normal trials (s).")]
        public float normalWindowSeconds = 1.20f;
        [Tooltip("SHORTEST accepted movement time on Normal trials (s). Without this lower bound a " +
                 "participant can reach fast on every trial and satisfy both windows, and H3a compares " +
                 "Urgent against Urgent. Must sit comfortably above the Urgent upper bound.")]
        public float normalWindowMinSeconds = 0.85f;
        [Tooltip("Response window for Urgent trials (s). Must be genuinely fast but still achievable — " +
                 "if compliance falls below ~60% this is instruction failure, not a tempo manipulation.")]
        public float urgentWindowSeconds = 0.70f;
        [Tooltip("Shortest accepted movement time on Urgent trials (s). 0 = faster is always fine.")]
        public float urgentWindowMinSeconds = 0f;

        [Header("Analysis tolerances")]
        [Tooltip("Timing-tolerance band (s) for flagging protocol deviations. The default flags almost " +
                 "everything because delivery runs short — set it around EXPECTED delivery from pilot data.")]
        public float timingToleranceSeconds = 0.05f;

        [Header("Trial flow")]
        public float maxTrialSeconds = 4.0f;
        public float interTrialSeconds = 1.2f;
        [Tooltip("Limb must be within this of home before a trial may start (m).")]
        public float homeTolerance = 0.10f;
        [Tooltip("Mandatory break every N trials.")]
        public int breakEveryTrials = 60;
        [Tooltip("How long the NORMAL/FAST prompt shows before the target appears (s).")]
        public float tempoPromptSeconds = 0.8f;
        [Tooltip("Operator abort key. Read through IMGUI so it works under either input backend.")]
        public KeyCode abortKey = KeyCode.Escape;
        public float breakSeconds = 30f;

        [Header("Haptics")]
        [Tooltip("Off = dry run: the cue is logged but never played. Never valid for data collection.")]
        public bool useLiveHaptics = true;
        [Range(0f, 1f)] public float hapticIntensity = 0.7f;
        [Tooltip("Optional per-site intensity file from the E2 cue calibration. Empty = flat intensity.")]
        public string cueIntensityFile = "";

        [Header("Scene")]
        public BodyTrackerRig trackerRig;
        [Tooltip("Optional. Prefab spawned as the reach target; a sphere is generated if empty.")]
        public GameObject targetPrefab;
        [Tooltip("Optional marker showing the home region.")]
        public Transform homeMarker;

        private IKeypointSource _source;
        private IFeedbackSink _sink;
        private string _sessionDir;
        private E2TrialLogWriter _log;
        private DeviationLogWriter _deviations;   // deviations.csv [PAPER2 §14]
        private SessionEventLogWriter _events;    // events.csv — NON-TRIAL chronology [PAPER2 §14]
        [Tooltip("Collects between-block discomfort and the post-session battery. Without one, those " +
                 "measures are skipped and the shoulder-discomfort stopping rule cannot fire.")]
        [SerializeField] private QuestionnairePanel questionnairePanel;

        private QuestionnaireLogWriter _questionnaire;   // questionnaires.csv [PAPER2 §14]
        private bool _discomfortStop;                    // latched by the shoulder-discomfort rule
        private TrajectoryLogWriter _trajectory;  // trajectories_<block>.csv [PAPER2 §14]
        private int _trajectoryBlock = int.MinValue;
        private float _lastFrameTime;             // wall clock of the newest tracking frame
        private bool _trackingLost;
        private E2PlanParams _planParams;
        private List<E2ScheduledTrial> _plan;
        private GameObject _target;
        private bool _abort;
        private string _status = "idle";
        private string _prompt = "";
        private float _promptUntil;
        private int _completed;

        // ── Lifecycle ─────────────────────────────────────────────────────────────────────────

        private void OnEnable()
        {
            _sessionDir = Path.Combine(Application.persistentDataPath, "sessions", $"E2_P{participantId:D3}");
            if (Directory.Exists(_sessionDir) && Directory.GetFiles(_sessionDir).Length > 0 && !allowOverwrite)
            {
                Debug.LogError($"[E2SessionRunner] Session folder already has data: {_sessionDir}. " +
                               "Change participantId or tick allowOverwrite.");
                enabled = false;
                return;
            }
            Directory.CreateDirectory(_sessionDir);

            if (trackerRig == null) trackerRig = FindFirstObjectByType<BodyTrackerRig>();
            if (trackerRig == null || !trackerRig.IsComplete)
            {
                Debug.LogError("[E2SessionRunner] No complete BodyTrackerRig. Cannot run.");
                enabled = false;
                return;
            }
            _source = trackerRig.CreateSource();

            _sink = useLiveHaptics ? CreateHapticSink() : null;
            if (_sink == null)
            {
                Debug.LogWarning("[E2SessionRunner] useLiveHaptics is OFF — the cue is logged but never felt. " +
                                 "Fine for a dry run; NOT valid for data collection.");
                _sink = new NullSink();
            }

            if (Mathf.Approximately(pipelineLatencySeconds, 0f))
                Debug.LogWarning("[E2SessionRunner] pipelineLatencySeconds is 0. Every delivered lead time " +
                                 "will be short by the true hardware latency. Set it from the bench " +
                                 "measurement (§5) before collecting data.");

            _planParams = new E2PlanParams
            {
                TotalTrials = totalTrials,
                Seed = planSeed,
                LeadLevels = leadLevels,
                MiniBlockSize = miniBlockSize,
                ReachDistance = reachDistance,
                HazardHalfExtents = hazardHalfExtents,
                HazardFractionMin = hazardFractionMin,
                HazardFractionMax = hazardFractionMax,
                AzimuthSpreadDeg = azimuthSpreadDeg,
                ElevationSpreadDeg = elevationSpreadDeg,
                Home = homePosition,
                PracticeTrials = practiceTrials,
                MovementOnsetSpeed = new E2Params().MovementOnsetSpeed,
            };

            try
            {
                _plan = E2SessionPlan.Build(_planParams);
            }
            catch (System.InvalidOperationException ex)
            {
                // The geometry cannot deliver the leads it assigns. Refusing to run is the point: the trials
                // it would lose are concentrated at the long leads, which is the tail that sets LT80.
                Debug.LogError("[E2SessionRunner] PLAN REJECTED — " + ex.Message);
                enabled = false;
                return;
            }

            Debug.Log("[E2SessionRunner] " + E2SessionPlan.Audit(_plan, _planParams).Describe());

            _log = new E2TrialLogWriter(Path.Combine(_sessionDir, "e2_trials.csv"), participantId, sessionIndex);
            _deviations = new DeviationLogWriter(Path.Combine(_sessionDir, "deviations.csv"),
                                                 "Paper2_E2", participantId, sessionIndex);
            _questionnaire = new QuestionnaireLogWriter(Path.Combine(_sessionDir, "questionnaires.csv"));
            if (questionnairePanel == null) questionnairePanel = FindFirstObjectByType<QuestionnairePanel>();
            _events = new SessionEventLogWriter(Path.Combine(_sessionDir, "events.csv"),
                                                "Paper2_E2", participantId, sessionIndex,
                                                Time.realtimeSinceStartup);
            _events.Phase = "SETUP";
            _events.Record(SessionEventKind.SessionStart,
                           $"plan={_plan?.Count ?? 0} trials, seed={planSeed}");
            WriteSessionManifest();

            int practice = _plan.FindAll(t => t.IsPractice).Count;
            Debug.Log($"[E2SessionRunner] P{participantId}: {_plan.Count} trials planned " +
                      $"({practice} practice at the longest lead, not logged). " +
                      $"Latency={pipelineLatencySeconds * 1000f:F0} ms. CSV → {_sessionDir}");

            StartCoroutine(RunSession());
        }

        private void OnDisable()
        {
            (_source as System.IDisposable)?.Dispose();
            _source = null;
            if (_target != null) Destroy(_target);
        }

        // The abort hotkey is read in OnGUI via the IMGUI event stream, not here — see OnGUI below.
        // UnityEngine.Input throws on this project (Player Settings use the Input System package), and it
        // threw once per frame, which buries the console the operator needs to read.

        // ── Session ───────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Writes `session.json` [PAPER2 §14] — the provenance record that makes a session reproducible from
        /// its own log. **Every parameter that could change the numbers goes in here**, because §15 gate 10
        /// asks that the logs reconstruct every trial, and a plan seed is only reproducible alongside the
        /// parameters it was drawn against.
        ///
        /// Hand-rolled rather than JsonUtility because that serialiser will not emit a nested mix of arrays
        /// and scalars in a readable shape, and this file is meant to be read by a human at audit time.
        /// </summary>
        private void WriteSessionManifest()
        {
            string leads = "";
            for (int i = 0; i < leadLevels.Length; i++)
                leads += (i > 0 ? ", " : "") + leadLevels[i].ToString("F3", CultureInfo.InvariantCulture);

            string j =
                "{\n" +
                $"  \"schema\": \"e2-session/1\",\n" +
                $"  \"paper\": \"Paper 2 — E2 warning lead-time experiment\",\n" +
                $"  \"participant\": {participantId},\n" +
                $"  \"session\": {sessionIndex},\n" +
                $"  \"started_utc\": \"{System.DateTime.UtcNow:o}\",\n" +
                $"  \"unity_version\": \"{Application.unityVersion}\",\n" +
                $"  \"application_version\": \"{Application.version}\",\n" +
                $"  \"device\": \"{SystemInfo.deviceModel}\",\n" +
                $"  \"device_id\": \"{SystemInfo.deviceUniqueIdentifier}\",\n" +
                $"  \"operating_system\": \"{SystemInfo.operatingSystem}\",\n" +
                "  \"timing\": {\n" +
                $"    \"pipeline_latency_s\": {F(pipelineLatencySeconds)},\n" +
                $"    \"latency_is_placeholder\": {(Mathf.Approximately(pipelineLatencySeconds, 0f) ? "true" : "false")},\n" +
                $"    \"timing_tolerance_s\": {F(timingToleranceSeconds)}\n" +
                "  },\n" +
                "  \"schedule\": {\n" +
                $"    \"total_trials\": {totalTrials},\n" +
                $"    \"practice_trials\": {practiceTrials},\n" +
                $"    \"mini_block_size\": {miniBlockSize},\n" +
                $"    \"plan_seed\": {planSeed},\n" +
                $"    \"lead_levels_s\": [{leads}]\n" +
                "  },\n" +
                "  \"geometry\": {\n" +
                $"    \"reach_distance_m\": {F(reachDistance)},\n" +
                $"    \"hazard_half_extents_m\": [{F(hazardHalfExtents.x)}, {F(hazardHalfExtents.y)}, {F(hazardHalfExtents.z)}],\n" +
                $"    \"hazard_fraction\": [{F(hazardFractionMin)}, {F(hazardFractionMax)}],\n" +
                $"    \"azimuth_spread_deg\": {F(azimuthSpreadDeg)},\n" +
                $"    \"elevation_spread_deg\": {F(elevationSpreadDeg)},\n" +
                $"    \"home_m\": [{F(homePosition.x)}, {F(homePosition.y)}, {F(homePosition.z)}]\n" +
                "  },\n" +
                "  \"tempo\": {\n" +
                $"    \"normal_window_s\": [{F(normalWindowMinSeconds)}, {F(normalWindowSeconds)}],\n" +
                $"    \"urgent_window_s\": [{F(urgentWindowMinSeconds)}, {F(urgentWindowSeconds)}]\n" +
                "  },\n" +
                "  \"haptics\": {\n" +
                $"    \"live\": {(useLiveHaptics ? "true" : "false")},\n" +
                $"    \"intensity\": {F(hapticIntensity)},\n" +
                $"    \"cue_intensity_file\": \"{cueIntensityFile}\"\n" +
                "  },\n" +
                $"  \"plan_audit\": \"{E2SessionPlan.Audit(_plan, _planParams).Describe()}\"\n" +
                "}\n";

            File.WriteAllText(Path.Combine(_sessionDir, "session.json"), j);
        }

        private static string F(float v) => v.ToString("F4", CultureInfo.InvariantCulture);

        private IEnumerator RunSession()
        {
            int measured = 0;
            for (int i = 0; i < _plan.Count && !_abort; i++)
            {
                // The plan marks its own practice prefix, so this no longer assumes practice is the first N.
                bool isPractice = _plan[i].IsPractice;
                if (!isPractice) measured++;

                // One trajectory file per mini-block, practice kept separate (-1) so it can never be
                // pooled with measured data by a glob.
                int block = isPractice ? -1
                          : (miniBlockSize > 0 ? (measured - 1) / miniBlockSize : 0);
                if (block != _trajectoryBlock)
                {
                    _trajectory?.Dispose();
                    _trajectoryBlock = block;
                    string name = block < 0 ? "trajectories_practice.csv" : $"trajectories_{block}.csv";
                    _trajectory = new TrajectoryLogWriter(Path.Combine(_sessionDir, name),
                                                          participantId, sessionIndex, block);
                }
                if (_events != null)
                {
                    string phase = isPractice ? "PRACTICE" : "TRIALS";
                    if (_events.Phase != phase)
                    {
                        // The practice/measured boundary is invisible in e2_trials.csv, because practice
                        // rows are never written there. Without this the log cannot say when the measured
                        // session actually began.
                        if (_events.Phase == "PRACTICE") _events.Record(SessionEventKind.PracticeEnd);
                        _events.Phase = phase;
                        if (phase == "PRACTICE") _events.Record(SessionEventKind.PracticeStart);
                    }
                }

                if (!isPractice && breakEveryTrials > 0 &&
                    measured > 1 && (measured - 1) % breakEveryTrials == 0)
                {
                    _status = $"BREAK — {breakSeconds:F0}s";
                    _events?.Record(SessionEventKind.BreakStart, $"after {measured - 1} measured trials");

                    // Discomfort is asked at the BREAK, before the rest, so the rating describes the
                    // block just finished rather than a recovered arm.
                    yield return AdministerOne(Questionnaire.ShoulderDiscomfort(), block);
                    if (_discomfortStop)
                    {
                        _abort = true;
                        break;
                    }

                    yield return new WaitForSeconds(breakSeconds);
                    _events?.Record(SessionEventKind.BreakEnd);
                }

                yield return WaitForHome();
                if (_abort) break;

                yield return RunTrial(_plan[i], isPractice);
            }

            // Post-session battery [PAPER2: "Post-session comfort, sickness, and adverse-event check"].
            // Runs even after an operator stop - especially then, since that is when a sickness or
            // discomfort reading is most informative.
            yield return AdministerOne(Questionnaire.ShoulderDiscomfort(), -1);
            yield return AdministerOne(Questionnaire.Ssq(), -1);
            yield return AdministerOne(Questionnaire.CueAcceptability(), -1);

            _questionnaire = null;
            _events?.Record(SessionEventKind.SessionEnd,
                            _abort ? "operator stop" : $"complete, {_completed} measured trials");
            _log = null;          // close the writers before hashing
            _deviations = null;
            _events = null;
            _trajectory?.Dispose();
            _trajectory = null;
            // Manifest LAST, after every writer has closed [§12]. Hashing earlier would certify a
            // half-written state that never existed on disk, which is worse than not hashing at all.
            AnalysisManifestWriter.Write(
                _sessionDir, "Paper2_E2", participantId,
                notes: _abort ? new[] { "Session ended by operator stop - partial data." } : null);

            _status = _abort
                ? $"E-STOP — {_completed} trials written to {_sessionDir}"
                : $"Complete — {_completed} trials written to {_sessionDir}";
            Debug.Log("[E2SessionRunner] " + _status);
        }

        /// <summary>
        /// Show one instrument, persist BOTH the raw item responses and the scored values [§14 "item-level
        /// comfort, sickness, timeliness/urgency, and scored values"], and apply the shoulder-discomfort
        /// stopping rule.
        ///
        /// Skipped with a warning when no panel is present rather than throwing: a missing panel must not
        /// destroy a session that is otherwise collecting good trial data. But it is logged as a protocol
        /// departure, because a session without its safety measure is not a complete session.
        /// </summary>
        private IEnumerator AdministerOne(Questionnaire q, int block)
        {
            if (questionnairePanel == null)
            {
                Debug.LogWarning($"[E2SessionRunner] No QuestionnairePanel — skipping {q.Instrument}.");
                _deviations?.Record(DeviationKind.ProtocolDeparture, "questionnaire_skipped",
                                    $"{q.Instrument} not administered: no panel in scene.",
                                    scope: block < 0 ? "session" : "block", id: block < 0 ? "" : block.ToString());
                yield break;
            }

            _status = $"{q.Title} — participant answering";
            bool done = false;
            IReadOnlyDictionary<string, float> measures = null;
            IReadOnlyDictionary<string, int> responses = null;
            questionnairePanel.Administer(q, (m, r) => { measures = m; responses = r; done = true; });
            yield return new WaitUntil(() => done || _abort);
            if (measures == null) yield break;

            _questionnaire?.Append(participantId, block, "E2", q.Instrument, measures, responses);

            // THE STOPPING RULE. Reads `max`, never a mean - averaging would let a severe shoulder
            // complaint be diluted by comfortable fingers and the session continue past the point this
            // rule exists to catch.
            if (q.Instrument == "DISCOMFORT" &&
                measures.TryGetValue("max", out float worst) &&
                worst >= Questionnaire.DiscomfortStopThreshold)
            {
                _discomfortStop = true;
                string detail = $"Worst-site discomfort {worst:F0} >= threshold " +
                                $"{Questionnaire.DiscomfortStopThreshold:F0} (Borg CR10).";
                Debug.LogWarning($"[E2SessionRunner] DISCOMFORT STOP — {detail}");
                _deviations?.Record(DeviationKind.AdverseEvent, "shoulder_discomfort_stop", detail);
                _events?.Record(SessionEventKind.OperatorStop, detail);
            }
        }

        /// <summary>Blocks until the limb is back in the home region, so every trial starts identically (§8).</summary>
        private IEnumerator WaitForHome()
        {
            _status = "return to home";
            float waited = 0f;
            while (!_abort && waited < 10f)
            {
                if (TryLatestFrame(out PoseFrame f))
                {
                    Vector3 limb = f.Get(_plan[0].Trial.TargetLimb);
                    if (Vector3.Distance(limb, _plan[0].Home) <= homeTolerance) yield break;
                }
                waited += Time.deltaTime;
                yield return null;
            }

            // Timed out: the trial still runs, but it did NOT start from the home region, so its
            // approach geometry differs from every other trial. Previously this was silent - the only
            // trace was a slightly odd realized lead with no explanation attached.
            if (!_abort)
            {
                _events?.Record(SessionEventKind.HomeWaitTimeout, "10 s elapsed; trial started off-home");
                _deviations?.Record(DeviationKind.ProtocolDeparture, "home_wait_timeout",
                                    "Limb did not return to the home region within 10 s.");
            }
        }

        private IEnumerator RunTrial(E2ScheduledTrial st, bool isPractice)
        {
            var p = new E2Params
            {
                PipelineLatencySeconds = pipelineLatencySeconds,
                TimingToleranceSeconds = timingToleranceSeconds,
                NormalWindowSeconds = normalWindowSeconds,
                UrgentWindowSeconds = urgentWindowSeconds,
                NormalWindowMinSeconds = normalWindowMinSeconds,
                UrgentWindowMinSeconds = urgentWindowMinSeconds,
            };
            var runner = new E2TrialRunner(st.Trial, new List<Obstacle> { st.Hazard }, p, _sink);

            // The participant is told which speed is asked for BEFORE the target appears — the instruction
            // script (§A2) promises exactly this, and tempo cannot manipulate anything they cannot anticipate.
            // It carries no information about whether this is a warning trial.
            _prompt = st.Trial.Tempo == Tempo.Urgent ? "FAST" : "NORMAL";
            _promptUntil = Time.time + tempoPromptSeconds;
            yield return new WaitForSeconds(tempoPromptSeconds);

            ShowTarget(st.Trial.TargetPosition);
            _status = (isPractice ? "practice " : "") + st.Trial.Id +
                      (st.Trial.IsWarningTrial ? $"  warn@{st.Trial.AssignedLeadSeconds:F2}s" : "  go") +
                      $"  {st.Trial.Tempo}";

            float elapsed = 0f;
            while (!_abort && elapsed < maxTrialSeconds)
            {
                // Drain every queued frame: the runner is driven by DATA time, not frame time, so dropping
                // frames would silently change the measured lead time.
                //
                // The trajectory log takes the SAME frames, in the same order, before the runner sees
                // them consume anything. That is what lets a disputed trial be recomputed exactly: the
                // file contains the input the measurement was made from, not a parallel sampling of it.
                while (_source != null && _source.TryGetFrame(out PoseFrame f))
                {
                    _trajectory?.Write(f, st.Trial.Id);
                    runner.Tick(f);
                }
                elapsed += Time.deltaTime;
                yield return null;
            }

            E2TrialOutcome outcome = runner.Finish();
            _trajectory?.Flush();   // bound the loss to one trial if the process dies
            HideTarget();

            // Pace feedback on GO TRIALS ONLY (§8). Giving it after warning trials would tell the participant
            // whether their stop succeeded, which is feedback about the hazard — §8 forbids that, and it
            // would let them score themselves and tune a strategy mid-session.
            if (!st.Trial.IsWarningTrial && outcome.ReachedTarget && !double.IsNaN(outcome.MovementTimeSeconds))
            {
                _prompt = outcome.WithinResponseWindow
                    ? "good pace"
                    : (outcome.MovementTimeSeconds > outcome.ResponseWindowSeconds ? "too slow" : "too fast");
                // "too fast" is reachable only because Normal has a lower bound — see E2Params.
                _promptUntil = Time.time + 0.6f;
            }

            // Practice trials are run identically and then discarded — they must not reach the primary file.
            if (!isPractice)
            {
                _log.Write(outcome);
                _completed++;

                // Recorded per trial rather than swept up at the end: a session that dies mid-way is
                // exactly when the reasons matter, and an end-of-session pass would lose all of them.
                if (!outcome.Valid)
                    _deviations?.Record(DeviationKind.InvalidTrial,
                        string.IsNullOrEmpty(outcome.InvalidReason) ? "unspecified" : outcome.InvalidReason,
                        scope: "trial", id: outcome.TrialId);

                if (outcome.TimingDeviation)
                    _deviations?.Record(DeviationKind.TimingDeviation, "outside_timing_tolerance",
                        "Kept for analysis against the realized lead, not deleted.",
                        scope: "trial", id: outcome.TrialId);
            }

            yield return new WaitForSeconds(interTrialSeconds);
        }

        // ── Scene helpers ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Drain to the newest frame, and detect tracking DROPOUTS on the way.
        ///
        /// A false return does NOT mean tracking was lost — at 90 Hz capture against a slower render
        /// loop, plenty of ticks legitimately have no new frame. Only a GAP longer than
        /// <see cref="trackingDropoutSeconds"/> counts, which is why this is a duration test rather than
        /// a per-tick one. Without it the log would either miss real dropouts or fill with false ones.
        ///
        /// This matters for the estimand: a dropout mid-trial is a reason a realized lead may be wrong,
        /// and before this it left no trace in any file.
        /// </summary>
        private bool TryLatestFrame(out PoseFrame frame)
        {
            frame = default;
            bool got = false;
            while (_source != null && _source.TryGetFrame(out PoseFrame f)) { frame = f; got = true; }

            float now = Time.realtimeSinceStartup;
            if (got)
            {
                if (_trackingLost)
                {
                    float gap = now - _lastFrameTime;
                    _trackingLost = false;
                    _events?.Record(SessionEventKind.TrackingRegained, $"gap {gap:F2} s");
                    _deviations?.Record(DeviationKind.EquipmentFailure, "tracking_dropout",
                                        $"No frames for {gap:F2} s.");
                }
                _lastFrameTime = now;
            }
            else if (!_trackingLost && _lastFrameTime > 0f &&
                     now - _lastFrameTime > trackingDropoutSeconds)
            {
                _trackingLost = true;
                _events?.Record(SessionEventKind.TrackingLost,
                                $"no frames for {trackingDropoutSeconds:F2} s");
            }
            return got;
        }

        private void ShowTarget(Vector3 at)
        {
            if (_target == null)
            {
                _target = targetPrefab != null
                    ? Instantiate(targetPrefab)
                    : GameObject.CreatePrimitive(PrimitiveType.Sphere);
                if (targetPrefab == null)
                {
                    _target.transform.localScale = Vector3.one * 0.08f;
                    Collider c = _target.GetComponent<Collider>();
                    if (c != null) Destroy(c);          // visual only; never participates in physics
                }
                _target.name = "E2_ReachTarget";
            }
            _target.transform.position = at;
            _target.SetActive(true);
        }

        private void HideTarget()
        {
            if (_target != null) _target.SetActive(false);
        }

        /// <summary>Same construction as <see cref="SessionRunner.CreateHapticSink"/>, so the cue a Paper 2
        /// participant feels is identical in waveform and per-site gain to the Paper 1 cue.</summary>
        private BHapticsSink CreateHapticSink() =>
            string.IsNullOrWhiteSpace(cueIntensityFile)
                ? HapticDeviceBinding.CreateThreePulseSink(this, hapticIntensity)
                : HapticDeviceBinding.CreateThreePulseSink(this, CueIntensityFile.Load(cueIntensityFile));

        /// <summary>Swallows cues when haptics are off, so a dry run still exercises the full path.</summary>
        private sealed class NullSink : IFeedbackSink
        {
            public void Fire(in FeedbackCommand command) { }
        }

        private void OnGUI()
        {
            // Abort hotkey via the IMGUI event stream, the same way OperatorEStop does it. This is
            // **input-backend agnostic**: it works whether Player Settings use the legacy Input Manager or
            // the Input System package, and needs no reference to either.
            Event e = Event.current;
            if (!_abort && e != null && e.type == EventType.KeyDown && e.keyCode == abortKey)
            {
                _abort = true;
                _deviations?.Record(DeviationKind.OperatorStop, "operator_abort_key",
                                    $"Aborted after {_completed} logged trials.");
                _events?.Record(SessionEventKind.OperatorStop, $"after {_completed} logged trials");
                Debug.LogWarning($"[E2SessionRunner] ABORT ({abortKey}) — {_completed} trials written.");
                e.Use();
            }

            GUI.Label(new Rect(12, 12, 900, 24), $"[E2] {_status}   ({_completed} logged)");

            // The participant-facing prompt: the requested speed before the reach, pace feedback after a go
            // trial. Large and central so it is readable in the HMD.
            if (Time.time <= _promptUntil && !string.IsNullOrEmpty(_prompt))
            {
                var style = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 48,
                    alignment = TextAnchor.MiddleCenter,
                };
                GUI.Label(new Rect(0, Screen.height * 0.35f, Screen.width, 80), _prompt, style);
            }
        }
    }
}
