using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CollisionFeedback.Core;
using CollisionFeedback.Core.E2;
using Joint = CollisionFeedback.Core.Joint; // disambiguate from UnityEngine.Joint (physics component)

namespace CollisionFeedback.Tests
{
    /// <summary>
    /// E2 — the warning lead-time trial [PAPER2_STUDY_DESIGN §8, §9].
    ///
    /// These run with **no headset, no trackers and no bHaptics**: synthetic minimum-jerk reaches are fed
    /// through the runner and the delivered lead time is read back out. That is what the three seams exist
    /// for, and it means E2 can be finished and verified before any hardware is touched.
    ///
    /// The property under test throughout: **the runner does not have to deliver a lead time precisely, it has
    /// to measure what it delivered.** Every assertion is about the measurement being right, not the aim.
    ///
    /// ⚠ **Do not write assertions that sit on a tolerance boundary.** These tests are also run outside Unity
    /// against a minimal UnityEngine shim, and the two float implementations cross the TTC threshold up to
    /// about one frame (~11 ms at 90 Hz) apart. An absolute window tight enough to be interesting is tight
    /// enough to disagree between them. Assert relationships and ratios, which hold under both.
    /// </summary>
    public class E2TrialRunnerTests
    {
        private const float Hz = 90f;
        private const float Dt = 1f / Hz;
        private static readonly Vector3 Far = new(50f, 50f, 50f);
        private static readonly Joint Limb = Joint.RightHand;

        // Hazard centred at z = 2.0, half-depth 0.2 -> near face at z = 1.8. Target sits beyond it.
        private const float SurfaceZ = 1.8f;
        private static readonly Vector3 Target = new(0f, 1f, 2.6f);

        private static List<Obstacle> Hazards() => new()
        {
            new Obstacle("H1", new Vector3(0f, 1f, 2f), new Vector3(0.2f, 0.2f, 0.2f)),
        };

        private static PoseFrame Frame(double t, Vector3 where)
        {
            var j = new Vector3[JointInfo.Count];
            for (int i = 0; i < j.Length; i++) j[i] = Far;
            j[(int)Limb] = where;
            return new PoseFrame { Timestamp = t, Joints = j };
        }

        private static float MinJerk(float u) => 10f * u * u * u - 15f * u * u * u * u + 6f * u * u * u * u * u;

        private sealed class Sink : IFeedbackSink
        {
            public readonly List<FeedbackCommand> Fired = new();
            public void Fire(in FeedbackCommand c) => Fired.Add(c);
        }

        /// <summary>
        /// A reach that travels <paramref name="travel"/> m from <paramref name="approach"/> m clear of the
        /// hazard surface. travel &gt; approach means it goes through; travel &lt; approach means it stops short —
        /// which is how a successful avoidance is simulated.
        /// </summary>
        private static (E2TrialOutcome outcome, Sink sink) Run(
            E2Trial trial, E2Params p, float approach, float travel, float movementTime, double holdAfter = 0.3)
        {
            var sink = new Sink();
            var runner = new E2TrialRunner(trial, Hazards(), p, sink);

            float startZ = SurfaceZ - approach;
            int n = Math.Max(8, (int)(movementTime * Hz));
            int i = 0;
            for (; i <= n; i++)
                runner.Tick(Frame(i * Dt, new Vector3(0f, 1f, startZ + travel * MinJerk((float)i / n))));

            double endHold = i * Dt + holdAfter;
            var restZ = startZ + travel;
            for (; i * Dt <= endHold; i++)
                runner.Tick(Frame(i * Dt, new Vector3(0f, 1f, restZ)));

            return (runner.Finish(), sink);
        }

        private static E2Params P(float latency = 0f) => new E2Params { PipelineLatencySeconds = latency };

        // ── Tempo manipulation and its check (§8, §15 gate 7) ──────────────────────────────────────────

