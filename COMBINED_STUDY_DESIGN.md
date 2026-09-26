# Combined Research Program — When, Where, and How Early to Warn

**Version:** 1.3 — 2026-09-08  
**Authority:** `PAPER1_STUDY_DESIGN.md` and `PAPER2_STUDY_DESIGN.md` control their respective experiments  
**Status:** Pre-implementation design; no human data collection is authorized

**Changes in 1.1:** added the cross-study decision framework describing how each paper determines "what works
best" (§6); corrected the program diagram to show parallel rather than sequential studies (§2); corrected the
venue claim (§8).

## 1. One program, two independent studies

The research program asks two connected but non-duplicative questions:

| Study | Question | Manipulated variables | Primary outcome | Main contribution |
|---|---|---|---|---|
| Paper 1 | Which warning policy and body mapping work best during an immersive task? | Proximity vs predictive policy × generic torso vs at-risk-limb haptics | Target limb enters target virtual hazard during a scripted opportunity | System/design comparison |
| Paper 2 | How much physical warning time does an ongoing reach require, and why do people differ? | Controlled warning lead time × movement tempo | Target hand enters a trial-specific virtual hazard | Psychometric threshold and motor mechanism |

The studies share the tracking/haptic platform and cue family. They do not share confirmatory trials, primary outcomes, models, or participants under the preferred design.

### Sampling — DECIDED 2026-09-07

**Independent cohorts.**

- **Cohort A** completes Paper 1 only — one visit, ~36–48 analyzable participants.
- **Cohort B** completes Paper 2 only — **one visit** (~95–110 min) with E1/E2 **task order randomized**,
  ~32–40 analyzable participants.
- Each paper has its own power analysis and preregistration.

No participant, trial, outcome, or inferential test is shared. This is the clearest protection against fatigue
carryover, circular condition selection, and duplicate-publication concerns, and it lets the ISMAR disclosure
letter assert independence outright rather than explain a shared sample.

**Total recruitment is the sum, not the maximum** — roughly 70–90 people, each attending once. Ethics
application, budget, and timeline must reflect that figure. The previously documented shared-participant
fallback is **rejected**; reintroducing it requires switching the disclosure letter to Option B and recording
the change.

## 2. How the studies connect scientifically

```text
Paper 1                                  Paper 2
Which warning design performs best       How early must a warning arrive during a
in a realistic VR task?                  controlled reach, and can stopping latency
                                         + movement speed predict that need?
        |                                              |
        +----------------------+-----------------------+
                               v
              Complementary evidence about the same
              design problem; no dependency either way
```

The two studies are **parallel, not sequential**. Paper 1 provides ecological task evidence; Paper 2 provides
timing precision and a motor-control explanation. Either may be run, analyzed, and published first, and neither
depends on the other producing a positive result. Do not draw the relationship as an arrow from Paper 1 to
Paper 2 in any manuscript figure — that implies Paper 2 was conditioned on Paper 1's outcome, which is exactly
the dependency the independent-cohort design exists to rule out.

Paper 2 uses a localized arm cue chosen a priori. It does not wait for Paper 1 to identify a winning condition.

## 3. Paper 1 participant experience

1. The participant enters an apparently open VR arena.
2. They reach for orbs and dodge projectiles.
3. Invisible virtual hazard zones lie near some scripted movements.
4. Across six counterbalanced blocks they receive different warning designs: four haptic combinations, no warning, and a matched visual benchmark.
5. The system records whether the intended body part enters the intended hazard for each opportunity.
6. Short questionnaires follow each block; hazard penalties are revealed only afterward.

Paper 1 estimates the total performance of each warning policy. If the predictive policy warns more often or earlier, that is part of the policy's behavior and is reported—not statistically adjusted away.

## 4. Paper 2 participant experience

### E1 — stopping measurement

1. The participant repeatedly reaches from a home point to one of two targets.
2. Most trials are ordinary fast reaches.
3. On about one trial in four, the tested arm buzzes and the participant tries to cancel the reach.
4. The delay changes after successful and failed stops.
5. Many trials, not a short 70-trial block, are used to estimate tactile SSRT reliably.

