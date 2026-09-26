using System;
using System.Collections.Generic;
using UnityEngine;

namespace CollisionFeedback.Core.E2
{
    /// <summary>One trial plus the geometry it is run against.</summary>
    public readonly struct E2ScheduledTrial
    {
        public readonly E2Trial Trial;
        public readonly Obstacle Hazard;
        /// <summary>Where the reaching limb starts. The home region is identical every trial (§8).</summary>
        public readonly Vector3 Home;
        /// <summary>Clear limb-to-hazard-surface distance at trial start.</summary>
        public readonly float ApproachDistance;
        /// <summary>Practice trial: run identically, never written to the primary file.</summary>
        public readonly bool IsPractice;

        public E2ScheduledTrial(E2Trial trial, Obstacle hazard, Vector3 home, float approachDistance,
                                bool isPractice = false)
        {
            Trial = trial;
            Hazard = hazard;
            Home = home;
            ApproachDistance = approachDistance;
            IsPractice = isPractice;
        }
    }

    /// <summary>Session-construction knobs [PAPER2_STUDY_DESIGN §8]. Freeze before collection.</summary>
    public sealed class E2PlanParams
    {
        /// <summary>Total trials. §8 plans 320–400, giving 80–100 warning trials at 25% prevalence.</summary>
        public int TotalTrials = 360;

        /// <summary>
        /// Trials per mini-block, each containing exactly one warning trial. **This is what locks prevalence
        /// at 25% locally, not just on average** — §8 requires prevalence held at 25% to prevent strategic
        /// slowing, and a globally-25% schedule that happens to cluster warnings would let a participant
        /// detect a run and start braking pre-emptively.
        /// </summary>
        public int MiniBlockSize = 4;

        /// <summary>
        /// The preregistered lead-time levels (s). §8: 5–7 levels spanning ~10–90% avoidance.
        ///
        /// **These are targets the system aims at, not promises.** Measured delivery with this geometry is
        /// systematically ~15–20% short at the longer levels — see
        /// <see cref="E2SessionPlan"/> "Delivered leads run short" below. The achieved levels for these
        /// defaults are roughly **0.06 / 0.12 / 0.21 / 0.28 / 0.40 s**, which brackets the 150–220 ms hand
        /// reaction-time band the threshold plausibly sits in.
        ///
        /// §8 requires these to be set from pilot data. Set them so the **achieved** distribution spans
        /// 10–90% avoidance, not the assigned one.
        /// </summary>
        public float[] LeadLevels = { 0.08f, 0.15f, 0.25f, 0.35f, 0.50f };

        /// <summary>
        /// Reach amplitude from home to the visible target (m). Raised from 0.55 to 0.70 after simulation:
        /// 0.55 m left only ~0.19 m of clear approach, too little for the longer leads to be deliverable at
        /// all. Past ~0.70 m the gain plateaus, because the residual shortfall is acceleration, not geometry.
        /// </summary>
        public float ReachDistance = 0.70f;
        /// <summary>Half-extents of every hazard volume (m).</summary>
        public Vector3 HazardHalfExtents = new Vector3(0.10f, 0.10f, 0.10f);

        /// <summary>
        /// Where along the home→target segment the hazard centre sits, as a fraction. Varied per trial so the
        /// hazard boundary cannot be memorised (§8 item 3).
        /// </summary>
        public float HazardFractionMin = 0.60f;
        public float HazardFractionMax = 0.80f;

        /// <summary>Azimuth spread of the target around straight ahead (degrees, ±).</summary>
        public float AzimuthSpreadDeg = 35f;
        /// <summary>Elevation spread of the target (degrees, ±).</summary>
        public float ElevationSpreadDeg = 18f;

        public Joint TargetLimb = Joint.RightHand;
        public Vector3 Home = new Vector3(0.20f, 1.10f, 0.20f);
        public int Seed = 20260914;

        /// <summary>
        /// Must match <see cref="E2Params.MovementOnsetSpeed"/>. Used only by <see cref="E2SessionPlan.Audit"/>
        /// to decide whether a lead is deliverable at all; the runner uses its own copy.
        /// </summary>
        public float MovementOnsetSpeed = 0.15f;

        /// <summary>
        /// Practice trials emitted before the measured sequence. Run identically and discarded, but drawn
        /// from <see cref="PracticeLeadLevel"/> rather than the full range.
        ///
        /// **Why practice is not just "the first N measured trials".** Drawing practice from the full range
        /// means a participant meets the short, impossible-to-stop leads before forming a stable response —
        /// exactly the condition the instruction script's "sometimes you will not be able to stop" clause
        /// exists to defuse, arriving before the reassurance can do any work. Stop-signal practice
        /// conventionally uses easy stop signals first, and this follows that.
        /// </summary>
        public int PracticeTrials = 16;

