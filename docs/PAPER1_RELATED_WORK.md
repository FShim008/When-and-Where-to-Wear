# Paper 1 — Related Work

**"What Makes a Collision Warning Work? Separating Trigger Timing, Body Site, and Cue Form in VR"**
Drafted 2026-09-23. ~950 words across five subsections, matching the length of `PAPER2_RELATED_WORK.md`
(1,025 words / 1.09 pages) which was reviewed against recent accepted ISMAR papers.

Every citation resolved against Crossref on 2026-09-23. **Nothing here is quoted from memory**, and the
Valkov and Linsen claims are from the full PDF, not the abstract — see §5B of
`PAPER1_PARAMETER_JUSTIFICATION.md` for why that distinction cost us a false novelty claim.

---

## 2.1 Real-World Awareness in Immersive VR

A head-mounted display replaces the user's view of the room, so any obstacle that was not modelled becomes
invisible. Commercial systems handle this with a user-drawn boundary shown when the user approaches it.
Research systems have gone further: rendering a stylised view of the real world for obstacle avoidance
[Kim et al. 2021], reconstructing nearby geometry to improve awareness during interaction [Valentini et al.
2020], and warning through vibrotactile actuators mounted on the headset itself [Valkov and Linsen 2019].

These systems share an architecture. Something senses proximity between the user and a real object, a rule
decides when that proximity warrants an alert, and a display delivers it. The literature has studied the
third stage closely and the second stage hardly at all.

## 2.2 What the Cue Should Be

Most work on real-world awareness asks what the warning should look or feel like. Vibrotactile patterns can
encode obstacle distance in a form users learn to interpret [Kim et al. 2015], and body-worn tactile
displays have a long history in navigation and obstacle avoidance for users with limited vision
[Samsel et al. 2025]. Recent work extends tactile warning to human-robot collaboration, where an operator
must be told about an obstacle they cannot see [Sirintuna et al. 2024].

This work establishes that tactile warning is effective and that its form matters. It does not address when
the warning should be issued, which is treated as an implementation detail fixed by a threshold.

## 2.2b Where to Warn: Spatial Tactile Warning Signals

A parallel literature asks not what the cue is but **where on the body it sits**. Driving research has
studied spatial tactile warnings for two decades: a cue presented at the location of an impending hazard
orients attention faster than a non-spatial one, and the effect holds across modalities [Spence and Ho
2008]. A survey of haptic driver-support systems catalogues the design space and its evidence [Petermeijer
et al. 2015]. Meng et al. [2014] went further and made the cue itself spatially *dynamic*: sequentially
firing tactors on the hands, forearms and waist creates apparent motion across the body, and a cue moving
**towards the torso** produced faster braking than an equivalent static cue on the waist.

That literature is closer to ours than the VR work, and it constrains the design in two ways. It shows body
location is not a free parameter — where a cue sits, and which way it appears to move, changes the response
it produces. And it supplies a directional prior: a cue that moves *toward* the body outperformed one moving
away from it.

But it cannot answer our question, for two reasons. The hazard is **external** — a lead vehicle, responded
to by braking — so the cue signals *an approaching object's direction*, never *which of the participant's own
limbs is about to be struck*. And **warning timing is held constant throughout**: the warning onset coincided
with the hazard event, in every condition of all three experiments. Timing is a fixed feature of the
paradigm, not a factor. Crossing it with somatotopic mapping is what we contribute.

## 2.3 When to Warn: Evidence From Automotive Human Factors

Automotive human factors settled the structure of the timing question decades ago. A forward collision
warning can fire at a fixed distance or when predicted time-to-collision crosses a threshold, and production
systems have been built both ways; some combine the two criteria [Chen et al. 2013].

Timing changes behaviour rather than merely changing when behaviour happens. Warnings judged too early are
distrusted and eventually ignored; warnings that arrive too late cannot be acted on at all
[Parasuraman et al. 1997; Abe and Richardson 2006a, 2006b]. Warning timing is therefore treated in that
literature as a design variable with its own effects on trust and compliance, and algorithms are tuned
against driver response characteristics rather than against sensor capability alone [Bao and Wang 2024].

Two things do not transfer directly. A driver acts through a vehicle with well-characterised braking
dynamics; a VR user acts with a limb, over a few hundred milliseconds, at distances of tens of centimetres.
And a driver's warning arrives in a fixed location, whereas a body-worn display can place the cue on the
limb at risk. The question transfers; the parameters do not.

## 2.4 The One Prior Comparison, and What It Could Not Separate

Valkov and Linsen [2019] compared the two trigger policies directly in VR. Forty participants walked toward
an invisible virtual wall while vibrotactile actuators in the HMD face cushion warned them, under a
fixed-distance threshold and under a threshold scaled by walking speed. Firing when distance falls below
`u · t` is the same rule as firing when time-to-collision falls below `t`, so the speed-scaled condition is
a forecast-triggered policy.

Their result ran against the intuition that earlier warning helps: the speed-scaled policy produced
significantly *more* collisions and smaller safety margins. They attributed this not to the trigger but to
the display mapping. Vibration intensity was a continuous function of distance and speed, so slowing down
reduced the vibration; participants "continued walking slowly forward while constantly decreasing the speed
to adjust the vibration level." The warning became a signal to manage rather than a signal to obey.

The two conditions therefore differed in two ways at once. The speed-scaled condition changed *when* the
cue fired and also made its intensity a quantity the participant could modulate by changing behaviour. Their
null is consistent with forecast triggering being unhelpful, and equally consistent with continuous
modulable mapping being harmful. Their own discussion favours the second and calls for further work.

