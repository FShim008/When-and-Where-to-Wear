# Pilot Readiness — Paper 1, Ultimate Trackers only

**Prepared:** 2026-09-10 for a pilot session on 2026-09-11
**Design authority:** `../PAPER1_STUDY_DESIGN.md` · **Gap list:** `PAPER1_CODE_GAP.md`

---

## 1. Read this first — what this session can and cannot be

**This is a feasibility and instrumentation pilot, not a confirmatory session and not the tracker-selection
pilot.** Three things follow, and all three should be written into your lab notes before you start.

| Gate | Status for tomorrow |
|---|---|
| §14.1 ethics approval | **Not in place.** Run on yourself or trained lab staff as protocol development. Do **not** run a naive participant. |
| §14.18–20 tracker selection by **paired** comparison | **Not satisfied.** Those gates require Vicon and commodity trackers logging *simultaneously on the same movement*. A trackers-only session cannot close them, and choosing trackers because this session went well is explicitly "not a decision" (§11). |
| §14.24 sternum-Tactosy vs vest discrimination | **Not satisfied** unless you have five Tactosy units. See §6 below. |

**PI decision, 2026-09-11 — controllers are the final Paper 1 hand configuration, not a stand-in.**
Participants keep both controllers throughout, including measured blocks, because room-scale VR gameplay is
performed holding controllers and that matches the deployment scenario the study models. Recorded in
`../PAPER1_STUDY_DESIGN.md` §11. Three consequences for tomorrow:

- **Tomorrow's hand setup is the real one.** The pilot is more representative than originally planned — you are
  not validating a configuration you intend to replace.
- **Tracker count is settled at three for Paper 1** (chest, left ankle, right ankle). No wrist trackers, and
  the unimplemented wrist-to-hand offset is no longer needed.
- **The questionnaire hand-over and set-down steps are dropped.** Participants answer with a controller already
  in hand. Self-report validity is preserved because they still answer unobserved.

*Limitation to carry into the manuscript:* a held controller adds hand mass and alters reaching and braking
dynamics. Second-order for a binary violation outcome, but state it. Paper 2 is undecided and must choose
separately.

What it **can** establish, which is genuinely worth a day:

- that six real tracked joints feed the study brain cleanly during vigorous movement;
- **gate §14.10** — engagement, and whether the violation rate is off the floor and off the ceiling;
- that the new opportunity-level primary dataset reconstructs correctly (**gate §14.9**);
- per-limb tracker dropout and drift behaviour under real dodging (feeding §14.21);
- whether the session length and questionnaire load are tolerable.

Record the tracker-selection criteria **before** you run, per §14.18, even though this session cannot apply them.

---

## 2. What changed in the code today

| Change | Why it matters tomorrow |
|---|---|
| **Opportunity-level primary outcome** — new `opportunities.csv`, one row per scheduled opportunity, with `violation`, `presented`, `valid`, `invalid_reason`, entry episodes, first-entry time, min clearance, max penetration, unattributed contacts | Your pilot now produces the dataset the confirmatory model actually consumes. Without this you would have been validating a pipeline that is being replaced. |
| **Attribution now checks limb AND hazard** | Previously any open opportunity for that limb could absorb a collision. Verified: wrong limb, wrong hazard, and outside-window contacts no longer count as violations, and are retained as unattributed safety events. |
| **Validity gating** | An opportunity that never presented, or whose window the block never reached, is excluded with a reason instead of silently sitting in the denominator. |
| **Visual now fires on the PREDICTIVE trigger** | It was grouped with the reactive conditions, which silently confounded H4 with timing. Verified: Visual and PB both report `Predictive`, RB still reports `Reactive`. |
| **Rig accepts real chest and ankle trackers** | `ControllerTrackerStandIn` gained a `chestTracker` slot, so the intended baseline (HMD + 2 controllers + 3 trackers) wires up with no proxies. |
| **Console now reports REAL vs PROXY per joint, and the primary rate per block** | A silent fall back to proxies would look like a working session while producing meaningless torso and foot data. Now it says so. |
| **Provisional limb radii** (chest 0.12, hand 0.08, foot 0.10 m) | *Found in QA:* `LimbContactRadius` was never set, so every joint was a dimensionless point needing to pass within 3 cm of a hazard surface. That would very likely have floored the violation rate at zero and defeated the pilot's main purpose. These are PROVISIONAL, not the frozen anthropometric model. |
| **Self-representation: ankle markers** (`SelfRepresentation.cs`, added 2026-09-14) | With controllers in both hands, hands render as controller models and feet do not — so a hand-targeted opportunity was avoidable with visual self-knowledge and a foot-targeted one was not. That asymmetry is a pure artifact of the 2026-09-11 controllers decision and would have been invisible in the data. Markers also narrow the largest controllable real-vs-virtual behavioural gap (missing limb vision). Identical in all six conditions; see `VIRTUAL_HAZARD_VALIDITY.md` §3.5. |
| **Startup hazard-ID validation** | *Found in QA:* nothing checked that the hazards the schedule targets (`O1–O3`, `O4a`, `O4b`) actually exist. A missing or misnamed one is silent and fatal — that opportunity can never record a violation, so it reads as perfect avoidance forever. Now it logs an ERROR at startup and names the missing ids. |

