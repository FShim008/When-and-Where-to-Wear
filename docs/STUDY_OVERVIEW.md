# Study Overview — *What Makes a Collision Warning Work?*
### Predictive, Body-Localized Tactile Cues for Real-Obstacle Collision Avoidance in VR (IEEE VR 2027 / TVCG)

> **⚠ SUPERSEDED (2026-09-08).** Two rounds of revision have overtaken this document. (1) 2026-07: all hazards
> became **virtual volumes — no physical foam**. (2) 2026-09: the protocol was rewritten — the design authority
> is now **`../PAPER1_STUDY_DESIGN.md`**, and the primary outcome, Visual benchmark, and chest-cue hardware all
> changed. This file is kept only as a plain-language introduction to the *general* idea. **Do not implement or
> run anything from it.** See `PAPER1_CODE_GAP.md` for what actually differs.

A plain-language description of what the study tests, how it's designed, what happens in a session, and what
data is collected. Companion docs: `PILOT_SETUP_GUIDE.md` (how to build/run it), `SAFETY_PROTOCOL.md`
(human-subjects safety), `SESSION_CHECKLIST.md` (the printable session-day checklist), and the code plans
`../IMPLEMENTATION_PLAN.md` / `../PRE_PILOT_PLAN.md`.

---

## 1. What the study is (one paragraph)
A person wears a VR head-mounted display, so their vision is fully occupied by the virtual world and they
**cannot see the real room** — but **real foam obstacles are physically present** in that room. As they move
around doing a task, they risk bumping into those real objects. The system tracks their body, **predicts when a
limb is about to hit a real obstacle**, and fires a **vibrotactile cue** (bHaptics suit) to warn them. The study
asks **which kind of warning best prevents real collisions while preserving the sense of "being there"
(presence).** Two ideas are tested: **predictive timing** (warn based on where a limb is *heading*, not just how
close it is) and **body-localization** (buzz the *specific* at-risk limb, not a generic chest cue). The proposed
best technique is **predictive + body-localized (PB)**.

## 2. Design — 6 conditions (a 2×2 plus 2 anchors)

|  | **Generic** (cue the chest) | **Body-localized** (cue the at-risk limb) |
|---|---|---|
| **Reactive** (fire when current distance < **D = 0.30 m**) | **RG** | **RB** |
| **Predictive** (fire when forecast time-to-collision < **T = 0.50 s**) | **PG** | **PB** ⭐ full technique |

Two anchors outside the 2×2:
- **None** — no feedback → the **floor** (baseline collision rate with nothing).
- **Visual** — a best-practice on-screen obstacle highlight → the "current practice" comparison (detection-matched: same distance D and tracking as the haptic conditions).

**Six conditions: None, RG, RB, PG, PB, Visual.**
- **Within-subjects:** every participant completes **all 6** as 6 blocks.
- **Counterbalanced:** block order follows a **6×6 Williams Latin square** so order/carryover effects balance out.

## 3. What is held identical (the controls that make it clean)
- **Identical cue everywhere** — a fixed **3-pulse vibration** (~480 ms). Only *when* (timing) and *where* (which limb) change; cue *strength* is not manipulated.
- **Perceptually-equalized intensity across body sites** (the E2 calibration) so a chest cue (40-motor vest) isn't confounded with a 3-motor hand/foot unit — "chest vs limb" is a *location* difference, not an energy difference.
- **Deliberately simple predictor** — a constant-velocity time-to-collision estimate, framed as an **idealized oracle** (an *upper bound* on predictive warning), because the manipulated variable is the *timing strategy*, not predictor sophistication.
- **Clock-scripted task moments** — opportunities are driven by the block clock, **not** by the participant's own motion (avoids circularity in the outcome).

## 4. The task (what the participant does)
A room-scale **reach/dodge** task. Each ~**180 s** block has **12 scripted opportunities** (onsets 8, 22, 36 … 152 s). At each, an **orb appears to reach for** and/or a **projectile flies at them to dodge**, forcing committed movement that produces genuine near-approaches to the real foam obstacles. A motivating score (+1 per orb delivered, −1 per projectile hit) drives effort — but the **measured outcome is collisions**, computed from body tracking, not the score.