        /// <summary>
        /// Lead used for practice warning trials. Negative means "the longest configured level", which is the
        /// easiest to stop and therefore the right one to learn the response on.
        /// </summary>
        public float PracticeLeadLevel = -1f;

        /// <summary>
        /// Throw from <see cref="E2SessionPlan.Build"/> when the geometry cannot deliver the assigned leads.
        /// Leave on: a plan that silently contains undeliverable trials produces non-random missingness
        /// concentrated in the tail that determines LT80.
        /// </summary>
        public bool ThrowOnFailedAudit = true;
    }

    /// <summary>
    /// Builds an E2 session [PAPER2_STUDY_DESIGN §8]: the trial sequence, the lead-level allocation, the tempo
    /// assignment, and the per-trial geometry.
    ///
    /// Three properties this guarantees, each because §8 requires it:
    ///
    /// 1. **Prevalence is locked at 25% inside every mini-block**, not merely on average, so a participant
    ///    cannot detect a run of warning trials and slow down pre-emptively.
    /// 2. **Lead level and tempo are crossed and balanced.** §8 requires "the cue waveform, prevalence, hazard
    ///    geometry distribution, and lead-time allocation identical across tempo" — if urgent trials carried
    ///    short leads more often, tempo and lead time would be confounded and H3a would be uninterpretable.
    /// 3. **Geometry varies per trial** — target azimuth, elevation, and where the hazard sits along the reach
    ///    — so neither the target direction nor the hazard boundary can be memorised.
    ///
    /// Deterministic given <see cref="E2PlanParams.Seed"/>, so a session is reproducible from its log.
    ///
    /// ── DELIVERED LEADS RUN SHORT, AND IT IS NOT A BUG ─────────────────────────────────────────────────
    /// Simulation against the real runner shows the delivered lead is **systematically shorter than the
    /// assigned lead**, by roughly 15–20% at the longer levels, and the shortfall barely improves when the
    /// reach is lengthened from 0.70 m to 0.90 m.
    ///
    /// The cause is acceleration. The cue is committed when TTC reaches `lead + latency`, but the limb is
    /// still speeding up; over the latency window the gap closes faster than the frozen speed predicts, and
    /// TTC can overshoot the threshold between frames. A forward model using acceleration would remove it —
    /// and would also replace the deliberately simple constant-velocity estimator the design commits to,
    /// where the manipulated variable is *timing*, not predictor quality.
    ///
    /// **So it is accepted rather than corrected.** The psychometric fit uses the *realized* lead, so a
    /// systematic offset shifts which levels were achieved, not whether the curve is valid. What it does mean:
    ///
    /// - `LeadLevels` must be chosen so the **achieved** spread covers 10–90% avoidance (§8), not the assigned
    ///   spread. Tune the knob at pilot against measured delivery.
    /// - `E2Params.TimingToleranceSeconds` cannot be set a priori. Measure the bias at pilot, then set the
    ///   band around the *expected* delivery. Left at a naive 0.05 s it flags almost every trial and tells
    ///   you nothing.
    /// </summary>
    public static class E2SessionPlan
    {
        public static List<E2ScheduledTrial> Build(E2PlanParams p)
        {
            var rng = new System.Random(p.Seed);
            int blocks = Math.Max(1, p.TotalTrials / Math.Max(2, p.MiniBlockSize));

            // Lead levels crossed with tempo, one balanced pool, shuffled. Each mini-block draws one warning
            // trial from this pool, so levels and tempo stay balanced across the whole session.
            var pool = new List<(float lead, Tempo tempo)>(blocks + p.LeadLevels.Length * 2);
            var tempos = new[] { Tempo.Normal, Tempo.Urgent };
            int i = 0;
            while (pool.Count < blocks)
            {
                pool.Add((p.LeadLevels[i % p.LeadLevels.Length], tempos[(i / p.LeadLevels.Length) % 2]));
                i++;
            }
            Shuffle(pool, rng);

            var plan = new List<E2ScheduledTrial>(blocks * p.MiniBlockSize + Math.Max(0, p.PracticeTrials));
            int trialNo = 0;

            // ── Practice prefix: same task, same geometry distribution, easy stops only ────────────────
            if (p.PracticeTrials > 0 && p.LeadLevels.Length > 0)
            {
                float practiceLead = p.PracticeLeadLevel > 0f ? p.PracticeLeadLevel : Max(p.LeadLevels);
                int practiceWarnEvery = Math.Max(2, p.MiniBlockSize);

                for (int k = 0; k < p.PracticeTrials; k++)
                {
                    trialNo++;
                    string pid = "P" + trialNo.ToString("D4");
                    bool isWarning = (k % practiceWarnEvery) == practiceWarnEvery - 1;
                    Tempo tempo = tempos[rng.Next(2)];
                    Geometry(p, rng, out Vector3 ptarget, out Obstacle phazard, out float papproach, trialNo);

                    E2Trial ptrial = isWarning
                        ? E2Trial.Warning(pid, p.TargetLimb, phazard.Id, ptarget, tempo, practiceLead)
                        : E2Trial.Go(pid, p.TargetLimb, phazard.Id, ptarget, tempo);

                    plan.Add(new E2ScheduledTrial(ptrial, phazard, p.Home, papproach, isPractice: true));
                }
            }

            for (int b = 0; b < blocks; b++)
            {
                int warningSlot = rng.Next(p.MiniBlockSize);
                (float lead, Tempo tempo) w = pool[b];

                for (int s = 0; s < p.MiniBlockSize; s++)
                {
                    trialNo++;
                    string id = "T" + trialNo.ToString("D4");
                    bool isWarning = s == warningSlot;

                    // Go trials get their own randomised tempo; warning trials use the pooled assignment so
                    // the lead × tempo crossing stays balanced.
                    Tempo tempo = isWarning ? w.tempo : tempos[rng.Next(2)];

                    Geometry(p, rng, out Vector3 target, out Obstacle hazard, out float approach, trialNo);

                    E2Trial trial = isWarning
                        ? E2Trial.Warning(id, p.TargetLimb, hazard.Id, target, tempo, w.lead)
                        : E2Trial.Go(id, p.TargetLimb, hazard.Id, target, tempo);

                    plan.Add(new E2ScheduledTrial(trial, hazard, p.Home, approach));
                }
            }

            // Fail loudly rather than hand back a plan containing trials whose leads can never be delivered.
            E2PlanAudit audit = Audit(plan, p);
            if (!audit.Pass && p.ThrowOnFailedAudit)
                throw new System.InvalidOperationException(
                    audit.Describe() +
                    " Lengthen ReachDistance, move HazardFraction outward, shrink HazardHalfExtents, or drop " +
                    "the longest lead level. Do not disable this check to get past it — the trials it flags " +
                    "become 'cue_never_eligible' losses concentrated at the long leads, which is exactly the " +
                    "tail that determines LT80.");

            return plan;
        }

