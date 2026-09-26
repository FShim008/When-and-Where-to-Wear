# Virtual-Only Hazards — validity evidence and reviewer defence

**Revised:** 2026-09-14 (second pass — supersedes the 2026-07 draft and the first 2026-09-14 pass)
**For:** Paper 1 · **Companion:** `PAPER1_STUDY_DESIGN.md` §§2, 11, 13

**The worry:** *"Real VR gaming has physical furniture around the user. Studying only virtual hazards destroys
generalization, so the study is unreliable."*

---

## 0. Verdict

**The design survives, and the evidence is stronger than expected.** Fifteen studies spanning 2001–2026 —
walking, stepping over, reaching around, and fleeing a threat — converge on one pattern:

> Real vs virtual changes the **magnitude** of avoidance margins. It does **not** change the **structure** of
> avoidance behaviour — the scaling with obstacle size, the side choice, the onset timing, the steering
> dynamics, or the pattern across obstacle locations.

Main effects, not interactions. Paper 1's confirmatory claims are all within-subject condition contrasts, which
live entirely in the structure. **That is the whole defence, and it is now empirically supported rather than
argued.**

**And there is a stronger argument available than the one above, specific to this design: the hazards are
invisible.** See §2.6 — this is the single best point in this document and it was not in the first pass.

Four things follow from this review. One reverses advice given earlier the same day.

1. **The hidden-hazard argument goes in related work** (§2.6). The perceptual mechanism behind the
   real-vs-virtual gap needs a *visible* virtual obstacle to act on. Yours has none, in any condition.
   **DONE** — written into `PAPER1_STUDY_DESIGN.md` §13, gate §14.28.
2. **Ankle markers, identical in every condition** (§3.5). A large share of the "VR is different" effect is
   *missing limb vision*, not virtuality — and with controllers in hand, feet are currently the only invisible
   target limb. **DONE** — `PAPER1_STUDY_DESIGN.md` §5, `Runtime/SelfRepresentation.cs`, gate §14.27.
   Carries an H2-moderation caveat: read §3.5 before treating it as free.
3. **A physical-prop validation must be BETWEEN-subjects.** §3.4 found that handling a physical obstacle
   *recalibrates* subsequent virtual performance. A within-subject prop check would contaminate every block
   after it. The earlier recommendation was wrong on this point; it is now future work only.
4. **Reframe the Coolen (2020) result as motivation, not limitation** (§4.2) — it is the strongest argument
   *for* the intervention Paper 1 tests.

### Verification status

| Level | Sources |
|---|---|
| **Full text / detailed extract read** | Weber 2022 · Bühler & Lamontagne 2025 · Giesel 2025 · Kruijff 2022 · Suda 2022 |
| **Publisher record + detailed abstract/summary** | Fink 2007 · Coolen 2020 · He/Okubo 2024 · Sci Rep 2024 · Virtual Worlds 2026 · Displays 2023 · Abdlkarim 2026 · Kelly 2022 · Slater 2009 · Insko 2001 |

Everything below is reliable enough to design and argue from. **Open each one before it enters the manuscript**
— confirm the specific number you attribute to it.

---

## 1. Separate three claims — the objection only lands on one

| # | Claim | Paper 1's? | Threatened? |
|---|---|---|---|
| 1 | Absolute violation rates predict real furniture-collision rates | **No** | Yes — and it would be wrong to claim |
| 2 | Participants respond realistically to virtual hazards | Yes | No — §2 supports it |
| 3 | The **relative ranking** of warning designs holds | **Yes — this is the paper** | No — §2 and §3 argue against |

For claim 3 to fail, physicality would have to **interact** with warning design: real obstacles would have to
change *which* warning strategy wins. The only study that manipulated physical consequence directly (§3.3)
found a **main effect** — everyone got modestly better, with no sign that it reordered anything.

---

## 2. The direct real-vs-virtual comparisons

These are the studies that matter most, because they measured the same people doing the same task in both.

**2.1 Fink, Foo & Warren (2007), *ACM TAP* 4(1)** — matched physical and virtual obstacle courses, walking.
Steering dynamics the same. In VR: clearance **+0.16 m**, max deviation **+0.16 m**, speed **−0.13 m/s**.
Simulations ruled out walking speed and distance compression as causes; the authors attribute it to **greater
uncertainty about the egocentric location** of virtual obstacles.

