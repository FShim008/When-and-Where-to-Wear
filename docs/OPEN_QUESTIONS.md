# Open Questions — whole project

**Compiled 2026-09-23 · rebuilt 2026-09-25.** Swept from every design doc, the code, the analysis scripts
and the audit records. Supersedes the scattered "STILL OPEN" markers, which had drifted: several were
already resolved, and one resolved item was still being cited as a blocker.

> **Rebuild note.** This file was accidentally truncated to zero bytes on 2026-09-25 by a scripted edit that
> opened it for writing and then raised before writing. Reconstructed from the session record and the
> current state of the project. Content should be complete; exact earlier wording may differ.

**Scope:** Paper 1 (*What Makes a Collision Warning Work?*), Paper 2 (*Stopping a Reach in VR*), and the
shared hardware / ethics / venue questions.

---

## How to read this

| Mark | Meaning |
|---|---|
| **BLOCKING** | No human data collection until resolved |
| **Before confirmatory data** | Pilot may proceed; the main study may not |
| **Before submission** | Does not block collection |
| **Decision on record** | Needs a written choice, not new work |

**Owner** is who can actually resolve it — most of these are not code.

---

## 1. BLOCKING — nothing proceeds

### Q1. Latency bench has not been run
**Owner: lab (needs the tactor).**
`OracleParams.PipelineLatencySeconds = 0`, so the predictive threshold does not compensate for the
motion→tactor delay. Both papers depend on it: Paper 1's *delivered* lead is not its *nominal* lead, and
Paper 2's whole measurement is a lead time. `LatencyBenchRunner` and `LatencyBench` are built and tested;
neither has run against hardware. **Also carries Q3.**

**If ignored:** every lead time in both papers is nominal, not measured.

### Q2. IRB
**Owner: PI — WITH THE PROFESSOR, awaiting delivery (noted 2026-09-23).**

The professor holds the IRB and will provide it. **Still blocking until it is in hand** — approval that
exists but has not been read cannot be cited, and its scope must be checked before PBC runs on a naive
participant. The protocol changed materially on 2026-09-23: a new sixth condition, roughly **3× vibration
exposure** in that block (continuous ~1 s per opportunity vs a ~0.3 s pulse train), a new pre-session
calibration, and a longer block. Consent element 3 must say the vibration is *continuous during an
approach* in one block. Checklist in `SAFETY_PROTOCOL.md` § Devices.

**An approval predating 2026-09-23 cannot mention PBC — an amendment is needed either way.**

### Q3. Does `BhapticsLibrary.PlayMotors` replace or queue?
**Owner: lab (needs the tactor). Fold into the latency-bench visit.**
`ContinuousCueGate` submits at ~22 Hz with 60 ms duration so submissions overlap rather than gap.
**Assumes replace.** If the SDK queues, the overlap accumulates across an approach, the cue lags the
participant's motion, and H4′ measures a delayed stimulus — *while every log still looks correct*.

**Test:** drive a slow approach; the level should rise with proximity and **stop within ~60 ms** of the limb
disengaging. If it lingers, `continuousIntervalMillis` must exceed `continuousMillis`.

### ~~Q9b. The analysable target~~ — ✅ DECIDED 2026-09-25: **N = 36 × 24**

Holm-corrected, nsim = 200: H1 100%, H2 89%, H4′ 97%, **all three land 88%**. A legal allocation
(multiple of 36) and 12 fewer participants than the withdrawn 48. Set in
`OpportunitySchedules.TargetOpportunitiesPerBlock`, `power_analysis.R`, the fixture, and §4.

**Cost:** block 180 s → ~320 s, task time ~18 → ~33 min, and **twelve** more opportunity events to
author rather than six. A 24-event candidate passes the geometry audit 24/24.

Original framing, retained:
**Owner: PI decision, once the power numbers land.**

A counterbalancing confound was found and fixed on 2026-09-25 (see §6, Q23). The fix restricts valid
allocations to **multiples of 36**, because §7 requires the complete 6 × 6 crossing of condition-order row
and layout-order row. **N = 48 is not legal** — it divides by 6, not by 36, so it balances condition order
alone.

Candidates under measurement: **36 × 24 opportunities** (fewer participants, longer session) and
**72 × 18**. §4 is explicit that an incomplete crossing is rounded **up**, not accepted.

**If ignored:** either an incomplete crossing the design forbids, or an under-powered H2.

---

## 2. Before confirmatory data — Paper 1

### Q4. The schedule supplies 12 opportunities; the design requires 18 (or 24)
**Owner: storyboard author (human) + code.**
A candidate 18-event schedule passes the geometry audit **18/18** (`O1×3 · O2×6 · O3×6 · O4×3`, 13 s
cadence, no same-limb overlaps, last close 235 s). **Authoring happens ONCE** — `LayoutVariants` derives
L2–L6 from L1 by rigid transform, so only `OpportunitySchedules.Layout1()` and `Layout1Stimuli.All()`
change.

