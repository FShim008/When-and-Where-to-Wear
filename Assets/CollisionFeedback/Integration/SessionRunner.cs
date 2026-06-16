using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using CollisionFeedback.Core;
using CollisionFeedback.Runtime;
using Joint = CollisionFeedback.Core.Joint; // disambiguate from UnityEngine.Joint (physics component)

namespace CollisionFeedback.Integration
{
    /// <summary>
    /// FULL-SESSION driver [Plan Tasks 5.1–5.4 + 5.6]. Sequences one participant's entire session from the
    /// live camera stream: an excluded practice block → the 6 condition blocks in the participant's
    /// <see cref="SessionPlan"/> (Williams-square) order, with enforced inter-block breaks. Owns a single
    /// <see cref="UdpKeypointSource"/> for the whole session, applies the camera→VR calibration, drives the
    /// oracle/conditions/detector/spawner per block, logs raw keypoints, and writes per-block CSVs into a
    /// per-participant folder with overwrite protection.
    ///
    /// This is the PRODUCTION driver; <see cref="LiveSessionController"/> remains the single-block debug tool.
    /// Run only ONE of them in a scene (both bind UDP 9000). Needs the same scene as LiveSessionController:
    /// SceneObstacles, the XR rig, the [bHaptics] prefab + Player (for live cues), OpportunitySpawner, and a
    /// VisualObstacleAlert. Operator advances each gate with the on-screen buttons (desktop mirror).
    ///
    /// NOTE: only the Layout-L1 schedule is authored; until other layouts' storyboards exist, all blocks use
    /// L1 geometry (logged as a warning). Per-site cue-intensity equalization (Plan Task 3.1) and the e-stop
    /// protocol (Plan Task 4.5) are separate and not implemented here.
    /// </summary>
    public sealed class SessionRunner : MonoBehaviour
    {
        [Header("Participant")]
        [SerializeField] private int participantId = 0;
        [Tooltip("Layout pool for counterbalancing. Only 'L1' is authored today; add ids as storyboards land.")]
        [SerializeField] private string[] layoutIds = { "L1" };
        [Tooltip("Refuse to start if this participant's folder already has data (prevents silent overwrite).")]
        [SerializeField] private bool allowOverwrite = false;

        [Header("Timing (seconds)")]
        [SerializeField] private float blockSeconds = 180f;
        [SerializeField] private float practiceSeconds = 90f;
        [SerializeField] private float minBreakSeconds = 30f;

        [Header("Tracking (UDP)")]
        [SerializeField] private int udpPort = 9000;

        [Header("Feedback")]
        [SerializeField] private bool useLiveHaptics = true;
        [SerializeField] private float hapticIntensity = 1f;
        [SerializeField] private float pipelineLatencySeconds = 0f; // set from the M3 latency measurement
        [Tooltip("Condition used for the (excluded) practice block.")]
        [SerializeField] private Condition practiceCondition = Condition.PB;

        [Header("Scene wiring (auto-found if left empty)")]
        [SerializeField] private SceneObstacles sceneObstacles;
        [SerializeField] private OpportunitySpawner spawner;
        [SerializeField] private VisualObstacleAlert visualAlert;

        // The 5 cue-able joints the oracle/conditions/detector track (Head excluded — no head tactor).
        private static readonly List<Joint> Limbs = new()
        {
            Joint.Chest, Joint.LeftHand, Joint.RightHand, Joint.LeftFoot, Joint.RightFoot,
        };

        private UdpKeypointSource _source;
        private RigidTransform _camToVr = RigidTransform.Identity;
        private List<Obstacle> _obstacles;
        private List<BlockAssignment> _plan;
        private string _sessionDir;
        private QuestionnaireLogWriter _questionnaire; // Plan Task 5.5 — writer ready; the administration UI is not built yet

        // Operator-gate + HUD state
        private bool _proceed;        // set by the on-screen button at each gate
        private bool _abort;          // set by the "Stop block" button
        private bool _live;           // a block is currently running
        private string _phase = "Init";
        private string _status = "Starting…";
        private Condition _curCondition;
        private double _hudBlockTime;
        private float _hudTarget;
        private int _hudFrames, _hudAlerts, _hudOutcomes;

