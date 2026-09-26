# CLAUDE.md — What Makes a Collision Warning Work? (VR collision-feedback study)

Unity 6.3 LTS (`6000.3.16f1`), URP, new Input System. This project implements the controlled VR
user study **"What Makes a Collision Warning Work? Separating Trigger Timing, Body Site, and Cue Form in VR"** (ISMAR 2027).

## What the study is (one paragraph)
A within-subjects **2x2 (Timing x Localization)** + a **no-feedback floor** + a **continuous-cue
comparison**, isolating whether **predictive** (fire on forecast time-to-collision) and **body-localized**
(cue the specific at-risk limb) vibrotactile feedback cut real-obstacle collisions while preserving presence.
6 conditions: **None, RG, RB, PG, PB, PBC** (PB = full technique; **PBC** = PB's trigger with a
CONTINUOUS intensity ramp, carrying H4'). `Visual` is implemented and tested but **no longer scheduled**
-- swapped out for PBC on 2026-09-23 so the paper can answer Valkov & Linsen (IEEE VR 2019), who found
forecast triggering WORSE but confounded it with a continuous mapping participants could modulate. The predictive signal comes from
an idealized 6-DoF tracking **oracle** (VIVE Ultimate Trackers, one per limb) that computes per-limb TTC.

Design/protocol authority lives in the docs folder (read these before changing behavior):
`C:\Users\faisa\OneDrive\Desktop\IEEEVR Project Ideas\IEEEVR2027_StudyDesign.md` (and `_StudyProtocol`,
`_Implementation_Guide`, `_Layout1_Storyboard`, `_Pilot_Design`, `_Risk_Register`).

## The architecture rule (do not break this)
The science is decoupled from the hardware by **interfaces**, so the brain runs and is unit-tested with
NO trackers, NO headset, NO bHaptics device.

```
Assets/CollisionFeedback/
  Core/      (asmdef CollisionFeedback.Core)    pure logic + math. NO MonoBehaviours, NO scene access, NO I/O, NO device calls.
  Runtime/   (asmdef CollisionFeedback.Runtime) MonoBehaviours + device glue. Depends on Core (+ later bHaptics/OpenXR).
  Tests/EditMode/ (asmdef CollisionFeedback.Tests) EditMode unit tests; references Core only. Run with zero hardware.
```

The three seams (in `Core/Contracts.cs`):
- `IKeypointSource` — tracking in. Real = VIVE Ultimate Trackers via SteamVR (`TrackerKeypointSource` reading a `BodyTrackerRig`, Runtime); dev/test = `MockKeypointSource` (Core).
- `IFeedbackSink`  — feedback out. Real = bHaptics / in-HMD visual (Runtime); test = `RecordingSink` (Tests).
- `IClock`         — wall-clock seam for the Runtime logger (Core logic uses `PoseFrame.Timestamp`, never wall time).

**Invariant:** `Core` must never reference `UnityEngine` MonoBehaviours, networking, the file system, or
any device SDK. If logic needs the outside world, add a method to one of the three interfaces and
implement it in `Runtime` (or a fake in `Tests`). Keep Core deterministic (driven by frame timestamps),
so tests are reproducible.

> **⚠ CODE-VS-DESIGN DRIFT.** Some rules below describe the code as it *was*. Match the design
> (`PAPER1_STUDY_DESIGN.md`), not this section. Full list in `docs/PAPER1_CODE_GAP.md`.
>
> **FIXED 2026-09-21 — H2 and H4 confounds closed:**
> - **Visual fires on the PREDICTIVE trigger** (was reactive; confounded H4 with timing). `TriggerFor` and
>   the `Tick` switch agree, and a test asserts Visual matches PB.
> - **Visual cue form** is now a transient, limb-anchored marker (`Runtime/LimbCueIndicator`), duration-matched
>   to the haptic train. `VisualObstacleAlert` glowed the *hazard* and is superseded — it gave the Visual
>   condition hazard location and proximity, which the haptic conditions never get.
> - **Chest warning cue is a sternum Tactosy, not the vest** (`BHapticsTactorMap`). The vest is now
>   unreachable from the warning-cue path, including the default branch, which used to fall back to it.
>
> **STILL OPEN:** the primary outcome is a **per-opportunity binary violation**, not a block count
> (CODE_GAP #1); contact geometry (#5); opportunity validity tracking (#6).

## Condition rules (Core/ConditionManager.cs)
- **None** → never fires.
- **RG / RB** → Reactive: fire when `closing && minDistance < D`.
- **PG / PB / PBC / Visual** → Predictive: fire when `closing && minTtc < T`.
  **Every predictive condition shares ONE trigger**, so each comparison isolates exactly one variable:
  PB vs PG = localization (H2); PB vs RB = trigger policy with cue form constant (H1);
  **PB vs PBC = CUE FORM with trigger constant (H4′)**.
- Routing: **RB, PB, PBC, Visual** are localized (cue the at-risk limb's site); **RG, PG** cue the Chest —
  which is a **sternum Tactosy**, never the vest (H2 device confound, `BHapticsTactorMap`).
  `Visual` uses `Modality.Visual` and renders through `Runtime/LimbCueIndicator`; all others `Modality.Haptic`.
- Edge-triggered with hysteresis: at most ONE alert per approach (re-arms when the limb clears
  `ReleaseDistance`), so alert counts stay meaningful as a covariate.
- **`PBC` is the one exception and it is a manipulation, not a leak.** It emits EVERY frame while engaged,
  with intensity from `ContinuousCueMapping` tracking current TTC. No edge trigger, no debounce. Because
  `TTC = distance / speed`, slowing down lowers the intensity — **that feedback loop is the thing under
  test** (Valkov & Linsen's own explanation for their contrary result), so do not "fix" it. Its onset is
  derived from the *same* effective threshold as PB's trigger, which is what keeps H4′ free of a timing
  confound; `ContinuousCueTests` asserts this. Alert burden for PBC is **engagement episodes** and
  **intensity-seconds** (`BlockRunner.AlertEpisodes` / `.CueDoseSeconds`), never the raw command count.
- The oracle (`Core/CollisionOracle.cs`) is a **deliberately simple constant-velocity TTC** estimator
  (EMA velocity), NOT a learned/SOTA predictor — the manipulated variable is *timing*, not predictor
  quality. A Kalman filter can replace the EMA behind the same shape later.

## How to run the tests
Unity → **Window ▸ General ▸ Test Runner ▸ EditMode ▸ Run All**. Everything under `Tests/EditMode`
runs headless (no Play mode, no devices). Add a test for every new Core behavior before wiring it to
hardware. To see the brain run live: drop `ExperimentRunner` (Runtime) on an empty GameObject, pick a
Condition in the Inspector, press Play, read the Console for `[FEEDBACK]` lines.

## Build order (who does what)
1. **Brain + mock + tests** — DONE (all hardware-free logic complete & tested): oracle (+latency-compensation), 6 conditions, routing, edge-trigger, collision/near-miss detector (+Flush, +CurrentDistances), opportunity scheduler, `BlockRunner` (collisions→opportunities + avoidance latency → `BlockResult`), `SessionPlan`/`WilliamsSquare` (counterbalancing), `AvoidanceLatencyDetector`, `TactorArbiter`, `TrackerKeypointSource`+`BodyTrackerRig` (VIVE Ultimate Tracker source), `KeypointDeserializer` (CSV replay), `EventLogFormatter`/`KeypointLogFormatter`+writers, `BHapticsTactorMap`+`BHapticsSink`+`HapticDeviceBinding` (live bHaptics, hardware-validated), `SyntheticBlock`+rewired `ExperimentRunner` (end-to-end CSV demo), `Staircase`+`CueIntensityCalibration`+`CueIntensityCalibrationRunner` (E2 perceptual cue-intensity matching → `cue_intensity.csv`, tested), `Analysis/*.R`. Extend here. *(Claude)*
2. **Scene** — arena, XR Rig, foam-obstacle GameObjects at surveyed Layout-L1 coords, the orbs/dodge task. *(human, in Editor)*
3. **Wire real devices** — tracking DONE: `TrackerKeypointSource` reads the `BodyTrackerRig` (VIVE Ultimate Trackers; already in the VR frame, **no camera↔VR calibration**). Remaining: the bHaptics sink + the scene tracker rig. *(human + Claude)*
4. **Logging, opportunity scheduler (12 events/block), latency, pilot.** *(Claude logic, human runs it)*

## Data outputs (both papers) — complete as of 2026-09-25

Every session writes an **immutable participant directory** ending with a checksum manifest.

| Paper 1 | Paper 2 (E2) |
|---|---|
| `session.json` | `session.json` |
| `opportunities.csv` · `events.csv` · `summary.csv` | `e2_trials.csv` · `events.csv` · `deviations.csv` |
| `keypoints_*.csv` | `trajectories_<block>.csv` |
| `questionnaire.csv` | `questionnaires.csv` |
| `estop_log.csv` | `timing_calibration.csv` · `cue_calibration.csv` |
| `analysis_manifest.json` | `analysis_manifest.json` |

**Three rules the writers enforce, easy to break by accident:**

- **The manifest is written LAST**, after every writer has closed. Hashing earlier certifies a
  half-written state that never existed on disk.
- **Questionnaires write items AND scores** (`kind` column). Scores cannot be decomposed back into
  items, so dropping the items loses them irreversibly at the moment of collection.
- **Nothing writes to the participant root.** §12 names participant-root append files a release
  blocker; `OperatorEStop.SetLogDirectory()` exists for exactly this reason.

`events.csv` (E2) is **non-trial only** — session/practice boundaries, breaks, tracking dropouts,
operator actions. Per-trial timing lives in `e2_trials.csv` and must not be duplicated; a test fails if
an event kind containing `warning`/`onset`/`boundary`/`movement`/`lead`/`response` is added.

## Conventions
- C# 9 (Unity 6.3). One public type per file where practical; tiny enums/interfaces grouped.
- **`Joint` gotcha:** `UnityEngine` defines a `Joint` (physics) type. Any file outside the `CollisionFeedback.Core`
  namespace that `using`s both `UnityEngine` and `CollisionFeedback.Core` must add
  `using Joint = CollisionFeedback.Core.Joint;` or `Joint` is ambiguous (CS0104). Core files are fine
  (same-namespace type wins over a using-imported one).
- Don't add packages or touch `ProjectSettings`/scenes from code without saying so — that's Editor work.
- Distances in meters, time in seconds, world frame = OpenXR / SteamVR (the VIVE trackers + HMD share it natively — one PC, one tracking space).