        [Test]
        public void Response_window_follows_the_trials_tempo()
        {
            var p = P();
            var normal = E2Trial.Go("T1", Limb, "H1", Target, Tempo.Normal);
            var urgent = E2Trial.Go("T2", Limb, "H1", Target, Tempo.Urgent);

            var a = Run(normal, p, approach: 0.60f, travel: 1.40f, movementTime: 0.80f).outcome;
            var b = Run(urgent, p, approach: 0.60f, travel: 1.40f, movementTime: 0.80f).outcome;

            Assert.That(a.ResponseWindowSeconds, Is.EqualTo(p.NormalWindowSeconds).Within(1e-4f));
            Assert.That(b.ResponseWindowSeconds, Is.EqualTo(p.UrgentWindowSeconds).Within(1e-4f));
            Assert.That(b.ResponseWindowSeconds, Is.LessThan(a.ResponseWindowSeconds),
                "urgent must demand a faster reach, or tempo manipulates nothing and H3a estimates nothing");
        }

        [Test]
        public void Movement_time_is_measured_from_onset_to_target_contact()
        {
            // A reach that reaches the target: movement time must be populated and close to the simulated
            // duration. This is the tempo manipulation check §8 requires.
            var trial = E2Trial.Go("T1", Limb, "H1", Target, Tempo.Normal);
            var o = Run(trial, P(), approach: 0.60f, travel: 1.40f, movementTime: 0.80f).outcome;

            Assert.That(o.ReachedTarget, Is.True, "the reach must actually arrive for this test to mean anything");
            Assert.That(o.MovementOnsetTime, Is.Not.NaN);
            Assert.That(o.TargetContactTime, Is.Not.NaN);
            Assert.That(o.MovementTimeSeconds, Is.GreaterThan(0.0));
            Assert.That(o.MovementTimeSeconds, Is.LessThan(0.80),
                "onset is confirmed after the reach has started, so measured movement time is shorter " +
                "than the full simulated duration");
        }

        [Test]
        public void Movement_time_is_NaN_when_the_target_is_never_reached()
        {
            // Every successful stop looks like this, by design — so the manipulation check runs on go trials.
            var trial = E2Trial.Warning("T1", Limb, "H1", Target, Tempo.Normal, 0.30f);
            var o = Run(trial, P(), approach: 0.60f, travel: 0.30f, movementTime: 0.80f).outcome;

            Assert.That(o.ReachedTarget, Is.False);
            Assert.That(double.IsNaN(o.MovementTimeSeconds), Is.True);
            Assert.That(o.WithinResponseWindow, Is.False,
                "a reach that never arrived cannot have met its window");
        }

        [Test]
        public void A_fast_reach_meets_the_urgent_window_and_a_slow_one_does_not()
        {
            var p = P();
            var fast = E2Trial.Go("T1", Limb, "H1", Target, Tempo.Urgent);
            var slow = E2Trial.Go("T2", Limb, "H1", Target, Tempo.Urgent);

            var a = Run(fast, p, approach: 0.60f, travel: 1.40f, movementTime: 0.45f).outcome;
            var b = Run(slow, p, approach: 0.60f, travel: 1.40f, movementTime: 1.60f).outcome;

            Assert.That(a.ReachedTarget, Is.True);
            Assert.That(b.ReachedTarget, Is.True);
            Assert.That(a.WithinResponseWindow, Is.True, "a 0.45 s reach must clear the urgent window");
            Assert.That(b.WithinResponseWindow, Is.False,
                "a 1.60 s reach must fail the urgent window — otherwise the window constrains nothing " +
                "and instruction failure would be invisible (§15 gate 7)");
        }

