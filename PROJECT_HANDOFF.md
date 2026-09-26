# PROJECT HANDOFF — *What Makes a Collision Warning Work?*
### Engineering handoff and historical design summary

**Audience:** an engineer or AI agent integrating or auditing the project.

**Repo root:** `WhenandWheretoWarn/My project (1)/` · Unity **6.3 LTS (6000.3.16f1)**, URP, new Input System, C# 9
**Status as of 2026-09-05:** pre-study development; implementation is incomplete and has not been certified
against the revised protocols; no human pilot or main study is authorized.

> **Authority notice.** This handoff contains a historical implementation target and code inventory. It is not
> the scientific design authority and must not be used to start a user study. The current authorities are
> `PAPER1_STUDY_DESIGN.md`, `PAPER2_STUDY_DESIGN.md`, and `COMBINED_STUDY_DESIGN.md`. Where this file conflicts
> with those documents, the paper-specific design wins. A component named below may be planned but absent from
> the current Unity project; verify source, scene wiring, tests, and physical hardware behavior before relying on it.

---

## 1. TL;DR — the project in five sentences

1. A VR user wearing an HMD cannot see hazards in the room; this study tests **vibrotactile warnings** that tell
   them a body part is about to hit one.
2. It crosses two design factors in a **2×2 within-subjects factorial**: warning **POLICY** (proximity-triggered
   vs TTC/prediction-triggered) × haptic **MAPPING** (generic torso cluster vs at-risk-limb cue). Policy is a
   total system manipulation, not a pure timing manipulation unless coverage and false alarms are matched.
3. Two anchor conditions bracket the 2×2: **None** and a transient **Visual benchmark** driven by the same
   predictive trigger as PB, giving 6 conditions total; it is not described as “current practice.”
4. Hazards are **invisible virtual volumes** registered in the tracking space (no physical props); a collision
   is scored when a tracked limb enters a volume, and each collision silently costs points revealed only at the
   end of the round.
5. The primary outcome is the binary occurrence of a designated target-limb × target-hazard boundary entry per
   valid scripted opportunity, analyzed with an opportunity-level logistic mixed model; block counts are a
   sensitivity analysis.

---

## 2. Research question, hypotheses, and framing

**Question:** Given a system that estimates impending virtual-hazard contact, how do warning **policy** and
haptic **spatial mapping** independently and jointly affect the probability that the at-risk body part crosses
a virtual hazard boundary?

**Conditions (6).** Cue waveform is **identical** in all haptic conditions — only timing and location differ.

| | **Generic** (cue a torso motor cluster) | **Body-localized** (cue the at-risk limb) |
|---|---|---|
| **Proximity** — fire on a frozen distance boundary (prototype default D = 0.30 m) | **RG** | **RB** |
| **Predictive** — fire on a frozen TTC threshold (prototype default T = 0.50 s) | **PG** | **PB** |

`PB` is one of four factorial cells, not a privileged "full technique." Labeling it as the intended winner
prejudges §15 of the Paper 1 authority, which ranks conditions under a preregistered decision rule. `D` and `T`
are unvalidated prototype defaults (§12), not approved constants.

Plus **None** and a transient **Visual benchmark** driven by the same predictive trigger and eligibility as PB.

**Hypotheses**
- **H1 (Policy):** Predictive TTC policy changes violation probability vs proximity policy (main effect).
- **H2 (Mapping):** At-risk-limb mapping changes violation probability vs generic torso mapping (main effect).
- **H3 (Interaction):** the mapping effect differs by policy. The confirmatory contrast is
  `(PB − PG) − (RB − RG)` on the preregistered model scale; PB having the lowest mean is not an interaction test.
- **H4 (benchmark):** PB differs from the trigger-matched Visual benchmark.
- **H5 (floor):** preregistered feedback families differ from None, with multiplicity control.
- **Experience (secondary):** presence, workload, sickness, and acceptability are estimated. “Preserved” or
  “not degraded” requires a justified equivalence/non-inferiority margin and adequate power.
- **Mechanism (secondary):** delivered coverage, physical lead time, false alerts, clearance, and valid
  cue-to-avoidance response are reported but are not post-treatment covariates in the primary total-effect model.

**Deliberate scope limits (state these in any paper):**
- The predictor is an **idealized oracle** (constant-velocity TTC on near-perfect tracking) — results are an
  **upper bound** on what predictive timing can add, not a product claim. This framing is **restored** by the
  2026-09-07 Vicon decision and is the correct one for both papers.

  Note what the upper-bound reading does and does not license. It supports: "predictive timing can add at most
  this much, given ideal sensing." It does not support any claim about a fielded system, whose tracking and
  prediction will both be worse. The robustness analysis (§12 in both design docs) is what bridges the two,
  and it is a sensitivity analysis rather than a device certification.

  A brief 2026-09-07 revision replaced this with a "deployable commodity hardware" framing after switching to
  inside-out tracking. That switch was reverted the same day — see §7 — and the upper-bound framing stands.
