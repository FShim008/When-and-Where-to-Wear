# IMPLEMENTATION PLAN — "When and Where to Warn"
### Predictive, Body-Localized Tactile Cues for Real-Obstacle Collision Avoidance in VR (IEEE VR 2027 / TVCG)

**Purpose.** A single, ordered, self-contained plan that takes the project to a **complete, comprehensive, bulletproof, publication-ready** within-subjects VR study. Each task is **problem → why it matters → solution → done-when**. Execute phases top-to-bottom.

**Status legend:** ☑ done · ◐ partial/built-not-validated · ☐ not started
**Owner tags:** `[ENG]` engineering · `[PI]` PI/design decision · `[IRB]` ethics/safety · `[STAT]` statistics

> **Architecture (2026-06-22): SINGLE PC, VIVE Ultimate Trackers.** The study switched from the 5-camera Azure-Kinect/Femto rig (separate capture PC + UDP bridge) to **VIVE Ultimate Trackers** read in-process. Everything now runs on **one VR PC**. The camera/UDP/2-PC/cam→VR-calibration code has been removed (recoverable from git history + the archived `AzureKinectUnity` repo). See [System overview](#system-overview).

> **Authority note.** The original design docs (`IEEEVR2027_StudyDesign`, `_StudyProtocol`, `_Implementation_Guide`, `_Layout1_Storyboard`, `_Pilot_Design`, `_Risk_Register`) are **not on this machine**. Parameters here are code-derived and **must be reconciled against those docs** (Task 0.1); where they disagree, the docs win.

---

## System overview

**One PC, one tracking space:**

```
ONE VR PC  (Unity project "When-and-Where-to-Wear" / CollisionFeedback)
  VIVE Focus Vision  ── wired DisplayPort PC-VR ──►  SteamVR (OpenXR runtime)
  5× VIVE Ultimate Tracker  ── wireless dongle ──►   SteamVR  (chest, L/R wrist, L/R ankle; head = HMD)
        │  (all share ONE SteamVR tracking space — no calibration to align)
        ▼
  BodyTrackerRig → TrackerKeypointSource (IKeypointSource, VR-frame, ~frame-rate)
        ▼
  LiveSessionController / SessionRunner
     → BlockRunner: oracle (TTC) + ConditionManager + CollisionDetector + OpportunityScheduler
     → IFeedbackSink → bHaptics (3-pulse) / Visual
     → session_summary.csv · session_events.csv · keypoints · questionnaire.csv
  bHaptics: TactSuit X40 (chest) + 4 Tactosy (hands + shins)
```

**Verdict:** the Core "brain" + the R analysis are mature and unit-tested; tracking is now a clean, low-latency, single-PC VIVE-tracker source; **session orchestration, counterbalancing, practice/breaks, and keypoint logging are done.** What remains: VR/tracker bring-up (Phase 1), obstacle-coordinate truth (Phase 2), the cue-salience confound (Phase 3), scene completion (Phase 4), questionnaires (Phase 5.5), a one-shot latency measure (Phase 6), detector tuning (Phase 7), and the human/ethics/stats work (Phase 0).

---

## Phase −1 — Foundation already in place

- ☑ **Single-PC VIVE Ultimate Tracker source** — `TrackerKeypointSource` + `BodyTrackerRig` (Runtime) read the 5 trackers + HMD directly in the VR world frame (no network, no calibration). `LiveSessionController`, `SessionRunner`, and `TrackingBench` rewired to it. **Old plan removed:** `UdpKeypointSource`, `CameraVrCalibration(+File)`, `RigidTransformSolver`/`RigidTransformSerializer`, the capture PC, UDP, firewall, clock-sync.
- ☑ **Session orchestration + counterbalancing** (Tasks 5.1, 5.2) — `SessionRunner` sequences an excluded practice block → the 6 **Williams-square-ordered** condition blocks (`SessionPlan.For`) with enforced breaks; one tracker source for the whole session; per-participant CSV folder `sessions/P###/` with **overwrite protection**; IMGUI operator console (Start/Next/Stop). *Only Layout-L1 geometry is authored; other layout ids fall back to L1 with a warning.*
- ☑ **Practice block + inter-block breaks** (Tasks 5.3, 5.4) — in `SessionRunner` (`practiceCondition`/`practiceSeconds`/`minBreakSeconds`; practice → separate `practice_*` files). *Break SSQ-gating depends on questionnaires (5.5).*
- ☑ **Raw keypoint logging** (Task 5.6) — `KeypointLogWriter` (AutoFlush) in `SessionRunner` + `LiveSessionController`.
- ◐ **Hand/foot → contact-surface offset mechanism, default-OFF** (Task 7.3): `DetectorParams.LimbContactRadius` (per-limb reach, m) subtracted from the keypoint→obstacle distance; **null/0 ⇒ unchanged**. Unit-tested. Now models the *tracker-mount → fingertip/toe* offset; enable once D6 is decided.
- ◐ **Questionnaire pipeline groundwork** (Task 5.5): `QuestionnaireFormatter` (Core) + `QuestionnaireLogWriter` (Runtime) + `SessionRunner.RecordQuestionnaire(...)` → `questionnaire.csv`; `analysis.R` reads it (instrument ∈ {IPQ, NASA_TLX, SSQ}). **Remaining:** the administration UI + item wording/scoring.
- ☑ **Git sync** — private GitHub repo (`FShim008/When-and-Where-to-Wear`), branch `main`. One canonical VR PC now; `git pull` there.

---

## Phase 0 — Authority, ethics & statistics (BLOCKING)

> Nothing downstream is valid until 0.1–0.3. No participant runs before then.

### Task 0.1 — Retrieve & ingest the authoritative design docs `[PI]` ☐
- **Problem.** The 6 design/protocol docs aren't on this machine; parameters are code-derived.
- **Solution.** Retrieve all six; copy into a repo `/docs`; reconcile every value in [Appendix A] against them and fix the code where they differ.
- **Done when.** `/docs` holds the six files; a "parameters reconciled" note lists each as *matches code* / *corrected to X*.

### Task 0.2 — IRB approval & written safety protocol `[IRB]` ☐
- **Problem.** Participants make *real* near-collisions with physical obstacles while blind in an HMD.
- **Solution.** Confirm IRB coverage; lock a written protocol with: **trained spotter** (stop-word + physical guard); **chaperone boundary** around Layout-L1; **foam-obstacle spec** (material/dimensions/padding/mounting); **cable management** (HMD + suit + tracker straps); **emergency stop** (verbal + a button that ends the block and unblanks); **SSQ pre/post** + stop criteria; **exclusion criteria** + consent.
- **Done when.** Signed protocol + IRB number in `/docs`; e-stop (Task 4.5) implemented and rehearsed.

### Task 0.3 — Power analysis & design sizing `[STAT][PI]` ☐
- **Problem.** 12 opportunities/block caps collisions ≤12/cell; strong cues push toward 0 → zero-inflation for the NB-GLMM. No power analysis (mock uses N=48 as a placeholder).
- **Solution.** Monte-Carlo power sim on the planned NB-GLMM over plausible rates/effects (seed from the Phase-8 pilot). Decide **N**, **#opportunities/block**, extra blocks. Extend `OpportunityScheduler` if more events/block are needed.
- **Done when.** `power_analysis.R` + report justify N at ≥80% power for the Timing×Localization interaction and the H4 (PB vs Visual) contrast.

### Task 0.4 — Lock the analysis plan & pre-registration `[STAT][PI]` ☐
- **Solution.** Finalize `analysis.R` (primary CPO NB-GLMM; FLOOR vs None; H4; presence/TLX/SSQ). **Pre-register** hypotheses, model, exclusions, stopping rule.
- **Done when.** Pre-registration submitted; `analysis.R` runs end-to-end on `simulate_mock_data.R` with no stubs.

---

## Phase 1 — VR + tracker bring-up

> Today, pressing Play yields a flat desktop window — XR is not enabled, and the tracker rig isn't built.

### Task 1.1 — Run the Focus Vision as PC-VR via SteamVR `[ENG]` ☐
- **Solution.** Connect the **Focus Vision over the Wired Streaming Kit (DisplayPort)** for low-latency PC-VR; SteamVR becomes the OpenXR runtime. In Unity, **Project Settings ▸ XR Plug-in Management ▸ enable OpenXR** for Standalone; resolve OpenXR validation. *(Wired, not wireless — latency matters for a timing study.)*
- **Done when.** Pressing Play renders the scene to the HMD.

### Task 1.2 — Pair the 5 Ultimate Trackers + assign roles `[ENG]` ☐
- **Solution.** Plug the **Wireless Dongle** into the PC; pair the **5 Ultimate Trackers** in VIVE Hub/SteamVR; assign each a body role (chest, L/R wrist, L/R ankle). Mount on the bHaptics suit, oriented so each tracker's cameras can see the room.
- **Done when.** All 5 trackers + the HMD report stable pose in SteamVR.

### Task 1.3 — Build the scene rig + `BodyTrackerRig` `[ENG]` ☐
- **Solution.** Add an **XR Origin** to `Dryrun.unity`. Get each tracker's pose into a Transform (OpenXR HTC Vive Tracker feature + a `TrackedPoseDriver` per tracker, or SteamVR pose components), parented under the XR Origin. Add a **`BodyTrackerRig`** component and assign its 6 slots: the **XR camera** for `head`, and the 5 tracker Transforms.
- **Done when.** `BodyTrackerRig.IsComplete` is true; the M4 `TrackingBench` shows all 6 joint spheres tracking 1:1.

### Task 1.4 — Build settings `[ENG]` ☐
- **Solution.** Add `Dryrun` (and the bench scene) to Build Settings. *(Code-side input audit already done — zero legacy `UnityEngine.Input`.)*
- **Done when.** The correct scenes build; no input exceptions.

---

## Phase 2 — Collision ground-truth (obstacle coordinates)

> The primary DV is collisions = tracker-vs-obstacle distance. With VIVE trackers the **frame alignment is free** (trackers + HMD share SteamVR space — no calibration). What remains is getting the **obstacle coordinates** right and the small **tracker-mount offset**.

### Task 2.1 — Reconcile obstacle coordinates to a single source of truth `[ENG][PI]` ☐
- **Problem.** `Dryrun` scene boxes disagree with the surveyed `Layout1Stimuli` coordinates.
- **Solution.** Adopt the **surveyed** coords (design doc / `Layout1Stimuli`) as truth; physically place the foam to match; set the scene `BoxCollider`s to those world positions/sizes (keep them **axis-aligned**), in the **same origin as the SteamVR play area / XR Origin**.
- **Done when.** Scene box centers/extents == surveyed coords == measured physical foam, in SteamVR world space.

### Task 2.2 — Verify tracker→obstacle registration `[ENG]` ☐
- **Solution.** With the rig live, have the participant touch a known obstacle corner; confirm the in-engine limb sits on it (the trackers + obstacles are already co-registered — this just validates mounting + the obstacle coords). Set the per-limb **mount offset** (tracker-on-strap → fingertip/toe) via `DetectorParams.LimbContactRadius` (Task 7.3 / D6).
- **Done when.** Touch test lands within tolerance; the mount offset is recorded.

---

## Phase 3 — Close the cue-salience confound

### Task 3.1 — Equalize perceived cue intensity across body sites `[ENG][PI]` ☐
- **Problem.** Generic (chest) cue = **40 X40 motors**; localized (hand/foot) cue = **3 Tactosy motors**, all at flat intensity 1.0. The **Localization factor is confounded with stimulus energy** — a reviewer reads your localization effect as "the chest just buzzes harder."
- **Solution.** (1) Per-participant **perceptual match** (method-of-adjustment or 2-up/1-down staircase, ~2–3 min) finding, per site, the device intensity yielding **equal perceived magnitude** vs a reference. (2) Store per-site multipliers; apply them via a per-`HapticSite` intensity in `HapticDeviceBinding` (the `Intensity` field exists; replace the flat 1.0). (3) Acknowledge spatial *extent* can't be equated — equate perceived intensity, report extent as inherent.
- **Done when.** A `CueIntensityCalibration` step writes per-site intensities; the live cue uses them; a methods paragraph documents it.

---

## Phase 4 — Complete the experimental scene  (all in `Dryrun.unity`)

### Task 4.1 — Live haptics in scene `[ENG]` ☐
- Add the `[bHaptics]` prefab; run the Player; set `useLiveHaptics = 1`. **Done when** RG/RB/PG/PB drive the correct tactor(s); None/Visual produce no buzz.

### Task 4.2 — Configure the dodge/orb task `[ENG][PI]` ☐
- `OpportunitySpawner` prefabs (`orbPrefab`, `projectilePrefab`, `aimTarget`) are null → assign them per the storyboard. **Done when** the 12 scheduled events produce the intended dodge demands at the right times.

### Task 4.3 — Visual condition renderer `[ENG]` ☐
- Add a `VisualObstacleAlert` over the obstacles. **Done when** Visual shows the proximity-graded highlight; haptic conditions don't.

### Task 4.4 — Protocol parameters in scene `[ENG]` ☐
- Set `blockSeconds = 180` (or the doc value); feed `condition/layoutId/blockIndex` from `SessionRunner`, not by hand. **Done when** length + IDs come from the plan.

### Task 4.5 — Emergency-stop & operator HUD `[ENG][IRB]` ☐
- A controller button + key that immediately ends the block, unblanks, and logs the abort. (`SessionRunner` already has a "Stop block" button + a live HUD — extend to a controller binding and a hard unblank.) **Done when** the e-stop halts a block within one frame and writes an `aborted` marker.

---

## Phase 5 — Session orchestration & instruments

- ☑ **Task 5.1 Counterbalancing**, ☑ **5.2 Automated runner**, ☑ **5.3 Practice**, ☑ **5.4 Breaks**, ☑ **5.6 Keypoint logging** — all built in `SessionRunner` (see Phase −1).

### Task 5.5 — Questionnaires (presence is in the title) `[ENG][STAT]` ◐
- **Problem.** Presence (IPQ), NASA-TLX, SSQ are a stated co-outcome but only the *data pipeline* exists (formatter + writer + `RecordQuestionnaire` + `analysis.R` schema). The **administration UI + items** are not built.
- **Solution.** Build the in-VR/desktop panels for **IPQ + NASA-TLX after each block** and **SSQ pre/post session**; on submit call `SessionRunner.RecordQuestionnaire(block, condition, "IPQ"/"NASA_TLX"/"SSQ", scores)` (writes `questionnaire.csv`, already consumed by `analysis.R`).
- **Done when.** Presence/workload/sickness are collected every block and flow into the analysis.

---

## Phase 6 — Latency (one-shot; clocks no longer an issue)

> One PC = **one clock** — the old cross-PC clock-sync problem is gone.

### Task 6.1 — Measure tracker→cue latency & set `pipelineLatencySeconds` `[ENG][PI]` ☐
- **Problem.** `OracleParams.PipelineLatencySeconds = 0`; predictive cues aren't lead-compensated. With VIVE trackers the chain is short (tracker → SteamVR → Unity → oracle → bHaptics actuation), but the **bHaptics actuation delay** still matters relative to T = 0.50 s.
- **Solution.** Measure the end-to-end latency once (e.g., a physical tap that's both tracked and instrumented, or injected-motion-to-buzz timing). Set `pipelineLatencySeconds` = measured median.
- **Done when.** A measured value (with spread) is documented and entered; predictive lead is verified against it.

---

## Phase 7 — Detector fidelity

### Task 7.1 — (Optional) smooth keypoints feeding the detector `[ENG]` ☐
- VIVE trackers are low-jitter, so this is likely unnecessary — but confirm on the M4 bench. If a contact band (0.03 m) is near the measured tracker jitter, add a light EMA/median filter to the detector input (separate from the oracle EMA). **Done when** no jitter-induced false engagements at the chosen thresholds.

### Task 7.2 — Reconcile contact/near-miss thresholds with measured error `[ENG][PI]` ☐
- `DetectorParams` (Contact 0.03 m, NearMiss 0.12 m, ExitMargin 0.05 m) are defaults. Set from (a) foam half-extent + a documented contact tolerance and (b) measured tracker error; make per-limb if needed. **Done when** thresholds are justified in writing.

### Task 7.3 — Tracker-mount → contact-surface offset `[ENG][PI]` ◐ *(mechanism built, default-off)*
- **Problem.** A tracker on the wrist/ankle strap sits ~10–15 cm from the fingertip/toe that actually hits the foam; at a 0.03 m band, real strikes get under-counted.
- **Solution.** Set per-limb `DetectorParams.LimbContactRadius` (the mechanism exists, default 0) so a contact counts when the tracker is within (mount-offset + foam tolerance). Mirror the same reach in the oracle for cue/outcome consistency. Decision **D6**.
- **Done when.** Collision detection accounts for the mount→contact offset; the operational definition of "limb collision" is documented.

---

## Phase 8 — Pilot → power → run

- **8.1 Integration dry-run `[ENG]` ☐** — full session on the operator: VR renders, trackers track, all 6 conditions cue, orchestration sequences blocks, questionnaires capture, all CSVs feed `analysis.R` clean.
- **8.2 Pilot (1–2) `[PI][ENG]` ☐** — full protocol + safety; check tracker robustness under fast dodges (Ultimate Trackers are inside-out — watch for occlusion/drift), collision realism, cue-salience parity, sickness, data integrity; seed effect sizes for 0.3.
- **8.3 Lock & run `[PI]` ☐** — tag a release; run participants per the pre-registered plan with the locked build.

---

## Decisions required from PI / IRB / Stats

| # | Decision | Owner |
|---|----------|-------|
| D1 | Final safety protocol + IRB coverage of collision risk | `[IRB][PI]` |
| D2 | Target N and #opportunities/block (from power sim) | `[STAT][PI]` |
| D3 | Per-site cue-intensity matching method + reference level | `[PI]` |
| D4 | Authoritative Layout-L1 obstacle coordinates | `[PI]` |
| D5 | Visual-vs-haptic modality match (haptic graded, or Visual discrete?) | `[PI]` |
| D6 | Limb collision definition (tracker-mount offset / effective radius) | `[PI]` |
| D7 | Pre-registration content + stopping rule | `[STAT][PI]` |
| D8 | Multi-limb policy (keep single-alert-per-approach + 1.0 s debounce?) | `[PI]` |

---

## Issue → fix → verification traceability

| Issue | Severity | Fix (Task) | Verify |
|-------|----------|-----------|--------|
| VR + trackers not running | Showstopper | 1.1–1.4 | HMD renders; all 6 joints track on the bench |
| Safety protocol / IRB not confirmed | Showstopper | 0.2, 4.5 | Signed protocol; e-stop rehearsed |
| Cue-salience confound (40 vs 3 motors) | Showstopper | 3.1 | Per-site equal perceived intensity |
| Wrong obstacle coordinates | Showstopper | 2.1, 2.2 | Touch test lands on the obstacle |
| Thresholds vs tracker error | Validity | 7.1, 7.2 | No jitter false-positives; thresholds justified |
| Tracker-mount → contact offset | Validity | 7.3 | Contact counted at the limb surface |
| Latency = 0 (not lead-compensated) | Validity | 6.1 | Measured latency entered |
| Presence/TLX/SSQ UI not built | Validity | 5.5, 0.4 | Instruments per block; models un-stubbed |
| Power not established (12 opps) | Validity | 0.3, 8.2 | ≥80% power for interaction + H4 |
| Visual not modality-matched | Validity | D5 / 4.3 | Documented/aligned cue dynamics |
| Scene incomplete (haptics/spawner/visual/blockSeconds) | Operational | 4.1–4.4 | All conditions function in `Dryrun` |
| Idealized-oracle framing | Reporting | 0.4 | Limitations paragraph (upper-bound prediction) |

*(Resolved & removed: counterbalancing/orchestration/practice/breaks/keypoint-logging — done; cross-PC UDP/firewall/clock-sync/cam→VR calibration — eliminated by the single-PC VIVE switch.)*

---

## Pre-flight checklist (before first participant)

- [ ] IRB number on file; safety protocol signed; spotter trained; e-stop rehearsed (0.2, 4.5)
- [ ] Focus Vision in wired PC-VR; HMD renders; chaperone boundary around Layout-L1 (1.1)
- [ ] 5 Ultimate Trackers paired + role-assigned; `BodyTrackerRig` complete; M4 bench shows all 6 joints clean (1.2, 1.3)
- [ ] Obstacle coords == surveyed == measured physical foam, in SteamVR space (2.1, 2.2)
- [ ] Per-site cue intensities matched and applied (3.1)
- [ ] bHaptics Player running; all conditions cue correctly; None/Visual silent (4.1, 4.3)
- [ ] Orb/projectile task spawns at the 12 scheduled times (4.2)
- [ ] `blockSeconds` = protocol; condition/layout from `SessionRunner` (4.4)
- [ ] Questionnaire UI wired to `RecordQuestionnaire` (5.5)
- [ ] Latency measured and entered (6.1)
- [ ] Detector thresholds + tracker-mount offset reconciled (7.1–7.3)
- [ ] Dry-run CSVs feed `analysis.R` clean; power confirmed; build tagged (8.1–8.3)

---

## Definition of "publication-ready"

Publication-ready when: **(1)** every Showstopper and Validity item is closed and verified; **(2)** the design is pre-registered and the safety protocol IRB-approved; **(3)** a tagged, immutable build runs a full counterbalanced session unattended-by-code (operator only spots/advances); **(4)** all four data streams (summary, events, keypoints, questionnaires) are captured per block and flow into the locked `analysis.R`; **(5)** cue salience is perceptually equated and documented, collisions are measured at the limb surface with justified thresholds, predictive lead is latency-compensated; **(6)** the limitations (idealized oracle = upper bound; spatial-extent difference inherent to localization; tracker-error bounds on contact) are written up honestly.

---

## Appendix A — Key parameters (code defaults; reconcile vs docs)

| Parameter | Default | Source |
|-----------|---------|--------|
| Reactive distance D | 0.30 m | `Core/CollisionOracle.cs` OracleParams |
| Predictive TTC T | 0.50 s | OracleParams |
| Reactive release margin | +0.15 m | OracleParams |
| Predictive release margin | +0.20 s | OracleParams |
| Pipeline latency comp | **0 s** (set from Task 6.1) | OracleParams |
| EMA velocity alpha | 0.5 | CollisionOracle |
| Min closing speed | 0.05 m/s | OracleParams |
| Global re-fire debounce | 1.0 s | ConditionManager |
| Contact distance | 0.03 m | `Core/CollisionDetector.cs` DetectorParams |
| Near-miss distance | 0.12 m | DetectorParams |
| Exit margin (hysteresis) | 0.05 m | DetectorParams |
| Per-limb contact radius | none (0 = off) | DetectorParams.LimbContactRadius (Task 7.3) |
| Haptic pulse | 3 × 100 ms, 60 ms gap (~480 ms) | `Integration/HapticDeviceBinding.cs` |
| Cue intensity | per-site gains (E1: `CueIntensityTable`/`CueIntensityFile`); flat 1.0 if uncalibrated | HapticDeviceBinding, BHapticsSink |
| Block length | 180 s (set per scene) | `LiveSessionController` / `SessionRunner` |
| Opportunities/block | 12 @ 8,22,…,152 s; window 6 s | `Core/OpportunityScheduler.cs` |
| Conditions | None, RG, RB, PG, PB, Visual | `Core/Enums.cs`, ConditionManager |
| bHaptics sites | Chest = VestFront (40), Hand/Foot = Tactosy (3) | `Core/BHapticsTactorMap.cs`, HapticDeviceBinding |
| Counterbalance | 6×6 Williams square (wired) | `Core/SessionPlan.cs` → `SessionRunner` |

## Appendix B — File map (single PC)

**`Assets/CollisionFeedback`**
- `Core/` — `ConditionManager`, `CollisionOracle` (+OracleParams), `CollisionDetector` (+DetectorParams +LimbContactRadius), `OpportunityScheduler`, `SessionPlan`/`WilliamsSquare`, `AvoidanceLatencyDetector`, `TactorArbiter`, `BHapticsTactorMap`, `VisualAlertModel`, `Enums`, `PoseFrame`, `KeypointDeserializer` (CSV replay), `CsvFormatter`, `EventLogFormatter`, `KeypointLogFormatter`, `QuestionnaireFormatter`.
- `Runtime/` — **`TrackerKeypointSource`**, **`BodyTrackerRig`**, `MockKeypointSource`, `SceneObstacles`, `TrackingBench`, `BHapticsSink`, `KeypointLogWriter`, `QuestionnaireLogWriter`, CSV writers.
- `Integration/` — `LiveSessionController` (single-block), `SessionRunner` (full session), `SessionController`/`ExperimentRunner` (synthetic demos), `HapticDeviceBinding`, `OpportunitySpawner`, `VisualObstacleAlert`.
- Scenes — `Dryrun.unity` (experiment-capable), `Bench_M4.unity` (tracking bench), `SampleScene.unity` (offline demo).
- `Analysis/` — `analysis.R` (NB-GLMM + presence/TLX/SSQ), `simulate_mock_data.R`, (+ add `power_analysis.R`).

*Archived (not used): the `AzureKinectUnity` capture project (5 Kinects + UDP sender) — retained as a fallback only.*

---

*Updated 2026-06-22 for the single-PC VIVE Ultimate Tracker architecture. Reconcile all code-derived parameters against the authoritative design docs (Task 0.1) before data collection.*
