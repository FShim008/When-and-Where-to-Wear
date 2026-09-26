# Defending `D` = 0.30 m and `T` = 1.0 s

**Paper 1 · created 2026-09-23 · companion to `PAPER1_STUDY_DESIGN.md` §6**

Everything below is written to be pasted into the paper or used verbatim in a rebuttal.
Every citation was resolved against Crossref on 2026-09-23; none is quoted from memory.

---

## 1. The attack this document exists to stop

> *"Where did 0.30 m and 1.0 s come from? These look assumed. The result may be an
> artefact of two arbitrary numbers."*

This is the most likely reviewer objection to Paper 1, and the design is already well
defended against it — but **almost none of the defense is currently visible in the
paper.** It lives in `PAPER1_STUDY_DESIGN.md`, in `PolicyTimingSeparationTests.cs`, and in
columns of `opportunities.csv` that nothing reported. This document moves it into view.

---

## 2. The principle: which burden of proof actually applies

A parameter can play one of three roles, and each carries a different burden:

| Role | The claim | Burden of proof |
|---|---|---|
| **The finding** | "0.30 m is the right distance" | Must sample many values |
| **The operationalization** | "0.30 m instantiates *fire-on-distance*" | Must be representative, and must make the contrast valid |
| **Nuisance constant** | irrelevant to the claim | None |

**Paper 1's `D` and `T` are role 2. The paper never says so.** That silence is what invites
the attack: a reviewer assumes role 1 and holds the paper to a burden it never took on.

The manipulated variable is the **timing policy class** — fire-on-distance versus
fire-on-forecast. The parameter values are how those classes are instantiated, not what is
being claimed about them.

---

## 3. Defense layer 1 — declare the burden (paste into Methods §6)

> We do not claim that `D` = 0.30 m and `T` = 1.0 s are optimal values. They are
> representative operating points of two policy classes — a cue triggered by proximity and
> a cue triggered by forecast time-to-contact — selected so that the timing contrast holds
> in a consistent direction across the movement speeds this task produces. The manipulated
> variable is the policy class, not the parameter value.

Three sentences convert an attack surface into a stated scope limit. Reviewers press hard
on unstated assumptions and rarely on declared ones.

---

## 4. Defense layer 2 — the values are derived, and alternatives were rejected on record

**Move this table out of `PAPER1_STUDY_DESIGN.md` §6 and into the paper.** It is the single
strongest piece of evidence that the values were reasoned about rather than picked.

A proximity policy fires at a fixed **distance** `D`. A predictive policy fires at a fixed
**time** `T`, which for a limb closing at speed `v` is the distance `v·T`. So predictive
leads proximity only while `v > D/T` — and, more restrictively, only when the limb has
enough clear approach distance that the proximity cue is not already firing at movement
onset.

Driving the **real** `CollisionOracle` and `ConditionManager` through minimum-jerk reaches
at 90 Hz across movement times of 0.4–1.4 s gives the clear approach distance each setting
requires before predictive reliably leads by ≥ 50 ms:

| `D` | `T` | Minimum approach distance | Verdict |
|---|---|---|---|
| 0.30 m | 0.50 s | 0.55 m | **Rejected** — inverted at slow movement times |
| 0.30 m | 0.80 s | 0.45 m | Rejected — worse floor, ~25% less lead |
| **0.30 m** | **1.00 s** | **0.40 m** | **Adopted** — task geometry guarantees 0.40 m |
| 0.25 m | 1.00 s | 0.35 m | Rejected — see below |

**Why `D` stays at 0.30 m.** Dropping to 0.25 m buys only 0.05 m of dead-zone reduction
while making the proximity cue less actionable. The reactive condition has to remain a
plausible warning rather than a token one, or H1 degenerates into "useful versus useless"
instead of a comparison of two warning policies.

**Why `T` = 1.00 s and not 0.80 s.** A better approach floor (0.40 m vs 0.45 m) and roughly
25% more lead throughout. The cost is that predictive fires earlier and more often — which
§3 already treats as part of the total effect of a policy and §15 Step 3 measures as alert
burden. It is a declared cost, not a hidden one.

**Both values are also inside published operating ranges.** Valkov and Linsen [2019] ran the
same two policy classes at this venue with `tmin` = 600 ms, `tmax` = 1.6 s and `dmin` = 60 cm
for walking. `T` = 1.0 s falls inside their time range; `D` = 0.30 m follows their own
`v × t_stop` method with a reach-appropriate stopping time (§5B). So the derivation above and
the published literature support the same numbers by independent routes — say both.

