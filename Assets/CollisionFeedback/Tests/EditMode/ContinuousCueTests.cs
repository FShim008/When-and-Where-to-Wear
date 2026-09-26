using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CollisionFeedback.Core;
using Joint = CollisionFeedback.Core.Joint;   // disambiguate from UnityEngine.Joint (physics component)

namespace CollisionFeedback.Tests
{
    /// <summary>
    /// Guards for H4' — the PB vs PBC cue-form manipulation
    /// [PAPER1_STUDY_DESIGN §3; docs/PAPER1_PARAMETER_JUSTIFICATION.md §5B].
    ///
    /// Valkov and Linsen (IEEE VR 2019) found forecast-triggered warning produced MORE collisions than
    /// distance-triggered warning, and attributed it to their continuous intensity mapping rather than to
    /// the trigger: slowing down lowered the vibration, so participants crept forward modulating it. Their
    /// design varied trigger and cue form together, so it cannot separate the two.
    ///
    /// PB vs PBC separates them, but ONLY while three things hold, and each is silent if it breaks:
    ///   1. PBC's onset is identical to PB's         -> otherwise cue form is confounded with timing
    ///   2. PBC's site and information match PB's    -> otherwise it is confounded with localization
    ///   3. PBC's intensity really does track TTC    -> otherwise it is not a continuous cue at all
    /// A silent failure of any of these makes H4' a comparison of PB against itself.
    /// </summary>
    public class ContinuousCueTests
    {
        private const float Hz = 90f;
        private const float Dt = 1f / Hz;
        private static readonly Joint Limb = Joint.RightHand;
        private static readonly List<Joint> Limbs = new() { Limb };

        private static List<Obstacle> Hazard() => new()
        { new Obstacle("O1", new Vector3(0f, 1.0f, 3.0f), new Vector3(0.2f, 0.2f, 0.2f)) };

        private static OracleParams Params() => new();

        // Minimum-jerk displacement fraction — the standard model of a human reach.
        private static float MinJerk(float u) => 10f * u * u * u - 15f * u * u * u * u + 6f * u * u * u * u * u;

        /// <summary>Runs one minimum-jerk reach and returns every command the condition emitted.</summary>
        private static List<FeedbackCommand> Run(Condition condition, float approach = 0.6f,
                                                 float movementTime = 0.9f)
        {
            var sink = new RecordingSink();
            var cm = new ConditionManager(condition, Params(), Hazard(), Limbs, sink);
            Obstacle obs = Hazard()[0];
            float startZ = obs.Center.z - obs.HalfExtents.z - approach;

            PoseFrame frame = PoseFrame.Create(0);
            int n = Math.Max(8, (int)(movementTime * Hz));
            for (int i = 0; i <= n; i++)
            {
                frame.Timestamp = i * Dt;
                frame.Joints[(int)Limb] = new Vector3(0f, 1.0f, startZ + approach * MinJerk((float)i / n));
                cm.Tick(frame);
            }
            return sink.Fired;
        }

        // ── The transfer function itself ─────────────────────────────────────────────────────────────

        [Test]
        public void Intensity_is_silent_at_and_above_the_threshold()
        {
            var p = ContinuousCueParams.ForThreshold(1.0f);

            Assert.That(ContinuousCueMapping.Intensity(1.0f, p), Is.EqualTo(0f),
                "At the predictive threshold the cue must be silent — that is the instant PB fires, so " +
                "anything above zero here means PBC starts EARLIER than PB and H4' is confounded with timing.");
            Assert.That(ContinuousCueMapping.Intensity(2.0f, p), Is.EqualTo(0f));
            Assert.That(ContinuousCueMapping.Intensity(float.PositiveInfinity, p), Is.EqualTo(0f));
        }

        [Test]
        public void A_degraded_ttc_estimate_is_silent_not_maximal()
        {
            var p = ContinuousCueParams.ForThreshold(1.0f);

            Assert.That(ContinuousCueMapping.Intensity(float.NaN, p), Is.EqualTo(0f),
                "A NaN forecast must produce silence. Falling through to full intensity would turn a " +
                "tracking dropout into a maximum-amplitude cue on the participant's limb.");
        }

        [Test]
        public void Intensity_rises_monotonically_as_contact_approaches()
        {
            var p = ContinuousCueParams.ForThreshold(1.0f);
            float previous = -1f;

            for (float ttc = 0.95f; ttc >= 0f; ttc -= 0.05f)
            {
                float level = ContinuousCueMapping.Intensity(ttc, p);
                Assert.That(level, Is.GreaterThanOrEqualTo(previous),
                    $"Intensity must never fall as TTC shrinks (failed at ttc={ttc:F2}).");
                Assert.That(level, Is.InRange(0f, 1f));
                previous = level;
            }
            Assert.That(ContinuousCueMapping.Intensity(0f, p), Is.EqualTo(1f));
        }

