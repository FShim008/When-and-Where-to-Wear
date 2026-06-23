# PRE-PILOT PLAN — get the study pilot-ready while the VIVE trackers ship

**Goal:** drive the study to **"tracker-ready"** — done to the point where the *only* things left when the trackers arrive are: pair them, assign 3 Transform slots, a one-shot latency measure, and the pilot. Everything here uses gear you **already have** (VIVE Focus Vision + its controllers + the bHaptics suit) and the hardware-free software.

**Baseline config (decided):** head = **HMD**, hands = **the 2 controllers**, **3 Ultimate Trackers** = **chest + left ankle + right ankle**. (Add wrist trackers later only if free hands prove necessary for the avoidance task.) → you only need **one 3+1 Kit**.

**Owner tags:** `[PI]` · `[IRB]` · `[STAT]` · `[ENG]` · `[LAB]` · `[CLAUDE]` (I can build it). Task IDs reference `IMPLEMENTATION_PLAN.md`.

---

## The finish line — what the trackers actually gate (small)
When the 3 trackers + dongle arrive: **(1)** pair in SteamVR/VIVE Hub + assign roles; **(2)** drop the 3 tracker Transforms into the `BodyTrackerRig` chest/L-foot/R-foot slots (head + hands already wired); **(3)** measure tracker→cue latency (6.1) + set `LimbContactRadius` from a touch test (7.3); **(4)** pilot. Everything below gets you to that 1-day finish.

---

## Workstreams (run in PARALLEL — different owners)

### A — Ethics, authority & stats · THE CRITICAL PATH `[PI][IRB][STAT]`
> These take **weeks** and gate running any participant. The trackers arriving in days is **not** the bottleneck — this is. Start today.
- **A1 Retrieve the 6 design docs** → repo `/docs`; reconcile every value in [IMPLEMENTATION_PLAN Appendix A]. *(Done: docs in repo + a "reconciled" note.)*
- **A2 IRB submission + written safety protocol** — spotter, chaperone boundary, foam spec/padding, cable management, e-stop, SSQ pre/post, exclusions, consent. *(Done: IRB submitted; protocol drafted.)* **← do first; longest lead.**
- **A3 Power analysis** on `analysis.R` + `simulate_mock_data.R` → tentative **N** + #opportunities (refine post-pilot). *(Done: power report + draft N.)*
- **A4 Pre-registration draft** + finalize `analysis.R`; resolve decisions D1–D8 where possible. *(Done: pre-reg draft; analysis runs stub-free on mock.)*

### B — VR bring-up with the headset + controllers `[ENG]`
- **B1 Focus Vision PC-VR via SteamVR + enable OpenXR.** Wireless Vive Streaming is fine for dev now; switch to the wired DisplayPort kit when it arrives. *(Done: Play renders to the HMD.)*
- **B2 Add XR Origin to `Dryrun`;** head + both controllers track 1:1.
- **B3 Build `BodyTrackerRig`;** wire **head = XR camera, leftHand/rightHand = the 2 controllers.** Chest/L-foot/R-foot slots stay empty for now. *(Done: 3 of 6 joints live.)*
- **B4 Build settings:** add `Dryrun` + `Bench_M4`.
- **B5 Pre-stage the tracker path:** enable the OpenXR **HTC Vive Tracker** feature + make a `TrackedPoseDriver` template, so the 3 trackers are a ~10-minute drop-in on arrival.

### C — Validate the whole software pipeline (no hardware) `[ENG][CLAUDE]`
- **C1 Run EditMode tests** — all green.
- **C2 Full synthetic dry-run:** `SessionRunner` with `MockKeypointSource` → practice + 6 Williams-ordered blocks + breaks → confirm all four CSVs (summary / events / keypoints / questionnaire) write and feed `analysis.R` clean. *(Catches software bugs before the pilot.)*
- **C3 Partial LIVE test (after B):** run `LiveSessionController` with real head + 2 controllers (chest/feet read 0). Validate the **hand-localized** conditions (RB/PB) fire on real hand motion and the bHaptics **hand Tactosy** buzz. *(Exercises the real tracker path for 2 of 5 cue sites now.)*

