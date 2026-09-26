> **⚠ SUPERSEDED 2026-09-08.** The design authority is now `../PAPER1_STUDY_DESIGN.md` (with
> `../PAPER2_STUDY_DESIGN.md` and `../COMBINED_STUDY_DESIGN.md`). This file is retained as the *rationale
> record* for the virtual-hazard decision and the v2 mechanics that survived — deferred penalty, layout
> isometries, cue tour, practice = None, hidden hazards. **Do not implement from it.** Three things here are
> now wrong: the primary outcome (block counts → per-opportunity binary violation), the Visual benchmark
> (proximity glow → transient predictively-triggered limb indicator), and the chest cue hardware
> (vest → Tactosy at sternum). See `PAPER1_CODE_GAP.md`.

# Study Design v2 — Virtual-Hazard Redesign
### What Makes a Collision Warning Work? Separating Trigger Timing, Body Site, and Cue Form in VR

**Status:** design authority for the study as of 2026-07-16, superseding the physical-foam design in
`STUDY_OVERVIEW.md` where they conflict. Decided with the PI: **all hazards are virtual volumes; no physical
obstacles are placed in the play area.** This document exists to make that version bulletproof — every cost of
going virtual is named and paid, every known threat has a mitigation.

---

## 1. What changed, and why (one page)

The original design placed real foam obstacles co-located with virtual hazard volumes. Analysis identified that
the foam was **behaviorally motivating but measurement-irrelevant and operationally corrosive**:

- The DV was *never* physical contact — collisions are scored as tracker-vs-collider distance in both designs.
- Foam soft enough to be safe is light enough to displace on contact. A displaced obstacle desynchronizes the
  physical room from the system's hazard model: warnings fire at empty space, no warning fires at the real foam,
  and the participant cannot see either. That is simultaneously a **safety failure** (invisible untracked trip
  hazard) and a **data failure** (every post-displacement trial is corrupt).
- A shin-height block targeted by stepping events is a **trip hazard** for a vision-occluded, possibly tethered
  participant — padding does not mitigate tripping.
- Physical contact is itself an **information channel**: participants in high-collision conditions (None) would
  feel more bumps and learn the layout faster than participants in low-collision conditions — an asymmetric
  learning confound *favoring the control condition*.

Going virtual removes all four problems and enables one improvement the foam design could never deliver
operationally: **per-block layout rotation** (Section 6), which kills location-learning across blocks.

The two real costs of going virtual — participant motivation and the paper's claims — are paid in Sections 5
and 2 respectively.

## 2. Claims and framing

