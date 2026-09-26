# NEXT STEPS — live working document

**Purpose:** the single running list of what to do next. Rewritten whenever you ask for next steps.
**Last updated:** 2026-09-23
**Design authority:** `PAPER1_STUDY_DESIGN.md` · `PAPER2_STUDY_DESIGN.md` · `COMBINED_STUDY_DESIGN.md`
**Open questions (all of them, both papers):** `docs/OPEN_QUESTIONS.md`
**Paper 1 readiness detail:** `docs/PAPER1_READINESS.md`

> **Current goal: run a protocol-development pilot on yourself.** No naive participants until the IRB is
> in hand. "Code runs" is not evidence that a human experiment is ready.

---

## ✅ Done 2026-09-23

- **Step 1b — O2 verified in the Unity scene.** `Dryrun.unity` was edited on disk on 2026-09-14 with the
  Editor closed and had never been opened since. Now visually confirmed at (0.95, 0.70, 0.60), nested under
  the root `Obstacles` GameObject alongside O1 and O3. (O4a/O4b are spawned in code by `SceneObstacles` at
  `Awake()` and correctly do not appear in the Hierarchy at edit time.)
- **Step 1c — `protocolDevelopmentMode` enabled** on `SessionRunner`.

---

## STEP 1a — Order the sternum Tactosy ⬅ START THIS, IT HAS A LEAD TIME

A **bHaptics Tactosy for arms** (3-motor unit), worn on the sternum.

**Why it blocks:** `HapticDeviceBinding.PositionFor` throws `NotImplementedException` for `TactosyTorso`
because the unit is not bought or paired. **RG and PG — both generic conditions — cannot run at all**, so
H2 cannot be piloted. The throw is deliberate: the alternative was falling back to the vest, which is the
H2 device confound this design exists to avoid.

You can pilot the other four conditions meanwhile.

- [ ] Ordered — date: ____________
- [ ] Arrived and paired
- [ ] `PositionFor(TactosyTorso)` mapping filled in (tell me the `PositionType`; one line)

---

## STEP 2 — Latency bench ⬅ THE BIGGEST UNKNOWN IN EITHER PAPER

**Needs:** one tactor, a phone that shoots **240 fps** slow-motion.

Right now `pipelineLatencySeconds = 0`, which means the predictive cue arrives late by an unmeasured
amount. Every lead time in both papers is currently nominal, not measured.

1. Open the bench scene, set `LatencyBenchRunner.mode = FullCharacterisation` (needs ≥ 100 activations)
2. **Position the tactor in frame next to the monitor** so the screen flash and the tactor are in one shot
3. Film at 240 fps and run the bench
4. Count frames from **screen flash** to **first visible tactor movement**. At 240 fps one frame = **4.17 ms**
5. Enter your monitor's display latency in `displayLatencySeconds` — skip this and you over-report by 10–20 ms

**Record:**

| Value | Result |
|---|---|
| Mean latency | ________ ms |
| SD | ________ ms |
| 95th percentile | ________ ms |
| Drop rate | ________ % |
| Monitor display latency used | ________ ms |

- [ ] `SessionRunner.pipelineLatencySeconds` set to the measured mean
- [ ] Same value carried into Paper 2's runner

---

## STEP 3 — The PBC question (~10 min, same lab visit)

`ContinuousCueGate` assumes `BhapticsLibrary.PlayMotors` **replaces** an in-flight command on the same
position. If it **queues**, the overlap accumulates across an approach, the cue lags the participant's
motion, and H4' measures a delayed stimulus — while every log still looks correct.

Put `SessionRunner` in a **PBC** block and move a tracked hand slowly toward an obstacle.

| What you feel | Verdict |
|---|---|
| Buzz strengthens as you close, **stops within ~0.06 s** of pulling away | ✅ replace — assumption holds |
| Buzz **lags** your motion, or keeps going after you pull away | ❌ queue — tell me, the timing needs inverting |

Then read the console after the block:

```
[SessionRunner] PBC cue delivery: N submitted, M throttled (X% reached the device)
```

- Submit rate: ________ %
- **If below 25%**, the ramp is steppier than designed — say so and `continuousIntervalMillis` comes down.

---

## STEP 4 — One full session on yourself

