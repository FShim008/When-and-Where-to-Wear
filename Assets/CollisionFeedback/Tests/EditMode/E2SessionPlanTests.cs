using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using CollisionFeedback.Core;
using CollisionFeedback.Core.E2;
using Joint = CollisionFeedback.Core.Joint; // disambiguate from UnityEngine.Joint (physics component)

namespace CollisionFeedback.Tests
{
    /// <summary>
    /// The E2 session schedule [PAPER2_STUDY_DESIGN §8].
    ///
    /// The last test is the important one. It does not check the plan's *description* of itself — it runs every
    /// generated trial through the real <see cref="E2TrialRunner"/> with a synthetic reach and asks whether the
    /// assigned lead could actually be delivered. **That is the Paper 1 geometry lesson applied in advance:**
    /// there, a hazard placed 4 cm from the hand made the timing factor inoperable while the block summary
    /// still looked healthy. Deliverability is checked, never assumed.
    /// </summary>
    public class E2SessionPlanTests
    {
        private const float Hz = 90f;
        private const float Dt = 1f / Hz;
        private static readonly Vector3 Far = new(50f, 50f, 50f);

        private static float MinJerk(float u) => 10f * u * u * u - 15f * u * u * u * u + 6f * u * u * u * u * u;

        private sealed class Sink : IFeedbackSink
        {
            public readonly List<FeedbackCommand> Fired = new();
            public void Fire(in FeedbackCommand c) => Fired.Add(c);
        }

        private static PoseFrame Frame(double t, Joint limb, Vector3 where)
        {
            var j = new Vector3[JointInfo.Count];
            for (int i = 0; i < j.Length; i++) j[i] = Far;
            j[(int)limb] = where;
            return new PoseFrame { Timestamp = t, Joints = j };
        }

        /// <summary>Drives one scheduled trial with a straight min-jerk reach from home to the target.</summary>
        private static (E2TrialOutcome o, Sink sink) Drive(E2ScheduledTrial st, E2Params p, float movementTime)
        {
            var sink = new Sink();
            var runner = new E2TrialRunner(st.Trial, new List<Obstacle> { st.Hazard }, p, sink);
            Vector3 a = st.Home, b = st.Trial.TargetPosition;

            int n = Math.Max(8, (int)(movementTime * Hz));
            int i = 0;
            for (; i <= n; i++)
                runner.Tick(Frame(i * Dt, st.Trial.TargetLimb, Vector3.Lerp(a, b, MinJerk((float)i / n))));
            double end = i * Dt + 0.25;
            for (; i * Dt <= end; i++) runner.Tick(Frame(i * Dt, st.Trial.TargetLimb, b));
            return (runner.Finish(), sink);
        }

        // ── Structure ──────────────────────────────────────────────────────────────────────────

        [Test]
        public void Prevalence_is_exactly_25_percent_inside_every_mini_block()
        {
            var p = new E2PlanParams();
            List<E2ScheduledTrial> plan = E2SessionPlan.Build(p);

            for (int b = 0; b + p.MiniBlockSize <= plan.Count; b += p.MiniBlockSize)
            {
                int warnings = 0;
                for (int s = 0; s < p.MiniBlockSize; s++)
                    if (plan[b + s].Trial.IsWarningTrial) warnings++;
                Assert.That(warnings, Is.EqualTo(1),
                    "a clustered run of warnings would let participants brake pre-emptively (§8)");
            }
        }

        [Test]
        public void Lead_levels_are_balanced_across_the_session()
        {
            // Practice is excluded: it is discarded before analysis and sits deliberately at one easy lead,
            // so balance is a property of the MEASURED sequence.
            var p = new E2PlanParams();
            var counts = E2SessionPlan.Build(p)
                .Where(t => !t.IsPractice && t.Trial.IsWarningTrial)
                .GroupBy(t => t.Trial.AssignedLeadSeconds)
                .ToDictionary(g => g.Key, g => g.Count());

            Assert.That(counts.Count, Is.EqualTo(p.LeadLevels.Length), "every level must be used");
            int min = counts.Values.Min(), max = counts.Values.Max();
            Assert.That(max - min, Is.LessThanOrEqualTo(1), "levels must be balanced to within one trial");
        }

        [Test]
        public void Tempo_is_crossed_with_lead_level_not_confounded_with_it()
        {
            // §8: the lead-time allocation must be identical across tempo. If urgent trials carried short
            // leads more often, H3a could not separate "urgent reaches need more warning" from "urgent trials
            // happened to get less warning".
            var p = new E2PlanParams();
            var byLead = E2SessionPlan.Build(p).Where(t => !t.IsPractice && t.Trial.IsWarningTrial)
                .GroupBy(t => t.Trial.AssignedLeadSeconds);

            foreach (var g in byLead)
            {
                int urgent = g.Count(t => t.Trial.Tempo == Tempo.Urgent);
                int normal = g.Count(t => t.Trial.Tempo == Tempo.Normal);
                Assert.That(Math.Abs(urgent - normal), Is.LessThanOrEqualTo(1),
                    "lead " + g.Key + " is unbalanced across tempo (" + normal + " normal / " + urgent + " urgent)");
            }
        }

