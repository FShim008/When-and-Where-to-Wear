using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CollisionFeedback.Core;
using Joint = CollisionFeedback.Core.Joint; // disambiguate from UnityEngine.Joint (physics component)

namespace CollisionFeedback.Tests
{
    /// <summary>
    /// The counterfactual timing manipulation check [PAPER1_STUDY_DESIGN §6].
    ///
    /// H1 assumes the predictive policy fires earlier than the proximity policy. That is not guaranteed:
    /// proximity fires at a fixed DISTANCE and prediction at a fixed TIME, so prediction leads only when the
    /// limb has enough clear approach distance and enough closing speed. <see cref="PolicyTriggerProbe"/>
    /// records what BOTH policies would have done on every opportunity in every condition, so an opportunity
    /// whose manipulation ran backwards is visible in the data instead of silently averaging into H1.
    /// </summary>
    public class PolicyTriggerProbeTests
    {
        private const float Hz = 90f;
        private const float Dt = 1f / Hz;
        private static readonly Vector3 Far = new(50f, 50f, 50f);
        private static readonly Joint Limb = Joint.RightHand;

        // Single hazard centred at z = 2, half-depth 0.2 → near face at z = 1.8.
        private const float SurfaceZ = 1.8f;

        private static List<Obstacle> Obstacles() => new()
        {
            new Obstacle("O1", new Vector3(0f, 1f, 2f), new Vector3(0.2f, 0.2f, 0.2f)),
        };

        private static List<Joint> Limbs() => new()
        {
            Joint.Chest, Joint.LeftHand, Joint.RightHand, Joint.LeftFoot, Joint.RightFoot,
        };

        private static List<Opportunity> Schedule() => new()
        {
            new Opportunity("E1", onsetTime: 0.0, windowSeconds: 10.0, targetLimb: Limb, targetObstacleId: "O1"),
        };

        private static BlockContext Ctx(Condition c = Condition.None) => new()
        {
            ParticipantId = 1, BlockIndex = 0, Condition = c, LayoutId = "L1",
        };

        // Minimum-jerk displacement fraction — the standard model of a human reach: slow, fast, slow. Velocity
        // starting at zero is what makes the inversion possible, so a constant-velocity stimulus would not
        // exercise the behaviour these tests exist to pin.
        private static float MinJerk(float u) => 10f * u * u * u - 15f * u * u * u * u + 6f * u * u * u * u * u;

        private static PoseFrame Frame(double t, Vector3 where)
        {
            var joints = new Vector3[JointInfo.Count];
            for (int j = 0; j < joints.Length; j++) joints[j] = Far;
            joints[(int)Limb] = where;
            return new PoseFrame { Timestamp = t, Joints = joints };
        }

        /// <summary>Drives a whole block with a min-jerk reach and returns the single opportunity's row.</summary>
        private static OpportunityOutcome RunReach(float approach, float movementTime,
                                                   Condition condition = Condition.None)
        {
            var block = new BlockRunner(Ctx(condition), Obstacles(), Limbs(), Schedule(),
                                        new OracleParams(), new DetectorParams());
            block.MarkPresented("E1");

            float startZ = SurfaceZ - approach;
            int n = Math.Max(8, (int)(movementTime * Hz));
            for (int i = 0; i <= n; i++)
                block.Tick(Frame(i * Dt, new Vector3(0f, 1f, startZ + approach * MinJerk((float)i / n))));

            block.Finish();
            return block.OpportunityOutcomes[0];
        }

        [Test]
        public void Records_both_policy_triggers_even_in_the_None_condition()
        {
            // The whole point: the counterfactual must exist where no cue is delivered, or None cannot be
            // compared against the others on timing validity.
            OpportunityOutcome o = RunReach(approach: 1.2f, movementTime: 0.9f, condition: Condition.None);

            Assert.That(o.Timing.TriggerTimeProximity, Is.Not.NaN, "proximity counterfactual missing in None");
            Assert.That(o.Timing.TriggerTimePredictive, Is.Not.NaN, "predictive counterfactual missing in None");
            Assert.That(o.Timing.PolicyLeadTime, Is.Not.NaN);
        }