`protocolDevelopmentMode` is on, so the 12-vs-18 opportunity gate is a warning and every session folder
gets a `PROTOCOL_DEVELOPMENT.txt`. **This data is not analysable as confirmatory** — its job is to produce
the numbers below.

**Start a stopwatch when the headset goes on. Stop it when it comes off.**

| Value | Result |
|---|---|
| **Total session time** ⬅ documented nowhere; the most valuable number here | ________ min |
| Per-block time | ________ s |
| Break time actually taken | ________ min |
| Violation rate per block (want ~30–70%, not 0 or 100) | ________ % |
| Any block that felt fatiguing / confusing | |

---

## STEP 5 — Onset-match calibration on yourself (~15 min)

Closes the one confound that would quietly undermine H4': PB has a **sharp** onset, PBC **fades in**, and
a sharp onset is inherently more detectable. Without this, PBC's *effective* warning is later than PB's
and cue form is confounded with timing.

Add `OnsetMatchCalibrationRunner` to a GameObject. **SPACE** to begin, then **1** or **2** for which of the
two buzzes felt stronger. **R** replays, **Esc** aborts.

| Value | Result |
|---|---|
| Pooled floor (→ `ContinuousCueMinIntensity`) | ________ |
| Spread across sites | ________ |
| Any site `pinned at bound`? | ________ |

**Two outcomes change what H4' may claim — report either immediately:**
- **Pinned at bound** → PBC's onset cannot be matched at any drive → H4' must not claim cue form was isolated
- **Spread > 0.10** → sites disagree → a single frozen floor is wrong; per-site floors needed

---

## ✅ STEP 5b — DECIDED 2026-09-25: **N = 36 × 24 opportunities**

Holm-corrected: H1 100%, H2 89%, H4′ 97%, **all three land 88%**. Legal allocation, 12 fewer
participants than the withdrawn 48. Now set in code (`TargetOpportunitiesPerBlock = 24`), the power
script, the fixture, and §4.

**Two consequences:** the block grows to ~320 s (task time ~33 min — re-read the fatigue section), and
**twelve** events need authoring, not six.

### Original framing, retained

A counterbalancing confound was found and fixed 2026-09-25 (condition was correlated with layout — 12 of
36 cells never used). The fix restricts valid allocations to **multiples of 36**, so **N = 48 is
withdrawn**.

| Option | Participants | Session task time | Allocation |
|---|---|---|---|
| **36 x 24 opportunities** | 36 | ~31 min | complete |
| **72 x 18 opportunities** | 72 | ~24 min | complete |

Holm-corrected power for both is being measured. **36 x 24 needs twelve fewer participants than the old
48 and is a legal allocation** — it is the likely choice if its H2 power clears 80%.

## STEP 6 — Send me the numbers

With Steps 2–5 filled in I will:

- Re-run the power grid from your **real** effect sizes and confirm or revise **N = 48 × 18**
- Freeze the measured parameters into `PAPER1_STUDY_DESIGN.md` §4 and §6
- Help place the six new opportunities (see Step 8)

---

## STEP 7 — IRB from your professor

When it arrives, check its scope covers the 2026-09-23 protocol changes:

- [ ] The **PBC condition** exists in it
- [ ] **~3× vibration exposure** in that block (continuous ~1 s per opportunity vs a ~0.3 s pulse train)
- [ ] **Consent element 3** says the vibration is *continuous during an approach* in one block
- [ ] The **new pre-session onset calibration** is covered

**An approval predating 2026-09-23 cannot mention PBC — an amendment is needed either way.** Not on your
critical path while you are only piloting on yourself.

---

## STEP 8 — The six new opportunities (after the pilot)

The design is **N = 48 × 18**; `OpportunitySchedules.Layout1()` authors **12**.

**Authoring happens ONCE, not per layout** — `LayoutVariants` derives L2–L6 from L1 by rotation and mirror,
so only `OpportunitySchedules.Layout1()` and `Layout1Stimuli.All()` change.

A candidate 18-event schedule already passes the geometry audit **18/18**
(`O1×3 · O2×6 · O3×6 · O4×3`, 13 s cadence, no same-limb overlaps, last close 235 s). What it still needs
from you is **six orb/projectile positions** — narrative placement tied to the physical layout, which the
audit cannot decide.