        [Test]
        public void An_engaged_cue_is_never_below_the_perceptible_floor()
        {
            var p = ContinuousCueParams.ForThreshold(1.0f);
            float justInside = ContinuousCueMapping.Intensity(0.999f, p);

            // NOTE: no .Within() here — Unity's bundled NUnit does not support a tolerance on
            // GreaterThanOrEqualTo (the NuGet NUnit used by the C:\cc harness does, which is exactly
            // how this compiled there and failed here). Subtract the tolerance from the bound instead.
            Assert.That(justInside, Is.GreaterThanOrEqualTo(0.15f - 1e-4f),
                "Just inside the band the gamma curve is near zero. Without the floor the cue would be " +
                "imperceptible for the first part of every approach, making PBC's EFFECTIVE onset later " +
                "than PB's — the same confound, arriving by the back door.");
        }

        // ── THE MECHANISM UNDER TEST: slowing down must weaken the cue ───────────────────────────────

        [Test]
        public void Slowing_down_lowers_the_continuous_cue_but_not_the_discrete_one()
        {
            // TTC = distance / closing speed. Same distance, half the speed, so TTC doubles.
            var p = ContinuousCueParams.ForThreshold(1.0f);

            float fast = ContinuousCueMapping.Intensity(0.35f, p);   // closing quickly
            float slow = ContinuousCueMapping.Intensity(0.70f, p);   // same distance, half the speed

            Assert.That(slow, Is.LessThan(fast),
                "This IS the Valkov and Linsen feedback loop and it must be present, not fixed. Slowing " +
                "down raises TTC and so lowers intensity, which is exactly the behaviour they observed " +
                "('continued walking slowly forward while constantly decreasing the speed to adjust the " +
                "vibration level'). If this assertion ever fails, PBC no longer reproduces the mechanism " +
                "H4' exists to test.");

            // The discrete cue has already fired and carries no intensity information at all, so there is
            // nothing for the participant to modulate. That asymmetry is the whole manipulation.
            List<FeedbackCommand> discrete = Run(Condition.PB);
            Assert.That(discrete, Is.Not.Empty);
            foreach (FeedbackCommand c in discrete)
                Assert.That(c.Intensity, Is.EqualTo(1f),
                    "A discrete cue's amplitude is fixed by design. If it varied, H1 would be confounded " +
                    "with cue form and PB would stop being a clean control for PBC.");
        }

        // ── PB and PBC must differ in EXACTLY one respect ────────────────────────────────────────────

        [Test]
        public void Pbc_shares_pbs_trigger_site_and_information()
        {
            Assert.That(ConditionManager.TriggerFor(Condition.PBC),
                        Is.EqualTo(ConditionManager.TriggerFor(Condition.PB)),
                "PBC must use PB's predictive trigger. A different trigger makes H4' a timing test.");

            Assert.That(ConditionManager.IsLocalized(Condition.PBC),
                        Is.EqualTo(ConditionManager.IsLocalized(Condition.PB)),
                "PBC must be body-localized like PB, or H4' is confounded with H2's mapping factor.");

            List<FeedbackCommand> pb = Run(Condition.PB);
            List<FeedbackCommand> pbc = Run(Condition.PBC);
            Assert.That(pb, Is.Not.Empty, "PB produced no cue — the fixture geometry is wrong.");
            Assert.That(pbc, Is.Not.Empty, "PBC produced no cue — the continuous path never engaged.");

            Assert.That(pbc[0].Site, Is.EqualTo(pb[0].Site), "Same cue site.");
            Assert.That(pbc[0].Limb, Is.EqualTo(pb[0].Limb), "Same at-risk limb.");
            Assert.That(pbc[0].Trigger, Is.EqualTo(pb[0].Trigger), "Same trigger kind.");
            Assert.That(pbc[0].Modality, Is.EqualTo(Modality.Haptic), "PBC is haptic, not visual.");
        }

        [Test]
        public void Pbc_onset_matches_pb_onset_to_the_frame()
        {
            List<FeedbackCommand> pb = Run(Condition.PB);
            List<FeedbackCommand> pbc = Run(Condition.PBC);

            Assert.That(pbc[0].DataTime, Is.EqualTo(pb[0].DataTime).Within(Dt + 1e-6),
                "PB and PBC must become perceptible at the same instant. Any onset difference means H4' " +
                "compares cue form AND timing, which is precisely the confound this condition exists to " +
                "remove from the literature.");
        }

