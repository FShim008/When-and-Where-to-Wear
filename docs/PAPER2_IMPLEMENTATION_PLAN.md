# Paper 2 — Implementation Plan to Launch-Ready

**Reviewed:** 2026-09-14 · **Authority:** `../PAPER2_STUDY_DESIGN.md` v2.0 (1822 lines) · **Tests at review:** 182/182 pass

This is a full audit of Paper 2's design and code against its own §15 launch gates, plus the ordered work to
close the gaps. It distinguishes **what I can build** from **what only you can do** (hardware, ethics, humans).

---

## 1. What is actually done

| Area | State |
|---|---|
| Design document | v2.0, E2-only, 1822 lines. Comprehensive. E1/H2/H4 deferred cleanly, not deleted. |
| Novelty search | Done and recorded with real database counts (§1). Re-run before submission. |
| Core engine | `E2Trial`, `E2TrialOutcome` (26-col CSV), `E2TrialRunner`, `E2SessionPlan` — pure, hardware-free |
| Tests | 27 E2 tests; 182 total, all green in the dotnet harness and in Unity |
| Unity driver | `E2SessionRunner` (Integration), `E2TrialLogWriter` (Runtime) |
| Power / precision | `e2_power_analysis.R` — **run**, three output tables, conclusions in §4 |
| Confirmatory analysis | `e2_analysis.R` — **written and dry-run validated** against known ground truth |
| Test fixture | `e2_simulate_dataset.R` — emits the real 26-column contract |
| Scene setup | `E2_SCENE_SETUP_GUIDE.md` |
| Instruction script | `E2_INSTRUCTION_SCRIPT.md` — stop-signal framing, expectancy probe |

The **science** is in good shape. The **instrumentation** is not, and that is what this plan is about.

---

## 2. Gaps, by severity

### ✅ S1 — CLOSED. Tempo IS signalled: `E2SessionRunner` prompts "FAST"/"NORMAL" for
`tempoPromptSeconds` before each trial, and `E2TrialRunner` enforces a response-window BAND (upper and
lower bound) so a participant reaching fast on every trial cannot satisfy both windows.

### 🔴 S1 — (original) The tempo manipulation does not exist

**H3a is a confirmatory hypothesis with no manipulation behind it.**

`Tempo` is assigned by `E2SessionPlan` and written to the CSV, and that is the entire implementation.
Verified by grep: outside the plan and the logger, `Tempo` appears only in `E2SessionRunner:197`, in a debug
status string. There is:

- no differential response window (`maxTrialSeconds` is a fixed 4.0 s for both);
- no signal to the participant of which tempo this trial is — yet the instruction script (A2) promises
  *"the display will tell you which before each target appears"*;
- no pace feedback (§8 permits it on go trials only);
- no movement-time measurement, so **gate 7 — "tempo changes pre-cue speed without unacceptable instruction
  failure" — cannot be evaluated at all.**

A session run today would produce a `tempo` column that is pure noise, and H3a would be an estimate of nothing.

### ✅ S2 — CLOSED 2026-09-25. Eight of nine applicable files now written.

| File | State |
|---|---|
| `session.json` · `e2_trials.csv` · `timing_calibration.csv` · `cue_calibration.csv` | already existed |
| **`analysis_manifest.json`** | added — SHA-256 per file, versions, exclusions |
| **`deviations.csv`** | added — invalid trials, timing deviations, operator stops, equipment failures |
| **`events.csv`** | added — **non-trial** chronology only; per-trial timing stays in `e2_trials.csv` |
| **`trajectories_<block>.csv`** | added — the same frames the runner consumed, so a disputed trial recomputes exactly |
| **`questionnaires.csv`** | added — item-level + scored |
| `e1_trials.csv` | n/a, E1 deferred at v2.0 |

Two scoping decisions worth carrying forward:

- **`events.csv` is deliberately narrow.** §14 lists per-trial quantities in its event-stream
  description, but those are already columns of `e2_trials.csv`. Re-emitting them would create two
  records of one measurement that could silently disagree. A test fails if an event kind containing
  `warning`, `onset`, `boundary`, `movement`, `lead` or `response` is added.
- **No tracking-quality column.** `PoseFrame` carries no confidence field and `TrackerKeypointSource`
  surfaces none. A constant "good" would read as a measurement. `gap_s` is the honest substitute — a
  property of delivery, not of tracker confidence.

Also added: **tracking-dropout detection** (a gap longer than `trackingDropoutSeconds`, not a per-tick
test — at 90 Hz against a slower render loop, plenty of ticks legitimately have no new frame), and
**shoulder/arm discomfort** with a stopping rule scored on the **maximum site, never the mean**.