**This argument is also executable.** `Tests/EditMode/PolicyTimingSeparationTests.cs` pins
the ordering at the adopted operating point and pins the dead zone that motivates the
opportunity-geometry requirement, so neither can regress unnoticed. A reviewer asking
whether the derivation is real can be told it is a passing test, not a paragraph.

---

## 5. Defense layer 3 — the dichotomy is inherited, not invented

Paste-ready Related Work subsection (~230 words, 12th-grade reading level):

> **Choosing when a warning fires.** Automotive human factors settled the structure of this
> question decades ago. A forward collision warning can fire at a fixed distance or when
> predicted time-to-collision crosses a threshold, and production systems have been built
> both ways; some combine the two criteria [Chen et al. 2013]. The timing itself changes
> behavior rather than merely changing when behavior happens: warnings judged too early are
> distrusted and eventually ignored, and warnings that arrive too late cannot be acted on at
> all [Parasuraman et al. 1997; Abe and Richardson 2006a, 2006b]. Warning timing is
> therefore treated in that literature as a design variable in its own right, with its own
> effects on trust and compliance [Bao and Wang 2024].
>
> Virtual reality has inherited the question. Systems warn users when a tracked body part
> approaches a real object [Valkov and Linsen 2019; Valentini et al. 2020; Kim et al. 2021],
> and a body of work has studied what the cue should be — its modality, pattern, and how it
> encodes distance [Kim et al. 2015]. Valkov and Linsen [2019] compared the two trigger
> policies directly, warning walkers through head-mounted tactors under a fixed-distance
> threshold and under a speed-scaled threshold. The speed-scaled method produced
> *significantly more* collisions. Their explanation is that vibration intensity was a
> continuous function of both distance and speed, so slowing down reduced the vibration:
> participants "continued walking slowly forward while constantly decreasing the speed to
> adjust the vibration level." The trigger policy was confounded with a display mapping the
> user could modulate.
>
> We instantiate the two policies at `D` = 0.30 m and `T` = 1.0 s, both within the published
> operating ranges [Valkov and Linsen 2019], and hold the cue form constant across them: a
> single edge-triggered pulse train per approach, identical in both policies, so no closed
> loop between the user's speed and the signal exists to be modulated.

**Do not write "no prior work compares these."** An earlier draft of this document said
exactly that, and it is **false** — Valkov and Linsen [2019] compared them at this venue in
2019 and got the opposite of Paper 1's H1. That sentence would have been a gift to a
reviewer who knows the paper. See §5B.

---

## 5B. Valkov & Linsen 2019 — read 2026-09-23, and it reframes the paper

**This is the most important related work for Paper 1 and it was not in the design.** Read
in full from the PDF, not from an abstract. Everything below is quoted or computed from it.

### What they did

A within-subjects study, **N = 40**, IEEE VR 2019. Participants walked toward an invisible
virtual wall from 3 / 3.5 / 4 / 4.5 m and tried to stop as close as possible. Vibrotactile
warning came from **ERM actuators in the HMD face cushion** (two top tactors). Five transfer
functions × two control methods:

- **Distance control** — fixed thresholds, `dmin = 60 cm`, `dmax = 160 cm`
- **Speed control** — thresholds scale with speed, `dmin = u·tmin`, `dmax = u·tmax`,
  with **`tmin = 600 ms`, `tmax = 1.6 s`**

### Why this matters enormously

**Their speed control is mathematically identical to Paper 1's predictive policy.** Firing
when `d < u·t` and firing when `TTC < T` are the same rule under constant velocity, since
`TTC = d/u`. They are the same manipulation, in the same venue, seven years earlier.

**And their result went the other way.** Collisions (defined as `du < 0.05 m`): Wilcoxon
signed-rank **Z = −2.23, p = 0.024**, speed control *worse*. They rejected their H1 — which
is Paper 1's H1 almost word for word: *"The number of collisions ... is lower with using the
speed control method when compared to using the distance control method."* Minimum safety
distance was also worse under speed control, **F(1,39) = 17.65, p < 0.01**, mean difference
**−0.151 m**.

### Why their null does not predict Paper 1's

Their own explanation is a **closed control loop**, not a failure of forecast-based timing:

> "the participants also decreased their speed at high vibration levels, but since this
> slowing down also decreased the vibration level, they continued walking slowly forward
> while constantly decreasing the speed to adjust the vibration level."

Because intensity was a **continuous** function of distance *and* speed, slowing down turned
the warning **off**. The signal rewarded creeping forward. They say the method "performed
exactly as intended, but turned out to be difficult to understand," and call for future work
on whether practice or a hybrid approach fixes it.

