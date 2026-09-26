# E2 Participant Instruction Script — Paper 2

**Design authority:** `../PAPER2_STUDY_DESIGN.md` §§5, 6, 8, 9 · **Written:** 2026-09-14
**Model:** `../PAPER1_STUDY_DESIGN.md` §8 instruction script — but the *content* is deliberately different; see §B.

> **PRINT THIS. READ IT VERBATIM.** Paraphrasing between participants is an uncontrolled variable on the one
> factor that determines what the primary outcome means. Read once, after the tactor detection check (§6
> stage 3, so the words attach to a sensation already felt) and before practice.

---

## A. The script

### A1 — The task

> *"You'll see a target appear in front of you. Reach out and touch it with your right hand, as quickly as you
> can, then come back to the start position. A new target will appear each time. That's the whole task —
> reach, touch, return."*

### A2 — The two speeds

> *"Sometimes the target will ask for a normal reach. Sometimes it will ask for a fast one. The display will
> tell you which before each target appears, and you'll practise both until they feel natural. Try to hit the
> speed you're asked for — not slower, not faster."*

### A3 — The vibration, and what to do about it

> *"On some reaches you'll feel a short vibration on your arm. It's a collision warning: it means your hand is
> heading into a zone you can't see."*
>
> *"When you feel it, stop and pull your hand straight back, as fast as you possibly can. Don't finish the
> reach. Don't try to curve around anything. Just stop and withdraw. Then return to the start position and
> wait for the next target."*

### A4 — The four things that keep the measurement honest

> *"Four things it's important you know."*
>
> *"**First** — you can't predict where the zones are, and you're not expected to. They move every time.
> Nothing about the target tells you where one is. The vibration is the only information you get."*
>
> *"**Second** — do not slow down waiting for a vibration. Most reaches won't have one. If you hold back to
> catch it, you'll be too slow on the ordinary reaches, and we can see that in the data. Reach at the speed
> you're asked for, every time."*
>
> *"**Third** — sometimes you will feel the vibration and still not be able to stop in time. That is expected.
> It is not a mistake, and it is not something you're doing wrong — some warnings simply arrive too late for
> anyone to stop. When it happens, carry on exactly as before. Don't change your approach."*
>
> *"**Fourth** — you won't get any feedback about the zones. No sound, no score, nothing on screen. You won't
> know whether you entered one, and that's deliberate."*

### A5 — Comfort and stopping

> *"Reaching this many times can tire your shoulder. We'll take scheduled breaks and I'll ask you about
> discomfort between blocks. If you want to stop at any point, for any reason, say so — you don't need to give
> a reason and nothing happens as a result."*

### A6 — Before every block, verbatim and identical

> *"Same as before: reach and touch the target at the speed it asks for. If you feel the vibration, stop and
> pull straight back as fast as you can."*

---

## B. Why it is worded this way

### B1 — It never tells them to avoid the hazards. This is the single most important difference from Paper 1.

Paper 1 says *"stay out of the zones."* **E2 must not**, and a well-meaning operator who adds it has broken
the study.

75% of E2 trials carry a hazard intersecting the direct reach and **no warning**. Their crossing rate *is* the
avoidance probability at zero warning — the curve's measured lower asymptote (§8, "The no-warning baseline
anchors the curve"). If participants adopt a private policy of reaching cautiously around where zones might
be, that anchor rises toward ceiling, the psychometric curve compresses into a range where nothing can be
estimated, and **LT50 and LT80 stop meaning what the paper says they mean.**

`e2_analysis.R` checks this directly and warns below 50%. But a diagnostic that fires after a session is
wasted money. The instruction is where it gets prevented.

### B2 — A3 gives the goal, and for once it also gives the movement

Paper 1 deliberately avoids prescribing a movement, because it studies whether a cue is *useful* and drilling
a stimulus–response mapping would make it a test of training compliance instead.

**E2 inverts that, on purpose.** §8's capacity framing is explicit: the estimand is what the motor system
*can* do, not what a user *chooses* to do, and that is what makes LT80 portable as a design floor. A
prescribed maximum-effort withdrawal is therefore correct here — the variability we want on the x-axis is
timing, not strategy. "Don't try to curve around anything" removes the main alternative response, which would
otherwise produce a trial that is neither a clean stop nor a clean crossing.

### B3 — A4's four points are each defending a specific, named threat

| Clause | Threat it defends against |
|---|---|
| **First** — can't predict, not expected to | Proactive avoidance destroying the zero-warning anchor (B1). Saying *"you're not expected to"* removes the sense that they are failing by not avoiding. |
| **Second** — don't slow down | **Strategic slowing.** §8 locks warning prevalence at 25% specifically to prevent it. Slowing inflates realized lead on every trial, shifting the whole curve left and making the system look better than it is. Standard and required in the stop-signal literature. |
| **Third** — sometimes stopping is impossible | The design *guarantees* failures at the short leads — that is the floor of the psychometric function. Without this sentence, participants interpret those failures as their own error and compensate by slowing, which is threat two arriving by a different route. |
| **Fourth** — no feedback | Prevents a participant inferring hazard geometry across trials and hunting for it. Also stops them scoring themselves, which is the Paper 1 deferred-score safeguard in a different form. |