---

### 🔴 S2 — §14 requires ten output files; one is written (original)

Only `e2_trials.csv` exists. Missing: `session.json`, `events.csv`, `deviations.csv`, `trajectories_<block>.csv`,
`questionnaires.csv`, `analysis_manifest.json`, `timing_calibration.csv`, `cue_calibration.csv`.

**Gate 10** ("end-to-end logs reconstruct every trial and preserve invalid-trial reasons") and **gate 17**
(freeze and archive) cannot pass. `session.json` is the worst absence: without it a dataset has no record of
code version, device, operator, or parameters.

### 🔴 S3 — Session parameters are neither settable nor recorded

`E2SessionRunner:122` builds `new E2PlanParams { TotalTrials = totalTrials, Seed = planSeed }`. Everything
else takes its compiled-in default: `LeadLevels`, `ReachDistance`, `HazardHalfExtents`, `MiniBlockSize`,
`TargetLimb`, `Home`, and every `E2Params` field including `TimingToleranceSeconds`.

Two consequences. §8 requires lead levels to be **set from pilot data** — today that needs a recompile. And
nothing writes down what was used, so **a session cannot be reproduced from its own log**, which is what
gate 10 asks for.

### ✅ S4 — CLOSED. `E2PlanAudit` runs inside `Build()`, which throws on a failed audit.

### 🟠 S4 — (original)

`E2SessionPlan.RequiredApproach()` exists, is documented as "the Paper 1 geometry lesson in a new form", and
is **never called by `Build()`**. Nothing prevents a trial whose approach distance is too short for its
assigned lead to be delivered at all.

This is the exact failure Paper 1 hit: there, a hazard 4 cm from the hand made the timing factor inoperable
while the summary line still looked healthy. Here it surfaces as `cue_never_eligible` losses **concentrated at
the long leads** — missingness that is emphatically not at random, in precisely the tail that determines LT80.

### ✅ S5 — CLOSED. `E2SessionPlan` emits a practice prefix at the longest lead
(`PracticeTrials`, `PracticeLeadLevel`), and `E2ScheduledTrial.IsPractice` marks it so the runner no
longer assumes practice is the first N rows. **Record the decision in the design doc** — it still says
this "must be decided and recorded before piloting", and implementation is not preregistration.

### 🟠 S5 — (original)

`E2SessionPlan` has no practice concept; `E2SessionRunner:151,213` suppresses the log for the first N rows of
the ordinary plan. Participants therefore meet the impossible-to-stop short leads before forming a stable
response — the condition the instruction script's third clause exists to defuse, arriving before the
reassurance can work.

### 🟠 S6 — Movement onset and target-contact times are not recorded

`E2TrialOutcome` has `ReachedTarget` (a bool) but no movement onset time, no contact time, and no movement
time. Needed for the tempo manipulation check (S1), for §14's `events.csv`, and for the fatigue/drift index
§15 gate 12 still requires.

### 🟡 S7 — The kinematic detector is crude and unvalidated

`E2TrialRunner:286`: `FrameGuess(t) => (t - _firstTime) / 100.0` — back-dating a confirmed run by a *guessed*
frame interval derived from elapsed trial time. It is honestly commented as crude, but it feeds
`ResponseOnsetTime`, a reported secondary outcome. Gate 8 requires blinded validation against hand-labelled
pilot trials; that needs pilot data, but the detector should use the **real** frame interval first.

### 🟡 S8 — No E2 tracking-robustness sweep (§12)

`TrackingNoise.cs` exists for Paper 1. §12's degradation sweep — warning-timing error, probability of delivery
before LT50/LT80, miss and false-alert rates under spatial bias, jitter, latency, dropout, frozen frames — has
no E2 harness. Secondary analysis, explicitly "must not compete with H1–H4", so it is not launch-blocking.

---

## 3. Gate status (§15)

