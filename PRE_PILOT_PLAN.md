# PRE-PILOT PLAN — get the study pilot-ready while the VIVE trackers ship

**Goal:** drive the study to **"tracker-ready"** — done to the point where the *only* things left when the trackers arrive are: pair them, assign 3 Transform slots, a one-shot latency measure, and the pilot. Everything here uses gear you **already have** (VIVE Focus Vision + its controllers + the bHaptics suit) and the hardware-free software.

**Baseline config (decided):** head = **HMD**, hands = **the 2 controllers**, **3 Ultimate Trackers** = **chest + left ankle + right ankle**. (Add wrist trackers later only if free hands prove necessary for the avoidance task.) → you only need **one 3+1 Kit**.

### Who does each task — execution badge (read this first)
- **🧑 YOU** — only you can do or test it: physical hardware, in-Editor scene wiring, IRB/ethics, perceptual judgments on the suit, pressing Play.
- **🤖 ME** — I implement it fully in code / scripts / docs with **no hardware**; you just review + commit.
- **🤝 TEAM** — **I write the code, you wire it in the Editor and test it.** (The hand-off is spelled out per task: *I build X → you do Y*.)

`[PI]`/`[IRB]`/`[STAT]`/`[ENG]`/`[LAB]` = which of *your* hats. Task IDs reference `IMPLEMENTATION_PLAN.md`.

---

## ✅ Progress (2026-06-22) — the entire 🤖 column is done
Implemented + committed: **E1** per-site cue intensity (`0de28b3`) · **F1** questionnaire UI (`6f9e4fe`) · **D5** e-stop (`95a7570`) · **D4** protocol guard (`8d2f197`). Verified already-built: **D3** VisualObstacleAlert · **D1** OpportunitySpawner. Drafted: **A2** safety protocol (`316eee8`) · **A3** power script (`394da98`) · **A4** analysis-plan finalize (`a7a56ac`). All pushed.
Remaining = the 🧑 / 🤝 rows only: VR/Editor bring-up (B), live tests (C2/C3), prefab authoring (D1–D3 art), **E2** perceptual matching (suit), **F2** official item wording, lab logistics (G), and submitting IRB. **Next human step:** `git pull` on the VR PC, open Unity (generates `.meta` for the new scripts — commit those), run EditMode tests.

## ✅ Progress (2026-07-06) — 🤖 queue re-verified end-to-end; **E2 built** (the last code gap closed)
Full pass over every 🤖 row against the actual codebase: **E1, F1, D5, D3, D1, D4** confirmed implemented and high-quality; **A2** (`docs/SAFETY_PROTOCOL.md`), **A3** (`Analysis/power_analysis.R`), **A4** (stub-free `Analysis/analysis.R` — the locked analysis plan: NB-GLMM + FLOOR + H4 + presence/TLX/SSQ) confirmed complete; **H1** threshold mechanism (`DetectorParams.LimbContactRadius`, default-off) and **B1/B5** walkthroughs (`docs/PILOT_SETUP_GUIDE.md` Steps 2 & 7) in place.
**Built E2 — the one thing that was missing:** the `CueIntensityCalibration` step. New code — `Core/Staircase` (adaptive 2AFC staircase → the Point of Subjective Equality) + `Core/CueIntensityCalibration` (per-site matching → `CueIntensityTable`) + `Integration/CueIntensityCalibrationRunner` (bHaptics playback, operator UI, per-trial logging, writes `cue_intensity.csv`) + `HapticDeviceBinding.PlayThreePulse` + EditMode tests (`StaircaseTests`, `CueIntensityCalibrationTests`).
**Verified** by compiling the pure Core standalone (`dotnet`) against a synthetic observer: the staircase converges to the PSE from both directions; the calibration **attenuates the 40-motor chest** and **boosts the 3-motor Tactosys** to equal perceived magnitude (all within tolerance of the analytic match); unreachable sites are flagged. → the entire 🤖 column is now code-complete.
**Next human step:** `git pull`; open Unity (commit the new `.meta`); run EditMode tests (now includes the two calibration suites); then run the **E2 match on the suit** (`docs/PILOT_SETUP_GUIDE.md` Step 9.3) to write `cue_intensity.csv`, and point `SessionRunner.cueIntensityFile` at it.