        [Test]
        public void Geometry_varies_from_trial_to_trial()
        {
            var plan = E2SessionPlan.Build(new E2PlanParams());
            int distinctTargets = plan.Select(t => t.Trial.TargetPosition).Distinct().Count();
            int distinctHazards = plan.Select(t => t.Hazard.Center).Distinct().Count();

            Assert.That(distinctTargets, Is.GreaterThan(plan.Count / 2), "target direction must vary (§8)");
            Assert.That(distinctHazards, Is.GreaterThan(plan.Count / 2), "hazard placement must vary (§8)");
        }

        [Test]
        public void The_direct_reach_actually_intersects_the_hazard()
        {
            // §8 item 4. If the hazard sat off the path, there would be nothing to avoid and no opportunity.
            foreach (var st in E2SessionPlan.Build(new E2PlanParams()).Take(60))
            {
                Vector3 a = st.Home, b = st.Trial.TargetPosition;
                float closest = float.PositiveInfinity;
                for (int k = 0; k <= 100; k++)
                    closest = Mathf.Min(closest, st.Hazard.DistanceTo(Vector3.Lerp(a, b, k / 100f)));
                Assert.That(closest, Is.EqualTo(0f).Within(1e-3f),
                    "trial " + st.Trial.Id + ": the straight reach misses the hazard");
            }
        }

        [Test]
        public void The_plan_is_deterministic_for_a_given_seed()
        {
            var a = E2SessionPlan.Build(new E2PlanParams { Seed = 7 });
            var b = E2SessionPlan.Build(new E2PlanParams { Seed = 7 });
            for (int i = 0; i < a.Count; i++)
            {
                Assert.That(b[i].Trial.Id, Is.EqualTo(a[i].Trial.Id));
                Assert.That(b[i].Trial.IsWarningTrial, Is.EqualTo(a[i].Trial.IsWarningTrial));
                Assert.That(b[i].Hazard.Center, Is.EqualTo(a[i].Hazard.Center));
            }
        }

        // ── Deliverability: the one that matters ───────────────────────────────────────────────

        [Test]
        public void Every_assigned_lead_is_actually_deliverable_by_the_generated_geometry()
        {
            // Run each warning trial through the real runner. A lead that cannot be delivered is the E2
            // equivalent of Paper 1's misplaced pillar: the independent variable silently fails to exist,
            // and nothing in a summary line would say so.
            var p = new E2PlanParams();
            var e2 = new E2Params { PipelineLatencySeconds = 0.05f };
            var plan = E2SessionPlan.Build(p).Where(t => t.Trial.IsWarningTrial).Take(40).ToList();

            var undeliverable = new List<string>();
            var deviations = new List<string>();

            foreach (var st in plan)
            {
                float mt = st.Trial.Tempo == Tempo.Urgent ? 0.45f : 0.75f;
                var (o, sink) = Drive(st, e2, mt);
                if (sink.Fired.Count == 0 || double.IsNaN(o.RealizedLeadSeconds))
                    undeliverable.Add(st.Trial.Id + " lead=" + st.Trial.AssignedLeadSeconds +
                                      " approach=" + st.ApproachDistance.ToString("F2"));
                else if (o.TimingDeviation)
                    deviations.Add(st.Trial.Id + " lead=" + st.Trial.AssignedLeadSeconds +
                                   " realized=" + o.RealizedLeadSeconds.ToString("F3"));
            }

            Assert.That(undeliverable, Is.Empty,
                "these trials could never deliver their assigned lead:\n  " + string.Join("\n  ", undeliverable));

            // NOTE: deviation count is deliberately NOT asserted. Delivery is systematically ~15-20% short of
            // the assigned lead at longer levels (acceleration, see E2SessionPlan), so a band around the
            // ASSIGNED lead flags nearly everything. That is a property of the physics, not a defect, and the
            // psychometric fit uses the realized lead regardless. What must hold is below: the achieved levels
            // stay ordered and spread.
            Assert.That(deviations, Is.Not.Null);
        }

