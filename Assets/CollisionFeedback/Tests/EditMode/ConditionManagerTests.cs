using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CollisionFeedback.Core;
using Joint = CollisionFeedback.Core.Joint; // disambiguate from UnityEngine.Joint (physics component)

namespace CollisionFeedback.Tests
{
    public class ConditionManagerTests
    {
        // O2 "pillar" 2 m ahead at hand height; thin in X/Z, tall in Y.
        private static Obstacle Pillar =>
            new Obstacle("O2", new Vector3(0f, 1.0f, 2.0f), new Vector3(0.15f, 1.0f, 0.15f));

        private static readonly List<Joint> Limbs = new()
        {
            Joint.LeftHand, Joint.RightHand, Joint.LeftFoot, Joint.RightFoot,
        };

        private static (RecordingSink sink, ConditionManager cm) Build(Condition c, OracleParams p = null)
        {
            p ??= new OracleParams();
            var sink = new RecordingSink();
            var cm = new ConditionManager(c, p, new List<Obstacle> { Pillar }, Limbs, sink);
            return (sink, cm);
        }

        // Right hand starts in front of the chest (z=0) and sweeps +Z at 2 m/s into the pillar.
        // 90 Hz so the constant-velocity estimate has settled well before the obstacle.
        private static List<PoseFrame> RightHandApproach() =>
            SyntheticTrajectory.LinearApproach(
                Joint.RightHand,
                start: new Vector3(0.20f, 1.0f, 0.0f),
                velocity: new Vector3(0f, 0f, 2.0f),
                seconds: 1.0f,
                dt: 1f / 90f);

        private static void Run(ConditionManager cm, List<PoseFrame> frames)
        {
            foreach (var f in frames) cm.Tick(f);
        }

        [Test]
        public void None_never_fires()
        {
            var (sink, cm) = Build(Condition.None);
            Run(cm, RightHandApproach());
            Assert.That(sink.Fired, Is.Empty);
        }

        [Test]
        public void RG_fires_on_chest_reactively()
        {
            var (sink, cm) = Build(Condition.RG);
            Run(cm, RightHandApproach());

            Assert.That(sink.Fired, Is.Not.Empty);
            Assert.That(sink.Fired[0].Site, Is.EqualTo(HapticSite.Chest));
            Assert.That(sink.Fired[0].Trigger, Is.EqualTo(TriggerKind.Reactive));
            Assert.That(sink.Fired[0].Modality, Is.EqualTo(Modality.Haptic));
        }

        [Test]
        public void RB_fires_on_the_at_risk_limb_site()
        {
            var (sink, cm) = Build(Condition.RB);
            Run(cm, RightHandApproach());

            Assert.That(sink.Fired, Is.Not.Empty);
            Assert.That(sink.Fired[0].Site, Is.EqualTo(HapticSite.RightHand));
            Assert.That(sink.Fired[0].Limb, Is.EqualTo(Joint.RightHand));
        }

        [Test]
        public void PB_fires_on_the_at_risk_limb_and_earlier_than_RB()
        {
            var (sinkPb, cmPb) = Build(Condition.PB);
            Run(cmPb, RightHandApproach());

            var (sinkRb, cmRb) = Build(Condition.RB);
            Run(cmRb, RightHandApproach());

            Assert.That(sinkPb.Fired, Is.Not.Empty);
            Assert.That(sinkRb.Fired, Is.Not.Empty);
            Assert.That(sinkPb.Fired[0].Site, Is.EqualTo(HapticSite.RightHand));
            Assert.That(sinkPb.Fired[0].Trigger, Is.EqualTo(TriggerKind.Predictive));

            // Predictive lead-time: PB fires while the hand is still farther from the obstacle than RB.
            Assert.That(sinkPb.Fired[0].Distance, Is.GreaterThan(sinkRb.Fired[0].Distance));
        }

        [Test]
        public void Visual_condition_emits_visual_modality()
        {
            var (sink, cm) = Build(Condition.Visual);
            Run(cm, RightHandApproach());

            Assert.That(sink.Fired, Is.Not.Empty);
            Assert.That(sink.Fired[0].Modality, Is.EqualTo(Modality.Visual));
        }

