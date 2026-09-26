# E2 Scene Setup Guide — Paper 2

**Design authority:** `../PAPER2_STUDY_DESIGN.md` §§5, 6, 8 · **Built:** 2026-09-14
**Code:** `Core/E2/*` (pure, 27 tests) · `Integration/E2SessionRunner.cs` · `Runtime/E2TrialLogWriter.cs`

---

## 0. Read this first

**E2 needs far less space than Paper 1.** It is a seated-or-standing reaching task, not room-scale. The entire
working volume is measured from the actual session plan:

| | Range | Size |
|---|---|---|
| Reach targets, x | −0.19 … 0.59 m | 0.78 m wide |
| Reach targets, y | 0.88 … 1.31 m | 0.43 m tall |
| Reach targets, z | 0.75 … 0.90 m | 0.15 m deep |
| Hazard centres | x −0.10…0.50, y 0.94…1.26, z 0.54…0.76 | 0.20 m cubes |
| Clear approach per trial | 0.27 … 0.46 m | median 0.36 m |

Home is **(0.20, 1.10, 0.20)** — a right hand at rest, standing. **About a metre of clear space in front of
the participant is enough.** No 3.5 m arena.

**Default session:** 360 **measured** trials, 90 of them warning trials (25%), five lead levels
(0.08 / 0.15 / 0.25 / 0.35 / 0.50 s assigned), **plus** 16 practice trials on the front.

> **CHANGED 2026-09-14.** Practice is now a prefix the plan *adds*, run at the **longest lead only**, and it no
> longer consumes measured trials. `totalTrials = 360, practiceTrials = 16` produces a **376-trial plan of
> which 360 are logged** — previously it was a 360-trial plan of which 344 were logged. Budget the extra 16.

Expect roughly **55–65 minutes** including breaks, consent and questionnaires.

---

## 1. ⛔ THE BLOCKER — measure the haptic latency first

**`pipelineLatencySeconds` defaults to 0, and 0 is wrong.** Every delivered lead time is short by the true
hardware latency until you set it, which shifts the entire psychometric curve. Nothing else in this guide
matters if you skip this.

**It is also Paper 1 gate §14.23, so you are doing it for both papers.**

### The procedure — one afternoon, about $30

1. **Tape an accelerometer to the tactor** you will use for the cue. A contact microphone works too — the
   units are audible.
2. **Log it on the same clock as Unity.** Both the "send cue" command and the actual vibration must land on
   one timeline, or the subtraction is meaningless.
3. **Fire 100+ commands** with the tactor sitting on the bench. No participant needed.
4. **Measure the gap on each.** You want two numbers:

```
   command sent  ->  vibration starts
      mean:   58 ms     <- this goes in pipelineLatencySeconds (as 0.058)
      SD:      9 ms     <- this is your jitter budget
```

5. **Record both, dated**, in the session log. The SD is what §5 checks against the frozen tolerance.

**Sanity check on the jitter.** Your lead levels span roughly 60–400 ms of achieved delivery. Jitter of ±10 ms
is about 5% of that and invisible in the fit. Jitter approaching ±100 ms would smear the curve into a falsely
shallow slope — if you measure that, stop and fix the hardware path before collecting anything.

---

## 2. Build the scene (~30 min)

### 2.1 New scene

`File ▸ New Scene`. Save as `Assets/Scenes/E2.unity`. **Do not reuse `Dryrun`** — that scene carries Paper 1's
hazard volumes, spawners and session driver, and two session runners in one scene will fight each other.

### 2.2 XR rig

Same as Paper 1: XR Origin with Main Camera and the two controller objects. Paper 2 has **not** recorded a
controllers-in-hand decision (Paper 1 §11 explicitly scopes its decision to Paper 1 only, because Paper 2
measures mid-flight braking where added hand mass acts directly on the estimand).

> **Decide and record this before piloting.** Holding a controller changes reach and braking dynamics. Either
> track the hand directly and run empty-handed, or keep the controller and state it as a limitation. Write the
> choice into `PAPER2_STUDY_DESIGN.md` §11 with a date, the way Paper 1 did.

### 2.3 Body rig

1. Empty GameObject named **`BodyRig`**.
2. Add **`BodyTrackerRig`**.
3. Fill the slots. E2 only measures **one reaching limb**, but `BodyTrackerRig.IsComplete` requires all six —
   so assign what you have and use proxies for the rest, exactly as `ControllerTrackerStandIn` does for Paper 1.
4. Add **`ControllerTrackerStandIn`** if you want the auto-discovery and the REAL/PROXY startup line.

**Check the startup log names the reaching limb as REAL.** A proxy on the measured limb produces a beautifully
formatted file of meaningless lead times.

### 2.4 Home marker