All verified by compiling the pure Core standalone and running the scenarios. New EditMode tests:
`OpportunityOutcomeTests`.

---

## 3. Tonight, before the lab (~30 min, no hardware)

1. Open the project in Unity, let it compile. **Console must be clean.**
2. **Test Runner ▸ EditMode ▸ Run All.** `OpportunityOutcomeTests` should join the suite green.
3. Charge: HMD, both controllers, all three Ultimate Trackers, the bHaptics units.
4. Decide and **write down** your tracker-selection criteria (§14.18) even though you cannot apply them yet.
5. Decide your participant ID. `sessions/P900` already exists. **Use P910 for tomorrow** so nothing collides.

---

## 4. In the lab — setup (~45 min)

### 4.1 Pair the trackers
1. Plug in the wireless dongle. Pair all three Ultimate Trackers in VIVE Hub / SteamVR.
2. Assign roles: **chest, left foot, right foot**.
3. Confirm all three plus the HMD and both controllers report stable pose in SteamVR before opening Unity.

### 4.2 Get tracker Transforms into Unity
Enable the OpenXR **HTC Vive Tracker** feature, then add one GameObject per tracker with a
**TrackedPoseDriver** bound to that tracker's role. Detail in `PILOT_SETUP_GUIDE.md` Step 7.
**Verify each one moves** before wiring anything: select it, move the tracker, watch Transform ▸ Position change.

### 4.3 Wire the rig
On `BodyRig` → `Controller Tracker Stand In`:

| Slot | Assign |
|---|---|
| Head | XR **Main Camera** |
| Left Hand / Right Hand | **Left / Right Controller** |
| **Chest Tracker** | chest tracker Transform |
| **Left / Right Foot Controller** | ankle tracker Transforms |

Then add the **`SelfRepresentation`** component to the same `BodyRig` object. Leave its rig slot empty — it
finds `BodyTrackerRig` itself. Defaults are the decided configuration: **feet ON, hands off** (controller models
already render), **chest off**, marker **0.07 m**. Do not change these for the pilot; they must be identical for
every participant in the eventual sample.

Press Play and read the Console. You want:

```
Rig complete — head=… (REAL), leftHand=… (REAL), rightHand=… (REAL),
chest=… (REAL), leftFoot=… (REAL), rightFoot=… (REAL). All six joints are real devices.
```

**If any joint says PROXY, stop and fix it.** A proxy joint is not a measurement, and foot data from a proxy is
meaningless.

You should also see:

```
[SelfRepresentation] feet=ON, hands=off (controller models), chest=off, marker=0.070 m.
Identical in every condition — verify this matches the frozen protocol.
```

Then put the headset on and look down. Two small neutral markers at your ankles, moving with your feet. A marker
sitting frozen at the floor origin means that foot tracker is not reaching the rig.

### 4.4 Session settings
On `Session` → `SessionRunner`: **Participant Id = 910**, `useLiveHaptics` per §6 below, practice condition
**None**, cue tour **on**, layouts **all variants on**. Ctrl+S.

---

## 5. Run the pilot (~70 min)

Follow `SESSION_CHECKLIST.md`, with these additions:

1. **Bench check first.** Open `Bench_M4`, move fast, dodge hard, crouch. Watch for dropouts — especially the
   ankle trackers during crouches, which is the known stress case. Note anything that freezes or jumps.