- **Title (revised):** drop "Real-Obstacle". Working title: *What Makes a Collision Warning Work? Separating Trigger Timing, Body Site, and Cue Form in VR (was: Predictive, Body-Localized
  Vibrotactile Warnings for Hazard Avoidance in Room-Scale VR.*
- **What we claim:** a controlled comparison of warning **timing** (reactive vs predictive) and **body-localization**
  (generic vs at-risk limb) for avoiding **precisely registered, invisible hazard volumes** during room-scale VR
  movement, under an idealized tracking oracle. The oracle framing (upper bound on what prediction can add) was
  already central; the registered-hazard framing extends it: *given a system that knows where hazards are, how
  should it warn?* Every deployed warning system reasons over a hazard model, not over reality — we evaluate the
  warning policy on exactly the model it reasons over, with zero registration error.
- **What we do not claim:** transfer to physical furniture with its真 real consequences and imperfect hazard models.
  Stated in limitations; a small physical-obstacle validation is named as future work.
- **No deception:** participants are told truthfully that invisible hazard zones exist in the room-scale space and
  that contacting them costs points. They are never told the hazards are physical.

## 3. Core design (unchanged)

Within-subjects **2×2 (Timing × Localization)** + **None** floor + **Visual** reference = 6 conditions
(None, RG, RB, PG, PB, Visual), Williams 6×6 order, ~180 s blocks, 12 scripted clock-driven opportunities per
block, identical 3-pulse cue with per-site perceived-intensity equalization (E2), NB-GLMM primary analysis.
Nothing in the manipulation, cue, oracle, detector, or analysis changes.

## 4. The hazard model (revised)

- **O1 (low block), O2 (pillar), O3 (panel):** virtual box volumes, **meshes never rendered** (enforced in code:
  `VisualObstacleAlert` hides all obstacle renderers from frame 0 in every condition). Only the Visual condition
  renders anything: a translucent proximity-graded glow that fades in on approach and back to invisible.
- **NEW — O4 (wall segments) replace the BOUNDARY targets.** The two boundary events (E5, E11) previously pushed
  the participant toward the *chaperone edge*, where the system-level SteamVR grid appears **in every condition** —
  a visual-feedback leak that contaminates those events. v2 fix: author **virtual wall-segment volumes (O4a, O4b)**
  at the former boundary-event locations, and keep the entire virtual arena **≥ 0.5 m inside the physical
  chaperone**. Result: all 12 events target registered virtual volumes, the system chaperone never appears during
  a block, and physical-boundary safety *improves*.
- **Arena:** a **square 3.0 × 3.0 m** virtual arena (squareness is required for the rotation scheme in §6),
  centered in a physical clear space of ≥ 4 × 4 m. The floor is completely empty — nothing to trip on.

## 5. Motivation: the deferred-penalty incentive (pays cost #1)

Without physical consequence, participants could learn that collisions are free and stop responding to warnings.
v2 makes collisions costly **without creating a feedback channel**:

- **Score rule:** +1 per orb delivered · −1 per projectile hit · **−3 per hazard collision**.
- **CRITICAL CONSTRAINT — the penalty is silent and deferred.** No per-event sound, flash, rumble, score flicker,
  or counter change at the moment of collision. The collision tally and penalty appear **only on the end-of-block
  score summary**. A per-event penalty signal would *be* a collision-feedback system and would destroy the None
  condition. This constraint binds all future UI work.
- **Instruction script — ⚠ SUPERSEDED 2026-09-14.** The current script lives in
  `../PAPER1_STUDY_DESIGN.md` §8. **Do not read the version below to a participant:** it covers the hazards and
  the penalty but says nothing about the vibration cue, which leaves participants to invent their own response
  rule and puts H2 at risk. Kept here only as the record of what v2 said.
  *"Invisible hazard zones stand in the play space, like furniture you can't
  see. Move as though they were real — each time any part of your body enters one, you lose 3 points. You won't
  feel or hear anything when it happens; you'll see the total at the end of the round."*
- **Penalty magnitude** (−3) is a pilot-tunable decision (**D9**): large enough that participants report caring,
  small enough that nobody freezes (floor effects). Pilot criterion: None-condition collision rate must sit
  clearly below the task-forced incursion ceiling AND clearly above zero.
- **Manipulation checks:** (a) post-block item "I actively tried to avoid the hazard zones" (0–6); (b) avoidance
  latency and dodge success as engagement covariates (already logged); (c) None-block trajectories quantify
  task-forced exposure (mean min-distance per opportunity), verifying the opportunities genuinely induce risk.

## 6. Layout rotation L1–L6 (pays the location-learning threat)

Foam made layout rotation operationally impossible; virtual hazards make it free. v2 uses **isometries of the
authored L1 geometry** — rotations and mirror reflections of the square arena. Isometries preserve every
distance, approach angle, and event demand **by construction**, so all six layouts are identical in difficulty
and differ only in what a participant could memorize.

- **The six variants:** L1 = identity · L2 = 90° rotation · L3 = 180° rotation · L4 = 270° rotation ·
  L5 = mirror (left-right) · L6 = mirror + 90°. Applied to obstacle volumes **and** stimulus spawn points
  **and** projectile origins as one rigid transform about the arena center.
- **Limb remapping:** mirror variants (L5, L6) swap left↔right target limbs (E1's right-hand reach becomes a
  left-hand reach). L1's schedule is left-right balanced (4 R-arm / 4 L-arm, 1 R-foot / 1 L-foot, 2 chest), so
  every variant preserves the same limb-count balance exactly.
- **Assignment:** one layout per block, rotated across blocks and crossed against condition order via the
  existing `SessionPlan` machinery (layout was always a planned random effect — `(1 | layout)` is already in the
  primary model; v2 finally makes it meaningful).
- **Practice** uses its own dedicated variant **LP** (the seventh dihedral element: mirror + 180°, **never
  reused** in the six test blocks), so practice cannot teach any test layout.

## 7. Practice & cue familiarization (removes novelty artifacts without privileging a condition)

- **Cue tour (new, scripted, ~2 min, before practice):** each haptic site fires the study cue once with a verbal
  label ("that was your left shin"), and the Visual glow is demonstrated once on a sample volume. Every
  participant meets every cue **outside any measured block**, so first-block novelty doesn't differ by condition.
  (The E2 intensity-matching session already exposes all sites; the cue tour additionally shows the Visual glow
  and names the sites.)
- **Practice block condition: None** (was PB). Practice teaches the *task* (orbs, projectiles, scoring, moving
  through the space); the cue tour teaches the *cues*. Running practice under PB would hand the headline
  condition a familiarity advantage — under None, no condition is privileged.

## 8. Measures

| Tier | Measure | Source |
|---|---|---|
| **Primary** | collisions per opportunity | detector (edge-triggered per engagement) |
| **Secondary — mechanism (promoted 2026-08)** | **avoidance latency** (alert → limb begins retreating). The primary DV says *whether* the warning worked; this says *why*. If predictive timing helps by buying reaction time, it shows up here, and the two results corroborate each other | `AvoidanceLatencyDetector` → `summary.csv`; modelled in `analysis.R` |
| Secondary (confirmatory) | presence (IPQ), workload (NASA-TLX) | post-block questionnaires |
| **Secondary — adoption (new 2026-08)** | **warning acceptability** (helpful / timely / trusted / annoying, instrument `CUE`). A warning that cuts collisions but is distrusted or resented will not be adopted. **TIMELY doubles as the subjective manipulation check for Timing** | post-block, warning conditions only |
| Secondary (safety/quality) | SSQ, near-misses, min clearance, alerts | pipeline |
| Exploratory (new in v2) | **incursion dwell time** and **max penetration depth** per collision — virtual volumes don't physically stop a limb, so dwell/depth characterize what "a collision" was; robustness check that condition effects hold on dwell, not just counts | keypoint log post-processing |
| Manipulation checks | avoidance-intent item; alert counts; predictive-lead verification; task-forced exposure in None | mixed |
| Exploratory probe | end-of-session **layout awareness**: participant places the hazards on a top-down map; tests whether rotation actually suppressed location learning | debrief sheet |

## 9. Threats to validity — the bulletproofing table

| Threat | v2 answer | Residual |
|---|---|---|
| No physical consequence → warnings ignorable | deferred −3 penalty + instruction framing + manipulation checks (§5) | motivation is instructed, not intrinsic — limitations ¶ |
| Penalty becomes a feedback channel | penalty strictly silent & deferred to block end (§5) | none if constraint honored |
| Location learning across blocks | 6 isometric layout variants + separate practice variant (§6) | tested by debrief probe |
| Asymmetric bump-learning confound (None feels more contacts) | eliminated — no contact channel exists | none (v2 strictly cleaner than v1) |
| Chaperone grid leaks visual feedback at boundary events | O4 virtual walls + arena ≥ 0.5 m inside chaperone (§4) | none during blocks |
| Hazard-model vs reality registration error | eliminated — DV computed on the exact model the warnings use | claims scoped to registered hazards (§2) |
| Pass-through changes collision semantics | edge-triggered count unchanged; dwell/depth reported as robustness (§8) | reviewers may ask; answer pre-built |
| First-block cue novelty | cue tour before practice (§7) | none |
| Practice privileges a condition | practice = None (§7) | none |
| Visual vs haptic information asymmetry (glow reveals geometry; buzz reveals limb) | inherent to the modality comparison; detection-matched (same D, same tracking); documented (decision D5) | inherent — reported honestly |
| Ecological transfer to real furniture | out of scope by design; limitations + future-work validation study (§2) | the accepted trade |
| Oracle idealization | unchanged from v1: upper-bound framing | inherent — framed |

## 10. Safety (v2 — greatly reduced, not zero)

Falls during lunges/dodges are now the primary risk (floor is empty; nothing to strike or trip on).
Retained: cleared 4×4 m space, chaperone ON (never reached during blocks by §4 margin), cable managed or
wireless, e-stop (built + rehearsed), SSQ monitoring with stop criteria, attendant present (full two-person
spotter protocol no longer safety-critical; one attendant suffices — PI/IRB call). `SAFETY_PROTOCOL.md`'s foam
sections become obsolete; a v2 pass will trim it for IRB submission (minimal-risk claim now defensible).

## 11. Analysis (unchanged models; two additions)

Primary NB-GLMM, floor model, PB-vs-Visual model, and questionnaire models exactly as implemented in
`Analysis/analysis.R`. Additions: **(a)** `(1 | layout)` is now estimable (six real levels); **(b)** two
pre-planned sensitivity analyses — excluding the two chest/O4 events, and re-running the primary contrast on
dwell time — to show robustness of the headline effect. Power analysis re-runs on pilot-estimated rates
(virtual-only rates may differ from foam assumptions; `power_analysis.R` takes new rates directly).

## 12. Decisions (updated register)

| # | Decision | Status |
|---|---|---|
| D1 | Safety protocol scope (v2 minimal-risk, attendant model) | PI/IRB — redraft ready on request |
| D2 | N + opportunities/block | from power sim after pilot |
| D3 | Cue-intensity reference site/level | default: hand @ 0.8 (built) |
| D4 | ~~Obstacle coordinates vs docs~~ → **v2 canonical geometry = authored L1 + isometries** | closed by v2 |
| D5 | Visual-vs-haptic information asymmetry | documented as inherent |
| D6 | LimbContactRadius (tracker-mount offset) | set at tracker touch-test |
| D7 | Analysis-plan lock + stopping rule | before data collection |
| D8 | Multi-limb / debounce policy | keep single-alert-per-approach, 1.0 s |
| **D9** | **Penalty magnitude (−3 default)** | **pilot-tuned (§5)** |
| **D10** | **Layout scheme sign-off (6 isometries + practice variant)** | **PI confirm** |

## 13. Implementation delta (code/scene work this design requires)

1. **Deferred penalty + end-of-block score summary** — collision count × −3 joined with the spawner score,
   shown only at block end (operator screen + optional HMD text). *No per-event signal anywhere.*
2. **O4 wall volumes** — two thin box volumes at the former boundary-event locations; retarget E5/E11 from
   `BOUNDARY` to `O4a`/`O4b`; shrink virtual arena inside the chaperone.
3. **Layout isometries** — a pure-Core `LayoutTransform` (rotation/mirror about arena center) applied to
   obstacle volumes, stimulus positions, projectile origins, and target-limb left↔right remap on mirrors;
   `SessionPlan` layout pool → the 6 variants + practice variant. Unit tests: isometry preserves pairwise
   distances; mirror preserves limb-count balance.
4. **Cue tour** — scripted pre-practice sequence (per-site pulse + Visual glow demo).
5. **Practice condition → None** (one Inspector default + doc updates).
6. **Dwell/penetration post-processing** — added to the R pipeline from the existing keypoint logs (no Unity change).
7. **Conforming doc edits** — `STUDY_OVERVIEW.md`, `SAFETY_PROTOCOL.md` (v2 trim), `SESSION_CHECKLIST.md`
   (drop foam steps, add cue tour), abstract wording ("hazard volumes").

Items 1–5 are Unity/Core code I can implement and unit-test without hardware; 6 is R; 7 is docs.

---

---

## 14. Revisions of 2026-08 — novelty audit response

A literature audit found that **"Belt and whistles — adding lower body collision awareness for MR experiences"
(CHI 2026)** — a haptic **belt** giving **directional** cues about **virtual** obstacles — landed after v2 was
written. It does not manipulate warning timing and does not cue the at-risk limb, but it partially occupies the
"body-referenced haptics reduces collisions" space. Repeated targeted searches found **no factorial study
crossing warning timing (TTC vs proximity) with cue localization (generic vs at-risk limb)** — that gap is
still open, and it is now the centre of the contribution. Six changes followed.

| # | Change | Status |
|---|---|---|
| 14.1 | **Task scoring wired.** `OpportunitySpawner.CheckInteractions()` scores orbs collected (hand within 0.15 m) and projectile hits (within 0.25 m of head/chest), driven by tracking rather than physics colliders so it works identically on controllers, VIVE trackers, or Vicon. Engagement is the documented failure mode of virtual-hazard studies (10% vs 52.5% collision rates by task engagement), so an inert task was a live validity risk | ✅ code |
| 14.2 | **Framing shifts to TIMING.** Predictive-vs-reactive is the unoccupied axis; localization is now the second factor rather than the headline. Title/abstract/intro to be rewritten accordingly | ⬜ writing |
| 14.3 | **Tracking-noise robustness analysis.** `TrackingNoise` + `WarningFidelity` (Core) + `RobustnessAnalysisRunner` (Runtime) replay recorded keypoints through the real warning logic at increasing tracking error, reporting detection rate, lead time, and false-alarm rate per condition. Answers the oracle objection with data instead of a caveat | ✅ code |
| 14.4 | **Warning-acceptability instrument** (`CUE`, 4 items) added after every warning condition | ✅ code |
| 14.5 | **Avoidance latency promoted** to a headline secondary + modelled in `analysis.R` | ✅ code |
| 14.6 | **Cite and differentiate** Belt & Whistles (CHI 2026) and Samsel et al. (2025, *Applied Ergonomics*) | ⬜ writing |

**Scope limit on 14.3 — state it exactly this way.** Replay cannot simulate how a participant would have *moved*
under degraded warnings, so the analysis measures **warning fidelity, not collision reduction**. Report it as
*"the predictive lead time and detection rate hold up to X cm of tracking error"* — never *"collisions would
still have dropped."*

**Additional limitation surfaced by the audit.** The six layout variants are **isometries of a single authored
arrangement**, so they control for *location memory* but not for *arrangement diversity*; generalization across
genuinely different room layouts is untested and should be stated. Related: published work notes that missing
haptic input and self-avatars make VR behaviour *more conservative* in collision contexts, which supports the
existing "absolute rates are attenuated" caveat.

*Created 2026-07-16 after the PI decision to go virtual-only. Rationale: the foam contributed stakes but not
measurement, and its displacement risk attacked both safety and data integrity; v2 pays the motivation cost with
a silent deferred penalty, pays the framing cost by scoping claims to registered hazard volumes, and banks the
gains (no registration error, no bump-learning confound, true layout rotation, minimal-risk IRB).*