1. Create a small sphere or ring at **(0.20, 1.10, 0.20)**.
2. Scale ~0.10 m. **Remove its collider** — nothing in E2 should participate in physics.
3. This is what the participant returns to between trials. `homeTolerance` defaults to 0.10 m.

### 2.5 The session runner

1. Empty GameObject named **`E2Session`**.
2. Add **`E2SessionRunner`** (namespace `CollisionFeedback.Integration`).
3. Set the Inspector fields:

| Field | Dry run | Real session |
|---|---|---|
| `participantId` | 901 | the real ID |
| `sessionIndex` | 1 | 1 |
| `allowOverwrite` | ✅ on | ❌ **off** |
| **`pipelineLatencySeconds`** | 0.058 *(or your measured value)* | **your measured value** |
| `totalTrials` | **12** | 360 |
| `practiceTrials` | 4 | 16 |
| `planSeed` | 20260914 | record whatever you use |
| `useLiveHaptics` | ❌ off | ✅ **on** |
| `hapticIntensity` | 0.7 | from calibration |
| `cueIntensityFile` | empty | your `cue_intensity.csv` |
| `trackerRig` | drag `BodyRig` | same |
| `homeMarker` | drag the marker | same |
| `targetPrefab` | leave empty | optional |

**Lead levels / Geometry / Tempo — new Inspector sections, all pilot-set (§8, §15 gate 7).** They were
compiled-in constants until 2026-09-14; §8 requires the lead levels to come from pilot data, so they are now
editable *and* recorded in `session.json`. Leave every one at its default for the first dry run.

| Field | Default | Set it from |
|---|---|---|
| `leadLevels` | 0.08 / 0.15 / 0.25 / 0.35 / 0.50 | measured **achieved** delivery, not intuition |
| `miniBlockSize` | 4 | leave — 4 locks prevalence at 25% |
| `reachDistance` | 0.70 | your participant's comfortable reach |
| `hazardHalfExtents` | 0.10 cube | leave unless the geometry audit complains |
| `hazardFractionMin/Max` | 0.60 / 0.80 | leave |
| `azimuth / elevationSpreadDeg` | 35 / 18 | your tracked working volume |
| `homePosition` | (0.20, 1.10, 0.20) | **must match the home marker you placed** |
| `normalWindowMinSeconds` / `normalWindowSeconds` | 0.85 / 1.20 | pilot movement times |
| `urgentWindowMinSeconds` / `urgentWindowSeconds` | 0 / 0.70 | pilot movement times |
| `timingToleranceSeconds` | 0.05 | pilot delivery bias — the default flags nearly everything |
| `tempoPromptSeconds` | 0.8 | leave |

> ⚠ **`normalWindowMinSeconds` must stay above `urgentWindowSeconds`.** If the bands touch, one movement time
> satisfies both tempos, Normal and Urgent stop differing, and H3a compares Urgent against Urgent while every
> compliance check still reads green.

**Leave `targetPrefab` empty to start** — the runner generates a plain sphere and strips its collider. Swap in
a prefab later only if you want a nicer target.

### 2.6 What you do *not* need to build

- **No hazard GameObjects.** Hazards are generated per trial by `E2SessionPlan`, invisible by design, and exist
  only as `Obstacle` structs. There is nothing to place and nothing to hide.
- **No layout variants.** That is Paper 1's machinery. E2 varies geometry per trial instead.
- **No opportunity spawner, no projectile system, no scoring.** E2's task is reach-and-touch.

---

## 3. Dry run — verify before a human (~15 min)

Set `totalTrials = 12`, `practiceTrials = 4`, `useLiveHaptics = off`, `allowOverwrite = on`. Press Play.

**Startup console — all three must appear:**

- [ ] `[E2SessionRunner] P901: 12 trials planned (4 practice, not logged). Latency=58 ms. CSV → …`
- [ ] `Rig complete — … All six joints are real devices.` ← **any PROXY on the reaching limb = STOP**
- [ ] No warning about `pipelineLatencySeconds is 0`

**During the run, watch the on-screen status line:**

```
[E2] T0005  warn@0.25s  Normal   (3 logged)
[E2] T0006  go  Urgent              (4 logged)
```

- [ ] Targets appear one at a time, in different directions each trial
- [ ] Between trials it says `return to home` and waits until you go back
- [ ] Roughly **1 in 4** trials shows `warn@` — that is the locked 25% prevalence
- [ ] `Esc` aborts cleanly and the log survives

**During the run you should also see**, because the tempo manipulation is now implemented:

- [ ] A large **NORMAL** or **FAST** prompt ~0.8 s *before* each target appears
- [ ] **`good pace` / `too slow` / `too fast`** after go trials — and **never** after a warning trial
      (that would be feedback about the hazard, which §8 forbids)