        private static float Max(float[] v)
        {
            float m = v[0];
            for (int i = 1; i < v.Length; i++) if (v[i] > m) m = v[i];
            return m;
        }

        /// <summary>
        /// Procedural per-trial geometry: a target at a varied azimuth/elevation, with the hazard sitting on
        /// the direct home→target path so "the natural direct reach intersects the hidden volume" (§8).
        /// </summary>
        private static void Geometry(E2PlanParams p, System.Random rng, out Vector3 target, out Obstacle hazard,
                                     out float approach, int trialNo)
        {
            float az = (float)((rng.NextDouble() * 2 - 1) * p.AzimuthSpreadDeg) * Mathf.Deg2Rad;
            float el = (float)((rng.NextDouble() * 2 - 1) * p.ElevationSpreadDeg) * Mathf.Deg2Rad;

            var dir = new Vector3(Mathf.Sin(az) * Mathf.Cos(el), Mathf.Sin(el), Mathf.Cos(az) * Mathf.Cos(el));
            target = p.Home + dir * p.ReachDistance;

            float f = p.HazardFractionMin +
                      (float)rng.NextDouble() * (p.HazardFractionMax - p.HazardFractionMin);
            Vector3 centre = p.Home + dir * (p.ReachDistance * f);

            hazard = new Obstacle("HZ" + trialNo.ToString("D4"), centre, p.HazardHalfExtents);
            approach = hazard.DistanceTo(p.Home);
        }