**Still needs a human:** six new **orb/projectile positions**. Then re-run the audit over **all six**
layouts, since rotation changes approach distances even though the authoring does not repeat.

**Q9b selected 36 × 24, so this is TWELVE new events, not six.** The block also grows to ~320 s.
`AssertMeetsTarget()` throws until it lands; a test pins the gap.

### Q5. Session and block duration are documented nowhere — now sharper at 24 opportunities
**Owner: pilot.**
Everything downstream is arithmetic on the 13 s inter-event spacing, not observation. Estimates: 12 events
× 6 blocks ≈ **18 min** task; 18 events ≈ **24 min**; 24 events ≈ **31 min**. **Measure it and replace
these.**

### Q6. The onset-match calibration must actually be run
**Owner: pilot.**
Built and tested 2026-09-23 (`ContinuousOnsetCalibration`, `OnsetMatchCalibrationRunner`, 7 tests).
`ContinuousCueMinIntensity = 0.15` remains an **unmeasured default** until it is run, once per participant,
after E1.

Two outcomes change what H4′ may claim:
- `PinnedAtBound` → PBC's onset cannot be matched at any drive → **do not claim cue form was isolated**
- `Spread > 0.10` → sites disagree → a single frozen floor is wrong; use per-site floors

### Q8. `TactosyTorso` has no `PositionType` mapping
**Owner: PI (hardware purchase).**
`HapticDeviceBinding.PositionFor` throws `NotImplementedException` — the sternum unit is not bought or
paired. **The generic conditions RG and PG cannot run.** The throw is deliberate: the alternative was
falling back to the vest, which is the H2 device confound this design exists to avoid.

### Q9. Freeze the recruitment rule
**Owner: PI. Decision on record.**
Recruit above the analysable target to absorb exclusions, and analyse the first *N* **complete balanced
sets**. Preregister the rule rather than deciding it after seeing dropout. **Superseded in its specifics by
Q9b** — the numbers depend on the allocation chosen.

### Q24. Limb capsule radii are provisional
**Owner: pilot + PI.**
Chest 0.12 / hand 0.08 / foot 0.10 m carry an explicit Inspector warning that they are provisional pilot
defaults and "not the frozen model" (§5). **The primary outcome's definition is not frozen until they are
replaced** with participant-measured anthropometry.

---

## 3. Before confirmatory data — Paper 2

### Q10. Lead levels, response windows and `SD_THRESHOLD` are unfrozen
**Owner: pilot.**
`LeadLevels = {0.08, 0.15, 0.25, 0.35, 0.50}` are *targets*, achieving ~`0.056–0.401 s`. §8 requires they be
set from pilot data so the **achieved** distribution spans 10–90% avoidance. Also unfrozen:
`timingToleranceSeconds`, the response windows, and the `SD_THRESHOLD` that drives N.

**Known bias:** the fitted threshold SD runs **~18% low** (partial pooling; 49 ms recovered from 60 ms).
Inflate the pilot SD before reading it into the N table, or N comes out too small.

### Q11. The practice-prefix decision is implemented but not recorded
**Owner: PI. Decision on record.**
`PAPER2_STUDY_DESIGN.md` still says practice at the longest lead only "must be decided and recorded before
piloting." It **is implemented** (`PracticeTrials = 16`, `PracticeLeadLevel` defaulting to the longest).
Record the decision — implementation is not preregistration.

---

## 4. Before submission

### Q12. Novelty searches against FULL TEXT — round 5 run 2026-09-23/24
**Owner: library access.** See `docs/NOVELTY_SEARCH_ROUND5.md`.

**The live threat was read and Paper 1's crossing claim SURVIVES.** Meng, Ho, Gray & Spence (2014,
*Ergonomics*) held warning timing **constant** in all three experiments — the warning onset coincided with
the hazard event — and varied spatial pattern and apparent-motion direction only. Now cited in §2.2b for
what it does show: body location and motion direction change the response, and a cue moving toward the body
beat a static one.

**Still to pull:** Spence & Ho 2008 and Petermeijer et al. 2015 (cited from metadata; confirm the
characterisations — low risk, not load-bearing), and *"Hand movement times and machine guarding"*
(*Applied Ergonomics* 1982, `10.1016/0003-6870(82)90087-4`) — **the only remaining paper that could
overturn a claim**, Paper 2's C2 about the provenance of ISO 13855's `K = 2000 mm/s`.

