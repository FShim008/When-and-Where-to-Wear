using System.Collections.Generic;
using System.IO;
using UnityEngine;
using CollisionFeedback.Core;
using CollisionFeedback.Runtime;
using Joint = CollisionFeedback.Core.Joint; // disambiguate from UnityEngine.Joint (physics component)

namespace CollisionFeedback.Integration
{
    /// <summary>
    /// LIVE single-block driver: runs one real experimental block end-to-end from the VIVE Ultimate Tracker
    /// stream. Reads obstacles from the SCENE (<see cref="SceneObstacles"/>), pulls keypoint frames from a
    /// <see cref="TrackerKeypointSource"/> (already in the VR world frame — the trackers share the headset's
    /// SteamVR space, so NO camera→VR calibration is needed), runs the Layout-L1 12-event schedule through
    /// <see cref="BlockRunner"/> with a live 3-pulse bHaptics cue, drives the <see cref="OpportunitySpawner"/>
    /// on the same block clock, and writes the summary + per-event CSVs. (The hardware-free synthetic demo is
    /// <see cref="SessionController"/>; the full counterbalanced session is <see cref="SessionRunner"/>.)
    ///
    /// Block-relative time is rebased to the first frame's timestamp so the scheduler, oracle, and spawner
    /// share one clock. The Visual condition emits Visual-modality alerts the bHaptics sink ignores by design.
    ///
    /// Lives in Integration (Assembly-CSharp) so it can see the bHaptics SDK + Core/Runtime. Needs a
    /// <see cref="BodyTrackerRig"/> in the scene (HMD + 5 tracker Transforms) and the [bHaptics] prefab + Player.
    /// </summary>
    public sealed class LiveSessionController : MonoBehaviour
    {
        [Header("Session")]
        [SerializeField] private Condition condition = Condition.PB;
        [SerializeField] private int participantId = 0;
        [SerializeField] private int blockIndex = 0;
        [SerializeField] private string layoutId = "L1";
        [SerializeField] private float blockSeconds = 180f;

        [Header("Tracking (VIVE Ultimate Trackers — auto-found if empty)")]
        [SerializeField] private BodyTrackerRig trackerRig;

        [Header("Feedback")]
        [SerializeField] private bool useLiveHaptics = true;
        [SerializeField] private float hapticIntensity = 1f;
        [Tooltip("Optional per-site cue-gain CSV from the E2 perceptual-matching pass (Plan Task 3.1). Relative " +
                 "names resolve under persistentDataPath. Empty = uniform hapticIntensity (no equalization).")]
        [SerializeField] private string cueIntensityFile = "";
        [SerializeField] private float pipelineLatencySeconds = 0f; // tracker→cue latency (tiny on one PC; measure once)

        [Header("Scene wiring (auto-found if left empty)")]
        [SerializeField] private SceneObstacles sceneObstacles;     // reads the foam-obstacle BoxColliders
        [SerializeField] private OpportunitySpawner spawner;        // optional: the orb/projectile timeline
        [SerializeField] private VisualObstacleAlert visualAlert;   // optional: the Visual-condition highlight

        // The limbs the oracle / conditions / detector track: the 5 cue-able joints (chest + hands + feet).
        // Head is excluded — there is no head tactor and the L1 events never target it.
        private static readonly List<Joint> Limbs = new()
        {
            Joint.Chest, Joint.LeftHand, Joint.RightHand, Joint.LeftFoot, Joint.RightFoot,
        };

        private IKeypointSource _source;
        private BlockRunner _block;
        private BlockContext _ctx;
        private bool _started;
        private bool _done;
        private double _t0;
        private double _blockTime;
        private float _firstFrameWall;
        private PoseFrame _latest;
        private KeypointLogWriter _kp; // raw per-frame VR-frame keypoint log (Plan Task 5.6)

