# Vicon Tracking Setup — co-registering Vicon with Unity/SteamVR

Replaces the VIVE Ultimate Trackers as the limb-tracking source. Rationale: the collision detector decides
contact at a **0.03 m** band, which is the same order as inside-out tracker error; Vicon's sub-millimetre
accuracy leaves two orders of magnitude of headroom and genuinely realizes the study's "idealized oracle"
framing. The head still comes from the **HMD** (already in Unity space, zero alignment error) — Vicon supplies
the five cue-able limbs.

## Components (all new)
| File | Role |
|---|---|
| `Core/RigidTransformSolver.cs` | Horn's method rigid fit + **residual reporting**; `RigidAlignment` applies/inverts/serializes |
| `Runtime/ViconAlignmentFile.cs` | load/save the 4×4 matrix (notebook-compatible) |
| `Runtime/ViconCalibrationRecorder.cs` | live in-engine calibration with a pass/fail residual |
| `Runtime/ViconBodyRig.cs` | drop-in that fills `BodyTrackerRig` from aligned Vicon Transforms |
| `Tests/EditMode/RigidTransformSolverTests.cs` | synthetic-transform recovery, residual, round-trips |

Nothing downstream changes — `SessionRunner`, `LiveSessionController`, and `TrackingBench` keep reading
`BodyTrackerRig` exactly as before (same seam the controller stand-in uses).

## Relationship to `transformation_matrix_new_vicon_vive.ipynb`
- **Cell 0 (Kabsch on paired points) is the sound method** and is what the solver reproduces (Horn's method,
  numerically equivalent, no SVD dependency). Its printed label says "Vicon→Vive" but the math maps
  **Vive→Vicon** — direction matters; the runtime needs **Vicon→Unity**, which the recorder produces directly.
- **Cells 2–3 have a frame-mixing bug:** `Rv_inv.dot(m1 - p_v)` subtracts a Vive-space position from a
  Vicon-space marker (two unaligned frames) to derive "local marker offsets", which are then used to compute the
  alignment — circular. It shows up as ~1.8 m offsets for markers mounted on a controller, and as cell 3
  disagreeing with cell 0. **Do not use cell 3's matrix.**
- **What was missing in both: a fit residual.** Without it there is no way to know the alignment is tighter than
  the 30 mm contact band. The recorder reports RMS + worst-case live.
- An existing notebook matrix can still be used: paste it into `vicon_alignment.txt` (brackets/commas fine) —
  but only if it is the Vicon→Unity direction, and prefer re-calibrating in-engine to get the residual.

## Setup

**1. Vicon side (you)**
- Rigid bodies / marker clusters: **chest, both wrists, both ankles**, plus **one on the HMD** (calibration only).
- Wand-calibrate; confirm camera coverage spans the full 3.5 × 3.5 m arena **at floor level** (ankle markers
  during crouches are the stress case — the one condition that can flip the Vicon-vs-tracker recommendation).
- Stream to the VR PC via the Vicon DataStream Unity plugin so each body appears as a Transform.

**2. Calibrate (per session, ~2 min)**
1. Empty GameObject → **Add Component ▸ Vicon Calibration Recorder**.
2. Assign `hmdUnity` = the XR camera; `hmdVicon` = the HMD's Vicon cluster Transform.
3. Set `unitScale`: **1** if the plugin streams metres, **0.001** if millimetres. A rigid fit cannot absorb a
   unit error, so getting this wrong makes everything meaningless.
4. Play → **● Record** → walk the headset around the whole volume at several heights, including crouches.
   Samples auto-record every 15 cm of movement; watch the coverage diagonal grow.
5. Read the residual: **≤ 5 mm green (good) · ≤ 10 mm yellow (usable) · > 10 mm red (redo)**.
6. **💾 Save alignment** → writes `vicon_alignment.txt`.

**3. Wire the rig (once)**
1. On the `BodyRig` GameObject, **remove `ControllerTrackerStandIn`** (it would overwrite the slots).
2. **Add Component ▸ Vicon Body Rig**. Assign `head` = XR camera, and the five Vicon limb Transforms.
3. Set `alignmentFile` = `vicon_alignment.txt` and the same `unitScale` as the recorder.
4. Play → expect `[ViconBodyRig] rig complete — … 5 limbs from Vicon via alignment (rms … mm)`.

**4. Validate (before any participant)**
- **Touch test:** reach a controller/hand to a known physical point; confirm the in-engine limb lands there.
- **`Bench_M4`:** fast dodges, all six joints clean — watch for marker dropouts during crouches.
- Set per-limb `DetectorParams.LimbContactRadius` for the marker→contact-surface offset (wrist strap → fingertip,
  ankle → toe), the same Task 7.3 mechanism the tracker plan used.

## Notes
- A missing alignment file logs an **error** and falls back to identity — deliberately loud, because a silent
  identity means "Vicon coordinates are Unity coordinates", which they are not.
- Marker dropouts hold the last position rather than snapping to the origin, so a dropout cannot fabricate a
  collision at the arena centre.
- Worth doing once both systems exist: a **dual-tracked session** quantifying Ultimate-tracker error against
  Vicon ground truth — a free validation paragraph for the methods.

*Created 2026-08. Pairs with `STUDY_DESIGN_V2.md` (virtual hazards) and `PILOT_SETUP_GUIDE.md`.*