        /// <summary>
        /// Distance at which the cue fires for a given lead, if the limb were travelling at
        /// <paramref name="closingSpeed"/> at that moment: the cue commits when TTC reaches the lead, i.e. at
        /// distance `speed × lead`.
        ///
        /// ⚠ **This is NOT a deliverability test, and using it as one produces false alarms.** Substituting a
        /// *peak* closing speed says the 0.25 / 0.35 / 0.50 s levels need 0.45 / 0.61 / 0.85 m of clear
        /// approach against the ~0.32–0.46 m this geometry provides — yet simulation against the real runner
        /// shows all three deliver (0.50 s assigned lands at ~0.40 s achieved).
        ///
        /// The reason is that the cue fires *before* peak speed is reached: TTC starts high because the limb
        /// is slow, and falls as the reach accelerates. So the cue fires early, at a lower speed and a shorter
        /// distance than a peak-speed calculation assumes. That is the same acceleration effect that makes
        /// delivery run short — a documented, accepted bias, not a failure.
        ///
        /// Use <see cref="Audit"/> for the real check. This function is retained because the fire distance is
        /// a genuinely useful quantity for reasoning about geometry; it is just not a pass/fail criterion.
        /// </summary>
        public static float FireDistance(float leadSeconds, float closingSpeed) => leadSeconds * closingSpeed;

        /// <summary>
        /// Whether a plan's geometry can actually deliver the leads it assigns [PAPER2 §8].
        ///
        /// **This is the Paper 1 geometry lesson in a new form.** There, a hazard 4 cm from the hand made the
        /// timing factor inoperable while the block summary still looked healthy. Here the equivalent failure
        /// is silent too: undeliverable trials surface as `cue_never_eligible`, concentrated at the long
        /// leads — missingness that is not at random, in precisely the tail that determines LT80.
        /// </summary>
        public readonly struct E2PlanAudit
        {
            public readonly int WarningTrials;
            /// <summary>Hazard volume swallows the home position — the reach starts inside the hazard.</summary>
            public readonly int HazardEngulfsHome;
            /// <summary>
            /// TTC at movement onset is already below the assigned lead, so the cue is overdue before the
            /// reach has begun and can never be delivered at its assigned time.
            /// </summary>
            public readonly int OverdueAtOnset;
            public readonly float MinApproach;
            public readonly float MaxApproach;
            /// <summary>Longest assigned lead that every warning trial can still deliver (s).</summary>
            public readonly float LongestDeliverableLead;

            public E2PlanAudit(int warningTrials, int engulfs, int overdue,
                               float minApproach, float maxApproach, float longestLead)
            {
                WarningTrials = warningTrials; HazardEngulfsHome = engulfs; OverdueAtOnset = overdue;
                MinApproach = minApproach; MaxApproach = maxApproach; LongestDeliverableLead = longestLead;
            }

            public bool Pass => HazardEngulfsHome == 0 && OverdueAtOnset == 0;

            public string Describe() =>
                $"E2 plan audit: {WarningTrials} warning trials, clear approach {MinApproach:F3}–{MaxApproach:F3} m, " +
                $"longest deliverable lead {LongestDeliverableLead:F2} s. " +
                (Pass ? "PASS."
                      : $"FAIL — {HazardEngulfsHome} trials start inside the hazard, " +
                        $"{OverdueAtOnset} have a lead already overdue at movement onset.");
        }

        /// <summary>
        /// Audits a built plan. The criterion is <b>TTC at movement onset must exceed the assigned lead</b> —
        /// if the limb, moving at <see cref="E2PlanParams.MovementOnsetSpeed"/>, is already closer than the
        /// lead demands, the cue can never land on time.
        ///
        /// Deliberately permissive: it flags geometry that is *impossible*, not geometry that merely delivers
        /// short. Delivering short is expected and handled by fitting on the realized lead.
        /// </summary>
        public static E2PlanAudit Audit(IReadOnlyList<E2ScheduledTrial> plan, E2PlanParams p)
        {
            int warning = 0, engulfs = 0, overdue = 0;
            float min = float.PositiveInfinity, max = float.NegativeInfinity;
            float longest = float.PositiveInfinity;

            for (int i = 0; i < plan.Count; i++)
            {
                E2ScheduledTrial st = plan[i];
                if (st.ApproachDistance < min) min = st.ApproachDistance;
                if (st.ApproachDistance > max) max = st.ApproachDistance;

                if (st.ApproachDistance <= 0f) { engulfs++; continue; }

                float ttcAtOnset = st.ApproachDistance / Mathf.Max(0.01f, p.MovementOnsetSpeed);
                if (ttcAtOnset < longest) longest = ttcAtOnset;

                if (!st.Trial.IsWarningTrial) continue;
                warning++;
                if (ttcAtOnset <= st.Trial.AssignedLeadSeconds) overdue++;
            }

            if (float.IsPositiveInfinity(min)) { min = 0f; max = 0f; }
            if (float.IsPositiveInfinity(longest)) longest = 0f;
            return new E2PlanAudit(warning, engulfs, overdue, min, max, longest);
        }

        private static void Shuffle<T>(IList<T> list, System.Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
