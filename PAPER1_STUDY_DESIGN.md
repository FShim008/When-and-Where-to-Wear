# Paper 1 Study Design — What Makes a Collision Warning Work?

**Document status:** Design authority for Paper 1  
**Version:** 1.8 — 2026-09-14  
**Study status:** Design only; implementation and human-study readiness are not certified  

**Changes in 1.8 — adversarial design review (ISMAR-reviewer pass).** One blocking issue and four gaps.
**Blocking, now partly resolved:** the timing factor **could invert**, because `D`/`T` = 0.30/0.50 put the
policy crossover at 0.60 m/s — inside the range of real limb speeds — so on slow approaches the "predictive"
cue fired *later* than the "proximity" cue. Simulation against the real oracle showed the dominant driver is
**clear approach distance**, not speed: a limb starting closer to the hazard than `D` triggers the proximity
cue at movement onset, and no value of `T` can beat that. `T` is raised to **1.00 s**, regression-tested, and
the residual requirement is an **opportunity-geometry audit** (≥ 0.40 m clear approach; §6, gate 27). The frozen policy parameters, which previously
existed only in `Core/CollisionOracle.cs`, are now recorded here (§6). Added: a preregistered engagement pass
criterion (§8), a demand-characteristic/expectancy probe (§8), screening and recorded participant
characteristics including handedness and an SSQ stopping threshold (§4), and the Samsel et al. (2025)
directional prior for H4 (§2). Recorded that the §5 ankle markers are a **dependency of the Visual
benchmark** rather than a cosmetic addition. Gates renumbered, now 34.


**Changes in 1.7 — tracking decided by paired pilot comparison** (PI discussion), superseding the 1.5 Vicon
decision. Both systems are built and logged **simultaneously** on the same pilot participants — sequential
runs would confound tracker with participant and session. Selection is **per paper** (a split outcome is
permitted and likely) against **criteria recorded and dated before the first pilot session**, since choosing
afterwards is a researcher degree of freedom rather than a decision. Paper 1's criteria are tabulated in §11,
the decisive one being **delivered lead-time jitter in the predictive condition**, which guards against
velocity noise degrading one arm of H1 and not the other. The 1.5 velocity analysis is retained as the
**prediction the pilot tests**, not withdrawn. §5 contact geometry is now conditional on the outcome; §11
covers both systems' failure modes and both hand-tracking arrangements. Both systems must work before
piloting, which front-loads engineering. Gates renumbered, now 27.


**Changes in 1.6 — remaining open decisions closed.** Participant input is **one controller, questionnaires
only**, set down before each block, chosen so the experience outcomes are answered unobserved (§11; gate 24
closed). **Simultaneous commodity-tracker logging is rejected** on scope, so robustness degradation is
synthetic and the manuscript must state that deployment inference is extrapolation rather than measurement
(§11). Sampling is **independent cohorts** — Paper 1 and Paper 2 share no participants — recorded in
`COMBINED_STUDY_DESIGN.md` and the disclosure letter.


**Changes in 1.5 — tracking reverted to Vicon.** Superseding the 1.3 inside-out decision. Rationale: position
error propagates into velocity error amplified ~70× at 100 Hz, and the **predictive** policy depends on
closing speed while **proximity** depends only on distance — so inside-out noise would have degraded one arm
of H1 and not the other. That is a directional confound, not symmetric attenuation, and it would have biased
H1 against prediction with no way to separate a null result from insufficient velocity precision. Neither
paper is a deployment study, so the commodity-hardware framing bought nothing here. Consequences: contact
geometry is no longer noise-limited and the capsule model stands on anthropometric grounds (§5); hands are
tracked directly, removing the wrist-to-hand offset entirely (§11); the tracking risk profile moves from
sensor noise to **marker occlusion, swapping/mislabelling, and detachment** (§11); co-registration returns as
a gate, mitigated by the already-verified rigid-alignment solver; the robustness analysis is restored to its
stronger original form with a real reference stream (§11). Gates renumbered, now 26.


**Changes in 1.4 — open design decisions closed:** the **H4 visual cue** indicates the at-risk body part,
making Visual information-matched to PB and H4 a clean modality comparison (§2); **H5's "feedback family"**
is two confirmatory contrasts (pooled haptic vs None, Visual vs None) with per-condition eligibility computed
descriptively to operate the §15 gate without spending confirmatory budget on a foregone conclusion (§3); the
**H2 information confound** is resolved by narrowing the claim to "somatotopic limb cueing versus
undifferentiated torso alert," with three manuscript obligations and the directional-torso study designated as
follow-up (§3). Rejected alternatives are recorded in each case so choices are not revisited after seeing
data. Gates 16 and 17 closed.


**Changes in 1.3 — hardware decision (VIVE Ultimate Trackers + TactSuit/Tactosy).** *The tracking half of
this entry is SUPERSEDED by 1.5; the actuator half stands.* Recorded the actuator
decision and recommended the Tactosy-at-torso variant that preserves an anatomical-mapping claim for H2 (§2);
made contact-geometry sizing against measured tracker error a blocking requirement, since inside-out tracking
re-opens the exact problem Vicon was selected to solve (§5); replaced Vicon co-registration requirements with
inside-out dropout, drift, motion-blur and wireless-latency requirements, and recorded the five-tracker
decision (§11); replaced the reference-stream robustness analysis with a bench-characterized error model (§11);
added launch gates 18–21.


**Changes in 1.1:** added the blocking actuator decision (§2); flagged the `VisualAlertModel` implementation
conflict (§2); added a novelty-freeze procedure with known prior art (§1); added provisional sample size (§4)
and provisional equivalence margins (§3); ruled task score out as an independent outcome (§5); added the
condition-ranking and design-recommendation framework (§15); corrected the venue claim.

**Changes in 1.2 — hypothesis audit (§3):** added the **interpretation-order rule** making H3 govern whether
H1 and H2 are reported as main effects or simple effects — previously unspecified, and the largest gap in the
hypothesis structure; named the **information confound in H2** (somatotopic placement bundled with limb
identity) and required a recorded response to the directional-torso alternative published as Belt & Whistles;
justified the two-sided framing of H1 and H2 with explicit mechanisms in both directions; preregistered the
competing synergy and redundancy mechanisms for H3; required an a-priori justification for comparing **PB**
specifically in H4 and forced the unresolved decision about what the visual cue indicates; reframed **H5** as a
validity gate and required "feedback family" to be defined; added a contribution-to-hypothesis map (§1).

**Planned venue:** IEEE ISMAR 2027 — **host city and dates not yet announced**; deadline provisionally expected around March 2027 by analogy with ISMAR 2026 (Bari, 5–9 Oct 2026; abstracts 9 Mar 2026). Verify on `ieeeismar.net` before planning. See `PROJECT_HANDOFF.md` §9.

This document replaces the scientific design claims in `PROJECT_HANDOFF.md`. The handoff remains an engineering inventory and may describe components that are planned rather than implemented.

## 1. Research question and contribution

### Primary question

Given a system that estimates impending virtual-hazard contact, how do warning **policy** and haptic **spatial mapping** independently and jointly affect the probability that an at-risk body part crosses a virtual hazard boundary?

### Factors

Within the confirmatory 2 × 2 factorial:

- **Warning policy:** proximity-triggered versus TTC/prediction-triggered.
- **Spatial mapping:** generic torso cue versus somatotopic cue on the at-risk limb.

Two separately analyzed anchors bracket the factorial:

- **None:** no hazard warning.
- **PBC (continuous cue):** PB's predictive trigger, site, information and perceptible onset, delivering a
  CONTINUOUS intensity ramp over the approach instead of one discrete pulse train. Carries **H4'**.
  Because TTC = distance / speed, slowing down lowers its intensity - that feedback loop is the mechanism
  under test, not a defect (see §3 H4' and docs/PAPER1_PARAMETER_JUSTIFICATION.md §5B).
- ~~**Visual benchmark:**~~ a transient visual warning on PB's predictive trigger. **DESCOPED 2026-09-23** -
  it remains implemented and tested but is no longer scheduled; PBC now occupies that block. The sections
  below describing the Visual cue are retained for the record and for any future paper that schedules it.

The primary interpretation of the timing factor is the **total effect of a warning policy**. It may include differences in warning coverage, false alarms, and delivered lead time. It is not described as a pure timing effect unless those quantities are experimentally matched.

### Defensible novelty — REWRITTEN 2026-09-23

**The previous claim is withdrawn.** It read: *"a controlled factorial test of proximity versus TTC warning
policy crossed with generic versus at-risk-limb tactile mapping."* **Valkov and Linsen (IEEE VR 2019) already
ran proximity versus TTC warning in VR**, with N = 40, and found the TTC-like policy produced *more*
collisions. Claiming that comparison as novel would have been refuted by any reviewer who knows the paper —
and it survived a title-and-abstract search, dying only on the first full-text read.

**The defensible novelty is now:**

1. **Separating trigger policy from cue form.** Valkov and Linsen varied both at once: their speed-scaled
   condition changed the trigger *and* introduced a continuous intensity mapping participants could switch
   off by slowing down. Their null is attributable to either. H1 (trigger varies, cue form constant) and
   H4′ (cue form varies, trigger constant) are the two halves of that confound, and PB is the shared cell.
2. **Crossing trigger policy with somatotopic mapping.** No prior work crosses these factors; theirs used
   head-mounted tactors with no localization factor at all.
3. **Reaching at real obstacles**, per-limb, rather than walking toward a virtual wall with a simulated sensor.

**The contribution is a resolution, not a first look.** That is a stronger position: both outcomes are
informative, which is what a preregistered hypothesis should have. Full analysis, the comparison table, and
the paste-ready framing: `docs/PAPER1_PARAMETER_JUSTIFICATION.md` §5B.

### Contribution-to-hypothesis map

Every claim must trace to a confirmatory hypothesis, an explicitly descriptive analysis, or a preregistered
decision rule. Mixing these is how exploratory findings acquire confirmatory confidence.

| Contribution | Status | Where |
|---|---|---|
| Policy × mapping factorial | Confirmatory (H1, H2); **H3 exploratory** | H1, H2 (§3) |
| **Discrete versus continuous cue form** | **Confirmatory, planned contrast** | **H4′ (§3) — PB vs PBC** |
| ~~Haptic versus timing-matched visual~~ | **Descoped 2026-09-23** — Visual is implemented and tested but no longer scheduled; the block it occupied now runs PBC | ~~H4~~ |
| Feedback beats no feedback | **Validity gate**, not a finding | H5 (§3); admits conditions to §15 Step 0 |
| Experience outcomes (presence, workload, sickness, acceptability) | Secondary; equivalence claims only with a justified margin | §3 |
| Warning coverage, delivered lead time, false alerts, alert burden | **Descriptive policy performance.** No hypothesis — these are consequences of assigned policy, not covariates | §3, §15 Step 3 |
| Avoidance-response kinematics | **Descriptive mechanism.** No hypothesis | §10 |
| Tracking-noise robustness | Exploratory simulation | §12, §11 |
| Which condition to recommend | **Decision rule**, not a hypothesis test | §15 |

Two entries deserve emphasis. Alert burden carries no hypothesis yet does real work in the §15 ranking, where
it is the tie-break — so it must be reported precisely even though nothing is being tested about it. And the
ranking in §15 is a decision procedure; it never produces a p-value and must not be written as though it did.

Do not claim that haptic collision warning, predictive warning, or body-localized vibration is independently new. Complete a systematic/scoping literature search and freeze the novelty statement before submission.

### Novelty-freeze procedure

"Run a literature search before submission" is not actionable as written. Do this, and date the result in this
document:

1. **Search now, not at write-up.** The novelty claim determines whether the actuator decision in §2 matters,
   whether the Visual anchor is worth rebuilding, and whether the factorial is the right design at all. A gap
   discovered after data collection cannot be repaired.
2. **Record the search** — databases, strings, date, screening criteria, and counts — so it can be reported and
   re-run. Targeted searching is what produced the earlier unverified venue and prior-art claims in this
   project; a recorded protocol is the correction.
3. **Address these known items explicitly in related work:**
   - **"Belt and whistles" (CHI 2026)** — haptic belt, directional torso encoding, **virtual** obstacles,
     plausibility framing, no timing manipulation. Also establishes A*-venue precedent for virtual-obstacle
     collision studies, which helps the method's legitimacy.
   - **Samsel et al. (2025), *Applied Ergonomics*** — vibrotactile pattern comparison on a vest.
   - **Ring, Tietenberg, Emmerich & Masuch (CHI 2024)**, Collision Anxiety Questionnaire, plus follow-ups
     through CHI 2025 — an active group in this space, and a validated instrument worth adopting.
   - **Martini, Solari & Chessa (ICIAP 2023)** — deep-learning 3D obstacle detection for XR avoidance,
     successor to Valentini et al. (IEEE VR 2020).
   - **Bloomfield & Badler (2007), "Collision Awareness Using Vibrotactile Arrays," *IEEE Virtual Reality
     Conference* 2007, pp. 163–170, doi:10.1109/VR.2007.352477** — Honorable Mention. **Added 2026-09-14
     after it surfaced in the Paper 2 venue search; it should have been here from the start.**

     A sleeve of tactors on the **arm**, driven by a real-time human model, firing when the corresponding body
     segment contacts a virtual object. Human-subject experiments showed full-arm vibrotactile feedback
     improved performance over purely visual feedback. **This is the direct ancestor of H2** — the earliest
     demonstration that limb-localized vibrotactile signalling beats a visual channel for body–object contact
     in VR.

     **It does not threaten H1–H5, and the reason is the sharp differentiator:** their cue fires **on
     contact**, so it is *feedback*, not *warning*. Nothing is predicted and nothing arrives in advance. Paper
     1 manipulates whether the signal arrives **before** contact and whether it names the limb; Bloomfield &
     Badler establish only that the body is a useful place to put the signal once contact has happened.

     **Cite it in related work as the origin of somatotopic collision signalling, not in limitations.** A
     reviewer from the VR haptics literature will expect to see it, and its absence would read as an
     unsearched related-work section.
   - Boundary/safety-region work that predicts **boundary geometry** rather than time-to-contact with a
     designated object, which is adjacent to but distinct from the policy manipulation here.
4. **Freeze the statement** in this section with a date, and re-run the search shortly before submission to
   catch anything published in the interval.
5. **If the crossing has been taken**, reframe rather than argue. The mechanism analyses, the ranking
   framework in §15, and the robustness curve remain contributions even if the factorial does not.

## 2. Conditions

| Code | Policy | Mapping/modality |
|---|---|---|
| `RG` | Proximity | Generic torso haptic |
| `RB` | Proximity | At-risk-limb haptic |
| `PG` | Predictive TTC | Generic torso haptic |
| `PB` | Predictive TTC | At-risk-limb haptic |
| `None` | No warning | None |
| **`PBC`** | **Predictive TTC, identical to PB** | **At-risk-limb haptic, CONTINUOUS intensity ramp (H4')** |
| ~~`Visual`~~ | ~~Predictive TTC, matched to PB~~ | **DESCOPED 2026-09-23** - implemented and tested, not scheduled |

### Cue constraints

- The four haptic conditions use the same temporal waveform.
- To support a clean anatomical-mapping claim, use the **same actuator model and mounting method** at the generic
  torso site and each localized site whenever technically possible.
- Generic and localized cues are calibrated for detectability/perceived salience before the experiment.
- Generic torso stimulation uses a localized motor cluster comparable in stimulated area to a limb unit; it must not activate the entire vest.
- The motor pattern, gain table, device, firmware, and physical command-to-onset latency are recorded.
- The tactor sound is masked so the haptic condition is not inadvertently audiovisual.
- A cue tour occurs before practice and outside measured blocks.

If identical actuators cannot be used, bench-measure amplitude/frequency/onset at every site, perceptually match
salience, and name the factor **haptic mapping implementation** rather than pure anatomical localization. A
TactSuit torso pattern versus a different Tactosy device is otherwise a bundled device × body-site manipulation.

### Actuator decision — blocking, must be recorded before implementation

The inventory in `PROJECT_HANDOFF.md` §7 currently specifies **TactSuit X40 (torso) + Tactosy (limbs)** — two
different device families, drivers, and mounting methods. As specified, the Mapping factor is confounded with
device, and **H2 cannot be cleanly interpreted**. This is not a residual limitation to disclose; it is a design
decision that must be resolved before any implementation work continues.

Choose one and record it, with date and rationale, in this document:

| Option | Consequence for H2 |
|---|---|
| **A — matched actuators.** Use the same actuator model and mounting at torso and every limb site. | H2 is an anatomical-mapping effect. Preferred. |
| **B — mixed devices, matched perceptually.** Bench-measure and salience-match, then rename the factor **haptic mapping implementation**. | H2 is a bundled device × site effect. Title, hypotheses, and abstract must all use the weaker construct name. |
| **C — torso cluster from the same limb-unit family.** Mount a limb-class actuator at the torso site instead of using the vest. | H2 is an anatomical-mapping effect at the cost of not using the vest. |

Option B is publishable but materially weaker, and the weaker construct name must propagate to every claim.
Do not defer this decision to the pilot: it determines procurement, the cue-calibration procedure, and the
wording of the primary mapping hypothesis.

### Recorded decision — 2026-09-07

The selected hardware is **bHaptics TactSuit (torso) + Tactosy units on forearms and feet**. Taken at face
value this is **Option B**: two device families split across the generic and localized sites, which confounds
Mapping with device and forces the weaker construct name.

**Recommended variant — Option C without giving up the vest.** The confound is avoidable at near-zero cost by
separating the two jobs the vest is currently doing:

| Channel | Hardware | Role |
|---|---|---|
| **Warning cue** (the manipulated factor) | Tactosy units only — including one **mounted at the torso** for the generic condition | Same device family, driver, and mounting at every site. Restores an anatomical-mapping claim for H2. |
| **Task feedback** (projectile hits) | TactSuit vest | Identical in every condition, carries no hazard information, and is therefore permitted under the §10 integration constraint on task versus hazard feedback. |

This keeps the vest where it adds value — game feel — and removes it from the comparison that has to be clean.
The generic torso cue becomes a Tactosy unit strapped to the sternum, matched by construction to the limb
units.

**Two things to verify at pilot before committing:**

- **Confusability.** The generic warning cue and the projectile-hit feedback would both be on the torso. They
  must be reliably distinguishable — different waveform, different site, and confirmed by a forced-choice
  discrimination check during the cue tour. If participants confuse them, the generic condition is corrupted.
- **The fallback if they are confusable.** Deliver projectile-hit feedback through audio and visuals only and
  drop vest haptics entirely. This costs a little immersion and removes the problem completely. It is the
  cleaner design and should be preferred if the discrimination check is at all marginal.

**If the Tactosy-at-torso variant is rejected**, then the study is running Option B, and every downstream claim
must use **"haptic mapping implementation"** rather than anatomical localization — in the title, the
hypotheses, the abstract, and §16. Record which path was taken and the date.

### DECISION — Tactosy at torso, confirmed 2026-09-07

The **Tactosy-at-torso variant is adopted**. Five Tactosy units carry the warning cue: sternum (generic
condition), both forearms, both feet. The TactSuit vest is retained for projectile-hit task feedback only and
never delivers a warning cue.

**H2 therefore remains an anatomical-mapping hypothesis** and does not require the weaker construct name.

Three execution details this decision creates:

**1. Mounting is not perfectly matched, and the salience calibration now carries more weight.** §2 requires the
same actuator model *and mounting method* at every cue site. A sternum unit worn under or over the vest is not
coupled to the body identically to a forearm unit strapped directly. The per-site perceptual salience
calibration already required in §2 compensates for this at the perceptual level, which is the level that
matters for the hypothesis — but it is now load-bearing rather than a formality. Bench-measure amplitude and
frequency at the torso site as mounted, report the mounting difference as a stated limitation, and do not skip
the calibration to save session time.

**2. Separate the two torso channels spatially.** The generic warning cue and vest projectile feedback are both
on the torso and must not be confusable. Constrain **projectile-hit feedback to vest regions away from the
sternum** — the back panel is the cleanest choice. A projectile hit does not need to be spatially accurate to
read as "you were hit," so nothing is lost. This should make the required forced-choice discrimination check
easy to pass rather than marginal.

**3. Keep the fallback documented.** If the discrimination check is at all marginal at pilot, drop vest haptics
entirely and deliver projectile feedback through audio and visuals. That removes the problem completely at a
small cost in engagement.

**Procurement note.** Five Tactosy units are needed. bHaptics sells arm and foot units in pairs, so the torso
unit is most easily a spare arm-class unit — which is also what keeps the actuator family matched.

### Visual benchmark — RETAINED FOR THE RECORD, NOT SCHEDULED (descoped 2026-09-23)

> Everything in this section still describes the implemented `Visual` condition accurately, and the H4
> modality question remains a legitimate one. It is simply not part of this paper: the sixth block now runs
> `PBC` (H4', cue form), which addresses the contrary result in Valkov & Linsen (2019). Re-scheduling Visual
> means editing `SessionPlan.Conditions` and restoring H4 to §3 — and finding a seventh block, or dropping
> something else.


The Visual condition is an engineered benchmark, not “current practice.” It must:

- use the same predictive trigger, eligibility, and nominal onset as PB;
- be transient and duration-matched rather than a continuously visible hazard;
- avoid showing the obstacle itself or revealing its exact location persistently; and
- indicate **the at-risk body part** (DECIDED 2026-09-07 — see below).

### DECIDED 2026-09-07 — the visual cue indicates the at-risk body part

The Visual benchmark highlights **the specific limb predicted to collide**, not merely that a collision is
imminent and not the hazard's direction.

This makes Visual **information-matched to PB**: both conditions deliver the same predictive trigger and the
same spatial content, differing only in modality. H4 is therefore a clean **modality** comparison, and a PB
advantage is attributable to haptics rather than to carrying more information than its comparator.

Rejected alternatives, recorded so the choice is not revisited after seeing data:

| Alternative | Why rejected |
|---|---|
| Undifferentiated "collision imminent" cue | Information-matched to **PG**, not PB. H4 would confound modality with information content, and a PB advantage would prove little. |
| Directional indication toward the hazard | Matched to neither condition cleanly; introduces a third information level into a two-condition contrast. |

Implementation constraints that follow: the highlight must be transient and duration-matched to the haptic
pulse train, must identify the limb without persistently revealing hazard geometry, and must be rendered
somewhere the participant can perceive it without a head turn — an on-body or avatar-anchored indication
rather than a screen-edge overlay. Pilot that it is detectable during vigorous movement; a visual cue the
participant misses is a failed manipulation, not evidence against the visual modality.

PB versus Visual is therefore a planned cross-modality benchmark, not part of the 2 × 2 factorial.

**The ankle markers are a dependency of this cue, not a cosmetic addition (noted 2026-09-14).** The Visual
benchmark must be *on-body or avatar-anchored* and must identify the at-risk limb. Hands carry controller
models and can be highlighted. **Before the §5 ankle markers there was nothing on a foot to anchor a
highlight to**, so the required cue was not implementable for foot opportunities — and roughly half the target
limbs are feet. The markers give every target limb a consistent anchor. Record this dependency: removing the
self-representation would silently break H4 for foot opportunities rather than merely changing what
participants see.

**A directional prior for H4 that should be acknowledged rather than discovered by a reviewer.** Samsel et al.
(2025) report **significantly shorter response times for visual alerts than for vibrotactile alerts** in an
obstacle early-warning task. That is a published prior predicting Visual may beat PB on response latency. It
does not predict the violation-probability outcome, and the two can diverge — but state the prior in related
work and frame H4 against it. A PB advantage found *despite* that prior is a stronger result than one
presented as though no prior existed.

**Implementation conflict.** The retained `VisualAlertModel` component described in `PROJECT_HANDOFF.md` §5
renders a *continuous, proximity-graded glow with a distance-driven pulse rate*. That is the opposite of what
this section requires on two counts: it is continuous rather than transient, and it is proximity-driven rather
than sharing PB's predictive trigger. The existing component cannot be used for the Visual benchmark without
being rewritten. Either rewrite it to be trigger- and duration-matched to PB, or drop the Visual anchor from the
confirmatory design — do not run the legacy glow and describe it as matched.

## 3. Hypotheses and planned contrasts

### Confirmatory factorial hypotheses

- **H1 — Policy main effect (CONFIRMATORY):** predictive TTC policy changes the probability of a target hazard-boundary violation relative to proximity policy, averaged over mapping. *Cue form is held constant across this contrast — that is what distinguishes it from Valkov and Linsen (2019).*
- **H2 — Mapping main effect (CONFIRMATORY):** at-risk-limb mapping changes violation probability relative to generic torso mapping, averaged over policy.
- **H3 — Interaction (EXPLORATORY, 32% power):** the effect of at-risk-limb versus generic mapping differs between predictive and proximity policies. Estimated and reported with its interval; **no decision attaches to it** and it does not govern H1 or H2.
- **H4′ — Cue form (CONFIRMATORY, planned contrast):** a continuous, intensity-modulated cue (PBC) changes violation probability relative to a discrete edge-triggered cue (PB), with trigger, site, information and onset held constant.

  **Why H4′ exists.** Valkov and Linsen (IEEE VR 2019, N = 40) found forecast-triggered warning produced
  significantly *more* collisions than distance-triggered warning (Z = −2.23, p = 0.024). Their explanation
  was the mapping, not the trigger: intensity was a continuous function of distance and speed, so slowing
  down reduced the vibration and participants *"continued walking slowly forward while constantly decreasing
  the speed to adjust the vibration level."* Trigger and cue form were confounded. H1 and H4′ separate them.

  **Direction.** Two-sided, but the literature gives a directional prior: their continuous cue underperformed.
  A PB advantage over PBC would explain their result; a null would mean cue form is not the explanation and
  the discrepancy lies elsewhere — in the task, the effector, or the trigger itself. Both are reportable.

  **What makes it clean.** PBC shares PB's predictive trigger, limb-localized site, and perceptible onset.
  The onset match is enforced in `ConditionManager` (PBC's intensity band is derived from the *same*
  effective threshold the trigger uses) and asserted in `ContinuousCueTests`. Without that match H4′ would
  confound cue form with timing — the very confound it exists to remove.

The H3 contrast is the difference-in-differences:

```text
(PB − PG) − (RB − RG)
```

on the model's preregistered scale. “PB is the lowest condition” is not itself proof of an interaction.

**Preregister the expected mechanisms, in both directions.** An interaction with no stated mechanism is
uninterpretable after the fact and invites a story fitted to whatever sign appears. Two plausible accounts
exist and they predict opposite signs:

- **Synergy.** Knowing *which limb* is only actionable if there is still time to move that limb. Under
  proximity timing the warning arrives too late for the location information to be used, so localization adds
  little; under predictive timing it pays off. Predicts localization helping *more* under prediction.
- **Redundancy.** A long lead time gives the participant room to work out what is threatened unaided, so being
  told adds less. Under a late warning there is no time to work it out and the cue must supply it. Predicts
  localization helping *more* under proximity.

Both are credible. Recording them now means either result is interpretable, and neither can be presented as
the outcome that was expected all along.

The redundancy case is also the commercially consequential one: limb-localized haptics requires an actuator per
limb, while a torso cue needs one device. Evidence that good predictive timing makes localization largely
redundant is a concrete, useful finding for developers, not a disappointment.

### Interpretation order — SUPERSEDED 2026-09-23. H3 no longer governs anything.

**The previous rule — test H3 first, and if supported read H1 and H2 as simple effects — is withdrawn.**

The rule is statistically orthodox and was wrong *here*. Measured power for H3 at the planned design is
**32%** (`power_analysis.R`, nsim = 200); 80% needs **N = 80 with 24 opportunities**, roughly 240
participant-hours. Letting a 32%-powered test decide how two well-powered tests are read means a coin flip
governs the paper's interpretation. A reviewer who opens the power table will say exactly that, and they
will be right.

The original concern remains valid and is handled differently: a main effect that conceals a real
interaction is misleading. The answer is to **report the cell means and the difference-in-differences every
time**, so the reader can see the pattern, rather than to let an underpowered test switch the narrative.

**The preregistered structure is now:**

| | Hypotheses | Status | Error control |
|---|---|---|---|
| Confirmatory family | **H1** policy, **H2** mapping, **H4′** cue form | Confirmatory | Holm across the three |
| Validity gate | **H5** feedback vs None | Gate, not a finding | Admits the study to interpretation |
| Exploratory | **H3** interaction | Exploratory | Reported with interval and MDE; no decision attaches |

H3 is estimated and reported — never used to reinterpret H1 or H2, and never described as a finding even if
it reaches p < .05. The analysis script prints this warning at every run (`paper1_analysis.R` §5).

**Why H4′ is in the confirmatory family and the floor contrast is not.** H4′ tests a claim; H5 admits the
study to interpretation. Spending alpha on a precondition would be a category error.

### Why the hypotheses are two-sided

H1 and H2 say "changes" rather than "reduces." That is deliberate, and the justification belongs in the paper
rather than reading as indecision — each direction has a real mechanism.

**H1 could go either way.** Predictive timing buys lead time, which should help. But it also fires *more
often* and *earlier*. More alerts mean more interruption within a three-minute block, and warnings delivered
very early are discounted in the alarm literature because the threat is not yet apparent. Whether the extra
time outweighs the extra burden is genuinely unknown, which is the point of measuring it.

**H2 could go either way too.** A cue on the at-risk limb carries more information. But it sits on a limb that
is *accelerating hard* at exactly the moment it fires, and a tactor on a rapidly moving segment may be less
detectable than one on the comparatively stable torso — mechanical coupling changes and attention is committed
to the movement. A localized cue could therefore be more informative and less perceptible at once.

Both are principled reasons to test two-sided. State them; do not let a reviewer supply them for you.

### What H2 actually contrasts — name this before a reviewer does

**The generic and localized cues differ in two ways at once, not one.** The localized cue is (a) on the limb
at risk and (b) tells you *which* limb. The generic chest cue is neither — it is a single fixed site carrying
no limb information. H2 therefore compares **somatotopic cueing against an undifferentiated torso alert**, not
"body location, holding information constant."

This matters because the obvious third design exists and is published: **Belt & Whistles (CHI 2026) encodes
direction on the torso**, carrying spatial information without somatotopic placement. A reviewer from that
literature will ask why the generic condition was not directional, and the honest answer must be ready.

### DECIDED 2026-09-07 — narrow the claim, name the confound

**H2 is stated as: somatotopic limb cueing versus an undifferentiated torso alert.** The information confound
is declared as a scope limit rather than designed away, and a directional-torso condition becomes the
designated next study.

Three obligations follow, and all three must appear in the manuscript:

1. **Use the narrowed wording consistently** — hypotheses, abstract, results, and discussion. Never claim H2
   isolates anatomical placement with information held constant.
2. **Position against Belt & Whistles in related work, not in limitations.** State that directional torso
   encoding occupies the space between the two conditions, carrying limb information without somatotopic
   placement; that this study's contrast answers the practical question of whether per-limb actuation earns
   its hardware cost; and that separating placement from information requires a dedicated study with proper
   training on the symbolic mapping.
3. **Name the follow-up concretely** — three conditions (generic / directional torso / somatotopic limb) at
   fixed timing. A reviewer who asks "why not directional?" gets an answer that is a research plan, not an
   apology.

Rejected alternatives, recorded:

| Alternative | Why rejected |
|---|---|
| **Add a fifth directional-torso condition** | Mapping becomes 3 levels → 8 conditions, ~64 participants for a complete condition × layout crossing, two further layout variants, and a 2-df interaction. It also collides with the §2 Tactosy-at-torso decision, since directional encoding needs multiple torso actuators and would push the design back toward the vest and its device confound. |
| **Substitute directional torso for the generic condition** | Free in session time and answers Belt & Whistles directly, but bets the headline factor on a subtler contrast likely to null out, discards the developer-facing hardware-cost question, and depends on a learned symbolic mapping that three-minute blocks may not establish — so a null could reflect insufficient training rather than the absence of an effect. |

### Planned anchor contrasts

- **H4:** PB versus the trigger-matched Visual benchmark.
- **H5:** each feedback family versus None, with multiplicity control.

#### H4 — justify the choice of PB, and settle what the visual cue shows

**Why PB and not the winner.** H4 compares only PB to Visual. That is defensible but must be justified *a
priori*, because comparing whichever haptic condition happens to score best would be data-dependent selection
and would invalidate the test. The justification: PB is the maximally informed haptic condition — earliest
trigger and most specific location — so it is the strongest available test of whether haptics can outperform a
timing-matched visual cue. If a weaker haptic condition beats Visual, that is reported descriptively, not as
H4.

**Unresolved: what does the visual cue indicate?** §2 currently requires "a preregistered direction/at-risk-body
indication appropriate to the research comparison," which defers the decision. It cannot stay deferred, because
it determines what H4 means:

| If the visual cue shows the at-risk body part | If it shows only that *something* is imminent |
|---|---|
| Information-matched to **PB**. H4 is a clean **modality** comparison — haptic versus visual, holding timing and information constant. | Information-matched to **PG**. H4 then confounds modality with information, and PB beating it proves little. |

**Choose the information-matched version** unless there is a stated reason not to. Otherwise H4 answers a
question nobody asked.

**What H4 can claim.** Because the Visual cue is predictive and transient, it is *not* what any shipping
headset does. H4 is therefore a controlled modality comparison, never evidence that this system beats current
practice. §16 already forbids the latter phrasing.

#### H5 — a validity gate, not a discovery

H5 is near-certain to hold: warnings beat no warnings. Frame it as a **manipulation and validity check** that
the paradigm is sensitive enough to detect anything at all, not as a finding. A failure here invalidates the
study rather than producing a result.

It also has a structural job. §15 Step 0 makes beating `None` the eligibility condition for entering the
ranking, so H5 is what admits conditions to that ranking. Say so, so its presence in the confirmatory family
is understood as functional rather than padding.

#### DECIDED 2026-09-07 — two confirmatory families, per-condition eligibility computed descriptively

**Confirmatory (enters the multiplicity correction), two contrasts:**

1. Pooled haptic (RG, RB, PG, PB) versus `None`
2. `Visual` versus `None`

**Descriptive (does not enter the correction), five comparisons:** each condition against `None` individually,
computed solely to operate the §15 Step 0 eligibility gate.

The split resolves a genuine tension. The eligibility gate is **per condition** — a condition enters the
ranking only on its own evidence, not its family's — but H5 is near-certain to hold, and spending five
confirmatory contrasts on a foregone conclusion would erode the multiplicity budget that H1–H3 need. Because
§15 is a **decision procedure rather than a hypothesis test**, its inputs do not require confirmatory status.

Report the per-condition comparisons with intervals and label them descriptive. Do not attach significance
claims to them, and do not promote one to confirmatory status afterwards because it looked interesting.

### Experience outcomes

Presence, workload, sickness, and acceptability are secondary outcomes. The paper may claim “preserved,” “not degraded,” or “acceptable” only if a meaningful equivalence/non-inferiority margin was justified before collection and the study was powered for that test. Otherwise report estimates and uncertainty without interpreting non-significance as equivalence.

**Provisional non-inferiority margins.** These feed the §15 ranking gates and must be finalized from pilot
variance before preregistration. State the justification for each, not just the number.

| Instrument | Provisional margin | Basis to confirm at pilot |
|---|---|---|
| IPQ spatial presence | 0.5 scale points (7-point item scale) | Below typical between-condition differences reported in VR presence studies; confirm against pilot SD |
| NASA-TLX overall | 5 points (0–100) | Conventional smallest practically meaningful TLX shift |
| SSQ total | No non-inferiority margin — treat as a **safety stopping criterion**, not a ranking gate | Any condition with elevated sickness is reported and investigated, never traded against a safety benefit |
| CUE acceptability | Tie-break only; no gate | Insufficient validation history to support an equivalence margin |

A margin chosen after seeing outcome data is not a margin. If pilot variance makes a margin unattainable at the
planned sample size, report estimates with intervals and drop the equivalence claim rather than widening the
margin to fit.

### Mechanism outcomes

Report delivered warning coverage, physical lead time, false alerts, minimum clearance, and cue-to-avoidance response. These describe why policies differ. They are not included as ordinary covariates in the primary total-effect model because warning count and delivered lead time are consequences of assigned policy.

## 4. Participants and power

### DESIGN DECIDED 2026-09-25 — N = 36 analysable × 24 opportunities per block

Holm-corrected power, nsim = 200 (`power_analysis.R` §3C, `p1_power_holm.csv`):

| | H1 | H2 | H4′ | H3 | **All three land** |
|---|---|---|---|---|---|
| **36 × 24** | 100% | **89%** | 97% | 57% *(exploratory)* | **88%** |

**Why 36.** §7 requires the **complete 6 × 6 crossing** of condition-order row and layout-order row, so a
full allocation is a multiple of 36. The earlier target of 48 was withdrawn: it divides by 6 — balancing
condition order alone — but not by 36, leaving 12 of the 36 combinations doubled.

**Why 24 and not 18.** At a fixed 36 participants, opportunities are the only remaining lever. 18 gives H2
84%, but puts **all three land at 78%** — roughly one study in five returning with a hole in the
confirmatory set. 24 moves that to 88%.

**Rejected: 72 × 18** (H2 98%, all three 98%). It buys 10 points for **double the recruitment**, about 72
extra participant-hours.

**Recruitment rule — preregister it.** Recruit above 36 to absorb exclusions; analyse the first **36
complete balanced sets**. Decide this now, not after seeing dropout.

**What it costs.** 24 × 6 = **144 opportunities per participant** against the authored 72. A candidate
24-event schedule passes the geometry audit **24/24** (balance O1×4 · O2×8 · O3×8 · O4×4, 13 s cadence, no
same-limb overlaps) with the last event closing at **313 s** — so the block grows from 180 s to **~320 s**
and task time from ~18 to **~33 min**.

> ⚠ **Re-read the fatigue section of `SAFETY_PROTOCOL.md` against the longer block.** 33 minutes of
> repeated reaching and stepping is a real musculoskeletal load. Paper 2 has a shoulder-discomfort
> stopping rule for exactly this reason and Paper 1 is now in similar territory.

**Still to author: twelve more opportunity events** (once — `LayoutVariants` derives L2–L6 from L1), then
re-run the geometry audit over all six layouts.

---

### ⚠ Superseded — why N = 48 was withdrawn (retained for the record)

> **A counterbalancing confound was found and fixed on 2026-09-25, and it invalidates the target below.**
>
> `SessionPlan.For` keyed BOTH the condition order and the layout order to `participantId`, so the two
> were perfectly correlated. Measured over 72 simulated participants: **6 distinct plans instead of 36**,
> participants 0/6/12/18/… identical, and **12 of the 36 condition × layout cells never occurred** — each
> condition met one "home" layout three times as often as any other, and two layouts not at all.
>
> **Condition was confounded with layout.** `paper1_analysis.R` fits `(1 | layout)` assuming layout is a
> decorrelated nuisance factor; a layout-difficulty effect would have loaded onto the condition estimates
> and biased H1, H2 and H4'. Fixed by indexing the layout row on `participantId / 6`; re-measured at
> 12 per cell across all 36. Guarded by three tests in `SessionPlanTests`.
>
> **Consequence for N.** §7 requires the complete 6 × 6 crossing, so complete allocations are multiples of
> **36** — 36, 72, 108. **48 divides by 6 but not by 36**, so it balances condition order alone and leaves
> some condition-order × layout-order combinations over-represented. §4 is explicit that an incomplete
> crossing is to be rounded **up**, not accepted. `SessionPlan.IsCompleteAllocation()` now enforces this.
>
> Candidates under measurement: **36 × 24** and **72 × 18**. Do not collect against the figure below.

### ~~TARGET SET 2026-09-23 — N = 48 analysable × 18 opportunities per block~~ (withdrawn)

Chosen from the measured power grid (`power_analysis.R`, nsim = 120), sizing on **H2, the weakest
confirmatory test** — not on H3, which is exploratory and constrains nothing.

| | H1 | H2 | H3 | H4′ |
|---|---|---|---|---|
| **N = 48 × 18** | 100% | **93%** | 57% *(exploratory)* | **96%** |

Alternatives considered and rejected: **N = 36 × 20** (H2 82%) clears by two points uncorrected, with no
room for the Holm caveat below. **N = 48 × 24** (H2 98%) is better powered but needs 144 opportunities per
participant — 2× the current session.

- **Analysable target: 48.** Recruit to **54** to absorb exclusions; 48 is divisible by 6, so the Williams
  square balances exactly (`SessionPlan`). 54 does not — over-recruit, then analyse the first 48 complete
  balanced sets, and preregister that rule.
- **H3 is not a sizing constraint.** It reaches 80% nowhere in the grid (ceiling 76% at N = 60 × 24). What
  the design *can* resolve is an interaction of **OR ≈ 1.57**; the assumed truth is OR 1.35. State that as
  what the study rules out, not as an expectation.
- **Holm correction MEASURED 2026-09-23** (nsim = 200): at N = 48 × 18 the family-wise cost to H2 is about
  one point, **88.5% → 87.5%**, and all three confirmatory hypotheses land together in **85.5%** of studies.
  The rejected alternative N = 36 × 20 falls to **79.0%** under correction — below threshold — which
  confirms the choice rather than merely asserting it. See `p1_power_holm.csv`.

### ⚠ BLOCKING — the engine cannot deliver 18 opportunities yet

`OpportunityScheduler.Layout1()` defines **12** hand-authored events (E1–E12) on a **180 s** block at ~13 s
spacing, balanced by obstacle (O2×4 · O3×4 · O1×2 · O4×2) and by limb. **Six more events per layout must be
authored before this target is real**, and it is not a constant change:

1. **Block duration grows.** 18 events at 13 s spacing needs **234 s**, against the current 180 s.
   Compressing the gap to 10 s fits 180 s exactly but leaves no margin over the 6 s attribution window —
   adjacent windows would nearly touch. **Plan on a ~240 s block and measure the real session length at
   pilot** (it is documented nowhere, which is its own blocker).
2. **Obstacle balance must be preserved.** A balanced 18 is O2×6 · O3×6 · O1×3 · O4×3.
3. **O2 is already fixed — but it is the tightest hazard.** O2 was re-sited to (0.95, 0.70, 0.60) on 2026-09-14 and the schedule re-audited **12/12 pass** (approach 0.62 m for O2 events, 0.78 m O3, 1.05 m O1, 1.70 m O4). The residual risk is a SENSITIVITY, not a failure: under a ±0.6 m standing-position sweep the O2 opportunities fall below the 0.40 m floor at 22% of positions (was 59%), comparable to O3's 13%. New O2 events inherit that
   sensitivity, so prefer O3/O1 for the added events where balance allows.
4. **Every new event needs ≥ 0.40 m clear approach** (`OracleParams.MinApproachDistanceForValidTiming`), and
   **the geometry audit must be re-run** over all 18 before collection.
5. **Authoring happens ONCE.** `LayoutVariants` derives L2–L6 from L1 by rigid transform, so only
   `OpportunitySchedules.Layout1()` and `Layout1Stimuli.All()` change. The **audit** must still run over
   all six layouts, because rotation changes each limb's approach distance.

Until that work lands, `SessionPlan`/`OpportunityScheduler` must **fail loudly** rather than silently
running 12 while the preregistration says 18 — a silent mismatch would put the wrong denominator in the
paper and nothing would catch it. See `OpportunityScheduler.TargetOpportunitiesPerBlock`.

### What the power simulation must reproduce

- six within-subject blocks;
- **18** planned opportunities per block;
- participant-level random intercepts and factor slopes;
- event type/target limb;
- period and layout effects;
- plausible invalid opportunities and early stops;
- overdispersion if a count sensitivity model is retained; and
- the smallest interaction and **PB-versus-PBC (H4′)** effect worth detecting.

**Re-run from pilot estimates before freezing.** Every effect size in §1 of the power script is an
assumption, `EFF_PBC = 0.40` most of all — it is a modest guess at an effect Valkov and Linsen measured on
a different task with a different outcome measure.

### Screening and recorded participant characteristics — ADDED 2026-09-14

§8 lists "consent, screening" without specifying what is screened or what is recorded. Two of these plausibly
moderate the primary outcome and one of them is structurally entangled with the design.

| Variable | Why it matters | Requirement |
|---|---|---|
| **Handedness** | Target limb is left/right hand and foot. A right-dominant sample will show systematically different avoidance for left- versus right-limb opportunities, and the §7 allocation does not balance it | **Record. Report the distribution. Confirm the opportunity schedule is balanced across left and right target limbs**, so handedness cannot align with condition |
| **Prior VR experience** | Predicts both comfort and collision behaviour; naive users are more cautious and more sickness-prone | Record on an ordinal scale. Report. Preregister as descriptive, not as a model covariate |
| **Height and limb-segment lengths** | §5 requires capsule radii "from body-segment data **and participant measurement**" | Measure and record per participant, or state explicitly that a population model was used instead and why |
| **Exclusion criteria** | Currently unstated | Preregister: vestibular disorder, uncorrected vision, skin conditions contraindicating vibrotactile contact, pregnancy if the ethics board requires it, and any condition affecting gait or reaching |
| **SSQ stopping threshold** | §3 designates SSQ a "safety stopping criterion" with no number attached | Preregister a value and the action it triggers — stop the session, and whether partial data is retained |

### Provisional planning figure — for ethics and budget only

Simulation determines the final number, but ethics submissions, recruitment budgets, and lab scheduling cannot
wait for it. Use a **provisional planning target of 36 analyzable participants, with a maximum recruitment
target of 48**, and label it explicitly as provisional in the ethics application.

The rationale is allocation, not power: §7 requires a complete 6 × 6 crossing of condition-order row and
layout-order row, so the natural complete allocations are 36 and 72. A target of 36 is the smallest complete
allocation; 48 covers exclusions and dropouts while remaining schedulable.

If the power simulation returns a requirement above 36 analyzable participants, round **up to 72** rather than
accepting an incomplete crossing, and revise the ethics application before collection. Record the simulated
power at the final number in the preregistration. Do not report 36 as a powered target.

## 5. Task and opportunities

Participants play the reach/dodge task while five registered virtual hazard volumes remain visually hidden. Physical props are not placed in the movement volume.

### Self-representation — what the participant can see of their own body

> #### DECIDED 2026-09-14 — ankle markers ON, identical in every condition
>
> The participant sees a small neutral marker at each tracked **ankle**. Hands are already visible as held
> controller models (§11). No sternum marker. No full-body avatar.
>
> **Rationale 1 — exproprioceptive symmetry between target limbs.** With controllers in both hands, hands are
> visible and feet are not. A hand-targeted opportunity would then be avoidable with visual self-knowledge
> while a foot-targeted one would not. Target limb is a nuisance factor, not a manipulated one, and this
> asymmetry is a pure artifact of the hand configuration decided on 2026-09-11. Ankle markers remove it.
>
> **Rationale 2 — it narrows the largest controllable real-vs-virtual gap.** Occluding the limbs *in the real
> world* already increases toe clearance while leaving baseline gait unchanged; VR without a limb
> representation amplifies the same adaptation. A meaningful share of the "VR behaves differently" effect is
> missing limb vision rather than virtuality, and limb vision is ours to set. See
> `docs/VIRTUAL_HAZARD_VALIDITY.md` §3.5.
>
> **Constraints that make this safe to add:**
> - **Identical in all six conditions, including None.** The self-representation is never driven by condition.
>   One that varied by condition would be a second uncontrolled feedback channel.
> - **Frozen before data collection**, like any other apparatus parameter.
> - **Marker diameter stays below the smallest limb contact radius.** The marker indicates where the limb *is*.
>   Drawn at the capsule radius it would disclose the collision geometry and function as a permanent proximity
>   aid. Current value 0.07 m against a 0.08 m minimum contact radius; re-check when §14.22 freezes the radii.
> - **Marker colour is neutral**, never the green-to-red of the Visual condition glow, so a marker can never
>   read as a hazard cue.
>
> **Acknowledged risk, to be reported.** Limb visibility plausibly *moderates* H2: a body-localized cue names
> a limb, and acting on it requires knowing where that limb is. Visible ankles may therefore raise or lower the
> localization benefit. This is a constant of the apparatus, so it cannot confound the within-subject contrasts,
> but it does bound their generality. State in the manuscript that the localization effect is estimated under a
> minimal visible self-representation, and name the no-avatar case as untested.
>
> **Implementation.** `Runtime/SelfRepresentation.cs`. Logs its configuration at startup so a session that
> silently ran a different self-representation cannot pass unnoticed.

Each condition block contains 12 scripted opportunities. Every opportunity has, before the block starts:

- an opportunity ID;
- target limb;
- target hazard ID;
- event type;
- planned opening and closing time;
- lure/trajectory specification;
- validity requirements; and
- scoring rule.

### Primary outcome unit

The primary unit is the **opportunity**, not the block-level collision count.

```text
violation = 1
```

only when the designated target limb enters the designated target hazard within that opportunity's valid attribution window. Repeated penetration within the same opportunity remains one primary event. Other contacts are retained as secondary/unattributed safety events.

An opportunity is included in the denominator only if it was presented and met preregistered tracking/task validity requirements. Early termination, tracking loss, unspawned stimuli, or operator stops cannot leave the denominator fixed at 12.

### Secondary physical outcomes

- number of entry episodes;
- penetration depth and duration;
- minimum target-limb clearance;
- near miss under a frozen definition;
- unattributed contacts; and
- task score/performance.

**Task score is not an independent outcome.** The in-experiment score exists to induce engagement, which the
virtual-hazard literature identifies as the moderator of realistic avoidance behavior. Because the −3 hazard
penalty is computed from the primary outcome, total score is algebraically dependent on it and must never be
analyzed as if it were separate evidence.

Report instead:

- the **task components only** (orbs collected, projectiles avoided) as the engagement/performance measure; and
- total score, if at all, as a participant-facing motivational device described in the procedure.

A condition that reduces violations will mechanically show a better total score. That is not a second finding.

Collision geometry must use a preregistered body-contact radius or capsule model rather than a mathematical point joint unless the point-joint limitation is the explicit construct.

### Contact geometry — sized against whichever tracker the pilot selects

**The tracking system is chosen by paired pilot comparison (§11, revised 2026-09-08).** Contact geometry
therefore has to be specified conditionally, and finalized once the tracker is selected.

**Under Vicon**, the prototype 0.03 m band sits roughly **two orders of magnitude** above tracking error, so
the primary outcome is not noise-limited. The capsule model stands on its own merits — modelling limb volume
because limbs *have* volume — rather than as error absorption.

**Under commodity trackers**, that protection is gone. If positional error is on the order of 1–2 cm against a
3 cm band, a large fraction of the primary outcome is measurement noise — violations recorded that did not
occur, and real ones missed. The capsule model must then be sized so the **effective contact band comfortably
exceeds the measured 95th-percentile tracking error**, and the band becomes partly a noise tolerance rather
than purely a body model.

**Required either way, and preregistered:**

1. **Freeze the limb-volume model from anthropometry.** Per-segment capsule radii from body-segment data and
   participant measurement, not convenience. Record the values and their source. The point-joint default is
   not acceptable for confirmatory analysis.
2. **Verify accuracy in your own volume, during vigorous movement** — not quiet standing. Sub-millimetre is a
   specification, not a guarantee, and inside-out figures degrade with speed and occlusion.
3. **Report per-condition tracking-quality metrics** and test for condition differences before interpreting
   the primary outcome (see the bias note below).
4. **Do not run confirmatory collection at the prototype 0.03 m band** without the sizing check above.

**Direction of the bias, and why it is not symmetric.** Positional noise alone is roughly non-differential —
the tracker does not know which warning the participant received — so its main effect would be attenuation.
But **velocity** noise is not symmetric: the predictive policy depends on closing speed while proximity
depends only on distance, so it degrades one arm of H1 and not the other. That is why delivered lead-time
jitter is a named selection criterion in §11 rather than an afterthought.

**Why this decision materially improves the design.** Positional error propagates into velocity error
amplified by roughly `1/(Δt·√2)` — about **70× at 100 Hz**. The **predictive** policy depends on closing
speed; the **proximity** policy depends only on distance. Inside-out velocity noise would therefore have
degraded one arm of H1 and not the other.

That is a **directional confound, not symmetric attenuation**. It would have biased H1 against prediction with
no way to separate "predictive timing does not help" from "our velocity estimates were too noisy to predict
with." Vicon removes the asymmetry, which is why the original design rationale selected it.

**The tracking risk has moved rather than vanished.** It is now marker occlusion, marker
swapping/mislabelling under fast motion, and physical marker detachment — see §11.

## 6. Warning policies

### Proximity policy

Fire when the designated at-risk limb crosses a preregistered distance boundary while approaching its designated hazard.

### Predictive policy

Fire when predicted time to the first intersection between the designated limb trajectory and designated hazard falls below the preregistered TTC threshold.

### Frozen policy parameters — RECORDED 2026-09-14

**These were previously defined only in `Core/CollisionOracle.cs` and appeared nowhere in this document.**
The two numbers that define the primary manipulated factor cannot live only in source code.

| Symbol | Meaning | Current value |
|---|---|---|
| `D` | Proximity trigger distance | **0.30 m** (unchanged) |
| `T` | Predictive TTC threshold | **1.00 s** — raised from 0.50 on 2026-09-14 |
| `PredictiveReleaseMargin` | TTC-axis hysteresis | **0.40 s** — held at 40% of `T` |
| `ReleaseDistance` | Re-arm distance for the edge trigger | see `OracleParams` — record before freeze |
| `NearMissDistance` | Near-miss band (§5 "frozen definition") | **0.12 m** |
| `ContactDistance` | Surface band counted as contact | **0.03 m** — superseded in effect by the capsule radii (§5) |

#### ⚠ PRIOR WORK RAN THIS COMPARISON AND GOT THE OPPOSITE RESULT — FOUND 2026-09-23

**Valkov & Linsen, IEEE VR 2019** (DOI `10.1109/vr.2019.8798036`, N = 40) compared fixed-distance
against speed-scaled triggering of vibrotactile collision warning in VR. Firing when `d < u·t`
is the same rule as firing when `TTC < T`, so **their speed control is Paper 1's predictive
policy.** Their result: speed control produced **significantly more collisions** (Z = −2.23,
p = 0.024) and smaller safety distances (F(1,39) = 17.65, p < 0.01). They rejected an H1 that
reads almost identically to ours.

**This must be cited and distinguished in Paper 1.** It is not fatal — their intensity was a
*continuous* function of distance and speed, so slowing down turned the warning off and
participants crept forward modulating it. The trigger policy was confounded with the display
mapping. Paper 1's edge-triggered single-alert design removes that loop by construction, and
holds cue form constant across the Policy factor, which theirs did not.

**It also anchors both parameters.** Their `tmin` = 600 ms / `tmax` = 1.6 s brackets `T` = 1.0 s;
their `dmin` = 60 cm follows `v × t_stop` for walking, and the same method with a reach-appropriate
stopping time lands at 0.25–0.32 m, containing `D` = 0.30 m.

Full analysis, the comparison table, and the revised motivation: **`docs/PAPER1_PARAMETER_JUSTIFICATION.md` §5B**.

#### WHAT THESE VALUES CLAIM — RECORDED 2026-09-23

**`D` and `T` are operationalizations, not findings.** We do not claim they are optimal. They
are representative operating points of two policy *classes* — fire-on-distance and
fire-on-forecast — chosen so the timing contrast holds in a consistent direction across the
speeds this task produces. **The manipulated variable is the policy class, not the value.**

This distinction must appear in the paper. Left unstated, a reviewer assumes the stronger
claim ("0.30 m is the right distance") and holds the paper to a burden it never took on.
Paste-ready Methods text, the derivation table below written up for publication, the
automotive-FCW citations that establish the dichotomy is inherited rather than invented, and
the Limitations paragraph that pre-empts the single fair criticism are all in
**`docs/PAPER1_PARAMETER_JUSTIFICATION.md`**.

Two analyses discharge the rest of the burden, both implemented and dry-run 2026-09-23:

- **`paper1_analysis.R` §3B** reconstructs both policies' trigger times across
  `D` ∈ [0.20, 0.45] m × `T` ∈ [0.70, 1.50] s from the counterfactual probe, showing the
  manipulation's *direction* does not depend on the frozen pair. It validates its own
  constant-speed assumption before using it. **This is manipulation robustness, never
  effect-size generality** — no participant experienced those settings.
- **`paper1_analysis.R` §6B** replaces the categorical `Policy` factor with the continuous
  lead time actually delivered, restating the finding as a curve over warning time that a
  reader can port to their own system. Observational and speed-confounded by construction;
  reported as such, and it does not supersede §9's confirmatory model.

### RESOLVED IN PART 2026-09-14 — the timing factor could invert; `T` raised to 1.00 s

**Parameters are fixed. The opportunity-geometry requirement this exposed is still open (gate §14.27).**

The proximity policy fires at a fixed **distance** `D`. The predictive policy fires at a fixed **time** `T`,
which for a limb closing at speed `v` corresponds to a distance `v · T`. Therefore:

```text
predictive fires EARLIER than proximity  ⇔  v · T > D  ⇔  v > D/T
crossover speed  v* = D / T = 0.30 / 0.50 = 0.60 m/s
```

**Above 0.60 m/s the predictive policy leads. Below 0.60 m/s it LAGS — the manipulation runs backwards.**
At exactly 0.60 m/s the two policies fire at the same instant and the factor has no manipulation at all.

At `T` = 0.50 s the crossover sat at **0.60 m/s** — inside the range of real limb speeds — so on slow
approaches the "predictive" condition fired *later* than the "proximity" condition. H1 would then have
averaged over opportunities whose manipulation ran in opposite directions, and a null would have been
uninterpretable: "predictive timing does not help" and "the predictive cue was not actually earlier" are
different findings. It also leaks into H3, since target limb correlates with typical closing speed.

#### What simulation showed — and how it corrected the first diagnosis

Driving the **real `CollisionOracle` and `ConditionManager`** through minimum-jerk reaches (the standard model
of human reaching) at 90 Hz, across movement times of 0.4–1.4 s, gave a different and more useful answer than
the closing-speed arithmetic above.

**The dominant driver is not speed. It is clear approach distance.** When a limb begins its movement closer to
the hazard than `D`, the proximity cue fires at *movement onset* and the predictive cue cannot lead it at any
speed — there is no room left to be early in. Raising `T` shrinks that dead zone but can never remove it.

Measured minimum clear approach distance for predictive to lead proximity by ≥ 50 ms at **every** movement
time tested:

| `D` | `T` | Minimum approach distance |
|---|---|---|
| 0.30 m | 0.50 s *(old)* | **0.55 m** |
| 0.30 m | 0.80 s | 0.45 m |
| **0.30 m** | **1.00 s** *(adopted)* | **0.40 m** |
| 0.25 m | 1.00 s | 0.35 m |

At `D` = 0.30 / `T` = 0.50, an opportunity offering 0.40 m of approach **inverted** at slow movement times.
At `D` = 0.30 / `T` = 1.00 the same geometry leads by 0.07–0.12 s, and 0.60 m of approach leads by 0.13–0.30 s.

**Why `D` stays at 0.30 m.** Dropping it to 0.25 m buys only 0.05 m of dead-zone reduction while making the
proximity cue less actionable. The reactive condition has to remain a plausible warning rather than a token
one, or H1 degenerates into "useful versus useless" rather than a comparison of warning policies.

**Why `T` = 1.00 and not 0.80.** Marginally better floor (0.40 m versus 0.45 m) and roughly 25% more lead
throughout. The cost is that predictive fires earlier and more often — which §3 already treats as part of the
*total effect of a policy* and §15 Step 3 measures as alert burden. It is a real cost, not a hidden one.

#### ⚠ STILL OPEN — opportunity geometry must be audited (gate §14.27)

**The parameter change is necessary but not sufficient.** Every scheduled opportunity must give the target
limb enough clear approach distance to its target hazard:

- **Hard floor: 0.40 m** (`OracleParams.MinApproachDistanceForValidTiming`). Below this the manipulation is
  unreliable or reversed and the opportunity contributes noise to H1.
- **Design target: 0.60 m** (`OracleParams.RecommendedApproachDistance`), for margin.

#### AUDIT RUN 2026-09-14 — 4 of 12 opportunities fail, and the cause is a misplaced hazard

Full report: `docs/OPPORTUNITY_GEOMETRY_AUDIT.md`. Audited with the real `Obstacle` math, the real
`OpportunitySchedules.Layout1()` schedule and the real `Layout1Stimuli` positions.

**E1, E6, E7 and E12 fail — every opportunity targeting O2.** O2's surface sits **0.04 m from the right hand
at a neutral standing posture**, so those four opportunities offer essentially no approach distance.

**The consequence exceeds the timing manipulation.** Against the provisional contact model (hand radius
0.08 m) the right hand is **already in contact with O2 at rest**. Those four opportunities would record
`violation = 1` from posture alone, regardless of condition — a ceiling on a third of the primary outcome,
contaminating the `None` baseline and shrinking every condition contrast. A block would report
`PRIMARY: 4/12 violated (33%)`, which passes gate §14.10 and looks entirely healthy.

**✅ FIXED 2026-09-14 — O2 centre moved from (0.40, 0.70, 0.00) to (0.95, 0.70, 0.60)** in
`Assets/Scenes/Dryrun.unity`, size unchanged. Re-audited: **12/12 pass**, right-hand approach 0.62 m,
effective clearance at rest 0.54 m, pillar still within 0.17 m of the reach paths to its three orbs. Layouts
L2–L6 and LP are rigid isometries of L1, so correcting the authored L1 baseline corrects all seven. Residual:
under a ±0.6 m standing-position sweep the O2 opportunities sit below the floor for 22% of positions (down
from 59%), comparable to O3's 13% — preregister it as per-trial variation.

O1, O3, O4a and O4b all pass. Two secondary items are recorded in the audit: O3 drops below the floor for 13%
of standing positions under a ±0.6 m sweep, and **O2 is over-subscribed** — four of twelve opportunities on
one hazard, all on the right hand, which compounds the unrecorded-handedness gap in §4.

**⚠ The hazard geometry has no single source of truth.** O1–O3 exist only as transforms in `Dryrun.unity`;
O4a/O4b only as `SerializeField` defaults. Nothing in Core defines, versions or tests the volumes that produce
the primary outcome — which is why a hazard could sit 4 cm from a participant's hand with nothing complaining.
Move the authored L1 geometry into Core as data, have `SceneObstacles` build from or assert against it, then
add the EditMode test that every opportunity clears `MinApproachDistanceForValidTiming`. In that order: a test
carrying its own copy of the geometry would create a second source of truth for the very thing at fault.

For any opportunity still failing after the geometry is corrected, either move the lure start position or
exclude it from H1 under a preregistered rule. **An opportunity that fails this test is not a weak trial — it
is a trial whose manipulation may have run backwards.**

Regression tests lock all of this in: `Tests/EditMode/PolicyTimingSeparationTests.cs` pins the ordering at the
documented floor, pins the crossover ceiling at 0.50 m/s, keeps the release margin proportional to `T`, and
deliberately **documents the inversion** when a limb starts inside `D` so the failure mode cannot quietly
reappear.

#### Still required before confirmatory collection

**1. ✅ IMPLEMENTED 2026-09-14 — counterfactual dual-policy trigger logging.**
`Core/PolicyTriggerProbe.cs` evaluates **both** policies against the designated target limb × target hazard on
every opportunity, in **every condition including `None`**, and folds the result into the primary row as
`OpportunityOutcome.Timing`. It is a pure observer: it emits no feedback and never touches the condition logic.

Eight columns added to `opportunities.csv`:

| Column | Meaning |
|---|---|
| `trigger_prox_s` / `trigger_pred_s` | when each policy **would have** fired (NA if it never would) |
| `policy_lead_s` | `prox − pred`. **> 0 means the predictive policy genuinely led**, which is what H1 assumes |
| `timing_inverted` | 1 when the predictive cue would have arrived *later* than the proximity cue |
| `delivered_trigger_s` | whichever of the two the assigned condition actually used (NA for `None`) |
| `approach_at_onset_m` | clear limb→hazard distance at the first open frame — the **per-trial, measured** version of the geometry audit |
| `closing_speed_prox_ms` / `closing_speed_pred_ms` | closing speed at each trigger |

`PolicyTriggerSummary` (Core, tested) rolls this up per block and `SessionRunner` prints it as a `TIMING:` line,
warning when anything is inverted or under the approach floor — so an operator sees it during the session
rather than discovering it in analysis.

**Two things this surfaced that are worth recording.**

*The proximity trigger is usually the FASTER of the two.* On a minimum-jerk reach the predictive cue fires
early while the limb is still accelerating; the proximity cue fires nearer the hazard, at or past peak speed.
The intuition that the later cue catches a slowing limb is wrong, which is why closing speed is logged at each
trigger rather than assumed.

*✅ The live cue now uses the designated pair too (fixed 2026-09-14 — see "Required corrections" below).*
`ConditionManager` and `PolicyTriggerProbe` therefore evaluate the same limb×hazard pair with the same rules, so
`trigger_*_s` is directly comparable to the logged alert times rather than being an idealization of them.

**2. Preregister a manipulation check with a pass criterion**, reported before any H1 result:

| Check | Criterion |
|---|---|
| Median counterfactual lead-time difference (predictive − proximity) | **> 0** by a preregistered minimum — set from pilot, not chosen afterwards |
| Proportion of valid opportunities where the predictive trigger was **later** than the proximity trigger | Reported, and preregistered as a threshold above which H1 is reported as **inconclusive for timing** |
| Distribution of `closing_speed_at_trigger`, per target limb | Reported with `v*` marked on it |

**3. Confirm `D` and `T` against the *measured* pilot distribution.** The values above come from simulated
minimum-jerk reaches, which are a good model of reaching and a weaker model of stepping and torso dodging.
Re-run the check against real pilot movement before freezing. Do not tune to maximise the H1 effect; tune so
the manipulation is consistent in **sign**, and say so.

**4. If the speed distribution straddles `v*` too widely to fix by tuning**, the honest options are to report
H1 conditioned on speed regime as a preregistered stratification, or to state that the policies were not
reliably separable in delivered timing for this task. Both are publishable. Discovering it after collection is
not.

### Required corrections

- ✅ **DONE 2026-09-14.** Use the target limb × target hazard pair; do not combine “closing toward any
  obstacle” with the current nearest obstacle.

  `ConditionManager` now takes the block schedule and cues **only** the designated target limb × target hazard
  of an open opportunity, read through `CollisionOracle.ReadAgainst` so the (obstacle, distance, TTC, closing)
  tuple is self-consistent by construction. Two distinct defects were fixed:

  1. **The reactive rule mixed two obstacles.** It combined `r.Closing` — closing toward *any* obstacle — with
     `r.MinDistance`, the distance to the *nearest* one. A limb closing on a distant hazard while merely sitting
     near a different one fired an alert about the hazard it was retreating from.
  2. **An undesignated alert could swallow the designated one.** The global re-fire debounce
     (`RefireDebounceSeconds` = 1.0 s) is shared across limbs and hazards, so a cue about an irrelevant hazard
     consumed the window and the designated warning was never delivered — the manipulation silently failed on
     that opportunity, with nothing in the data to say so. **This was the more serious of the two.**

  *Consequence to expect:* **alert counts fall**, because cues about undesignated hazards no longer occur.
  That is intended. Alert burden is a §15 Step 3 ranking input, so the numbers are not comparable to any run
  before this date.

  *Consequence accepted, and recorded:* the system no longer warns when a participant approaches a hazard that
  carries no scheduled opportunity. In a deployed system that would be a defect; here it is correct, because the
  warning system under study is an idealized oracle (§1) and hazards are **invisible** (§5), so a suppressed
  cue for an undesignated hazard is undetectable to the participant. Those contacts are still recorded as
  unattributed safety events.

  A schedule-free path is retained for bench tools and unit tests. It is hazard-agnostic but **also** fixed for
  defect 1, so no code path keeps the old inconsistency.
- Compute TTC as time to geometric intersection, not radial distance divided by closing speed when those differ.
- Make velocity filtering time-constant/frame-rate aware.
- Debounce per limb–hazard engagement, not globally across all limbs.
- Expire/reset alert and avoidance records when their engagement closes.
- Freeze prediction parameters before confirmatory collection.

### Policy interpretation

The primary analysis estimates the total effect of each policy as implemented. Do not adjust the primary model for `alerts`, `lead_time`, or `false_alarms`; those are post-assignment mediators. Report them as secondary policy-performance outcomes.

If the scientific goal changes to a pure warning-time experiment with identical alert coverage, that is Paper 2's role and requires controlled lead-time assignment rather than these autonomous policies.

## 7. Counterbalancing and layouts

Use a six-sequence Williams design for condition order and an independent six-sequence layout order. Allocate participants across the complete 6 × 6 crossing of condition-order row and layout-order row.

Across each complete set of 36 participants this provides:

- balanced condition period;
- balanced layout period; and
- balanced condition × layout pairing.

Power first determines the minimum sample; then round upward to a complete or near-complete crossed allocation. For any incomplete final allocation, use constrained minimization before enrollment and audit the achieved pair counts without inspecting outcomes.

Do not derive layout from participant ID and condition position using a simple cyclic shift; that produces structural condition–layout confounding.

The six test layouts may be isometries if required to control geometric difficulty, but they are nuisance levels, not evidence of generalization to six different rooms. Practice uses a seventh, unrepeated layout.

## 8. Session flow

1. Consent, screening, and baseline sickness/comfort.
2. Tracking and physical timing validation.
3. Per-site haptic detection/salience calibration.
4. Cue tour covering all sites and the visual benchmark.
5. Practice in an unrepeated layout, with no measured condition data.
6. Six counterbalanced condition blocks with mandatory breaks.
7. Block questionnaires before deferred score/collision summary.
8. Post-session sickness, comfort, and adverse-event check.
9. **Post-session expectancy probe** (added 2026-09-14) — see below.

### Participant instruction script — ADDED 2026-09-14

**Read verbatim, once, after the cue tour and before practice.** Until now the only instruction script lived in
the superseded `docs/STUDY_DESIGN_V2.md`, it was never carried into this authority, and **it said nothing about
the vibration cue at all** — so every participant would have invented their own rule for what to do when one
arrived, with no record of what that rule was.

> **1 — The task.** *"You'll see glowing orbs appear around you. Reach out and touch them — each one is worth
> 1 point. Things will also be thrown at you. Get out of the way — each hit costs you 1 point."*
>
> **2 — The hazards.** *"Invisible hazard zones stand in the play space, like furniture you can't see. Move as
> though they were real. Each time any part of your body enters one, you lose 3 points. You won't feel or hear
> anything when it happens — you'll see the total at the end of the round."*
>
> **3 — The cue.** *"During some rounds you'll feel a short vibration. It means a part of your body is heading
> into one of the zones. In some rounds you'll feel it on the body part that's at risk. In other rounds you'll
> feel it on your chest, which tells you that something is at risk but not which part. Either way: move clear
> and keep playing. You usually won't need to give up the orb — adjusting your reach is enough."*
>
> **4 — The no-warning rounds.** *"Some rounds have no vibration at all. The zones are still there, and they
> still cost points."*
>
> **Before every block, verbatim and identical:** *"Same as before: collect the orbs, avoid what's thrown at
> you, and stay out of the zones."*

#### Why it is worded this way

**§3 exists because H2 depends on it.** H2 asks whether naming the at-risk limb beats an undifferentiated
torso alert. That question is only meaningful if participants are meant to act on limb identity. A participant
whose private strategy is "vibration → freeze" avoids the hazard without using the limb information at all,
so localized and generic cues become behaviourally identical and **H2 nulls out for a reason that has nothing
to do with the cue** — indistinguishable, in the data, from localization genuinely not helping.

**It states what the cue means, not what movement to make.** "Move clear and keep playing" gives the goal;
"move your left hand 20 cm to the left" would drill a stimulus–response mapping and turn the study into a test
of training compliance rather than of whether the cue is useful.

**It tells them the cue differs between rounds.** They will discover this anyway during the cue tour, and an
unexplained change reads as a malfunction. Saying it up front prevents confusion being scored as cue
ineffectiveness. It does not reveal the hypothesis: nothing says which arrangement is expected to work better.

**It names the no-warning rounds.** Otherwise a `None` block reads as broken equipment, and a participant who
believes the system has failed behaves differently from one who knows there is simply no warning.

**The point values do the arbitration.** An orb is +1 and a hazard entry is −3, so abandoning a reach to stay
clear is the correct play when the two genuinely conflict — without the script having to prescribe it. Keep
§3's "you usually won't need to give up the orb" honest: if piloting shows the geometry often forces the
choice, the opportunity design is too aggressive, not the instruction.

#### Delivery requirements

- Read **verbatim** from a printed card. Paraphrasing between participants is an uncontrolled variable on the
  factor that determines what the primary outcome means.
- Read **once**, after the cue tour (so the words attach to sensations already felt) and before practice.
- The per-block reminder is **fixed and identical** across conditions. Never add emphasis in a warning block
  and not in `None`.
- Log that the script was delivered, and note any participant question and the answer given, so deviations are
  recoverable.
- Answer questions about *what the cue means*; do **not** answer questions about which cue is better, or
  whether they are being scored on collisions specifically. Record any such question — it is expectancy data
  (see the expectancy probe below).

### Demand characteristics and expectancy — ADDED 2026-09-14

Every participant experiences all six conditions, and the cue tour names and demonstrates each one. Most will
form a hypothesis, and the most natural hypothesis is the study's own. This is a standard within-subject
vulnerability and it is currently unaddressed anywhere in the design; a reviewer will raise it.

The manipulation cannot be blinded — the participant necessarily feels which cue they are receiving. What can
be done, and is required:

- **Record the guess.** A short post-session free-text item ("What do you think this study was testing?") plus a
  forced-choice item asking which cue they believe was intended to work best. Coded by two raters blind to
  condition order.
- **Report the distribution** in the manuscript rather than only in a supplement.
- **Preregister it as descriptive**, not as a covariate. Adding a post-hoc expectancy adjustment to the primary
  model would be a post-assignment adjustment of exactly the kind §6 forbids elsewhere.
- **The deferred-score design already helps** — hazard penalties stay silent within a block (§8), so
  participants cannot track their own violation rate condition by condition and confirm a hypothesis mid-session.
  Say so explicitly; it is a deliberate safeguard, not an incidental choice.

### Engagement — preregistered pass criterion, ADDED 2026-09-14

§14.10 requires the pilot to "establish engagement" without saying what would count. Engagement is not a
nicety here: it is the documented moderator of whether participants treat virtual hazards as real constraints
at all, with collision rates differing by a factor of five between engaging and boring tasks
(`docs/VIRTUAL_HAZARD_VALIDITY.md` §6.3). An unengaged sample does not produce a weak version of this study;
it produces a different one.

Freeze these at pilot and report them as a manipulation check alongside the timing check in §6:

| Check | Criterion |
|---|---|
| Task components completed (orbs collected, projectiles avoided) per block | Above a preregistered floor; a participant who stops playing the task is not avoiding hazards either |
| Violation rate in `None` | Off the floor and off the ceiling (§14.10) — the direct evidence that hazards constrain behaviour |
| Self-reported avoidance intent | Reported per condition; a decline across blocks is a disengagement signal |
| Trend in task components across the six blocks | Reported — separates fatigue from disengagement |

The practice condition should not selectively train one confirmatory technique. A neutral instructional practice may include short demonstrations of all cue types, followed by a no-warning task rehearsal.

Hazard penalties, if retained, remain silent during the block and are revealed only after questionnaires. Pilot the penalty to ensure engagement without unsafe or distorted reaching.

## 9. Primary analysis

### Opportunity-level model

Preferred primary model for the four factorial conditions:

```text
violation ~ Policy * Mapping
          + period
          + layout
          + event_type
          + target_limb
          + (1 + Policy + Mapping | participant)
```

Use a binomial/logistic mixed model because the primary opportunity outcome is binary. Layout is a fixed nuisance factor when the six authored levels exhaust the layouts used in the experiment. Simplify random effects only through a preregistered convergence procedure.

Report marginal risks and absolute risk differences in addition to odds ratios. The confirmatory interaction must be interpreted using the planned difference-in-differences/marginal contrasts, not by comparing which individual p-values are significant.

### Anchor analyses

Analyze PB versus Visual and feedback versus None in separate preregistered models/contrasts. Do not force modality anchors into the factorial interaction.

### Sensitivity analyses

- block-level count model using only presented valid opportunities and an offset;
- stricter/looser frozen collision radii;
- inclusion of unattributed contacts as a secondary endpoint;
- tracking-quality eligible sample;
- all-started-participants analysis with explicit missingness assumptions; and
- order/learning interaction checks.

### Multiplicity and missingness

Pre-register one primary outcome and an ordered testing or familywise/FDR plan covering H1–H5. Specify invalid opportunity, block termination, questionnaire missingness, convergence, and exclusion handling before outcome inspection.

## 10. Avoidance-response mechanism

For each alert, bind the detector to the warned limb, designated hazard, and engagement. Expire the detector at the opportunity close or a short preregistered response window.

Avoidance onset is the first sustained change away from the pre-alert counterfactual trajectory, validated on blinded pilot labels. Do not count spontaneous movement by another limb or response to a different obstacle.

Because no response may be observed, analyze response incidence and latency jointly or treat absent responses as censored. Do not calculate a responder-only mean and interpret it as the condition's causal latency.

## 11. Tracking, cue delivery, and quality

- Use sequence numbers, source identity, timestamps, and tracking-quality flags.
- Bound queues and log dropped/malformed frames.
- Measure HMD/tracker clock alignment and end-to-end latency.
- **Validate Vicon-to-Unity registration with residuals and independent check points.**
- Separate stationary behavior from frozen tracking.
- Define tracking-valid opportunity rules before collection.
- **Preserve both clean reference and algorithm-input streams when conducting robustness analyses.**
- Measure physical haptic onset; software command time alone is insufficient.

### REVISED 2026-09-08 (PI discussion) — decide by paired pilot comparison

**Both tracking systems are run during the pilot and the choice is made on measured evidence.** This
supersedes the 2026-09-07 decision below, which is retained because its analysis now serves as the
**prediction the pilot tests**.

#### Run them simultaneously, not sequentially

Record **Vicon and the commodity trackers at the same time, on the same pilot participants, during the same
movements.** Mount Vicon markers as a rigid cluster on each tracker with a fixed offset, or on adjacent
segments where fit prevents that.

Separate Vicon sessions and separate tracker sessions would confound tracking system with participant,
session, and day — leaving you comparing systems using different movements. Paired logging gives a direct
error measurement on identical motion, and it simultaneously satisfies the error-characterization
requirement in §5. One set of pilot sessions, two answers.

The mounting complexity that made simultaneous logging unattractive for 36+ confirmatory sessions is
acceptable across a handful of pilot sessions. This is the right place to bear it.

#### Fix the decision criteria before inspecting pilot data

Selecting "the best one based on results" is only sound if *best* is defined in advance. Otherwise the choice
is made after seeing which system produced the more agreeable numbers — a researcher degree of freedom that
reviewers check for. Derive each threshold from **what the analysis requires**, not from what the hardware
happens to deliver, and record them, dated, before the first pilot session.

**Paper 1 criteria** — thresholds below are illustrative and must be set from the capsule model and the §4
simulation before piloting:

| Metric | Candidate threshold | Why it matters |
|---|---|---|
| Positional error, 95th pct, per site, **at task speed** | < 25% of the capsule contact band | Keeps the contact decision from being noise-dominated |
| Valid-opportunity rate under dropout | ≥ 90% | Protects the denominator; dropout clusters on exactly the vigorous movements the task elicits |
| **Delivered lead-time jitter in the predictive condition** | SD < 15% of `T` | **The decisive one.** Guards against velocity noise degrading the predictive arm of H1 and not the proximity arm |
| Marker-integrity failures (Vicon) or tracking loss (commodity) per session | Recorded | Each system's characteristic failure mode |
| Setup time per participant | Recorded | The convenience half of the decision |

**A split outcome is permitted and should be expected.** Paper 2's requirements are materially stricter than
Paper 1's, so "commodity trackers are adequate for Paper 1 but not for Paper 2" is a coherent result. Do not
force one system across both papers for tidiness; record a per-paper decision.

**If both systems clear their thresholds**, convenience decides — and that is legitimate. Define convenience
in advance too: setup time, restart and failure rate, participant comfort, and operator workload.

#### What the pilot yields regardless of the outcome

Paired data characterizes commodity-tracker error against a reference on real task movement. That is the
empirical commodity-versus-reference comparison earlier deferred as an optional upgrade — obtained here as a
by-product of the selection process, and available to strengthen the §11 robustness analysis whichever system
is chosen for the confirmatory study.

### The 2026-09-07 analysis — now the prediction under test

The 2026-09-07 analysis concluded **Vicon for both papers**, on the velocity-error amplification argument in
§5. That analysis is not withdrawn — it is now the **prediction the paired pilot tests**, and it says what to
expect:

> Commodity trackers will prove adequate for Paper 1's binary contact outcome, and inadequate for Paper 2's
> millisecond threshold and kinematic account. The Paper 1 margin will be narrower than it looks, because
> velocity noise degrades the predictive arm of H1 and not the proximity arm.

If the pilot contradicts this, the pilot wins — that is the point of measuring. If it confirms it, the
argument is now backed by data rather than algebra, which is a better position for the manuscript either way.

**Both systems must therefore be working before the pilot**, which front-loads engineering rather than
reducing it. Budget for it.

Six joints under either system: head, chest, both hands, both feet.

**What Vicon costs, if selected.** Vicon→Unity co-registration returns as a launch gate. That risk is lower than it looks:
`RigidTransformSolver` / `RigidAlignment` are already implemented and verified to recover known transforms
exactly and to report residuals that track injected noise. `ViconBodyRig` and `ViconCalibrationRecorder`
return to the critical path, along with `docs/VICON_SETUP.md` — which is among the files currently missing
from disk.

**Each system has its own characteristic failure mode, and the pilot must exercise both.** Vicon's risk is
marker integrity; the commodity trackers' is occlusion-driven dropout, drift, and motion blur at speed. Pilot
each under full-speed movement, never on a stationary participant.

**Vicon-side checks:**

- **Marker occlusion.** Verify camera count and placement cover the full 3.5 × 3.5 m volume with limbs at
  full extension and the body between a marker and the cameras. Occlusion is a coverage problem, so it is
  solvable by camera placement — but only if measured first.
- **Marker swapping and mislabelling.** The characteristic outside-in failure during fast, large-amplitude
  motion, and far more insidious than dropout: a mislabelled marker produces confident, wrong data rather
  than a gap. Preregister the labelling-integrity check and the rule for invalidating an affected opportunity.
- **Physical detachment.** Markers come off during vigorous dodging. Build a between-block marker check into
  the session checklist.
- **Frame rate.** Target **≥ 200 Hz**. Velocity estimation quality scales with rate, and Paper 2's kinematics
  depend on it directly.

**Commodity-tracker-side checks:**

- **Occlusion-driven dropout** — a wrist tracker pressed against the torso, an ankle tracker occluded
  mid-stride, a limb swung outside usable camera view. Measure per-site dropout rate at task speed.
- **Drift** — SLAM-based tracking accumulates error across a session. Log a per-block drift check against a
  known physical reference pose.
- **Motion blur at speed** — accuracy degrades exactly when the warning matters most. Characterize error as a
  function of limb speed, not as a single scalar.
- **Wireless latency and jitter** — measured, not assumed.

**Latency compensation must be measured for whichever system is chosen.** The zero default in `OracleParams`
is unacceptable either way. Measure end-to-end tracker-to-application latency and replace it.

### Hand tracking and participant input

**If Vicon is selected**, hands are tracked directly and no wrist-to-hand offset is needed.

**If commodity trackers are selected**, trackers sit at the **wrist**, and a fixed anatomical wrist-to-hand
offset must be defined, frozen, and applied to the `LeftHand` / `RightHand` joints, the 0.15 m orb-collection
radius, and the capsule contact model. Decide whether it is standardized or measured per participant. An
unrecorded offset silently biases both task scoring and the primary outcome, and is unrecoverable afterwards.

The paired pilot resolves this too: with both systems logging simultaneously, the wrist-to-hand offset can be
**measured directly** against Vicon rather than taken from anthropometric tables.

> ### DECIDED 2026-09-11 (PI) — Paper 1 retains controllers in both hands throughout
>
> **This supersedes the 2026-09-07 "one controller, questionnaires only" decision below, for Paper 1 only.**
> Hands are tracked by the **held controllers**; chest and both ankles by commodity trackers (three trackers
> total). Participants keep both controllers for the whole session, including measured blocks.
>
> **Rationale — ecological validity.** Real room-scale VR gameplay is performed holding controllers. The
> deployment scenario this study models is a user colliding with a hazard *during VR gameplay*, so a
> controller-in-hand posture matches that scenario more closely than empty hands. The 2026-09-07 decision was
> reasoned from the Vicon path ("Vicon frees the hands") and did not weigh the ecological cost of removing
> them.
>
> **What this resolves:**
> - The **wrist-to-hand offset** requirement below does not apply to Paper 1. A held controller sits at the
>   hand, so there is no anatomical offset to define, freeze, or propagate into the orb radius and capsule
>   model. This closes an unimplemented gap rather than deferring it.
> - **Forearm real estate** (§14.26) is no longer contested for Paper 1: only the Tactosy arm unit mounts
>   there, with no wrist tracker to shadow or be shadowed.
> - **Questionnaire responding** simplifies. No hand-over or set-down step, and no operator verification that a
>   controller is down before a block. Participants answer with a controller already in hand. The
>   self-report-validity requirement that motivated the original decision is still met: the participant answers
>   **unobserved**, with the operator outside the response loop.
> - Tracker count for Paper 1 stays at **three** (chest, left ankle, right ankle).
>
> **Cost, to be stated as a limitation in the manuscript.** Holding a controller adds mass to the hand and
> alters reaching and braking dynamics relative to an unencumbered limb. Paper 1's outcome is a binary
> violation probability per opportunity rather than fine kinematics, so this is a second-order effect, but it
> must be named rather than left implicit.
>
> **Scope.** Paper 1 only. **Paper 2 is explicitly undecided** and must make its own recorded choice: it
> estimates mid-flight braking at millisecond resolution, where added hand mass acts directly on the quantity
> being measured. §11 permits a split outcome across the two papers.
>
> **Implementation status.** `ControllerTrackerStandIn` is therefore the sanctioned Paper 1 rig, not a
> protocol-validation stand-in: head = HMD, hands = controllers, chest and ankles = trackers. The
> "controller must not be tracked into the pose model" instruction below does not apply to Paper 1.

**DECIDED 2026-09-07 — one controller, questionnaires only.** *(Superseded for Paper 1 by the 2026-09-11
decision above. Retained as the record for Paper 2 and for the Vicon path.)* Vicon frees the hands, so
participants hold nothing during measured blocks. A single controller is handed over for questionnaire
responding and **set down before each block begins**.

The reason is measurement validity, not convenience: presence, workload, and acceptability are the secondary
outcomes that guard against a warning "succeeding" by wrecking the experience. Their validity depends on the
participant answering **unobserved**. Having the operator transcribe spoken answers would place the
experimenter inside the self-report loop and introduce a demand characteristic on exactly those items.

Implementation requirements:

- `QuestionnairePanel` and the operator gates bind to controller input; nothing in a measured block may
  require it.
- The session checklist must include hand-over and set-down steps, and the operator must verify the controller
  is down before starting a block — a held controller changes hand mass and the movement being measured.
- The controller must not be tracked into the pose model. `ControllerTrackerStandIn` stays retired to
  protocol-validation use; hands come from Vicon.

Rejected: operator-entered spoken responses (demand characteristic on the experience outcomes), and headset
hand tracking for UI (adds a second tracking modality to the session purely for form input, and depends on the
hand-tracking path being reliable enough on this headset).

**Forearm real estate remains contested** — a Tactosy arm unit and Vicon markers both need to mount without
the markers being shadowed by the haptic unit or the strap. Verify fit at full speed during setup rehearsal.

### Robustness analysis — restored to its original form

With a reference stream available, the robustness analysis runs as originally designed: treat Vicon as ground
truth, degrade a separate warning-input stream, and report warning fidelity against it.

This is **stronger than the bench-characterized error model** the inside-out decision had forced, because the
degradation is applied to real recorded movement rather than reconstructed from an offline error model.

**DECIDED 2026-09-07 — no simultaneous commodity-tracker logging.** Degradation is synthetic, applied to the
Vicon reference stream. Mounting a commodity tracker alongside Vicon markers was considered and rejected:
forearm space is already contested by the Tactosy unit and the markers, a bulky tracker there risks shadowing
markers, and the project has 26 open launch gates with no ethics approval. The upgrade was not worth another
thing to go wrong at pilot.

**Consequence to state honestly in the manuscript.** Without a measured commodity arm, the robustness curve
cannot answer "does this work on affordable hardware?" empirically — it characterizes algorithm sensitivity to
error families, and any deployment inference is extrapolation. Name that as a limitation and as the natural
next study rather than implying the sweep settles it.

It remains a sensitivity analysis of the algorithm, not a device certification.

## 12. Data outputs

Use one immutable participant/session directory with overwrite protection. Required outputs:

- session and block metadata;
- one row per planned opportunity with presentation/validity/reason fields, **and the counterfactual
  dual-policy timing columns** (§6) that make the timing manipulation auditable per trial;
- chronologically ordered event stream including TTC and physical cue timing;
- frame-level raw keypoints with sequence/quality fields;
- questionnaire item-level and scored data;
- cue calibration and hardware timing logs;
- e-stop/adverse-event log; and
- analysis-ready derivations with scripts and checksums.

Repeated CSV headers, unordered event groups, silent malformed-frame loss, and participant-root append files are release blockers.

## 13. Ethics, safety, and claims

No human pilot or main study begins before approval covering:

- repeated VR reaching/dodging and physical exertion;
- haptic stimulation and relevant contraindications;
- cybersickness and withdrawal;
- invisible virtual hazards and cleared physical workspace;
- spotter responsibilities and emergency stop;
- tracking loss and passthrough policy;
- compensation and partial-session payment;
- raw movement-data privacy, encryption, retention, and sharing; and
- sanitation and skin-contact devices.

Claims are relative effects on **virtual hazard-boundary violations**. Do not use “prevents collisions,” “improves safety,” or injury/deployment language without a separately validated real-world link.

### Virtual-hazard validity — the position to take in the manuscript

Full evidence review: `docs/VIRTUAL_HAZARD_VALIDITY.md` (15 studies, 2001-2026).

**The anticipated objection.** *Real VR gameplay has physical furniture around the user, so virtual-only
hazards destroy generalization.* It is a reasonable objection to the claim that absolute violation rates
predict real furniture collisions — which this study does not make — and it does not reach the within-subject
condition contrasts, which is everything §3 actually tests.

**The empirical position.** Direct real-versus-virtual comparisons of obstacle negotiation — walking around,
stepping over, reaching around, and fleeing a threat — consistently find that immersion alters the *magnitude*
of avoidance margins while preserving the *structure* of avoidance: scaling with obstacle dimensions, side
selection, onset timing, and steering dynamics. Main effects, not interactions. Participants are also
consistently **more** conservative in VR, not more reckless, which is the opposite of the objection's implicit
model.

**The argument specific to this design, and the strongest one available.** The hazard volumes here are
**visually hidden in every condition** (§5). The mechanism that drives the real-versus-virtual difference in
the literature is perceptual uncertainty about the location of a *visible* virtual obstacle. With no visible
hazard in any condition, that mechanism largely does not apply. More importantly, a hazard that is present,
registered, and invisible is a *closer* model of the deployment problem — real furniture the user cannot see
through the HMD — than a visible virtual obstacle would be. **Argue this in related work, not in limitations.**

**Why physical hazards were rejected on methodological, not only safety, grounds.** Physical contact is itself
a spatial-information channel: a participant who strikes a real obstacle learns where it is. Contacts are not
evenly distributed across conditions — **None** produces the most, by construction — so the floor condition
would accrue the most incidental spatial learning. Physical consequence additionally raises anxiety and
perceived difficulty, and that too would concentrate in **None**. Physical hazards would therefore confound the
floor comparison with both extra information and differential arousal. Virtual hazards remove both channels:
the only information about hazard location is the warning under manipulation.

**Manuscript obligations.**
1. Say *hazard-boundary violations*, never "collisions prevented" (already required above).
2. Place the justification in related work, so a reviewer meets it before forming the objection.
3. Report engagement as evidence rather than assertion — task components, avoidance-intent item, and non-floor
   violation rates — against the known failure mode of disengaged participants ignoring virtual hazards.
4. State the attenuation limitation: absolute rates are conservative relative to physical hazards and are not
   deployment estimates.
5. Name a **between-subjects** physical-prop validation as future work (§16 scope note).

## 14. Pilot and launch gates

1. Ethics approval and final safety protocol.
2. Complete implementation of session orchestration, layouts, questionnaires, logs, and e-stop.
3. Verified one-to-one target limb/hazard attribution.
4. Physical haptic latency/jitter characterized for every device class.
5. Per-site salience matching and torso motor-cluster verification.
6. Predictive and proximity policy unit tests plus replay-based adversarial cases.
7. Visual benchmark verified to be trigger/duration matched to PB.
8. Condition × layout allocation audit passes.
9. End-to-end dry run reconstructs all 72 opportunities and all invalidity reasons.
10. Pilot establishes engagement, non-floor/non-ceiling violation rates, and feasible questionnaires.
11. Power simulation and analysis recovery on synthetic data pass.
12. The §2 actuator decision is recorded, procured, and reflected in the mapping-hypothesis wording.
13. The §15 ranking rule, gates, equivalence margins, and practical-equivalence margin are preregistered.
14. The §1 novelty search is complete, recorded, and the novelty statement is frozen and dated.
15. The §3 interpretation-order rule and simple-effect contrasts are preregistered.
16. ~~The §3 response to the H2 information confound is recorded.~~ **CLOSED 2026-09-07** — narrowed claim.
    Remaining work: the three manuscript obligations in §3 (narrowed wording throughout, Belt & Whistles
    positioned in related work, follow-up study named).
17. ~~What the H4 visual cue indicates is decided, and "feedback family" in H5 is defined.~~
    **CLOSED 2026-09-07** — visual cue indicates the at-risk body part; H5 uses two confirmatory families with
    per-condition eligibility computed descriptively. Remaining work: implement the transient limb-highlight
    cue and pilot that it is detectable during vigorous movement.
18. **Tracker-selection criteria recorded and dated BEFORE the first pilot session** (§11). Choosing after
    inspecting pilot data is not a decision.
19. **Both tracking systems implemented and logging simultaneously**, with mounting resolved, so the pilot
    produces paired data on identical movement (§11).
20. **Paired pilot comparison complete and the per-paper selection recorded** against those criteria — a split
    outcome across Paper 1 and Paper 2 is permitted (§11).
21. **For whichever system is selected:** accuracy and frame rate verified in the actual volume during
    vigorous movement; Vicon → co-registration validated with residuals and `docs/VICON_SETUP.md` located or
    rewritten, plus marker-integrity piloting and a between-block marker check; commodity → per-site dropout,
    drift, and motion-blur characterization, plus a frozen wrist-to-hand offset applied to the orb radius and
    contact model (§11).
22. Limb-volume capsule radii frozen from anthropometry, and — if commodity trackers are selected — the
    effective contact band verified above the measured 95th-percentile error (§5).
23. Measured end-to-end tracker-to-application latency replaces the zero default in `OracleParams`.
24. Sternum-Tactosy versus vest-projectile discrimination check passes, with projectile feedback constrained
    away from the sternum (§2). Fallback to audio-visual projectile feedback if marginal.
25. ~~Participant input method for questionnaires is chosen.~~ **CLOSED 2026-09-07** — one controller,
    questionnaires only. Remaining work: bind `QuestionnairePanel` and operator gates to it, add hand-over and
    set-down steps to the session checklist with an operator verification that the controller is down before a
    block starts, and confirm the controller is excluded from the pose model (§11).
26. Forearm fit verified under full-speed movement for the selected system — Tactosy plus Vicon markers
    (markers not shadowed by the haptic unit), or Tactosy plus tracker (tracker cameras not occluded). During
    the paired pilot, **both** must coexist (§11).
27. **⚠ BLOCKING — policy separation verified** (§6). `T` raised to 1.00 s and regression-tested
    ✅ 2026-09-14. Schedule audited, O2 moved to (0.95, 0.70, 0.60), re-audited **12/12 pass**
    ✅ 2026-09-14 (`docs/OPPORTUNITY_GEOMETRY_AUDIT.md`). Counterfactual dual-policy trigger logging
    implemented and tested ✅ 2026-09-14 (`PolicyTriggerProbe`, 16 tests, suite 145/145).
    `ConditionManager` corrected to cue only the designated target limb × target hazard ✅ 2026-09-14
    (10 tests, mutation-checked; suite 155/155).
    **Outstanding:** give the hazard geometry a single source of truth in Core and add the approach-distance
    test; confirm `D`/`T` against measured pilot movement; **preregister the manipulation check and its pass
    criterion** — the data to compute it now exists, the threshold does not.
    **Until this closes, a null H1 is uninterpretable.**
28. **Engagement pass criteria frozen from pilot and preregistered** (§8).
29. **Expectancy probe added to the session and its coding procedure preregistered** (§8).
30. **Screening, exclusion criteria, and recorded participant characteristics preregistered** (§4), including
    the SSQ stopping threshold and the action it triggers.
31. **Opportunity schedule audited for left/right target-limb balance** (§4), so handedness cannot align with
    condition.
32. **Self-representation frozen and verified** (§5): ankle markers present, identical in all six
    conditions, marker diameter below the smallest frozen contact radius, and the startup configuration line
    checked against the protocol at the top of every session.
33. **Virtual-hazard validity position written into related work** (§13), with the hidden-hazard argument and
    the asymmetric-confound argument stated there rather than in limitations.
34. **Participant instruction script frozen and its delivery logged** (§8): read verbatim from a card,
    identical per-block reminder across conditions, and any participant question recorded.
35. Protocol, code, parameter file, analysis, and preregistration are frozen.

## 15. Condition ranking and design recommendation

The hypotheses in §3 ask whether effects exist. They do not answer “which condition should a developer use.”
That is a **decision** problem, and it is kept structurally separate from inference. Everything in this section
is preregistered, and none of it produces a p-value.

### 15.1 Why there is no composite score

Do not build a weighted composite (for example `0.5 × safety + 0.3 × presence + 0.2 × workload`) and rank on it.
Composite scores fail at review for four independent reasons:

- the weights are arbitrary and unfalsifiable, and any choice invites the objection that it was selected to
  favor the authors' technique;
- a condition can win by being mediocre on everything, which is rarely the design advice a reader wants;
- combining outcomes measured on incommensurable scales into one number hides the trade-off that is the
  actually interesting result; and
- it conflates the inferential question with the decision question.

If a composite is demanded by a collaborator or reviewer, the only defensible form is a **weight-sensitivity
analysis**: compute the ranking across the full simplex of plausible weights and report the proportion of weight
space in which each condition ranks first. That converts an arbitrary choice into a robustness claim. It is a
supplementary figure, never the headline.

### 15.2 The ranking rule — lexicographic, with gates

Apply in order. Every threshold below is frozen before outcome inspection.

**Step 0 — Eligibility.** A condition enters the ranking only if it shows a credible reduction in violation
probability relative to `None`. *This step exists to prevent a degenerate result:* `None` delivers no warnings,
so it trivially passes every experience gate and would otherwise rank well whenever safety differences are
small. `None` is a reference level, not a candidate.

**Step 1 — Primary criterion.** Rank eligible conditions by model-estimated marginal probability of a
target-limb × target-hazard boundary violation per valid opportunity, lower is better. Report as absolute risk
difference versus `None` with confidence intervals, alongside odds ratios.

**Step 2 — Experience gates.** A condition that fails non-inferiority on presence or workload against the §3
margins is flagged **not recommended**, regardless of its Step 1 position, and is reported with the failed gate
named. Elevated sickness is handled as a safety matter under §13, never as a ranking trade-off.

**Step 3 — Burden tie-break.** Conditions statistically indistinguishable at Step 1 are ordered by alert burden:
alerts delivered per valid opportunity, and the proportion of alerts issued on opportunities where no violation
would have occurred. Fewer is better. This matters for deployment because alert burden drives compliance decay,
and it is the dimension on which predictive and proximity policies most plausibly differ.

**Step 4 — Acceptability tie-break.** CUE acceptability, used only to separate conditions still tied after
Step 3.

“Statistically indistinguishable” means the confidence interval for the difference from the best-performing
condition falls entirely within a preregistered practical-equivalence margin on the risk-difference scale.
Fix that margin at pilot; a provisional value of **3 percentage points of absolute violation risk** is a
reasonable starting point and must be confirmed against pilot base rates.

### 15.3 Report the uncertainty in the ranking, not just the ranking

A point ranking of six conditions estimated from a realistic sample is mostly noise. Report:

- **rank probabilities** by bootstrap or posterior draw — `P(condition k has the lowest violation probability)`,
  and `P(rank ≤ 2)` — rather than a single ordering; and
- the full rank distribution as a figure.

“PB had the lowest estimated violation probability (posterior probability of being best = 0.62; PG = 0.21)” is
both more honest and more useful than “PB won.” If no condition exceeds roughly 0.5 probability of being best,
say so plainly: the study identified an effect structure without resolving a single winner, which is a
legitimate result and should not be laundered into a ranking.

### 15.4 Show the trade-off

Include a two-dimensional trade-off figure: violation probability against alert burden, with each condition
plotted with its confidence region, experience-gate status encoded by marker, and the Pareto frontier drawn.

If no condition dominates — plausible, since predictive policies buy safety by warning more — then the
recommendation is a frontier, not a point, and the paper should say so. A defensible finding is often
*“PB minimizes violations; PG achieves most of the benefit at materially lower alert burden; the choice
depends on tolerance for unnecessary warnings.”*

### 15.5 What the ranking may be called

The ranking is a **design recommendation under a preregistered decision rule for this task and population**.
It is not a general claim that one warning design is best, and it does not license deployment or injury
language (§13). The confirmatory claims remain the §3 contrasts.

## 16. Publication decision rules

- H1 supports a warning-policy conclusion, not a pure biological timing conclusion.
- H2 supports a spatial-mapping conclusion only after perceptual/device calibration, and only under actuator
  Option A or C in §2; under Option B the claim is about haptic mapping implementation.
- H3 requires the explicit interaction contrast; PB merely having the lowest mean is insufficient.
- PB versus Visual is a matched engineered-benchmark result, not “better than current practice,” and requires
  the rewritten transient visual cue rather than the legacy proximity glow.
- Experience preservation requires an equivalence/non-inferiority design.
- The §15 ranking is a decision recommendation under a preregistered rule, reported with rank uncertainty. It
  is never presented as a hypothesis test, and a condition ranked first without separation from the runner-up
  is reported as unresolved.
- Absolute deployment and injury-prevention claims remain out of scope.