        [Test]
        public void Predictive_leads_proximity_when_the_approach_distance_is_adequate()
        {
            OpportunityOutcome o = RunReach(approach: 1.2f, movementTime: 0.9f);

            Assert.That(o.Timing.TriggerTimePredictive, Is.LessThan(o.Timing.TriggerTimeProximity));
            Assert.That(o.Timing.PolicyLeadTime, Is.GreaterThan(0.05));
            Assert.That(o.Timing.TimingInverted, Is.False);
        }

        [Test]
        public void Flags_the_inversion_when_the_limb_starts_inside_the_proximity_boundary()
        {
            // Starting inside D, the proximity rule fires as soon as the limb is closing at all, while the
            // predictive rule has to wait for speed to build until TTC drops below T. The factor runs backwards.
            OpportunityOutcome o = RunReach(approach: 0.18f, movementTime: 0.9f);

            Assert.That(o.Timing.PolicyLeadTime, Is.LessThan(0d),
                "expected the predictive cue to arrive LATER than the proximity cue from inside D");
            Assert.That(o.Timing.TimingInverted, Is.True);
        }

        [Test]
        public void Approach_distance_at_onset_measures_the_geometry_the_opportunity_offered()
        {
            OpportunityOutcome o = RunReach(approach: 0.75f, movementTime: 0.9f);

            Assert.That(o.Timing.ApproachDistanceAtOnset, Is.EqualTo(0.75f).Within(0.01f));
            Assert.That(o.Timing.ApproachDistanceAtOnset,
                Is.GreaterThan(OracleParams.MinApproachDistanceForValidTiming));
        }

        [Test]
        public void Closing_speed_is_recorded_at_each_trigger()
        {
            OpportunityOutcome o = RunReach(approach: 1.2f, movementTime: 0.9f);

            Assert.That(o.Timing.ClosingSpeedAtPredictive, Is.GreaterThan(0f));
            Assert.That(o.Timing.ClosingSpeedAtProximity, Is.GreaterThan(0f));

            // No ordering is asserted between the two. On a min-jerk reach the predictive cue fires early,
            // while the limb is still accelerating, and the proximity cue fires nearer the hazard at or past
            // peak speed — so the proximity trigger is typically the FASTER of the two. That is the opposite
            // of the intuition that the later cue catches a slowing limb, and it is why the closing speed at
            // each trigger is logged rather than assumed.
            Assert.That(o.Timing.ClosingSpeedAtProximity, Is.GreaterThan(o.Timing.ClosingSpeedAtPredictive),
                "on a min-jerk reach the proximity trigger lands nearer peak speed than the predictive one");
        }

        [Test]
        public void A_limb_that_never_approaches_records_no_triggers()
        {
            var block = new BlockRunner(Ctx(), Obstacles(), Limbs(), Schedule(),
                                        new OracleParams(), new DetectorParams());
            block.MarkPresented("E1");
            for (int i = 0; i <= 90; i++) block.Tick(Frame(i * Dt, new Vector3(0f, 1f, 0f)));  // stationary, far
            block.Finish();

            PolicyTriggerRecord t = block.OpportunityOutcomes[0].Timing;
            Assert.That(t.TriggerTimeProximity, Is.NaN);
            Assert.That(t.TriggerTimePredictive, Is.NaN);
            Assert.That(t.PolicyLeadTime, Is.NaN);
            Assert.That(t.TimingInverted, Is.False, "an untriggered opportunity is not an inverted one");
        }

        // ── Block-level roll-up ────────────────────────────────────────────────────────────────

        private static OpportunityOutcome Row(bool valid, double prox, double pred, float approach) =>
            new OpportunityOutcome
            {
                OpportunityId = "X", Valid = valid, InvalidReason = valid ? "" : "not_presented",
                Timing = new PolicyTriggerRecord
                {
                    TriggerTimeProximity = prox, TriggerTimePredictive = pred,
                    ApproachDistanceAtOnset = approach,
                    ClosingSpeedAtProximity = float.NaN, ClosingSpeedAtPredictive = float.NaN,
                },
            };