The third point is the one most likely to be dropped by an operator who thinks it sounds discouraging.
**It is the most load-bearing sentence in the script.**

### B4 — A2 teaches tempo without naming the manipulation

Participants are told about two speeds because §8 requires practice to teach the response windows before
measurement. They are not told that speed is a manipulated variable, nor that the study predicts urgent
reaches need more warning. Block-level pace feedback is given for ordinary trials only (§8), so pace feedback
never leaks warning-trial performance.

### B5 — It does not lie

A3 states plainly that there are invisible zones and that the vibration is a collision warning. What is
withheld is only that hazard placement is procedural and that lead time is manipulated — withheld, not
misrepresented, and restored in debrief. A participant who later learns the full design should find nothing
they were told to be false.

---

## C. Practice — and one open decision

§6 stage 4: *"Practice for E2 until criterion — no measured threshold trials."* The criterion is not yet
frozen. Proposed, to be confirmed at pilot:

| Requirement | Proposed criterion |
|---|---|
| Normal-tempo reaches | 8 of the last 10 inside the normal response window |
| Urgent-tempo reaches | 8 of the last 10 inside the urgent response window |
| Stop response understood | At least 2 successful stops at a long lead, and no trial where they curved around instead of withdrawing |
| Cue detection | Already established at §6 stage 3; re-confirm verbally once |

### ⚠ An open decision the operator must not make alone

The 16 practice trials **are run identically to measured trials and discarded**. Verified in code:
`E2SessionPlan` has no concept of practice at all — `E2SessionRunner` (`:151`, `:213`) simply suppresses the
log for the first `practiceTrials` rows of the ordinary plan. So practice inherits the full randomized lead
distribution, and a participant may meet the short, impossible-to-stop leads *before* they have formed a
stable response.

That is the exact condition A4's third point exists to prevent, arriving before the reassurance can do any
work. Stop-signal practice conventionally uses easy stop signals first.

**Recommendation:** run practice warning trials at the **longest lead level only**, then switch to the full
range for measured trials. This teaches the response without training on the psychometric range.

**This requires a code change — a practice prefix emitted by `E2SessionPlan`, rather than the runner's current
"discard the first N rows" — and must be decided and recorded before piloting. It is not an operator judgement
call on the day.** If it is rejected, record why, and expect a higher early-block failure rate.

---

## D. Answering questions

**Answer freely:** what the vibration means · what to do when they feel it · how long the session is ·
anything about comfort, breaks, or withdrawal · that failures to stop are expected.

**Do not answer; record the question instead:**

- *"Where are the zones?"* → *"They move every time — there's no pattern to find. Just reach normally and
  respond to the vibration."*
- *"Am I doing well?" / "How many did I get?"* → *"There's no score on this task. You're doing it right."*
- *"Does the vibration come earlier sometimes?"* → *"I can't go into how it's timed until the end, but I'll
  explain everything then."* **Log this one — it is expectancy data (§E).**
- *"Should I go slower so I can stop?"* → Re-read A4's second point verbatim. Do not improvise a rationale.

Any question about timing, or any indication the participant has inferred that warning time is manipulated,
is recorded verbatim with the trial index.

---

## E. Expectancy probe — post-session

Paper 1 §8 requires one and Paper 2 has not had one. E2 is less exposed — there is one cue and no condition
tour, so there is less for a participant to form a theory about — but the timing manipulation is still
discoverable across ~90 warning trials, and a participant who has worked it out may be responding to their
model of the study rather than to the cue.

1. Free text: *"What do you think this study was testing?"*
2. Forced choice: *"Did the vibration always arrive at the same point in your reach, or did it vary?"*
   (always the same / varied / couldn't tell)
3. If *varied*: *"Did you change anything about how you reached because of that?"* (yes / no / free text)

Coded by two raters blind to the participant's lead-time sequence. **Preregistered as descriptive, never as a
covariate** — a post-assignment adjustment of exactly the kind the design forbids elsewhere. Report the
distribution in the manuscript, not only in a supplement.

---

## F. Delivery requirements

- Read **verbatim** from this printed card. Never from memory.
- Read **once**, after the tactor detection check and before practice.
- The per-block reminder (A6) is **fixed and identical** every time. Never add emphasis on a block where you
  expect short leads.
- **Log that the script was delivered**, plus any participant question and the answer given, so deviations are
  recoverable.
- If a participant is visibly discouraged by repeated failures to stop, re-read **A4's third point verbatim**
  and log that you did. Do not reassure them in your own words — improvised reassurance is an uncontrolled
  instruction, and the natural improvisation ("try to be a bit quicker off the mark") is a direct instruction
  to change strategy.

---

## G. Debrief (after all measures)

Explain: hazards were virtual and procedurally placed; warning timing was manipulated across trials; some
warnings were deliberately too late to stop; the failures were designed in and say nothing about their
reactions. State what the study is for — establishing how much warning time a VR safety system must give —
and that their data contributes to a published design floor.

Then take the expectancy probe answers, if not already taken.