- Hazards are **virtual volumes**, so absolute boundary-entry rates are not deployment or injury estimates; all
  confirmatory claims are relative within the tested task and population.
- “Predictive versus proximity” is a warning-policy comparison. Differences in coverage and realized lead time
  are part of each policy's total effect unless a separate experiment explicitly matches them.

Full authority, outcome definitions, counterbalancing, models, and launch gates are in
`PAPER1_STUDY_DESIGN.md`.

---

## 3. What a participant actually does

**Arena:** ~3.5 × 3.5 m cleared floor. In the headset it looks **empty**. Five invisible hazard volumes occupy it:

| Id | Shape | Role in the schedule |
|---|---|---|
| `O1` | low block (40 cm cube, floor-standing) | foot/shin events |
| `O2` | pillar (35 × 140 × 35 cm) | right-arm events |
| `O3` | panel (10 × 150 × 60 cm) | left-arm events |
| `O4a`, `O4b` | front + left wall segments (auto-spawned in code) | torso/boundary events |

**Task:** a reach/dodge game. Cyan **orbs** appear to reach for; red **projectiles** fly at the participant to
dodge. Per block, **12 scripted opportunities** fire on a fixed clock (onsets 8, 22, 35, 48, 61, 74, 87, 100,
113, 126, 139, 152 s; 6 s attribution window each). Each event is choreographed to lure a **specific limb**
toward a **specific hazard** — the participant experiences a game, not a list of traps.

**Session flow — superseded.** The legacy prototype flow was: baseline SSQ → cue tour → practice (90 s, None,
layout LP) → 6 condition blocks (~180 s) in Williams order with IPQ + NASA-TLX + SSQ after each → post-session
SSQ, totalling ≈ 50–60 min.

That flow is **no longer the protocol**. `PAPER1_STUDY_DESIGN.md` §8 adds tracking and physical-timing
validation and per-site haptic detection/salience calibration before the cue tour, and the 50–60 min figure
does not include them. Implement §8, not this paragraph, and re-derive the session duration from measured
pilot timing.

**Task scoring** is tracking-driven (`OpportunitySpawner.CheckInteractions`, called each frame by the session
loop): a hand within 0.15 m of an orb collects it; a projectile within 0.25 m of head/chest is a hit. Distance
tests rather than physics triggers, because tracked limbs are plain Transforms with no colliders — so scoring is
identical on controllers, VIVE trackers, or Vicon.

**Incentive (critical design constraint):** +1 per orb delivered, −1 per projectile hit, **−3 per hazard
collision**. The collision penalty is **silent and deferred** — no sound, flash, or counter at collision time;
it appears only in the end-of-block summary, and is suppressed during the questionnaires so it cannot bias
presence/workload ratings. *A per-event penalty signal would itself be a collision-feedback channel and would
destroy the None condition. Any future UI work must honor this.*

---

## 4. Design v2 — the virtual-hazard redesign (why the study looks like this)

The original design placed real foam obstacles co-located with the virtual volumes. The PI removed them. The
reasoning, and the compensations, are the core of the current design (`docs/STUDY_DESIGN_V2.md`):

**Why foam was dropped**
- The DV was never physical contact — collisions are tracker-vs-collider math either way, so foam contributed
  motivation but **no measurement**.
- Foam soft enough to be safe is light enough to displace on contact; a displaced obstacle desynchronizes the
  room from the hazard model → warnings fire at empty space while an invisible untracked hazard remains. That is
  simultaneously a safety failure and a data failure.
- A shin-height block targeted by stepping events is a trip hazard for a vision-occluded participant.
- Physical bumps are an **information channel**: high-collision conditions (None) would feel more contacts and
  learn the layout faster — an asymmetric confound favoring the control condition.

**What was added to pay the costs**
1. **Deferred −3 penalty** (motivation without a feedback channel) — §3.
2. **O4 virtual wall volumes** replacing the old chaperone-boundary targets, so the SteamVR grid (visible in
   *every* condition) never appears during a block; the virtual arena sits ≥ 0.5 m inside the physical chaperone.
3. **Six isometric layout variants (L1–L6)** — rotations/mirrors of L1 about the arena center. Isometries
   preserve every distance and approach angle *by construction*, so difficulty is identical while memorization
   is useless. Mirrors swap left↔right target limbs; L1's schedule is left-right balanced so limb counts are
   preserved exactly. Practice uses a 7th variant (**LP**) never reused in test blocks.
