# Safety Protocol — VR Collision-Warning Studies (Papers 1 and 2)

> **DRAFT for IRB submission. Rewritten 2026-09-21.** Claude-authored skeleton. **The PI must review every
> clause**, insert institution-specific names, numbers and contacts, align thresholds with the IRB of record,
> and put it on institutional letterhead before use. Bracketed `[…]` items need a human decision. This
> protocol governs human-subjects safety only.

> **What changed in this rewrite.** The previous draft specified **physical foam obstacles** in the play area.
> Both studies now use **invisible virtual hazards in a cleared room** — there is nothing physical to hit.
> The dominant risk therefore moves from *impact with props* to **fatigue, sickness, and fast movement while
> vision is occluded**. The foam specification, the prop-handling steps, and the impact-injury language are
> removed rather than edited, because they describe equipment that no longer exists.

> **Scope: one protocol, two studies.** They share a room, hardware, personnel and most risks, so they are
> submitted as one amendment. Where the two differ, the difference is stated explicitly.

---

## 1. The two studies, and the central risk

| | **Paper 1** | **Paper 2** |
|---|---|---|
| Task | Room-scale: collect virtual orbs, dodge virtual projectiles | Seated or standing: reach out and touch a target |
| Movement | Whole body, stepping, turning | One arm; **no locomotion** |
| Space needed | Full arena | ~1 m clear in front of the participant |
| Session | 6 blocks, [~90] min | 376 trials, ~65 min |
| Participants | ~36 | ~30 (separate cohort — nobody does both) |

**Both studies place invisible virtual hazard volumes in the workspace and deliberately induce fast movement
toward them.** Nothing physical occupies those volumes. The room is cleared.

**The defining hazard is therefore fast, committed movement while vision is occluded by the HMD** — loss of
balance, over-extension, or contact with a wall or the operator. Secondary hazards are simulator sickness,
musculoskeletal fatigue from many repeated fast reaches, and skin irritation from worn devices.

> **Why virtual hazards are the safer design, and should be presented that way.** The study needs participants
> to approach hazards *at speed* and sometimes fail to stop. With physical props that means repeated real
> impacts. With virtual volumes, a "collision" is a number in a log file and the participant feels nothing.
> The removal of the foam lowered the risk profile; it did not raise it.

**Risk level:** [no more than minimal risk, pending IRB determination] given a cleared arena, a spotter, an
instant stop, and no physical object in the hazard volumes.

---

## 2. Personnel and roles

| Role | Responsibilities |
|---|---|
| **Principal Investigator** | Overall safety accountability; trains staff; reviews adverse events; final stop authority. |
| **Operator** | Runs the session at the PC; watches tracking and the status line; triggers the **emergency stop**; administers questionnaires. |
| **Spotter** (trained, dedicated) | Stays within arm's reach whenever the HMD is on; guards against falls; calls the stop-word; never leaves the participant unattended in VR. |

- The spotter's **only** job during a block is participant safety — not data, not the screen.
- **Paper 1 requires operator + spotter. Two staff minimum; one person may not run a session alone.**
- **Paper 2:** the participant does not locomote, so [a spotter is recommended but the PI may permit
  operator-only sessions for the seated variant]. If the participant stands, a spotter is required.
- All staff complete a documented training run — this protocol plus a full dry run as mock participant —
  before running any participant.

---

## 3. Physical environment

**The workspace is cleared. No props, no furniture, no obstacles of any kind inside the play area.**

- **Paper 1:** [arena dimensions] of clear floor. Nothing within the boundary. Cables routed overhead or
  taped flat outside the play space.
- **Paper 2:** approximately **1 m of clear space in front of the participant**, plus room to step back. The
  full reach envelope is about 0.8 m wide, 0.4 m tall and 0.2 m deep — far smaller than Paper 1's.
- **Flooring:** even, non-slip, no thresholds or cable runs underfoot.
- **Chaperone / guardian boundary** configured in SteamVR and co-registered with the scene, enclosing the
  reachable space with margin. Participants are shown the boundary during familiarisation.
- **Clearance check before every session** — see Appendix A. The room is used for other work; "it was clear
  yesterday" is not a check.
- Lighting adequate for tracking; no reflective surfaces in the tracked volume.

---

## 4. Equipment safety and hygiene

| Device | Use | Safety notes |
|---|---|---|
| **HMD** | Both studies | Cleaned between participants; disposable face liner where available |
| **VIVE trackers** | Both | Straps checked for tension; not over bare skin where avoidable |
| **5 × Tactosy units** | **Warning cue** — sternum, both forearms, both feet | Skin contact; see below |
| **TactSuit vest** | **Paper 1 only**, projectile-hit feedback. Never a warning cue | Worn over clothing |

