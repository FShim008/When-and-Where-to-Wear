# Safety Protocol — *When and Where to Warn* (VR real-obstacle collision study)

> **DRAFT for IRB submission.** Claude-authored skeleton (Plan Task 0.2 / A2). The PI must review every
> clause, insert institution-specific names/numbers (IRB protocol #, EHS contacts, room), align thresholds
> with the IRB of record, and put it on institutional letterhead before use. Bracketed `[…]` items need a
> human decision. This protocol governs human-subjects safety only; see `PREREGISTRATION.md` for the science.

## 1. Study summary & the central risk
Participants wear a VR head-mounted display (HMD) and perform a room-scale reach/dodge task while **real,
physical foam obstacles** stand in the play area. The experiment **deliberately induces near-approaches to
those real obstacles** to measure whether predictive, body-localized vibrotactile cues reduce real-obstacle
collisions. Therefore the **defining hazard is intentional movement toward real objects while vision is
occluded by the HMD** — risk of bumping/tripping, loss of balance, and (secondarily) simulator sickness and
device-contact discomfort. Every control below exists to keep those contacts **low-energy and non-injurious**.

**Risk level:** [no more than minimal risk, pending IRB determination] given soft obstacles, a spotter, a
bounded arena, and an instant emergency stop.

## 2. Personnel & roles
| Role | Responsibilities |
|---|---|
| **Principal Investigator** | Overall safety accountability; trains staff; reviews adverse events; final stop authority. |
| **Operator** | Runs the session at the PC; watches tracking/HUD; triggers the **emergency stop**; administers questionnaires. |
| **Spotter** (trained, dedicated) | Stays within arm's reach of the participant **whenever the HMD is on**; guards against falls/collisions; calls the stop-word; never leaves the participant unattended in VR. |

- The spotter's **only** job during a block is participant safety — not data, not the screen.
- Minimum **two staff** present for any session with the HMD on (operator + spotter). One person may not run a session alone.
- All staff complete a documented training run (this protocol + a full dry-run as mock participant) before running real participants.

## 3. Physical environment
- **Arena.** A cleared, flat, non-slip floor of at least **[3.0 m × 3.0 m]**, with a **≥ 0.5 m clear margin** of empty space beyond the chaperone boundary on all sides. No furniture, cables, cords, or trip hazards inside the margin.
- **Chaperone / guardian boundary.** Configured in SteamVR to the arena and **co-registered with the scene** so the virtual boundary matches the physical clear zone. The Layout-L1 foam obstacles sit **inside** the boundary at surveyed coordinates; the boundary encloses the reachable play space with margin.
- **Foam obstacle specification** *(the items participants may contact)*:
  - Material: **soft open-cell/EVA foam**, density low enough to deform on contact; **no rigid cores, no hard edges or corners** (round/chamfer all edges; pad any internal frame).
  - Dimensions/positions: per the Layout-L1 storyboard (O1 low block, O2 pillar, O3 panel) at the surveyed coordinates; **[confirm exact sizes from the design docs — A1]**.
  - Stability: heavy/wide enough not to topple if brushed, **but** light enough to yield and not injure; **not anchored rigidly** in a way that turns a brush into a trip. Base must not protrude as a toe-stub hazard.
  - Inspection: checked before each session for wear, exposed hard parts, or displacement.
- **Lighting/temperature:** comfortable, consistent; floor free of glare/wet spots.
- **Overhead/clearance:** no low ceilings, fixtures, or wall protrusions within reach of an extended arm at the boundary.

## 4. Equipment safety & hygiene
- **HMD (VIVE Focus Vision):** fitted snugly; straps adjusted per participant; **passthrough available** and used as the "unblank" make-safe on emergency stop (see §9).
- **Tether/cables (PC-VR):** routed **overhead or behind** the participant via a cable manager so they cannot wrap a leg; the spotter manages slack; no cable crosses the floor of the play area.
- **Body trackers + straps (3 VIVE Ultimate Trackers: chest + both ankles) and 2 controllers (hands):** straps snug but not constricting; checked for circulation/pinching; ankle trackers positioned so they cannot catch on each other during steps.
- **bHaptics suit/Tactosy (vibrotactile):** low-amplitude vibration only.
  - **Intensity ceiling:** cues run at calibrated, comfortable levels (per-site equalization, Plan Task 3.1); a documented **maximum drive** is enforced in software and **never exceeded**. Participants confirm cues are "noticeable, not unpleasant" during the fit check.
  - **Skin contact:** worn over the participant's own clothing or a provided liner; not used on broken/irritated skin.
- **Hygiene / infection control:** HMD facial interface, straps, controllers, and suit contact surfaces are **wiped with [approved disinfectant] between participants**; disposable HMD face covers used where available; hand sanitizer offered before/after. Equipment is not used on anyone with visible illness.

## 5. Participant screening & exclusion criteria
Screen at consent; **exclude** anyone who:
- Has a **history of epilepsy or photosensitive seizures**.
- Has a **vestibular/balance disorder**, frequent vertigo, or **inner-ear** condition.
- Has a condition causing **fainting, dizziness, or syncope**, or is under the influence of alcohol/sedating medication.
- Is **pregnant** [if the IRB requires this exclusion].
- Has a **mobility, musculoskeletal, or cardiac** condition that makes brisk reaching/stepping/dodging unsafe.
- Has a **recent concussion / head or neck injury** [within [interval]].
- Has uncorrected vision preventing HMD use, or a skin condition aggravated by the suit/straps.
- Is under **[18]** years of age.
Participants are told the task involves **physical movement near real objects** and that they may stop at any time without penalty.

## 6. Informed consent — required elements
The consent form (separate document, IRB-approved) must state, in plain language:
- The task requires **moving, reaching, and dodging in room-scale VR with real foam obstacles present**, and that **contact with the foam obstacles is possible by design**.
- Foreseeable discomforts: **simulator sickness** (nausea, dizziness, eyestrain, disorientation), **minor bumps/contact** with soft obstacles, mild fatigue, and **vibration** on the body from the haptic suit.
- The **emergency stop** and the participant's **stop-word**; the right to **pause or withdraw at any time** without penalty and to keep any compensation per IRB policy.
- Presence of a **spotter** and that staff may physically steady them to prevent a fall.
- Data handling/privacy (motion logs, questionnaire responses) per the consent/IRB.
- Contact info for the PI and the IRB.

## 7. Pre-session procedure
1. Confirm consent signed; complete screening; assign a coded participant ID (no PII in study files).
2. **Safety briefing:** explain the arena, foam obstacles, the stop-word, the emergency stop, and "if you feel unwell, say so immediately."
3. **Baseline SSQ** (administered automatically before exposure; see §8).
4. **Fit check:** HMD, straps, trackers, suit; verify tracking (operator HUD shows all joints), comfortable cue intensity, and clear vision.
5. **Orientation/practice block** (excluded from analysis) so the participant learns the boundary and task before data collection.
6. Spotter takes position; operator confirms emergency stop is armed.

## 8. Simulator-sickness management (SSQ)
- **Instrument:** Simulator Sickness Questionnaire administered **before** exposure (baseline) and **after blocks** (automated; Plan Task 5.5). Subscale + total scores computed (Kennedy et al., 1993).
- **Continuous monitoring:** the operator/spotter ask about comfort at every break; participants are told to **report symptoms immediately, not tough it out**.
- **Stop / pause criteria** *(PI to finalize with the IRB)* — **stop the session** if any of:
  - the participant reports **moderate or severe nausea, dizziness, or disorientation** at any time;
  - SSQ total rises by **[≥ a pre-set threshold, e.g. an institution-standard increase]** over baseline;
  - the participant requests to stop, or the spotter judges balance/orientation is impaired.
- **Recovery:** on stop for sickness, remove the HMD, sit the participant down, provide water, and **do not let them leave or drive until symptoms resolve** (minimum **[15] min** seated recovery, longer if needed). Record the event.

## 9. Emergency stop procedure
Three independent triggers, any of which **immediately ends the current block**:
1. **Participant stop-word** (e.g., "**STOP**") — spoken aloud; spotter/operator act instantly.
2. **Operator emergency stop** — the on-screen **red E-STOP button** or the **`Esc` hotkey** (implemented; Plan Task 4.5 / D5).
3. **Spotter physical intervention** — the spotter may steady or guide the participant at any time, then call the stop.

On emergency stop, the software **(a)** ends the block, **(b)** silences all haptic cues, **(c)** restores vision (HMD **passthrough / unblank**) so the participant can see the real room, and **(d)** logs the abort (time, reason, context) to `estop_log.csv`. The spotter then helps remove the HMD and seats the participant. The session does not resume until the PI/operator confirm it is safe.

## 10. Adverse-event response & reporting
- **Fall / collision with injury, faint, or any unanticipated harm:** stop immediately, render aid / activate [emergency contact or 911 per policy], remove equipment, do not move a possibly-injured participant beyond making them safe.
- Document every adverse or near-miss event on the **Incident Form** (Appendix C) and **report to the IRB within [the IRB-required window]**.
- The PI reviews all incidents and may suspend the study pending corrective action.

## 11. Post-session
1. **Post-session SSQ**; compare to baseline.
2. **Fit-to-leave check:** confirm the participant is steady, symptom-free (or recovered to baseline), and not driving while symptomatic.
3. Debrief; answer questions; provide compensation per IRB policy.
4. Disinfect equipment (§4) before the next participant.

## 12. Records
Safety-relevant files: signed consents (stored separately from coded data), screening logs, `estop_log.csv`, SSQ records, and Incident Forms. Retain per IRB/institutional policy.

---

### Appendix A — Pre-session safety checklist (operator + spotter)
- [ ] Arena clear; ≥ 0.5 m margin; floor dry/non-slip
- [ ] Foam obstacles inspected (soft, no hard edges, stable, correct positions)
- [ ] Chaperone boundary set + matches the physical clear zone
- [ ] Cable routed overhead/behind; slack managed
- [ ] HMD/straps/trackers/suit fitted; tracking shows all joints; cues comfortable; passthrough works
- [ ] Emergency stop tested (button + `Esc`) **this session**
- [ ] Consent signed; screening passed; baseline SSQ done
- [ ] Spotter briefed and in position; stop-word agreed

### Appendix B — Roles at a glance
- **Stop-word** → anyone hearing it acts. **Spotter** → never more than arm's reach from the participant. **Operator** → hand on the E-STOP.

### Appendix C — Incident form (template)
`date · participant ID · staff present · what happened · trigger (stop-word/operator/spotter) · injury? (y/n + detail) · SSQ at event · action taken · recovery time · IRB reported (date) · corrective action`

*Draft created 2026-06-22 (A2). Pairs with the implemented D5 emergency stop and the F1 SSQ pipeline.*
