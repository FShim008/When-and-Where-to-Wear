# Paper 1 — Readiness Assessment

**"What Makes a Collision Warning Work? Separating Trigger Timing, Body Site, and Cue Form in VR"**
Assessed 2026-09-23, after implementing the recommended fixes.

> **Project-wide open questions (both papers, hardware, ethics, venue): `docs/OPEN_QUESTIONS.md`.**

**Verdict: the DESIGN is ready. The STUDY is not, and one scientific gap in H4′ is new and must be
closed at pilot.** Details below, worst news first.

---

## 1. The new problem: PB and PBC are not perceptually matched at onset

**This is a genuine confound in H4′ and it was not in the original recommendation.** It surfaced while
writing `ContinuousCueTests`.

PB delivers a **sharp-onset** pulse train at calibrated amplitude. PBC **fades in** from
`ContinuousCueMinIntensity` (currently 0.15) and ramps toward 1.0. A sharp onset is more detectable than a
gradual one — this is elementary psychophysics, not a subtle effect.

So if PBC produces more violations, there are two explanations:

1. **The hypothesis:** continuous mapping lets participants modulate the cue by slowing down.
2. **The artefact:** PBC's onset is simply harder to notice, so its *effective* warning arrives later.

**Explanation 2 is the same class of confound H4′ exists to remove.** Shipping it would be self-defeating.

**Mitigation already in the code.** `ContinuousCueMinIntensity` exists precisely so the cue steps to a
perceptible level the instant it engages rather than fading from silence — the same reason Valkov and
Linsen's f4/f5 introduce a discontinuity at `d_min`. `ContinuousCueTests` asserts the floor is applied.

**BUILT 2026-09-23 — `ContinuousOnsetCalibration` + `OnsetMatchCalibrationRunner`.**

Each trial presents **PB's onset** (the 3-pulse train at the site's E1-calibrated drive) and **PBC's onset**
(a single held step at the candidate floor, *no ramp*) in randomised order. The participant says which felt
stronger; a PSE staircase converges on the floor where they match.

**Why magnitude matching rather than reaction time.** The natural target is detection *latency*, but RT
matching is noisy, needs many trials and has no standard adaptive rule. Perceived magnitude **at onset** is
a direct proxy here, because the confound *is* that PBC's first moment is weaker than PB's. If the first
moments feel equal, the effective onsets coincide — which is the claim H4′ needs.

**The ramp is deliberately excluded from the test stimulus.** Including it would let the later, stronger
part of the cue carry the judgment, and the participant would be matching the whole approach instead of its
onset — the one moment that decides whether the warning effectively arrives on time.

**Two failure modes, both surfaced rather than averaged away:**

| Signal | Meaning | What H4′ may then claim |
|---|---|---|
| `PinnedAtBound` | PBC's onset could not be matched at **any** drive | **Not** that cue form was isolated — report the onset caveat |
| `Spread` > 0.10 | Sites disagree, so the E1 gains did not equalise onset salience | A single frozen floor is the wrong model; use per-site floors |

Seven tests cover it, including recovery of a **known** match point from either side (a staircase that only
converged from below would bias the floor in whichever direction the default happened to sit), and a check
that the calibrated value is exactly what `ContinuousCueMapping` delivers at onset — otherwise the
calibration is decorative and the confound returns silently.

**Run it once per participant, after E1 and before any H4′ data.**

**Report `BlockRunner.CueDoseSeconds` regardless.** PB and PBC differ in total delivered vibration by
construction (~0.3 s vs ~1 s per opportunity). That difference is part of what "cue form" means and is not
itself a confound — but it must be a measured number in the paper, not an unstated asymmetry.

---

## 1B. Implementation status — 2026-09-25

**Unity 6000.3.16f1 EditMode: 298 tests, 297 pass, 1 skipped, 0 compile errors.**
86 source files, 41 test files, 8 R scripts.