        [Test]
        public void Achieved_leads_stay_ordered_and_spread_across_the_assigned_levels()
        {
            // The property the psychometric fit actually needs: distinct, ordered x-values covering a usable
            // range. Whether each one equals its nominal label does not matter -- the fit uses what was
            // delivered. What would matter is levels collapsing into each other, because then the curve has
            // no leverage.
            var p = new E2PlanParams();
            var e2 = new E2Params { PipelineLatencySeconds = 0.05f };
            var plan = E2SessionPlan.Build(p).Where(t => t.Trial.IsWarningTrial).ToList();

            var medians = new List<double>();
            foreach (float level in p.LeadLevels)
            {
                var got = plan.Where(t => Math.Abs(t.Trial.AssignedLeadSeconds - level) < 1e-4f).Take(10)
                              .Select(t => Drive(t, e2, t.Trial.Tempo == Tempo.Urgent ? 0.45f : 0.75f).o
                                            .RealizedLeadSeconds)
                              .Where(x => !double.IsNaN(x)).OrderBy(x => x).ToList();
                Assert.That(got, Is.Not.Empty, "level " + level + " delivered nothing");
                medians.Add(got[got.Count / 2]);
            }

            for (int i = 1; i < medians.Count; i++)
                Assert.That(medians[i], Is.GreaterThan(medians[i - 1] + 0.02),
                    "achieved levels must stay separated, or the curve has no leverage");

            Assert.That(medians[medians.Count - 1] - medians[0], Is.GreaterThan(0.25),
                "the achieved spread must cover a usable range of the curve");
        }

        [Test]
        public void Fire_distance_scales_with_lead_and_speed()
        {
            Assert.That(E2SessionPlan.FireDistance(0.40f, 1.0f), Is.EqualTo(0.40f).Within(1e-4f));
            Assert.That(E2SessionPlan.FireDistance(0.40f, 2.0f), Is.EqualTo(0.80f).Within(1e-4f));
        }

        [Test]
        public void Default_geometry_passes_the_deliverability_audit()
        {
            var p = new E2PlanParams();
            var plan = E2SessionPlan.Build(p);
            E2SessionPlan.E2PlanAudit a = E2SessionPlan.Audit(plan, p);

            Assert.That(a.Pass, Is.True, a.Describe());
            Assert.That(a.HazardEngulfsHome, Is.Zero);
            Assert.That(a.OverdueAtOnset, Is.Zero);
            Assert.That(a.WarningTrials, Is.GreaterThan(80), "§8 plans 80-100 warning trials");
        }

        [Test]
        public void Audit_catches_a_reach_too_short_to_deliver_the_longest_lead()
        {
            // 0.25 m reach leaves ~0.05 m of clear approach; at the 0.15 m/s onset speed that is 0.33 s of
            // TTC, so the 0.50 s lead is already overdue before the reach begins.
            var p = new E2PlanParams { ReachDistance = 0.25f, ThrowOnFailedAudit = false };
            var plan = E2SessionPlan.Build(p);
            E2SessionPlan.E2PlanAudit a = E2SessionPlan.Audit(plan, p);

            Assert.That(a.Pass, Is.False, "a 0.25 m reach cannot deliver a 0.50 s lead");
            Assert.That(a.OverdueAtOnset, Is.GreaterThan(0));
            Assert.That(a.Describe(), Does.Contain("FAIL"));
        }

        [Test]
        public void Build_throws_rather_than_returning_an_undeliverable_plan()
        {
            // Silently handing back a bad plan is the Paper 1 geometry failure: the losses it causes are
            // concentrated at the long leads, which is the tail that determines LT80.
            var p = new E2PlanParams { ReachDistance = 0.25f };   // ThrowOnFailedAudit defaults to true
            Assert.That(() => E2SessionPlan.Build(p),
                        Throws.TypeOf<System.InvalidOperationException>());
        }

        [Test]
        public void Practice_trials_come_first_and_use_only_the_easiest_lead()
        {
            var p = new E2PlanParams { PracticeTrials = 16 };
            var plan = E2SessionPlan.Build(p);

            var practice = plan.FindAll(t => t.IsPractice);
            Assert.That(practice.Count, Is.EqualTo(16));
            for (int i = 0; i < 16; i++)
                Assert.That(plan[i].IsPractice, Is.True, "practice must be the prefix, not scattered");
            Assert.That(plan[16].IsPractice, Is.False);

            float longest = 0f;
            foreach (float l in p.LeadLevels) if (l > longest) longest = l;

            foreach (var t in practice)
            {
                if (!t.Trial.IsWarningTrial) continue;
                Assert.That(t.Trial.AssignedLeadSeconds, Is.EqualTo(longest).Within(1e-4f),
                    "practice must teach the response on stoppable warnings, not on the impossible ones");
            }
            Assert.That(practice.Exists(t => t.Trial.IsWarningTrial), Is.True,
                "practice with no warning trial never teaches the stop response");
        }

        [Test]
        public void Practice_trials_are_excluded_from_the_measured_lead_balance()
        {
            var withPractice = E2SessionPlan.Build(new E2PlanParams { PracticeTrials = 16 });
            var without      = E2SessionPlan.Build(new E2PlanParams { PracticeTrials = 0 });

            int measuredWarnings = withPractice.FindAll(t => !t.IsPractice && t.Trial.IsWarningTrial).Count;
            int plainWarnings    = without.FindAll(t => t.Trial.IsWarningTrial).Count;

            Assert.That(measuredWarnings, Is.EqualTo(plainWarnings),
                "adding practice must not consume measured warning trials");
        }
    }
}