| # | Gate | Status |
|---|---|---|
| 1 | Ethics covers E2, revised for v2.0 | ❌ **You** — old two-task scope |
| 2 | Cue-onset latency and jitter meet tolerance | ❌ **You** — the bench measurement; blocks everything |
| 3 | Tactor sound masked, detection meets criterion | ❌ **You** — no masking procedure written anywhere |
| 4, 5 | *(E1 / tSSRT)* | ✅ VOID at v2.0 |
| 6 | Lead levels cover the response range | ⚠ Needs pilot; S3 blocks even setting them |
| 7 | Tempo changes pre-cue speed | ❌ **S1 — not implementable today** |
| 8 | Kinematic detector passes blinded validation | ⚠ Needs pilot; S7 first |
| 9 | Tracking loss / e-stop / operator rehearsal | ❌ **You** — rehearsal |
| 10 | Logs reconstruct every trial | ❌ **S2, S3** |
| 11 | Simulation supports counts | ✅ **Done** |
| 12 | *(task order)* — discomfort + go-RT drift retained | ⚠ S6 |
| 13 | Visit duration within ceiling | ❌ **You** — pilot timing |
| 14 | Lead levels bracket the operating range | ⚠ Needs pilot |
| 15 | Counterfactual prediction error characterized | ✅ Instrumented; needs pilot data |
| 16 | Split-half reliability computed | ✅ **Done** in `e2_analysis.R` |
| 17 | Protocol/code/analysis frozen and archived | ❌ **S2** |

**Nine gates open. Five are mine, four are yours.**

---

## 4. The plan

### Phase A — code I can complete now (no hardware)

| # | Work | Closes | Status |
|---|---|---|---|
| A1 | Tempo response windows, per-trial tempo signalling, movement-time capture, window compliance | S1, gate 7 | ✅ **done** |
| A2 | Expose all plan/runner parameters; write `session.json` with full provenance | S3, gate 10 | ✅ **done** |
| A3 | Deliverability check in `E2SessionPlan.Build()`, failing loudly | S4 | ✅ **done** |
| A4 | Long-lead practice prefix emitted by the plan | S5 | ✅ **done** |
| A5 | Movement onset + contact times into the outcome and CSV | S6, gate 12 | ✅ **done** |
| A6 | Real frame interval in the detector instead of `/100.0` | S7 | ✅ **done** |
| A7 | `events.csv` + `trajectories_*.csv` + `questionnaires.csv` writers | S2, gate 10 | ⬜ remaining |
| A8 | Update the R scripts for the widened contract; re-validate recovery | keeps A honest | ✅ **done** |

**Tests: 182 → 195, all green.** CSV contract 26 → 32 columns, verified byte-exact against
`E2TrialOutcomeFormatter.HeaderLine` by an automated check on every regeneration.

#### Two things the work itself uncovered

**1. `RequiredApproach()` was the wrong deliverability test, and shipping it would have caused false alarms.**
The plan above assumed it was simply "never called". Working the numbers first showed that substituting a peak
closing speed demands 0.45 / 0.61 / 0.85 m of clear approach for the three longest leads against the
~0.32–0.46 m the geometry provides — yet simulation shows all three deliver. The cue fires *before* peak speed
is reached, so a peak-speed criterion fails levels that work. It is now renamed `FireDistance()` and
documented as **not** a pass/fail test; the real check (`Audit()`) asks whether TTC at movement onset already
undercuts the assigned lead, which catches genuinely broken geometry (a reach ≤ 0.25 m) and passes the rest.

**2. The response window needed a lower bound.** Found by noticing that a "too fast" branch in the pace
feedback was unreachable. With only an upper bound, reaching fast on every trial satisfies both windows at
once and Normal/Urgent become behaviourally identical — with all compliance checks still green. Normal is now
a band, and a test asserts the two bands cannot touch. This would not have been visible in pilot data as
anything but a null H3a.

### Phase B — needs pilot data (after your bench + dry run)

B1 set lead levels from measured delivery · B2 set `TimingToleranceSeconds` around *expected* delivery ·
B3 blinded detector validation · B4 re-run power from pilot `SD_THRESHOLD` · B5 visit-duration report

### Phase C — yours, and gating

C1 **haptic latency bench** (also Paper 1 gate 23) · C2 ethics revision for v2.0 scope · C3 tactor sound
masking procedure · C4 operator/e-stop rehearsal · C5 controllers-in-hand decision · C6 response-window values

### Phase D — before submission

D1 §12 robustness sweep (secondary) · D2 novelty re-run · D3 preregistration freeze · D4 archive

---

## 5. Two judgements worth recording

**The design is stronger than the implementation, and that asymmetry is the risk.** The document reasons
carefully about strategic slowing, counterfactual validation, and prediction error — and then the tempo
manipulation, which one of two confirmatory hypotheses rests on, turns out to be a string in a debug label.
Reading the design alone would not reveal that. This is why the audit was worth doing before the pilot rather
than after.

**Nothing found here threatens the study's logic.** Every gap is instrumentation, not inference. The estimand,
the analysis, the novelty position and the power reasoning all survive the audit intact. That is a much better
position than the reverse.
