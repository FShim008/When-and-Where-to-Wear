using System.Collections.Generic;
using System.IO;
using UnityEngine;
using CollisionFeedback.Core;
using CollisionFeedback.Runtime;
using Joint = CollisionFeedback.Core.Joint; // disambiguate from UnityEngine.Joint (physics component)

namespace CollisionFeedback.Integration
{
    /// <summary>
    /// LIVE session driver: runs one real experimental block end-to-end from the camera-tracking stream.
    /// Reads obstacles from the SCENE (<see cref="SceneObstacles"/>), pulls calibrated keypoint frames from a
    /// <see cref="UdpKeypointSource"/>, runs the Layout-L1 12-event schedule through <see cref="BlockRunner"/>
    /// with a live 3-pulse bHaptics cue, drives the <see cref="OpportunitySpawner"/> on the same block clock,
    /// and writes the summary + per-event CSVs. (The hardware-free synthetic-demo path stays in
    /// <see cref="SessionController"/>.)
    ///
    /// Assumes the incoming stream is ALREADY in the VR / study world frame (camera->VR calibration applied
    /// upstream; see <c>RigidTransformSolver</c>). Block-relative time is derived by rebasing the first frame's
    /// timestamp to ~0, so the scheduler, oracle, and spawner all share one clock. The Visual condition emits
    /// Visual-modality alerts that the bHaptics sink ignores by design (the step-7 renderer will play those).
    ///
    /// Lives in Integration (Assembly-CSharp) so it can see the bHaptics SDK + the Core/Runtime assemblies.
    /// Needs the <c>[bHaptics]</c> prefab + the bHaptics Player running for live cues.
    /// </summary>
    public sealed class LiveSessionController : MonoBehaviour
    {
        [Header("Session")]
        [SerializeField] private Condition condition = Condition.PB;
        [SerializeField] private int participantId = 0;
        [SerializeField] private int blockIndex = 0;
        [SerializeField] private string layoutId = "L1";
        [SerializeField] private float blockSeconds = 180f;

        [Header("Tracking (UDP)")]
        [SerializeField] private int udpPort = 9000;

        [Header("Feedback")]
        [SerializeField] private bool useLiveHaptics = true;
        [SerializeField] private float hapticIntensity = 1f;
        [SerializeField] private float pipelineLatencySeconds = 0f; // set from the M3 latency measurement

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

        private UdpKeypointSource _source;
        private BlockRunner _block;
        private BlockContext _ctx;
        private bool _started;
        private bool _done;
        private double _t0;
        private double _blockTime;
        private float _firstFrameWall;
        private PoseFrame _latest;
        private RigidTransform _camToVr = RigidTransform.Identity; // camera-rig -> VR world frame; loaded from the calibration file on Start
        private KeypointLogWriter _kp; // raw per-frame VR-frame keypoint log (Plan Task 5.6)

        private void Start()
        {
            if (sceneObstacles == null) sceneObstacles = FindFirstObjectByType<SceneObstacles>();
            if (spawner == null) spawner = FindFirstObjectByType<OpportunitySpawner>();
            if (visualAlert == null) visualAlert = FindFirstObjectByType<VisualObstacleAlert>();

            List<Obstacle> obstacles = sceneObstacles != null ? sceneObstacles.Collect() : new List<Obstacle>();
            if (obstacles.Count == 0)
                Debug.LogWarning("[LiveSessionController] No scene obstacles found — collisions can't be detected. " +
                                 "Add a SceneObstacles component over the O1/O2/O3 BoxColliders.");

            _ctx = new BlockContext
            {
                ParticipantId = participantId, BlockIndex = blockIndex, Condition = condition, LayoutId = layoutId,
            };

            // Same 3-pulse sink for every condition: it self-ignores Visual-modality commands, so the Visual
            // condition logs alerts (for the renderer) without buzzing the device. None simply never fires.
            IFeedbackSink deviceSink = useLiveHaptics
                ? HapticDeviceBinding.CreateThreePulseSink(this, hapticIntensity)
                : null;

            var oracleParams = new OracleParams { PipelineLatencySeconds = pipelineLatencySeconds };
            _block = new BlockRunner(_ctx, obstacles, Limbs, OpportunitySchedules.Layout1(),
                                     oracleParams, new DetectorParams(), deviceSink);

            try { _source = new UdpKeypointSource(udpPort); }
            catch (System.Exception e) { Debug.LogError($"[LiveSessionController] UDP {udpPort} failed: {e.Message}"); }

            if (spawner != null) spawner.DriveExternally();
            if (visualAlert != null) visualAlert.Configure(obstacles, Limbs, oracleParams.ReactiveDistance);

            if (CameraVrCalibrationFile.TryLoad(out RigidTransform camToVr))
            {
                _camToVr = camToVr;
                Debug.Log($"[LiveSessionController] Loaded camera->VR calibration from {CameraVrCalibrationFile.DefaultPath}.");
            }
            else
            {
                Debug.LogWarning($"[LiveSessionController] No camera->VR calibration at {CameraVrCalibrationFile.DefaultPath} — " +
                                 "keypoints used AS-IS (Identity), so collisions vs scene obstacles will be wrong. " +
                                 "Run the CameraVrCalibration scene first.");
            }

            _kp = new KeypointLogWriter(
                Path.Combine(Application.persistentDataPath, $"keypoints_P{participantId}_B{blockIndex}_{condition}.csv"),
                participantId, blockIndex);

            Debug.Log($"[LiveSessionController] P{participantId} block {blockIndex} [{condition}] layout {layoutId}. " +
                      $"Listening UDP {udpPort}; liveHaptics={useLiveHaptics}, latency={pipelineLatencySeconds * 1000f:F0} ms. " +
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
                rebased.Timestamp = f.Timestamp - _t0;   // block-relative (a constant shift -> velocities unchanged)
                for (int j = 0; j < rebased.Joints.Length; j++)
                    rebased.Joints[j] = _camToVr.Apply(rebased.Joints[j]); // Camera-1 frame -> VR world frame (rigid: velocities/TTC preserved)
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

        private void OnDisable()
        {
            _source?.Dispose();
            _source = null;
            _kp?.Dispose();
            _kp = null;
        }
    }
}
