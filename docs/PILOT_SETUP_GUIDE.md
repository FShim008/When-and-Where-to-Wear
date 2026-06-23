# Pilot Setup Guide — step-by-step to a runnable session

Detailed, click-by-click instructions for each item in `PRE_PILOT_PLAN.md`'s "runnable for pilot" list,
tailored to this repo (Unity 6.3 / URP / Input System), the VIVE Focus Vision + Ultimate Trackers, and the
bHaptics suit. Components referenced are already in the project unless marked **install**.

> **Two milestones.** **Technical pilot** = steps 1–5 + 12 (you in the headset with head + 2 controllers,
> no trackers, no IRB) — does the whole software loop. **Real pilot** = add 7–11. Do the technical pilot first.

Scene files: **`Assets/Scenes/Dryrun.unity`** (the study scene — has `Obstacles`, `Spawner`, `Session`) and
**`Assets/Scenes/Bench_M4.unity`** (tracking bench). Output lands in `…/AppData/LocalLow/<company>/<product>/sessions/P###/`.

---

## Part A — Make Play work in VR

### Step 1 — Compile clean + tests green  `[YOU · ~30 min]`
1. On the VR PC: `git pull` (gets all of today's commits).
2. Open the project in Unity 6.3 (`6000.3.16f1`). Let it finish importing — it generates `.meta` files for the new scripts (`CueIntensityTable`, `CueIntensityFile`, `Questionnaire`, `QuestionnairePanel`, `OperatorEStop`, + 2 test files).
3. **Commit those `.meta`:** `git add -A && git commit -m "Unity-generated .meta for new scripts" && git push`. *(Keeps GUIDs stable across machines.)*
4. **Window ▸ General ▸ Test Runner ▸ EditMode ▸ Run All** → everything green (includes the new `CueIntensityTableTests` + `QuestionnaireScoringTests`).
- **Done when:** no console compile errors; all EditMode tests pass.

### Step 2 — VR rendering via SteamVR + OpenXR  `[YOU · ~1 hr first time]`
1. **Install apps (PC):** Steam → **SteamVR**; **VIVE Streaming** (HTC) for wireless dev, *or* use the wired DisplayPort streaming kit (lower latency — preferred for the real study). Connect the Focus Vision and confirm it shows your desktop in VR.
2. **Make SteamVR the OpenXR runtime:** SteamVR ▸ **Settings ▸ Developer ▸ Set SteamVR as OpenXR runtime** (or VIVE Streaming's equivalent).
3. **Unity packages:** **Window ▸ Package Manager** → confirm **OpenXR Plugin** is installed (it is). **Install** **XR Interaction Toolkit** (gives you the XR Origin rig + controller tracking) if not present.
4. **Enable XR:** **Edit ▸ Project Settings ▸ XR Plug-in Management** → **PC/Standalone** tab → tick **OpenXR**. Under **OpenXR**:
   - Add an **Interaction Profile** → **HTC Vive Controller Profile** (and/or the Focus controllers' profile).
   - Tick the **HTC Vive Tracker** feature (greyed warnings are fine now; needed in Step 7).
   - Resolve any red ⚠ (click → Fix all).
5. Open **`Dryrun.unity`**, press **Play** with the headset on.
- **Done when:** the scene renders in the HMD and your head movement updates the view.

### Step 3 — Build the rig in `Dryrun.unity`  `[YOU · ~1 hr]`
The scene has `Obstacles`, `Spawner`, `Session` but only a plain `Main Camera`. Add the XR rig + the device objects:
1. **XR Origin:** delete (or disable) `Main Camera`. **GameObject ▸ XR ▸ XR Origin (VR)**. This creates `XR Origin` → `Camera Offset` → `Main Camera` (head) + `Left/Right Controller`. Set the XR Origin position to your play-area origin (floor center).
2. **bHaptics:** drag **`Assets/Bhaptics/SDK2/Prefabs/[bhaptics].prefab`** into the scene. (Runtime needs the **bHaptics Player** desktop app running + devices paired — see Step 6.)
3. **BodyTrackerRig:** create an empty GameObject `BodyTrackerRig`, **Add Component ▸ Body Tracker Rig**, and assign:
   - `head` = the XR `Main Camera` transform,
   - `leftHand` / `rightHand` = the `Left/Right Controller` transforms,
   - `chest` / `leftFoot` / `rightFoot` = **leave empty for now** (filled in Step 7).
   *(With 3 of 6 slots filled, `IsComplete` is false — SessionRunner will warn. For the technical pilot, see the note below.)*
4. **New UI components (this session):** create two empty GameObjects and add **`QuestionnairePanel`** and **`OperatorEStop`** respectively. (They render via IMGUI on the desktop mirror; no Canvas needed.)
5. **VisualObstacleAlert:** add a **`VisualObstacleAlert`** component (e.g., on the `Obstacles` object) if not already present.
6. **Session:** select `Session`; confirm it has **`SessionRunner`** (full study) — its Scene-wiring fields auto-find the above on Play, or assign them explicitly. Set `participantId`, `useLiveHaptics = true`.
- **Done when:** Play logs `[SessionRunner] Protocol: block 180s …` and warns only about things you intend to add later.

> **Technical-pilot shortcut (no trackers):** `BodyTrackerRig.IsComplete` requires all 6. To dry-run with head+controllers only, either (a) temporarily assign the 3 empty slots to **any** tracked transforms (e.g., the camera) so it runs, or (b) use **`LiveSessionController`** on a simple object and test the hand-localized conditions. Real data needs Step 7.

---

## Part B — Make the task physical

### Step 4 — The dodge/orb task  `[AUTOMATIC now]`
The **primitive-spawn fallback is built in** (committed): with `orbPrefab`/`projectilePrefab` left empty on `Spawner`, it auto-spawns a **cyan trigger sphere** (orb, reach target) and a **red flying sphere** (projectile) at each of the 12 scheduled events. **No art needed for the pilot.**
1. Select `Spawner`; confirm **`spawnPrimitivesIfNoPrefab = true`** (default). Optionally set `aimTarget` = the head camera so projectiles fly at the participant; tune `projectileSpeed`, `orbSize`, `projectileSize`.
2. To use real art later: assign `orbPrefab` / `projectilePrefab` (the fallback steps aside automatically).
- **Done when:** in Play, spheres appear at 8 s, 22 s, … 152 s (watch the console `[OpportunitySpawner] E# @ t=…` lines).

### Step 5 — Obstacle coordinates  `[YOU · needs the design docs (A1)]`
1. Retrieve the Layout-L1 surveyed coordinates + obstacle sizes from `IEEEVR2027_Layout1_Storyboard.md` (Task A1; copy the docs into `/docs`).
2. In `Dryrun.unity`, select the obstacle children (`01`/`02`/`03` under `Obstacles`) and set their **Transform positions** + **BoxCollider** sizes to the surveyed values **in SteamVR world space** (origin = your XR Origin floor point).
3. Confirm the **`SceneObstacles`** component on `Obstacles` lists O1/O2/O3 (it reads the child colliders). The obstacle GameObject **names must match** the obstacle Ids the schedule uses (`O1`,`O2`,`O3`) for the visual alert to find them.
- **Done when:** `SceneObstacles.Collect()` returns 3 obstacles at the surveyed coordinates (no "No scene obstacles" warning).

---

## Part C — Hardware

### Step 6 — bHaptics suit  `[YOU]`
1. Run the **bHaptics Player** desktop app; pair the **TactSuit X40** (vest) + the **Tactosy** units (hands + feet) so all show connected.
2. With `[bhaptics]` in the scene and `useLiveHaptics = true`, press Play and trigger a cue (e.g., run a block in `PB`).
- **Done when:** RG/RB/PG/PB produce the correct tactor buzz; None/Visual produce none. (Use `HapticSelfTest` for a per-site sweep.)

### Step 7 — Ultimate Trackers (when they arrive)  `[YOU · ~30 min]`
1. Plug the **dongle**; in **VIVE Hub / SteamVR** pair the **3 Ultimate Trackers**; assign roles **chest**, **left foot**, **right foot** (SteamVR ▸ Devices ▸ Manage Trackers → role).
2. In Unity each tracker appears as an OpenXR tracked device. Add a **`TrackedPoseDriver`** (Input System) to three GameObjects bound to the tracker pose actions (OpenXR HTC Vive Tracker bindings), or use the SteamVR/Focus tracker objects.
3. Drag those three tracker transforms into the empty `BodyTrackerRig` slots (`chest`, `leftFoot`, `rightFoot`). Now `IsComplete = true`.
- **Done when:** all 6 `BodyTrackerRig` slots are filled and track live.

---

## Part D — Calibrate & measure (trackers on)

### Step 8 — Tracking bench  `[YOU]`
1. Open **`Bench_M4.unity`** (it reads the `BodyTrackerRig`); press Play; do fast dodge/reach motions.
2. Read the HUD: delivery Hz, max gap, per-joint validity/jumps/freeze.
- **Done when:** PASS — all 6 joints clean (no invalid/jumps, freeze ≤ 250 ms, rate ≥ ~30 Hz, gap ≤ 100 ms) under fast motion. Inside-out trackers can occlude/drift — note any limb that struggles.

### Step 9 — Latency + contact radius  `[🤝 you measure → set values]`
1. **Latency:** measure tracker-motion → cue delay (one-shot; e.g., film the HMD mirror + a tactor, or log frame stamps). Set **`pipelineLatencySeconds`** on `SessionRunner` to that value.
2. **Contact radius:** with a tracker on the wrist/ankle, touch an obstacle and read the residual distance; set **`DetectorParams.LimbContactRadius`** per limb (the strap-to-fingertip/toe offset, ~0.10–0.15 m). *(Default 0 = off.)*
3. **Cue intensity (optional for pilot 1):** run flat, or do the **E2** perceptual match later and point `cueIntensityFile` at the result.
- **Done when:** latency + radii reflect the physical rig; cues fire at the right moment/distance.

---

## Part E — Lab & ethics (real participants)

### Step 10 — Physical lab + safety  `[YOU/LAB]`
1. Place the **foam obstacles** at the surveyed coordinates (match Step 5). Soft, no hard edges, stable but yielding (see `docs/SAFETY_PROTOCOL.md`).
2. Set the **SteamVR chaperone** boundary around the arena with ≥ 0.5 m clear margin.
3. **Rehearse the e-stop:** Play a block, hit the on-screen **E-STOP** button and the **`Esc`** key — confirm the block ends, haptics stop, the red veil shows, and `estop_log.csv` gets a row. Optionally wire `OperatorEStop.onEmergencyStop` to HMD passthrough.
4. Train the **spotter**; agree the **stop-word**.
- **Done when:** the safety checklist in `SAFETY_PROTOCOL.md` Appendix A passes.

### Step 11 — IRB + consent + instruments  `[YOU/PI]`
1. Finalize `docs/SAFETY_PROTOCOL.md` (institutional letterhead, bracketed items) + the **consent form**; submit/obtain **IRB** approval. *(Required before real participants; not for a technical pilot of yourself.)*
2. **F2:** replace the approximate IPQ/NASA-TLX/SSQ wording in `Core/Questionnaire.cs` with the official licensed items (structure/scoring already correct).
3. Pre-register (`docs/PREREGISTRATION.md`) once N is set.
- **Done when:** IRB number on file; consent ready; official items in.

---

## Part F — Verify the whole pipeline

### Step 12 — End-to-end dry-run  `[🤝]`
1. Run a full session (synthetic via `MockKeypointSource`, or yourself with head+controllers). Use a fresh `participantId`.
2. Check `sessions/P###/` contains **`summary.csv`, `events.csv`, `keypoints_*.csv`, `questionnaire.csv`** (+ `practice_*`). Answer the IMGUI questionnaires between blocks.
3. Run the analysis: `Rscript Analysis/analysis.R sessions/P###/summary.csv` → completes without error (questionnaire models run if `questionnaire.csv` is present + `lme4` installed).
- **Done when:** all CSVs write and `analysis.R` runs clean on your pilot data.

---

## Quick troubleshooting
| Symptom | Likely cause / fix |
|---|---|
| Play is a flat desktop window | XR not enabled (Step 2.4) or SteamVR not the OpenXR runtime (Step 2.2). |
| `[SessionRunner] No complete BodyTrackerRig … ABORTING` | A `BodyTrackerRig` slot is empty — fill all 6 (Step 3.3 / 7), or use the technical-pilot shortcut. |
| No haptics | bHaptics Player not running / devices unpaired (Step 6), or `useLiveHaptics = false`. |
| "No scene obstacles found" | `SceneObstacles` missing or obstacle colliders not under it (Step 5.3). |
| No orbs/projectiles | `spawnPrimitivesIfNoPrefab` is off and no prefab assigned (Step 4). |
| Questionnaires never appear | No `QuestionnairePanel` in the scene (Step 3.4) — SessionRunner logs a warning and skips. |
| E-stop does nothing | No `OperatorEStop` in the scene (Step 3.4). |

*Created 2026-06-23. Pairs with `PRE_PILOT_PLAN.md` (what/who) and `IMPLEMENTATION_PLAN.md` (full plan).*