4. **Cue tour** before practice so first-block cue novelty doesn't differ by condition.
5. **Practice = None** (was PB) so no condition gets a familiarity advantage.
6. **Hazard meshes hidden** in all conditions from frame 0; only the Visual condition renders anything.

---

## 5. Technical architecture

**The invariant (do not break):** the science is decoupled from hardware by interfaces, so the "brain" runs and
is unit-tested with **no trackers, no headset, no haptic device**.

```
Assets/CollisionFeedback/
  Core/       (asmdef CollisionFeedback.Core)     pure logic + math. NO MonoBehaviours, NO scene access,
                                                  NO I/O, NO device calls. UnityEngine math types only.
  Runtime/    (asmdef CollisionFeedback.Runtime)  MonoBehaviours + device glue. Depends on Core.
  Integration/(no asmdef → Assembly-CSharp)       code that must see 3rd-party SDKs (bHaptics) AND Core/Runtime.
  Tests/EditMode/ (asmdef CollisionFeedback.Tests) EditMode unit tests; references Core only. Zero hardware.
```

**The three seams** (`Core/Contracts.cs`) — these are the integration points for any other project:

| Interface | Purpose | Real impl | Test impl |
|---|---|---|---|
| `IKeypointSource` | tracking **in** — `bool TryGetFrame(out PoseFrame)` | `TrackerKeypointSource` (reads a `BodyTrackerRig`) | `MockKeypointSource`, `KeypointDeserializer` (CSV replay) |
| `IFeedbackSink` | feedback **out** — `void Fire(in FeedbackCommand)` | `BHapticsSink`, visual renderer | `RecordingSink`, `CountingSink` |
| `IClock` | wall-clock seam for logging | `SystemClock` | fake |

`PoseFrame` = `{ double Timestamp; Vector3[] Joints }` indexed by `enum Joint { Head, Chest, LeftHand,
RightHand, LeftFoot, RightFoot }`. **Core logic is driven by `PoseFrame.Timestamp` (the data clock), never wall
time** — that is what makes it deterministic and replayable.

**Gotcha:** `UnityEngine` defines its own `Joint` (physics). Any file outside the `CollisionFeedback.Core`
namespace that uses both `UnityEngine` and `CollisionFeedback.Core` must add
`using Joint = CollisionFeedback.Core.Joint;` or it is CS0104-ambiguous.

### Key Core classes
| Class | Responsibility |
|---|---|
| `CollisionOracle` (+`OracleParams`) | per-limb distance + constant-velocity TTC (EMA velocity, α=0.5); latency compensation |
| `ConditionManager` | the 6 condition rules; edge-triggered with hysteresis (one alert per approach); 1.0 s global debounce; routing to `HapticSite` |
| `CollisionDetector` (+`DetectorParams`) | outcomes: collisions (≤ 0.03 m), near-misses (≤ 0.12 m), min clearance; per-engagement edge triggering; optional per-limb `LimbContactRadius` |
| `OpportunityScheduler` (+`OpportunitySchedules.Layout1`) | the 12 scripted opportunities; `ActiveFor(limb)` attributes a collision to the open opportunity |
| `Layout1Stimuli` | spawn positions/kinds for the 12 events |
| `LayoutVariants` | the L1–L6 + LP isometry engine (point/extent transforms, limb mirroring, per-variant schedules and stimuli) |
| `BlockRunner` | ticks scheduler + condition + detector in lockstep; produces `BlockResult` |
| `SessionPlan` / `WilliamsSquare` | 6×6 Williams-square counterbalancing + layout rotation |
| `AvoidanceLatencyDetector` | time from alert → the limb starts retreating |
| `Questionnaire` | IPQ (14) / NASA-TLX (6) / SSQ (16) / **CUE acceptability (4)** items + scoring → canonical measures |
| `TrackingNoise` | deterministic seeded jitter + fixed per-joint bias injected into recorded frames |
| `WarningFidelity` | offline robustness: warnings from **degraded** tracking vs ground truth from **clean** tracking → detection rate, lead time, false alarms |
| `CueIntensityTable` | per-`HapticSite` gains (0..1) |
| `Staircase` / `CueIntensityCalibration` | adaptive 2AFC staircase (PSE) for perceptual cue-intensity matching |
| `RigidTransformSolver` / `RigidAlignment` | Horn's-method rigid fit **with residual reporting** (Vicon↔Unity alignment) |
| `VisualAlertModel` | **unusable as written** — continuous proximity-graded glow + distance-driven pulse rate. `PAPER1_STUDY_DESIGN.md` §2 requires the Visual benchmark to be *transient* and driven by *PB's predictive trigger*. Must be rewritten or the Visual anchor dropped |
| CSV formatters | `CsvFormatter`, `EventLogFormatter`, `KeypointLogFormatter`, `QuestionnaireFormatter` |