        private void OnEnable()
        {
            if (sceneObstacles == null) sceneObstacles = FindFirstObjectByType<SceneObstacles>();
            if (spawner == null) spawner = FindFirstObjectByType<OpportunitySpawner>();
            if (visualAlert == null) visualAlert = FindFirstObjectByType<VisualObstacleAlert>();

            _obstacles = sceneObstacles != null ? sceneObstacles.Collect() : new List<Obstacle>();
            if (_obstacles.Count == 0)
                Debug.LogWarning("[SessionRunner] No scene obstacles found — collisions can't be detected. " +
                                 "Add a SceneObstacles component over the O1/O2/O3 BoxColliders.");

            if (CameraVrCalibrationFile.TryLoad(out RigidTransform t))
            {
                _camToVr = t;
                Debug.Log($"[SessionRunner] Loaded camera→VR calibration from {CameraVrCalibrationFile.DefaultPath}.");
            }
            else
            {
                Debug.LogWarning($"[SessionRunner] No camera→VR calibration at {CameraVrCalibrationFile.DefaultPath} — " +
                                 "keypoints used AS-IS (Identity), so collisions will be wrong. Run CameraVrCalibration first.");
            }

            // Per-participant output folder with overwrite protection.
            _sessionDir = Path.Combine(Application.persistentDataPath, "sessions", $"P{participantId:D3}");
            if (Directory.Exists(_sessionDir) && Directory.GetFiles(_sessionDir).Length > 0 && !allowOverwrite)
            {
                Debug.LogError($"[SessionRunner] Session folder already has data: {_sessionDir}. " +
                               "Use a fresh participantId or enable allowOverwrite. ABORTING.");
                enabled = false;
                return;
            }
            Directory.CreateDirectory(_sessionDir);
            _questionnaire = new QuestionnaireLogWriter(Path.Combine(_sessionDir, "questionnaire.csv"));

            var pool = (layoutIds != null && layoutIds.Length > 0) ? new List<string>(layoutIds) : new List<string> { "L1" };
            _plan = SessionPlan.For(participantId, pool);

            try { _source = new UdpKeypointSource(udpPort); }
            catch (System.Exception e) { Debug.LogError($"[SessionRunner] UDP {udpPort} failed: {e.Message}"); enabled = false; return; }

            Debug.Log($"[SessionRunner] P{participantId}: practice + {_plan.Count} blocks, order = " +
                      string.Join(" ", _plan.ConvertAll(b => b.Condition.ToString())) +
                      $". Listening UDP {udpPort}; CSVs → {_sessionDir}");

            StartCoroutine(RunSession());
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            _source?.Dispose();
            _source = null;
        }

        private IEnumerator RunSession()
        {
            // Practice (excluded from analysis).
            yield return RunBlock(new BlockAssignment(-1, practiceCondition, FirstLayout()), isPractice: true);

            // The 6 counterbalanced condition blocks, with a break before each.
            foreach (BlockAssignment a in _plan)
            {
                yield return Break();
                yield return RunBlock(a, isPractice: false);
            }

            _phase = "DONE";
            _status = $"Session complete for P{participantId}. Files in {_sessionDir}";
            Debug.Log($"[SessionRunner] {_status}");
        }

        private IEnumerator Break()
        {
            _phase = "BREAK";
            _proceed = false;
            float t = 0f;
            while (t < minBreakSeconds || !_proceed)
            {
                t += Time.unscaledDeltaTime;
                _status = t < minBreakSeconds
                    ? $"Rest — {Mathf.Max(0f, minBreakSeconds - t):F0}s minimum, HMD may be removed…"
                    : "Rest — click \"Next\" when the participant is ready.";
                yield return null;
            }
        }

        private IEnumerator RunBlock(BlockAssignment a, bool isPractice)
        {
            int blockIndex = isPractice ? -1 : a.BlockIndex;
            _curCondition = a.Condition;
            float target = isPractice ? practiceSeconds : blockSeconds;
            _hudTarget = target;

            // ── Ready gate ──────────────────────────────────────────────
            _phase = isPractice ? "PRACTICE" : $"Block {a.BlockIndex + 1}/{_plan.Count}";
            _status = $"{_phase}: {a.Condition} ({a.LayoutId}) — click \"Start\" when the participant is ready.";
            _proceed = false;
            yield return new WaitUntil(() => _proceed);

            // ── Build the block ─────────────────────────────────────────
            var ctx = new BlockContext
            {
                ParticipantId = participantId, BlockIndex = blockIndex, Condition = a.Condition, LayoutId = a.LayoutId,
            };
            IFeedbackSink sink = useLiveHaptics ? HapticDeviceBinding.CreateThreePulseSink(this, hapticIntensity) : null;
            var oracleParams = new OracleParams { PipelineLatencySeconds = pipelineLatencySeconds };
            List<Opportunity> schedule = ScheduleFor(a.LayoutId);
            var block = new BlockRunner(ctx, _obstacles, Limbs, schedule, oracleParams, new DetectorParams(), sink);

            if (spawner != null) spawner.DriveExternally();
            if (visualAlert != null) visualAlert.Configure(_obstacles, Limbs, oracleParams.ReactiveDistance);

            string tag = isPractice ? "practice" : $"B{a.BlockIndex}_{a.Condition}";
            var kp = new KeypointLogWriter(Path.Combine(_sessionDir, $"keypoints_{tag}.csv"), participantId, blockIndex);

            // ── Run the block on the block clock ────────────────────────
            double t0 = 0, blockTime = 0; bool started = false; float firstWall = 0f; PoseFrame latest = default;
            _hudFrames = 0; _abort = false; _live = true;
            FlushSource(); // discard frames queued during the gate/break so t0 = first fresh frame

            while (true)
            {
                bool advanced = false;
                while (_source != null && _source.TryGetFrame(out PoseFrame f))
                {
                    if (!started) { t0 = f.Timestamp; started = true; firstWall = Time.unscaledTime; }

                    PoseFrame rebased = f;
                    rebased.Timestamp = f.Timestamp - t0;                    // block-relative
                    for (int j = 0; j < rebased.Joints.Length; j++)
                        rebased.Joints[j] = _camToVr.Apply(rebased.Joints[j]); // Camera-1 → VR world frame

                    blockTime = rebased.Timestamp;
                    latest = rebased;
                    block.Tick(rebased);
                    kp.Write(rebased);
                    _hudFrames++;
                    advanced = true;
                }

                if (started)
                {
                    if (advanced && spawner != null) spawner.Tick(blockTime);
                    if (visualAlert != null) visualAlert.UpdatePose(latest, a.Condition == Condition.Visual, Time.deltaTime);
                    _hudBlockTime = blockTime;
                    _hudAlerts = block.Alerts.Count;
                    _hudOutcomes = block.Outcomes.Count;
                }

                bool timeDone = started && blockTime >= target;
                bool wallGuard = started && (Time.unscaledTime - firstWall) > target + 10f; // stalled-stream guard
                if (timeDone || wallGuard || _abort) break;
                yield return null;
            }

            _live = false;
            kp.Dispose();

            // ── Finish + persist ────────────────────────────────────────
            BlockResult r = block.Finish();
            WriteCsvs(ctx, r, block, isPractice);
            if (_abort) Debug.LogWarning($"[SessionRunner] {_phase} ({a.Condition}) ABORTED by operator at {blockTime:F1}s; partial data written.");
            else Debug.Log($"[SessionRunner] {_phase} ({a.Condition}) done: {_hudFrames} frames, alerts={r.Alerts}, collisions={r.Collisions}/{r.Opportunities}.");
        }