**2.2 Bühler & Lamontagne (2025), *Sensors* 25(6):1667** — 15 adults, omnidirectional-treadmill VR vs overground
real-world, circumventing an interferer. In VR: minimum distance **+0.19 m** (p<.01), max deviation **+0.18 m**
(p<.01), speed **0.40–0.41 m/s slower** (p<.001). Onset distance **similar** (p=.05, ns). Right-side
circumvention proportions **similar**. The money quote:

> "differences brought by ODT-VR manifest in the **magnitude of certain variables rather than changes in the
> fundamental characteristics of circumvention strategies**."

That is an independent 2025 replication of Fink 2007, eighteen years later, on different hardware. Both find
**more conservative, structurally identical** avoidance in VR.

**2.3 Coolen et al. (2020), *Sensors* 20(4):1095 — "Avoiding 3D Obstacles in Mixed Reality: Does It Differ from
Negotiating Real Obstacles?"** The title is your reviewer's question. 12 participants stepped over **real** vs
**holographic** obstacles of varying height and depth, Kinect-tracked.

- Holographic obstacles **elicited avoidance that scaled with obstacle dimensions**, as real ones did.
- **No significant effect of obstacle type** on maximum step height, crossing height, or foot clearance —
  except a slightly but systematically **increased lead-foot step height** for holographic.
- **But**: collision rates were higher with holographic, and some participants failed to raise the trail foot.

Read §4.2 before you treat that last line as bad news.

**2.4 Scientific Reports (2024) 14:5262** — VR vs physical-reality paradigms, pedestrian responses to a
knife-wielding aggressor. **"Almost identical psychological responses"**; **minimal differences in movement
responses**. Concluded VR "can produce similarly valid data as physical experiments," and — note this — that VR
is **"far more ethically viable than physical reality, owing to the much reduced physical risk."** That is your
PI's argument, peer-reviewed, in a threat-avoidance context.

**2.5 The mechanism.** Kelly (2022), *IEEE TVCG* — meta-analysis, 137 samples / 61 publications / 20 HMDs.
Egocentric distance is underperceived in VR; accuracy improves with **field of view** and **resolution**, and
degrades with **headset weight**. So the caution is a perceptual-uncertainty effect with known moderators, not
a motivational one.

**2.6 The argument specific to this design — and the strongest one available.**

Every study in §2.1-2.4 used **visible** obstacles. That matters more than it first appears, because of *why*
they found a difference. Fink attributes the wider clearance to "greater uncertainty about the **egocentric
location** of virtual obstacles." Kelly's meta-analysis traces the same family of effects to **distance
underestimation**, moderated by field of view and resolution. Bühler & Lamontagne attribute their larger
clearances to "distance perception bias causing users to perceive virtual objects as being **closer** than
their actual distance."

**Every one of those mechanisms requires a visible virtual object to misperceive.**

Paper 1's hazard volumes are **visually hidden in all six conditions** (`PAPER1_STUDY_DESIGN.md` §5). There is
no rendered hazard whose distance could be underestimated. The dominant explanation for real-vs-virtual
divergence in this literature therefore largely does not apply to this paradigm.

Then turn it around. The reviewer's premise is that real VR gameplay has physical furniture present. True — and
**invisible to the user**, because they are wearing a headset. That is the entire problem the study addresses.

> A hazard that is present, spatially registered, and invisible is a **closer** model of the deployment
> condition than a visible virtual obstacle would be. The paradigm is not a compromised version of the real
> situation; it is a controlled instance of it.

This converts the objection into a statement of the study's construct validity. **Put it in related work.**

**Direction matters.** The reviewer's unstated model is *virtual → no consequence → recklessness → inflated,
meaningless numbers*. Every study above finds the **opposite**: participants are more cautious in VR. A reviewer
asserting "they won't care" is making an empirical claim the literature contradicts.

---

## 3. Where the honest problems are — and what to do about each

I went looking for evidence against the design. Here is what exists.

**3.1 Weber et al. (2022), *Sci Rep* 12:19655 — the paper a reviewer will cite.** VR obstacle training, then
**physical foam** obstacles. Transfer partial: trained participants cleared at **8 cm** vs the **4.7 cm** they
achieved in VR. *Why it does not land:* it studies **training transfer**, not measurement validity; the authors
explicitly confine their caution to "VR-based locomotor skill **training** paradigms… if they are to replace
training in the physical world"; and they **affirm within-VR validity** — "VR adaptation was fully retained over
1 week." Their physical comparator was **foam** — the very thing your PI rejected on displacement grounds.