### Key Runtime / Integration components
| Component | Responsibility |
|---|---|
| `SessionRunner` (Integration) | **the production driver** — full session orchestration, gates, questionnaires, cue tour, deferred scoring, layout application, CSV writing |
| `LiveSessionController` (Integration) | single-block debug driver (disabled in the scene) |
| `BodyTrackerRig` (Runtime) | 6 Transform slots (head, chest, L/R hand, L/R foot) → `CreateSource()` |
| `ControllerTrackerStandIn` (Runtime) | **interim**: fills the rig from HMD + 2 controllers, synthesizing chest/feet proxies |
| `ViconBodyRig` (Runtime) | fills the rig from Vicon Transforms via a calibrated `RigidAlignment`; head still from the HMD |
| `ViconCalibrationRecorder` (Runtime) | live Vicon↔Unity co-registration with a pass/fail residual readout |
| `OpportunitySpawner` (Runtime) | spawns orbs/projectiles per the variant's stimulus set; primitive fallback needs no art |
| `VisualObstacleAlert` (Runtime) | hides all hazard meshes; renders the Visual-condition glow |
| `OperatorEStop` (Runtime) | red button + `Esc`, latching veil, `estop_log.csv`, UnityEvent for passthrough |
| `QuestionnairePanel` (Runtime) | IMGUI questionnaire administration |
| `HapticDeviceBinding` (Integration) | the only file touching the bHaptics SDK; 3-pulse cue (3×100 ms, 60 ms gaps) |
| `CueIntensityCalibrationRunner` (Integration) | the **cue-intensity perceptual-matching** session; writes `cue_intensity.csv`. Historically called "E2" in prototype comments — **rename in code**: `E1`/`E2` now denote Paper 2's stop-signal and lead-time experiments, and the collision will mislead anyone reading both documents |
| `RobustnessAnalysisRunner` (Runtime) | replays a recorded `keypoints_*.csv` through the warning logic at increasing tracking error → `robustness_analysis.csv`; needs no hardware |

---

## 6. Data outputs

Target schema: written to a unique immutable `<persistentDataPath>/sessions/P<id>/<session_id>/` directory with
overwrite protection. This is a requirement, not a claim that the present prototype already produces it.

| File | Schema / contents |
|---|---|
| `opportunities.csv` | one row per planned opportunity: assignment, target limb/hazard, presented, valid, invalid reason, binary primary violation, secondary contacts/clearance, tracking quality |
| `summary.csv` | derived block summaries; never the sole source for the primary outcome |
| `events.csv` | one chronological stream of opportunity, alert, cue-onset, response, outcome, tracking, and operator events, including distance and TTC |
| `keypoints_<tag>.csv` | raw per-frame joint positions with source timestamp, sequence, receive time, and quality/status fields |
| `questionnaire.csv` | long format: `participant, block, condition, instrument, measure, value`; instruments `IPQ`/`NASA_TLX`/`SSQ`/`CUE`; canonical measures `presence`, `overall`, `total`, `acceptability` + `timely` (block = −1 for session-level; `CUE` absent for None by design) |
| `robustness_analysis.csv` | offline degradations for bias, structured jitter, latency, frame rate, dropout/freeze, and filter delay; sensitivity analysis, not device certification |
| `practice_*` | practice block, kept separate and excluded |
| `estop_log.csv` | `utc, reason, context` (persistentDataPath root) |
| `cue_intensity.csv` | per-site gains from pre-study perceptual calibration |
| `cue_calibration_log_P###.csv` | per-trial staircase record |
| `vicon_alignment.txt` | 4×4 Vicon→Unity matrix + residual comment |

**Required analysis** (`Analysis/`, R): the Paper 1 primary model is an opportunity-level logistic mixed model
with `Policy*Mapping`, fixed layout/period/event nuisance terms, and participant intercept/slopes. Do not adjust
the primary total-effect model for alerts or delivered lead time. Separate preregistered models handle anchors,
experience outcomes, mechanism, and sensitivity checks. Power simulation and schema-exact mock-data recovery
must pass before launch. The retained partial R script is not the design authority.

---

## 7. Hardware configuration

- **HMD:** VIVE Focus Vision as PC-VR via SteamVR + VIVE Streaming; SteamVR is the OpenXR runtime; Unity OpenXR
  + XR Interaction Toolkit 3.3.2 (`XR Origin (XR Rig)` prefab from Starter Assets).