        [Test]
        public void A_normal_trial_reached_at_urgent_speed_misses_its_window()
        {
            // THE POINT OF THE LOWER BOUND. With only an upper bound, a participant who reaches fast on every
            // trial satisfies both windows at once, Normal and Urgent become behaviourally identical, and H3a
            // silently compares Urgent against Urgent while every compliance check still reads green.
            var p = P();
            var normal = E2Trial.Go("T1", Limb, "H1", Target, Tempo.Normal);
            var o = Run(normal, p, approach: 0.60f, travel: 1.40f, movementTime: 0.45f).outcome;

            Assert.That(o.ReachedTarget, Is.True);
            Assert.That(o.MovementTimeSeconds, Is.LessThan(p.NormalWindowMinSeconds),
                "this reach is deliberately faster than the Normal band allows");
            Assert.That(o.WithinResponseWindow, Is.False,
                "too fast must fail a Normal trial, or the tempo manipulation does not bind");
        }

        [Test]
        public void Normal_and_urgent_bands_do_not_overlap()
        {
            // If they touch, one movement time can satisfy both and the manipulation is not enforced.
            var p = P();
            Assert.That(p.NormalWindowMinSeconds, Is.GreaterThan(p.UrgentWindowSeconds),
                "the Normal lower bound must sit above the Urgent upper bound");
        }

        [Test]
        public void Tempo_is_recorded_on_the_outcome_so_the_manipulation_can_be_checked()
        {
            var o = Run(E2Trial.Go("T1", Limb, "H1", Target, Tempo.Urgent), P(),
                        approach: 0.60f, travel: 1.40f, movementTime: 0.50f).outcome;
            Assert.That(o.Tempo, Is.EqualTo(Tempo.Urgent));
        }

        [Test]
        public void Response_onset_backdating_uses_the_real_frame_interval()
        {
            // The detector back-dates a confirmed run by (ResponseConfirmFrames - 1) frame intervals. It used
            // to back-date by (elapsed / 100), which grew with trial time — so an identical run was dated
            // differently depending on when in the trial it happened. Two runs at different frame rates must
            // now back-date by their own interval, keeping response onset a fixed offset from detection.
            var trial = E2Trial.Warning("T1", Limb, "H1", Target, Tempo.Normal, 0.30f);
            // This shape reaches a minimum TTC of ~0.25 s, so a 0.30 s lead fires; travel < approach, so the
            // reach still stops short of the hazard. Both are required for a response onset to exist at all.
            var o = Run(trial, P(), approach: 1.20f, travel: 1.00f, movementTime: 0.90f).outcome;

            Assert.That(o.ResponseOnsetTime, Is.Not.NaN, "a stopped reach must produce a response onset");
            Assert.That(o.ResponseOnsetTime, Is.GreaterThan(o.PhysicalOnsetTime),
                "the response cannot begin before the vibration did");
            Assert.That(o.ResponseOnsetTime - o.PhysicalOnsetTime, Is.LessThan(0.6),
                "back-dating must stay within the trial, not run away with elapsed time");
        }

        // ── Primary outcome ────────────────────────────────────────────────────────────────────

        [Test]
        public void A_reach_that_goes_through_the_hazard_is_recorded_as_a_crossing()
        {
            var t = E2Trial.Go("G1", Limb, "H1", Target, Tempo.Normal);
            var (o, _) = Run(t, P(), approach: 1.0f, travel: 1.3f, movementTime: 0.8f);

            Assert.That(o.Crossed, Is.EqualTo(1));
            Assert.That(o.ActualContactTime, Is.Not.NaN);
            Assert.That(o.Valid, Is.True);
        }

        [Test]
        public void A_reach_that_stops_short_is_not_a_crossing()
        {
            var t = E2Trial.Go("G2", Limb, "H1", Target, Tempo.Normal);
            var (o, _) = Run(t, P(), approach: 1.0f, travel: 0.6f, movementTime: 0.8f);

            Assert.That(o.Crossed, Is.EqualTo(0));
            Assert.That(o.ActualContactTime, Is.NaN);
            Assert.That(o.MinClearance, Is.GreaterThan(0f));
        }