### E2 — warning-time measurement

1. The participant again reaches toward visible targets.
2. Each direct reach passes through a new invisible virtual hazard boundary.
3. On warning trials, the buzz occurs while the hand is already moving.
4. Sometimes the buzz arrives early and stopping is easy; sometimes it arrives late and stopping is difficult.
5. Normal and urgent reaches are randomly mixed.
6. The system measures boundary entry, braking, redirection, stopping distance, and timing.

The result is a response curve rather than a single lucky staircase value. The curve estimates LT50 and, when supported, higher success thresholds such as LT80.

## 5. What the combined program can establish

If supported by the data:

- Paper 1 can establish which policy/mapping configurations reduce virtual hazard-boundary violations in the immersive task.
- Paper 2 can establish how avoidance probability changes with physically delivered lead time.
- Paper 2 can establish whether tSSRT and movement speed explain or predict individual warning-time requirements.
- Together they can provide design guidance for when and where to warn.

The program cannot claim real-world injury reduction, universal whole-body thresholds, or superior personalization without additional validation.

## 6. How each study decides what "works best"

Both papers promise a practical answer, and both keep that answer structurally separate from hypothesis testing.
Neither uses a weighted composite score.

| | Paper 1 | Paper 2 |
|---|---|---|
| Decision question | Which of six conditions should a developer choose? | What lead time should a system use? |
| Form of the answer | A ranking over discrete conditions | A continuous requirement, as a function of movement speed |
| Method | Lexicographic rule with preregistered gates (`PAPER1_STUDY_DESIGN.md` §15) | Operating-point selection on a trade-off curve (`PAPER2_STUDY_DESIGN.md` §16) |
| Primary criterion | Marginal violation probability per valid opportunity | Avoidance probability at a physically realized lead time |
| Cost side of the trade-off | Alert burden; presence and workload gates | Unnecessary-warning rate |
| Uncertainty reporting | Rank probabilities, not a point ranking | Interval bounds, with the upper bound as the engineering figure |
| Never used | A weighted composite of safety and experience | LT50 as a recommended operating value |

**In-experiment task points are motivational, not analytic.** Paper 1's +1/−1/−3 scoring exists to induce the
engagement that makes virtual-hazard avoidance behaviorally realistic. Because the −3 penalty is computed from
the primary outcome, total score is algebraically dependent on it and is never reported as separate evidence.

**The two answers compose.** Paper 1 identifies which policy and mapping to use; Paper 2 specifies how early
that warning must arrive and how the requirement scales with movement speed. Neither result presupposes the
other, and each stands if the other returns a null.

## 7. Shared engineering requirements

**Hardware, revised 2026-09-08 (PI discussion):** bHaptics TactSuit + Tactosy for feedback. **Tracking is
decided by paired pilot comparison** — Vicon and commodity trackers logged **simultaneously** on the same
pilot participants, then selected **per paper** against criteria fixed and dated before the first session.
Procedure and criteria in `PAPER1_STUDY_DESIGN.md` §11; Paper 2 sets its own, stricter, in its §5.

Three things this requires: both systems built before piloting, criteria recorded in advance (choosing after
seeing which produced tidier data is not a decision), and acceptance that **a split outcome is legitimate** —
commodity trackers may suit Paper 1 and not Paper 2.

**The analytical prediction the pilot tests.** Both studies compute **velocity**, and differentiating position
amplifies error by roughly `1/(Δt·√2)` — about 70× at 100 Hz. Two consequences are expected:

- **Paper 2 would become unmeasurable.** Its entire deliverable is a millisecond-scale threshold plus a
  kinematic account, both derivative-based. Filtering trades noise against lag as `√N` versus `~N/2`, so
  usable precision costs hundreds of milliseconds — inside an experiment whose range is 100–800 ms.