        private void Start()
        {
            if (sceneObstacles == null) sceneObstacles = FindFirstObjectByType<SceneObstacles>();
            if (spawner == null) spawner = FindFirstObjectByType<OpportunitySpawner>();
            if (visualAlert == null) visualAlert = FindFirstObjectByType<VisualObstacleAlert>();
            if (trackerRig == null) trackerRig = FindFirstObjectByType<BodyTrackerRig>();

            List<Obstacle> obstacles = sceneObstacles != null ? sceneObstacles.Collect() : new List<Obstacle>();
            if (obstacles.Count == 0)
                Debug.LogWarning("[LiveSessionController] No scene obstacles found — collisions can't be detected. " +
                                 "Add a SceneObstacles component over the O1/O2/O3 BoxColliders.");

            _ctx = new BlockContext
            {
                ParticipantId = participantId, BlockIndex = blockIndex, Condition = condition, LayoutId = layoutId,
            };

            IFeedbackSink deviceSink = useLiveHaptics ? CreateHapticSink() : null;

            var oracleParams = new OracleParams { PipelineLatencySeconds = pipelineLatencySeconds };
            _block = new BlockRunner(_ctx, obstacles, Limbs, OpportunitySchedules.Layout1(),
                                     oracleParams, new DetectorParams(), deviceSink);

            if (trackerRig != null && trackerRig.IsComplete)
                _source = trackerRig.CreateSource();
            else
                Debug.LogError("[LiveSessionController] No complete BodyTrackerRig — assign the HMD + 5 Ultimate " +
                               "Tracker Transforms (chest, both wrists, both ankles). No tracking until then.");

            if (spawner != null) spawner.DriveExternally();
            if (visualAlert != null) visualAlert.Configure(obstacles, Limbs, oracleParams.ReactiveDistance);

            _kp = new KeypointLogWriter(
                Path.Combine(Application.persistentDataPath, $"keypoints_P{participantId}_B{blockIndex}_{condition}.csv"),
                participantId, blockIndex);

            Debug.Log($"[LiveSessionController] P{participantId} block {blockIndex} [{condition}] layout {layoutId}. " +
                      $"VIVE trackers; liveHaptics={useLiveHaptics}, latency={pipelineLatencySeconds * 1000f:F0} ms. " +
                      $"CSVs -> {Application.persistentDataPath}");
        }

        private void Update()
        {
            if (_done || _source == null) return;

            bool advanced = false;
            while (_source.TryGetFrame(out PoseFrame f))
            {
                if (!_started) { _t0 = f.Timestamp; _started = true; _firstFrameWall = Time.unscaledTime; }

                PoseFrame rebased = f;
                rebased.Timestamp = f.Timestamp - _t0;   // block-relative (trackers already in VR frame; a constant shift)
                _blockTime = rebased.Timestamp;
                _latest = rebased;
                _block.Tick(rebased);
                _kp?.Write(rebased);
                advanced = true;
            }

            if (!_started) return;                       // still waiting for the first tracking frame

            if (advanced && spawner != null) spawner.Tick(_blockTime);
            if (visualAlert != null) visualAlert.UpdatePose(_latest, condition == Condition.Visual, Time.deltaTime);

            // Finish on block time, with a wall-clock guard so a stalled stream still terminates the block.
            bool blockTimeDone = _blockTime >= blockSeconds;
            bool wallGuard = (Time.unscaledTime - _firstFrameWall) > blockSeconds + 10f;
            if (blockTimeDone || wallGuard) Finish();
        }

        private void Finish()
        {
            _done = true;
            BlockResult r = _block.Finish();

            var summary = new CsvFileWriter(Path.Combine(Application.persistentDataPath, "session_summary.csv"));
            summary.EnsureHeader();
            summary.Append(r);

            new EventLogWriter(Path.Combine(Application.persistentDataPath, "session_events.csv"))
                .WriteBlock(_ctx, _block.Alerts, _block.Outcomes, _block.Opportunities);

            int score = spawner != null ? spawner.Score : 0;
            Debug.Log($"[LiveSessionController] DONE [{condition}] collisions={r.Collisions}/{r.Opportunities} " +
                      $"(CPO={r.CollisionsPerOpportunity:F3}), nearMisses={r.NearMisses}, alerts={r.Alerts}, " +
                      $"avoidances={r.AvoidanceCount}, score={score}. Wrote session_summary.csv + session_events.csv " +
                      $"to {Application.persistentDataPath}");
        }

        // The live cue sink: per-site calibrated gains if a cueIntensityFile is set [Plan Task 3.1 / E1],
        // otherwise a uniform hapticIntensity (identical to the pre-E1 behavior).
        private BHapticsSink CreateHapticSink()
        {
            return string.IsNullOrWhiteSpace(cueIntensityFile)
                ? HapticDeviceBinding.CreateThreePulseSink(this, hapticIntensity)
                : HapticDeviceBinding.CreateThreePulseSink(this, CueIntensityFile.Load(cueIntensityFile));
        }

        private void OnDisable()
        {
            (_source as System.IDisposable)?.Dispose();
            _source = null;
            _kp?.Dispose();
            _kp = null;
        }
    }
}