- **Haptics (selected 2026-09-07):** bHaptics **TactSuit** vest + **Tactosy** units on forearms and feet.
  Requires the bHaptics Player desktop app + `[bhaptics]` prefab in the scene.

  **Split the two jobs across the two device families** — this is what keeps H2 interpretable:

  | Channel | Hardware | Notes |
  |---|---|---|
  | **Warning cue** (manipulated factor) | Tactosy only, including one at the **torso** for the generic condition | Same device family at every cue site. Preserves an anatomical-mapping claim. |
  | **Task feedback** (projectile hits) | TactSuit vest | Identical in all conditions, carries no hazard information — permitted under §10 constraint 3 |

  `HapticSite` values become Chest (Tactosy), LeftHand, RightHand, LeftShin, RightShin.

  **DECIDED 2026-09-07 — Tactosy at torso.** Five Tactosy units carry every warning cue: sternum, both
  forearms, both feet. The vest never delivers a warning. **H2 remains an anatomical-mapping hypothesis** and
  keeps the strong construct name.

  Required consequences, detailed in `PAPER1_STUDY_DESIGN.md` §2:
  - **Constrain projectile-hit feedback to vest regions away from the sternum** — back panel preferred — so
    the two torso channels are spatially separated and the discrimination check passes comfortably.
  - **Per-site salience calibration is now load-bearing**, because a sternum unit worn with the vest is not
    coupled identically to a directly strapped forearm unit. Do not skip it to save session time.
  - **Fallback:** if discrimination is marginal at pilot, drop vest haptics and use audio-visual projectile
    feedback.
  - **Procurement:** five Tactosy units. Arm and foot units ship in pairs, so the torso unit is most easily a
    spare arm-class unit — which is also what keeps the actuator family matched.
- **Tracking — decided by paired pilot comparison (2026-09-08, PI discussion).** Both candidate systems are
  built and logged **simultaneously** during pilot sessions on the same participants and the same movements;
  the selection is then made per paper against criteria fixed and dated beforehand. Procedure, criteria, and
  the rationale for simultaneous rather than sequential logging: `PAPER1_STUDY_DESIGN.md` §11.

  | Option | Status |
  |---|---|
  | Controllers as stand-in | Protocol validation only — foot events are not real foot tracking |
  | VIVE Ultimate Trackers | **Candidate.** Inside-out; wrist-mounted, so a wrist-to-hand offset is required |
  | Vicon | **Candidate.** Sub-millimetre, frees the hands; requires Vicon→Unity co-registration |

  **Both systems must work before the pilot** — this front-loads engineering rather than reducing it.
  A **split outcome is permitted**: commodity trackers may prove adequate for Paper 1's binary contact
  outcome and inadequate for Paper 2's millisecond threshold. Do not force one system across both papers.

  The velocity analysis below is not withdrawn — it is the **prediction the pilot tests.**

  > **Why Vicon, stated properly this time.** Position error propagates into *velocity* error amplified by
  > roughly `1/(Δt·√2)` — about **70× at 100 Hz**. Both papers need velocity: Paper 1's predictive policy
  > computes TTC from closing speed, and Paper 2's entire deliverable is a lead-time threshold plus a
  > kinematic account. Two consequences made inside-out tracking untenable:
  >
  > 1. **Paper 2 becomes unmeasurable.** Filtering trades noise against lag as √N versus ~N/2, so reaching
  >    usable velocity precision costs hundreds of milliseconds of delay — in an experiment whose entire
  >    range is 100–800 ms.
  > 2. **Paper 1's H1 acquires a directional confound.** Predictive needs velocity; proximity needs only
  >    distance. Velocity noise degrades one arm of the comparison and not the other, biasing H1 *against*
  >    prediction and making a null uninterpretable.
  >
  > The original handoff rationale — that Vicon "genuinely realizes the idealized oracle" — was correct.
  > Neither paper is a deployment study, so the commodity-hardware framing bought nothing. The 34-home
  > deployment is a separate, later study.

  **Components back on the critical path:** `ViconBodyRig`, `ViconCalibrationRecorder`, `RigidTransformSolver`
  / `RigidAlignment`, `vicon_alignment.txt`, and `docs/VICON_SETUP.md` — the last of which is **among the
  files currently missing from disk**. Co-registration risk is lower than it looks: the rigid-alignment solver
  is already verified to recover known transforms exactly and to report residuals that track injected noise.

  **The tracking risk profile is marker integrity, not sensor noise.** Occlusion (a coverage problem, solvable
  by camera placement), marker swapping/mislabelling under fast motion (worse than dropout — it yields
  confident wrong data), and physical detachment during vigorous dodging. Target **≥ 200 Hz**. All of it must
  be piloted at full speed, never on a stationary participant. See `PAPER1_STUDY_DESIGN.md` §11.

  **Hands are tracked directly**, so no wrist-to-hand offset is needed — one fewer frozen parameter and one
  fewer source of silent bias in the 0.15 m orb radius and the contact decision.

  **`ControllerTrackerStandIn`** stays available for protocol validation. Because Vicon frees the hands,
  decide whether participants hold one controller for questionnaire input only — preferred, so they answer
  privately — or whether the operator enters spoken responses. `QuestionnairePanel` and the operator gates
  need whichever is chosen.

  **Simultaneous logging — REINSTATED for the pilot only (2026-09-08).** Rejected on 2026-09-07 as a
  confirmatory-study feature, on scope grounds: forearm space is contested by the Tactosy unit and markers,
  and the project carries no ethics approval. **That judgement still holds for the 36+ confirmatory
  sessions.** But the paired comparison is exactly what the pilot needs, and the mounting complexity is
  acceptable across a handful of sessions.

  Two by-products worth having: it characterizes commodity-tracker error against a reference **on real task
  movement**, which strengthens the robustness analysis whichever system is selected; and it lets the
  wrist-to-hand offset be **measured** rather than taken from anthropometric tables, should commodity
  trackers be chosen.

