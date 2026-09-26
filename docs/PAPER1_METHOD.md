# Paper 1 — Method (draft)

**"What Makes a Collision Warning Work? Separating Trigger Timing, Body Site, and Cue Form in VR"**
Drafted 2026-09-25. Source of truth for every number: `PAPER1_STUDY_DESIGN.md`.
Parameter defence (paste-ready): `PAPER1_PARAMETER_JUSTIFICATION.md`.

> **⚠ `[N]` IS NOT YET DECIDED.** The 2026-09-25 counterbalancing fix restricted valid allocations to
> multiples of 36. See §3.1 — do not fill this in until the allocation run reports.

---

## 3 METHOD

### 3.1 Participants

`[N]` adults (`[M/F/other]`; mean age `[X]`, SD `[X]`) took part. All had normal or corrected-to-normal
vision and reported no vestibular disorder, no skin condition contraindicating vibrotactile contact, and no
condition affecting gait or reaching.

**Handedness was recorded and reported**, because the target limb of an opportunity is a specific left or
right hand or foot, and a right-dominant sample would otherwise show systematically different avoidance for
left- versus right-limb opportunities. The opportunity schedule is balanced across left and right target
limbs so handedness cannot align with condition. Prior VR experience was recorded on an ordinal scale and
reported descriptively; it is not a model covariate.

Height and limb-segment lengths were measured per participant to set the contact radii in §3.5.

**Allocation.** Condition order follows a six-sequence Williams design, which balances position and
first-order carryover. Layout order follows an **independent** six-sequence design, and participants are
allocated across the **complete 6 × 6 crossing** of condition-order row and layout-order row. One complete
allocation is therefore **36 participants**; valid targets are multiples of 36.

> **OPEN — the analysable target.** An earlier draft used 48 on the grounds that it divides by six. That
> balances condition order alone, not the crossing with layout order, and leaves some combinations
> over-represented. Candidates under measurement are **36 × 24 opportunities** and **72 × 18**. Fill in from
> the allocation power run, and state the simulated power at the final number.

**Exclusions.** A participant is excluded if the session is abandoned, if the SSQ stopping criterion is met,
or if a tracking fault invalidates more than `[X]`% of opportunities. Recruitment continues until `[N]`
complete balanced sets are obtained.

### 3.2 Apparatus

The study ran in Unity 6.3 LTS with the Universal Render Pipeline on a single PC. Head pose and five body
keypoints — chest, both hands, both feet — were tracked by an HTC VIVE headset and five VIVE Ultimate
Trackers. Trackers and headset share the SteamVR world frame natively, so no camera-to-VR calibration step
was required and no external tracking system was used.

Vibrotactile cues were delivered by bHaptics Tactosy units: one per hand, one per foot, and one on the
sternum. **The sternum unit is a Tactosy, not the vest.** A vest would confound the generic-versus-localized
comparison with a change of device, since the vest differs from a Tactosy in motor count, contact area and
driver; routing the generic cue to a sternum Tactosy holds the device class constant across the mapping
factor. The code refuses to route a warning cue to the vest at two independent layers.

### 3.3 Task and environment

Participants stood in a cleared room-scale area and performed a reaching and stepping task: coloured orbs
appeared in the space around them and were collected by touching them, with a deferred score. Between the
participant and several of the orbs stood **real physical obstacles** — a low block, a pillar, and an
upright panel — which were tracked and registered in the virtual world but **never rendered**. Two large
virtual wall volumes stood at the front and left of the arena in place of the system's own boundary display,
so the platform chaperone never appeared.

Contact with an obstacle was therefore a genuine physical event, and the warning cue was the only
information the participant had about where a hazard was.

**The score was deferred.** Collisions cost points, but the loss was revealed only on the end-of-block
summary, never as a per-event signal. A per-event signal would itself be collision feedback and would
destroy the no-feedback condition.

### 3.4 Collision opportunities

Collisions were not left to chance. Each block scripted **`[18]` collision opportunities**: a stimulus
timed to draw a *designated limb* toward a *designated obstacle*, opening an attribution window of 6 s.
Opportunities are behaviour-independent — they come from the script and the clock, never from the
participant's own movements — which is what makes the denominator of the primary outcome meaningful.

Events were spaced ~13 s apart and balanced by obstacle and by target limb. Every opportunity was audited
to guarantee the designated limb at least 0.40 m of clear approach to its designated obstacle, the distance
below which the proximity cue would fire at movement onset and the forecast cue could not lead it.

Six isometric layout variants (rotations and mirrors of a base layout) were used so obstacle positions
could not be memorised across blocks.

### 3.5 Contact model and the primary outcome

Each tracked joint was treated as a sphere of a radius set from the participant's measurements (`[chest]`,
`[hand]`, `[foot]` m) rather than as a dimensionless point. A **violation** is recorded when the designated
limb's surface enters the designated obstacle's volume within the opportunity's window.

**The unit of analysis is the opportunity, not the block**, and the denominator is opportunities that were
*presented and valid*. An opportunity whose stimulus never spawned, or whose window was cut short by an
abort or a tracking dropout, is recorded with a reason and excluded — it is neither a success nor a failure.
Dividing by a planned count would credit participants with avoiding opportunities that never happened, and
the bias is not random: it follows whatever went wrong in that block.

