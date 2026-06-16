# IMPLEMENTATION PLAN — "When and Where to Warn"
### Predictive, Body-Localized Tactile Cues for Real-Obstacle Collision Avoidance in VR (IEEE VR 2027 / TVCG)

**Purpose.** A single, ordered, self-contained plan that takes the project from "working tracking prototype" to a **complete, comprehensive, bulletproof, publication-ready** within-subjects VR study. Every issue found in the full-stack review is listed with its **problem → why it matters → solution → touchpoints → done-when**. Execute phases top-to-bottom; items marked *(parallel)* may overlap.

**Status legend:** ☑ done · ◐ partial/built-not-validated · ☐ not started
**Owner tags:** `[ENG]` engineering · `[PI]` PI/design decision · `[IRB]` ethics/safety · `[STAT]` statistics

> **Authority note.** The original design docs (`IEEEVR2027_StudyDesign`, `_StudyProtocol`, `_Implementation_Guide`, `_Layout1_Storyboard`, `_Pilot_Design`, `_Risk_Register`) are **not on this machine**. Parameters below are recovered from code and **must be reconciled against those authoritative docs**; where they disagree, the docs win. Retrieving them is Task 0.1.

---

## Table of contents
- [System overview](#system-overview)
- [Phase −1 — Foundation already in place (☑)](#phase-1--foundation-already-in-place)
- [Phase 0 — Authority, ethics & statistics (BLOCKING)](#phase-0--authority-ethics--statistics-blocking)
- [Phase 1 — Make it actually run in VR](#phase-1--make-it-actually-run-in-vr)
- [Phase 2 — Calibration & collision ground-truth](#phase-2--calibration--collision-ground-truth)
- [Phase 3 — Close the cue-salience confound](#phase-3--close-the-cue-salience-confound)
- [Phase 4 — Complete the experimental scene](#phase-4--complete-the-experimental-scene)
- [Phase 5 — Session orchestration & instruments](#phase-5--session-orchestration--instruments)
- [Phase 6 — Latency budget & clock sync](#phase-6--latency-budget--clock-sync)
- [Phase 7 — Detector fidelity](#phase-7--detector-fidelity)
- [Phase 8 — Pilot → power → run](#phase-8--pilot--power--run)
- [Decisions required from PI / IRB / Stats](#decisions-required-from-pi--irb--stats)
- [Issue → fix → verification traceability](#issue--fix--verification-traceability)
- [Pre-flight checklist (before first participant)](#pre-flight-checklist-before-first-participant)
- [Definition of "publication-ready"](#definition-of-publication-ready)
- [Appendix A — Key parameters (code defaults)](#appendix-a--key-parameters-code-defaults)
- [Appendix B — File map](#appendix-b--file-map)

---

## System overview

Two machines, one data path:

```
CAPTURE PC (AzureKinectUnity)                         STUDY PC (My project (1) / CollisionFeedback)
5× Azure Kinect → body tracking (GPU)                UdpKeypointSource :9000 (binds 0.0.0.0)
  → SkeletonTracker (Camera-1 world frame)              → [Phase 2] apply T_cam→VR
  → StudyKeypointUdpSender (6 joints, conf-fused)        → LiveSessionController
  → UDP CSV @ ~30 Hz  ───────────────────────────►        → BlockRunner: oracle (TTC) + ConditionManager
   (keypoints-only scene; point clouds OFF)                 + CollisionDetector + OpportunityScheduler
                                                          → IFeedbackSink → bHaptics (3-pulse) / Visual
                                                          → session_summary.csv + session_events.csv
                                                       VR: OpenXR HMD + controllers  ·  bHaptics TactSuit X40 + 4 Tactosy
```

**Verdict going in:** the Core "brain" (oracle, conditions, detectors, scheduler, counterbalancer) and the R analysis are **mature and unit-tested**; the **rig is a prototype** (VR off, scene incomplete, no orchestration); and there are **3 design confounds + a safety/docs gap** to close. This plan fixes all of it.

---

## Where each phase is implemented (PC assignment)

**Two machines:**
- 🟦 **STUDY PC** (`10.30.10.37`) — runs the VR experiment project **`My project (1)`** (OpenXR HMD + bHaptics + the brain). Everything the participant sees/feels.
- 🟩 **CAPTURE PC** (`10.30.11.28`, this dev machine) — runs **`AzureKinectUnity`** (5 Kinects + body tracking + `StudyKeypointUdpSender`).
- 🟪 **BOTH** — needs the two running together (capture streaming ↔ study receiving), or work on each.
- ⬜ **OFF-RIG** — human / lab / analysis; do on any workstation and store results in the repo/lab (not tied to either Unity PC).

> ⚠️ **Critical sync caveat — read before editing further.** The study project edited in this session physically lives on the **CAPTURE PC** at `Desktop\WhenandWheretoWarn\My project (1)`, but the experiment **runs on the STUDY PC**, which has its **own copy**. So every study-side change — Phases 1–5, 7, *and the already-built `SessionRunner` / calibration / keypoint-logging code* — **must be transferred to the STUDY PC** before it has any effect there. Use the `My project (1)` git/GitHub repo as the sync path (commit on the dev machine → pull on the Study PC); never hand-copy the `Library/` folder. **Confirm which copy is canonical** before doing more study-side work.

| Phase / Task | PC | Why |
|---|----|-----|
| 0.1 Retrieve design docs | ⬜ Off-rig | human; store copies in the study repo |
| 0.2 IRB + safety protocol | ⬜ Off-rig / lab | ethics & lab; the e-stop *code* is Study PC (Task 4.5) |
| 0.3 Power analysis · 0.4 Pre-registration + `analysis.R` | ⬜ Off-rig (analysis) | R; data originates on the Study PC |
| 1.1–1.4 XR Plug-in Mgmt · OpenXR loader · controller profile · XR Origin rig · build settings | 🟦 Study PC | OpenXR + HMD live here |
| 2.1 Reconcile obstacle coordinates | 🟦 Study PC (+ lab) | scene edit + physical foam placement |
| 2.2 Run & validate cam→VR calibration | 🟪 Both | Capture PC streams keypoints; Study PC runs the `CameraVrCalibration` scene + controller, solves, saves |
| 3.1 Cue-intensity equalization | 🟦 Study PC | bHaptics worn by the participant |
| 4.1–4.5 Scene completion + e-stop/HUD | 🟦 Study PC | scene/Editor + bHaptics |
| 5.1–5.6 Orchestration · practice · breaks · questionnaires · keypoint logging | 🟦 Study PC | study project (5.1/5.2/5.3/5.4/5.6 code already written — **must be synced to the Study PC**) |
| 6.1 Clock sync | 🟪 Both | sync **both** PCs to a common NTP source |
| 6.2 Measure end-to-end latency → set `pipelineLatencySeconds` | 🟪 Both | the chain spans both; the value is entered on the Study PC |
| 7.1–7.3 Detector smoothing · thresholds · hand=wrist offset | 🟦 Study PC | study Core; tune using the Capture-fed M4 bench |
| 8.1 Integration dry-run | 🟪 Both | full pipeline running together |
| 8.2 Pilot · 8.3 Lock & run | 🟪 Both (+ lab) | full rig: Capture tracking + Study VR + spotter |

**Capture-PC standing config (🟩, keep as-is every session, in `AzureKinectUnity`):** the keypoints-only scene (point-cloud objects disabled), `StudyKeypointUdpSender` (Hand→Wrist, `jointHoldSeconds`, `logDiagnostics`), and the cross-PC firewall allowance. Any change to the *sender/tracking* is made here; nothing else study-side is.

---

## Phase −1 — Foundation already in place

These are DONE and validated this cycle — recorded so the plan is a complete record.

- ☑ **Live cross-PC tracking** at ~28.7 Hz (Kinect 30 fps ceiling), all 6 study joints confident.
- ☑ **Hand→Wrist remap** in `StudyKeypointUdpSender` (Kinect hand-tip joints report `None`; wrists are reliable). *Creates a measurement caveat — see Task 7.3.*
- ☑ **Point-cloud pipeline disabled** on the capture PC → keypoints-only scene (the synchronous fusion in `KinectDevice.Update` was throttling the app to ~2.3 FPS). Run a **keypoints-only capture scene** for sessions.
- ☑ **Cross-PC UDP unblocked** — deleted the `Unity.exe` inbound **Block** rule on the **Public** firewall profile (Block overrides the port-9000 Allow).
- ☑ **Gate relaxation** — `jointHoldSeconds` (default 0.25 s) bridges a briefly-missing joint with its last-good position.
- ◐ **cam→VR calibration scaffolding built** (`RigidTransformSerializer` [Core], `CameraVrCalibrationFile` [Runtime], `CameraVrCalibration` [Runtime], `LiveSessionController` loads + applies `T`). **Not yet run/validated** → Phase 2.

**Completed 2026-06-15 (autonomous, no decisions/hardware needed):**
- ☑ **Session orchestration + counterbalancing** (Tasks 5.1, 5.2) — new `SessionRunner` (Integration) sequences an *excluded practice block* → the 6 **Williams-square-ordered** condition blocks (`SessionPlan.For`) with enforced breaks; owns one UDP source for the whole session; applies the cam→VR transform; writes a per-participant CSV folder `sessions/P###/` with **overwrite protection**; IMGUI operator console (Start / Next / Stop). *Only Layout-L1 geometry is authored — other layout ids fall back to L1 with a warning until their storyboards exist.*
- ☑ **Practice block + inter-block breaks** (Tasks 5.3, 5.4) — built into `SessionRunner` (`practiceCondition`/`practiceSeconds`/`minBreakSeconds` configurable; practice written to separate `practice_*` files, excluded from analysis). *Break SSQ-gating still depends on questionnaires (Task 5.5).*
- ☑ **Raw keypoint logging** (Task 5.6) — `KeypointLogWriter` (Runtime, AutoFlush) wired into **both** `SessionRunner` (per block) and `LiveSessionController`; persists the VR-frame `PoseFrame`s via `KeypointLogFormatter`.
- ☑ **Input-backend audit** (Task 1.4, code part) — confirmed **zero** legacy `UnityEngine.Input` usage in the study code. *Build-Settings scene list still needs the Editor.*

---

## Phase 0 — Authority, ethics & statistics (BLOCKING)

> Nothing downstream is valid until this phase is complete. No participant runs before 0.1–0.3.

### Task 0.1 — Retrieve & ingest the authoritative design docs `[PI]` ☐
- **Problem.** The 6 design/protocol docs are not on this machine; current parameters are code-derived.
- **Why.** Thresholds, layout coordinates, sample size, and the safety protocol must come from the approved design, not reverse-engineered defaults.
- **Solution.** Retrieve all six docs; place a copy in the repo (`/docs`); reconcile every parameter in [Appendix A] against them and correct the code where they differ.
- **Done when.** `/docs` contains the six files; a one-page "parameters reconciled" note lists each value as *matches code* / *corrected to X*.

### Task 0.2 — IRB approval & written safety protocol `[IRB]` ☐
- **Problem.** Participants make *real* near-collisions with physical obstacles while blind in an HMD. The `_Risk_Register` / `_StudyProtocol` (spotter, boundary, padding, e-stop, exclusions) are not available here.
- **Why.** Ethically and legally mandatory; also a reviewer-required methods section.
- **Solution.** Confirm IRB approval covers the collision-risk paradigm. Lock a written protocol covering, at minimum:
  - **Trained spotter** within arm's reach at all times; stop-word + physical guard.
  - **Play-area boundary** (OpenXR chaperone) sized with margin around Layout-L1; obstacles inside it.
  - **Foam obstacle spec** — material, dimensions, padding, mounting (must not tip/injure).
  - **Cable/tether management** for HMD + suit (trip hazard).
  - **Emergency stop** — verbal halt + a single key/controller button that ends the block and unblanks.
  - **Cybersickness**: SSQ pre/post; stop criteria; rest policy.
  - **Exclusion criteria** (vestibular, pregnancy, etc.) and consent script.
- **Done when.** Signed protocol + IRB number filed in `/docs`; the e-stop is implemented (Task 4.5) and rehearsed.

### Task 0.3 — Power analysis & design sizing `[STAT][PI]` ☐
- **Problem.** 12 opportunities/block caps collisions ≤12/cell; strong cues push conditions toward 0 → zero-inflation/overdispersion for the NB-GLMM. No power analysis exists (mock uses N=48 as a placeholder).
- **Why.** Under-powered 2×2 interaction = unpublishable; over-collection = wasted runs and fatigue confounds.
- **Solution.** Monte-Carlo power simulation on the planned NB-GLMM (`Analysis/analysis.R` model) over plausible collision rates and effect sizes (seed pilot estimates from Phase 8). Decide **N**, **#opportunities/block**, and whether to add blocks/repeats. Extend `OpportunityScheduler` Layout-L1 if more events/block are needed.
- **Done when.** A `power_analysis.R` + report justify N and opportunities at ≥80% power for the primary Timing×Localization interaction and the H4 (PB vs Visual) contrast.

### Task 0.4 — Lock the analysis plan & pre-registration `[STAT][PI]` ☐
- **Problem.** Presence/workload/sickness models in `analysis.R` are commented stubs; primary/secondary hypotheses not pre-registered.
- **Solution.** Finalize the analysis script (primary CPO NB-GLMM; FLOOR vs None; H4 PB vs Visual; presence/TLX/SSQ models). **Pre-register** hypotheses, model, exclusions, and stopping rule.
- **Done when.** Pre-registration submitted; `analysis.R` runs end-to-end on `simulate_mock_data.R` output with no stubs.

---

## Phase 1 — Make it actually run in VR

> Today, pressing Play yields a **flat desktop window** — XR is not enabled.

### Task 1.1 — Enable XR Plug-in Management + OpenXR `[ENG]` ☐
- **Problem.** No `XRGeneralSettings.asset`/loader boot config; `com.unity.xr.management` is only a transitive dependency; pressing Play runs in desktop mode.
- **Solution.**
  1. Add `com.unity.xr.management` to `Packages/manifest.json` (pin a version compatible with `com.unity.xr.openxr@1.16.1`).
  2. Project Settings ▸ **XR Plug-in Management** ▸ install ▸ enable **OpenXR** for **Standalone** (and Android if targeting Quest standalone).
  3. Confirm `XRGeneralSettings`/`XRManagerSettings` assets are generated and the OpenXR loader is in the boot list.
- **Done when.** Pressing Play with the headset on renders the scene to the HMD.

### Task 1.2 — Enable a controller interaction profile `[ENG]` ☐
- **Problem.** Every OpenXR interaction profile is disabled (`m_enabled: 0`) → input/validation fails.
- **Solution.** In OpenXR settings enable the headset's profile (e.g., **Meta Quest Touch** / **Oculus Touch**) for the active build target; resolve any OpenXR validation warnings.
- **Done when.** OpenXR validation is clean; controllers report pose/buttons in Play.

### Task 1.3 — Add an XR Origin rig to the experimental scene `[ENG]` ☐
- **Problem.** All scenes use a plain Main Camera (no XR Origin / TrackedPoseDriver).
- **Solution.** Add **XR Origin (Action-based)** with TrackedPoseDriver to `Dryrun.unity`; make it the scene camera; wire left/right controllers (needed as the `vrProbe` for calibration and the e-stop input). Confirm the floor/eye height matches the physical room.
- **Done when.** Head + both controllers track 1:1 in `Dryrun`; play-area origin coincides with the surveyed room origin used for Layout-L1.

### Task 1.4 — Build-settings & input hygiene `[ENG]` ☐
- **Problem.** Only `SampleScene` is in Build Settings; Active Input Handling = **Input System only** (legacy `Input` API disabled).
- **Solution.** Add `Dryrun` (and the calibration scene) to Build Settings. Ensure all interactive scripts use the Input System or IMGUI (the calibration UI already uses IMGUI — safe). Audit for any `UnityEngine.Input.*` usage and replace.
- **Done when.** Correct scenes build; no legacy-Input exceptions at runtime.

---

## Phase 2 — Calibration & collision ground-truth

> The primary DV is collisions measured by tracking-vs-obstacle distance. It is only meaningful once limbs and obstacles share one frame **and** the obstacle coordinates are correct.

### Task 2.1 — Reconcile obstacle coordinates to a single source of truth `[ENG][PI]` ☐
- **Problem.** `Dryrun` scene boxes (e.g., O1 ≈ (−0.8,0.4,0.8)) **disagree** with the surveyed `Layout1Stimuli` coordinates (e.g., O1 ≈ (0,0.40,1.20)).
- **Why.** `SceneObstacles.Collect()` reads world-space `BoxCollider` bounds — wrong coords = wrong collisions for every trial.
- **Solution.** Adopt the **surveyed** coordinates (from the design doc / `Layout1Stimuli`) as truth; physically place foam obstacles to match; update the scene `BoxCollider`s to the surveyed world positions/sizes; keep obstacles **axis-aligned** (rotated boxes inflate the AABB).
- **Done when.** Scene box centers/extents == surveyed coords == physical foam positions (measured), all in the same world origin as the XR rig.

### Task 2.2 — Run & validate the cam→VR calibration `[ENG]` ◐
- **Problem.** No `cam_to_vr_calib.txt` exists yet → `LiveSessionController` runs at Identity (limbs not in obstacle space).
- **Solution.** Use the built `CameraVrCalibration` scene: capture-PC streaming, hold the tracked controller against the sampled wrist at ≥4–6 well-spread points (vary height), Solve + Save. Then **independently verify**: have the participant touch a known obstacle corner and confirm the in-engine limb sits on it.
- **Done when.** Calibration RMS ≲ 30–50 mm; a touch test shows the limb landing on the physical obstacle within tolerance; `LiveSessionController` logs "Loaded camera→VR calibration".
- **Note.** Re-run calibration if cameras are bumped or the rig origin moves. Calibration and a session can't run together (both bind UDP 9000).

---

## Phase 3 — Close the cue-salience confound

### Task 3.1 — Equalize perceived cue intensity across body sites `[ENG][PI]` ☐
- **Problem.** Generic (chest) cue = **40 X40 motors**; localized (hand/foot) cue = **3 Tactosy motors**; all at flat intensity 1.0 (`HapticDeviceBinding.cs`). The **Localization factor is confounded with stimulus energy** — a reviewer reads your localization effect as "the chest just buzzes harder."
- **Why.** This threatens the core 2×2 internal validity (RG/PG vs RB/PB).
- **Solution.**
  1. **Per-participant perceptual match** before experimental blocks: a brief psychophysical procedure (method-of-adjustment or a 2-up/1-down staircase, ~2–3 min) that finds, for each site (Chest, L/R Hand, L/R Foot/shin), the device intensity yielding **equal perceived magnitude** against a fixed reference.
  2. Store per-site intensity multipliers; **apply them** by extending the haptic path to a per-`HapticSite` intensity (the `Intensity` field exists; replace the flat 1.0 with a per-site lookup in `HapticDeviceBinding`).
  3. Acknowledge that spatial *extent* can't be equated (vest vs point) — equate perceived intensity and report extent as inherent to the manipulation.
- **Done when.** A `CueIntensityCalibration` step writes per-site intensities; the live cue uses them; a methods paragraph documents the procedure and the residual extent difference.

---

## Phase 4 — Complete the experimental scene

All in `Dryrun.unity` (the only experiment-capable scene).

### Task 4.1 — Live haptics in scene `[ENG]` ☐
- **Problem.** `[bHaptics]` prefab absent from `Dryrun`; `useLiveHaptics = 0`.
- **Solution.** Add the `[bHaptics]` prefab; run the bHaptics Player; set `useLiveHaptics = 1`. Verify each site fires on the matching condition.
- **Done when.** RG/RB/PG/PB drive the correct tactor(s); None/Visual produce no buzz.

### Task 4.2 — Configure the dodge/orb task `[ENG][PI]` ☐
- **Problem.** `OpportunitySpawner` prefabs (`orbPrefab`, `projectilePrefab`, `aimTarget`) are null → the task spawns nothing.
- **Solution.** Author/assign the orb + projectile prefabs and aim target per the Layout-1 storyboard; confirm spawner is driven on the block clock (`spawner.DriveExternally()` + `Tick(blockTime)`).
- **Done when.** The 12 scheduled events produce the intended physical-dodge demands at the correct times.

### Task 4.3 — Visual condition renderer `[ENG]` ☐
- **Problem.** No `VisualObstacleAlert` object in any scene → Visual condition logs but renders nothing.
- **Solution.** Add a `VisualObstacleAlert` configured over the obstacles; confirm `LiveSessionController.UpdatePose(..., condition==Visual, ...)` drives the highlight.
- **Done when.** Visual condition shows the proximity-graded highlight; haptic conditions don't.

### Task 4.4 — Protocol parameters in scene `[ENG]` ☐
- **Problem.** Scene has `blockSeconds = 30` (protocol = 180); `condition` hardcoded; `participantId/blockIndex = 0`.
- **Solution.** Set `blockSeconds = 180` (or the doc value); feed `condition/layoutId/blockIndex` from the session orchestrator (Phase 5), not by hand.
- **Done when.** Block length and IDs come from the plan, not Inspector edits.

### Task 4.5 — Emergency-stop & operator HUD `[ENG][IRB]` ☐
- **Problem.** No e-stop wired (Task 0.2 requires one).
- **Solution.** A controller button + keyboard key that immediately ends the block, unblanks/pauses the task, and logs the abort. Operator HUD on the desktop mirror showing condition, block #, time remaining, live collision/near-miss counters, and tracking health (delivery Hz, held-joint count).
- **Done when.** E-stop halts a block within one frame and writes an `aborted` marker; HUD shows live state.

---

## Phase 5 — Session orchestration & instruments

### Task 5.1 — Wire counterbalancing into the live run `[ENG]` ☐
- **Problem.** `SessionPlan`/`WilliamsSquare` (validated) is **dead code**; live order is whatever the operator types; `layoutId` is ignored (always Layout-1).
- **Solution.** Drive the run from `SessionPlan.For(participantId)` → ordered `BlockAssignment`s (condition, layoutId, position). Honor `layoutId` (load the matching layout, not always L1).
- **Done when.** Given a participant ID, the 6 blocks run in the correct Williams order with the assigned layouts — no manual condition entry.

### Task 5.2 — Automated session runner `[ENG]` ☐
- **Problem.** `LiveSessionController` runs **one block per Play**; 6 manual re-Plays per participant invite wrong/duplicate conditions and CSV overwrites (append-only).
- **Solution.** A `SessionRunner` MonoBehaviour that sequences: consent/SSQ-pre → **practice block** (Task 5.3) → the 6 counterbalanced blocks with **breaks** (Task 5.4) and per-block questionnaires (Task 5.5) → SSQ-post. Refactor `LiveSessionController` to expose "configure + run one block" that the runner calls per `BlockAssignment`. **Safe file naming** per `participant/block/condition` with overwrite protection (refuse or timestamp).
- **Done when.** One Play runs an entire participant session end-to-end; files are uniquely named; re-running a used participant ID is blocked.

### Task 5.3 — Practice / familiarization block `[ENG][PI]` ☐
- **Problem.** No practice block → task-learning + cue-novelty load onto whichever condition is in position 1.
- **Solution.** A non-scored practice block (e.g., 60–90 s) introducing the dodge task and one example of each cue type; excluded from analysis.
- **Done when.** Every participant completes practice before block 1; practice data is flagged/excluded.

### Task 5.4 — Inter-block breaks & sickness gating `[ENG][IRB]` ☐
- **Problem.** No breaks across 6×180 s HMD+suit blocks (fatigue, sweat, drift, sickness).
- **Solution.** Mandatory rest screen between blocks (min duration, operator-advance); optional SSQ checkpoint; allow HMD doff.
- **Done when.** Breaks are enforced; sickness stop-criteria are actionable.

### Task 5.5 — Questionnaires (presence is in the title) `[ENG][STAT]` ☐
- **Problem.** **IPQ presence**, **NASA-TLX**, **SSQ** are commented stubs in `analysis.R`; presence is a stated primary co-outcome ("…while preserving presence").
- **Solution.** Administer **IPQ + NASA-TLX after each block** and **SSQ pre/post session**; capture responses (in-VR panel or desktop between blocks) into a CSV merged on `participant/block/condition`. Un-stub the corresponding `analysis.R` models.
- **Done when.** Presence/workload/sickness are collected every block and flow into the analysis pipeline.

### Task 5.6 — Persist raw keypoints `[ENG]` ☐
- **Problem.** `KeypointLogFormatter` exists but **no writer calls it** — raw tracking is not saved.
- **Solution.** In `LiveSessionController.Update`, write each VR-frame `PoseFrame` via `KeypointLogFormatter` to a per-block keypoint CSV; flush/close in `Finish()`.
- **Why.** Enables offline re-analysis, oracle re-derivation, and reviewer scrutiny; irreplaceable once a session is over.
- **Done when.** Each block writes a complete per-frame keypoint log alongside the summary/event CSVs.

---

## Phase 6 — Latency budget & clock sync

### Task 6.1 — Sync the two PC clocks `[ENG]` ☐
- **Problem.** Capture-PC timestamps vs study-PC wall clock differed by ~13 s; absolute latency is meaningless until synced.
- **Solution.** Sync both PCs to a common time source (NTP via `w32tm`, or PTP for tighter bounds); verify offset < a few ms. (Block-relative TTC already cancels a constant offset, but latency measurement and cross-log alignment need true sync.)
- **Done when.** Measured inter-PC offset is small and stable; documented.

### Task 6.2 — Measure end-to-end latency (M3) & compensate `[ENG][PI]` ☐
- **Problem.** `OracleParams.PipelineLatencySeconds = 0`; predictive cues aren't lead-compensated. If total latency approaches T = 0.50 s, the predictive advantage erodes.
- **Solution.** Measure the full chain (camera capture → tracking → fusion → UDP → oracle → **bHaptics actuation**), e.g., a synchronized physical event observed by both systems, or injected-motion-to-cue timing. Set `pipelineLatencySeconds` = measured median. Include the bHaptics actuation delay in the budget.
- **Done when.** A measured latency value (with distribution) is documented and entered per build; predictive lead is verified against it.

---

## Phase 7 — Detector fidelity

### Task 7.1 — Smooth keypoints feeding the detector `[ENG]` ☐
- **Problem.** The collision detector uses **raw** keypoints (EMA lives only in the oracle); contact = 0.03 m is finer than ~cm tracking jitter.
- **Solution.** Apply a small EMA/median filter to the detector's input (separate from the oracle's velocity EMA), tuned to the measured per-joint jitter from the M4 bench.
- **Done when.** Detector input jitter is below the contact band; no jitter-induced false engagements.

### Task 7.2 — Reconcile contact/near-miss thresholds with measured error `[ENG][PI]` ☐
- **Problem.** `DetectorParams` (Contact 0.03 m, NearMiss 0.12 m, ExitMargin 0.05 m) are hardcoded defaults, not derived from foam size or measured tracking error.
- **Solution.** Set thresholds from (a) the physical foam half-extent + a documented contact tolerance and (b) measured tracking error; make them **per-limb** if needed; record the rationale.
- **Done when.** Thresholds are justified in writing and survive a sensitivity check.

### Task 7.3 — Correct the hand=wrist / foot=ankle collision offset `[ENG][PI]` ☐
- **Problem.** The Hand→Wrist remap (a tracking-reliability fix) means the detector sees the **wrist**, ~10–15 cm proximal to the fingertips; at a 0.03 m contact band, real hand strikes (wrist 15 cm away) are **not counted**. Feet are detected at the ankle similarly.
- **Why.** Systematic under-counting of the very collisions the study measures.
- **Solution (pick one, document it):**
  - **(a) Effective contact radius per limb** — count a hand contact when the wrist is within ~(hand length + foam tolerance); analogous for foot/ankle. Simplest, defensible. Extend `DetectorParams` to per-limb radii.
  - **(b) Forward offset** — project the wrist point toward the hand along a body-relative/velocity direction (cruder; no hand orientation in the 6-joint stream).
  - **(c) Stream an extra hand joint** from the capture side with smoothing (less reliable; reason we used wrist).
- **Done when.** Collision detection accounts for the wrist→hand (and ankle→foot) segment; the operational definition of "limb collision" is documented.

---

## Phase 8 — Pilot → power → run

### Task 8.1 — Integration dry-run (no participant) `[ENG]` ☐
- Full session on the operator/experimenter: VR renders, calibration valid, all 6 conditions cue correctly, orchestration sequences blocks, questionnaires capture, all CSVs (summary + events + keypoints + questionnaires) write and feed `analysis.R`.
- **Done when.** `analysis.R` runs clean on the dry-run data with no missing columns.

### Task 8.2 — Pilot (1–2 participants) `[PI][ENG]` ☐
- Run the full protocol with the safety procedure; check tracking health, collision realism, cue salience parity, sickness, data integrity; seed effect-size estimates for Task 0.3.
- **Done when.** Pilot data are clean; power sim re-run with pilot estimates confirms N.

### Task 8.3 — Lock & run `[PI]` ☐
- Freeze code/params (tag a release); run participants per the pre-registered plan with the locked build.
- **Done when.** Data collection proceeds against an immutable, documented build.

---

## Decisions required from PI / IRB / Stats

| # | Decision | Owner |
|---|----------|-------|
| D1 | Final safety protocol + IRB coverage of collision risk | `[IRB][PI]` |
| D2 | Target N and #opportunities/block (from power sim) | `[STAT][PI]` |
| D3 | Per-site cue-intensity matching method (adjustment vs staircase) and reference level | `[PI]` |
| D4 | Authoritative Layout-L1 coordinates (reconcile scene vs surveyed) | `[PI]` |
| D5 | Visual-vs-haptic modality match: make haptic graded, or Visual discrete? | `[PI]` |
| D6 | Hand/foot collision definition (effective radius vs offset) | `[PI]` |
| D7 | Pre-registration content + stopping rule | `[STAT][PI]` |
| D8 | Multi-limb policy: keep single-alert-per-approach + 1.0 s debounce, or allow simultaneous localized cues? | `[PI]` |

---

## Issue → fix → verification traceability

| Issue | Severity | Fix (Task) | Verify |
|-------|----------|-----------|--------|
| VR doesn't run (no XR boot/rig/profile) | Showstopper | 1.1–1.4 | HMD renders; controllers track |
| Safety protocol / IRB not confirmed | Showstopper | 0.2, 4.5 | Signed protocol; e-stop rehearsed |
| Cue-salience confound (40 vs 3 motors) | Showstopper | 3.1 | Per-site equal perceived intensity |
| No cam→VR calibration / wrong obstacle coords | Showstopper | 2.1, 2.2 | Touch test lands on obstacle; RMS ≲ 50 mm |
| Contact 0.03 m < tracking jitter; raw keypoints | Validity | 7.1, 7.2 | No jitter false-positives; thresholds justified |
| Hand=wrist (foot=ankle) collision offset | Validity | 7.3 | Contact counted at the limb surface |
| Latency = 0 / clocks unsynced | Validity | 6.1, 6.2 | Measured latency entered; offset < few ms |
| Presence/TLX/SSQ not collected | Validity | 5.5, 0.4 | Instruments per block; models un-stubbed |
| Counterbalancing not applied; layoutId ignored | Validity | 5.1 | Williams order + assigned layouts run live |
| No practice block | Validity | 5.3 | Practice precedes block 1; excluded |
| Power not established (12 opps) | Validity | 0.3, 8.2 | ≥80% power for interaction + H4 |
| Visual not modality-matched | Validity | D5 / 4.3 | Documented/aligned cue dynamics |
| No session orchestration (1 block/Play) | Operational | 5.2 | One Play = full session; safe filenames |
| No breaks | Operational | 5.4 | Enforced rests + sickness gating |
| Keypoints not logged | Operational | 5.6 | Per-frame keypoint CSV per block |
| Scene incomplete (haptics/spawner/visual/blockSeconds) | Operational | 4.1–4.4 | All conditions function in `Dryrun` |
| Idealized-oracle framing | Reporting | 0.4 | Limitations paragraph (upper-bound prediction) |

---

## Pre-flight checklist (before first participant)

- [ ] IRB number on file; safety protocol signed; spotter trained; e-stop rehearsed (0.2, 4.5)
- [ ] HMD renders; controllers track; chaperone boundary set around Layout-L1 (1.1–1.3)
- [ ] Capture PC on the **keypoints-only** scene; delivery ≥ ~28 Hz; calibration ON; one fused body (Phase −1, 2.2)
- [ ] `cam_to_vr_calib.txt` present; touch test passes; obstacle coords == surveyed == physical (2.1, 2.2)
- [ ] Per-site cue intensities matched and applied (3.1)
- [ ] bHaptics Player running; all conditions cue correctly; None/Visual silent (4.1, 4.3)
- [ ] Orb/projectile task spawns at the 12 scheduled times (4.2)
- [ ] `blockSeconds` = protocol; condition/layout from `SessionPlan` (4.4, 5.1)
- [ ] Practice + breaks + questionnaires + keypoint logging wired (5.3–5.6)
- [ ] Clocks synced; latency measured and entered (6.1, 6.2)
- [ ] Detector smoothed; thresholds + hand/foot offset reconciled (7.1–7.3)
- [ ] Dry-run CSVs feed `analysis.R` clean; power confirmed; build tagged (8.1–8.3)

---

## Definition of "publication-ready"

The study is publication-ready when: **(1)** every Showstopper and Validity item above is closed and verified; **(2)** the design is pre-registered and the safety protocol is IRB-approved; **(3)** a tagged, immutable build runs a full counterbalanced session unattended-by-code (operator only spots/advances); **(4)** all four data streams (summary, events, keypoints, questionnaires) are captured per block and flow into the locked `analysis.R`; **(5)** cue salience is perceptually equated and documented, collisions are measured at the limb surface with justified thresholds, predictive lead is latency-compensated; and **(6)** the limitations (idealized oracle = upper bound; spatial-extent difference inherent to localization; tracking-error bounds on contact) are written up honestly.

---

## Appendix A — Key parameters (code defaults; reconcile vs docs)

| Parameter | Default | Source |
|-----------|---------|--------|
| Reactive distance D | 0.30 m | `Core/CollisionOracle.cs` OracleParams |
| Predictive TTC T | 0.50 s | OracleParams |
| Reactive release margin | +0.15 m | OracleParams |
| Predictive release margin | +0.20 s | OracleParams |
| Pipeline latency comp | **0 s** (set from M3) | OracleParams / `LiveSessionController` |
| EMA velocity alpha | 0.5 | CollisionOracle |
| Min closing speed | 0.05 m/s | OracleParams |
| Global re-fire debounce | 1.0 s | ConditionManager |
| Contact distance | 0.03 m | `Core/CollisionDetector.cs` DetectorParams |
| Near-miss distance | 0.12 m | DetectorParams |
| Exit margin (hysteresis) | 0.05 m | DetectorParams |
| Haptic pulse | 3 × 100 ms, 60 ms gap (~480 ms) | `Integration/HapticDeviceBinding.cs` |
| Cue intensity | flat 1.0 (→ Phase 3) | HapticDeviceBinding |
| Block length | 180 s (scene overrides 30) | `LiveSessionController` |
| Opportunities/block | 12 @ 8,22,…,152 s; window 6 s | `Core/OpportunityScheduler.cs` |
| Conditions | None, RG, RB, PG, PB, Visual | `Core/Enums.cs`, ConditionManager |
| bHaptics sites | Chest = VestFront (40), Hand/Foot = Tactosy (3) | `Core/BHapticsTactorMap.cs`, HapticDeviceBinding |
| Counterbalance | 6×6 Williams square (unused → Phase 5) | `Core/SessionPlan.cs` |

## Appendix B — File map

**Study PC (`My project (1)/Assets/CollisionFeedback`)**
- `Core/` — `ConditionManager`, `CollisionOracle` (+OracleParams), `CollisionDetector` (+DetectorParams), `OpportunityScheduler`, `SessionPlan`/`WilliamsSquare`, `AvoidanceLatencyDetector`, `TactorArbiter`, `BHapticsTactorMap`, `VisualAlertModel`, `Enums`, `PoseFrame`, `KeypointDeserializer`, `RigidTransformSolver`, `RigidTransformSerializer`, `CsvFormatter`, `EventLogFormatter`, `KeypointLogFormatter`.
- `Runtime/` — `UdpKeypointSource`, `SceneObstacles`, `TrackingBench`, `CameraVrCalibration`, `CameraVrCalibrationFile`, `BHapticsSink`, CSV writers.
- `Integration/` — `LiveSessionController` (live driver), `HapticDeviceBinding`, `OpportunitySpawner`, `VisualObstacleAlert`.
- Scenes — `Dryrun.unity` (experiment-capable), `SampleScene.unity` (offline demo), `Bench_M4.unity` (tracking bench).
- `Analysis/` — `analysis.R` (NB-GLMM), `simulate_mock_data.R`, (+ add `power_analysis.R`).

**Capture PC (`Documents/GitHub/AzureKinectUnity/Assets/Scripts`)**
- `StudyKeypointUdpSender` (6-joint conf-fused UDP bridge; Hand→Wrist; `jointHoldSeconds`; `logDiagnostics`), `SkeletonTracker`, `KinectDevice`, `CalibrationUtility`/`DualCameraCalibrator` (multi-cam), point-cloud pipeline (disable for sessions).

---

*Generated 2026-06-15 from a full-stack review of both repositories. Reconcile all code-derived parameters against the authoritative design docs (Task 0.1) before relying on this plan for data collection.*