Then: re-run the audit over **all six** layouts (rotation changes approach distances even though the
authoring does not repeat), and untick `protocolDevelopmentMode`.

---

## Gate B — before the first naive participant

- [ ] IRB in hand and scope confirmed (Step 7)
- [ ] 18-event schedule authored and audited (Step 8)
- [ ] `protocolDevelopmentMode` **OFF**
- [ ] All Step 2–5 values frozen into the design doc
- [ ] **Limb capsule radii replaced with real anthropometry** — the current chest 0.12 / hand 0.08 /
      foot 0.10 carry an explicit warning that they are provisional pilot defaults and "not the frozen
      model" (`PAPER1_STUDY_DESIGN` §5)
- [ ] Recruitment rule preregistered: recruit 54, analyse the first 48 complete balanced sets

---

## Before submission (not before collection)

- [x] ~~Read Meng, Ho, Gray & Spence (2014), *Ergonomics*~~ — **done 2026-09-24. Paper 1's crossing claim
      SURVIVES.** Timing was a constant in all three experiments (warning onset coincided with the hazard
      event); they varied spatial pattern and apparent-motion direction only. Cited in §2.2b.
- [ ] Confirm the Spence & Ho 2008 and Petermeijer et al. 2015 characterisations — both are currently
      cited from Crossref metadata, not from the PDFs. Lower risk: neither is load-bearing for a claim.
- [ ] Pull *"Hand movement times and machine guarding"* (Applied Ergonomics 1982,
      `10.1016/0003-6870(82)90087-4`) — could refute Paper 2's claim that nobody reports how ISO 13855's
      K = 2000 mm/s was measured.
- [x] ~~Add the spatial-tactile-warning literature to Paper 1~~ — **done 2026-09-23**: new §2.2b *"Where to
      Warn"* cites Spence & Ho 2008, Petermeijer et al. 2015 and Meng et al. 2014. The Meng sentence is
      written from the abstract and **flagged provisional** until the PDF is read.
- [x] ~~Add an SSRT paragraph to Paper 2's Related Work~~ — **already present** in §2.4, which ends with the
      estimand distinction verbatim. The round-5 recommendation was stale.
- [x] ~~Soften Paper 2's counterfactual-logging claim~~ — **done 2026-09-23**
- [ ] Recheck ISMAR 2027 dates monthly — still unannounced; ~March 2027 is a planning assumption only

Full detail: `docs/NOVELTY_SEARCH_ROUND5.md`.

---

## Paper 2 (E2)

Nearer to ready than Paper 1 and **should submit first**. Outstanding:

- [ ] Latency number from Step 2 (shared)
- [ ] Freeze lead levels, response windows and `SD_THRESHOLD` from its own pilot. **Inflate the pilot SD
      first** — the fitted threshold SD runs ~18% low through partial pooling (49 ms recovered from 60 ms),
      so reading it straight in makes N too small.
- [ ] Record the practice-prefix decision — it is **implemented** (`PracticeTrials = 16` at the longest
      lead) but `PAPER2_STUDY_DESIGN.md` still says it "must be decided and recorded." Implementation is
      not preregistration.

---

## Recorded decisions

- **2026-09-11 (PI) — Paper 1 keeps controllers in both hands throughout, including measured blocks.**
  Rationale: ecological validity, since room-scale VR gameplay is performed holding controllers. Supersedes the
  2026-09-07 "one controller, questionnaires only" decision **for Paper 1 only**. Recorded in
  `PAPER1_STUDY_DESIGN.md` §11.
  Consequences: tracker count settled at **three** (chest + both ankles); the wrist-to-hand offset requirement
  is void for Paper 1; forearm real estate is uncontested (Tactosy only); questionnaire hand-over/set-down
  steps dropped. Limitation to state in the manuscript: added hand mass alters reaching and braking dynamics.
  **Paper 2 is explicitly undecided** and must record its own choice — it measures mid-flight braking at
  millisecond resolution, where hand mass acts directly on the estimand.

## History log (completed milestones)

