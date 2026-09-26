# Paper 1 — Code Gap Analysis

**Compiled:** 2026-09-08 · **Authority:** `PAPER1_STUDY_DESIGN.md` (the design wins wherever this file disagrees)
**Purpose:** what the existing Unity implementation must change to satisfy the revised Paper 1 protocol.
Verified by reading the current source, not from memory.

> **Headline:** the engine is sound but the *measurement layer* is now wrong. The primary outcome moved from
> block-level counts to opportunity-level binary violations, and the Visual benchmark changed from a proximity
> glow on the hazard to a transient, predictively-triggered limb indicator. Those two block confirmatory data
> collection; everything else is smaller.

## ⚠ STATUS REFRESH 2026-09-25 — THIS TABLE WAS STALE

Five rows below said **Yes** (blocks confirmatory data) when the work was already done or was never a
code gap. Verified against source on 2026-09-25:

| # | Old claim | Verified state |
|---|---|---|
| 5 | Contact geometry blocks | Mechanism exists; the **values** are a pilot measurement, not code |
| 6 | Opportunity validity blocks | **Already implemented** — `Presented`/`Valid`/`InvalidReason` all set |
| 8 | §12 outputs block | **Now complete**, including session metadata and checksums |
| 9 | Item-level questionnaire | **Was genuinely open — now closed** (see below) |
| 10 | Haptic onset | Hardware, not code |

### Closed since, with dates