**3.2 The counterweight — Suda, Fukuhara, Sato & Higuchi (2022), *Front Sports Act Living* 4:844436.** VR
aperture-crossing training **did** transfer to a real doorway: spatial margins significantly smaller
post-training with **collision rates unchanged** — safer *and* more efficient, in both younger and older adults.
Cite this alongside Weber. Transfer is task-dependent, not absent.

**3.3 He, Okubo et al. (2024), *Applied Ergonomics* — the only direct manipulation of physical consequence.**
56 older adults, split-belt treadmill; contacting a **virtual** obstacle triggered a real belt acceleration
simulating a trip. VR+Physical vs VR-only: collisions **0.75 → 0.63**, trailing-foot **0.68 → 0.57**, greater
margin of stability. **But** significantly **higher anxiety and perceived task difficulty**.

This is the closest thing to a test of "does physicality matter." Read it carefully:
- The effect is a **main effect of modest size** (~16% relative), not a reordering.
- It came bundled with **elevated anxiety** — i.e. adding physical consequence introduces its *own* confound.
- The obstacles were still **virtual**. Only the consequence was physical.

**3.4 Giesel, Ruseva & Hesse (2025), *Virtual Reality* — the most nuanced finding, and it changes a
recommendation.** Reaching around obstacles (limb avoidance, like Paper 1 — not locomotion). Sensitivity of
avoidance height to obstacle height, as a regression slope:

| Condition | Slope |
|---|---|
| Stereoscopic 3D, before physical exposure | 0.42 ± 0.16 |
| Physical obstacles | 1.31 ± 0.16 |
| Stereoscopic 3D, **after** physical exposure | **1.04 ± 0.18** — statistically indistinguishable from physical |
| 2D pictorial (before **and** after) | 0.06 / 0.11 — no height modulation at all |

Two consequences.

*First, the reassuring one:* stereo disparity gets you most of the way, flat imagery gets you nothing, and the
residual gap **closes with calibration experience**. Paper 1 has a cue tour and a practice block, room-scale
walking, a visible floor, and absolute-distance cues that a 43 cm shutter-glass setup lacks. Their obstacles were
**3.4–32.2 mm** tall — a millimetre-scale metric-calibration problem, far harder than decimetre-scale room
hazards.

*Second, the correction:* the authors warn that under a "tear-down" design, **"starting with the most complete
stimulus might influence (improve) performance for subsequently viewed less complex stimuli."** Handling a
physical obstacle **recalibrates** the participant for everything after it.

> **This reverses my earlier advice.** I previously suggested a passive-haptic check with one physical prop
> co-located with one virtual hazard. **That must be between-subjects, never within.** A prop introduced
> mid-session would recalibrate every subsequent block and confound it with condition order. Run it as a
> separate cohort or not at all.

**3.5 *Virtual Worlds* (2026) 5(1):6 — the actionable one.** Three conditions: real environment with full vision,
real environment with **lower limbs occluded**, and VR with **no lower-limb representation**. Occluding the limbs
*in the real world* already increased toe clearance while leaving baseline gait unchanged. VR **amplified** the
same adaptation — greater clearance, wider base of support, slower walking.

**So a substantial share of the "VR is different" effect is missing limb vision, not virtuality.** You control
that variable.

> **DECIDED 2026-09-14 — ankle markers, identical in every condition.** `PAPER1_STUDY_DESIGN.md` §5,
> `Runtime/SelfRepresentation.cs`, gate §14.27. Not a full avatar: a small neutral marker at each tracked ankle.

Two reasons, and the second is specific to this study rather than borrowed from the literature.

**Reason 1 — it narrows the largest controllable contributor to the real-vs-virtual gap**, and lets you write
"participants saw a tracked representation of their limbs, addressing the exproprioceptive deficit identified
by [Virtual Worlds 2026]" — converting a reviewer's objection into an anticipated design decision. Corroborating:
self-avatar presence changes collision-avoidance paths and body-volume awareness (IEEE VR 2019).

**Reason 2 — it repairs an asymmetry the controllers decision created.** Since 2026-09-11 participants hold
controllers in both hands, and XRI renders controller models. So **hands are visible and feet are not**. A
hand-targeted opportunity is avoidable with visual self-knowledge; a foot-targeted one is not. Target limb is a
nuisance factor, so this is a pure artifact — and it is invisible in the data unless someone looks for it.
Ankle markers remove it. This reason did not come from the literature; it came from reading §5 and §11 together.

**Why not a full avatar.** More rendering, more calibration, more to go wrong, and a mis-scaled avatar creates
its own distortions. Ankle markers buy most of the exproprioceptive benefit at a fraction of the risk.

