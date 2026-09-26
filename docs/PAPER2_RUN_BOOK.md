# Paper 2 — Run Book: Dry Run → Bench → Pilot → Study

**Written:** 2026-09-15 · **Authority:** `../PAPER2_STUDY_DESIGN.md` v2.0
**Companions:** `E2_SCENE_SETUP_GUIDE.md` (scene) · `E2_INSTRUCTION_SCRIPT.md` (what you say) ·
`PAPER2_IMPLEMENTATION_PLAN.md` (what is built and what is not)

Four stages, in order. Each one exists to produce something the next stage needs. **Do not skip forward** —
the pilot's numbers are meaningless without the bench, and the study's numbers are meaningless without the
pilot.

| Stage | Trials | Haptics | Latency set | Participants | Produces |
|---|---|---|---|---|---|
| **1. Dry run** | 12 | off | no | you | proof the software works |
| **2. Latency bench** | ~100 pulses | bench only | — | none | the latency number |
| **3. Protocol pilot** | 376 | **on** | **yes** | you + trained staff | the frozen parameters |
| **4. Confirmatory study** | 376 | on | yes | naive, **IRB required** | the paper |

> **Ethics boundary.** Stages 1–3 are protocol development on yourself and trained lab staff, and need no
> IRB approval. **Stage 4 needs the revised v2.0 approval in hand.** Never run a naive participant on
> stages 1–3.

---

# STAGE 1 — Dry run

**Purpose:** prove the trial flow, the CSV, and the tempo prompts work. **This is not data.** Haptics are off
and latency is zero, so no lead time in it means anything.

**Time:** 20 min · **Needs:** the E2 scene built per `E2_SCENE_SETUP_GUIDE.md`

## 1.1 Settings

| Field | Value |
|---|---|
| `participantId` | `901` |
| `allowOverwrite` | ✅ on |
| `totalTrials` | `12` |
| `practiceTrials` | `4` |
| `useLiveHaptics` | ❌ off |
| everything else | defaults |

## 1.2 Run it

Press Play, **put the headset on, and do the reaches.** If you let it play without moving, every trial times
out as `no_movement_onset` and the file is 12 rows of invalid. The runner waits for you to return to the home
marker between trials.

Press **Esc** near the end to test the abort.

## 1.3 Expected — console

```
[ControllerTrackerStandIn] Rig complete — head=Main Camera (REAL), leftHand=Left Controller (REAL),
    rightHand=Right Controller (REAL), chest=PROXY, leftFoot=PROXY, rightFoot=PROXY.
[E2SessionRunner] useLiveHaptics is OFF — …
[E2SessionRunner] pipelineLatencySeconds is 0. …
[E2SessionRunner] E2 plan audit: 4 warning trials, clear approach 0.293–0.448 m,
    longest deliverable lead 1.95 s. PASS.
[E2SessionRunner] P901: 16 trials planned (4 practice at the longest lead, not logged). Latency=0 ms.
```

| Expected | Meaning |
|---|---|
| `rightHand … (REAL)` | ✅ the measured limb is a real device |
| `chest/feet = PROXY` | ✅ **fine** — E2 measures one limb; the rig just requires all six slots filled |
| audit ends `PASS` | ✅ geometry can deliver its longest lead |
| **16** trials planned from `totalTrials=12` | ✅ practice is a *prefix that adds*, not the first 12 |
| the two warnings | ✅ correct and temporary — they disappear in stage 3 |

**Anything else is a problem.** `PLAN REJECTED` means the geometry cannot deliver its leads — do not disable
the check, fix the geometry. Repeating `InvalidOperationException … UnityEngine.Input` means your
`E2SessionRunner.cs` predates 2026-09-15.

## 1.4 Expected — on screen

- **NORMAL** or **FAST**, large, ~0.8 s *before* each target
- **`good pace` / `too slow` / `too fast`** after go trials — **never** after a warning trial
- `return to home` between trials
- On Esc: `ABORT (Escape) — N trials written.`