- **Cue intensity** is set by a calibration procedure and held fixed. It is a **clearly perceptible but
  comfortable** vibration, never startling or painful. Participants confirm this during familiarisation and
  can request a stop at any point.

- **⚠ PROLONGED VIBRATION IN THE PBC BLOCK — ADDED 2026-09-23, requires IRB amendment.**
  The `PBC` condition (Paper 1, block 6 of 6) delivers a **continuous** cue for the duration of each
  approach rather than a single ~300 ms pulse train. Per-opportunity exposure rises from roughly **0.3 s to
  roughly 1 s**, so **one PBC block carries on the order of 3× the vibration exposure of any other block**,
  concentrated on one limb at a time.

  This is well below any occupational hand-arm vibration limit — those are framed in hours of continuous
  tool use, not seconds — but it is **a change to what participants experience and it must be disclosed.**
  Required before the PBC block runs with any participant:
  - [ ] **IRB amendment filed** describing the continuous cue and the increased exposure
  - [ ] **Consent element 3 updated** ("they will wear vibrating devices") to say that in one block the
        vibration is **continuous during an approach rather than a brief pulse**
  - [ ] **Familiarisation includes a PBC sample**, so the first continuous cue a participant feels is not
        during a measured trial
  - [ ] **Skin inspection after the PBC block specifically**, not only at session end
  - [ ] `BlockRunner.CueDoseSeconds` logged per block, so **actual** delivered exposure is a measured
        number in the record rather than the estimate above

  **Do not run PBC on a naive participant until the amendment is approved.** Self/trained-staff protocol
  development is unaffected.
- **Skin contact.** Tactors sit over clothing or on a clean strap. Inspect skin sites before and after.
  Participants reporting itching, redness or discomfort have the device removed immediately.
- **Hygiene.** All body-contact surfaces cleaned between participants per [institutional guidance].
- **Battery and cable check** before every session. No device is worn while charging.
- **No device is attached over a wound, rash, or implanted electronic device site.**

---

## 5. Screening and exclusion criteria

Exclude any participant who:

- is under [18];
- has uncorrected vision that prevents seeing the display clearly;
- reports a history of seizures, epilepsy, or photosensitivity;
- has a vestibular disorder or reports severe motion sickness;
- has any upper-limb injury, pain or limitation that makes **repeated fast reaching** inadvisable;
- has a shoulder, neck or back condition aggravated by repeated arm movement;
- has an implanted electronic medical device (pacemaker, neurostimulator);
- has skin sensitivity or a condition at a device contact site;
- is pregnant [PI/IRB decision];
- is under the influence of alcohol or sedating medication;
- cannot reliably detect the calibrated tactile cue *(this is a study requirement, not a safety one, but it
  is screened at the same point)*.

Screening is by self-report on a standard form. No medical examination is performed.

---

## 6. Informed consent — required elements

Consent must state, in plain language:

1. **What they will do** — wear a VR headset and repeatedly reach for or move toward virtual targets, for
   about [65 / 90] minutes including breaks.
2. **That hazards are virtual.** There is nothing physical to hit. The room is cleared.
3. **That they will wear vibrating devices** on the body, at a level they will feel but which is not painful.
4. **That the task involves many fast arm movements**, and may cause shoulder or arm tiredness.
5. **Paper 2 specifically: that some warnings are deliberately timed so that stopping is impossible.**
   Failing to stop is expected, is not a personal failure, and is part of the measurement. *(This is in the
   instruction script; it belongs in consent too.)*
6. **Motion sickness risk**, and that they may stop at any time for any reason without giving one.
7. **The stop-word**, and that the operator or spotter may also end the session.
8. **What data is recorded** — including **frame-by-frame body movement traces**, which are behavioural data
   that may be identifying. State retention, access control, and any sharing (§12).
9. **That no clinical or diagnostic feedback is given.** The study measures movement, not health.
10. Compensation, voluntariness, withdrawal, and contact details for the PI and the IRB.

---

## 7. Pre-session procedure

1. Confirm the room is clear (Appendix A).
2. Consent and screening; answer questions.
3. Fit the HMD; adjust IPD and comfort. Confirm a clear image.
4. Place trackers and tactors. Inspect skin sites.
5. **Cue detection check** — participant confirms they feel the cue reliably at the calibrated intensity.
6. Show the guardian boundary and the physical layout of the cleared space.
7. Teach the **stop-word** and confirm they will use it.
8. Read the instruction script verbatim.
9. Practice trials until criterion.
10. Begin.