        [Test]
        public void Pbc_is_continuous_and_pb_is_not()
        {
            List<FeedbackCommand> pb = Run(Condition.PB);
            List<FeedbackCommand> pbc = Run(Condition.PBC);

            Assert.That(pb.Count, Is.EqualTo(1),
                "Edge-triggered: exactly one alert per approach. More would corrupt alert counts.");
            Assert.That(pbc.Count, Is.GreaterThan(5),
                "PBC must emit repeatedly while engaged. One command means the edge trigger is still " +
                "gating it and PBC has silently degenerated into PB.");

            foreach (FeedbackCommand c in pb)
                Assert.That(c.Form, Is.EqualTo(CueForm.Discrete));
            foreach (FeedbackCommand c in pbc)
                Assert.That(c.Form, Is.EqualTo(CueForm.Continuous));
        }

        [Test]
        public void Pbc_intensity_actually_varies_across_an_approach()
        {
            List<FeedbackCommand> pbc = Run(Condition.PBC);

            float min = float.MaxValue, max = float.MinValue;
            foreach (FeedbackCommand c in pbc)
            {
                if (c.Intensity < min) min = c.Intensity;
                if (c.Intensity > max) max = c.Intensity;
            }

            Assert.That(max - min, Is.GreaterThan(0.1f),
                "A 'continuous' cue whose intensity never moves is a discrete cue emitted repeatedly. " +
                "H4' would then compare PB against PB.");
        }

        // ── Regression: the discrete conditions must be untouched ────────────────────────────────────

        [Test]
        public void Every_scheduled_discrete_condition_still_emits_one_full_intensity_discrete_cue()
        {
            foreach (Condition c in new[] { Condition.RG, Condition.RB, Condition.PG, Condition.PB })
            {
                List<FeedbackCommand> fired = Run(c);
                Assert.That(fired.Count, Is.EqualTo(1), $"{c} must fire exactly once per approach.");
                Assert.That(fired[0].Form, Is.EqualTo(CueForm.Discrete), $"{c} must stay discrete.");
                Assert.That(fired[0].Intensity, Is.EqualTo(1f), $"{c} must stay at full intensity.");
            }
        }

        [Test]
        public void None_still_never_fires()
        {
            Assert.That(Run(Condition.None), Is.Empty);
        }

        // ── The protocol actually schedules it ───────────────────────────────────────────────────────

        [Test]
        public void The_session_schedules_pbc_and_not_visual()
        {
            Assert.That(SessionPlan.Conditions, Has.Length.EqualTo(6),
                "The Williams square is 6x6; changing the count breaks counterbalancing.");
            Assert.That(SessionPlan.Conditions, Contains.Item(Condition.PBC),
                "PBC carries H4'. If it is not scheduled, the paper cannot address Valkov and Linsen.");
            Assert.That(SessionPlan.Conditions, Has.No.Member(Condition.Visual),
                "Visual was swapped out for PBC on 2026-09-23. It remains implemented, but scheduling " +
                "both would make a seventh block and blow the session length.");
        }

        // ── Alert burden must stay comparable across cue forms ───────────────────────────────────────

        private static BlockRunner RunBlockWith(Condition condition)
        {
            var obstacles = new List<Obstacle> { new("O", new Vector3(0f, 1f, 1.0f), new Vector3(0.30f, 1f, 0.30f)) };
            var limbs = new List<Joint> { Joint.RightHand };
            var schedule = new List<Opportunity> { new("OP01", 0.0, 2.0, Joint.RightHand, "O") };
            var ctx = new BlockContext { ParticipantId = 1, BlockIndex = 0, Condition = condition, LayoutId = "L1" };

            var runner = new BlockRunner(ctx, obstacles, limbs, schedule, new OracleParams(), new DetectorParams());
            foreach (PoseFrame f in SyntheticTrajectory.LinearApproach(
                         Joint.RightHand, new Vector3(0f, 1f, 0f), new Vector3(0f, 0f, 2f), 1.0f, 1f / 90f))
                runner.Tick(f);
            runner.Finish();
            return runner;
        }

        [Test]
        public void Continuous_burden_is_counted_as_episodes_not_frames()
        {
            BlockRunner pb = RunBlockWith(Condition.PB);
            BlockRunner pbc = RunBlockWith(Condition.PBC);

            Assert.That(pbc.Alerts.Count, Is.GreaterThan(10),
                "Sanity: the continuous cue should emit many raw commands over one approach.");

            Assert.That(pbc.AlertEpisodes, Is.EqualTo(pb.AlertEpisodes),
                "One approach is ONE engagement episode under either cue form. If PBC reported its raw " +
                "frame count instead, alert burden would show ~1000 alerts for PBC against 1 for PB and " +
                "the §15 tie-break would be decided by a units mistake rather than by the data.");

            Assert.That(pbc.CueDoseSeconds, Is.GreaterThan(0.0),
                "Intensity-seconds is the only burden measure on which PB and PBC compare honestly; " +
                "it must actually accumulate.");
            Assert.That(pb.CueDoseSeconds, Is.EqualTo(0.0),
                "A discrete cue carries no dose — its burden is the episode count.");
        }