## 1.5 Expected — files

`…\AppData\LocalLow\DefaultCompany\My project (1)\sessions\E2_P901\`

**`e2_trials.csv`**

| Check | Expected |
|---|---|
| header | once, **32 columns** |
| rows | **12** (the 4 practice are extra and excluded) |
| `realized_lead_s` | populated on warning rows, `NA` on go rows |
| `movement_time_s` | populated on completed reaches, `NA` on successful stops |
| `response_window_s` | `1.2` on Normal rows, `0.7` on Urgent rows |
| `crossed` | 0 or 1 |
| literal `NaN` / `Infinity` | **none** — missing values are `NA` |

**`session.json`**

| Check | Expected |
|---|---|
| `latency_is_placeholder` | `true` *(correct at this stage)* |
| `plan_audit` | ends `PASS.` |
| `lead_levels_s`, `geometry`, `tempo` | match the Inspector |

## 1.6 Gate out of stage 1

- [ ] 12 well-formed rows, 32 columns
- [ ] tempo prompts and go-only pace feedback seen
- [ ] Esc aborts and the log survives
- [ ] `session.json` written, audit PASS

---

# STAGE 2 — Latency bench 🔴 THE BLOCKER

**Purpose:** measure command→first-vibration delay. **Closes Paper 2 gate 2 *and* Paper 1 gate 23.**

**Time:** one afternoon · **Cost:** ~$25 · **Needs:** no participant, no HMD

## 2.0 Why this gates everything

`E2TrialRunner` fires the cue when predicted TTC reaches `assignedLead + pipelineLatency`, so the *vibration*
lands one lead time before predicted contact. With the latency at 0 the cue is issued too late by exactly the
true hardware delay, **every realized lead is short by that amount, and the whole psychometric curve shifts**.
LT80 — the number the paper delivers — moves with it.

You measure two things and they are not interchangeable:

| | Goes to | Correctable? |
|---|---|---|
| **Mean** delay | `pipelineLatencySeconds` | ✅ compensated away |
| **SD** (jitter) | frozen tolerance, §5 | ❌ **an error bar on your x-axis** |

## 2.1 Choose a method

The whole difficulty is putting *"Unity sent it"* and *"the tactor moved"* on **one clock**. Cross-device
timestamps will not do it.

**Method A — microcontroller + piezo ✅ recommended.** ~$25, sub-millisecond, gives a real jitter
distribution. Arduino/Pi Pico + piezo disc. Unity sends a byte on the serial port when it fires; the board
timestamps arrival with `micros()`, and timestamps the piezo onset. Both from the same crystal. Measure the
serial write overhead once (echo immediately) and subtract.

**Method B — 240 fps phone video (fallback).** $0, ±4.2 ms. Unity flashes the screen white on the command
frame; salt grains or a paper tab on the tactor make its motion visible; count frames.
⚠ **Adequate for the mean, not for the SD** — 4.2 ms resolution cannot resolve a ~5 ms jitter SD, and §5
requires that number. Use it to get moving; do Method A before stage 4.

## 2.2 Build the rig

1. Piezo taped **firmly** to the tactor face. Loose coupling smears the onset and inflates SD artificially.
2. Tactor **on the bench**. No body, no HMD.
3. **Same device, same dongle, same port, same firmware** you will use in sessions. Latency is a property of
   the whole chain.
4. Still, quiet surface.

## 2.3 Software ⚠ not yet built

You need a driver that fires the **same three-pulse cue the study uses**
(`HapticDeviceBinding.CreateThreePulseSink` — not a different waveform, or you have measured the wrong thing),
≥100 times, with randomised 2–4 s gaps, logging each command time and writing **`timing_calibration.csv`**
(a §14 file that nothing currently writes).

`HapticSelfTest.cs` is a bare smoke test, not this. **`LatencyBenchRunner` is unbuilt — ask and it gets
built.** Otherwise you are hand-rolling the logging and analysing in a spreadsheet.

## 2.4 Collect

**≥100 activations** (§5). At ~100 samples the SD estimate is stable to roughly ±7%, and you are about to
freeze a tolerance against it. Start it and leave the bench alone.

## 2.5 Expected results

```
   command sent  ->  vibration starts
      mean:   58 ms     <- pipelineLatencySeconds = 0.058
      SD:      9 ms     <- jitter budget
      n:     120