- **2026-09-23** — `Visual` descoped; **`PBC`** added (PB's trigger, continuous cue) carrying **H4'**.
  Continuous sink path, throttle gate, engagement-episode burden accounting.
- **2026-09-23** — Onset-match calibration (`ContinuousOnsetCalibration` + runner) closing the
  sharp-vs-fade-in confound in H4'.
- **2026-09-25** — **Condition was confounded with layout.** Both orders keyed off `participantId`, so
  only 6 of 36 crossing cells were used and 12 condition x layout pairs never occurred. Fixed, verified
  at 12 per cell, guarded by three tests. **This also withdrew N = 48** — legal allocations are multiples
  of 36.
- **2026-09-25** — **Item-level questionnaire export (gap 9).** The panel collected per-item responses
  and discarded them at the callback boundary. Subscale scores cannot be decomposed back into items, so
  every raw response was being lost irreversibly at the moment of collection. `kind` column added.
- **2026-09-25** — **`analysis_manifest.json`** with SHA-256 per file, versions and exclusions.
- **2026-09-25** — **`session.json`.** Paper 1 wrote no session metadata at all; §12 lists it first.
- **2026-09-25** — **E-stop log relocated into the session directory.** It defaulted to
  `persistentDataPath/estop_log.csv`: one file shared by every participant ever run on the machine, with
  no participant column. §12 names participant-root append files an explicit **release blocker**.

**All §12 output requirements are now met.** What remains for Paper 1 is not plumbing: twelve
hand-authored opportunity events and the sternum Tactosy purchase.

---

## Severity summary (original, retained for the record)

| # | Gap | Blocks confirmatory data? | Size |
|---|---|---|---|
| 1 | Opportunity-level primary outcome + export | ✅ **CLOSED 2026-09-21** | Large |
| 2 | Visual benchmark: trigger + form | ✅ **CODE FIXED 2026-09-21** | Medium |
| 3 | Visual is coded as *reactive*; must share PB's predictive trigger | ✅ **FIXED** | Small |
| 4 | Haptic mapping: Tactosy at sternum, vest demoted | ✅ **CODE FIXED**; hardware still to buy/mount | Medium |
| 5 | Contact geometry: anthropometric capsule, frozen | **Yes** | Medium |
| 6 | Opportunity validity tracking | **Yes** | Medium |
| 7 | Dual tracking (paired pilot comparison) | Pilot only | Medium |
| 8 | Data-output requirements (§12) | **Yes** (release blockers named) | Medium |
| 9 | Item-level questionnaire export | Yes | Small |
| 10 | Physical haptic onset measurement | Yes | Small + hardware |
| 11 | Projectile-hit feedback via vest back panel | No | Small |
| 12 | Power simulation targets the wrong model | ✅ **REWRITTEN 2026-09-21** — and H3 needs N=80, see below | Small |

---

## STATUS UPDATE — 2026-09-21 (b): GAP #1 CLOSED

**The engine was already correct; the analysis was not.** Re-reading the source rather than this document:

| Piece | State found |
|---|---|
| `Core/OpportunityOutcome.cs` + formatter | ✅ already existed, 26 columns |
| `Runtime/OpportunityLogWriter` → `opportunities.csv` | ✅ already wired in `SessionRunner` |
| `BlockRunner` emits one outcome per scheduled opportunity | ✅ already did |
| **Attribution on the designated pair** | ✅ `SampleOpenOpportunities` tracks target limb × target hazard inside the window, rising-edge only, repeats collapsed |
| Validity / denominator | ✅ `FinalizeOpportunityValidity` sets `presented`, `valid`, `invalid_reason` |
| **Primary model** | ❌ **this was the gap** — `analysis.R` still fits a negative-binomial GLMM on *block* counts with `offset(log(opportunities))` |

> The stale item in this document was the attribution note. `ActiveFor(limb)` is limb-only, but it feeds the
> **block-level** `_hitOpportunityIds` legacy counter, not the primary outcome. The per-opportunity
> `Violation` never went through it.

**Written:** `Analysis/paper1_analysis.R` — binomial mixed model on opportunity rows,
`violation ~ Policy * Mapping + (1 + Policy + Mapping | participant) + (1 | layout)`, denominator =
`presented & valid`. Includes the gate-27 manipulation check first, the §3 interpretation order (a supported
H3 turns H1/H2 into simple effects), the None floor and H4 contrasts, and alert burden.
`analysis.R` is demoted to the block-level **sensitivity** analysis.

**Written:** `Analysis/paper1_simulate_opportunities.R` — fixture in the real 26-column contract,
verified byte-exact against `OpportunityOutcomeFormatter.HeaderLine`.

### ⚠ What validating it surfaced: H3 is underpowered at N = 36

A recovery study over 8 simulated replicates. The analysis is unbiased — H1 recovers to three decimals —
but the replicate SDs give the achievable precision:

| Effect | true log-odds | SE across replicates | z | approx. power |
|---|---|---|---|---|
| H1 policy | −0.70 | 0.129 | 5.4 | **~100%** |
| H2 mapping | −0.35 | 0.118 | 3.0 | **~84%** |
| **H3 interaction** | **+0.30** | **0.194** | **1.55** | **~34%** |

> **Superseded by a proper power simulation, 2026-09-21.** `power_analysis.R` was rewritten for this
> model and run at nsim = 200. Measured power for H3, by participants × opportunities per block:
>
> | N | 12 opps | 24 opps |
> |---|---|---|
> | 24 | 24% | 42% |
> | 36 | **32%** ← planned | 48% |
> | 48 | 34% | 70% |
> | 60 | 49% | 76% |
> | 80 | 58% | **86%** |
>
> **H3 reaches 80% only at N = 80 with 24 opportunities per block.** H1 clears at N=24×12; H2 at
> N=36×24. Both columns are monotone in N, so the grid is sound.
>
> Doubling opportunities adds 16–36 points at every N — a large and cheap gain, and the opposite of
> what an earlier comment in the script predicted. Within-participant noise dominates first: with 12
> opportunities per cell at a ~50% violation rate, each participant's per-condition estimate is a
> proportion from about eleven trials.
>
> **Minimum detectable interaction at N=36 × 24 opps: OR ≈ 1.57** (90%). An OR of 1.35 — the assumed
> truth — is only 42% detectable there. §3 records two mechanisms predicting opposite signs for H3,
> so this limit belongs in the preregistration.

**This matters more than it looks, because §3 makes H3 govern the interpretation of H1 and H2.** A
34%-powered gate on the two main effects means the most likely outcome is an inconclusive H3 and main
effects reported without knowing whether they should have been simple effects.

Options, for the PI: accept and preregister H3 as underpowered and exploratory; raise N; or raise
opportunities per block. Interactions need roughly 4× the N of a main effect of the same size, so a
modest bump will not fix it.

**This is gap #12's job** — the power simulation must target *this* model, not the negative-binomial one,
and must report power for the interaction separately. Assumed effect sizes above are illustrative and need
replacing with pilot estimates.

---

## STATUS UPDATE — 2026-09-21: H2 and H4 confounds closed in code

Gaps 2, 3 and 4 were the two confounds that made **H2 and H4 uninterpretable**. All three are now fixed in
code, with tests. 222 EditMode tests pass.

| What was wrong | Why it mattered | Fix |
|---|---|---|
| `BHapticsTactorMap` routed `Chest` to `VestFront`, and its **default branch also fell back to the vest** | "Localized vs generic" was bundled with "different device, driver, mounting, salience". H2 would have been a device × site effect, forcing the weaker construct name through the title, hypotheses and abstract | `Chest` → `TactosyTorso`; vest unreachable from `For()`; unmapped sites **throw** instead of falling back; `CarriesWarningCue()` exposed so the sink can assert it |
| `VisualObstacleAlert` glowed the **hazard**, proximity-graded | Visual got hazard location *and* a continuous distance readout that the haptic conditions never provide. A Visual win would have shown "revealing hazards helps" — not a modality result | New `Runtime/LimbCueIndicator`: transient, limb-anchored, duration-matched. Old component marked superseded |
| Visual used the reactive trigger | Confounded H4's modality contrast with timing | Already fixed in `ConditionManager`; now guarded by a test asserting Visual == PB |

**The type system now carries the H4 guarantee.** `LimbCueVisual.Intensity()` takes *time since onset only* —
no distance, no hazard, no position argument. It is not able to leak proximity. Adding such a parameter
re-breaks H4, which is why the signature is the guard rather than a comment.

**Still required for H2, outside code:** five Tactosy units, one mounted at the sternum, and the pilot
**confusability check** — the generic warning cue and the projectile-hit vest feedback are both on the torso
and must be reliably distinguishable (§2). If they are not, drop vest haptics and deliver projectile feedback
through audio and visuals only.

---

## 1. Opportunity-level primary outcome — BLOCKING

**Design (§5, §9).** The unit is the **opportunity**, not the block. `violation = 1` only when the *designated
target limb* enters the *designated target hazard* inside that opportunity's valid attribution window; repeats
within one opportunity stay a single event. The denominator counts only opportunities that were presented and
met validity requirements — it cannot be fixed at 12. Primary model is **binomial/logistic** with
`(1 + Policy + Mapping | participant)`.

**Code today.** `BlockRunner` accumulates block totals into `BlockResult` (`Collisions`, `OpportunitiesHit`, …);
`CsvFormatter` writes one row per block. `_hitOpportunityIds` is a `HashSet<string>` — it knows *which*
opportunities were hit but discards limb/hazard/timing detail and never emits a per-opportunity row. Attribution
uses `ActiveFor(limb)`, which matches *any* open opportunity for that limb, **not specifically the designated
target hazard**.

**Change.** New `Core/OpportunityOutcome.cs` (id, block, condition, layout, target limb, target hazard, event
type, planned open/close, presented, valid, invalid-reason, violation 0/1, entry episodes, max depth, min
clearance, first-entry time) plus a formatter and writer; `BlockRunner` records one per scheduled opportunity;
`SessionRunner` writes `opportunities.csv`. Attribution must check limb **and** hazard id. Block counts stay as
the sensitivity analysis. `analysis.R`'s primary model becomes a binomial mixed model on those rows.

## 2. Visual benchmark — wrong form — BLOCKING

**Design (§2).** Transient, duration-matched to the haptic pulse train, indicates **the at-risk limb**, does not
persistently reveal hazard geometry, on-body/avatar-anchored. The design names our component directly: *"The
existing component cannot be used for the Visual benchmark without being rewritten."*

**Code today.** `Runtime/VisualObstacleAlert.cs` renders a **continuous, proximity-graded glow on the hazard
volume** with a distance-driven pulse rate (`Core/VisualAlertModel`). Opposite on both counts.

**Change.** A limb-anchored transient indicator fired from the same `FeedbackCommand` stream as the haptic cue
(`Modality.Visual` already exists), duration-matched to 3×100 ms + 60 ms gaps. `VisualAlertModel`'s
distance→intensity grading no longer applies to the benchmark. Keep the hazard-hiding behaviour of the current
component — that part is still required.

## 3. Visual fires on the reactive rule — BLOCKING (small fix)

**Design.** Visual must use "the same predictive trigger, eligibility, and nominal onset as PB" so H4 is a clean
modality contrast.

**Code today.** `Core/ConditionManager.cs:75` — `case Condition.Visual: trigger = TriggerKind.Reactive;`, firing
on `MinDistance < ReactiveDistance`. `CLAUDE.md` also documents Visual as reactive.

**Change.** Move `Condition.Visual` into the predictive branch; update `CLAUDE.md` and the condition-rule
comments. A small edit — but it silently invalidates H4 until done.

## 4. Haptic mapping — Tactosy at sternum — BLOCKING

**Design (§2, decided 2026-09-07).** Five **Tactosy** units carry the warning cue (sternum, both forearms, both
feet). The **TactSuit vest never delivers a warning** — it is retained for projectile-hit feedback only, on
back-panel regions away from the sternum.

**Code today.** `Core/BHapticsTactorMap.cs:39` maps `HapticSite.Chest → BHapticsDevice.VestFront` with six motor
indices; `HapticDeviceBinding.MotorCountFor` returns 40 for vest positions.

**Change.** Chest routes to a Tactosy-class device at 3 motors; add a separate vest path used *only* for
projectile-hit feedback. This makes the E2 salience calibration load-bearing rather than optional (a sternum
unit is not coupled to the body like a strapped forearm unit) and requires a forced-choice discrimination check
in the cue tour so the generic warning is never confused with vest hit-feedback. **Procurement: five Tactosy
units.**

## 5. Contact geometry — capsule model — BLOCKING

**Design (§5).** *"The point-joint default is not acceptable for confirmatory analysis."* Frozen per-segment
capsule radii from anthropometry, recorded with their source; if commodity trackers win the pilot, sized so the
effective band comfortably exceeds measured 95th-percentile tracking error.

**Code today.** `CollisionDetector` computes point-to-box distance; `DetectorParams.LimbContactRadius` exists but
defaults to null/0 (off) and is a scalar reach, not a capsule.

**Change.** Populate per-limb radii from anthropometric data with the citation recorded; make them mandatory for
confirmatory runs (fail loudly if unset); decide capsule-vs-sphere and document. The existing mechanism is the
right hook — it just has to be filled in and made non-optional.

## 6. Opportunity validity — BLOCKING

**Design (§5).** Every opportunity carries validity requirements; "early termination, tracking loss, unspawned
stimuli, or operator stops cannot leave the denominator fixed at 12."

**Code today.** `OpportunityScheduler` opens and closes on time only. Nothing records whether the stimulus
actually spawned, whether tracking was valid during the window, or why an opportunity was dropped.

**Change.** Validity fields on the opportunity record, fed by spawner confirmation, tracking-quality flags, and
session state (abort / e-stop). Preregister the rules before collection.

## 7. Dual tracking for the paired pilot

**Design (§11, revised 2026-09-08).** Vicon **and** commodity trackers logged **simultaneously** on the same
pilot participants; selection per paper against criteria fixed and dated beforehand.

**Code today.** `BodyTrackerRig` holds one set of six Transforms. `ViconBodyRig`, `ControllerTrackerStandIn`, and
`TrackerKeypointSource` each *overwrite* those slots — they are alternatives, not parallel sources.

**Change.** A dual-source logger writing both streams with a **source-identity column**, plus a comparison report
(paired error, dropout, velocity noise, delivered lead-time jitter). Useful by-product either way: commodity-vs-
reference error measured on real task movement.

## 8. Data-output requirements (§12)

Named **release blockers**: repeated CSV headers, unordered event groups, silent malformed-frame loss,
participant-root append files. Also required: **sequence numbers, source identity, tracking-quality flags** on
frames; a **chronologically ordered** event stream including TTC and **physical** cue timing; checksums.

**Code today.** `KeypointLogFormatter` writes `participant,block,time_s,<joints>` — no sequence, source, or
quality fields. `EventLogWriter.WriteBlock` writes alerts, outcomes, and opportunities as **grouped sections**,
not one chronological stream. `estop_log.csv` and `cue_intensity.csv` sit at the participant root and are
appended.

**Change.** Extend the keypoint schema; merge the event stream into a single time-ordered log; move session-level
logs under the session directory; add checksums.

## 9. Item-level questionnaire export

**Design (§12).** "questionnaire item-level **and** scored data."

**Code today.** `QuestionnaireFormatter.Rows(...)` takes the *scored measures* dictionary only; raw item
responses are discarded after `Questionnaire.Score()`.

**Change.** Emit raw per-item rows alongside scored measures — `QuestionnairePanel` already holds `_resp`.

## 10. Physical haptic onset

**Design (§11).** "Measure physical haptic onset; software command time alone is insufficient." Delivered
lead-time jitter is a named tracker-selection criterion.

**Code today.** `pipelineLatencySeconds` is an unset Inspector field; nothing measures actual onset.

**Change.** Bench measurement (accelerometer or microphone on a tactor) plus a logged per-cue
commanded-vs-physical delta. Hardware task, small code.

## 11. Projectile-hit feedback (vest, back panel)

Not implemented at all — `CheckInteractions` counts hits but fires no feedback. The design wants vest haptics
away from the sternum, with an audio/visual-only fallback if the discrimination check is marginal.

## 12. Power simulation targets the wrong model

`power_analysis.R` simulates the **NB count** model. §4 requires simulation of the opportunity-level model with
participant random slopes, event type, target limb, period, layout, and invalid opportunities. Provisional
planning figure is **36 analyzable / 48 maximum** (allocation-driven: §7 needs a complete 6×6 crossing, so the
natural complete allocations are 36 and 72).

---

## What survives unchanged

Virtual hazards (validated); layout isometries L1–L6 + LP; the deferred −3 penalty (explicitly endorsed as
motivational, and correctly excluded from analysis because it is algebraically dependent on the primary
outcome); the cue tour; practice = None; Williams counterbalancing; the Core/Runtime/Integration seam
architecture; `TrackingNoise` + `WarningFidelity` (§11 notes the robustness analysis is "restored to its original
form"); task scoring reported via its components only, as an engagement measure.

## Suggested order

1. **Gap 3** — one-line trigger fix; cheapest, and silently invalidates H4 until done.
2. **Gaps 1 + 6** together — the opportunity record is the same data structure.
3. **Gap 2** — Visual rewrite.
4. **Gaps 4 + 11** — haptic re-mapping, alongside Tactosy procurement.
5. **Gap 5** — anthropometric radii (needs a decision and a citation, not just code).
6. **Gaps 8 + 9** — logging schema.
7. **Gaps 7 + 10 + 12** — pilot infrastructure and power, before the ethics submission.

None of this is startable as *confirmatory* work until ethics approval exists; gaps 1–6 are the prerequisites for
a valid pilot.

## Relationship to the parent grant (`project description_vr_safety.pdf`)

Paper 1 is **Aim 3** of the funded proposal ("Ascertain the optimal modality and locations of real obstacle
alerts"). The study has evolved substantially from the grant text, and the deviations should be conscious:

| Grant Aim 3 | Paper 1 now |
|---|---|
| 5 conditions: Chaperone baseline, SafeXR, Embodied Visual, Embodied Tactile, Embodied Audio | 6 conditions: 2×2 policy × mapping + None + Visual benchmark |
| Modality comparison (visual / audio / **tactile**) | Timing × localization within the haptic modality; **audio dropped** |
| **Two real physical objects** in the room; "reduce injury" | **Virtual hazard volumes**; injury/safety claims explicitly forbidden |
| 60 participants | 36 analyzable / 48 max |
| 6–10 Kinect cameras, HTC Vive Pro Eye | Vicon vs commodity trackers (pilot-decided), VIVE Focus Vision |

Two consequences worth noting. First, **Aim 4** (the in-home longitudinal study) is specified to deploy "the
most effective safety approach identified in Aim 3", so Paper 1's ranking output (§15) is the input to a later
aim — the lexicographic ranking rule matters beyond this paper. Second, **Aims 1–2** (multi-camera obstacle
detection and AI motion prediction) are the real system that Paper 1's idealized oracle stands in for; that
makes the upper-bound framing coherent within the program rather than a limitation invented after the fact.
The grant's promise of *real* obstacles and injury reduction remains unfulfilled by Paper 1 alone and should be
tracked as a program-level obligation.
