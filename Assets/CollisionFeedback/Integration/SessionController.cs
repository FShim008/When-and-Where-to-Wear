using System.Collections.Generic;
using System.IO;
using UnityEngine;
using CollisionFeedback.Core;
using CollisionFeedback.Runtime;

namespace CollisionFeedback.Integration
{
    /// <summary>
    /// Runs a full block END-TO-END with LIVE bHaptics cues. Plays the synthetic demo block in REAL TIME
    /// (frames paced by their timestamps), so cues fire exactly as the synthetic limb approaches each
    /// obstacle; routes the study's fixed 3-pulse cue to the real devices via
    /// <see cref="HapticDeviceBinding.CreateThreePulseSink"/>; and writes the summary + per-event CSVs.
    ///
    /// The real study swaps <see cref="SyntheticBlock"/> for a TrackerKeypointSource (drain TryGetFrame each
    /// frame instead of timestamp-pacing) — the BlockRunner / condition / cue logic is unchanged.
    ///
    /// Requires the <c>[bHaptics]</c> prefab in the scene and the bHaptics Player running.
    /// </summary>
    public sealed class SessionController : MonoBehaviour
    {
        [SerializeField] private Condition condition = Condition.PB;
        [SerializeField] private int participantId = 0;
        [SerializeField] private float hapticIntensity = 1f;
        [SerializeField] private bool useLiveHaptics = true;

        private List<PoseFrame> _frames;
        private int _i;
        private float _startTime;
        private BlockRunner _block;
        private BlockContext _ctx;
        private bool _done;

        private void Start()
        {
            SyntheticBlock.Data demo = SyntheticBlock.Demo();
            _frames = demo.Frames;
            _ctx = new BlockContext
            {
                ParticipantId = participantId, BlockIndex = 0, Condition = condition, LayoutId = "DEMO",
            };

            IFeedbackSink deviceSink = useLiveHaptics
                ? HapticDeviceBinding.CreateThreePulseSink(this, hapticIntensity)
                : null;

            _block = new BlockRunner(_ctx, demo.Obstacles, demo.Limbs, demo.Schedule,
                                     new OracleParams(), new DetectorParams(), deviceSink);

            _startTime = Time.time;
            Debug.Log($"[SessionController] Block in {condition} | liveHaptics={useLiveHaptics}. " +
                      (useLiveHaptics ? "You should FEEL 3-pulse cues fire on the at-risk limb's device. " : "") +
                      $"CSVs -> {Application.persistentDataPath}");
        }

        private void Update()
        {
            if (_done) return;

            // Real-time pacing: tick every frame whose data timestamp has now elapsed.
            float elapsed = Time.time - _startTime;
            while (_i < _frames.Count && _frames[_i].Timestamp <= elapsed)
                _block.Tick(_frames[_i++]);

            if (_i < _frames.Count) return; // block still playing

            _done = true;
            BlockResult r = _block.Finish();

            var summary = new CsvFileWriter(Path.Combine(Application.persistentDataPath, "session_summary.csv"));
            summary.EnsureHeader();
            summary.Append(r);

            new EventLogWriter(Path.Combine(Application.persistentDataPath, "session_events.csv"))
                .WriteBlock(_ctx, _block.Alerts, _block.Outcomes, _block.Opportunities);

            Debug.Log($"[SessionController] DONE [{condition}] collisions={r.Collisions}/{r.Opportunities} " +
                      $"(CPO={r.CollisionsPerOpportunity:F3}), nearMisses={r.NearMisses}, alerts={r.Alerts}, " +
                      $"avoidances={r.AvoidanceCount}. Wrote session_summary.csv + session_events.csv to " +
                      $"{Application.persistentDataPath}");
        }
    }
}