---

## 8. Current status

**Readiness verdict:** no-go for piloting or human data collection.

The 2026-08 “complete and verified” claim was not supported by the retained project evidence. The inspected Unity
project contained a partial prototype, one synthetic build scene, missing production/session components, no
current all-green test artifact, and no Vicon/bHaptics end-to-end validation. Treat every component list in this
handoff as a target architecture until it is located, reviewed, scene-wired, and retested.

**Required before Paper 1 implementation can be certified:**

- implement the protocol in `PAPER1_STUDY_DESIGN.md`, including opportunity-level attribution and corrected
  policy geometry;
- implement and audit crossed condition-order × layout allocation;
- implement physical haptic onset measurement, per-site salience calibration, and the trigger-matched visual cue;
- complete immutable chronological data logging, questionnaires, invalid-trial reasons, and overwrite protection;
- add current EditMode, PlayMode, replay, session, logging, and hardware-in-loop tests;
- wire and validate Vicon/Unity timing and alignment, haptics, tracking loss, and e-stop behavior;
- install and validate the frozen analysis environment and simulation-based power code;
- obtain PI sign-off, approved ethics/safety/data plans, and preregistration; and
- pass engineering, behavioral-pilot, analysis-recovery, and launch gates.

The six layout variants may control location memory but do not represent six independently authored rooms. State
that limitation and treat layout as a fixed nuisance factor in the planned analysis.

---

## 9. Literature positioning (as of 2026-08; not an exhaustive review)

**Closest prior work — must be cited and differentiated:**
- **"Belt and whistles — adding lower body collision awareness for MR experiences" (CHI 2026)** — a wireless
  haptic **belt** giving **directional** signals about **virtual** obstacles on a Quest 3; improves spatial
  awareness and reduces unintended collisions. *Differences:* it encodes direction on the torso rather than
  cueing the at-risk limb; it does not manipulate warning **timing**; it demonstrates that a system works rather
  than isolating which design dimension does the work. **It also establishes A*-venue precedent for
  virtual-obstacle collision studies.**
- **Samsel et al. (2025), *Applied Ergonomics*** — vibrotactile **patterns** (point/column/wave) on a vest for
  obstacle early warning. Manipulates pattern design, not timing × localization.

**The claimed gap (audited 2026-08).** Prior work demonstrates *that* haptic warning helps (Belt & Whistles;
VRCAT, *The Visual Computer* 2022; haptic-vest navigation studies), compares *patterns* (Samsel 2025), or
compares *modalities* (visual/audio/haptic). Repeated targeted searches found **no factorial study crossing
warning timing (TTC vs proximity) with cue localization (generic vs at-risk limb)** — that crossing, plus the
tracking-noise robustness curve, is the contribution. *Caveat: these were targeted searches, not a systematic
review; run a full related-work sweep before submission.*

**Venue plan (re-verified 2026-09-06).** Checked directly against the official sites:

- **IEEE ISMAR 2026 — Bari, Italy, 5–9 October 2026.** Abstracts were due 9 March 2026 and full papers
  16 March 2026. **Both deadlines have passed.** ISMAR 2026 is not reachable.
- **IEEE ISMAR 2027 — host city and dates are not yet announced** on `ieeeismar.net`. The site currently
  redirects to the 2026 edition and contains no 2027 information.

