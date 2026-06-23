# CLAUDE.md — When and Where to Warn (VR collision-feedback study)

Unity 6.3 LTS (`6000.3.16f1`), URP, new Input System. This project implements the controlled VR
user study **"When and Where to Warn: Predictive, Body-Localized Tactile Cues for Real-Obstacle
Collision Avoidance in VR"** (IEEE VR 2027 / TVCG track).

## What the study is (one paragraph)
A within-subjects **2x2 (Timing x Localization)** + a **no-feedback floor** + a **visual reference**,
isolating whether **predictive** (fire on forecast time-to-collision) and **body-localized** (cue the
specific at-risk limb) vibrotactile feedback cut real-obstacle collisions while preserving presence.
6 conditions: **None, RG, RB, PG, PB, Visual** (PB = full technique). The predictive signal comes from
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

## Condition rules (Core/ConditionManager.cs)
- **None** → never fires.
- **RG / RB / Visual** → Reactive: fire when `closing && minDistance < D`.
- **PG / PB** → Predictive: fire when `closing && minTtc < T`.
- Routing: **RB, PB, Visual** are localized (cue the at-risk limb's site); **RG, PG** cue the Chest.
  `Visual` uses `Modality.Visual`; all others `Modality.Haptic`.
- Edge-triggered with hysteresis: at most ONE alert per approach (re-arms when the limb clears
  `ReleaseDistance`), so alert counts stay meaningful as a covariate.
- The oracle (`Core/CollisionOracle.cs`) is a **deliberately simple constant-velocity TTC** estimator
  (EMA velocity), NOT a learned/SOTA predictor — the manipulated variable is *timing*, not predictor
  quality. A Kalman filter can replace the EMA behind the same shape later.

## How to run the tests
Unity → **Window ▸ General ▸ Test Runner ▸ EditMode ▸ Run All**. Everything under `Tests/EditMode`
runs headless (no Play mode, no devices). Add a test for every new Core behavior before wiring it to
hardware. To see the brain run live: drop `ExperimentRunner` (Runtime) on an empty GameObject, pick a
Condition in the Inspector, press Play, read the Console for `[FEEDBACK]` lines.

## Build order (who does what)
1. **Brain + mock + tests** — DONE (all hardware-free logic complete & tested): oracle (+latency-compensation), 6 conditions, routing, edge-trigger, collision/near-miss detector (+Flush, +CurrentDistances), opportunity scheduler, `BlockRunner` (collisions→opportunities + avoidance latency → `BlockResult`), `SessionPlan`/`WilliamsSquare` (counterbalancing), `AvoidanceLatencyDetector`, `TactorArbiter`, `TrackerKeypointSource`+`BodyTrackerRig` (VIVE Ultimate Tracker source), `KeypointDeserializer` (CSV replay), `EventLogFormatter`/`KeypointLogFormatter`+writers, `BHapticsTactorMap`+`BHapticsSink`+`HapticDeviceBinding` (live bHaptics, hardware-validated), `SyntheticBlock`+rewired `ExperimentRunner` (end-to-end CSV demo), `Analysis/*.R`. Extend here. *(Claude)*
2. **Scene** — arena, XR Rig, foam-obstacle GameObjects at surveyed Layout-L1 coords, the orbs/dodge task. *(human, in Editor)*
3. **Wire real devices** — tracking DONE: `TrackerKeypointSource` reads the `BodyTrackerRig` (VIVE Ultimate Trackers; already in the VR frame, **no camera↔VR calibration**). Remaining: the bHaptics sink + the scene tracker rig. *(human + Claude)*
4. **Logging, opportunity scheduler (12 events/block), latency, pilot.** *(Claude logic, human runs it)*

## Conventions
- C# 9 (Unity 6.3). One public type per file where practical; tiny enums/interfaces grouped.
- **`Joint` gotcha:** `UnityEngine` defines a `Joint` (physics) type. Any file outside the `CollisionFeedback.Core`
  namespace that `using`s both `UnityEngine` and `CollisionFeedback.Core` must add
  `using Joint = CollisionFeedback.Core.Joint;` or `Joint` is ambiguous (CS0104). Core files are fine
  (same-namespace type wins over a using-imported one).
- Don't add packages or touch `ProjectSettings`/scenes from code without saying so — that's Editor work.
- Distances in meters, time in seconds, world frame = OpenXR / SteamVR (the VIVE trackers + HMD share it natively — one PC, one tracking space).