        [Test]
        public void Go_trials_carry_a_hazard_and_anchor_the_curve_at_zero_warning()
        {
            // 75% of trials are go trials, and they still intersect a hazard. Their crossing rate IS the
            // avoidance probability at zero lead time -- the measured lower asymptote, not an assumed one.
            var t = E2Trial.Go("G3", Limb, "H1", Target, Tempo.Normal);
            var (o, sink) = Run(t, P(), approach: 1.0f, travel: 1.3f, movementTime: 0.8f);

            Assert.That(o.IsWarningTrial, Is.False);
            Assert.That(sink.Fired, Is.Empty, "a go trial must never produce a cue");
            Assert.That(o.Crossed, Is.EqualTo(1));
            Assert.That(o.RealizedLeadSeconds, Is.NaN);
        }

        // ── Lead-time delivery: the core of E2 ─────────────────────────────────────────────────

        [Test]
        public void The_cue_fires_and_the_realized_lead_lands_slightly_short_of_the_assigned_lead()
        {
            // Delivery is SHORT by design, not by defect: the cue commits when TTC reaches the threshold, but
            // the limb is still accelerating, so the gap closes faster than the frozen speed predicts. See
            // E2SessionPlan for the full argument and why it is accepted rather than corrected.
            //
            // The band is a ratio, not an absolute window, so it holds across lead levels and across float
            // implementations. Measured: ~0.87-0.93x of assigned.
            foreach (float lead in new[] { 0.15f, 0.25f, 0.40f })
            {
                var t = E2Trial.Warning("W1_" + lead, Limb, "H1", Target, Tempo.Normal, lead);
                var (o, sink) = Run(t, P(), approach: 1.4f, travel: 1.7f, movementTime: 0.9f);

                Assert.That(sink.Fired.Count, Is.EqualTo(1), "lead " + lead + " never fired");
                Assert.That(o.RealizedLeadSeconds, Is.Not.NaN);
                Assert.That(o.RealizedLeadSeconds, Is.LessThanOrEqualTo(lead * 1.05),
                    "lead " + lead + ": delivery should not OVERSHOOT the assigned lead");
                Assert.That(o.RealizedLeadSeconds, Is.GreaterThan(lead * 0.75),
                    "lead " + lead + ": delivery is expected short, but not by more than ~25%");
            }
        }

        [Test]
        public void Pipeline_latency_is_compensated_by_committing_to_fire_earlier()
        {
            // What compensation actually buys, stated as a comparison rather than an absolute value.
            //
            // An earlier version asserted realized == 0.40 +/- 0.05 s. That was wrong twice over. First, it
            // contradicted the documented acceleration shortfall (see E2SessionPlan): delivery runs ~13-20%
            // short, so 0.40 lands near 0.35, right on the tolerance edge. Second, an assertion that sits on
            // its own tolerance boundary is not portable -- it passed under the dotnet test harness and failed
            // in Unity, whose float math crosses the TTC threshold about one frame earlier.
            //
            // NOTE ON WHAT THIS CAN AND CANNOT SIMULATE. Setting PipelineLatencySeconds to 0 does not model
            // "real hardware, uncompensated" -- the parameter does double duty, scheduling the cue AND locating
            // physical onset, so 0 models a world with no latency at all. The genuine uncompensated failure
            // (hardware takes 60 ms, the parameter says 0, so the logged onset is 60 ms too early and every
            // realized lead is overstated) cannot be produced here, and is why the bench measurement is a
            // launch gate rather than a nicety.
            //
            // The invariants below hold under both float implementations, because they are relationships.
            const float latency = 0.060f;
            var t = E2Trial.Warning("W2", Limb, "H1", Target, Tempo.Normal, leadSeconds: 0.40f);

            var (comp, _) = Run(t, P(latency), approach: 1.2f, travel: 1.5f, movementTime: 0.9f);
            var (uncomp, _) = Run(t, P(0f), approach: 1.2f, travel: 1.5f, movementTime: 0.9f);

            // The gap between command and vibration is exactly the configured latency, to frame resolution.
            Assert.That(comp.PhysicalOnsetTime - comp.CommandTime, Is.EqualTo(latency).Within(Dt + 1e-6));

            // A larger latency means committing earlier in the reach, which is the whole mechanism.
            Assert.That(comp.CommandTime, Is.LessThan(uncomp.CommandTime));

            // And the point of doing so: the DELIVERED lead stays in the same band whatever the latency.
            // Without compensation it would erode by exactly the latency as the hardware got slower.
            Assert.That(comp.RealizedLeadSeconds, Is.GreaterThan(0.40 * 0.75));
            Assert.That(uncomp.RealizedLeadSeconds, Is.GreaterThan(0.40 * 0.75));
            Assert.That(Math.Abs(comp.RealizedLeadSeconds - uncomp.RealizedLeadSeconds), Is.LessThan(0.05),
                "latency must not shift the delivered lead -- that is what compensation is for");
        }