**The "Kobe, Japan, October 11–15, 2027" claim is unverified and should not be used for planning.** No official
IEEE ISMAR source states it. Searches for it surface **IEEE/SICE SII 2027**, a different conference that *is*
held in Kobe — the same class of confusion as the earlier Lyon error, where `ismar.org` turned out to be the
International Society of Magnetic Resonance (Lyon, July 2027, jointly with EUROMAR), not the IEEE symposium.
Two different conferences have now been mistaken for IEEE ISMAR 2027; treat any unsourced venue claim as
suspect until it appears on `ieeeismar.net`.

**Planning inference, not an authority.** ISMAR 2026 ran abstracts in early March for an early-October
conference. If 2027 follows the same pattern, the submission deadline is likely **around March 2027**,
roughly six months out. Plan against that provisionally, re-check `ieeeismar.net` monthly, and note that
this timeline is extremely tight given that ethics approval has not been submitted.

Venue targeting never overrides ethics, pilot, or preregistration gates.

**Supporting precedent for the virtual-hazard method** (detail in `docs/VIRTUAL_HAZARD_VALIDITY.md`):
Fink, Foo & Warren 2007 (people avoid virtual obstacles like real ones, with larger clearance); a 2021 IEEE
study of purely virtual walls (same collision operationalization; **engagement is the moderator** — 10%
collided in an engaging task vs 52.5% in a boring one); Meehan et al. 2002 and Slater 2009 (realistic response
to virtual threat); Insko 2001 (passive haptics amplifies response → our absolute rates are attenuated).

---

## 10. Integration notes — combining this with another project

