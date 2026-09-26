# PILOT DAY CHECKLIST — Paper 1, trackers only

**Print this.** Reasoning and context: `PILOT_READINESS_TRACKERS.md`. Session procedure: `SESSION_CHECKLIST.md`.

Date: `__________`  Operator: `__________`  Participant ID: **P910**  Start: `______`  End: `______`

> ⚠️ **Yourself or trained lab staff only.** No naive participants until ethics approval.
> This is a feasibility pilot. It does **not** close the tracker-selection gates (§14.18–20).

---

## A. Record these BEFORE touching Unity

Decisions made after seeing data are not decisions. Fill this in first.

- [ ] **Tracker-selection criteria recorded** (see §F below, copy the table into your notes with today's date)
- [ ] **Haptics option chosen:** ▢ Option B + E2 calibration ▢ Option B uncalibrated ▢ haptics OFF
- [ ] **Contact radii in use:** chest `0.12` / hand `0.08` / foot `0.10` m — provisional, not frozen
- [ ] **Self-representation:** ankle markers `ON`, hands = controller models, chest `off`, marker `0.07` m
      (decided 2026-09-14, `PAPER1_STUDY_DESIGN.md` §5 — must be identical for every participant)
- [ ] Session goal written in one line: `_________________________________________________`

## A2. ✅ O2 was moved for you — just confirm it (2026-09-14)

`docs/OPPORTUNITY_GEOMETRY_AUDIT.md`. O2 used to sit **0.04 m from your right hand when you stand normally at
the arena centre** — inside the 0.08 m hand contact radius — so E1, E6, E7 and E12 would have recorded a
violation **from posture alone, in every condition**. It has been moved to (0.95, 0.70, 0.60) and re-audited:
12/12 opportunities now pass.

**The scene was edited on disk with Unity closed, so the Editor has not loaded it yet. Confirm before running:**

- [ ] Open `Dryrun`, select **O2**, check Position reads **x = 0.95, y = 0.7, z = 0.6** and Scale still reads
      **0.35 / 1.4 / 0.35**
- [ ] Put the headset on and stand at the arena centre — **the pillar should be forward-right and clearly out
      of arm's reach at rest**, not beside you
- [ ] Console clean on Play (a malformed scene would fail to load, not fail quietly)

If anything looks wrong, the pre-move backup is `Assets/Scenes/Dryrun.unity.bak-20260914-031750`.

## A3. 🖶 Print this card and read it verbatim

`PAPER1_STUDY_DESIGN.md` §8. Read **once**, after the cue tour and before practice. Do not paraphrase — this
is the instruction that determines what the primary outcome means, and H2 depends on part 3.

> **1.** "You'll see glowing orbs appear around you. Reach out and touch them — each one is worth 1 point.
> Things will also be thrown at you. Get out of the way — each hit costs you 1 point."
>
> **2.** "Invisible hazard zones stand in the play space, like furniture you can't see. Move as though they
> were real. Each time any part of your body enters one, you lose 3 points. You won't feel or hear anything
> when it happens — you'll see the total at the end of the round."
>
> **3.** "During some rounds you'll feel a short vibration. It means a part of your body is heading into one of
> the zones. In some rounds you'll feel it on the body part that's at risk. In other rounds you'll feel it on
> your chest, which tells you that something is at risk but not which part. Either way: move clear and keep
> playing. You usually won't need to give up the orb — adjusting your reach is enough."
>
> **4.** "Some rounds have no vibration at all. The zones are still there, and they still cost points."
>
> **Before every block, identical every time:** "Same as before: collect the orbs, avoid what's thrown at you,
> and stay out of the zones."

- [ ] Card printed and to hand
- [ ] Questions asked by the participant, and the answers given: `________________________________________`
      (answer "what does the cue mean" — do **not** answer "which cue is better"; record either way)

## B. Room and power (~10 min)

- [ ] Floor cleared, 3.5 × 3.5 m plus margin, nothing to trip on
- [ ] Chaperone set, well outside the virtual arena
- [ ] HMD, both controllers, 3 trackers, dongle all charged and on
- [ ] SteamVR running
- [ ] Water within reach; phone on silent

## C. Pair the trackers (~15 min)

- [ ] All 3 Ultimate Trackers paired in VIVE Hub / SteamVR
- [ ] Roles assigned: **chest**, **left foot**, **right foot**
- [ ] **All six devices show stable pose in SteamVR** (HMD + 2 controllers + 3 trackers) before opening Unity

## D. Unity wiring (~25 min)

- [ ] OpenXR **HTC Vive Tracker** feature enabled
- [ ] One GameObject per tracker with a **TrackedPoseDriver** bound to its role
- [ ] **Each tracker Transform verified moving** — select it, move the tracker, watch Position change
  - [ ] chest   - [ ] left foot   - [ ] right foot
- [ ] `BodyRig` → **Controller Tracker Stand In** slots filled:
  - [ ] Head = XR **Main Camera**
  - [ ] Left Hand = **Left Controller**   - [ ] Right Hand = **Right Controller**
  - [ ] **Chest Tracker** = chest   - [ ] **Left Foot** = left ankle   - [ ] **Right Foot** = right ankle
- [ ] `BodyRig` → add **Self Representation** component (leave the rig slot empty — it finds `BodyTrackerRig`)
  - [ ] Show Feet ✅ ON   - [ ] Show Hands ☐ off   - [ ] Show Chest ☐ off   - [ ] Marker Diameter `0.07`
- [ ] `Session` → `SessionRunner` → **Participant Id = 910**
- [ ] **Ctrl+S**

## E. GO / NO-GO GATE ⛔

Press Play. All three lines must appear.

- [ ] `Rig complete — … All six joints are real devices.` ← **any PROXY = STOP**
- [ ] `hazard check OK — all 5 scene volumes resolve (O1, O2, O3, O4a, O4b)` ← **any ERROR = STOP**
- [ ] `contact geometry: PROVISIONAL radii chest=0.120 hand=0.080 foot=0.100 m`
- [ ] `[SelfRepresentation] feet=ON, hands=off (controller models), chest=off, marker=0.070 m`
      ← **any other configuration = STOP.** It must be identical for every participant in the sample.

Do not proceed past a failed gate. All three failures are silent killers of the primary outcome.

**Then look down.** You should see a small neutral marker at each ankle, and nothing else new. If a marker sits
frozen at the floor origin, that foot tracker is not feeding the rig — go back to step D.

## F. Tracker-selection criteria — copy into notes, dated

Today measures only some of these. Record the criteria anyway; the rule must predate the data.

| Metric | Threshold | Measurable today? |
|---|---|---|
| Positional error, 95th pct, per site, **at task speed** | < 25% of the capsule contact band | ✗ needs a reference (Vicon) |
| Valid-opportunity rate under dropout | ≥ 90% | ✓ from `opportunities.csv` |
| **Delivered lead-time jitter, predictive condition** | SD < 15% of `T` (T = 0.50 s → **< 75 ms**) | ✗ needs physical onset measurement |
| Tracking-loss events per session | recorded | ✓ observe and note |
| Setup time per participant | recorded | ✓ time phases C + D |

Setup time today: `______ min`  Tracking-loss events observed: `______`

## G. Bench check (~10 min)

- [ ] Open `Bench_M4`
- [ ] Move fast, dodge hard, **crouch deeply** (the known stress case for ankle trackers)
- [ ] Note any freeze, jump, or dropout: `_______________________________________________`

## H. Haptics (~15 min if running Option B + E2)

- [ ] bHaptics Player running; vest + all 4 Tactosy connected
- [ ] `HapticSelfTest` sweep — chest → L hand → R hand → L shin → R shin, **left/right not swapped**
- [ ] **E2 calibration run** → `cue_intensity.csv` written (expect the chest **well below 1.0**)
- [ ] `SessionRunner` → **Cue Intensity File = `cue_intensity.csv`**
- [ ] `useLiveHaptics` = ▢ on ▢ off
- [ ] Recorded in notes: *"pilot ran Option B — generic cue on vest, Mapping confounded with device"*

## I. Run the session (~70 min)

- [ ] Baseline SSQ
- [ ] Cue tour
- [ ] **Read the instruction script verbatim** from the printed card (§A3 below) — after the cue tour, before practice
- [ ] Practice block (None, layout LP)

**After EVERY block record the PRIMARY line.** Want: `presented = 12/12`, rate off both extremes.

**Alert counts are lower than in any earlier run.** As of 2026-09-14 cues fire only for the *designated*
hazard of an open opportunity, so warnings about incidental hazards no longer occur. That is intended (§6).
Do not chase it as a fault — but do record the counts, since they are not comparable to older sessions.

**There is now a second line to read — `TIMING:`.** It reports what *both* warning policies would have done,
in every condition including None. Want: `inverted=0`, `below approach floor=0`, and a positive mean lead. It
prints as a **warning** if anything is inverted or under the floor — that means the predictive cue would have
arrived *later* than the proximity cue on those opportunities, so they push H1 the wrong way rather than just
adding noise. Note the numbers per block; they are the manipulation check for H1.

| Block | Condition | presented | violated | rate | notes |
|---|---|---|---|---|---|
| 1 | | `___/12` | `___` | `___%` | |
| 2 | | `___/12` | `___` | `___%` | |
| 3 | | `___/12` | `___` | `___%` | |
| 4 | | `___/12` | `___` | `___%` | |
| 5 | | `___/12` | `___` | `___%` | |
| 6 | | `___/12` | `___` | `___%` | |

- [ ] **Block 1 is a gate.** If `presented < 12`, or the rate is 0% or 100% — **STOP, diagnose, restart with a fresh ID.** Do not finish a session whose numbers are already meaningless.

> ⚠️ **Expect a FLOOR, not a ceiling.** Direct real-vs-virtual comparisons find people leave **larger** margins
> around virtual hazards than physical ones (+0.16 m Fink 2007, +0.19 m Bühler & Lamontagne 2025). Combined with
> provisional radii, 0% is the likely failure mode. **If block 1 reads 0%, suspect the geometry before the task**
> — deliberately reach into a hazard and confirm detection fires at all. See `VIRTUAL_HAZARD_VALIDITY.md` §6b.
- [ ] E-stop rehearsed (`Esc`) — veil appears, haptics silent, `estop_log.csv` row written
- [ ] Post-session SSQ
- [ ] Session length: `______ min`  Questionnaire load tolerable? ▢ yes ▢ no

## J. Before leaving (~10 min)

- [ ] `sessions/P910/` copied somewhere safe
- [ ] **`opportunities.csv` has 72 rows** (6 × 12)
- [ ] **All 7 `keypoints_*.csv` present and non-trivial in size** — these carry the limb closing-speed
      distribution needed to resolve the §6 policy-crossover issue offline. **Highest-value file to bring back
      after `opportunities.csv`.** Do not delete them even if the session is aborted.
- [ ] `summary.csv` (6 rows), `events.csv`, 7× `keypoints_*.csv`, `questionnaire.csv` all present
- [ ] `practice_*` files present and separate
- [ ] Notes photographed or typed up

## K. Troubleshooting

| Symptom | Most likely cause |
|---|---|
| Tracker Transform doesn't move | TrackedPoseDriver role binding (step D) |
| A joint reports PROXY | that slot isn't assigned on `BodyRig` |
| `SCHEDULE TARGETS MISSING HAZARDS` | obstacle renamed, or `SceneObstacles.addWallSegments` unticked |
| `presented` < 12 | block ended early, or spawner not ticking |
| Every block reports ~4/12 violated | O2 back at its old position — see section A2. Those are posture violations, not behaviour |
| `TIMING: ... inverted=N` with N > 0 | those opportunities gave too little approach distance — record which, do not just carry on |
| `TIMING: no opportunity had both policy triggers` | limbs never approached their designated hazards, or hazard ids are wrong |
| Violations 0% everywhere | **the predicted failure mode** — radii too small, or hazards unreachable. Reach into one deliberately to confirm detection fires. See §6b of `VIRTUAL_HAZARD_VALIDITY.md` |
| No ankle markers visible | `SelfRepresentation` not added, or Show Feet unticked — check the startup line |
| A marker frozen at the floor origin | that foot tracker isn't reaching the rig (step D) |
| Violations 100% everywhere | radii too large, or hazard geometry wrong |
| Chest cue feels far stronger than limb cues | E2 calibration not run or not loaded |

---

### One extra thing to watch for

**`T` changed on 2026-09-14 (0.50 → 1.00 s).** Predictive cues now fire noticeably **earlier and more often**
than in any previous run. That is intended — see §6. Two things follow for today:

- **Do not read the higher alert rate in PG/PB/PBC as a fault.** Note it; §15 Step 3 treats alert burden as
  a real cost of the predictive policy.
- **If a Predictive block still feels indistinguishable from a Proximity block, write it down.** It would mean
  those opportunities do not offer enough clear approach distance (≥ 0.40 m needed), which is the open half of
  the issue and exactly what the keypoint logs will settle.

### What today does NOT establish

Tracker selection (needs paired Vicon logging) · frozen anthropometric capsule radii · measured haptic
latency · the PBC continuous-cue condition (H4') · item-level questionnaire export · ethics approval ·
the final marker diameter (0.07 m is provisional and must stay below the smallest frozen contact radius).