        [Test]
        public void Continuous_cue_does_not_collapse_avoidance_latency()
        {
            BlockRunner pbc = RunBlockWith(Condition.PBC);

            // Re-registering an alert every frame would reset the latency clock continuously and drive the
            // measured latency to ~0 by construction. Exactly one registration per engagement prevents it.
            Assert.That(pbc.AlertEpisodes, Is.EqualTo(1),
                "Exactly one alert registration per engagement. More means the latency detector is being " +
                "re-armed mid-approach and PBC's avoidance latency is an artefact, not a measurement.");
        }

        // ── The preregistered opportunity count vs what the schedules actually supply ────────────────

        [Test]
        public void Layout1_does_not_yet_meet_the_preregistered_opportunity_target()
        {
            // THIS TEST IS EXPECTED TO PASS TODAY AND TO BE DELETED LATER.
            // It pins a KNOWN GAP: the design requires 24 opportunities per block (N = 36 x 24, decided
            // 2026-09-25) and Layout1 authors 12. Authoring the missing twelve is real work - the block grows
            // from 180 s to about 320 s, obstacle balance must hold, and the geometry audit must re-run
            // over all 24 (the current 12 pass 12/12 after O2 was re-sited on 2026-09-14).
            //
            // When that work lands, this test FAILS. That is the point: delete it and replace it with
            // Layout1_meets_the_preregistered_opportunity_target below. A gap recorded only in prose gets
            // forgotten; a gap recorded as a test cannot be.
            var schedule = OpportunitySchedules.Layout1();

            Assert.That(OpportunitySchedules.TargetOpportunitiesPerBlock, Is.EqualTo(24),
                "The preregistered target is 24, decided 2026-09-25 [PAPER1_STUDY_DESIGN §4].");
            Assert.That(schedule.Count, Is.EqualTo(12),
                "Layout1 still authors 12 events. If this now fails because the count CHANGED, the six new " +
                "events were authored - delete this test and enable the one below. If it fails because the " +
                "count is something else entirely, the schedule was edited without re-running the geometry " +
                "audit, which is worse.");

            Assert.Throws<System.InvalidOperationException>(
                () => OpportunitySchedules.AssertMeetsTarget(schedule, "L1"),
                "The mismatch must be LOUD. A short schedule looks like clean data downstream, because the " +
                "analysis counts the rows it is given rather than assuming 12 (CODE_GAP #1).");
        }

        [Test]
        [Ignore("Enable when the six additional events per layout have been authored and the geometry " +
                "audit re-run over all 18. Then delete Layout1_does_not_yet_meet_the_preregistered_target.")]
        public void Layout1_meets_the_preregistered_opportunity_target()
        {
            var schedule = OpportunitySchedules.Layout1();
            Assert.DoesNotThrow(() => OpportunitySchedules.AssertMeetsTarget(schedule, "L1"));

            // Obstacle balance for 24: O2x8, O3x8, O1x4, O4x4.
            var byObstacle = new Dictionary<string, int>();
            foreach (Opportunity o in schedule)
            {
                string key = o.TargetObstacleId.StartsWith("O4") ? "O4" : o.TargetObstacleId;
                byObstacle[key] = byObstacle.TryGetValue(key, out int c) ? c + 1 : 1;
            }
            Assert.That(byObstacle["O2"], Is.EqualTo(8), "Obstacle balance must survive the extension.");
            Assert.That(byObstacle["O3"], Is.EqualTo(8));
            Assert.That(byObstacle["O1"], Is.EqualTo(4));
            Assert.That(byObstacle["O4"], Is.EqualTo(4));
        }

        [Test]
        public void The_two_by_two_factorial_is_still_intact()
        {
            foreach (Condition c in new[] { Condition.RG, Condition.RB, Condition.PG, Condition.PB })
                Assert.That(SessionPlan.Conditions, Contains.Item(c),
                    $"{c} is a cell of the 2x2. H1 and H2 both need all four.");
            Assert.That(SessionPlan.Conditions, Contains.Item(Condition.None),
                "The no-feedback floor is the validity gate (H5).");
        }
    }
}
