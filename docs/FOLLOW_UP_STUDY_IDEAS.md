# Follow-up Study Ideas — ranked, from Paper 1's apparatus

**Prepared:** 2026-09-14 · **Basis:** targeted literature search plus a full review of
`PAPER1_STUDY_DESIGN.md`, `PAPER2_STUDY_DESIGN.md`, `docs/VIRTUAL_HAZARD_VALIDITY.md`, and the built codebase.

**Status of the novelty claims below: provisional.** Targeted searching, not a systematic review. This project
has produced wrong prior-art claims twice from targeted searching (two miscitations corrected in
`PAPER2_STUDY_DESIGN.md` §19). Treat every "nothing found" here as a lead, not a conclusion.

---

## 0. Read this before the ideas

**You have two unfinished papers, no ethics approval, and five of thirty-five launch gates closed.** A third
study idea has a real opportunity cost. The best use of what follows is:

1. as **named follow-ups** inside Papers 1 and 2 — a reviewer who asks "why didn't you test X?" should get a
   research plan, not an apology; and
2. as a **queued** next study that reuses the rig once Paper 1 ships.

Not as a replacement for finishing what exists.

### What the apparatus gives you for free

This is why these ideas are cheap and why unrelated ideas are not.

| Asset | Already built and tested |
|---|---|
| Invisible registered hazard volumes, six isometric layouts | ✅ |
| Six-joint body tracking (HMD + 2 controllers + 3 trackers) | ✅ |
| Vest + four Tactosy units, per-site salience calibration | ✅ |
| Opportunity scheduler — 12 designated limb×hazard events per block | ✅ |
| Oracle (distance + TTC per limb per hazard), reactive and predictive policies | ✅ |
| **Counterfactual dual-policy trigger logging** | ✅ |
| Opportunity-level binary outcome with validity gating and attribution | ✅ |
| Williams counterbalancing, deferred scoring, e-stop, questionnaires | ✅ |
| `SelfRepresentation` (ankle markers), currently a **constant** | ✅ |

Any idea that reuses this is weeks of work. Any idea that doesn't is a new project.

---

## 1. RECOMMENDED — Does seeing your body replace being told which limb?

**Confidence: HIGH.** Best novelty-per-unit-effort of anything here.

### The question

Paper 1's H2 asks whether a cue on the at-risk limb beats a generic chest cue. Paper 1 answers that **under one
fixed self-representation** — ankle markers on, hands visible as controllers — and §5 already records that
limb visibility *plausibly moderates H2* and names the no-avatar case as untested.

So the obvious next question is the one a developer actually asks:

> **If I render the user's limbs, do I still need an actuator on every limb?**

Somatotopic cueing says "your left hand." A visible avatar also says "your left hand" — continuously, for free,
in a modality that is already there. They may be redundant. Nobody has tested it.

### Design — and it absorbs the follow-up Paper 1 already promised