**Then open the CSV** at `…/sessions/E2_P901/e2_trials.csv`:

- [ ] Header present **once**, **32 columns**
- [ ] **12 rows** — the 4 practice trials are *extra*, run first, and must not appear
- [ ] `realized_lead_s` populated on warning rows, `NA` on go rows
- [ ] `movement_time_s` populated on completed reaches, `NA` on successful stops
- [ ] `response_window_s` is 1.20 on Normal rows and 0.70 on Urgent rows
- [ ] No literal `NaN` or `Infinity` anywhere
- [ ] `crossed` is 0 or 1

**And open `session.json` in the same folder** — new, and the file that makes the session reproducible:

- [ ] `latency_is_placeholder` is `false` *(it will be `true` until you set the measured latency)*
- [ ] `lead_levels_s`, `geometry`, and `tempo` match what you set in the Inspector
- [ ] `plan_audit` ends in `PASS`

---

## 4. Before the first real participant

- [ ] **`pipelineLatencySeconds` set from the bench**, and the jitter recorded
- [ ] **`useLiveHaptics` ON** and the cue felt at the tested site
- [ ] **Cue detection check** — §6 stage 3, participant confirms they feel it reliably before anything is measured
- [ ] **`allowOverwrite` OFF**
- [ ] **The controllers-in-hand decision recorded** (§2.2 above)
- [ ] **Instruction script printed** — `E2_INSTRUCTION_SCRIPT.md`. Read verbatim, once, after the tactor
      detection check and before practice. **Do not improvise it, and do not import Paper 1's "stay out of the
      zones" wording** — that instruction destroys E2's zero-warning anchor (§B1 of the script).
- [ ] **Practice-lead decision recorded.** The 16 practice trials run identically to measured trials —
      `E2SessionPlan` has no practice concept, the runner just suppresses their logs — so participants meet
      impossible-to-stop leads before they have a stable response. Recommendation is long-lead-only practice,
      which needs a practice prefix in `E2SessionPlan`. Decide before piloting.
- [ ] Ethics approval covering E2 (Paper 2 §15 gate 1, updated for the v2.0 single-task design)

---

## 5. What to bring back

Copy the whole `sessions/E2_P901/` folder.

**Then send me `e2_trials.csv`.** The first things worth checking, before any curve is fitted:

1. **The achieved lead distribution** — are the five levels actually separated after delivery? Simulation says
   they land near 0.06 / 0.12 / 0.21 / 0.28 / 0.40 s, but that is simulated reaching, not a person.
2. **The crossing rate on go trials** — this is the curve's zero-warning anchor. If people avoid the hazard
   without any warning, the geometry is not producing genuine near-approaches and everything above it is
   compressed.
3. **`prediction_error_s`** — the constant-velocity counterfactual scored against real contact times. This is
   the number that tells you how much to trust the x-axis.
4. **`timing_deviation` rate** — expected to be high with the placeholder tolerance. Use the pilot to set a
   real band around *expected* delivery, not around the assigned lead.

---

## 6. Troubleshooting

| Symptom | Likely cause |
|---|---|
| `No complete BodyTrackerRig. Cannot run.` | A slot is empty — all six are required even though E2 measures one |
| Reaching limb logs as PROXY | That slot is not assigned; the lead times will be meaningless |
| `Session folder already has data` | Change `participantId` or tick `allowOverwrite` |
| Stuck on `return to home` | `homeTolerance` too tight, or the home marker is not where the plan thinks home is (0.20, 1.10, 0.20) |
| Every warning row has `timing_deviation = 1` | Expected with the default tolerance — delivery runs systematically short (§8 note in `E2SessionPlan`). Set the band from pilot data |
| `realized_lead_s` all `NA` | The cue never fired — check `Closing`, the reach may not be heading at the hazard |
| Lots of `cue_never_eligible` | Reaches are stopping short of the hazard, or too slow to cross the TTC threshold |
| Cue felt but nothing logged | `useLiveHaptics` on but the trial is practice — practice is not written |
| `InvalidOperationException: You are trying to read Input using the UnityEngine.Input class` repeating every frame | Fixed 2026-09-15. The abort key now goes through the IMGUI event stream, which works under either input backend. If you still see it, your `E2SessionRunner.cs` predates the fix |
| `chest=PROXY, leftFoot=PROXY, rightFoot=PROXY` | **Expected and fine for E2** — it measures one limb. Only the reaching limb must be REAL |

---

## 7. What this setup does **not** establish

Tracker selection (Paper 2's criteria are stricter than Paper 1's and need the paired comparison) · the
validated kinematic response detector (§8 requires blinded human labels; the current back-dating is
deliberately crude) · the final lead levels (set from pilot delivery, not from these defaults) · the timing
tolerance band · ethics approval.