> **The caveat — this is not free, and it should be stated.** Limb visibility plausibly **moderates H2**. A
> body-localized cue names a limb, and acting on it requires knowing where that limb is. Visible ankles may
> raise the localization benefit (the cue now points somewhere you can see) or lower it (the visual channel
> already supplied what the cue adds). Because the self-representation is a **constant of the apparatus**, it
> cannot confound the within-subject contrasts — but it does bound their generality. Say in the manuscript that
> the localization effect is estimated **under a minimal visible self-representation**, and name the no-avatar
> case as untested.

**Guardrails, all enforced in code.** Identical in all six conditions including None; never driven by
`ConditionManager`; frozen before data collection; marker diameter (0.07 m) kept **below** the smallest contact
radius (0.08 m) so it indicates limb *position* rather than disclosing contact geometry; neutral colour, never
the green-to-red of the Visual glow. `SelfRepresentation` logs its configuration at startup so a session that
silently ran a different setup cannot pass unnoticed. **Re-check the diameter when §14.22 freezes the radii.**

**3.6 The general critique.** Delgado Rodriguez, Radiah, Mäkelä & Alt (2023), *AHs '23* — raises ethics, internal
and external validity as standing VR-study challenges. Worth citing yourself: it signals you know the landscape.

---

## 4. Two arguments that put you on the front foot

**4.1 Physical contact is itself a feedback channel, and it contaminates the control condition.**
A participant who bumps a real obstacle *learns where it is*. Contacts are **not** evenly distributed across
conditions — **None** produces the most, by construction. So the worst condition would receive the most
incidental spatial learning, biasing the floor comparison in favour of every feedback condition. Virtual hazards
remove that channel: the only information about hazard location is the warning you manipulate.

There is now a second, sharper version. He/Okubo (§3.3) showed physical consequence **raises anxiety**. Anxiety
is not condition-neutral either — it would be highest in **None**. So physical hazards would confound the
control condition with *both* extra spatial information *and* elevated arousal. **On this dimension virtual-only
is strictly superior, and you can cite evidence for it.**

**4.2 Coolen (2020) is your motivation section, not your limitation.** Their holographic collision increase was
caused by participants having to rely on **feedforward visual information acquired earlier in the approach**,
because the obstacle left the display's field of view during crossing. That is a *loss of online perceptual
information about a hazard near the body* — which is **exactly the deficit Paper 1's haptic warning is designed
to fill**, and exactly what happens in real VR gameplay with real furniture below the FOV.

Reframed: *"Virtual and mixed-reality hazards are known to produce elevated contact rates because users lose
online visual information about hazards near the body during the approach [Coolen 2020]. This is precisely the
perceptual gap a body-localized tactile warning is intended to close, and it makes the virtual-hazard paradigm a
faithful model of the deployment problem rather than a departure from it."*

That paragraph turns the single most citable piece of counter-evidence into the reason your study exists.

**4.3 Operational.** Layout rotation (six isometric variants per participant) is free virtually and unrealistic
with physical props — a physical design would have to accept location learning as an uncontrolled confound.
And foam soft enough to strike safely is light enough to displace; once displaced, the system warns about empty
space and stays silent about where the foam is — a safety failure and a silent data failure at once.

---

## 5. What comparable papers actually do

| Work | Venue | Obstacles | Manipulated |
|---|---|---|---|
| **Abdlkarim, Mukherjee, Giunchi et al. 2026** — *Belt and Whistles* | **CHI 2026** | **virtual** | haptic belt, collision + proximity |
| *Improving VR navigation… haptic vest + upper-body tracking* 2023 | *Displays* | **virtual (static)** | proximity vibrotactile, collision rate DV |
| Samsel et al. 2025, *Applied Ergonomics* 122:104396 | vest early-warning | obstacle alert | vibrotactile **pattern** (point/column/wave) |
| Kruijff, Riecke, Trepkowski & Lindeman 2022 | *Front. VR* 3 | **virtual** | feet/lower-body proximity vs collision cues |
| Virtual-wall collision behaviour 2021 | IEEE | **virtual** | task engagement, wall appearance |
| Boundary-metaphor / depth-aware boundary work | Sensors, SUI | virtual boundary | visualization |
| VRCAT 2022 | *Visual Computer* | **real**, camera-detected | detection + display |
| Guardian awareness for bystanders 2023 | **IEEE VR** | real bystanders | notification technique |