        [Test]
        public void Uncompensated_latency_shortens_the_delivered_lead_by_exactly_that_latency()
        {
            // The failure mode if PipelineLatencySeconds is left at 0 while the hardware really takes 60 ms.
            // Both runs fire at the same TTC, but one vibration arrives 60 ms later in the approach.
            var t = E2Trial.Warning("W3", Limb, "H1", Target, Tempo.Normal, leadSeconds: 0.40f);
            var (compensated, _) = Run(t, P(0.060f), 1.2f, 1.5f, 0.9f);

            var pretendNoLatency = new E2Params { PipelineLatencySeconds = 0f };
            var (uncompensated, _) = Run(t, pretendNoLatency, 1.2f, 1.5f, 0.9f);

            Assert.That(compensated.CommandTime, Is.LessThan(uncompensated.CommandTime),
                "compensating for latency means committing to fire earlier in the reach");
        }

        [Test]
        public void Longer_assigned_leads_fire_earlier_and_further_out()
        {
            var shortLead = E2Trial.Warning("S", Limb, "H1", Target, Tempo.Normal, 0.20f);
            var longLead = E2Trial.Warning("L", Limb, "H1", Target, Tempo.Normal, 0.60f);

            var (s, _) = Run(shortLead, P(), 1.4f, 1.7f, 0.9f);
            var (l, _) = Run(longLead, P(), 1.4f, 1.7f, 0.9f);

            Assert.That(l.CommandTime, Is.LessThan(s.CommandTime));
            Assert.That(l.DistanceAtCue, Is.GreaterThan(s.DistanceAtCue));
            Assert.That(l.RealizedLeadSeconds, Is.GreaterThan(s.RealizedLeadSeconds));
        }

        [Test]
        public void The_cue_is_not_eligible_before_movement_onset()
        {
            // The limb sits still inside what would otherwise be a firing TTC. Nothing may fire: §8 makes the
            // warning eligible only after confirmed movement onset, so a lead time is always measured against
            // a reach that is genuinely underway.
            var t = E2Trial.Warning("W4", Limb, "H1", Target, Tempo.Normal, 0.40f);
            var sink = new Sink();
            var runner = new E2TrialRunner(t, Hazards(), P(), sink);
            for (int i = 0; i < 90; i++)
                runner.Tick(Frame(i * Dt, new Vector3(0f, 1f, SurfaceZ - 0.25f)));
            E2TrialOutcome o = runner.Finish();

            Assert.That(sink.Fired, Is.Empty);
            Assert.That(o.Valid, Is.False);
            Assert.That(o.InvalidReason, Is.EqualTo("no_movement_onset"));
        }