- **Paper 1's H1 would acquire a directional confound.** Predictive timing needs closing speed; proximity
  needs only distance. Velocity noise degrades one arm of the comparison and not the other, biasing H1
  *against* prediction and making a null uninterpretable.

If the pilot contradicts this, the pilot wins — that is why it is being measured. If it confirms it, the
argument is backed by data rather than algebra, which is a stronger position for both manuscripts.

**Each system carries a different failure mode, and the pilot must exercise both.** Vicon: marker occlusion,
swapping or mislabelling under fast motion (worse than dropout — confident wrong data rather than a gap), and
physical detachment. Commodity: occlusion-driven dropout, session drift, and motion blur at speed. Paper 1 is
the harder case for either (five sites, room-scale, vigorous dodging); Paper 2 is comparatively easy (one arm,
confined volume, fixed home region) but has the stricter accuracy requirement.

**A useful by-product regardless of outcome:** paired data characterizes commodity-tracker error against a
reference on real task movement — the empirical commodity-versus-reference comparison, obtained free as part
of the selection.

Both studies require:

- tracker-selection criteria recorded and dated **before** the first pilot session;
- both systems implemented and logging simultaneously during the pilot;
- for the selected system: verified accuracy and frame rate in the actual capture volume during vigorous
  movement, plus its own failure-mode piloting (co-registration and marker integrity for Vicon; dropout,
  drift, and a frozen wrist-to-hand offset for commodity trackers);
- limb-volume capsule radii frozen from anthropometry, sized above measured error if commodity trackers win;
- synchronized timestamps and bounded/logged tracking queues;
- physical measurement of haptic onset and jitter;
- calibrated cue detectability and masked tactor sound;
- raw chronological event and trajectory logs;
- participant/session directories with overwrite protection;
- emergency-stop, tracking-loss, and adverse-event procedures;
- immutable analysis code and parameter versions; and
- an approved movement-data privacy plan.

Shared code is allowed. Shared confirmatory data and recycled inferential results are not.

## 8. Publication and disclosure strategy

**IEEE ISMAR 2027's host city and dates have not been announced.** Verified 2026-09-06: `ieeeismar.net` carries
only the 2026 edition (Bari, Italy, 5–9 October 2026), whose abstract and paper deadlines — 9 and 16 March 2026
— have already passed. Earlier "Kobe, October 11–15" and "Lyon, July" claims both trace to *different*
conferences (IEEE/SICE SII 2027 and the International Society of Magnetic Resonance respectively) and must not
be used.

By analogy with 2026, expect an ISMAR 2027 deadline **around March 2027**. Treat that as a provisional planning
assumption only, recheck the official site monthly, and hold a fallback venue in view — the program's gating
constraint is ethics approval and pilot completion, not the calendar.

If both manuscripts are under review concurrently, disclose and provide the related anonymous manuscript/letter as required by the applicable ISMAR author guidelines. The disclosure must state accurately whether cohorts are independent or reused.

Each manuscript must include:

- its own preregistration and power analysis;
- a clear statement of non-overlap;
- the shared apparatus description;
- distinct hypotheses, trials, outcomes, and analyses; and
- appropriately restrained claims.

## 9. Program-wide no-go gates

No study launch until:

1. The relevant ethics protocol is approved.
2. The scientific protocol and analysis are preregistered.
3. Implementation matches the design authority.
4. Physical timing, tracking, haptic calibration, and safety tests pass.
5. Pilots demonstrate valid task behavior and estimable outcomes.
6. Simulation supports sample and trial counts.
7. Data reconstruction and analysis-recovery tests pass end to end.

“Code runs” is not sufficient evidence that a human experiment is ready.

## 10. Plain-language summary

Paper 1 is the **which design works?** study. People play a VR task while different warning systems are compared.

Paper 2 is the **how early must it warn?** study. People perform controlled reaches so the experiment can measure their stopping process and the warning-time response curve accurately.

The first paper finds the better warning strategy. The second explains the timing limits of the human arm. Keeping them as separate experiments makes both conclusions clearer and more defensible.