## 2.5 What This Paper Adds

We separate the two explanations. Our predictive and proximity conditions differ only in trigger: the cue is
a single edge-triggered pulse train, identical in form, amplitude and duration, with nothing for the
participant to modulate. A third condition holds the predictive trigger, the cue site, the information
content and the perceptible onset constant, and varies only whether intensity is discrete or continuously
ramped with time-to-contact. Together these isolate trigger policy and cue form, which prior work varied
together.

We also cross trigger policy with somatotopic mapping — whether the cue is delivered to the limb at risk or
to an undifferentiated torso site — which no prior work has done. Valkov and Linsen's actuators were all on
the head, so localization could not vary.

Finally, our task is reaching rather than locomotion, with real physical obstacles rather than a virtual
wall and a simulated sensor. Stopping a reach and stopping a walk are different motor problems on different
timescales, and warning parameters derived for one should not be assumed to hold for the other.

---

## Bibliography

All 15 resolved against Crossref (12 on 2026-09-23, 3 added from novelty round 5). Counts per Crossref.

| Ref | Citation | DOI | Cites |
|---|---|---|---|
| Abe & Richardson 2006a | Alarm timing, trust and driver expectation for forward collision warning systems. *Applied Ergonomics* 37, 577–586 | `10.1016/j.apergo.2005.11.001` | 136 |
| Abe & Richardson 2006b | The influence of alarm timing on driver response to collision warning systems following system failure. *Behaviour & Information Technology* 25, 443–452 | `10.1080/01449290500167824` | 33 |
| Bao & Wang 2024 | Optimization of forward collision warning algorithm considering truck driver response behavior characteristics. *Accident Analysis & Prevention* 198, 107450 | `10.1016/j.aap.2023.107450` | 17 |
| Chen et al. 2013 | Forward collision warning system considering both time-to-collision and safety braking distance. *Int. J. Vehicle Safety* 6, 347 | `10.1504/ijvs.2013.056968` | 20 |
| Kim et al. 2015 | Identification of vibrotactile patterns encoding obstacle distance information. *IEEE Trans. Haptics* 8, 298–305 | `10.1109/toh.2015.2415213` | 31 |
| Kim et al. 2021 | The effect of 2D stylized visualization of the real world for obstacle avoidance and safety in VR. *ACM VRST*, 1–3 | `10.1145/3489849.3489943` | 0 |
| Parasuraman et al. 1997 | Alarm effectiveness in driver-centred collision-warning systems. *Ergonomics* 40, 390–399 | `10.1080/001401397188224` | 175 |
| Samsel et al. 2025 | A comparison of vibrotactile patterns in an early warning system for obstacle detection. *Applied Ergonomics* | `10.1016/j.apergo.2024.104396` | 15 |
| Sambo et al. 2012 | Defensive peripersonal space. *J. Neurophysiology* 107, 880–889 | `10.1152/jn.00731.2011` | 132 |
| Sirintuna et al. 2024 | Enhancing human-robot collaborative transportation through obstacle-aware vibrotactile warning. *Robotics and Autonomous Systems* | `10.1016/j.robot.2024.104725` | 21 |
| Meng et al. 2014 | Dynamic vibrotactile warning signals for frontal collision avoidance: towards the torso versus towards the head. *Ergonomics* 58, 411–425 | `10.1080/00140139.2014.976278` | 26 |
| Petermeijer et al. 2015 | The effect of haptic support systems on driver performance: a literature survey. *IEEE Trans. Haptics* 8, 467–479 | `10.1109/toh.2015.2437871` | 126 |
| Spence & Ho 2008 | Tactile and multisensory spatial warning signals for drivers. *IEEE Trans. Haptics* 1, 121–129 | `10.1109/toh.2008.14` | 108 |
| **Valkov & Linsen 2019** | **Vibro-tactile feedback for real-world awareness in immersive virtual environments. *IEEE VR*, 340–349** | `10.1109/vr.2019.8798036` | 17 |
| Valentini et al. 2020 | Improving obstacle awareness to enhance interaction in virtual reality. *IEEE VR*, 44–52 | `10.1109/vr46266.2020.00022` | 27 |

`Sambo et al. 2012` is cited in Methods for the `D` = 0.30 m operating point, not in Related Work.

---

## Notes for revision

**Length.** ~1,120 words after §2.2b. Paper 2's reviewed section is 1,025 words / 1.09 pages, so this is
now slightly over. If it must shrink, §2.1 compresses best — it is scene-setting. If it must shrink, §2.1 compresses best — it is
scene-setting. **§2.4 must not be cut.** It carries the paper's motivation and is the single paragraph a
reviewer who knows Valkov and Linsen will look for first.

**Tone in §2.4.** The framing is "their design could not separate two explanations," never "their study was
flawed." Their own discussion reaches the same conclusion and calls for the follow-up. Say so — it is both
more accurate and more collegial, and a reviewer may well be one of them.

**Before submission.** Re-run the novelty search **against full text**, not titles and abstracts. The claim
that no one had compared these policies survived a title-and-abstract sweep and died on the first PDF. Any
remaining "no prior work has…" sentence in either paper needs the same treatment. The one surviving such
claim here is in §2.5 ("which no prior work has done", re: crossing policy with localization) — verify it.

**Still to check.** Valentini et al. 2020 and Kim et al. 2021 are cited from Crossref metadata for their
topic, not their parameters. If either reports a proximity threshold, add it to the Methods anchor beside
Valkov and Linsen's `dmin` = 60 cm.
