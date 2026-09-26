# Session-Day Checklist — *What Makes a Collision Warning Work?*
**One page. Print it. Operator + Spotter both present the whole time.** Full detail: `STUDY_OVERVIEW.md`,
`SAFETY_PROTOCOL.md`. Participant ID: `P_____`   Date: `________`   Operator: `______`   Spotter: `______`

---

## 🚨 STOP anytime — any of these ends the block instantly
- Participant says the **STOP-WORD** ( ▢ agreed: “__________” )
- Operator hits the **E-STOP** button or **`Esc`** key
- Spotter physically steadies/guides, then calls the stop
**On stop:** vision restores, cues silence, help remove the HMD, seat the participant. Do not resume until safe.

---

## A. Room setup (before the participant arrives)
- ▢ Arena clear; **≥ 0.5 m** margin; floor dry/non-slip
- ▢ Foam obstacles inspected (soft, no hard edges, stable, correct positions)
- ▢ Chaperone boundary set + matches the physical clear zone
- ▢ Cable routed overhead/behind; slack managed
- ▢ bHaptics Player running; suit + all tactors paired
- ▢ E-STOP tested (button **and** `Esc`) **this session**
- ▢ Fresh `participantId` set in `SessionRunner`; `sessions/P###/` empty

## B. Intake (participant present)
- ▢ **Consent** signed
- ▢ **Screening** passed (no seizure / vestibular / fainting / cardiac / mobility exclusions; 18+; vision & skin OK)
- ▢ Coded **ID assigned** (no names in study files)
- ▢ **Safety briefing** given: arena, real foam, stop-word, e-stop, “report discomfort immediately”

## C. Fit & calibrate
- ▢ HMD + suit + trackers/controllers fitted; comfortable
- ▢ Tracking verified — operator HUD shows **all joints**
- ▢ Cues confirmed **“noticeable, not unpleasant”**
- ▢ **Cue-intensity calibration (E2)** run → `cue_intensity.csv` written; `SessionRunner.cueIntensityFile` points to it
- ▢ **Spotter in position; e-stop armed**

## D. Baseline & practice (software-driven)
- ▢ **Baseline SSQ** completed (auto, before VR)
- ▢ **Practice block** done (excluded) — participant understands task + boundary

## E. The 6 blocks — repeat for each (order = the participant’s Williams row)
Software runs: **break → block → 3 questionnaires.** Operator advances gates; spotter guards throughout.

| # | Break (≥30 s) | **Start block** | ~180 s run — spotter guards | IPQ | NASA-TLX | SSQ | Comfort OK? |
|---|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
| 1 | ▢ | ▢ | ▢ | ▢ | ▢ | ▢ | ▢ |
| 2 | ▢ | ▢ | ▢ | ▢ | ▢ | ▢ | ▢ |
| 3 | ▢ | ▢ | ▢ | ▢ | ▢ | ▢ | ▢ |
| 4 | ▢ | ▢ | ▢ | ▢ | ▢ | ▢ | ▢ |
| 5 | ▢ | ▢ | ▢ | ▢ | ▢ | ▢ | ▢ |
| 6 | ▢ | ▢ | ▢ | ▢ | ▢ | ▢ | ▢ |

**Stop the session if:** moderate/severe nausea, dizziness or disorientation; SSQ rises past your pre-set threshold; the participant asks to stop; or the spotter judges balance is impaired.

## F. Post-session
- ▢ **Post-session SSQ** completed
- ▢ **Fit-to-leave check** — steady, symptom-free (or recovered to baseline); not driving while symptomatic
- ▢ **Debrief** + questions + compensation
- ▢ Confirm data present in `sessions/P###/`: `summary.csv`, `events.csv`, `keypoints_*`, `questionnaire.csv`
- ▢ **Disinfect** HMD face interface, straps, controllers, suit contact surfaces

## G. If an incident occurred
- ▢ Rendered aid / activated emergency contact per policy
- ▢ **Incident form** completed (see `SAFETY_PROTOCOL.md` Appendix C)
- ▢ Reported to IRB within the required window

---
*Created 2026-07-07. Pairs with `SESSION` flow in `STUDY_OVERVIEW.md` §7 and the safety controls in `SAFETY_PROTOCOL.md`.*