## 5. Outcomes & hypotheses
- **Primary outcome (DV):** **collisions per opportunity** (real-obstacle limb collisions ÷ 12; lower is better).
- **Secondary:** **presence** (IPQ), **workload** (NASA-TLX), **simulator sickness** (SSQ), plus near-misses, avoidance latency, minimum clearance, and alert counts (covariate / manipulation checks).
- **Hypotheses:**
  - **H1 (Timing):** Predictive reduces collisions vs Reactive.
  - **H2 (Localization):** Body-localized reduces collisions vs Generic.
  - **H3 (Full technique):** **PB is lowest** — the Timing×Localization interaction; PB < RG, RB, PG.
  - **H4 (vs practice):** PB reduces collisions vs the **Visual** reference.
  - **Floor:** every feedback condition beats **None**.
  - **Presence:** haptic conditions **preserve** presence (don't reduce it vs None/Visual).

## 6. How it's analyzed
Collisions are bounded (≤ 12) and zero-heavy toward strong cues, so the primary model is a **negative-binomial
mixed model (GLMM)**: `collisions ~ Timing * Localization + alert covariate + offset(log opportunities) +
(1|participant) + (1|layout)` on the RG/RB/PG/PB cells, plus a **floor** model (each condition vs None) and an
**H4** model (PB vs Visual), and mixed models for presence/workload/sickness. All implemented in
`Analysis/analysis.R`; sample size is set from `Analysis/power_analysis.R` (seeded by the pilot).

## 7. A session, start to finish
The software (`SessionRunner`) automates the sequence; the operator advances gates and the participant answers
questionnaires on the operator screen. (Printable version: `SESSION_CHECKLIST.md`.)

**Before the headset goes on**
1. **Consent** (signed, IRB-approved).
2. **Screen for exclusions** (see `SAFETY_PROTOCOL.md` §5: seizure/vestibular/fainting/cardiac/mobility/pregnancy per IRB/concussion history; 18+; correctable vision; skin OK for the suit).
3. **Assign a coded participant ID** (e.g. `P001`) — no names in the data; set it in `SessionRunner`.
4. **Safety briefing** — arena, real foam, the **stop-word**, the **emergency stop**, "report any discomfort immediately."
5. **Baseline SSQ** (sickness) — administered automatically before any VR.
6. **Fit the gear** — HMD, suit, trackers/controllers; confirm all body joints track; confirm cues feel "noticeable, not unpleasant."
7. **Cue-intensity calibration** (E2) — run once → writes `cue_intensity.csv`; point `SessionRunner.cueIntensityFile` at it.
8. **Spotter takes position; operator arms the e-stop.**

**Warm-up**
9. **One practice block** (excluded from data) so they learn the task and the boundary.

**The 6 real blocks** (Williams order) — per block the software does:
10. **A rest/break** (≥ 30 s; HMD may come off).
11. Operator clicks **Start block**.
12. **~180 s block:** the 12 opportunities fire; the participant moves; the system detects limb-vs-obstacle collisions and fires that condition's cue (or none / visual).
13. **Three questionnaires** on the operator screen: **presence (IPQ)**, **workload (NASA-TLX)**, **sickness (SSQ)**.
14. **Spotter monitors continuously**; stop for sickness/discomfort.

**After all 6 blocks**
15. **Post-session SSQ.**
16. **Fit-to-leave check** (steady, symptom-free).
17. **Debrief + compensation.**
18. **Disinfect the gear** before the next participant.

## 8. Data collected (auto-written to `sessions/P###/`)
| File | Contents | Used for |
|---|---|---|
| `summary.csv` | one row per block: collisions, **collisions-per-opportunity**, near-misses, alerts, avoidance latency, min clearance | primary analysis |
| `events.csv` | every alert + collision/near-miss with timestamps | manipulation checks / timelines |
| `keypoints_*.csv` | raw per-frame body tracking | replay / audit |
| `questionnaire.csv` | all IPQ / NASA-TLX / SSQ scores | presence / workload / sickness |
| `practice_*` | the practice block | kept separate, **excluded** |
| `estop_log.csv` | any emergency stops (time, reason, context) | safety record |
| `cue_intensity.csv` | that participant's per-site calibration | reproducibility |

## 9. Roles in the room
- **Operator** — runs the PC, watches tracking/HUD, triggers the e-stop, administers the questionnaires.
- **Spotter** — stays within arm's reach whenever the HMD is on, guards against falls/collisions, calls the stop-word; their **only** job is participant safety.
- **Minimum two staff per session — never run one alone.** (Full roster + PI responsibilities in `SAFETY_PROTOCOL.md` §2.)

## 10. Safety essentials
Soft foam obstacles (no hard edges), a chaperone boundary with clear margin, cables routed overhead/behind,
**three independent ways to stop instantly** (participant **stop-word**, operator **E-STOP** button / `Esc`,
spotter physical intervention), and SSQ sickness monitoring with stop criteria. Full protocol in
`SAFETY_PROTOCOL.md`.

## Key parameters (code defaults — reconcile against the design docs)
| Parameter | Value | Where |
|---|---|---|
| Reactive distance **D** | 0.30 m | `Core/CollisionOracle.cs` |
| Predictive TTC **T** | 0.50 s | `Core/CollisionOracle.cs` |
| Contact / near-miss distance | 0.03 m / 0.12 m | `Core/CollisionDetector.cs` |
| Block length | 180 s | `SessionRunner` |
| Opportunities / block | 12 @ 8, 22 … 152 s | `Core/OpportunityScheduler.cs` |
| Haptic cue | 3 × 100 ms pulses, 60 ms gaps | `Integration/HapticDeviceBinding.cs` |
| Conditions | None, RG, RB, PG, PB, Visual | `Core/Enums.cs` |
| Counterbalance | 6×6 Williams square | `Core/SessionPlan.cs` |
| Questionnaires | IPQ (14 items) · NASA-TLX (6) · SSQ (16) | `Core/Questionnaire.cs` |

*Created 2026-07-07. Plain-language companion to the code + `IMPLEMENTATION_PLAN.md`. Parameters are code-derived; reconcile against the authoritative design docs before data collection.*