        // Only Layout-L1 is authored; other ids fall back (with a warning) until their storyboards exist.
        private List<Opportunity> ScheduleFor(string layoutId)
        {
            if (!string.Equals(layoutId, "L1", System.StringComparison.OrdinalIgnoreCase))
                Debug.LogWarning($"[SessionRunner] Layout '{layoutId}' has no authored schedule yet — using Layout-1 " +
                                 "geometry. Author its storyboard schedule before relying on layout counterbalancing.");
            return OpportunitySchedules.Layout1();
        }

        private string FirstLayout() => (layoutIds != null && layoutIds.Length > 0) ? layoutIds[0] : "L1";

        private void WriteCsvs(BlockContext ctx, BlockResult r, BlockRunner block, bool isPractice)
        {
            // Practice goes to separate files so it never enters the analysis dataset.
            string prefix = isPractice ? "practice_" : "";
            var summary = new CsvFileWriter(Path.Combine(_sessionDir, $"{prefix}summary.csv"));
            summary.EnsureHeader();
            summary.Append(r);

            new EventLogWriter(Path.Combine(_sessionDir, $"{prefix}events.csv"))
                .WriteBlock(ctx, block.Alerts, block.Outcomes, block.Opportunities);
        }

        private void FlushSource()
        {
            while (_source != null && _source.TryGetFrame(out _)) { /* discard stale queue */ }
        }

        /// <summary>
        /// Record one scored questionnaire into the per-participant <c>questionnaire.csv</c>
        /// (instrument = "IPQ" / "NASA_TLX" / "SSQ"; block = -1 for session-level e.g. SSQ pre/post).
        /// Call this from the questionnaire UI once items are scored. [Plan Task 5.5 — UI not built yet.]
        /// </summary>
        public void RecordQuestionnaire(int block, Condition condition, string instrument,
                                        IReadOnlyDictionary<string, float> measures)
        {
            _questionnaire?.Append(participantId, block, condition.ToString(), instrument, measures);
        }

        // IMGUI operator console (no input-backend dependency); shows on the desktop mirror.
        private void OnGUI()
        {
            var style = new GUIStyle(GUI.skin.label) { fontSize = 14, alignment = TextAnchor.UpperLeft, richText = true };
            float hz = (_live && _hudBlockTime > 0.25) ? (float)(_hudFrames / _hudBlockTime) : 0f;
            GUI.Label(new Rect(12, 12, 960, 120),
                $"<b>Session — P{participantId}</b>   <b>{_phase}</b>\n{_status}\n" +
                (_live ? $"cond {_curCondition}   t {_hudBlockTime:F1}/{_hudTarget:F0}s   ~{hz:F0} Hz   alerts {_hudAlerts}   outcomes {_hudOutcomes}" : ""),
                style);

            // Gate buttons.
            if (!_live && (_phase.StartsWith("Block") || _phase == "PRACTICE"))
                if (GUI.Button(new Rect(12, 120, 160, 32), "▶ Start block")) _proceed = true;
            if (_phase == "BREAK")
                if (GUI.Button(new Rect(12, 120, 160, 32), "▶ Next block")) _proceed = true;
            if (_live)
                if (GUI.Button(new Rect(12, 120, 160, 32), "■ Stop block")) _abort = true;
        }
    }
}