**One claim remains unverified, not passed:** the counterfactual-logging claim returned pure search noise.
Softened to "we are not aware of" in `PAPER2_STUDY_DESIGN.md` on 2026-09-23.

### Q14. My rebuilt audit disagrees with the 2026-09-14 numbers
**Owner: me. Low severity, unresolved.**
Approach distances for hand events run ~0.10 m higher than the recorded run (O2: 0.72 m vs 0.62 m).
Verdicts and sensitivity agree closely (O2 21% vs 22%). **The original program was throwaway and is gone**,
so this cannot be reconciled. Mine uses `SyntheticTrajectory.NeutralPose()` and subtracts hand radius
explicitly, both auditable in source. Decide which number the paper quotes.

### Q15. The MDE table is noisy at nsim = 30
**Owner: me (code).**
The `+0.20` row reads 37/13/27/57 — power cannot fall as N rises, so that is sampling error (SE ≈ 9 points).
Report it as "undetectable at any N tested", never cell by cell. If a reviewer makes any of it load-bearing,
re-run §6 at higher nsim rather than defending the numbers.

### Q16. Is `Visual` a third paper, or abandoned?
**Owner: PI. Decision on record.**
Descoped from Paper 1 on 2026-09-23 but **kept implemented and tested**. Re-scheduling costs a seventh block
the session cannot absorb. §524 of the design doc still carries the old unresolved question "what does the
visual cue indicate?" — moot while descoped, live again if it returns.

### Q17. Does H5 spend confirmatory alpha?
**Owner: PI. Decision on record.**
H5 (feedback vs None) is framed as a **validity gate, not a finding**, and is excluded from the Holm family
on that basis. §560 still debates spending five contrasts on it. Settle it in writing.

---

## 5. Venue and submission

### Q18. ISMAR 2027 dates are not announced
**Owner: PI. Recheck monthly.**
`ieeeismar.net` carries only the 2026 edition (Bari, 5–9 Oct 2026). **~March 2027 is a planning assumption
by analogy — nothing more.** IEEE VR 2027's deadline (31 Aug 2026) has passed. **The gating constraint is
ethics approval and pilot completion, not the calendar.**

### Q19. Submission order
**Owner: PI. Decision on record.**
Recommended: **Paper 2 first.** Closer to ready, and its LT80 gives Paper 1's Discussion the sufficiency
argument. A *later* Paper 1 can cite a *published* Paper 2 — better than anonymous self-citation in a
double-blind cycle.

Safe only because **`T` is not grounded in Paper 2**. If `T` ever becomes dependent on Paper 2's result,
submission order becomes a hard constraint rather than a preference.

---

## 6. Process questions this project keeps re-learning

Not tasks — failure modes that have each cost real work, and that recur.

### Q20. Who owns scene geometry?
Hazard positions live in `Dryrun.unity` (human-authored, Editor) but the audit, the analysis and
`MinApproachDistanceForValidTiming` all depend on them, with no mechanism keeping them in sync. The
2026-09-14 O2 fix was made by editing the scene file **on disk**. Decide: does Core own the surveyed
geometry, or does the scene?

### Q21. Artefacts that outlive their generators go stale silently
`audit_output.txt` sat on disk for nine days showing **pre-fix** results (O2 at 59%) while
`SceneGeometry.cs` already carried the post-fix position, because the audit program had been deleted as
"throwaway." Anyone reading it would have concluded the schedule was broken.
**An output file without its generator is not evidence.**

### Q22. The `C:\cc` harness has two blind spots
1. **Different NUnit version** — `Is.GreaterThanOrEqualTo(x).Within(tol)` compiles there and **fails in
   Unity**.
2. **Core + Tests only** — `Runtime` and `Integration` are never compiled there.

**Green in the harness is not green.** Run Unity batch mode before believing a Runtime/Integration change.

### Q23. Counterbalancing was confounded and nothing caught it — NEW 2026-09-25
`SessionPlan.For` derived **both** the condition order and the layout order from `participantId`, so they
were perfectly correlated. Measured over 72 simulated participants: **6 distinct plans instead of 36**,
participants 0/6/12/18/… identical, and **12 of the 36 condition × layout cells never occurred**.

`paper1_analysis.R` fits `(1 | layout)` assuming layout is a decorrelated nuisance factor. It was not — a
layout-difficulty effect would have loaded onto the condition estimates and biased H1, H2 and H4′.

**Found only by drafting the Method section**, three days after the design was declared "bulletproof." The
existing `SessionPlanTests` checked that each condition appeared once per position — true, and blind to the
confound. **A counterbalancing scheme needs a test of the crossing, not of each factor separately.** Now
guarded by three tests.