**All §12 output requirements are met.** Added since the first assessment: item-level questionnaire
export, `analysis_manifest.json` with SHA-256, `session.json`, and the e-stop log moved out of the
participant root (§12's explicit release blocker).

**Paper 1 is roughly 92% of code.** The remaining 8% is not plumbing — it is twelve hand-authored
opportunity events, which need design judgment about orb placement, plus the `TactosyTorso` mapping
once the hardware arrives.

For the project-wide picture including Paper 2, see `docs/OPEN_QUESTIONS.md` §6B.

## 2. What is genuinely ready

| | Status | Evidence |
|---|---|---|
| Hypothesis structure | ✅ | Confirmatory H1/H2/H4′ under Holm; H3 exploratory and de-gated |
| Novelty claim | ✅ | Rewritten post-Valkov; the false "no prior work" claim is gone |
| Related work | ✅ | `docs/PAPER1_RELATED_WORK.md`, ~950 words, 12 Crossref-verified citations |
| Parameter defense | ✅ | §3B sweep, §6B dose-response, derivation table, published anchors |
| Core engine | ✅ | **Unity 6000.3.16f1: 246 tests, 245 pass, 0 compile errors.** 22 new for PBC, cue-form separation and onset matching |
| Analysis pipeline | ✅ | Runs end-to-end; recovers H4′ ground truth (OR 0.696 vs true 0.670) |
| Alert burden across cue forms | ✅ | `AlertEpisodes` + `CueDoseSeconds`; frame-counting bug prevented by test |
| Safety protocol | ✅ | PBC vibration-exposure section added (§ Devices) |

**The design-level reviewer questions are closed.** *"Where did 0.30 m and 1.0 s come from?"* — §3B, §4,
published ranges. *"Valkov & Linsen found the opposite"* — H4′ is built to answer exactly that.
*"Your interaction is underpowered"* — declared exploratory, with its MDE.

---

## 2B. NEW 2026-09-25 — condition was confounded with layout

Found while drafting the Method section. `SessionPlan.For` derived both the condition order and the layout
order from `participantId`, so they were perfectly correlated: **6 distinct plans instead of 36**, and
**12 of 36 condition × layout cells never used**. A layout-difficulty effect would have loaded onto the
condition estimates and biased all three confirmatory hypotheses.

**Fixed** (layout row indexes on `participantId / 6`), **re-measured** at 12 per cell across all 36, and
**guarded** by three tests. Unity: 249 tests, 248 pass, 0 compile errors.

**The consequence is that N = 48 is withdrawn.** Complete allocations are multiples of 36. See Q9b in
`OPEN_QUESTIONS.md`.

## 3. What blocks the real study

| # | Blocker | Why it blocks | Owner |
|---|---|---|---|
| 1 | **Latency bench not run** | `PipelineLatencySeconds = 0`. The predictive threshold does not compensate for an unmeasured motion→tactor delay, so the *delivered* lead is not the *nominal* lead. Affects H1, H4′ and the §6B curve. **Also carries the PBC queue/replace check (§3B).** | Lab, needs the tactor |
| 2 | **PBC hardware path** | **CODE COMPLETE 2026-09-23**, hardware-unverified. `HapticDeviceBinding.CreateStudySink` supplies the continuous action; `SessionRunner`, `LiveSessionController` and `SessionController` all use it. Compiles clean in Unity. **One open question needs the tactor** — see below. | Lab |
| ~~3~~ | ~~Runtime compile unverified~~ | **CLOSED 2026-09-23.** Unity 6000.3.16f1 batch mode, EditMode: **239 tests, 238 passed, 0 failed, 1 skipped**, **0 compile errors**. Runtime + Integration (including the modified `BHapticsSink`) compile clean. | ✅ |
| ~~4~~ | ~~Power run incomplete~~ | **CLOSED 2026-09-23.** Full 15-cell grid + 5-level MDE at nsim=120. Design set: **N = 48 x 18**. | ✅ |
| 5 | **PBC parameters unfrozen** | **Procedure built 2026-09-23** (`ContinuousOnsetCalibration` + `OnsetMatchCalibrationRunner`, 7 tests). `ContinuousCueMinIntensity = 0.15` is still an unmeasured default until the calibration is *run*; `ContinuousCueGamma = 2.0` is Valkov & Linsen's f3 and stays fixed. | Pilot — run it |
| 6 | **IRB** | ⏳ **With the professor, awaiting delivery.** Blocking until in hand. Check its scope covers: new PBC condition, ~3× vibration exposure in that block, consent element 3 wording, and the new pre-session onset calibration. An approval predating 2026-09-23 cannot mention PBC — an amendment would still be needed. | PI |
| 7 | **Session duration unknown** | Documented nowhere. Cannot schedule, cannot assess fatigue, cannot justify the opportunity count. Now sharper: N=48×18 means **108 opportunities per participant against the current 72**, with the block growing 180 s → ~240 s. | Pilot — **measure it** |
| 8 | **Schedules supply 12, design requires 18** | An 18-event schedule passing the audit **18/18** exists (`C:\cc\Candidate18.cs`); six new **orb positions** per layout still need authoring, then a re-audit including orb→hazard clearance. `AssertMeetsTarget()` throws until then. | Storyboard |
| ~~9~~ | ~~O2 re-site unverified in Unity~~ | **CLOSED 2026-09-23.** Opened and visually confirmed: O2 at (0.95, 0.70, 0.60) under the root `Obstacles` object. | ✅ |
| 10 | **`TactosyTorso` unmapped** | `PositionFor` throws — the sternum unit is not bought or paired. **RG and PG cannot run.** | PI (purchase) |

---

## 3B. The one PBC question only the hardware can answer

`ContinuousCueGate` submits `BhapticsLibrary.PlayMotors` at a throttled ~22 Hz with a 60 ms duration, so
consecutive submissions overlap rather than gap. **It is unverified whether the SDK REPLACES an in-flight
command on the same position or QUEUES behind it.**

- **If replace** (assumed): the level tracks TTC smoothly and stops within 60 ms of disengagement. Correct.
- **If queue**: the overlap accumulates across an approach, the cue lags the participant's motion, and H4′
  measures a delayed stimulus — while every log still looks right.

**Verify at the latency bench**, which is already going to the lab: drive a slow approach, watch the tactor,
confirm the level rises with proximity and *stops* within ~60 ms of the limb disengaging. If it lingers or
lags, the cue is queueing and `continuousIntervalMillis` must exceed `continuousMillis` instead.

**The throttle is measured, not assumed.** `SessionRunner` logs submitted/throttled counts and the submit
rate after every PBC block, and warns below 25%. If the rate is low, the ramp that reached the skin was
steppier than `ContinuousCueMapping` describes, and H4′ is testing a coarser cue than the paper claims —
which is reportable, but only if it is known.

## 4. Power — full grid, `power_analysis.R` nsim = 120, completed 2026-09-23

| N | opps | H1 | H2 | H3 | H4′ |
|---|---|---|---|---|---|
| 36 | 12 | 99% | 68% | 30% | 79% |
| 36 | 16 | 100% | 74% | 30% | 88% |
| 36 | 18 | 100% | 73% | 48% | 95% |
| 36 | 20 | 100% | 82% | 43% | 93% |
| 36 | 24 | 100% | 86% | 48% | 99% |
| 48 | 12 | 99% | 74% | 37% | 88% |
| 48 | 16 | 100% | 89% | 48% | 96% |
| **48** | **18** | **100%** | **93%** | 57% | **96%** |
| 48 | 20 | 100% | 93% | 57% | 97% |
| 48 | 24 | 100% | 98% | 67% | 100% |
| 60 | 12 | 100% | 84% | 40% | 94% |
| 60 | 16 | 100% | 94% | 56% | 98% |
| 60 | 18 | 100% | 94% | 63% | 100% |
| 60 | 20 | 100% | 97% | 63% | 100% |
| 60 | 24 | 100% | 98% | **76%** | 100% |

**80% is reached at:** H1 — N=36×12 · H2 — N=36×20 · H4′ — N=36×16 · **H3 — nowhere in the grid.**

### ►► SET 2026-09-23: N = 48 analysable × 18 opportunities per block ◄◄

All three confirmatory hypotheses clear with margin (H1 100%, H2 93%, H4′ 96%). H3 lands at 57% and is
reported as exploratory with its MDE. Sizing is on **H2, the weakest confirmatory test** — H3 constrains
nothing.

**Recruit to 54, analyse the first 48 complete balanced sets.** 48 is divisible by 6 so the Williams square
balances exactly; 54 is not. Preregister that rule rather than deciding it after seeing dropout.

Rejected: **N = 36 × 20** (H2 82% — clears by two points, no room for the Holm caveat below);
**N = 48 × 24** (H2 98%, but 144 opportunities per participant, twice the current session).

Recorded in `PAPER1_STUDY_DESIGN.md` §4, `OpportunitySchedules.TargetOpportunitiesPerBlock`,
`power_analysis.R`, and the fixture defaults.

> **⚠ The engine cannot deliver 18 yet — this is now blocker 8.** `OpportunitySchedules.Layout1()` authors
> **12** events on a **180 s** block. Six more per layout are needed, across six layouts, and it is not a
> constant change: at ~13 s spacing 18 events need **234 s**, so the block grows to roughly 240 s; obstacle
> balance must be preserved (O2×6 · O3×6 · O1×3 · O4×3); every new event needs ≥ 0.40 m clear approach; and the
> geometry audit must re-run over all 18. **O2 was already re-sited on 2026-09-14** and the current 12 pass
> **12/12** (approach: O2 0.62 m · O3 0.78 m · O1 1.05 m · O4 1.70 m). O2 remains the tightest hazard — below
> the floor at 22% of standing positions in a ±0.6 m sweep — so prefer O3/O1 for the added events.
>
> `OpportunitySchedules.AssertMeetsTarget()` throws until this lands, and
> `ContinuousCueTests.Layout1_does_not_yet_meet_the_preregistered_opportunity_target` pins the gap so it
> cannot be forgotten. **The mismatch is deliberately loud**: the analysis counts the rows it is given
> (CODE_GAP #1), so a 12-event block would look like clean data while the preregistration said 18.

### Holm-corrected — MEASURED 2026-09-23, nsim = 200

| N × opps | H1 raw→Holm | H2 raw→Holm | H4′ raw→Holm | all three |
|---|---|---|---|---|
| **48 × 18** | 100→100 | **88.5→87.5** | 97.0→97.0 | **85.5%** |
| 36 × 20 | 100→100 | **80.0→79.0** ❌ | 95.0→93.5 | 74.5% |
| 48 × 24 | 100→100 | 97.0→97.0 | 97.0→97.0 | 94.0% |
| 60 × 18 | 100→100 | 98.5→98.5 | 98.5→98.5 | 97.0% |

**The correction costs H2 about one point and the design holds at 87.5%.** It also settles the
alternative: **N = 36 × 20 lands at 79.0% — below threshold.** It was rejected on the argument that it
cleared H2 by only two points with no room for the correction; that is now measured.

**"All three" (85.5%) is the probability H1, H2 and H4′ are all supported in the same study** — the
number that describes the paper landing, which no per-test figure shows.

### ⚠ The grid tables above are uncorrected per-test powers

The analysis applies **Holm** across the three confirmatory hypotheses (`paper1_analysis.R` §6A); the power
script does not simulate it. Holm is step-down, so the practical cost is small — H1 will almost always be
the smallest p-value and H4′ the second, leaving H2 tested at the full α = 0.05. But that is **conditional
on H1 and H4′ both passing**; if H4′ fails, H2 faces α/2. Read the H2 column as slightly optimistic and pick
a cell with margin. If H2 becomes decision-critical, simulate Holm directly.

### Three things the grid settles

1. **H3 is unrescuable.** It does not respond to either axis in the affordable range — 30% → 48% across all
   of N=36, and 76% at N=60×24 is the ceiling. This is a **stronger** argument for de-gating it than the
   convenience argument: no design you can run fixes it.
2. **Opportunities are the cheap axis.** 12 → 20 at N=36 moves H2 by 14 points and H4′ by 14. Going
   N=36 → 60 at 12 opportunities moves H2 by 16 and costs **24 whole sessions**.
3. **H4′ is comfortably powerable**, which the single dry-run draw (p = 0.18) had made look doubtful. One
   draw is not a power estimate.

### What size of interaction H3 can resolve

`power_analysis.R` §6, nsim = 30, 24 opportunities/block. **This is what the preregistration should say
about H3** — not "we expect an interaction" but "this design can rule out interactions smaller than X."

| Interaction | OR | N=36 | N=48 | N=60 | N=80 |
|---|---|---|---|---|---|
| +0.20 | 1.22 | 37% | 13% | 27% | 57% |
| +0.30 | 1.35 | 43% | 57% | **83%** | 83% |
| +0.45 | 1.57 | **93%** | **93%** | 100% | 97% |
| +0.60 | 1.82 | 100% | 100% | 100% | 100% |
| +0.80 | 2.23 | 100% | 100% | 100% | 100% |

**The design resolves an interaction of OR ≈ 1.57 or larger at any N in the grid. OR 1.35 — the value
assumed in §1 of the power script — needs N = 60. OR 1.22 is out of reach entirely.**

That is the whole story of H3 in one line: the effect we think is there is roughly the smallest one the
study cannot reliably find.

> **⚠ The +0.20 row is noise, not a result.** Power cannot fall as N rises, so 37/13/27/57 is sampling
> error — at nsim = 30 the standard error is about 9 points. Quote it as "undetectable at any N tested"
> and never cite its individual cells. The 93/93/100/97 non-monotonicity in the +0.45 row is the same
> thing, smaller. If any of these rows becomes load-bearing for a reviewer's question, re-run §6 at a
> higher nsim rather than defending the numbers as they stand.

### But do not freeze N from this table

Two independent reasons:

- **Every effect size in §1 of the power script is an assumption**, including `EFF_PBC = 0.40` which is a
  deliberately modest guess at an effect Valkov and Linsen measured on a different task with a different
  outcome. Re-run from pilot estimates.
- **The session-length ceiling is unmeasured** (blocker 7). N=48×18 means 108 opportunities per participant
  against the current 72 — 50% more task time. Whether that fits is the single most important number the
  pilot must return.

---

## 5. Can the pilot start?

**Yes for the five discrete conditions** (None, RG, RB, PG, PB) as protocol development on self or trained
lab staff — no naive participants before IRB. That pilot should return:

1. **Block and session duration** — blocker 7, and it decides the opportunity count
2. **Real reach speeds and approach distances** — validates the `MinApproachDistanceForValidTiming = 0.40 m`
   figure, which is currently simulation-derived
3. **Pilot effect sizes** → re-run `power_analysis.R` → freeze N
4. **Opportunity geometry** — confirm the O2 fix holds with real bodies

**No for PBC** until blockers 2, 3 and 5 clear. It cannot execute (the sink throws), and running it before
the onset-matching calibration in §1 would produce data that cannot support the H4′ claim.

---

## 6. Honest publication assessment

**The paper now has a defensible contribution it did not have a week ago.** "Resolving a contrary in-venue
result whose authors identified the likely confound themselves" is a stronger and more reviewable position
than "first comparison," and it survives either outcome.

**The remaining risk is execution, not design.** Specifically:

- If the **latency bench** shows a large or variable pipeline delay, the delivered leads differ from nominal
  and H1's interpretation narrows. §6B was built for exactly this case.
- If **onset matching fails** (§1), H4′ degrades from a clean isolation to a confounded comparison. It would
  still be reportable, but the headline claim weakens substantially.
- If the **pilot shows sessions run long**, the opportunity count must drop, and the §4 table says that
  costs H2 and H4′ directly.

**None of these is a design flaw. All three are measurements not yet taken.** That is the correct state for
a study about to pilot — and it is the difference between "not ready" and "not finished."