**Paper 1 has no such loop, by construction.** The cue is edge-triggered — at most one alert
per approach, re-arming only after the limb clears `ReleaseDistance` — and the pulse train
is identical under both policies. Nothing the participant does modulates the signal's
intensity, so the behaviour that broke their speed control cannot occur.

### The comparison table for Related Work

| | Valkov & Linsen 2019 | Paper 1 |
|---|---|---|
| Task | Walk toward a virtual wall | Reach past real obstacles |
| Effector | Whole-body locomotion | Per-limb, four limbs |
| Actuator site | HMD face cushion | Body-localized limb site vs generic chest |
| **Cue form** | **Continuous intensity ramp** `f(d,u)` | **Discrete edge-triggered pulse train** |
| Obstacles | Virtual wall, simulated sensor | Real physical obstacles |
| Trigger policies | Fixed distance vs speed-scaled | Fixed distance vs per-limb TTC oracle |
| Trigger and cue form | **Confounded** | **Separated** — cue form held constant |
| Result | Speed control **worse**, p = .024 | H1 predicts better |

### The reframe — this makes Paper 1's motivation stronger, not weaker

Paper 1 is no longer "nobody has compared these." It is:

> The one published comparison of distance-triggered and forecast-triggered collision
> warning in VR found the forecast policy performed *worse*, and attributed this to a
> continuous intensity mapping that participants could modulate by slowing down. We separate
> the trigger policy from the cue form and test the policy alone.

A motivated replication that fixes an identified confound in a contrary in-venue result is a
**stronger** contribution than an unmotivated first look. It also gives H1 real stakes: both
outcomes are now informative, which is exactly what a preregistered hypothesis should have.

### The risk, stated plainly

A reviewer may ask: *"If you find the opposite of Valkov and Linsen, is it the trigger
policy or the discrete cue?"* Paper 1 answers this because **cue form is held constant
across the Policy factor** — same site, same pulse train, same intensity, only the trigger
differs. Theirs was not. Say this explicitly in the Discussion; do not leave it implied.

### Their derivation method, reused for `D`

Their reasoning is reusable and worth citing as method, not just as a value:

> "the typical walking speed of an IVE user is about 3.5 km/h and ... the average reaction
> time of a user is about 700 ms, we need to set the minimal distance `dmin` to about 70 cm"

That is `dmin = v × t_stop`. Applied to a reach instead of a walk, with a hand closing at
roughly 1.0 m/s and the 150–220 ms hand reaction-time band `E2SessionPlan` targets, plus the
deceleration that follows the reaction, the same arithmetic lands at roughly **0.25–0.32 m**.
**`D` = 0.30 m sits in that band.**

Present this as a plausibility check using a published method, **not** as a derivation — the
deceleration term is an estimate, and Paper 2 is what will measure the arrest time properly.
That is the legitimate, post-hoc role Paper 2 plays for Paper 1 (§`PAPER1_WHY_T_IS_1_SECOND.txt`).

---

## 6. Defense layer 4 — the choice is not load-bearing (`paper1_analysis.R` §3B)

**Implemented and dry-run 2026-09-23.** Writes `p1_param_sensitivity.csv`.

`PolicyTriggerProbe` records, on **every** opportunity and in **every** condition including
`None`, when *both* policies would have fired. Because `trigger_prox_s` and
`trigger_pred_s` are lead times before contact, counterfactual settings reconstruct exactly
under locally constant closing speed:

```
lead_prox(D') = D' / closing_speed_prox_ms
lead_pred(T') = trigger_pred_s + (T' - 1.00)
```

§3B-i **measures** that assumption before using it, by checking whether `D / v_prox`
reproduces the recorded proximity lead. On the fixture the median error is 10 ms (3.3% of
the proximity lead). If it exceeds 25% on real data the sweep is reported as indicative
only, and the script says so itself.

The sweep then reports, across `D` ∈ [0.20, 0.45] m × `T` ∈ [0.70, 1.50] s, the share of
opportunities on which predictive still leads. Fixture result: **35 of 36 pairs** hold the
manipulation within the 5% inversion tolerance.

**Paste-ready claim:**

> The direction of the timing manipulation does not depend on the specific operating point.
> Reconstructing both policies' trigger times across `D` ∈ [0.20, 0.45] m and `T` ∈ [0.70,
> 1.50] s, the predictive policy still fired first on [N] of 36 parameter pairs at an
> inversion rate below 5%.

**The limit, which must be stated with the claim:** this establishes robustness of the
**manipulation**, not generality of the **effect size**. No participant experienced those
other settings, so their outcomes are not recoverable. Overstating this hands a reviewer
exactly the attack it was built to prevent. Checklist item 6 in the script says so.