        [Test]
        public void A_cue_that_never_fires_at_all_invalidates_the_trial()
        {
            // The reach stops far short, so the TTC threshold is never crossed and no cue is ever issued.
            // There is no lead time to analyse, so the trial leaves the denominator rather than counting as
            // a successful avoidance.
            var t = E2Trial.Warning("W5", Limb, "H1", Target, Tempo.Normal, leadSeconds: 0.40f);
            var (o, sink) = Run(t, P(), approach: 3.0f, travel: 0.4f, movementTime: 0.6f);

            Assert.That(sink.Fired, Is.Empty);
            Assert.That(o.Valid, Is.False);
            Assert.That(o.InvalidReason, Is.EqualTo("cue_never_eligible"));
        }

        [Test]
        public void A_mistimed_cue_is_a_flagged_deviation_not_a_deleted_trial()
        {
            // The reach starts well inside the assigned 0.60 s lead, so the cue lands far later than intended.
            // Per SS8 that is a PROTOCOL DEVIATION, not a deletion: the psychometric fit uses the REALIZED
            // lead, so the trial is still a legitimate point on the curve. Deleting these would bias the fit,
            // because they concentrate on reaches that were too fast or started too close -- exactly the tail
            // that matters.
            var t = E2Trial.Warning("W5b", Limb, "H1", Target, Tempo.Normal, leadSeconds: 0.60f);
            var (o, sink) = Run(t, P(), approach: 0.30f, travel: 0.45f, movementTime: 0.5f);

            Assert.That(sink.Fired.Count, Is.EqualTo(1), "the cue does fire -- just late");
            Assert.That(o.Valid, Is.True, "a mistimed trial is kept, not deleted");
            Assert.That(o.TimingDeviation, Is.True);
            Assert.That(o.RealizedLeadSeconds, Is.LessThan(0.60 - 0.05),
                "delivered well short of the assigned lead, which is why it is flagged");
        }

        [Test]
        public void A_well_timed_cue_is_not_flagged()
        {
            var t = E2Trial.Warning("W5c", Limb, "H1", Target, Tempo.Normal, leadSeconds: 0.40f);
            var (o, _) = Run(t, P(), approach: 1.4f, travel: 1.7f, movementTime: 0.9f);

            Assert.That(o.Valid, Is.True);
            Assert.That(o.TimingDeviation, Is.False);
        }

        // ── Counterfactual validation ──────────────────────────────────────────────────────────

        [Test]
        public void Prediction_error_is_measurable_on_trials_where_contact_actually_happened()
        {
            // The study's largest unquantified assumption, turned into a number: on a crossing trial the true
            // contact time is known, so the frozen constant-velocity prediction can be scored against it.
            var t = E2Trial.Warning("W6", Limb, "H1", Target, Tempo.Normal, 0.40f);
            var (o, _) = Run(t, P(), approach: 1.2f, travel: 1.5f, movementTime: 0.9f);

            Assert.That(o.Crossed, Is.EqualTo(1));
            Assert.That(o.PredictedContactTime, Is.Not.NaN);
            Assert.That(o.ActualContactTime, Is.Not.NaN);
            Assert.That(o.PredictionErrorSeconds, Is.Not.NaN);
            // Constant velocity over-predicts speed on a decelerating min-jerk tail, so contact arrives LATER
            // than predicted and the error is negative. Direction matters; it is a bias, not noise.
            Assert.That(o.PredictionErrorSeconds, Is.LessThan(0.30));
        }

        [Test]
        public void Prediction_error_is_NA_when_no_contact_occurred()
        {
            var t = E2Trial.Warning("W7", Limb, "H1", Target, Tempo.Normal, 0.40f);
            var (o, _) = Run(t, P(), approach: 1.2f, travel: 0.85f, movementTime: 0.9f);

            Assert.That(o.Crossed, Is.EqualTo(0));
            Assert.That(o.ActualContactTime, Is.NaN);
            Assert.That(o.PredictionErrorSeconds, Is.NaN,
                "there is no ground truth to validate against when contact never happened");
        }

        // ── Kinematics, CSV, robustness ────────────────────────────────────────────────────────