        [Test]
        public void One_alert_per_approach_edge_triggered()
        {
            var (sink, cm) = Build(Condition.PB);
            Run(cm, RightHandApproach());

            // A single approach must yield exactly one alert, not one per frame.
            Assert.That(sink.Fired.Count, Is.EqualTo(1));
        }

        [Test]
        public void Arbitration_cues_only_the_lowest_TTC_limb_when_two_are_at_risk()
        {
            // Both hands sweep +Z into the pillar over the same frames; the right hand starts closer, so it
            // has the lower TTC and must be the ONE (and only) limb cued. Without arbitration + the re-fire
            // debounce, both hands would fire independently.
            var sink = new RecordingSink();
            var cm = new ConditionManager(Condition.PB, new OracleParams(),
                                          new List<Obstacle> { Pillar }, Limbs, sink);

            const float dt = 1f / 90f;
            for (int i = 0; i <= 27; i++) // ~0.3 s, both limbs still closing (no contact yet)
            {
                float t = i * dt;
                var j = SyntheticTrajectory.NeutralPose();
                j[(int)Joint.RightHand] = new Vector3(0.20f, 1.0f, 1.00f + 2.0f * t); // closer -> lower TTC
                j[(int)Joint.LeftHand]  = new Vector3(-0.20f, 1.0f, 0.90f + 2.0f * t);
                cm.Tick(new PoseFrame { Timestamp = t, Joints = j });
            }

            Assert.That(sink.Fired.Count, Is.EqualTo(1));                      // one cue, not one per limb
            Assert.That(sink.Fired[0].Limb, Is.EqualTo(Joint.RightHand));      // the lowest-TTC limb
            Assert.That(sink.Fired[0].Site, Is.EqualTo(HapticSite.RightHand)); // routed to that limb's site
        }

        [Test]
        public void Latency_compensation_makes_the_predictive_cue_fire_earlier()
        {
            var noComp = new OracleParams { PipelineLatencySeconds = 0f };
            var comp = new OracleParams { PipelineLatencySeconds = 0.3f };

            var (sinkA, cmA) = Build(Condition.PB, noComp);
            Run(cmA, RightHandApproach());
            var (sinkB, cmB) = Build(Condition.PB, comp);
            Run(cmB, RightHandApproach());

            Assert.That(sinkA.Fired, Is.Not.Empty);
            Assert.That(sinkB.Fired, Is.Not.Empty);
            // Forecasting ahead by the pipeline latency fires while the hand is still farther away.
            Assert.That(sinkB.Fired[0].Distance, Is.GreaterThan(sinkA.Fired[0].Distance));
        }

        [Test]
        public void Predictive_command_reports_the_soonest_obstacle_not_the_merely_nearest()
        {
            // A_back sits beside/behind the start: it is the NEAREST obstacle by distance at fire time,
            // but the hand is moving AWAY from it, so it never closes. B_ahead is the one approached.
            var obstacles = new List<Obstacle>
            {
                new Obstacle("A_back",  new Vector3(0.20f, 1f, -0.10f), new Vector3(0.05f, 1f, 0.05f)),
                new Obstacle("B_ahead", new Vector3(0.00f, 1f,  2.00f), new Vector3(0.15f, 1f, 0.15f)),
            };
            var limbs = new List<Joint> { Joint.RightHand };
            var sink = new RecordingSink();
            var cm = new ConditionManager(Condition.PB, new OracleParams(), obstacles, limbs, sink);

            foreach (var f in SyntheticTrajectory.LinearApproach(
                         Joint.RightHand, new Vector3(0f, 1f, 0f), new Vector3(0f, 0f, 2f), 1.0f, 1f / 90f))
                cm.Tick(f);

            Assert.That(sink.Fired, Is.Not.Empty);
            // Must describe the obstacle it forecast a collision with...
            Assert.That(sink.Fired[0].ObstacleId, Is.EqualTo("B_ahead"));
            // ...and report THAT obstacle's distance (~1 m), not the nearer non-threat A_back (~0.92 m).
            Assert.That(sink.Fired[0].Distance, Is.GreaterThan(0.95f));
        }
    }
}