---

## 8. Simulator sickness

- Administer the **SSQ** before and after the session.
- **Stop immediately** on any report of nausea, dizziness, cold sweat, or disorientation. Remove the HMD,
  seat the participant, offer water, observe for [15] minutes.
- Do not resume a session after a sickness stop. Record it as an adverse event.
- Scheduled breaks: **Paper 1** between blocks; **Paper 2** every 60 trials.
- A participant who reports rising discomfort during a break does not start the next block.

---

## 9. Musculoskeletal fatigue — new, and the dominant repetitive risk

Both studies involve hundreds of fast arm movements. Paper 2 is ~376 reaches in about an hour.

- **Between-block discomfort rating** on a simple scale [0–10] for shoulder and arm.
- **Stop rule:** a rating of [≥7], or any report of pain rather than tiredness, ends the session.
- Rising ratings across blocks, even below threshold, prompt a longer break and a check-in.
- The **urgent tempo** condition asks for fast reaches. Participants are told to move quickly but **never to
  strain, lunge, or over-extend**, and that the study does not measure maximum effort.
- Record discomfort ratings with the session data; they are both a safety measure and a fatigue covariate.

---

## 10. Emergency stop

- **Stop-word:** [chosen word], taught before every session. Anyone may call it.
- **Operator stop:** `Esc` aborts the session immediately and flushes the log. A hardware stop is also
  available via the operator station.
- On stop: the spotter steadies the participant, the operator removes the HMD, the participant is seated.
- **Never** remove the HMD by pulling it forward over the face while the participant is moving.
- Any stop is recorded with time, reason, and who called it.

---

## 11. Adverse events

| Event | Response |
|---|---|
| Nausea / dizziness | §8. Stop, seat, observe [15] min, record |
| Fall or near-fall | Stop. Assess. [Institutional incident procedure]. Do not resume |
| Skin reaction at a device site | Remove device, inspect, record, do not resume that site |
| Pain (not tiredness) in shoulder/arm | Stop. Record. Do not resume |
| Equipment failure during a block | Stop the block, do not improvise a fix with the HMD on |
| Participant distress | Stop, remove HMD, offer to end the session |

All events are logged to `deviations.csv` and reported to the PI within [24 h] and to the IRB per
[institutional reporting requirements].

---

## 12. Post-session

- Remove devices; inspect skin sites.
- Post-session SSQ and comfort check.
- **Do not release a participant reporting dizziness.** Observe until resolved, and [arrange transport] if
  it persists.
- Debrief: explain the virtual hazards, the manipulated warning timing, and — for Paper 2 — that some
  warnings were deliberately too late to act on.
- Answer questions; provide PI contact details.

---

## 13. Records and data protection

- Raw **frame-level body movement traces** are retained. These are behavioural data and **may be
  identifying**; treat them as such.
- Store under deidentified participant IDs. The linking key is held [separately, access-controlled].
- Retention, sharing transformations, and deletion per [institutional policy and the consent form].
- Session records: consent, screening, SSQ, discomfort ratings, deviations, adverse events.
- **Pilot sessions on the research team are protocol development, not human-subjects research**, and are run
  only on trained lab staff. **No naive participant is run before approval.**

---

## Appendix A — Pre-session checklist (operator + spotter)

- [ ] **Room cleared.** Nothing inside the play boundary. Checked today, not assumed
- [ ] Floor dry, even, no cables underfoot
- [ ] Guardian boundary configured and co-registered
- [ ] HMD and all contact surfaces cleaned
- [ ] Tactors and trackers charged, straps intact
- [ ] Cue intensity at the calibrated value; detection check passed
- [ ] Emergency stop tested this session
- [ ] Stop-word agreed with the participant
- [ ] Two staff present (Paper 1; Paper 2 per §2)
- [ ] Consent signed; screening reviewed; no exclusion met
- [ ] Pre-session SSQ recorded
- [ ] Water available; chair available

---

## Appendix B — Roles at a glance

| | Operator | Spotter |
|---|---|---|
| Watches | screen, tracking, status line | the participant, always |
| Triggers | `Esc` / hardware stop | calls the stop-word |
| During a block | may look at data | **safety only** |

---

## Appendix C — Incident form

```
Date / time:
Participant ID:
Study (Paper 1 / Paper 2):        Block / trial:
Staff present:
What happened:
Who called the stop:
Immediate response:
Participant condition on leaving:
Follow-up required:                Reported to PI (date):
Reported to IRB (date):
```