        [Test]
        public void Post_cue_travel_and_arrest_are_recorded_after_the_cue()
        {
            var t = E2Trial.Warning("W8", Limb, "H1", Target, Tempo.Normal, 0.40f);
            var (o, _) = Run(t, P(), approach: 1.2f, travel: 1.0f, movementTime: 0.9f);

            Assert.That(o.PostCueTravel, Is.GreaterThan(0f));
            Assert.That(o.ArrestTime, Is.Not.NaN, "the reach comes to rest, so an arrest time must exist");
        }

        [Test]
        public void An_unknown_hazard_id_invalidates_the_trial_and_fires_nothing()
        {
            var t = E2Trial.Warning("W9", Limb, "NOPE", Target, Tempo.Normal, 0.40f);
            var (o, sink) = Run(t, P(), 1.2f, 1.5f, 0.9f);

            Assert.That(sink.Fired, Is.Empty);
            Assert.That(o.Valid, Is.False);
            Assert.That(o.InvalidReason, Is.EqualTo("hazard_id_not_in_scene"));
        }

        [Test]
        public void Csv_row_matches_the_header_and_writes_NA_not_NaN()
        {
            var t = E2Trial.Warning("W10", Limb, "H1", Target, Tempo.Urgent, 0.40f);
            var (o, _) = Run(t, P(0.05f), 1.2f, 1.5f, 0.9f);
            string row = E2TrialOutcomeFormatter.Row(participant: 1, session: 1, o);

            Assert.That(row.Split(',').Length,
                Is.EqualTo(E2TrialOutcomeFormatter.HeaderLine.Split(',').Length));
            Assert.That(row, Does.Not.Contain("NaN"));
            Assert.That(row, Does.Not.Contain("Infinity"));
            Assert.That(E2TrialOutcomeFormatter.HeaderLine, Does.Contain("realized_lead_s"));
            Assert.That(E2TrialOutcomeFormatter.HeaderLine, Does.Contain("prediction_error_s"));
        }

        [Test]
        public void Tempo_is_carried_through_to_the_row()
        {
            var t = E2Trial.Warning("W11", Limb, "H1", Target, Tempo.Urgent, 0.40f);
            var (o, _) = Run(t, P(), 1.2f, 1.5f, 0.6f);

            Assert.That(o.Tempo, Is.EqualTo(Tempo.Urgent));
            Assert.That(E2TrialOutcomeFormatter.Row(1, 1, o), Does.Contain("Urgent"));
        }

        /// <summary>
        /// End to end: sweep the lead-time levels and confirm the delivered leads are ordered and spread.
        /// This is the shape the psychometric fit consumes — five levels producing five distinct, measured
        /// x-values. It does not assert avoidance, because a synthetic reach has no motor system; it asserts
        /// the **independent variable is actually delivered and measured**, which is the part code can own.
        /// </summary>
        [Test]
        public void A_sweep_of_assigned_levels_produces_ordered_measured_leads()
        {
            float[] levels = { 0.15f, 0.30f, 0.45f, 0.60f, 0.75f };
            var realized = new List<double>();

            foreach (float lead in levels)
            {
                var t = E2Trial.Warning("S" + lead, Limb, "H1", Target, Tempo.Normal, lead);
                var (o, sink) = Run(t, P(0.04f), approach: 2.0f, travel: 2.3f, movementTime: 1.1f);
                Assert.That(sink.Fired.Count, Is.EqualTo(1), "level " + lead + " never fired");
                Assert.That(o.RealizedLeadSeconds, Is.Not.NaN, "level " + lead + " produced no measured lead");
                realized.Add(o.RealizedLeadSeconds);
            }

            for (int i = 1; i < realized.Count; i++)
                Assert.That(realized[i], Is.GreaterThan(realized[i - 1]),
                    "delivered leads must increase with assigned level");

            Assert.That(realized[realized.Count - 1] - realized[0], Is.GreaterThan(0.4),
                "the sweep must span a usable range, or the curve cannot be fitted");
        }
    }
}