## The finish line — what the trackers actually gate (small)
When the 3 trackers + dongle arrive: **(1)** pair in SteamVR/VIVE Hub + assign roles; **(2)** drop the 3 tracker Transforms into the `BodyTrackerRig` chest/L-foot/R-foot slots (head + hands already wired); **(3)** measure tracker→cue latency (6.1) + set `LimbContactRadius` from a touch test (7.3); **(4)** pilot. Everything below gets you to that 1-day finish.

---

## Workstreams (run in PARALLEL — different owners)

### A — Ethics, authority & stats · THE CRITICAL PATH `[PI][IRB][STAT]`
> These take **weeks** and gate running any participant. The trackers arriving in days is **not** the bottleneck — this is. Start today.
- **A1 Retrieve the 6 design docs** → repo `/docs`. **🤝 TEAM** — *you copy them from OneDrive into the repo → I reconcile every value against `IMPLEMENTATION_PLAN` Appendix A.* *(Done: docs in repo + a "reconciled" note.)*
- **A2 IRB submission + written safety protocol.** **🤝 TEAM** — *I draft the safety-protocol document (spotter, chaperone boundary, foam spec/padding, cable management, e-stop, SSQ pre/post, exclusions, consent skeleton) → you review, put it on institutional letterhead, and submit to the IRB.* **← do first; longest lead.**
- **A3 Power analysis.** **🤝 TEAM** — *I write the Monte-Carlo power script on `analysis.R` + `simulate_mock_data.R` → you run it and pick the tentative **N** + #opportunities (refine post-pilot).*
- **A4 Analysis-plan lock.** **🤝 TEAM** — *I finalize `analysis.R` (stub-free) + resolve decisions D1–D8 where code can → you make the final scientific calls (hypotheses, model, exclusions, stopping rule).*