---

## 7. Defense layer 5 — restate the finding without the parameters (§6B)

**Implemented and dry-run 2026-09-23.** Writes `p1_lead_dose_response.csv`.

The categorical `Policy` factor is replaced by the continuous lead time actually delivered.
The finding then attaches to **warning time**, not to the settings that produced it, and a
reader can map it onto whatever lead their own system achieves. `Policy` is deliberately
absent from the model: the delivered lead *is* the policy's effect, so fitting both is
collinear by construction.

Fixture output — a curve, not a parameter:

| Delivered lead | P(violation) |
|---|---|
| 242 ms | 0.507 |
| 544 ms | 0.465 |
| 846 ms | 0.423 |
| 1048 ms | 0.396 |

**Three limits, all reported by the script rather than left to the reader:**

1. **Observational.** Lead was not randomised within condition; it varies with limb speed
   and opportunity geometry. An association, not a causal dose-response. It does **not**
   supersede the §4 confirmatory model.
2. **Speed-confounded by construction.** For the proximity policy `lead = D/v` is a
   deterministic function of closing speed. The script prints the correlation and a
   speed-adjusted fit beside the unadjusted one so the instability is visible.
3. **No extrapolation.** Predictions are emitted only across the 10th–90th percentile of
   observed lead — the same discipline Paper 2 applies to its psychometric curve.

---

## 8. Defense layer 6 — preregistration and a self-caught failure

- `D`, `T`, `PredictiveReleaseMargin`, `NearMissDistance` and `ContactDistance` are frozen
  and dated in `PAPER1_STUDY_DESIGN.md` §6 (**recorded 2026-09-14**), before any data exists.
- The geometry audit that the `T` change exposed found **4 of 12 opportunities failing** the
  approach requirement — every opportunity targeting O2, whose surface sat 0.04 m from the
  right hand at neutral stance. It was caught by audit and fixed.

**Report the audit failure in the paper.** A self-caught, self-reported defect raises
credibility more than a design that merely looks clean, because it demonstrates the checks
are real rather than decorative.

---

## 9. The residual weakness — state it first, in Limitations

There is one fair criticism left, and pre-empting it costs far less than defending it:

> Each policy is represented by a single operating point. Our results establish that a
> forecast-triggered cue outperforms a distance-triggered cue at these settings; they do not
> establish that this ordering holds at all settings, and we did not attempt to optimise
> either parameter. Mapping the parameter space is future work.

A reviewer who reads this has nothing left to raise. The same weakness discovered by a
reviewer instead reads as something the authors were hiding.

---

## 10. Reviewer question → where it is answered

| Question | Answer lives in |
|---|---|
| "Where did 0.30 m and 1.0 s come from?" | §4 derivation table + the passing test |
| "You assumed these values." | §3 declaration + §8 preregistration date |
| "Is the result an artefact of `T` = 1.0 s?" | §6B dose-response — the finding restated as a curve |
| "Would other settings have reversed it?" | §3B sweep — 35/36 pairs hold |
| "Is distance-vs-forecast even a real design question?" | §5 — automotive FCW, 25 years of it |
| "Did you check the manipulation worked?" | §3 gate 27, measured per opportunity |
| "Why not optimise the parameters?" | §9 Limitations, stated first |
| **"Valkov & Linsen 2019 already did this and found the opposite."** | **§5B — their trigger was confounded with a modulable continuous mapping; Paper 1 holds cue form constant** |
| **"Are your values plausible for a real system?"** | **§11 — `T` = 1.0 s is inside their published [0.6, 1.6] s range** |

---

## 11. Actions

**CLOSED 2026-09-23 — Valkov and Linsen 2019 obtained and read in full.** Findings in §5B.
Both parameters are now anchored to published in-venue values:

| | Paper 1 | Valkov & Linsen 2019 | Status |
|---|---|---|---|
| `T` (forecast threshold) | **1.00 s** | `tmin` = 600 ms, `tmax` = 1.6 s | **Inside the published range** |
| `D` (distance threshold) | **0.30 m** | `dmin` = 60 cm (walking) | Same *method*, reach-appropriate stopping time (§5B) |

`T` = 1.0 s no longer rests on simulation alone. It sits inside an operating range published
at this venue, and the simulation in §4 shows why that particular value suits *this* task's
geometry. Those are two independent supports for the same number.

**STILL OPEN — Valentini et al. 2020** (DOI `10.1109/vr46266.2020.00022`). Check whether they
report a proximity threshold; a second in-venue value would strengthen §4 further. Lower
priority now that §5B is closed.