### D — Experimental scene + task `[ENG]`
- **D1 Build the dodge/orb task** (4.2): author `orbPrefab` / `projectilePrefab` / `aimTarget`; `OpportunitySpawner` driven on the block clock; verify the 12 events fire at 8,22,…,152 s.
- **D2 bHaptics in scene** + `useLiveHaptics`; each site fires per condition (suit on, bench).
- **D3 `VisualObstacleAlert`** object configured over the obstacles.
- **D4 Protocol params** (`blockSeconds`; IDs from `SessionRunner`).
- **D5 E-stop + operator HUD** (controller button + key → end block, unblank, log abort).

### E — Cue-salience fix (needs the suit, NOT trackers) `[ENG][PI]`
- **E1 Per-site cue-intensity mechanism** in `HapticDeviceBinding` (per-`HapticSite` intensity loaded from a file).
- **E2 Perceptual-match procedure** (method-of-adjustment / 2-up-1-down staircase) + a `CueIntensityCalibration` step; pilot the matching on the suit. *(Closes your single biggest internal-validity confound — zero tracking needed.)*

### F — Instruments & UI `[ENG][STAT]`
- **F1 Questionnaire UI** (IPQ / NASA-TLX / SSQ) wired to `SessionRunner.RecordQuestionnaire`. *(The data pipeline + `analysis.R` already consume it.)*
- **F2 Gather validated item sets + scoring; finalize the consent script.**

### G — Physical lab & logistics `[LAB][ENG]`
- **G1 Place foam obstacles** at the surveyed Layout-L1 coords (after A1); set scene `BoxCollider`s to match in SteamVR world space; set the chaperone boundary around the arena.
- **G2 Tracker mounting** on the suit (chest + both ankles) — consistent, repeatable mounts (straps/clips); decide + acquire now so the mount→contact offset is stable across participants.
- **G3 Spotter training, cable management, room layout, data/log storage.**

### H — Detector prep `[ENG][PI]`
- **H1** Set contact/near-miss thresholds from the **foam half-extent + a documented tolerance** (7.2); plan the `LimbContactRadius` values (7.3). Final numbers need a tracker touch test, but the approach + foam-based thresholds lock now.

---

## Suggested order
1. **Today:** **A2 (IRB)** + **A1 (docs)** — start the long-lead path. In parallel: **C1/C2** (validate software, no hardware) and **B1** (VR rendering).
2. **This week:** **B2–B5** (rig: head + controllers), **D1** (the task), **D2–D4** (scene), **F1** (questionnaire UI).
3. **Then:** **E** (cue matching on the suit), **G1** (arena once docs are in), **D5** (e-stop), **H1** (thresholds), **C3** (partial live hand-cue test).
4. **A3 / A4** (power, pre-reg) as the stats track matures.

---

## Definition of "tracker-ready" (this plan's goal)
- ☐ IRB submitted + safety protocol drafted; docs reconciled; power/pre-reg drafted (A)
- ☐ VR renders; rig wired with head + controllers; build settings set (B)
- ☐ Full synthetic pipeline validated; partial live hand-cue test passes (C)
- ☐ Scene complete: dodge/orb task, haptics, visual alert, e-stop (D)
- ☐ Cue intensities perceptually equalized (E)
- ☐ Questionnaire UI live (F)
- ☐ Arena placed; tracker mounting + spotter ready (G)
- ☐ Detector thresholds set (H)

## When the 3 trackers + dongle arrive (≈1 day → pilot)
1. Pair the 3 trackers in SteamVR/VIVE Hub; assign chest / L-foot / R-foot roles.
2. Drop the 3 tracker Transforms into the `BodyTrackerRig` slots.
3. M4 `TrackingBench`: confirm all 6 joints track clean under fast dodges.
4. Touch test → set `LimbContactRadius`; latency measure → set `pipelineLatencySeconds`.
5. **Pilot** (Task 8.2) → refine power (A3) → lock & run.

---

## What Claude can build now (no hardware)
- **F1** questionnaire UI scaffold · **E1** per-site cue-intensity mechanism · **D5** e-stop + operator HUD · the **B1/B5** SteamVR + OpenXR + `BodyTrackerRig` walkthroughs · guide **C2/C3**.

*Created 2026-06-22. Tracker-independent execution view of `IMPLEMENTATION_PLAN.md` (single-PC VIVE architecture).*