**The clean seams.** Anything can be swapped at these boundaries without touching the science:
- **Tracking in:** implement `IKeypointSource` (or fill a `BodyTrackerRig`'s six Transforms). This is how the
  controller stand-in, VIVE trackers, and Vicon all coexist.
- **Feedback out:** implement `IFeedbackSink`. `FeedbackCommand` carries `{ Site, Modality, Trigger, Limb,
  ObstacleId, DataTime, Distance, Ttc }`.
- **Hazard geometry:** `SceneObstacles.Collect()` reads scene `BoxCollider`s into `Obstacle` records
  (`Id`, `Center`, `Extents`). Any other geometry source can produce that list.
- **Session orchestration:** `SessionRunner` is the driver; `BlockRunner` is one block; either can be replaced
  while keeping the Core logic.

**What is reusable vs study-specific**
- *Reusable:* the whole `Core` assembly (oracle, detector, condition rules, staircase/calibration, layout
  isometries, rigid alignment, CSV formatters), plus the Vicon and bHaptics bindings.
- *Study-specific:* the 12-event Layout-1 schedule, the 6 conditions, the Williams counterbalancing, the
  questionnaire battery, and the deferred-penalty scoring.

**Constraints any integration must respect**
1. `Core` must never reference MonoBehaviours, I/O, networking, or device SDKs.
2. Core decisions must be driven by `PoseFrame.Timestamp`, never wall time.
3. **No per-event *hazard* feedback of any kind** (audio/visual/haptic/score) outside the manipulated
   conditions — it would destroy the None condition. *Task* feedback is fine and required: an orb vanishing
   when collected, or a projectile registering a hit, is identical in every condition and reveals nothing about
   hazard locations. The line is whether the signal carries hazard information.
4. Hazard meshes stay hidden in all conditions except the Visual glow.
5. The cue waveform must stay identical across haptic conditions; only timing and site may vary.
6. Anything added to a block must not leak condition identity to the participant.

**Open question for the integrator:** this document does not know what the other project is. The most likely
integration points are (a) a different tracking source, (b) a different hazard/environment generator, (c) a
shared participant/session pipeline, or (d) a different predictor replacing the constant-velocity oracle
(`CollisionOracle` is deliberately swappable behind the same shape — a Kalman filter or learned predictor can
replace the EMA without touching `ConditionManager`).

---

## 11. Document map

| File | What it holds |
|---|---|
| `PAPER1_STUDY_DESIGN.md` | **Paper 1 design authority** — confirmatory design, outcomes, analysis, launch gates |
| `PAPER2_STUDY_DESIGN.md` | **Paper 2 design authority** — tSSRT, lead-time × tempo experiment, analysis, launch gates |
| `COMBINED_STUDY_DESIGN.md` | program relationship, sampling separation, publication strategy, plain-language summary |
| `ISMAR_DISCLOSURE_LETTER.md` | conditional related-work disclosure template; facts must be finalized after sampling |
| `docs/STUDY_DESIGN_V2.md` | historical virtual-hazard rationale; superseded where it conflicts with Paper 1 authority |
| `docs/STUDY_OVERVIEW.md` | plain-language study description (partly superseded by v2) |
| `docs/VIRTUAL_HAZARD_VALIDITY.md` | literature grounding for virtual-only hazards |
| `docs/VICON_SETUP.md` | Vicon co-registration procedure + notebook analysis |
| `docs/PILOT_SETUP_GUIDE.md` | click-by-click Unity/VR setup |
| `docs/SESSION_CHECKLIST.md` | printable operator + spotter session checklist |
| `docs/SAFETY_PROTOCOL.md` | IRB draft (still contains obsolete foam sections) |
| `IMPLEMENTATION_PLAN.md` | phased engineering plan + parameter appendix |
| `PRE_PILOT_PLAN.md` | task/ownership view |
| `next_steps.md` | **live** running list of what to do next |
| `CLAUDE.md` | architecture rules for AI agents working in this repo |

> **Missing files — verify before relying on this map.** A 2026-08-31 inspection of the repo root
> (`WhenandWheretoWarn/My project (1)/`) found `CLAUDE.md` and `Analysis/` present but **no `docs/` directory**,
> and none of `IMPLEMENTATION_PLAN.md`, `PRE_PILOT_PLAN.md`, or `next_steps.md`. Either they live elsewhere or
> they have been lost.
>
> This is urgent for one file in particular: **`docs/SAFETY_PROTOCOL.md` is the IRB draft, and ethics approval
> is the critical path for both papers.** Locate it, or rewrite it against the current Paper 1 and Paper 2
> authorities — which is in any case necessary, since the retained draft predates the virtual-hazard redesign
> and still contains foam-obstacle sections.

## 12. Key parameters (code defaults)

These are historical prototype defaults, not approved scientific constants. The paper-specific protocol and
pilot must justify/freeze them. In particular, zero latency compensation, frame-dependent EMA, global debounce,
and point-joint contact are not acceptable confirmatory defaults.

| Parameter | Value | Source |
|---|---|---|
| Reactive distance D | 0.30 m | `OracleParams` |
| Predictive TTC T | 0.50 s | `OracleParams` |
| Reactive/predictive release margins | +0.15 m / +0.20 s | `OracleParams` |
| Pipeline latency compensation | 0 s (**must be physically measured and replaced**) | `OracleParams` |
| EMA velocity α / min closing speed | 0.5 / 0.05 m/s (**replace with time-constant-aware filtering**) | `CollisionOracle` |
| Global re-fire debounce | 1.0 s (**replace with per limb–hazard engagement state**) | `ConditionManager` |
| Contact / near-miss / exit margin | 0.03 / 0.12 / 0.05 m (**freeze a validated limb-volume model**) | `DetectorParams` |
| Haptic cue | 3 × 100 ms pulses, 60 ms gaps | `HapticDeviceBinding` |
| Block / practice / break | 180 s / 90 s / ≥30 s | `SessionRunner` |
| Opportunities per block | 12 @ 8…152 s, 6 s window | `OpportunityScheduler` |
| Collision penalty | −3 points, deferred | `SessionRunner` |
| Orb touch / projectile hit radius | 0.15 m / 0.25 m | `OpportunitySpawner` |
| Layouts | L1–L6 isometries + LP practice | `LayoutVariants` |
| Robustness sweep σ levels | 0, 0.005, 0.01, 0.02, 0.03, 0.05, 0.10 m | `RobustnessAnalysisRunner` |

*Created 2026-08-11; audited and subordinated to the paper-specific design authorities on 2026-09-05;
cross-checked against those authorities on 2026-09-06.*

**2026-09-06 cross-check — unresolved items carried into the design authorities:**

1. **Actuator confound (blocking).** TactSuit torso + Tactosy limbs bundles device with body site; Paper 1's
   H2 cannot be read as anatomical mapping until resolved. `PAPER1_STUDY_DESIGN.md` §2.
2. **`VisualAlertModel` unusable (blocking).** Continuous proximity glow, where the protocol requires a
   transient predictive-trigger-matched cue. Rewrite or drop the Visual anchor.
3. **`E1`/`E2` naming collision.** Prototype code uses "E2" for cue-intensity calibration; Paper 2 uses
   `E1`/`E2` for its two experiments. Rename in code before anyone reads both.
4. **`docs/` directory missing**, including the IRB draft `SAFETY_PROTOCOL.md`, which is the critical path.
5. **ISMAR 2027 venue unannounced**; the previously recorded Kobe dates were unverified and appear to belong
   to a different conference.
6. **Legacy session flow and the 50–60 min estimate are superseded** by `PAPER1_STUDY_DESIGN.md` §8.