Repeated entry within one opportunity remains a single primary event and is counted separately.

### 3.6 Conditions

Six conditions, all within-subjects.

| | Trigger | Cue site | Cue form |
|---|---|---|---|
| **None** | — | — | no warning (floor) |
| **RG** | proximity | sternum | discrete |
| **RB** | proximity | limb at risk | discrete |
| **PG** | forecast | sternum | discrete |
| **PB** | forecast | limb at risk | discrete |
| **PBC** | forecast | limb at risk | **continuous** |

**Trigger.** The proximity policy fires when the limb is closing and within **D = 0.30 m** of the obstacle
surface. The forecast policy fires when estimated time-to-contact falls below **T = 1.0 s**. Time-to-contact
comes from a deliberately simple constant-velocity estimator over smoothed limb velocity — not a learned or
state-of-the-art predictor, because the manipulated variable is *timing*, not predictor quality.

`D` and `T` are **representative operating points of two policy classes, not claims about optimal values**.
Both fall inside published operating ranges, and the derivation, the alternatives considered, and the
sensitivity of the manipulation to them are reported in §`[results-sensitivity]`.

**Cue form.** The discrete cue is a fixed train of three 100 ms pulses separated by 60 ms gaps, **identical
in every discrete condition** — only when and where it fires differs. Cues are edge-triggered with
hysteresis, so each approach produces at most one alert.

`PBC` is the one exception, and it is the H4′ manipulation. It shares PB's trigger, site, information and
perceptible onset, but its intensity ramps continuously with time-to-contact instead of firing once. Because
time-to-contact is distance divided by closing speed, **slowing down lowers the intensity** — reproducing
the feedback loop that prior work identified as the likely cause of its own result. A pre-session
calibration matched PBC's onset salience to PB's, so the two conditions differ in cue form rather than in
effective onset.

**Multi-limb arbitration.** When several limbs are simultaneously at risk, only the most urgent is cued —
lowest time-to-contact for forecast conditions, nearest for proximity — throttled so a compound event
produces one cue rather than a burst.

### 3.7 Procedure

After consent and screening, participants were measured, fitted with the trackers and tactors, and
completed a baseline sickness questionnaire. A **cue tour** demonstrated each tactor site once, outside any
measured block, so first-block cue novelty would not differ by condition. A practice block under the **None**
condition taught the task; practice is excluded from analysis, and using a live condition for it would
privilege that condition.

The six condition blocks then ran in the participant's assigned order, each `[block length]` long, with an
enforced break of at least 30 s between blocks. After every block participants completed presence (IPQ),
workload (NASA-TLX) and sickness (SSQ) measures; after every block that delivered a warning they also
completed a four-item warning-acceptability scale (helpful, timely, trusted, annoying), in which **timely**
serves as the subjective manipulation check for the timing factor.

An operator monitored throughout with an emergency stop that silences all tactors and ends the block.

### 3.8 Analysis

The primary model is a binomial mixed-effects model on per-opportunity violations:

```
violation ~ Policy * Mapping + (1 + Policy + Mapping | participant) + (1 | layout)
```

fitted to the four factorial cells. If the full random-slope model is singular or fails to converge, a
random-intercepts model is fitted and **the fallback is reported as a deviation**.

**Confirmatory family: H1 (trigger policy), H2 (cue mapping), H4′ (cue form), under Holm correction.**
The floor contrast against **None** is a validity gate rather than a hypothesis and is not in the family;
spending alpha on a precondition would be a category error. **H3, the Policy × Mapping interaction, is
exploratory** — it is reported with its interval and its minimum detectable effect, and it does not govern
the interpretation of H1 or H2.

**The manipulation check is reported before any hypothesis test.** On every opportunity, in every condition
including None, the trigger times that *both* policies would have produced were recorded. This makes the
timing manipulation a measured quantity: the analysis reports the median lead the forecast policy actually
bought and the proportion of opportunities on which the ordering inverted.

Two further analyses address the dependence of the result on `D` and `T`. The first reconstructs both
policies' trigger times across a range of parameter values to show the manipulation's *direction* does not
depend on the operating point chosen. The second replaces the categorical policy factor with the **lead
time actually delivered**, restating the finding as a function of warning time rather than of the threshold
that produced it. Both are reported with their limits: the first establishes robustness of the manipulation,
not generality of the effect size; the second is observational and correlated with limb speed by
construction.

---

## Notes for revision

**§3.5 and §3.8 are the sections a methods-minded reviewer will read first.** The denominator argument and
the counterfactual trigger log are the two places this design is unusually careful, and both are easy to
skim past. Do not compress them for space before compressing §3.3.

**Brackets still to fill:** `[N]` and the allocation decision · `[18]` opportunities (12 are authored today)
· `[block length]` (180 s today; 18 events at 13 s needs ~240 s) · contact radii · demographics · the
tracking-fault exclusion threshold · the SSQ stopping value.

**Deliberately stated as limitations rather than hidden:** `D` and `T` are single operating points per
policy class; the contact radii are participant-measured but the capsule model is a simplification; PBC
differs from PB in total delivered vibration by construction, which is part of what "cue form" means and is
reported as a measured dose rather than left implicit.
