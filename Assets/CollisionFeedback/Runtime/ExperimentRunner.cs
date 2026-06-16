using System.IO;
using UnityEngine;
using CollisionFeedback.Core;

namespace CollisionFeedback.Runtime
{
    /// <summary>
    /// HARDWARE-FREE end-to-end demo. Runs a synthetic demo block (<see cref="SyntheticBlock.Demo"/>)
    /// through the full <see cref="BlockRunner"/> pipeline in the chosen condition, then writes a real
    /// summary CSV + per-event CSV to <c>Application.persistentDataPath</c> and logs the block result.
    /// Drop it on an empty GameObject, pick a Condition, press Play, read the Console + open the CSVs.
    /// To go live later: swap MockKeypointSource -> UdpKeypointSource and add a bHaptics sink.
    /// </summary>
    public sealed class ExperimentRunner : MonoBehaviour
    {
        [SerializeField] private Condition condition = Condition.PB;
        [SerializeField] private int participantId = 0;

        private IKeypointSource _source;
        private BlockRunner _block;
        private BlockContext _ctx;
        private bool _done;

        private void Start()
        {
            SyntheticBlock.Data demo = SyntheticBlock.Demo();
            _ctx = new BlockContext { ParticipantId = participantId, BlockIndex = 0, Condition = condition, LayoutId = "DEMO" };
            _block = new BlockRunner(_ctx, demo.Obstacles, demo.Limbs, demo.Schedule,
                                     new OracleParams(), new DetectorParams());
            _source = new MockKeypointSource(demo.Frames);

            Debug.Log($"[ExperimentRunner] Running synthetic demo block in condition {condition}. " +
                      $"CSVs will be written to: {Application.persistentDataPath}");
        }

        private void Update()
        {
            if (_done) return;

            if (_source.TryGetFrame(out PoseFrame frame))
            {
                _block.Tick(frame);
                return;
            }

            // Frames exhausted -> finalize once.
            _done = true;
            BlockResult result = _block.Finish();

            var summary = new CsvFileWriter(Path.Combine(Application.persistentDataPath, "demo_block_summary.csv"));
            summary.EnsureHeader();
            summary.Append(result);

            new EventLogWriter(Path.Combine(Application.persistentDataPath, "demo_block_events.csv"))
                .WriteBlock(_ctx, _block.Alerts, _block.Outcomes, _block.Opportunities);

            Debug.Log($"[ExperimentRunner] DONE [{condition}] " +
                      $"collisions={result.Collisions}/{result.Opportunities} (CPO={result.CollisionsPerOpportunity:F3}), " +
                      $"nearMisses={result.NearMisses}, alerts={result.Alerts}, avoidances={result.AvoidanceCount}. " +
                      $"Wrote summary + event CSVs to {Application.persistentDataPath}");
        }
    }
}