2. Baseline SSQ → cue tour → practice (None) → six blocks → post SSQ.
3. **After every block, read the new Console line:**
   `PRIMARY: n/m valid opportunities violated (x%); p/12 presented.`
   This is gate §14.10 in real time. What you want to see:
   - **`presented` = 12 every block.** Anything less means stimuli are not spawning, and those opportunities
     drop out of the denominator.
   - **Violation rate off both ends.** All 0% means the task is too easy or the hazards are unreachable. All
     100% means it is too hard or the geometry is wrong. Either extreme makes the study unable to detect a
     condition difference, and it is better to find that tomorrow than after twenty participants.
   - **Expect the floor, not the ceiling.** Every direct real-vs-virtual comparison in the literature finds
     people leave *larger* margins around virtual hazards than physical ones (+0.16 m Fink 2007, +0.19 m
     Bühler & Lamontagne 2025). With provisional radii on top of that, 0% is the predicted failure. If block 1
     reads 0%, **suspect the contact geometry before you suspect the task** — reach into a hazard deliberately
     and confirm detection fires at all. Reasoning in `VIRTUAL_HAZARD_VALIDITY.md` §6b.
4. Rehearse the e-stop (`Esc`) once.
5. Note session length and whether the four instruments per block felt tolerable.

---

## 6. The haptics decision you must make and record

§2 of the design now calls for **five Tactosy units** carrying the warning cue (sternum, both forearms, both
feet), with the vest demoted to projectile feedback only. The code still routes the chest cue to the vest.

- **If you have five Tactosy units:** do not use them tomorrow without re-mapping the code first. That is Gap 4
  and it is not done.
- **If you do not (most likely):** run with the current vest-chest mapping and **record in your notes that this
  pilot ran Option B — the generic cue on a different device family from the localized cues.** Under Option B
  the Mapping factor is confounded with device, and every downstream claim must say "haptic mapping
  implementation" rather than anatomical localization. For a feasibility pilot that is fine. For confirmatory
  data it is not.
- Alternatively set `useLiveHaptics = false` and treat tomorrow as a pure tracking and measurement pilot. You
  lose the cue experience but nothing about the primary outcome.

---

## 7. What to bring back

Copy the whole `sessions/P910/` folder, and confirm it contains:

- **`opportunities.csv`** — the new primary dataset. Should be **72 rows** for six blocks (12 each), plus
  practice rows in `practice_opportunities.csv`.
- `summary.csv` (6 rows), `events.csv`, `keypoints_*.csv` (7), `questionnaire.csv`.

Then paste me `opportunities.csv`. I will check attribution, validity reasons, and whether the violation rate
sits in a usable range, and we can decide what to change before anyone else is run.

---

## 8. Known limitations of this specific session

These are not problems to fix tomorrow. They are things to write down so the pilot is not over-read later.

1. **Not the paired tracker comparison** (§14.18–20). Tracker selection remains open.
2. **Contact geometry is PROVISIONAL, not frozen.** The session now applies sphere radii (chest 0.12 m,
   hand 0.08 m, foot 0.10 m) instead of point joints, so the rate is not floored by geometry. But §5 requires
   capsule radii **frozen from anthropometry with the source recorded**, and these are engineering guesses.
   Tomorrow's violation rates are indicative only and will shift when the real model lands. Record the values
   you ran with.
3. **`pipelineLatencySeconds` is still 0** (gate §14.23). Predictive cues are not lead-compensated for real
   hardware delay.
4. **The Visual benchmark is still the old continuous hazard glow** (Gap 2). Its *trigger* is now correct, but
   its *form* is not — it should be a transient limb-anchored indicator. Treat the Visual block tomorrow as a
   rough feasibility check only.
5. **Questionnaire export is scored-only**, not item-level (Gap 9).
6. **No tracking-quality flags yet**, so validity cannot currently be failed for tracking loss — only for
   non-presentation and early block end.
7. **Marker diameter (0.07 m) is provisional**, chosen to sit below the smallest provisional contact radius
   (hand 0.08 m) so the marker shows where the limb *is* rather than disclosing the contact geometry. When
   §14.22 freezes the anthropometric radii, **re-check this inequality** — if a frozen radius drops below
   0.07 m the marker starts acting as a permanent proximity aid.
8. **The self-representation may moderate H2** (localization). It is a constant of the apparatus so it cannot
   confound the within-subject contrasts, but it bounds their generality and must be stated in the manuscript.
   `PAPER1_STUDY_DESIGN.md` §5 records the wording obligation.