```

A plausible bHaptics-over-USB result is **30–80 ms mean**. Bluetooth runs higher and jitters more.

| Jitter SD | Verdict |
|---|---|
| ≤ 10 ms | ✅ ~5% of the shortest lead — invisible in the fit |
| 10–30 ms | ⚠ usable; report it and widen the reported interval |
| > 50 ms | 🔴 **stop and fix the hardware path** — this flattens the curve and biases LT80 |

**Read the shape, not just the SD.** A long right tail (Bluetooth retries) is worse than a symmetric spread of
equal width, because it drags individual trials badly late. If you see one, go wired.

## 2.6 Record and apply

Log **date, mean, SD, n, device model, firmware, Unity version, USB vs Bluetooth** (§5 wants provenance, not
just a number). Set `pipelineLatencySeconds`, re-run the stage-1 dry run, and confirm `session.json` now reads:

```json
"latency_is_placeholder": false
```

## 2.7 Also on the gate list

§5 also requires **tracking-to-application latency and frame-time distribution** — how stale tracker data is
when the oracle sees it. Different rig (log `PoseFrame.Timestamp` against arrival time), and it biases the
*predicted contact time* rather than cue delivery. Lower priority; don't lose it.

## 2.8 Gate out of stage 2

- [ ] ≥100 activations, mean and SD computed
- [ ] jitter within tolerance, distribution shape inspected
- [ ] provenance recorded and dated
- [ ] `pipelineLatencySeconds` set; `latency_is_placeholder: false`
- [ ] daily re-check procedure agreed (~20 activations each collection day)

---

# STAGE 3 — Protocol pilot

**Purpose:** produce every number the design currently guesses. **This is the stage that makes the study
defensible**, and it is not optional.

**Who:** you and trained lab staff only. **No IRB needed — it is protocol development, not research on
naive participants.**

**Time:** ~70 min per run · **Runs:** 3–5 people minimum

## 3.1 Settings

| Field | Value |
|---|---|
| `participantId` | 9xx (pilot series — keep separate from real IDs) |
| `allowOverwrite` | ❌ **off** |
| `pipelineLatencySeconds` | **your measured value** |
| `totalTrials` | `360` |
| `practiceTrials` | `16` |
| `useLiveHaptics` | ✅ **on** |
| `hapticIntensity` | from cue calibration |
| lead levels / geometry / tempo | defaults **for the first run only** |

## 3.2 Procedure

1. Tactor detection check and cue familiarisation (§6 stage 3). **The tactor then stays mounted.**
2. **Read `E2_INSTRUCTION_SCRIPT.md` verbatim** — even to yourself. You are testing the script as well as the
   software. Do not improvise the *"sometimes you will not be able to stop"* clause.
3. Practice until criterion (§C of the script).
4. Run. Breaks every 60 trials.
5. Discomfort rating between blocks; post-session comfort/sickness check.
6. Expectancy probe (§E of the script).

## 3.3 What you are extracting — and expected values

Run `Rscript e2_analysis.R <path>\e2_trials.csv 200` on each pilot file.

| # | Quantity | Expected | If it is wrong |
|---|---|---|---|
| 1 | **Go-trial crossing rate** | **high, ~75–90%** | **< 50% → the geometry is broken.** People are avoiding hazards with no warning, the curve's floor is not near zero, and LT50/LT80 stop meaning what the paper says. Fix before anything else. |
| 2 | **Achieved lead by level** | ~0.06 / 0.12 / 0.21 / 0.28 / 0.40 s *(simulated — real reaches will differ)* | Must stay **monotone and separated**. If levels collapse, widen `leadLevels`. |
| 3 | **Achieved spread** | ~80% of assigned spread | Delivery runs short by design. Retune `leadLevels` so the **achieved** range spans ~10–90% avoidance (§8). |
| 4 | **Tempo manipulation** | urgent movement time clearly shorter; pre-cue speed clearly higher | If urgent is not faster, **H3a is uninterpretable**. Widen the window separation. |
| 5 | **Window compliance** | **≥ 60%** each tempo | Below that is *instruction failure*, not tempo — and the lost trials are not missing at random. Loosen the windows. |
| 6 | **`prediction_error_s`** | small vs the shortest lead | Error of that order means the x-axis cannot resolve the short end; widen the reported interval and say so (§8). |
| 7 | **`timing_deviation` rate** | high with the default band | **Expected.** Set `timingToleranceSeconds` around *expected* delivery, not the assigned lead. |
| 8 | **Between-participant threshold SD** | assumed 60 ms | **The number that sets N.** Fitted values run **~18% low** — inflate before use. |
| 9 | **Session duration** | target < ethics ceiling | If over, cut elsewhere — hold warning trials at ≥ 80 (§15 gate 13). |
| 10 | **`cue_never_eligible` rate** | low, and **not concentrated at long leads** | Concentration there is non-random missingness in the tail that determines LT80. |

## 3.4 Then freeze the parameters

Feed pilot values back in and **record each with a date** in `PAPER2_STUDY_DESIGN.md`:

- `leadLevels` — from achieved delivery (#2, #3)
- `normalWindowMinSeconds` / `normalWindowSeconds` / `urgentWindowSeconds` — from movement times (#4, #5)
  ⚠ `normalWindowMinSeconds` **must stay above** `urgentWindowSeconds` or the bands touch and H3a compares
  Urgent against Urgent
- `timingToleranceSeconds` — from delivery bias (#7)
- **Re-run `e2_power_analysis.R`** with the pilot `SD_THRESHOLD` → **this sets your final N**

## 3.5 Decisions that must be recorded before stage 4

| Decision | Note |
|---|---|
| **Controllers in hand, or empty-handed?** | Paper 1's decision does **not** carry over — E2 measures mid-flight braking, so added hand mass acts directly on the estimand |
| **Practice at long lead only — keep?** | Implemented; if rejected, record why and expect higher early-block failure |
| **Kinematic detector validation** | §15 gate 8 — hand-label pilot trials **blind to lead condition**, report false-positive/negative rates |
| **Tactor sound masking** | §15 gate 3 — no procedure exists yet. If the tactor is audible, the cue is partly auditory |

## 3.6 Gate out of stage 3

- [ ] Go-trial crossing rate high; geometry produces genuine near-approaches
- [ ] Lead levels frozen from achieved delivery
- [ ] Tempo manipulation confirmed; compliance ≥ 60%
- [ ] Timing tolerance frozen
- [ ] Threshold SD measured; **power re-run; N frozen**
- [ ] Visit duration within ceiling
- [ ] Detector validated against blinded labels
- [ ] All decisions above recorded with dates

---

# STAGE 4 — Confirmatory study

**Purpose:** the paper. **Nothing here is exploratory.** Everything is preregistered before the first
participant.

## 4.1 Gates that must ALL pass first (§15)

| # | Gate | Owner |
|---|---|---|
| 1 | **Ethics approval covering E2 v2.0** — one task, one visit, ~half the old session length, 24/30 not 32/40 | you |
| 2 | Latency and jitter meet the frozen tolerance | stage 2 |
| 3 | Tactor sound masked; detection meets criterion | you |
| 6 | Lead levels cover the response range | stage 3 |
| 7 | Tempo changes pre-cue speed without unacceptable instruction failure | stage 3 |
| 8 | Kinematic detector passes blinded validation | stage 3 |
| 9 | Tracking loss, e-stop, operator procedures rehearsed | you |
| 10 | Logs reconstruct every trial | ⚠ **partly open** — see 4.5 |
| 11 | Simulation supports the counts | ✅ done, **re-run with pilot SD** |
| 13 | Visit duration within ceiling | stage 3 |
| 14 | Lead levels bracket the operating range | stage 3 |
| 15 | Counterfactual prediction error characterised | ✅ instrumented; report from pilot |
| 16 | Split-half reliability computed | ✅ in `e2_analysis.R` |
| 17 | Protocol, code, analysis, preregistration frozen and archived | you |

> Gates 4, 5 and 12 are **VOID at v2.0** (E1/tSSRT deferred, one task so no order to counterbalance).

## 4.2 Preregister before participant 1

Freeze: hypotheses (**H1 estimation, H3a directional — that is the whole confirmatory family**), primary
outcome, threshold definition, trial counts, exclusions, timing tolerances, the model, multiplicity handling,
and the confirmatory/secondary split. `e2_analysis.R` **is** the analysis plan — freeze the file and archive
its hash.

## 4.3 Per session

1. **Daily timing re-check** (~20 activations) before the first participant of the day
2. Consent, screening, handedness, safety briefing
3. HMD fit, tracker placement
4. Tactor detection check; tactor stays mounted
5. **Instruction script, verbatim, once**
6. Practice to criterion
7. 376 trials, breaks every 60, discomfort ratings between blocks
8. Post-session comfort/sickness; expectancy probe
9. `allowOverwrite` **OFF**; copy the whole session folder

## 4.4 Stopping rule

Fixed analysable target and a maximum recruitment target, both preregistered. **No optional stopping on
significance** (§4).

## 4.5 Known gaps at the time of writing

§14 requires ten output files; **two are written** (`e2_trials.csv`, `session.json`). Still unbuilt:
`events.csv`, `trajectories_<block>.csv`, `questionnaires.csv`, `deviations.csv`, `analysis_manifest.json`,
`timing_calibration.csv`, `cue_calibration.csv`.

`deviations.csv` is derivable from the trial file; **`events.csv` and the trajectories are not** — raw
trajectories are a §14 requirement and the only way to re-derive anything if a detector question comes up at
review. **Close these before stage 4**, not before stage 3.

## 4.6 Reporting

- Quote the **bootstrap** interval, and per §16.2 its **upper bound** as the operating point
- Re-run `e2_analysis.R` with `nsim_boot ≥ 2000` for the manuscript
- Report the between-participant threshold SD as a **headline result** (§10)
- Report the flow diagram from `e2_flow.csv`
- Report prediction error as a **distribution**, by lead and tempo
- **Re-run the novelty search** before submission (§1)

---

# Quick reference

**Session folder:** `…\AppData\LocalLow\DefaultCompany\My project (1)\sessions\E2_P<id>\`
**R:** `"C:\Program Files\R\R-4.6.1\bin\Rscript.exe"` · lme4 installed

```sh
Rscript e2_power_analysis.R 25          # quick look ~3 min; omit arg for nsim=400 (~1.5 h)
Rscript e2_simulate_dataset.R 24 sim.csv   # test fixture — NEVER goes in the paper
Rscript e2_analysis.R <trials.csv> 200     # confirmatory; use >=2000 for the manuscript
```

**Unity tests:** `Window ▸ General ▸ Test Runner ▸ EditMode ▸ Run All` → **195 passing**

**The three numbers that decide everything**

| Number | From | Why |
|---|---|---|
| Pipeline latency | stage 2 | shifts every lead time, and so LT80 |
| Threshold SD | stage 3 | sets N almost single-handedly |
| Go-trial crossing rate | stage 3 | if low, the curve has no floor and nothing above it is interpretable |