### B — VR bring-up with the headset + controllers `[ENG]`
- **B1 Focus Vision PC-VR via SteamVR + enable OpenXR.** **🧑 YOU** *(I'll give you the step-by-step walkthrough)* — install SteamVR + VIVE Streaming, set OpenXR runtime, enable Unity OpenXR. Wireless is fine for dev now; wired DisplayPort kit later. *(Done: Play renders to the HMD.)*
- **B2 Add XR Origin to `Dryrun`;** head + both controllers track 1:1. **🧑 YOU** *(Editor; I guide.)*
- **B3 Wire `BodyTrackerRig`** — head = XR camera, leftHand/rightHand = the 2 controllers; chest/feet slots empty for now. **🤝 TEAM** — *the `BodyTrackerRig` component is already written (mine) → you drag the HMD + 2 controller Transforms into its slots.* *(Done: 3 of 6 joints live.)*
- **B4 Build settings:** add `Dryrun` + `Bench_M4`. **🧑 YOU** *(Editor.)*
- **B5 Pre-stage the tracker path:** enable the OpenXR **HTC Vive Tracker** feature + make a `TrackedPoseDriver` template. **🧑 YOU** *(Editor; I guide)* — so the 3 trackers are a ~10-min drop-in on arrival.

### C — Validate the whole software pipeline (no hardware) `[ENG]`
- **C1 Run EditMode tests** — all green. **🧑 YOU** *(click Test Runner ▸ Run All; the tests are mine — tell me any red and I fix.)*
- **C2 Full synthetic dry-run:** `SessionRunner` + `MockKeypointSource` → practice + 6 Williams blocks + breaks → all four CSVs write + feed `analysis.R` clean. **🤝 TEAM** — *I build/verify the synthetic harness (`SyntheticBlock`) + the runner wiring → you press Play and confirm the CSVs appear.*
- **C3 Partial LIVE test (after B):** real head + 2 controllers (chest/feet read 0); validate the **hand-localized** conditions (RB/PB) fire on real hand motion + the bHaptics **hand Tactosy** buzz. **🧑 YOU** *(headset + suit on; I tell you exactly what to watch for.)*

### D — Experimental scene + task `[ENG]`
- **D1 Build the dodge/orb task** (4.2). **🤝 TEAM** — *I write `OpportunitySpawner` (drives the 12 events at 8,22,…,152 s off the block clock) → you author the `orbPrefab` / `projectilePrefab` / `aimTarget` art + drop them in the scene.*
- **D2 bHaptics in scene** + `useLiveHaptics`. **🤝 TEAM** — *the binding code (`HapticDeviceBinding`) is mine/done → you enable it in the scene + confirm each site buzzes per condition (suit on).*
- **D3 `VisualObstacleAlert`** over the obstacles. **🤝 TEAM** — *I write the component → you place/configure it in the scene.*
- **D4 Protocol params** (`blockSeconds`; IDs). **🤖 ME** — *I set the defaults in `SessionRunner`; you only override in the Inspector if needed.*
- **D5 E-stop + operator HUD** (button/key → end block, unblank, log abort). **🤝 TEAM** — *I write the e-stop + HUD code → you bind it to a controller button + test the abort.*

### E — Cue-salience fix (needs the suit, NOT trackers) `[ENG][PI]`
- **E1 Per-site cue-intensity mechanism** in `HapticDeviceBinding` (per-`HapticSite` intensity from a file). **🤖 ME** — *pure code; I build it.*
- **E2 Perceptual-match procedure** (method-of-adjustment / adaptive staircase) + a `CueIntensityCalibration` step. **🤝 TEAM** — ✅ **code built + verified** (`Core/Staircase` + `Core/CueIntensityCalibration` + `Integration/CueIntensityCalibrationRunner`, EditMode-tested; standalone-verified to converge on the equal-magnitude point) → *you run the matching on yourself/pilots wearing the suit (the judgment is inherently human): drop `CueIntensityCalibrationRunner` on a GameObject, press **Begin**, judge which pulse feels stronger; it writes `cue_intensity.csv`, then set `SessionRunner.cueIntensityFile` to it.* *(Closes your single biggest internal-validity confound.)*

### F — Instruments & UI `[ENG][STAT]`
- **F1 Questionnaire UI** (IPQ / NASA-TLX / SSQ) wired to `SessionRunner.RecordQuestionnaire`. **🤝 TEAM** — *I build the whole UI + wiring (the data pipeline + `analysis.R` already consume it) → you drop it in the scene + run it.*
- **F2 Gather validated item sets + scoring; finalize the consent script.** **🧑 YOU** — *the official IPQ/NASA-TLX/SSQ wording + your IRB consent text are institutional/licensed; give me the items and I'll load them into F1.*

### G — Physical lab & logistics `[LAB][ENG]`
- **G1 Place foam obstacles** at the surveyed Layout-L1 coords; match scene `BoxCollider`s in SteamVR space; set the chaperone boundary. **🤝 TEAM** — *you physically place + survey the foam coordinates → I set the scene colliders/positions to match.*
- **G2 Tracker mounting** on the suit (chest + both ankles) — repeatable mounts so the mount→contact offset is stable. **🧑 YOU** *(physical; decide + acquire straps/clips now.)*
- **G3 Spotter training, cable management, room layout, data/log storage.** **🧑 YOU** *(lab/logistics.)*

### H — Detector prep `[ENG][PI]`
- **H1** Set contact/near-miss thresholds from the **foam half-extent + a documented tolerance** (7.2); plan `LimbContactRadius` (7.3). **🤝 TEAM** — *you measure the foam block dimensions → I set the thresholds in code (final radius numbers need a tracker touch test later).*

---

## Suggested order
1. **Today:** **A2 (IRB)** + **A1 (docs)** — start the long-lead path. In parallel: **C1/C2** (validate software, no hardware) and **B1** (VR rendering).
2. **This week:** **B2–B5** (rig: head + controllers), **D1** (the task), **D2–D4** (scene), **F1** (questionnaire UI).
3. **Then:** **E** (cue matching on the suit), **G1** (arena once docs are in), **D5** (e-stop), **H1** (thresholds), **C3** (partial live hand-cue test).
4. **A3 / A4** (power, analysis plan) as the stats track matures.

---

## Definition of "tracker-ready" (this plan's goal)
- ☐ IRB submitted + safety protocol drafted; docs reconciled; power + analysis plan drafted (A)
- ☐ VR renders; rig wired with head + controllers; build settings set (B)
- ☐ Full synthetic pipeline validated; partial live hand-cue test passes (C)
- ☐ Scene complete: dodge/orb task, haptics, visual alert, e-stop (D)
- ☐ Cue intensities perceptually equalized (E) — *E2 routine built + verified; run `CueIntensityCalibrationRunner` on the suit to produce `cue_intensity.csv`*
- ☐ Questionnaire UI live (F)
- ☐ Arena placed; tracker mounting + spotter ready (G)
- ☐ Detector thresholds set (H)

## When the 3 trackers + dongle arrive (≈1 day → pilot)
1. Pair the 3 trackers in SteamVR/VIVE Hub; assign chest / L-foot / R-foot roles. **🧑 YOU**
2. Drop the 3 tracker Transforms into the `BodyTrackerRig` slots. **🧑 YOU**
3. M4 `TrackingBench`: confirm all 6 joints track clean under fast dodges. **🧑 YOU**
4. Touch test → set `LimbContactRadius`; latency measure → set `pipelineLatencySeconds`. **🤝 TEAM** *(you measure → I set).*
5. **Pilot** (Task 8.2) → refine power (A3) → lock & run. **🧑 YOU**

---

## Who does what — the two queues

### 🤖 MY queue — **✅ ALL DELIVERED** (no hardware)
| Task | What I deliver | Status |
|---|---|---|
| **E1** | per-site cue-intensity mechanism (code) | ✅ `Core/CueIntensityTable` + `Runtime/CueIntensityFile`, tested |
| **F1** | questionnaire UI (IPQ/NASA-TLX/SSQ) + wiring | ✅ `Core/Questionnaire` + `Runtime/QuestionnairePanel`, tested |
| **D5** | e-stop + operator HUD (code) | ✅ `Runtime/OperatorEStop` + SessionRunner abort |
| **D3** | `VisualObstacleAlert` component | ✅ `Runtime/VisualObstacleAlert` + `Core/VisualAlertModel`, tested |
| **D1** | `OpportunitySpawner` (the 12-event driver) | ✅ `Runtime/OpportunitySpawner` |
| **D4** | protocol-param defaults in `SessionRunner` | ✅ `SessionRunner.ValidateProtocol` |
| **A2** | safety-protocol document draft | ✅ `docs/SAFETY_PROTOCOL.md` |
| **A3** | Monte-Carlo power script | ✅ `Analysis/power_analysis.R` |
| **A4** | finalize `analysis.R` / lock the analysis plan | ✅ stub-free `Analysis/analysis.R` (NB-GLMM + FLOOR + H4 + presence/TLX/SSQ) |
| **E2** | the calibration routine + logging | ✅ **NEW** `Core/Staircase` + `Core/CueIntensityCalibration` + `Integration/CueIntensityCalibrationRunner`, tested (you run the match on the suit) |
| **H1** | detector threshold code | ✅ `DetectorParams.LimbContactRadius` mechanism (default-off; you supply foam dims / touch-test radius) |
| **B1/B5** | written SteamVR + OpenXR + tracker-feature walkthroughs | ✅ `docs/PILOT_SETUP_GUIDE.md` Steps 2 & 7 |

### 🧑 YOUR queue — only you (hardware / Editor / ethics / physical)
| Task | Why it's yours |
|---|---|
| **A1** | copy the design docs out of OneDrive into the repo |
| **A2** | submit to the IRB (institutional) |
| **B1–B5** | install SteamVR, build the XR rig + drag Transforms, build settings (in-Editor) |
| **C1** | press Run on the EditMode tests |
| **C2/C3** | press Play; live hand-cue test in headset + suit |
| **D1–D3** | author prefabs / place objects in the scene |
| **E2** | the perceptual intensity matching (human judgment on the suit) |
| **F2** | official instrument items + IRB consent wording |
| **G1–G3** | place/survey foam, tracker mounts, spotter, cabling, storage |

> Rule of thumb: **anything that compiles, I do. Anything you can touch, wear, place, judge, or click Play on, you do.** Every 🤝 row is me handing you finished code so your part is wiring + testing, not writing.

*Created 2026-06-22, badges added. Tracker-independent execution view of `IMPLEMENTATION_PLAN.md` (single-PC VIVE architecture).*