### Q25. Scripted edits that truncate on failure — NEW 2026-09-25
This file was reduced to **zero bytes** by a patch helper that opened it with `"w"` (truncating) and then
raised a `UnicodeEncodeError` before writing. No git, so no recovery — it was rebuilt by hand.
**Write to a temp file and rename, or validate the encode before opening the target.**

---

## 6B. Implementation completed 2026-09-25

Both papers' data pipelines are now feature-complete against their design documents.
**Unity 6000.3.16f1 EditMode: 298 tests, 297 pass, 1 skipped, 0 compile errors.**

| Added | Paper | Why it mattered |
|---|---|---|
| Item-level questionnaire export | 1 + 2 | Items were collected then **discarded at the callback boundary**. Scores cannot be decomposed back into items, so every raw response was lost irreversibly at collection. |
| `analysis_manifest.json` | 1 + 2 | §12/§14 both require checksums; neither paper had any. Gate 17 ("freeze and archive") could not pass. |
| `session.json` | 1 | Paper 1 recorded nothing about the apparatus that produced its data. |
| E-stop log relocated | 1 | Was a **participant-root append file** — §12's explicit release blocker. One file, every participant, no participant column. |
| `deviations.csv` | 2 | Invalid reasons existed only inside `e2_trials.csv`; operator stops and equipment failures had no row at all. |
| `events.csv` (non-trial) | 2 | Nothing could explain a gap in the timeline. |
| Tracking-dropout detection | 2 | A dropout mid-trial is a reason a realized lead may be wrong; it left no trace in any file. |
| `trajectories_<block>.csv` | 2 | A disputed trial could not be recomputed. Logs the **same frames the runner consumed**. |
| `questionnaires.csv` + shoulder discomfort | 2 | E2's foreseeable harm is musculoskeletal fatigue and **nothing measured it**. Scored on the max site, never the mean. |

**Two things deliberately NOT built**, recorded so they are not mistaken for oversights:

- **No tracking-quality column.** `PoseFrame` has no confidence field; a constant "good" would read as
  a measurement. `gap_s` is the honest substitute.
- **No per-trial data in `events.csv`.** It duplicates `e2_trials.csv`, and two records of one
  measurement can disagree after an edit. A test enforces the boundary.

---

## 7. Closed, recorded so they are not re-opened

| Was open | Resolution |
|---|---|
| Runtime/Integration compile unverified | Unity 6000.3.16f1 EditMode: **249 tests, 248 pass, 0 errors** |
| Power grid incomplete | Full 15-cell grid + MDE + Holm-corrected grid, nsim up to 200 |
| H3 gating two well-powered tests at 32% | De-gated; exploratory. Holm family is H1/H2/H4′ |
| Holm correction unmodelled | Measured: at 48 × 18 the cost to H2 was 1 point (88.5 → 87.5). *N itself now superseded by Q9b.* |
| PBC unrunnable (no continuous sink) | `CreateStudySink` + `ContinuousCueGate`, wired into all three P1 drivers |
| Onset confound unaddressed | Calibration built, 7 tests |
| "O2 must be re-sited" | Already done 2026-09-14; a stale pre-fix claim had been propagated to 5 places |
| O2 unverified in the Unity scene | Opened and confirmed 2026-09-23 at (0.95, 0.70, 0.60) |
| Meng et al. 2014 might refute the crossing claim | Read 2026-09-24 — timing was constant; **claim survives** |
| "Add an SSRT paragraph to Paper 2" | Already present in §2.4, which states the estimand distinction verbatim |
| §14 outputs unwritten | All writers exist: `events.csv`, `opportunities.csv`, `keypoints_*.csv`, questionnaires |
| CODE_GAP #1 (per-opportunity outcome) | Engine was already correct; the analysis was the gap |
| H2 device confound | Sternum Tactosy; vest unreachable at both Core and Integration layers |
| Condition confounded with layout | Fixed and guarded 2026-09-25 (Q23) |
| Item-level questionnaire data discarded | `kind` column; items written beside scores |
| No checksums / archivable manifest | `analysis_manifest.json`, both papers |
| Paper 1 had no session metadata | `session.json`, shared shape with Paper 2 |
| E-stop log was a participant-root append file | Relocated into the session directory |
| Paper 2 missing 5 of 9 §14 outputs | All 9 applicable now written |

---

## 8. The short version

**Implementation is no longer on the critical path for either paper.** Four things block everything:
the latency bench, the IRB, the sternum Tactosy, and the allocation decision.** The first three need a person — two need the lab, one needs a purchase. The fourth needs the
power numbers now running.

**Two things block the main study but not a pilot:** the opportunity schedule (orbs need authoring) and the
onset calibration (built, must be run).

**One thing would embarrass the paper if skipped:** pulling the 1982 *Applied Ergonomics* note, the last
paper that could still overturn a claim.

Everything else is a decision to write down.