        [Test]
        public void Summary_counts_inversions_and_averages_the_lead()
        {
            var rows = new List<OpportunityOutcome>
            {
                Row(true, prox: 2.0, pred: 1.0, approach: 0.80f),   // lead +1.0
                Row(true, prox: 2.0, pred: 1.5, approach: 0.80f),   // lead +0.5
                Row(true, prox: 1.0, pred: 1.6, approach: 0.80f),   // lead -0.6  INVERTED
            };

            PolicyTriggerSummary s = PolicyTriggerSummary.Summarize(rows);

            Assert.That(s.Measurable, Is.EqualTo(3));
            Assert.That(s.Inverted, Is.EqualTo(1));
            Assert.That(s.MeanLeadSeconds, Is.EqualTo((1.0 + 0.5 - 0.6) / 3.0).Within(1e-9));
            Assert.That(s.Clean, Is.False);
        }

        [Test]
        public void Summary_ignores_invalid_opportunities()
        {
            // Invalid rows are out of the primary denominator, so they must be out of the timing check too.
            var rows = new List<OpportunityOutcome>
            {
                Row(true,  prox: 2.0, pred: 1.0, approach: 0.80f),
                Row(false, prox: 1.0, pred: 9.0, approach: 0.05f),   // inverted AND below floor, but invalid
            };

            PolicyTriggerSummary s = PolicyTriggerSummary.Summarize(rows);

            Assert.That(s.Measurable, Is.EqualTo(1));
            Assert.That(s.Inverted, Is.EqualTo(0));
            Assert.That(s.BelowApproachFloor, Is.EqualTo(0));
            Assert.That(s.Clean, Is.True);
        }

        [Test]
        public void Summary_counts_approach_distance_below_the_floor_even_when_timing_looks_fine()
        {
            // A short approach can still produce a positive lead on a fast movement. It is flagged anyway:
            // the geometry is out of spec regardless of what this particular trial happened to do.
            var rows = new List<OpportunityOutcome>
            {
                Row(true, prox: 2.0, pred: 1.0, approach: 0.10f),
            };

            PolicyTriggerSummary s = PolicyTriggerSummary.Summarize(rows);

            Assert.That(s.Inverted, Is.EqualTo(0));
            Assert.That(s.BelowApproachFloor, Is.EqualTo(1));
            Assert.That(s.Clean, Is.False, "out-of-spec geometry must not be reported as clean");
        }

        [Test]
        public void Summary_reports_unmeasurable_rather_than_pretending_it_is_clean()
        {
            var rows = new List<OpportunityOutcome>
            {
                Row(true, prox: double.NaN, pred: double.NaN, approach: 0.80f),
            };

            PolicyTriggerSummary s = PolicyTriggerSummary.Summarize(rows);

            Assert.That(s.Measurable, Is.EqualTo(0));
            Assert.That(s.Clean, Is.False, "nothing measured is not the same as nothing wrong");
            Assert.That(s.Describe(), Does.Contain("unmeasurable"));
        }

        [Test]
        public void Summary_describe_names_the_numbers_an_operator_needs()
        {
            var rows = new List<OpportunityOutcome> { Row(true, 2.0, 1.0, 0.80f) };
            string text = PolicyTriggerSummary.Summarize(rows).Describe();

            Assert.That(text, Does.Contain("TIMING"));
            Assert.That(text, Does.Contain("inverted=0"));
            Assert.That(text, Does.Not.Contain("Inspect"), "a clean block should not warn");
        }

        [Test]
        public void Summary_over_a_real_block_matches_the_probe()
        {
            // The summary counts VALID opportunities only, so the block has to run past the window close —
            // otherwise the row is excluded as an early stop and nothing is measurable. This is the one test
            // that exercises probe → outcome → summary end to end on a genuinely valid opportunity.
            var schedule = new List<Opportunity>
            {
                new Opportunity("E1", onsetTime: 0.0, windowSeconds: 1.5, targetLimb: Limb,
                                targetObstacleId: "O1"),
            };
            var block = new BlockRunner(Ctx(), Obstacles(), Limbs(), schedule,
                                        new OracleParams(), new DetectorParams());
            block.MarkPresented("E1");

            const float approach = 1.2f;
            float startZ = SurfaceZ - approach;
            int n = (int)(0.9f * Hz);
            int i = 0;
            for (; i <= n; i++)
                block.Tick(Frame(i * Dt, new Vector3(0f, 1f, startZ + approach * MinJerk((float)i / n))));
            // Hold still past the window close so the opportunity finishes cleanly.
            for (; i * Dt <= 2.0; i++) block.Tick(Frame(i * Dt, new Vector3(0f, 1f, SurfaceZ)));
            block.Finish();

            Assert.That(block.OpportunityOutcomes[0].Valid, Is.True, "precondition: the window must have closed");

            PolicyTriggerSummary s = PolicyTriggerSummary.Summarize(block.OpportunityOutcomes);

            Assert.That(s.Measurable, Is.EqualTo(1));
            Assert.That(s.Inverted, Is.EqualTo(0));
            Assert.That(s.BelowApproachFloor, Is.EqualTo(0));
            Assert.That(s.Clean, Is.True);
        }

