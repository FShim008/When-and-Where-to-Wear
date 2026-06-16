using NUnit.Framework;
using CollisionFeedback.Core;

namespace CollisionFeedback.Tests
{
    public class SyntheticBlockTests
    {
        [Test]
        public void Demo_block_runs_end_to_end_with_a_collision_and_three_opportunities()
        {
            SyntheticBlock.Data d = SyntheticBlock.Demo();
            Assert.That(d.Schedule.Count, Is.EqualTo(3));
            Assert.That(d.Frames.Count, Is.GreaterThan(100));

            var ctx = new BlockContext { ParticipantId = 0, BlockIndex = 0, Condition = Condition.None, LayoutId = "DEMO" };
            var runner = new BlockRunner(ctx, d.Obstacles, d.Limbs, d.Schedule, new OracleParams(), new DetectorParams());
            foreach (var f in d.Frames) runner.Tick(f);
            BlockResult r = runner.Finish();

            Assert.That(r.Opportunities, Is.EqualTo(3));
            Assert.That(r.Collisions, Is.GreaterThanOrEqualTo(1));           // right hand drives into O2
            Assert.That(r.OpportunitiesAvoided, Is.GreaterThanOrEqualTo(1)); // the stop-short approaches
            Assert.That(r.NearMisses, Is.GreaterThanOrEqualTo(1));           // stop-short limbs enter the near-miss band
        }

        [Test]
        public void Predictive_condition_produces_alerts_on_the_demo_block()
        {
            SyntheticBlock.Data d = SyntheticBlock.Demo();
            var ctx = new BlockContext { ParticipantId = 0, BlockIndex = 0, Condition = Condition.PB, LayoutId = "DEMO" };
            var runner = new BlockRunner(ctx, d.Obstacles, d.Limbs, d.Schedule, new OracleParams(), new DetectorParams());
            foreach (var f in d.Frames) runner.Tick(f);
            BlockResult r = runner.Finish();

            Assert.That(r.Alerts, Is.GreaterThanOrEqualTo(1));
            Assert.That(r.AvoidanceCount, Is.GreaterThanOrEqualTo(1)); // alerts -> limbs turn away -> avoidance events
        }
    }
}