Paper 1 §3 designates a directional-torso follow-up ("generic / directional torso / somatotopic limb at fixed
timing") to answer Belt & Whistles. **Fold the two together:**

**2 × 3, within-subjects, timing held at predictive:**

| | Generic chest | Directional torso | Somatotopic limb |
|---|---|---|---|
| **No self-representation** | | | |
| **Tracked limbs visible** | | | |

Six blocks — the exact structure Paper 1 already runs, same Williams square, same layouts, same outcome.

### Why it is strong

- **Answers two reviewer questions in one study.** Belt & Whistles (directional torso) *and* the avatar
  moderator. Paper 1 currently owes the first and admits the second.
- **A real hardware-cost decision.** Five actuators versus one, conditional on whether you render a body.
  That is design guidance, not a phenomenon.
- **The interaction is the finding, and every sign is interesting.** Redundancy (avatar kills the localization
  benefit) is commercially decisive. Synergy is theoretically interesting. A null on the interaction still
  gives a clean three-level replication of the mapping factor.
- **Search found nothing.** The avatar+haptics literature is about *embodiment*, not about avatars substituting
  for spatial warning information.

### Risks

- Rendering an avatar well is not free — mis-scaled limbs create their own distortions. Use tracked markers or
  minimal limb proxies rather than a full rigged body, exactly as Paper 1 does now.
- Directional torso encoding needs multiple torso actuators, which reopens the §2 device-family question.
  Budget for it or use the vest and accept the weaker construct name **for that condition only**.

---

## 2. RECOMMENDED — The false-alarm price of prediction

**Confidence: MEDIUM-HIGH.** Strong question, one real reviewer risk.

### The question

Paper 1's predictive policy fires **earlier and more often** than the proximity policy. That is not a side
effect — it is structural: predicting further ahead means being wrong more often. Paper 1 measures alert burden
and uses it as a §15 tie-break, but **never manipulates it**, and Paper 2 §18 explicitly parks the reliability
question as "a separate reliability experiment."

> **Predictive warning buys lead time and pays in false alarms. Where does the trade reverse?**

Manipulate the false-alarm rate directly (e.g. 0%, 15%, 30% of alerts on opportunities where no violation
would have occurred — which your counterfactual logging can now identify exactly) and measure violations,
response rate to *valid* alerts, presence, and acceptability.

### Why it is strong

- It is **Paper 1's own unanswered question**, and Paper 2 already names it as missing.
- **Your new counterfactual logging makes it clean.** You can label an alert as genuinely unnecessary using
  `policy_lead_s` and the violation outcome — most false-alarm studies have to inject synthetic ones.
- Automotive design canon holds that "the key is knowing when *not* to warn," and Paper 2 §18 notes this has
  **never been operationalized as a threshold for body-scale limb collisions**. That is the gap.

### The reviewer risk — name it honestly

The cry-wolf effect is **very** well established in driving, aviation and clinical alerting. A reviewer who
knows that literature will ask what is new. Two defensible answers, and you need both:

1. **The false alarms are felt on the body and interrupt an ongoing movement**, not a glance at a dashboard.
   The interruption cost is motoric, not just attentional.
2. **The rate is endogenous.** In driving, false-alarm rate is a design parameter you set. Here it is a
   *consequence of choosing a predictive policy* — so the finding feeds directly back into Paper 1's ranking
   rather than being a separate human-factors result.

If you cannot make those two land, this drops to MEDIUM. Framed as "cry wolf, but in VR," it is a weak paper.

---

## 3. WORTH CONSIDERING — Does a warning still help in a room you already know?

**Confidence: MEDIUM-HIGH.** Excellent deployment relevance, one feasibility risk.

### The question

Real VR users mostly play in **their own room**, whose furniture they know. Warning systems are designed as if
every hazard were a surprise.

> **Does hazard-location knowledge substitute for a warning — and if so, warnings matter mainly for novel or
> changed spaces.**

That is a genuinely consequential finding: it would say safety systems should target guests, rearranged rooms,
and new venues rather than running constantly at home.

### Design

**2 × 2:** layout familiarity (extensively practised / novel) × warning (None / PB). Your **layout isometries
give the manipulation for free** — repeat L1 to overtraining, then test on L1 versus a never-seen L4, which is
identical in difficulty by construction.

### Why it is strong

- **CHI 2025** ("How your Physical Environment Affects Spatial Presence in VR") found that limited knowledge of
  the physical environment raises presence but *amplifies the harm of collisions*, and that repeated avoidance
  degrades presence. That is a citable motivation and it stops short of the warning question.
- Paper 1's layout isometries were built to *defeat* location learning. This study turns that same machinery
  around and makes learning the independent variable — an elegant reuse.

### Risk

**Within-session learning may be too weak to produce a real familiarity effect**, especially with invisible
hazards and only ~12 exposures per layout. Likely needs many more repeats or a two-session design, which raises
cost and dropout. Pilot the learning curve before committing.

---

## 4. WORTH CONSIDERING — Bounding the physicality effect

**Confidence: MEDIUM as a paper, HIGH as a section.**

A **between-subjects** cohort with one heavy, fixed, fully padded physical prop co-located with one virtual
hazard, compared against the virtual-only cohort. Full rationale in `docs/VIRTUAL_HAZARD_VALIDITY.md` §6.

**Why it matters:** it converts your single largest reviewer objection from an argument into a measurement.
Nobody has bounded the physicality effect for body-scale XR safety.

**Why it is not a paper on its own:** it is a validation study, not a finding. Reviewers rarely reward those at
full-paper length. **Run it as an extra cohort inside another study** and report it as a validity section —
that is where its value is highest.

**Non-negotiable:** between-subjects. Handling a physical obstacle *recalibrates* subsequent virtual behaviour
(Giesel et al. 2025), so a within-subject prop would contaminate every block after it.

---

## 5. LOWER PRIORITY — Field-of-view dependence of warning modality

**Confidence: MEDIUM-LOW. I initially rated this higher and the search brought it down.**

The idea: haptics should help more for hazards *outside* the visual field, which is Coolen et al.'s mechanism
turned into a factor.

**Why it dropped:** the general result is largely established. Haptic feedback significantly outperforms
peripheral visual cues for out-of-view targets (Information 2026), and the *Displays* 2023 vest study already
ran a limited-visibility condition. The headline is predictable, which is the worst property a study can have.

Salvageable only as a *parametric* contribution — hazard eccentricity as a continuous variable, yielding the
angle at which haptics starts to win. That is a narrower and more honest claim.

---

## 6. LOWER PRIORITY — Multi-limb arbitration

**Confidence: MEDIUM-LOW.**

When two limbs are at risk at once, which do you cue — the most urgent, both, or neither? `TactorArbiter`
already implements "most urgent," and compound events (E12) already exist.

**Why it is not higher:** compound events are 1 of 12 in the current schedule, so you would have to rebuild the
task around simultaneity, which changes its character and costs you the comparability with Paper 1 that makes
these ideas cheap. The tactile literature also already says the system has limited capacity for simultaneous
stimuli, so "cue one" is the likely answer and a predictable result.

---

## Ranking

| # | Idea | Novelty | Reuse | Feasibility | Reviewer risk | **Overall** |
|---|---|---|---|---|---|---|
| 1 | Self-representation × cue localization | High | Total | High | Low | **HIGH** |
| 2 | False-alarm price of prediction | Med-High | High | High | **Medium** | **MED-HIGH** |
| 3 | Familiarity × warning value | High | High | **Medium** | Low | **MED-HIGH** |
| 4 | Physicality bound (between-subjects) | Med | Total | High | Med | **MED** (high as a section) |
| 5 | FOV dependence | **Low-Med** | Total | High | Med-High | **MED-LOW** |
| 6 | Multi-limb arbitration | Med | Medium | Medium | Med | **MED-LOW** |

## Recommendation

**Pick #1, and fold Paper 1's already-promised directional-torso follow-up into it.** One study, six blocks,
the same rig and the same analysis, answering both the Belt & Whistles question Paper 1 owes and the avatar
moderator Paper 1 admits. Nothing else here has that ratio.

**Queue #2 second**, conditional on being able to make the two differentiators in §2 land. If they do not, it
is a competent paper in a crowded literature.

**Attach #4 to whichever runs first** as an extra cohort. It costs little and removes your most predictable
objection permanently.

**Do none of them until Paper 1 has ethics approval and a completed pilot.** The ideas will keep; the ethics
timeline will not.