**STILL OPEN — re-run the novelty search for Paper 1 before submission.** §5B is the reason:
the assumption that no one had compared these policies survived until the PDF was read. Any
remaining "no prior work has…" claim in either paper must be re-checked the same way, against
full text rather than titles and abstracts.

---

## 12. Bibliography — all 12 resolved against Crossref, 2026-09-23

**Warning timing in automotive human factors**

- Chen, Y., Shen, K., Wang, S. (2013). Forward collision warning system considering both
  time-to-collision and safety braking distance. *International Journal of Vehicle Safety*,
  6, 347. DOI `10.1504/ijvs.2013.056968` · 20 citations
- Parasuraman, R., Hancock, P. A., Olofinboba, O. (1997). Alarm effectiveness in
  driver-centred collision-warning systems. *Ergonomics*, 40, 390–399.
  DOI `10.1080/001401397188224` · 175 citations
- Abe, G., Richardson, J. (2006a). Alarm timing, trust and driver expectation for forward
  collision warning systems. *Applied Ergonomics*, 37, 577–586.
  DOI `10.1016/j.apergo.2005.11.001` · 136 citations
- Abe, G., Richardson, J. (2006b). The influence of alarm timing on driver response to
  collision warning systems following system failure. *Behaviour & Information Technology*,
  25, 443–452. DOI `10.1080/01449290500167824` · 33 citations
- Bao, Y., Wang, X. (2024). Optimization of forward collision warning algorithm considering
  truck driver response behavior characteristics. *Accident Analysis & Prevention*, 198,
  107450. DOI `10.1016/j.aap.2023.107450` · 17 citations

**A body-centred anchor for the distance**

- Sambo, C. F., Liang, M., Cruccu, G., Iannetti, G. D. (2012). Defensive peripersonal space:
  the blink reflex evoked by hand stimulation is increased when the hand is near the face.
  *Journal of Neurophysiology*, 107, 880–889. DOI `10.1152/jn.00731.2011` · 132 citations

**Obstacle warning in VR — in-venue**

- **Valkov, D., Linsen, L. (2019). Vibro-tactile feedback for real-world awareness in
  immersive virtual environments. *IEEE VR*, 340–349. DOI `10.1109/vr.2019.8798036` · 17 citations**
  — **The key related work. Read in full 2026-09-23; see §5B.** N = 40. Compared fixed-distance
  (`dmin` = 60 cm, `dmax` = 160 cm) against speed-scaled (`tmin` = 600 ms, `tmax` = 1.6 s)
  triggering of head-mounted vibrotactile warnings during walking. **Speed-scaled triggering
  produced significantly MORE collisions** (Z = −2.23, p = 0.024) and smaller safety distances
  (F(1,39) = 17.65, p < 0.01, mean difference −0.151 m). Attributed by the authors to a
  continuous intensity mapping that participants could switch off by slowing down. Paper 1's
  `T` = 1.0 s falls inside their published `[tmin, tmax]` range.
- Valentini, I., Ballestin, G., Bassano, C., Solari, F., Chessa, M. (2020). Improving obstacle
  awareness to enhance interaction in virtual reality. *IEEE VR*, 44–52.
  DOI `10.1109/vr46266.2020.00022` · 27 citations
- Kim, J., Jeong, H., Kim, G. J. (2021). The effect of 2D stylized visualization of the real
  world for obstacle avoidance and safety in virtual reality system usage. *ACM VRST*, 1–3.
  DOI `10.1145/3489849.3489943`
- Kim, Y., Harders, M., Gassert, R. (2015). Identification of vibrotactile patterns encoding
  obstacle distance information. *IEEE Transactions on Haptics*, 8, 298–305.
  DOI `10.1109/toh.2015.2415213` · 31 citations

**Precedent for measuring a threshold in one paper and applying it in another**

- Steinicke, F., Bruder, G., Jerald, J., Frenz, H., Lappe, M. (2010). Estimation of detection
  thresholds for redirected walking techniques. *IEEE TVCG*, 16, 17–27.
  DOI `10.1109/tvcg.2009.62` · **573 citations**
- Grechkin, T., Thomas, J., Azmandian, M., Bolas, M., Suma, E. (2016). Revisiting detection
  thresholds for redirected walking. *ACM SAP*, 113–120. DOI `10.1145/2931002.2931018` · 161 citations

> Grechkin et al. is titled *Revisiting* because the original thresholds did not transfer
> across conditions. Cite the pair together: it establishes that sequential
> threshold-then-apply is a legitimate and heavily used structure in this venue, **and** that
> the community expects authors to be careful about transfer. That is why Paper 1 keeps `T`
> independent of Paper 2 rather than importing a value from it.
