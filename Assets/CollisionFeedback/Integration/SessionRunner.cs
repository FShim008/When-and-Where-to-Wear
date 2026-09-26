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
    /// VIVE Ultimate Tracker stream: an excluded practice block → the 6 condition blocks in the participant's
    /// <see cref="SessionPlan"/> (Williams-square) order, with enforced inter-block breaks. Owns a single
    /// <see cref="TrackerKeypointSource"/> for the whole session (trackers are already in the VR world frame —
    /// no camera→VR calibration), drives the oracle/conditions/detector/spawner per block, logs raw keypoints,
    /// and writes per-block CSVs into a per-participant folder with overwrite protection.
    ///
    /// This is the PRODUCTION driver; <see cref="LiveSessionController"/> remains the single-block debug tool.
    /// Run only ONE of them in a scene. Needs the same scene as LiveSessionController: a BodyTrackerRig (HMD +
    /// 5 tracker Transforms), SceneObstacles, the XR rig, the [bHaptics] prefab + Player, OpportunitySpawner,
    /// and a VisualObstacleAlert. Operator advances each gate with the on-screen buttons (desktop mirror).
    ///
    /// NOTE: only the Layout-L1 schedule is authored; until other layouts' storyboards exist, all blocks use
    /// L1 geometry (logged as a warning). Per-site cue-intensity equalization (Plan Task 3.1) is supported via
    /// the optional <c>cueIntensityFile</c>; the e-stop protocol (Plan Task 4.5) is separate and not built here.
    /// </summary>
    public sealed class SessionRunner : MonoBehaviour
    {
        [Header("Participant")]
        [SerializeField] private int participantId = 0;
        [Tooltip("v2 [Design v2 §6]: rotate through the six isometric layout variants L1–L6 (practice uses LP) " +
                 "so obstacle locations can't be memorized across blocks. Off = use the layoutIds list below.")]
        [SerializeField] private bool useAllLayoutVariants = true;
        [Tooltip("Manual layout pool — only used when useAllLayoutVariants is OFF.")]
        [SerializeField] private string[] layoutIds = { "L1" };
        [Tooltip("Refuse to start if this participant's folder already has data (prevents silent overwrite).")]
        [SerializeField] private bool allowOverwrite = false;

        [Tooltip("PROTOCOL DEVELOPMENT ONLY. Downgrades the preregistered opportunity-count gate to a " +
                 "warning so a full session can be timed before the 18-event schedule exists. Data " +
                 "collected with this ON is NOT confirmatory and must never enter the analysis. " +
                 "Leave OFF for every real participant.")]
        [SerializeField] private bool protocolDevelopmentMode = false;

        [Header("Timing (seconds)")]
        [Tooltip("Block length. Must exceed the last opportunity close. At the 24-event target the last " +
                 "event closes at 313 s, so 320 s. The authored 12-event schedule closes at 158 s; if you " +
                 "are still running 12 in protocol-development mode, 180 s is enough and 320 s just adds " +
                 "idle time at the end of every block.")]
        [SerializeField] private float blockSeconds = 320f;
        [SerializeField] private float practiceSeconds = 90f;
        [SerializeField] private float minBreakSeconds = 30f;

        [Header("Tracking (VIVE Ultimate Trackers — auto-found if empty)")]
        [SerializeField] private BodyTrackerRig trackerRig;

        [Header("Feedback")]
        [SerializeField] private bool useLiveHaptics = true;
        [SerializeField] private float hapticIntensity = 1f;
        [Tooltip("Optional per-site cue-gain CSV from the E2 perceptual-matching pass (Plan Task 3.1). Relative " +
                 "names resolve under persistentDataPath. Empty = uniform hapticIntensity (no equalization).")]
        [SerializeField] private string cueIntensityFile = "";
        [SerializeField] private float pipelineLatencySeconds = 0f; // set from the M3 latency measurement
        [Tooltip("Condition used for the (excluded) practice block. v2 [Design v2 §7]: None — practice teaches " +
                 "the TASK, the cue tour teaches the cues; practicing under a live condition would privilege it.")]
        [SerializeField] private Condition practiceCondition = Condition.None;

        [Header("Contact geometry — PROVISIONAL [PAPER1 §5]")]
        [Tooltip("Treat each tracked joint as a sphere of this radius instead of a dimensionless point. " +
                 "PAPER1 §5 states the point-joint default is NOT acceptable for confirmatory analysis and " +
                 "requires capsule radii frozen from anthropometry with the source recorded. The values below " +
                 "are PROVISIONAL pilot defaults so violation rates are not floored by geometry; they are not " +
                 "the frozen model and must be replaced before confirmatory collection.")]
        [SerializeField] private bool useProvisionalLimbRadii = true;
        [Tooltip("Chest tracker to torso surface (m).")]
        [SerializeField] private float chestRadius = 0.12f;
        [Tooltip("Controller/wrist to hand surface (m).")]
        [SerializeField] private float handRadius = 0.08f;
        [Tooltip("Ankle tracker to foot surface (m).")]
        [SerializeField] private float footRadius = 0.10f;

        [Header("v2 incentive + familiarization [Design v2 §5/§7]")]
        [Tooltip("Deferred collision penalty: points lost per hazard collision, revealed ONLY on the end-of-block " +
                 "summary. NEVER surface a per-event signal — that would be a collision-feedback channel and " +
                 "would destroy the None condition.")]
        [SerializeField] private int collisionPenalty = 3;
        [Tooltip("Cue tour before practice: demo each haptic site + the Visual glow once, outside any measured " +
                 "block, so first-block cue novelty doesn't differ by condition.")]
        [SerializeField] private bool cueTour = true;

        [Header("Questionnaires (Plan Task 5.5 / F1) — needs a QuestionnairePanel in the scene")]
        [Tooltip("IPQ presence after each block.")]      [SerializeField] private bool surveyIpq = true;
        [Tooltip("NASA-TLX workload after each block.")] [SerializeField] private bool surveyTlx = true;
        [Tooltip("SSQ sickness baseline (pre) + after each block.")] [SerializeField] private bool surveySsq = true;
        [Tooltip("Warning acceptability (helpful / timely / trusted / annoying) after each block that actually " +
                 "delivers a warning — skipped for None. TIMELY is the subjective manipulation check for Timing.")]
        [SerializeField] private bool surveyCue = true;

        [Header("Scene wiring (auto-found if left empty)")]
        [SerializeField] private SceneObstacles sceneObstacles;
        [SerializeField] private OpportunitySpawner spawner;
        [SerializeField] private VisualObstacleAlert visualAlert;
        [SerializeField] private QuestionnairePanel questionnairePanel;
        [SerializeField] private OperatorEStop estop; // Plan Task 4.5 / D5 — emergency stop

        // The 5 cue-able joints the oracle/conditions/detector track (Head excluded — no head tactor).
        private static readonly List<Joint> Limbs = new()
        {
            Joint.Chest, Joint.LeftHand, Joint.RightHand, Joint.LeftFoot, Joint.RightFoot,
        };

        private IKeypointSource _source;
        private List<Obstacle> _obstacles;
        private List<BlockAssignment> _plan;
        private string _sessionDir;
        private QuestionnaireLogWriter _questionnaire; // Plan Task 5.5 / F1 — administered via QuestionnairePanel

        // Operator-gate + HUD state
        private bool _proceed;        // set by the on-screen button at each gate
        private bool _abort;          // set by the "Stop block" button / e-stop
        private bool _estop;          // latched emergency stop — halts the whole session [D5]
        private string _startedUtc;   // stamped once, written into session.json
        private bool _live;           // a block is currently running
        private string _phase = "Init";
        private string _status = "Starting…";
        private Condition _curCondition;
        private double _hudBlockTime;
        private float _hudTarget;
        private int _hudFrames, _hudAlerts, _hudOutcomes;

        // v2 deferred scoring + cue-tour state
        private int _sessionScore;        // running total across the 6 real blocks (practice excluded)
        private string _lastSummary;      // end-of-block score breakdown — the ONLY place the penalty surfaces
        private bool _tourWaiting;        // cue tour is at its operator gate

        private void OnEnable()
        {
            if (sceneObstacles == null) sceneObstacles = FindFirstObjectByType<SceneObstacles>();
            if (spawner == null) spawner = FindFirstObjectByType<OpportunitySpawner>();
            if (spawner != null) spawner.StandBy(); // claim it NOW — no self-paced stimuli before a block starts
            if (visualAlert == null) visualAlert = FindFirstObjectByType<VisualObstacleAlert>();
            if (questionnairePanel == null) questionnairePanel = FindFirstObjectByType<QuestionnairePanel>();
            if (trackerRig == null) trackerRig = FindFirstObjectByType<BodyTrackerRig>();
            if (estop == null) estop = FindFirstObjectByType<OperatorEStop>();
            if (estop != null) { estop.SetContextProvider(EStopContext); estop.OnStop += OnEmergencyStop; }

            _obstacles = sceneObstacles != null ? sceneObstacles.Collect() : new List<Obstacle>();
            if (_obstacles.Count == 0)
                Debug.LogWarning("[SessionRunner] No scene obstacles found — collisions can't be detected. " +
                                 "Add a SceneObstacles component over the O1/O2/O3 BoxColliders.");

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
            _startedUtc = System.DateTime.UtcNow.ToString("o");

            // §12 treats participant-root append files as a release blocker, and the abort log defaults
            // to one. Redirect it the moment the session directory exists, so the safety record lives
            // with the data it describes and the directory is self-contained.
            if (estop != null) estop.SetLogDirectory(_sessionDir);

            // A session run in protocol-development mode must be identifiable LATER, by someone who was not
            // in the room. A console warning scrolls away; a file in the session directory does not.
            if (protocolDevelopmentMode)
                File.WriteAllText(Path.Combine(_sessionDir, "PROTOCOL_DEVELOPMENT.txt"), string.Join("\n", new[]
                {
                    "This session ran with SessionRunner.protocolDevelopmentMode = TRUE.",
                    "",
                    "The preregistered opportunity-count gate was downgraded to a warning, so one or more",
                    $"blocks may carry fewer than {OpportunitySchedules.TargetOpportunitiesPerBlock} " +
                    "opportunities [PAPER1_STUDY_DESIGN section 4].",
                    "",
                    "THIS DATA IS NOT CONFIRMATORY. Do not pool it with study data and do not report it as",
                    "a result. It exists to develop the protocol - to time a session, check geometry, and",
                    "shake out the apparatus.",
                }));
            _questionnaire = new QuestionnaireLogWriter(Path.Combine(_sessionDir, "questionnaire.csv"));

            // v2 [Design v2 §6]: the six isometric variants are the default pool; the manual list is a debug path.
            var pool = useAllLayoutVariants
                ? new List<string>(LayoutVariants.StudyIds)
                : (layoutIds != null && layoutIds.Length > 0) ? new List<string>(layoutIds) : new List<string> { "L1" };
            _plan = SessionPlan.For(participantId, pool);

            if (trackerRig != null && trackerRig.IsComplete)
            {
                _source = trackerRig.CreateSource();
            }
            else
            {
                Debug.LogError("[SessionRunner] No complete BodyTrackerRig — assign the HMD + 5 Ultimate Tracker " +
                               "Transforms. ABORTING."); enabled = false; return;
            }

            Debug.Log($"[SessionRunner] P{participantId}: practice + {_plan.Count} blocks, order = " +
                      string.Join(" ", _plan.ConvertAll(b => b.Condition.ToString())) +
                      $". VIVE trackers; CSVs → {_sessionDir}");

            ValidateProtocol();
            StartCoroutine(RunSession());
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            if (estop != null) estop.OnStop -= OnEmergencyStop;
            (_source as System.IDisposable)?.Dispose();
            _source = null;
        }

        private IEnumerator RunSession()
        {
            // Baseline sickness before any VR exposure (session-level, block -1).
            if (surveySsq) yield return AdministerOne(Questionnaire.Ssq(), -1, practiceCondition);

            // v2 cue tour [Design v2 §7]: every cue met once, outside any measured block.
            if (cueTour && !_estop) yield return CueTour();

            // Practice (excluded from analysis) on the dedicated practice variant — never a test layout.
            if (!_estop) yield return RunBlock(new BlockAssignment(-1, practiceCondition, PracticeLayout()), isPractice: true);

            // The 6 counterbalanced condition blocks: break → block → post-block questionnaires.
            foreach (BlockAssignment a in _plan)
            {
                yield return Break();
                if (_estop) break;
                yield return RunBlock(a, isPractice: false);
                if (_estop) break;
                yield return AdministerQuestionnaires(a.BlockIndex, a.Condition);
            }

            if (_estop)
            {
                _phase = "STOPPED";
                _status = $"EMERGENCY STOP — session halted. Partial data in {_sessionDir}";
                Debug.LogWarning($"[SessionRunner] {_status}");
                yield break;
            }

            // Manifest LAST, after every writer has closed [§12]. Hashing earlier would certify a
            // half-written state that never existed on disk, which is worse than not hashing at all.
            WriteSessionMetadata(_estop ? "stopped" : "complete");
            _questionnaire = null;   // release the writer before the walk
            AnalysisManifestWriter.Write(
                _sessionDir, "Paper1", participantId,
                notes: protocolDevelopmentMode
                    ? new[] { "protocolDevelopmentMode was ON - NOT confirmatory data." }
                    : null);

            _phase = "DONE";
            _status = $"Session complete for P{participantId}. Files in {_sessionDir}";
            Debug.Log($"[SessionRunner] {_status}");
        }

        private IEnumerator Break()
        {
            _phase = "BREAK";
            _proceed = false;
            float t = 0f;
            while ((t < minBreakSeconds || !_proceed) && !_estop)
            {
                t += Time.unscaledDeltaTime;
                _status = t < minBreakSeconds
                    ? $"Rest — {Mathf.Max(0f, minBreakSeconds - t):F0}s minimum, HMD may be removed…"
                    : "Rest — click \"Next\" when the participant is ready.";
                yield return null;
            }
        }

        // Post-block battery (Plan Task 5.5 / F1): presence, workload, sickness as toggled.
        private IEnumerator AdministerQuestionnaires(int block, Condition cond)
        {
            if (surveyIpq) yield return AdministerOne(Questionnaire.Ipq(), block, cond);
            if (surveyTlx) yield return AdministerOne(Questionnaire.NasaTlx(), block, cond);
            // Warning acceptability only where a warning exists — the items are meaningless under None.
            if (surveyCue && cond != Condition.None)
                yield return AdministerOne(Questionnaire.CueAcceptability(), block, cond);
            if (surveySsq) yield return AdministerOne(Questionnaire.Ssq(), block, cond);
        }

        // Show one instrument on the QuestionnairePanel, wait for submit, record the scored measures.
        private IEnumerator AdministerOne(Questionnaire q, int block, Condition cond)
        {
            if (questionnairePanel == null)
            {
                Debug.LogWarning($"[SessionRunner] No QuestionnairePanel in scene — skipping {q.Instrument} " +
                                 $"(block {block}). Add one to collect presence/workload/sickness.");
                yield break;
            }
            _phase = $"SURVEY {q.Instrument}";
            _status = $"{q.Instrument}: participant answering on the operator screen…";
            bool done = false;
            IReadOnlyDictionary<string, float> measures = null;
            IReadOnlyDictionary<string, int> responses = null;
            // Take BOTH: the scored measures are what the analysis reads, the raw items are what make
            // the scoring auditable and re-scorable [§12]. Items cannot be recovered from scores.
            questionnairePanel.Administer(q, (m, r) => { measures = m; responses = r; done = true; });
            yield return new WaitUntil(() => done || _estop);
            if (_estop) yield break;
            RecordQuestionnaire(block, cond, q.Instrument, measures, responses);
            Debug.Log($"[SessionRunner] recorded {q.Instrument} (block {block}, {cond}).");
        }

        // v2 cue tour [Design v2 §7]: play the study cue once on every haptic site (with a plain-language
        // label the operator reads aloud) and demo the Visual glow once — all OUTSIDE any measured block, so
        // first-block cue novelty is identical across conditions and no condition gets a familiarity edge.
        private IEnumerator CueTour()
        {
            _phase = "CUE TOUR";
            _status = "Cue familiarization — participant suited, headset on. Click \"Start tour\".";
            _proceed = false;
            _tourWaiting = true;
            yield return new WaitUntil(() => _proceed || _estop);
            _tourWaiting = false;
            if (_estop) yield break;

            CueIntensityTable gains = string.IsNullOrWhiteSpace(cueIntensityFile)
                ? CueIntensityTable.Uniform(hapticIntensity)
                : CueIntensityFile.Load(cueIntensityFile);

            foreach (HapticSite site in System.Enum.GetValues(typeof(HapticSite)))
            {
                if (_estop) yield break;
                _status = $"Cue tour: this is the warning on your {SiteLabel(site)}.";
                if (useLiveHaptics) yield return HapticDeviceBinding.PlayThreePulse(site, gains.For(site));
                yield return new WaitForSeconds(1.2f);
            }

            if (visualAlert != null && !_estop)
            {
                _status = "Cue tour: the VISUAL warning — a glow appears in the headset where a hazard is close.";
                visualAlert.Configure(_obstacles, Limbs, new OracleParams().ReactiveDistance);
                yield return visualAlert.DemoGlow(3f);
            }
            _status = "Cue tour complete.";
            Debug.Log("[SessionRunner] Cue tour complete — all sites + visual glow demonstrated.");
        }

        private static string SiteLabel(HapticSite site) => site switch
        {
            HapticSite.Chest     => "chest",
            HapticSite.LeftHand  => "left hand",
            HapticSite.RightHand => "right hand",
            HapticSite.LeftShin  => "left shin",
            HapticSite.RightShin => "right shin",
            _                    => site.ToString(),
        };

        // Emergency stop [Plan Task 4.5 / D5]: abort the running block now, silence haptics, latch session halt.
        private void OnEmergencyStop(string reason)
        {
            _estop = true;
            _abort = true;
            HapticDeviceBinding.StopAll();
        }

        private string EStopContext() =>
            $"P{participantId} | {_phase} | {_curCondition} | t={_hudBlockTime:F1}s | frames={_hudFrames}";

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
            // v2 [Design v2 §6]: pose the scene volumes for this block's layout variant, THEN collect them, so
            // the detector, the spawner stimuli, and the visual alert all see the same transformed geometry.
            if (sceneObstacles != null)
            {
                sceneObstacles.ApplyVariant(a.LayoutId);
                _obstacles = sceneObstacles.Collect();
            }

            IFeedbackSink sink = useLiveHaptics ? CreateHapticSink() : null;
            var oracleParams = new OracleParams { PipelineLatencySeconds = pipelineLatencySeconds };
            List<Opportunity> schedule = ScheduleFor(a.LayoutId);

            // GATE: the schedule must supply the preregistered opportunity count before any CONFIRMATORY
            // block runs [PAPER1_STUDY_DESIGN §4]. The analysis counts the rows it is given (CODE_GAP #1),
            // so a short schedule produces clean-looking data against a preregistration that says otherwise
            // and nothing downstream would ever catch it.
            //
            // Practice is exempt: it is excluded from analysis by construction, and a practice block is
            // exactly where an operator may legitimately want a shorter run.
            //
            // protocolDevelopmentMode downgrades the gate to a warning. It exists because measuring session
            // duration — itself a precondition for finalising the opportunity count — requires running a
            // FULL session, which would otherwise be impossible until the schedule it is meant to inform
            // already existed. The flag is deliberately named for what it permits, defaults to false, and
            // stamps every affected block so the data can never be mistaken for confirmatory.
            if (!isPractice)
            {
                if (protocolDevelopmentMode)
                {
                    if (schedule.Count != OpportunitySchedules.TargetOpportunitiesPerBlock)
                        Debug.LogWarning($"[SessionRunner] PROTOCOL DEVELOPMENT MODE: block {a.Condition} ran " +
                            $"{schedule.Count} opportunities, not the preregistered " +
                            $"{OpportunitySchedules.TargetOpportunitiesPerBlock}. This data is NOT analysable " +
                            "as confirmatory - see PROTOCOL_DEVELOPMENT.txt in the session directory.");
                }
                else OpportunitySchedules.AssertMeetsTarget(schedule, a.LayoutId);
            }

            var block = new BlockRunner(ctx, _obstacles, Limbs, schedule, oracleParams, CreateDetectorParams(), sink);

            // An opportunity only counts toward the primary denominator if its stimulus actually presented
            // [PAPER1 §5], so the spawner reports each spawn back to the block as it happens.
            System.Action<Layout1Stimulus> onSpawned = s => block.MarkPresented(s.Id);
            if (spawner != null)
            {
                spawner.SetLayout(a.LayoutId);
                spawner.Spawned += onSpawned;
                spawner.DriveExternally();
            }
            if (visualAlert != null) visualAlert.Configure(_obstacles, Limbs, oracleParams.ReactiveDistance);

            // v2 [Design v2 §5]: per-block score baseline (spawner counters are cumulative across blocks).
            int deliveries0 = spawner != null ? spawner.Deliveries : 0;
            int hits0 = spawner != null ? spawner.Hits : 0;

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
                    rebased.Timestamp = f.Timestamp - t0;                    // block-relative (trackers already in VR frame)
                    blockTime = rebased.Timestamp;
                    latest = rebased;
                    block.Tick(rebased);
                    kp.Write(rebased);
                    _hudFrames++;
                    advanced = true;
                }

                if (started)
                {
                    if (advanced && spawner != null)
                    {
                        spawner.Tick(blockTime);
                        spawner.CheckInteractions(latest); // task scoring: orbs collected, projectiles dodged
                    }
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
            if (spawner != null) spawner.Spawned -= onSpawned;

            // ── Finish + persist ────────────────────────────────────────
            if (_abort) block.MarkAborted();   // must precede Finish(): it decides opportunity validity
            BlockResult r = block.Finish();
            WriteCsvs(ctx, r, block, isPractice);

            // PBC delivery health [H4']. The continuous cue is throttled to a rate the BLE device can
            // sustain, so what reached the skin is an approximation of ContinuousCueMapping's smooth ramp.
            // Report it: if the submit rate is low the cue was steppier than designed, and H4' is then
            // measuring a coarser stimulus than the one the paper describes. Measured, not assumed.
            if (ConditionManager.IsContinuous(a.Condition))
            {
                var gate = HapticDeviceBinding.LastContinuousGate;
                if (gate != null)
                {
                    Debug.Log($"[SessionRunner] PBC cue delivery: {gate.Submitted} submitted, {gate.Dropped} throttled " +
                              $"({100f * gate.SubmitRate:F0}% reached the device) · engagements={block.AlertEpisodes} " +
                              $"· dose={block.CueDoseSeconds:F2} intensity-seconds.");
                    if (gate.SubmitRate < 0.25f)
                        Debug.LogWarning("[SessionRunner] Fewer than a quarter of continuous updates reached the " +
                                         "device. The PBC ramp is coarse; lower continuousIntervalMillis or treat " +
                                         "H4' as testing a stepped cue rather than a smooth one.");
                    gate.ResetCounters();
                }
                else
                {
                    Debug.LogWarning("[SessionRunner] PBC block ran without a continuous-cue gate. The sink was " +
                                     "probably built with CreateThreePulseSink, which cannot deliver PBC.");
                }
            }
            if (_abort) Debug.LogWarning($"[SessionRunner] {_phase} ({a.Condition}) ABORTED by operator at {blockTime:F1}s; partial data written.");
            else
            {
                // Report the PRIMARY outcome directly: violations over VALID opportunities. Pilot gate 10 needs
                // a rate that is neither floor nor ceiling, and that cannot be read off a raw collision count.
                int valid = 0, violations = 0, presented = 0;
                foreach (OpportunityOutcome o in block.OpportunityOutcomes)
                {
                    if (o.Presented) presented++;
                    if (!o.Valid) continue;
                    valid++;
                    violations += o.Violation;
                }
                string rate = valid > 0 ? $"{(float)violations / valid:P0}" : "n/a";
                Debug.Log($"[SessionRunner] {_phase} ({a.Condition}) done: {_hudFrames} frames, alerts={r.Alerts}. " +
                          $"PRIMARY: {violations}/{valid} valid opportunities violated ({rate}); " +
                          $"{presented}/{block.OpportunityOutcomes.Count} presented. " +
                          $"[block-level collisions={r.Collisions}]");

                ReportTimingManipulation(block);
            }

            // v2 DEFERRED score summary [Design v2 §5] — the ONLY moment the collision penalty surfaces.
            // No sound, flash, rumble, or counter change ever happens at collision time; a per-event signal
            // would be a collision-feedback channel and would destroy the None condition.
            int delivered = spawner != null ? spawner.Deliveries - deliveries0 : 0;
            int hitsTaken = spawner != null ? spawner.Hits - hits0 : 0;
            int penalty = r.Collisions * collisionPenalty;
            int blockScore = delivered - hitsTaken - penalty;
            if (!isPractice) _sessionScore += blockScore;
            _lastSummary = $"ROUND RESULT{(isPractice ? " (practice)" : "")}   " +
                           $"orbs +{delivered}   projectile hits −{hitsTaken}   " +
                           $"hazard contacts {r.Collisions} × −{collisionPenalty} = −{penalty}\n" +
                           $"Round score: {blockScore}     Session total: {_sessionScore}";
            Debug.Log($"[SessionRunner] {_lastSummary.Replace('\n', ' ')}");
        }

        // v2 [Design v2 §6]: every variant's schedule derives from L1 by the same isometry that moves the scene
        // volumes and stimuli (mirrors swap left/right target limbs). Unknown ids fall back to L1 with a warning.
        private List<Opportunity> ScheduleFor(string layoutId)
        {
            if (!LayoutVariants.TryGet(layoutId, out _, out _))
                Debug.LogWarning($"[SessionRunner] Layout '{layoutId}' is not a known variant (L1–L6/LP) — " +
                                 "using L1 geometry.");
            return LayoutVariants.Schedule(layoutId);
        }

        private string PracticeLayout() => useAllLayoutVariants ? LayoutVariants.PracticeId : FirstLayout();

        // Per-limb effective contact radii. Without these every joint is a dimensionless point and a violation
        // requires the tracker itself to pass within ContactDistance (0.03 m) of the hazard surface, which
        // floors the primary outcome for a pilot. PROVISIONAL values — see the Inspector tooltip and §5.
        private DetectorParams CreateDetectorParams()
        {
            var p = new DetectorParams();
            if (!useProvisionalLimbRadii) return p;
            p.LimbContactRadius = new Dictionary<Joint, float>
            {
                { Joint.Chest, chestRadius },
                { Joint.LeftHand, handRadius },
                { Joint.RightHand, handRadius },
                { Joint.LeftFoot, footRadius },
                { Joint.RightFoot, footRadius },
            };
            return p;
        }

        // Protocol-param sanity check + provenance log [Plan Task 4.4 / D4]. Catches a blockSeconds set too
        // short to capture the last scripted opportunity (which would silently truncate the DV denominator),
        // and prints the reconciled timing for the operator. Values track Appendix A; reconcile vs the docs (A1).
        private void ValidateProtocol()
        {
            List<Opportunity> sched = ScheduleFor(FirstLayout());
            double end = 0;
            foreach (Opportunity op in sched) if (op.CloseTime > end) end = op.CloseTime;
            if (blockSeconds < end)
                Debug.LogWarning($"[SessionRunner] blockSeconds={blockSeconds:F0}s < last opportunity close " +
                                 $"({end:F0}s) — the final opportunities would be truncated. Set blockSeconds ≥ {end:F0}s.");
            Debug.Log($"[SessionRunner] Protocol: block {blockSeconds:F0}s · practice {practiceSeconds:F0}s · " +
                      $"break ≥{minBreakSeconds:F0}s · {sched.Count} opportunities (last closes {end:F0}s) · " +
                      $"latency comp {pipelineLatencySeconds * 1000f:F0} ms. [reconcile vs Appendix A / docs]");

            // v2 protocol checks [Design v2 §5–§7].
            if (practiceCondition != Condition.None)
                Debug.LogWarning($"[SessionRunner] practiceCondition={practiceCondition} — the v2 protocol uses " +
                                 "None (the cue tour familiarizes the cues; practicing under a live condition " +
                                 "would privilege it). Set it to None in the Inspector.");
            Debug.Log("[SessionRunner] v2: layouts " +
                      (useAllLayoutVariants
                          ? $"{string.Join(" ", LayoutVariants.StudyIds)} (practice {LayoutVariants.PracticeId})"
                          : string.Join(" ", layoutIds)) +
                      $" · collision penalty −{collisionPenalty}/contact (deferred to block end) · " +
                      $"cue tour {(cueTour ? "on" : "off")}.");

            // EVERY hazard the schedule targets must exist in the scene. A missing id is silent and fatal to the
            // primary outcome: that opportunity can never register a violation, so it looks like perfect
            // avoidance forever. Fail loudly at startup instead of discovering it in the data.
            var sceneIds = new HashSet<string>();
            foreach (Obstacle o in _obstacles) sceneIds.Add(o.Id);
            var missing = new List<string>();
            foreach (Opportunity op in sched)
                if (!sceneIds.Contains(op.TargetObstacleId) && !missing.Contains(op.TargetObstacleId))
                    missing.Add(op.TargetObstacleId);

            if (missing.Count > 0)
                Debug.LogError($"[SessionRunner] SCHEDULE TARGETS MISSING HAZARDS: {string.Join(", ", missing)}. " +
                               $"Scene has: {string.Join(", ", sceneIds)}. Those opportunities can NEVER record a " +
                               "violation and will read as perfect avoidance. Fix the obstacle names before running.");
            else
                Debug.Log($"[SessionRunner] hazard check OK — all {sceneIds.Count} scene volumes resolve " +
                          $"({string.Join(", ", sceneIds)}).");

            // Contact geometry provenance, so the log records which model produced the numbers.
            Debug.Log(useProvisionalLimbRadii
                ? $"[SessionRunner] contact geometry: PROVISIONAL radii chest={chestRadius:F3} hand={handRadius:F3} " +
                  $"foot={footRadius:F3} m + {new DetectorParams().ContactDistance:F3} m band. NOT the frozen " +
                  "anthropometric model — pilot only [PAPER1 §5]."
                : "[SessionRunner] contact geometry: POINT JOINTS (no limb radii). §5 rules this out for " +
                  "confirmatory analysis, and it will likely floor the violation rate.");
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

            // The PRIMARY dataset [PAPER1 §5/§9]: one row per scheduled opportunity. summary.csv is now the
            // block-level sensitivity analysis, not the confirmatory outcome.
            new OpportunityLogWriter(Path.Combine(_sessionDir, $"{prefix}opportunities.csv"))
                .WriteBlock(ctx, block.OpportunityOutcomes);
        }


        /// <summary>
        /// TIMING MANIPULATION CHECK [PAPER1_STUDY_DESIGN §6]. Reports what BOTH warning policies would have
        /// done this block, from <see cref="PolicyTriggerProbe"/> — in every condition, including None.
        ///
        /// H1 assumes the predictive policy fires EARLIER than the proximity policy. It does not always:
        /// proximity fires at a fixed distance and prediction at a fixed time, so prediction leads only when
        /// the limb has enough clear approach distance. An opportunity whose manipulation ran backwards looks
        /// identical to a working one in the violation column, which is why it is surfaced here rather than
        /// left for the analysis. The counting lives in <see cref="PolicyTriggerSummary"/> (Core, tested);
        /// this only decides how loudly to say it.
        /// </summary>
        private void ReportTimingManipulation(BlockRunner block)
        {
            PolicyTriggerSummary summary = PolicyTriggerSummary.Summarize(block.OpportunityOutcomes);
            string line = "[SessionRunner] " + summary.Describe();
            if (summary.Clean) Debug.Log(line);
            else Debug.LogWarning(line);
        }

        private void FlushSource()
        {
            while (_source != null && _source.TryGetFrame(out _)) { /* discard stale queue */ }
        }

        // The live cue sink: per-site calibrated gains if a cueIntensityFile is set [Plan Task 3.1 / E1],
        // otherwise a uniform hapticIntensity (identical to the pre-E1 behavior).
        private BHapticsSink CreateHapticSink()
        {
            return string.IsNullOrWhiteSpace(cueIntensityFile)
                ? HapticDeviceBinding.CreateStudySink(this, hapticIntensity)
                : HapticDeviceBinding.CreateStudySink(this, CueIntensityFile.Load(cueIntensityFile));
        }

        /// <summary>
        /// Record one scored questionnaire into the per-participant <c>questionnaire.csv</c>
        /// (instrument = "IPQ" / "NASA_TLX" / "SSQ"; block = -1 for session-level e.g. SSQ pre/post).
        /// Call this from the questionnaire UI once items are scored. [Plan Task 5.5 — UI not built yet.]
        /// </summary>
        public void RecordQuestionnaire(int block, Condition condition, string instrument,
                                        IReadOnlyDictionary<string, float> measures)
            => RecordQuestionnaire(block, condition, instrument, measures, null);

        /// <summary>
        /// Write `session.json` [§12 "session and block metadata"].
        ///
        /// This is the file that lets the dataset explain itself: which code, which devices, which
        /// parameter values were in force. Everything else in the directory records the participant's
        /// behaviour; this records the apparatus. Without it a result cannot be reproduced once the
        /// defaults move - and the pilot exists to move them.
        /// </summary>
        private void WriteSessionMetadata(string completion)
        {
            if (string.IsNullOrEmpty(_sessionDir)) return;
            try
            {
                var p = new OracleParams { PipelineLatencySeconds = pipelineLatencySeconds };

                var sections = new Dictionary<string, IReadOnlyDictionary<string, string>>
                {
                    ["software"] = SessionMetadata.Section(
                        ("unity", Application.unityVersion),
                        ("application", Application.version),
                        ("platform", Application.platform.ToString())),

                    ["protocol"] = SessionMetadata.Section(
                        ("conditions", string.Join("|", System.Array.ConvertAll(SessionPlan.Conditions, c => c.ToString()))),
                        ("block_seconds", SessionMetadata.Num(blockSeconds)),
                        ("practice_seconds", SessionMetadata.Num(practiceSeconds)),
                        ("practice_condition", practiceCondition.ToString()),
                        ("layout_variants", useAllLayoutVariants ? "L1-L6" : string.Join("|", layoutIds)),
                        ("opportunities_target", OpportunitySchedules.TargetOpportunitiesPerBlock.ToString()),
                        ("protocol_development_mode", protocolDevelopmentMode ? "true" : "false")),

                    // The numbers a reviewer will ask about, recorded as they were AT COLLECTION.
                    ["parameters"] = SessionMetadata.Section(
                        ("proximity_distance_m", SessionMetadata.Num(p.ReactiveDistance)),
                        ("predictive_ttc_s", SessionMetadata.Num(p.PredictiveTtc)),
                        ("predictive_release_margin_s", SessionMetadata.Num(p.PredictiveReleaseMargin)),
                        ("pipeline_latency_s", SessionMetadata.Num(pipelineLatencySeconds)),
                        ("continuous_cue_gamma", SessionMetadata.Num(p.ContinuousCueGamma)),
                        ("continuous_cue_min_intensity", SessionMetadata.Num(p.ContinuousCueMinIntensity)),
                        ("min_approach_for_valid_timing_m",
                            SessionMetadata.Num(OracleParams.MinApproachDistanceForValidTiming))),

                    ["contact_model"] = SessionMetadata.Section(
                        ("provisional_radii", useProvisionalLimbRadii ? "true" : "false"),
                        ("chest_radius_m", SessionMetadata.Num(chestRadius)),
                        ("hand_radius_m", SessionMetadata.Num(handRadius)),
                        ("foot_radius_m", SessionMetadata.Num(footRadius))),

                    ["devices"] = SessionMetadata.Section(
                        ("haptics", useLiveHaptics ? "bHaptics Tactosy" : "none (logged only)"),
                        ("cue_intensity_file", string.IsNullOrWhiteSpace(cueIntensityFile)
                            ? $"uniform {hapticIntensity}" : cueIntensityFile),
                        ("tracking", "VIVE Ultimate Trackers via SteamVR")),
                };

                File.WriteAllText(
                    Path.Combine(_sessionDir, SessionMetadata.FileName),
                    SessionMetadata.Build("Paper1", participantId, _startedUtc,
                                          System.DateTime.UtcNow.ToString("o"), completion, sections));
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SessionRunner] Could not write {SessionMetadata.FileName}: {e.Message}");
            }
        }

        /// <summary>Persist scored measures and, when available, the raw item responses [§12].</summary>
        public void RecordQuestionnaire(int block, Condition condition, string instrument,
                                        IReadOnlyDictionary<string, float> measures,
                                        IReadOnlyDictionary<string, int> responses)
        {
            _questionnaire?.Append(participantId, block, condition.ToString(), instrument,
                                   measures, responses);
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
            if (_tourWaiting)
                if (GUI.Button(new Rect(12, 120, 160, 32), "▶ Start tour")) _proceed = true;
            if (_live)
                if (GUI.Button(new Rect(12, 120, 160, 32), "■ Stop block")) _abort = true;

            // v2 DEFERRED score summary [Design v2 §5]: shown between blocks only — never during a live block
            // (no per-event signal) and never during the questionnaires (knowing the round result before rating
            // presence/workload could bias the ratings). It appears once the surveys are done.
            if (!_live && !_phase.StartsWith("SURVEY") && !string.IsNullOrEmpty(_lastSummary))
                GUI.Label(new Rect(12, 164, 960, 60), _lastSummary, style);
        }
    }
}