**The pattern is unambiguous.** Papers comparing *warning designs* use **virtual** hazards. Real obstacles appear
in *sensing and detection* papers, where detecting the real object **is** the contribution. Paper 1 is in the
first category.

**Two decisive precedents.** *Belt and Whistles* (CHI 2026) — haptic warning, virtual obstacles, collision-count
DV, accepted at an A\* venue **this year**. And the *Displays* 2023 vest study — haptic vest, real-time
upper-body mocap, proximity-based vibrotactile warning from **static virtual obstacles**, collision rate as the
dependent measure. That is Paper 1's methodology almost exactly, already through peer review.

If virtual obstacles invalidated this class of study, neither paper would exist.

---

## 6. Recommendations

**Design changes — all applied 2026-09-14:**

1. ✅ **Ankle markers, identical in every condition** (§3.5). `PAPER1_STUDY_DESIGN.md` §5 records the decision,
   the rationale, the guardrails, and the H2-moderation caveat; `Runtime/SelfRepresentation.cs` implements it;
   gate §14.27 verifies it. **Open item:** re-check marker diameter against the frozen radii (§14.22).
2. **Keep the practice block and cue tour, and describe them as perceptual calibration** (§3.4), not just task
   familiarization. Giesel shows the calibration gap closes with exposure — say that this is why they exist.
3. ✅ **No physical prop in the main study.** Any prop validation is **between-subjects future work** (§3.4).
   Never mid-session: physical exposure recalibrates everything after it.
4. **No structural change otherwise.** Virtual-only, hidden hazards, six isometric layouts, controllers in both
   hands all stand. Nothing in this review threatens them.

**Framing (do these):**

5. **Lead with the hidden-hazard argument** (§2.6). Strongest point available and specific to this design.
   ✅ Written into `PAPER1_STUDY_DESIGN.md` §13; gate §14.28 holds it to related work rather than limitations.
6. **Scope in the title and abstract, not the limitations.** *Hazard-boundary violations*, never "collisions
   prevented." Already required by §13.
7. **Pre-empt in related work.** Draft:

> Hazards are virtual volumes registered in the tracking space. Direct comparisons of real and virtual obstacle
> negotiation consistently find that immersion alters the *magnitude* of avoidance margins — participants are
> more conservative in VR — while preserving the *structure* of avoidance: scaling with obstacle dimensions,
> side selection, onset timing, and steering dynamics [Fink 2007; Coolen 2020; Bühler & Lamontagne 2025], with
> comparable psychological and movement responses even under threat [Sci Rep 2024]. We therefore treat absolute
> violation rates as non-transferable and confine all confirmatory claims to within-subject contrasts. Physical
> obstacles were additionally rejected on methodological grounds: physical contact is itself a spatial-information
> channel, and physical consequence elevates anxiety [He et al. 2024] — both unevenly distributed across
> conditions, since the no-feedback condition produces the most contacts. Physical hazards would therefore
> confound the floor comparison with both incidental learning and differential arousal.

8. **Use Coolen as motivation** (§4.2). Best single move in this document.
9. **Cite Suda 2022 wherever you cite Weber 2022** (§3.2) — do not let Weber stand unanswered.
10. **Report engagement as evidence, not assertion.** The 2021 virtual-wall study gives the failure mode (10%
   collisions when engaged vs 52.5% when bored) and therefore the test. Your task scoring, avoidance-intent item,
   and non-floor violation rates *demonstrate* it is absent.
11. **Name the ethics argument.** *Sci Rep* 2024 states in print that VR is "far more ethically viable… owing to
   the much reduced physical risk." Your IRB application should quote it.

**Future work, not this paper:** a **between-subjects** cohort with one heavy, fixed, fully padded prop
co-located with one virtual hazard, comparing that hazard's violation rate against the virtual-only cohort.
Bounds the physicality effect empirically. He/Okubo (§3.3) predicts a modest main effect — the result you want.
Naming it in the discussion costs a paragraph and removes the sting from "but what about real furniture."

---

## 6b. What this predicts for the pilot — read before tomorrow

The literature makes a **directional prediction about your violation rate**, and it is worth knowing in advance.

Every direct comparison found **larger** avoidance margins in VR: Fink **+0.16 m**, Bühler & Lamontagne
**+0.19 m**. Participants keep further away from virtual hazards than they do from physical ones.

**So expect violation rates at the LOW end of intuition — a floor is the likely failure mode, not a ceiling.**

Three consequences for the pilot:

1. **Gate §14.10 (non-floor, non-ceiling) is the gate that matters.** It is not a formality. The literature
   predicts pressure toward exactly the failure it screens for.
2. **The provisional capsule radii are load-bearing.** Chest 0.12 / hand 0.08 / foot 0.10 m are engineering
   guesses (`PILOT_READINESS_TRACKERS.md` §8.2). Combined with a population that naturally gives virtual
   hazards a wide berth, radii slightly too small will floor the rate at zero.
3. **If block 1 reads 0%, suspect geometry before you suspect the task.** Reach deliberately into a hazard to
   confirm detection fires at all, then revisit radii and opportunity geometry. Do not conclude "participants
   are just good at this."

The ankle markers cut mildly the other way — better limb awareness should tighten margins somewhat — but they
do not remove the concern.

---

## 7. Residual honest limitations

- Absolute violation rates are attenuated relative to physical hazards and are not deployment estimates
  (Fink 2007; Bühler & Lamontagne 2025; Insko 2001; He et al. 2024 bounds the consequence component).
- Fine metric calibration of avoidance to hazard *size* may be initially imperfect (Giesel 2025); the practice
  block mitigates but does not eliminate it.
- Motivation is instructed and point-based rather than intrinsic.
- The six layouts are isometries of a single authored arrangement; arrangement diversity is untested.
- Transfer to physical furniture is untested here and is named future work.

---

## Sources

**Direct real-vs-virtual comparisons**
- Fink, Foo & Warren 2007, *ACM TAP* — https://dl.acm.org/doi/10.1145/1227134.1227136
- Coolen et al. 2020, *Sensors* 20(4):1095 — https://www.mdpi.com/1424-8220/20/4/1095
- Bühler & Lamontagne 2025, *Sensors* 25(6):1667 — https://pmc.ncbi.nlm.nih.gov/articles/PMC11945152/
- Giesel, Ruseva & Hesse 2025, *Virtual Reality* — https://pmc.ncbi.nlm.nih.gov/articles/PMC11872779/
- *Virtual Worlds* 2026, 5(1):6 — https://doi.org/10.3390/virtualworlds5010006
- *Sci Rep* 2024, 14:5262 — https://www.nature.com/articles/s41598-024-55253-9

**Transfer**
- Weber et al. 2022, *Sci Rep* 12:19655 — https://www.nature.com/articles/s41598-022-24085-w
- Suda et al. 2022, *Front Sports Act Living* 4:844436 — https://doi.org/10.3389/fspor.2022.844436

**Physical consequence / passive haptics**
- He, Okubo et al. 2024, *Applied Ergonomics* — https://www.sciencedirect.com/science/article/abs/pii/S0003687024002199
- Insko 2001 — https://dl.acm.org/doi/10.5555/933178

**Method precedent**
- Abdlkarim et al. 2026, *Belt and Whistles*, CHI — https://doi.org/10.1145/3772318.3793143
- *Displays* 2023, haptic vest + upper-body tracking — https://www.sciencedirect.com/science/article/pii/S0141938223000501
- Samsel et al. 2025, *Applied Ergonomics* 122:104396 — https://pubmed.ncbi.nlm.nih.gov/39362084/
  **Verified 2026-09-14.** An earlier pass in this document wrongly doubted this citation and substituted the
  *Displays* 2023 paper for it. Both are real and distinct, and `PAPER1_STUDY_DESIGN.md` §1 cites Samsel
  correctly. Samsel also carries a **directional prior for H4**: visual alerts produced significantly shorter
  response times than vibrotactile alerts.
- Kruijff, Riecke, Trepkowski & Lindeman 2022, *Front. VR* 3:954587 — https://doi.org/10.3389/frvir.2022.954587
- Virtual-wall collision behaviour 2021 — https://arxiv.org/html/2107.08439v2
- VRCAT 2022, *Visual Computer* — https://link.springer.com/article/10.1007/s00371-022-02676-y

**Theory / perception / critique**
- Slater 2009, *Phil Trans R Soc B* — https://royalsocietypublishing.org/doi/10.1098/rstb.2009.0138
- Kelly 2022, *IEEE TVCG* — https://pubmed.ncbi.nlm.nih.gov/35925852/
- Delgado Rodriguez, Radiah, Mäkelä & Alt 2023, *AHs '23* — https://dl.acm.org/doi/10.1145/3582700.3582716
- Ecological validity of VR locomotion, *Front. VR* 2025 — https://www.frontiersin.org/journals/virtual-reality/articles/10.3389/frvir.2025.1699143/full