        [Test]
        public void Delivered_trigger_follows_the_assigned_condition()
        {
            var t = new PolicyTriggerRecord { TriggerTimeProximity = 2.0, TriggerTimePredictive = 1.0 };

            Assert.That(OpportunityOutcomeFormatter.DeliveredTrigger(Condition.RB, t), Is.EqualTo(2.0));
            Assert.That(OpportunityOutcomeFormatter.DeliveredTrigger(Condition.RG, t), Is.EqualTo(2.0));
            Assert.That(OpportunityOutcomeFormatter.DeliveredTrigger(Condition.PB, t), Is.EqualTo(1.0));
            Assert.That(OpportunityOutcomeFormatter.DeliveredTrigger(Condition.PG, t), Is.EqualTo(1.0));
            // Visual shares PB's predictive trigger so H4 isolates modality [§2].
            Assert.That(OpportunityOutcomeFormatter.DeliveredTrigger(Condition.Visual, t), Is.EqualTo(1.0));
            Assert.That(OpportunityOutcomeFormatter.DeliveredTrigger(Condition.None, t), Is.NaN);
        }

        [Test]
        public void Csv_row_matches_the_header_and_writes_NA_not_NaN()
        {
            OpportunityOutcome o = RunReach(approach: 1.2f, movementTime: 0.9f, condition: Condition.PB);
            string row = OpportunityOutcomeFormatter.Row(Ctx(Condition.PB), o);

            int headerCols = OpportunityOutcomeFormatter.HeaderLine.Split(',').Length;
            Assert.That(row.Split(',').Length, Is.EqualTo(headerCols),
                "row and header must stay in step — a mismatch silently shifts every downstream column");

            Assert.That(row, Does.Not.Contain("NaN"));
            Assert.That(row, Does.Not.Contain("Infinity"));
            Assert.That(OpportunityOutcomeFormatter.HeaderLine, Does.Contain("policy_lead_s"));
            Assert.That(OpportunityOutcomeFormatter.HeaderLine, Does.Contain("timing_inverted"));
        }

        [Test]
        public void Untriggered_opportunity_writes_NA_in_every_timing_column()
        {
            var block = new BlockRunner(Ctx(), Obstacles(), Limbs(), Schedule(),
                                        new OracleParams(), new DetectorParams());
            block.MarkPresented("E1");
            for (int i = 0; i <= 90; i++) block.Tick(Frame(i * Dt, new Vector3(0f, 1f, 0f)));
            block.Finish();

            string row = OpportunityOutcomeFormatter.Row(Ctx(), block.OpportunityOutcomes[0]);
            Assert.That(row, Does.Not.Contain("NaN"));
            Assert.That(row.Split(',').Length,
                Is.EqualTo(OpportunityOutcomeFormatter.HeaderLine.Split(',').Length));
        }

        [Test]
        public void Timing_is_recorded_alongside_the_primary_outcome_without_disturbing_it()
        {
            // The probe is a pure observer. That it does not perturb the primary outcome is pinned by the
            // pre-existing suite (OpportunityOutcomeTests and BlockRunnerTests still pass unchanged); this
            // test pins the other half — that both records are produced for the same opportunity.
            OpportunityOutcome hit = RunReach(approach: 1.2f, movementTime: 0.9f);
            Assert.That(hit.Violation, Is.EqualTo(1), "this reach ends at the hazard surface");
            Assert.That(hit.Timing.TriggerTimePredictive, Is.Not.NaN);
            Assert.That(hit.Timing.TriggerTimePredictive, Is.LessThan(hit.FirstEntryTime),
                "a warning that fires after contact would not be a warning");
        }
    }
}
