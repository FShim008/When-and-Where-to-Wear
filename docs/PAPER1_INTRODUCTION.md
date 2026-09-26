# Paper 1 — Introduction (draft)

**"What Makes a Collision Warning Work? Separating Trigger Timing, Body Site, and Cue Form in VR"**
Drafted 2026-09-25. ~780 words. Target: ISMAR introductions run 700–1,000 words and end with an
explicit contribution list.

Companion sections: `PAPER1_RELATED_WORK.md`, `PAPER1_ABSTRACT.txt`,
`PAPER1_PARAMETER_JUSTIFICATION.md` (Methods material).

---

## 1 INTRODUCTION

A head-mounted display replaces the user's view of the room. Anything the system has not been told about —
a chair that was moved, a person who walked in, a table just outside the play boundary — becomes invisible
at exactly the moment the user starts moving freely. Room-scale VR asks people to step, reach and lunge in
a space they cannot see, and the consequences are ordinary and physical: bruised shins, struck hands,
broken displays.

Systems respond by warning. Commercial headsets draw a boundary the user defined in advance and show it
when they approach. Research systems go further, sensing nearby geometry and signalling it through vision,
sound, or vibration on the body. Every such system, however it is built, does three things: it **senses**
proximity between the person and something real, a rule **decides when** that proximity is worth
interrupting for, and a **display delivers** the interruption.

The literature has studied the third stage closely. We know a great deal about what a warning should feel
like — which modality, which body location, which pattern, how to encode distance. The second stage has
attracted far less attention. In most systems the trigger is a threshold chosen once and reported in a
sentence, if at all.

This matters because the two obvious rules behave differently. A **proximity** rule fires at a fixed
distance and is trivially predictable. A **forecast** rule fires when estimated time-to-contact falls below
a threshold, so it fires earlier for a fast movement than a slow one. Automotive human factors has treated
this as a real design choice for decades, with evidence that warning timing changes not only when drivers
respond but whether they trust the system at all. The intuition is that forecasting should help: it buys
the user time in proportion to how much trouble they are in.

**One published study has tested that intuition in VR, and it came out the other way.** Comparing a
fixed-distance trigger against one scaled by the user's walking speed, the speed-scaled policy produced
*significantly more* collisions and smaller safety margins. If the finding is taken at face value, the
obvious next design move — forecast the contact and warn earlier — makes things worse.

We do not think it should be taken at face value, and neither did its authors. Their two conditions
differed in two ways at once. The speed-scaled condition changed the trigger, and it also made the
vibration's **intensity** a continuous function of distance and speed. Slowing down therefore turned the
warning *down*. Participants discovered this and, in the authors' description, continued walking slowly
forward while constantly adjusting their speed to manage the vibration level. The warning became something
to control rather than something to obey. Whether the policy failed, or the display did, cannot be
recovered from that design.

Separating them requires holding one constant while varying the other, twice. We do that. A **proximity**
and a **forecast** condition deliver a cue that is identical in form, amplitude, duration and body site,
differing only in the moment it fires — so any difference is attributable to timing. A further condition
holds the forecast trigger, the site and the information fixed, and varies only whether the cue is a single
discrete pulse train or a continuously modulated intensity that the user can influence by slowing. Together
these are the two halves of the original confound.

We cross the trigger factor with a second one that prior work could not vary. Warnings about an external
hazard — a vehicle ahead — can only say *something is coming*. A warning about the user's own body can say
*which limb*. We compare a cue delivered to the limb predicted to make contact against an undifferentiated
cue on the torso, holding trigger and cue form constant. No prior work crosses these factors: the closest
studies use head- or torso-mounted actuators in seated driving tasks, where the effector is the vehicle and
the hazard is outside the body.

Our task is reaching and stepping among **real physical obstacles** that are tracked but not rendered, so a
collision is a genuine contact rather than a simulated one, and the warning is the only information the
user has about where the hazard is.

### Contributions

1. **A controlled separation of trigger policy from cue form**, resolving a published result that
   confounded them. Both possible outcomes are informative: either the earlier finding is explained by the
   display, or it survives and the field learns that forecast triggering genuinely underperforms.
2. **The first crossing of trigger policy with somatotopic cue mapping**, testing whether a warning that
   names the at-risk limb helps more when it also arrives earlier.
3. **A per-opportunity analysis of scripted collision opportunities**, in which the trigger times of *both*
   policies are recorded on every opportunity regardless of the condition in force — making the timing
   manipulation a measured quantity rather than an assumed one, and letting the finding be restated as a
   function of the warning time actually delivered rather than of the threshold that produced it.

---

## Notes for revision

**On leading with someone else's negative result.** Unusual, and correct here. It gives the paper stakes a
reviewer sees in two sentences, and it converts the most dangerous objection — *"this was already done"* —
into the reason the paper exists. The treatment is deliberately fair: the confound is named as something
the authors themselves identified and called for follow-up on, not as a flaw they missed.

**Contribution 3 is the one to defend hardest.** It is the least visible and the most reusable. The
counterfactual trigger log turns "we assume the predictive cue arrived earlier" into a measured per-trial
number, and it is what makes §3B (parameter robustness) and §6B (realized-lead dose-response) possible at
all. Do not let it get cut for space.

**Deliberately absent:** presence and workload (secondary, equivalence-framed); H3 (exploratory at 57%
power); any claim about optimal warning time; any mention of Paper 2.

**Check before submission:** contribution 2's "first crossing" claim survived the round-5 novelty sweep and
a full read of Meng et al. (2014), whose timing was constant. If a later sweep turns up a crossing, this
sentence is the one that has to change.