- **2026-09-25** — **Both papers' data pipelines completed.** Found and fixed a counterbalancing
  confound (condition correlated with layout: 6 distinct plans instead of 36, 12 of 36 cells never
  used) — which **withdrew N = 48**, since legal allocations are multiples of 36; **36 x 24** now
  recommended at H2 = 89% under Holm. Closed irreversible questionnaire item-loss, added
  `analysis_manifest.json` (both papers), `session.json` (Paper 1), and moved the e-stop log out of the
  participant root (§12 release blocker). Paper 2 gained `deviations.csv`, a deliberately narrow
  `events.csv`, tracking-dropout detection, `trajectories_<block>.csv`, and `questionnaires.csv` with a
  shoulder-discomfort stopping rule scored on the worst site. Drafted Paper 1's Abstract, Introduction
  and Method. Unity: 298 tests, 297 pass, 0 compile errors.

- **2026-09-23** — Paper 1 redesigned around Valkov & Linsen (IEEE VR 2019), found by reading the PDF
  rather than the abstract: they ran this paper's H1 at N=40 and got the OPPOSITE result, but confounded
  trigger policy with a continuous intensity mapping participants could switch off by slowing down.
  **Changes:** `Visual` descoped and replaced by **`PBC`** (PB's trigger, continuous cue) carrying a new
  **H4'** that isolates cue form; **H3 de-gated** to exploratory after measuring 30–48% power; confirmatory
  family is H1/H2/H4' under Holm; **N set to 48 x 18** from a full power grid, verified under Holm
  (H2 88%, unchanged by correction); onset-match calibration built to close the sharp-vs-fade-in confound;
  geometry audit rebuilt as a permanent tool after its throwaway predecessor left stale output on disk for
  nine days. Unity: 246 tests, 245 pass, 0 compile errors. Docs: `OPEN_QUESTIONS.md`,
  `PAPER1_READINESS.md`, `PAPER1_RELATED_WORK.md`, `PAPER1_PARAMETER_JUSTIFICATION.md`,
  `NOVELTY_SEARCH_ROUND5.md`.
- **2026-09-14** — Virtual-hazard generalizability reviewed in depth (15 studies, 2001-2026) and the
  resulting changes implemented. `docs/VIRTUAL_HAZARD_VALIDITY.md` rewritten.
  **Verdict:** real-vs-virtual is a MAIN effect on avoidance *magnitude*, not an interaction with the
  *structure* of avoidance — which is exactly the level Paper 1's within-subject contrasts live at.
  **Strongest argument, found by reading §5 rather than the literature:** the hazards are *visually hidden* in
  every condition, so the perceptual mechanism behind the real-vs-virtual gap (misperceiving the distance of a
  *visible* virtual object) largely does not apply — and a registered, invisible hazard is a closer model of
  real furniture seen through an HMD than a visible virtual one. Now in `PAPER1_STUDY_DESIGN.md` §13, gate §14.28.
  **Changes made:** ankle markers (§5, §14.27, `SelfRepresentation.cs`); physical-prop validation demoted to
  between-subjects future work (reverses same-day advice — physical exposure recalibrates subsequent virtual
  performance); Coolen 2020 reframed as motivation rather than limitation; pilot docs updated with the
  predicted-floor warning.
- **2026-09-08** — New design authorities landed (`PAPER1`/`PAPER2`/`COMBINED_STUDY_DESIGN.md`,
  `ISMAR_DISCLOSURE_LETTER.md`, root `PROJECT_HANDOFF.md`, parent grant PDF). Analysed; `docs/PAPER1_CODE_GAP.md`
  written; `docs/PROJECT_HANDOFF.md` reduced to a pointer to kill the duplicate-filename trap; ISMAR venue error
  corrected here.
- **2026-08-11** — Novelty audit; six changes implemented (task scoring, robustness analysis, `CUE`
  acceptability instrument, avoidance-latency promotion) + tests.
- **2026-08** — Vicon integration built and unit-tested: `RigidTransformSolver` (Horn's method with residual
  reporting, which the original notebook lacked), `ViconBodyRig`, `ViconCalibrationRecorder`, `docs/VICON_SETUP.md`.
- **2026-07-16** — Design v2: virtual hazards, deferred penalty, O4 wall volumes, L1–L6 isometric layouts, cue
  tour, practice = None. Virtual-only decision validated against the literature.
- **2026-07** — VR bring-up complete (SteamVR + OpenXR, XR Origin, controllers tracking); full block ran
  end-to-end and wrote CSVs; E2 cue-intensity calibration built and verified.
