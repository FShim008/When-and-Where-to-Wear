# Paper 2 Study Design — Warning Lead Time for Limb Collision Avoidance

**Document status:** Design authority for Paper 2  
**Version:** 2.0 — 2026-09-14  
**Study status:** Design only; not authorized for human data collection  

> ## ⚠ VERSION 2.0 — SCOPE CUT. READ THIS BEFORE ANYTHING ELSE.
>
> **E1 (the tactile stop-signal task) and the tSSRT construct are REMOVED from the confirmatory design.**
> With them go **H2** (tSSRT predicts the threshold) and **H4** (tSSRT improves held-out prediction).
> Paper 2 is now a **single-experiment study: E2 alone — the warning lead-time curve.**
>
> ### Why
>
> 1. **H2 was probably unmeasurable at a feasible sample size.** SSRT test–retest reliability sits around
>    **r ≈ 0.5–0.7**, and Hall, Jenkinson & MacDonald (2022) found selective-stopping SSRT **improved by
>    98 ± 17 ms** between sessions. With predictor reliability ≈0.6 and threshold reliability ≈0.7, the ceiling
>    on any observable correlation is ≈ 0.65; a true association of 0.4 would be observed near 0.26, needing
>    roughly **110 participants** rather than the planned 32–40. §4 already anticipated attenuation and already
>    named this exact fallback — "demote H2 and H4 to exploratory and reframe the paper around H1 and H3."
>    This is that decision, taken deliberately rather than after collection.
> 2. **A null H2 would have been uninterpretable**, because "no relationship" and "unreliable predictor" cannot
>    be separated. That is the worst available outcome: months of work, no answer.
> 3. **E1 was the entire implementation risk.** E2's machinery largely exists and is tested — `Staircase`,
>    `AvoidanceLatencyDetector`, `CollisionOracle`, `PolicyTriggerProbe`, `CueIntensityCalibration`. E1 needed
>    a stop-signal task, a race model, an SSD staircase and movement-onset detection validated to ±10 ms, all
>    from zero, plus stricter tracking and a larger cohort. Cutting E1 removes roughly half the build, and the
>    hardest half.
>
> ### What survives — and it is a complete paper
>
> | Contribution | Status |
> |---|---|
> | Psychometric warning-lead-time curve: **LT50 / LT80 with intervals** | Confirmatory (H1) |
> | Does the requirement change with movement tempo? | Confirmatory (H3a) — tempo lives inside E2 |
> | Kinematic account of how the reach is arrested or redirected | Descriptive mechanism |
> | Sensing-error degradation of timely delivery | Exploratory simulation (§12) |
> | The operating-point / when-not-to-warn deliverable | Design contribution (§16) |
>
> **H1 is an estimation target, which is what makes this robust.** "LT50 = X ms, 95% CI [a, b]" is a result
> whatever the number turns out to be. It cannot fail — only return a wider interval than hoped.
>
> ### Nothing is deleted
>
> All E1/tSSRT material is **retained in place and marked DEFERRED**, not removed. Everything blocking it —
> settled tracker choice, measured haptic latency, characterised timing jitter — is produced by Paper 1's pilot
> as a by-product, and the cut version of Paper 2 delivers the very thresholds a tSSRT study would correlate
> against. It is a **better Paper 3 later than it was a Paper 2 now.** See §7 and §11.
>
> ### The one thing to verify before freezing
>
> §1 previously hedged that the novelty "must be the **connection**" between action cancellation and the curve,
> implying the curve alone was insufficient. A targeted search on 2026-09-14 found **no psychometric
> warning-lead-time function for limb collision avoidance with a haptic cue** anywhere — the driving literature
> operates at 2–5 s TTC on a practised pedal response, and XR manipulates cue *form*, not cue *timing*. That
> supports the reduced novelty statement, **but it is targeted searching, and this project has been wrong that
> way twice** (§19). Run the §1 novelty-freeze procedure before committing.

**Changes in 1.8 — tracking decided by paired pilot comparison** (PI discussion), superseding the 1.5 Vicon
decision. Procedure lives in `PAPER1_STUDY_DESIGN.md` §11; **Paper 2 sets its own, stricter criteria** in §5 —
velocity error at reach speed, simulated LT80 interval width under *measured* error, and movement-onset
detection agreement. A **split outcome is explicitly permitted**: commodity trackers may suit Paper 1 and not
this study, and that must not be overridden for the convenience of one system across both papers. The paired
pilot also supplies the "simulate the psychometric fit under measured error" check §5 already demanded. §12
revised — simultaneous logging is **reinstated for the pilot only**, so degradation can use a measured
commodity error model rather than synthetic noise; the rejection still stands for the confirmatory sessions.


**Changes in 1.7 — single visit with randomized task order**, revising the 1.6 two-visit decision on
participant-burden grounds. Randomized order is mandatory under a single visit: a fixed order would measure
tSSRT rested and the threshold fatigued for every participant, making a positive H2 equally consistent with
fatigue. Documented the cost (tSSRT is post-fatigue for half the sample, inflating variance in H2's weakest
term), the required mitigations (order × tSSRT interaction check, objective inter-task break plus readiness,
go-RT drift index and discomfort ratings, E1 stop trials protected at ≥ 75), and an indicative ~580-trial
budget for a ~95–110 min visit. Also added **§3 "How E1 and E2 differ"** — a methods-section table making
explicit that E1 measures suppression of an unexecuted motor program while E2 measures arrest of a limb in
flight, that this distinction *is* H2's construct, and that E1 cannot be replaced by E2. Session flow (§6)
rewritten for randomized order with the detection check held before the split.


**Changes in 1.6 — remaining open decisions closed.** **Two visits, order randomized**, ≥ 48 h apart (§6) —
this removes the blocking fatigue confound on H2, since both tSSRT and the threshold are now measured rested.
Added the four requirements it creates: randomization logging with an order × tSSRT check, visit-2
cue-calibration re-check, per-visit marker re-placement and co-registration, and a handling rule for
single-visit completers. Sampling is **independent cohorts** — no participant shared with Paper 1 (§2), with
the shared-participant fallback explicitly rejected. **Simultaneous commodity-tracker logging rejected** on
scope (§12). Gate 12 closed; gate 13 rescoped to per-visit duration plus between-visit attrition.


**Changes in 1.5 — tracking decided as Vicon.** Paper 2 drove this decision. Every contribution here depends
on **velocity** — lead-time delivery, `tSSRT` movement-onset detection, and the whole kinematic account — and
differentiating position amplifies noise ~70× at 100 Hz. Filtering cannot rescue it: noise falls as `√N`
while lag grows as `~N/2`, so usable precision would cost hundreds of milliseconds inside an experiment whose
manipulated range is 100–800 ms. §5 rewritten with the quantitative argument and the verification still
required. §12 **restored to its original reference-stream form**, which is stronger than the
bench-characterized error model the inside-out decision had forced, since degradation now applies to real
recorded movement. The optional simultaneous commodity-tracker logging is noted in both places.


**Changes in 1.4 — robustness path closed.** *SUPERSEDED by 1.5: the Vicon decision restored the reference
stream, so §12 returned to its original form.* §12 adopted the **bench-characterized error model** — measure
tracker error once against a controlled known trajectory, then inject the measured error structure into
replayed sessions. Chosen because Paper 1's gate 18 already makes that characterization blocking, so one
campaign serves both papers, and because injecting measured error beats sweeping synthetic Gaussian noise.
Added rig requirements (must span E2's speed range and the task's mounted orientations) and recorded the
rejected alternatives, including the condition under which retaining a reference tracker should be revisited.


**Changes in 1.3 — hardware decision:** flagged that Paper 2 is materially more tracking-sensitive than
Paper 1, because lead time depends on *velocity* and differentiating a noisy position stream amplifies error;
required empirical characterization plus a simulated psychometric fit under measured error before committing
to inside-out tracking, and kept retaining a reference tracker for Paper 2 alone as a live option (§5); gave
§12 three explicit paths now that no clean reference stream is guaranteed.


**Changes in 1.1:** identified the blocking E1 → E2 task-order fatigue confound on H2 and required a design
decision (§6); added a session-duration feasibility gate (§6.1); added provisional sample size with H2 as the
binding constraint (§4); required leave-one-participant-out cross-validation for H4 (§9); added the
operating-point selection framework (§16); added a prior-art differentiation section covering the driving
warning-timing literature (§18); corrected the venue claim.

**Changes in 1.2 — hypothesis audit (§9):** reframed **H1** as an estimation target rather than a directional
test and required a preregistered monotonicity check at long lead times; sharpened **H2** to state explicitly
that it tests generalization from pre-movement inhibition to mid-flight braking, preregistered the null
interpretation, and removed the assumption that the sign is certain; **split H3** — the previous "greater lead
time *or* greater post-cue travel" was a compound hypothesis confirmable two ways, now H3a (confirmatory,
threshold) and H3b (secondary, descriptive, near-arithmetic); required **H4** to enter the power simulation
with demotion to secondary if underpowered, and linked its interpretation to H2; fixed the confirmatory family
and testing sequence (§10); rewrote the publication decision rules to distinguish informative from
uninformative nulls (§17).

**Planned venue:** IEEE ISMAR 2027 — **host city and dates not yet announced**; deadline provisionally expected around March 2027 by analogy with ISMAR 2026 (Bari, 5–9 Oct 2026; abstracts 9 Mar 2026). Verify on `ieeeismar.net` before planning. See `PROJECT_HANDOFF.md` §9.

This document specifies the confirmatory design. It supersedes earlier descriptions of E1/E2/E3 as short extension blocks appended to the Paper 1 session.

## 1. Defensible contribution

### Primary research question

How much **physically realized vibrotactile warning lead time** is required to prevent an already-moving upper
limb from crossing a virtual hazard boundary, and how does that requirement change with movement tempo?

*(Superseded 2026-09-14. The previous question also asked whether an independently measured tactile stopping
latency explains between-person differences in the requirement. That half is deferred — see the v2.0 banner.)*

### Intended contribution

Paper 2 will contribute:

1. A psychometric warning-lead-time curve for an ongoing upper-limb reach — **the headline deliverable, a
   number with an interval that other work can use.**
2. A kinematic account of how the reach is cancelled or redirected after the warning.
3. A test of whether the lead-time requirement changes with movement tempo.
4. A secondary, explicitly simulation-based analysis of how sensing errors degrade timely warning delivery.
5. An operating-point recommendation that treats unnecessary warnings as a real cost (§16).

*(DEFERRED 2026-09-14: "a test of whether tactile stop-signal reaction time predicts a participant's warning
threshold beyond observable movement speed." See the v2.0 banner.)*

### Contribution-to-hypothesis map

Every claim the paper makes must trace to either a confirmatory hypothesis or an explicitly descriptive
analysis. Mixing the two is how exploratory findings get reported with confirmatory confidence.

| Contribution | Status | Where |
|---|---|---|
| 1. Lead-time curve | Confirmatory — **estimation**, not a directional test | H1 (§9) |
| 2. Kinematic account of cancellation/redirection | **Descriptive. No hypothesis.** | §8 detector, §9 Mechanism |
| 3. Requirement changes with tempo | Confirmatory | H3a (§9); H3b descriptive |
| 4. Sensing-error degradation | Exploratory simulation | §12 |
| 5. Operating-point recommendation | **Decision rule**, never a hypothesis test | §16 |
| ~~tSSRT predicts the threshold~~ | **DEFERRED 2026-09-14** — not in this paper | §7, §11 |

Contribution 2 carries no hypothesis by design — it characterizes *how* the reach is arrested rather than
testing a prediction about it. Report it descriptively with intervals, and do not attach significance claims
to post-hoc comparisons within it.

### Claims that are outside scope

The study does **not** by itself establish that:

- a personalized warning system outperforms a fixed system;
- vibration is superior to an auditory or visual warning;
- an upper-limb estimate generalizes to feet, torso, locomotion, or whole-body avoidance;
- a virtual boundary crossing represents an injury or real-world collision probability;
- LT50 is a safe operational threshold; or
- a simulated tracking-noise level is a universal hardware accuracy requirement.

**REVISED 2026-09-14.** The novelty claim is the **lead-time curve itself**: a psychometric threshold for how
much warning an already-moving limb needs in order to avoid a registered hazard, together with the kinematic
account of how the reach is arrested and the tempo dependence of the requirement.

A targeted search found nothing comparable — the driving literature operates at 2–5 s time-to-collision on a
practised single-degree-of-freedom pedal response, and the XR literature manipulates cue *form* (patterns,
belts, boundary visualisations) rather than cue *timing* as a fitted threshold. Prior work on tactile stop
signals and whole-arm countermanding establishes the *methods*, not this question.

### Novelty search record — RUN 2026-09-14

Required by `PAPER1_STUDY_DESIGN.md` §1 step 2: record databases, strings, date, screening criteria and counts
so the search can be reported and re-run.

**⚠ What this is, and what it is not.** This is a **recorded scoping search using general web search**. It is
**not** a database systematic review: no structured Boolean queries against Scopus, Web of Science, ACM DL,
IEEE Xplore or PubMed; no result counts; no PRISMA screening; no second screener. It materially reduces risk
and it is reportable as a scoping search — it is not sufficient for a claim of systematic coverage.
§1 step 4 (re-run shortly before submission) remains outstanding, and the database pass in
"What remains" below must happen before the novelty statement is final.

**Claim under test:** *No prior work fits a psychometric function relating physically realized warning lead
time to avoidance probability for an already-moving limb approaching a hazard.*

**Inclusion criteria for a disconfirming hit:** (a) a warning or cue delivered at a **controlled time before a
predicted contact**; (b) the effector is a **limb already in motion**, not a pedal, button, or whole-body
locomotion; (c) the outcome is **avoidance success**; and (d) a **threshold or curve is fitted** across lead
times, rather than two or three levels compared.

**Queries run (12), 2026-09-14:**

| # | Query | Outcome |
|---|---|---|
| 1 | `"warning lead time" psychometric function threshold avoidance probability fitted curve` | Psychometric *methodology* only (curve-fitting literature). No applied hit. |
| 2 | `time-to-collision warning threshold psychophysical dose-response curve avoidance success probability` | Driving TTC literature. See near-neighbour 2. |
| 3 | `vibrotactile cue timing arrest ongoing reach upper limb virtual reality how much warning needed` | Vibrotactile limb-state feedback and rehabilitation. No advance-warning timing. |
| 4 | `stimulus onset asynchrony movement cancellation probability reaching arm psychometric curve stop` | Stop-signal literature (SSD staircases). No lead-time-to-avoidance curve. |
| 5 | `ISO 13855 safety distance approach speed hand 1600 mm/s stopping time warning human reaction standard` | **Closest engineering precedent.** See near-neighbour 1. |
| 6 | `teleoperation robot proximity warning operator advance notice timing how early collision haptic study` | Time-to-impact-proportional haptic force. See near-neighbour 3. |
| 7 | `vibrotactile obstacle warning visually impaired how far in advance timing threshold detection distance user study` | Direction/distance encoding and sensor range. No lead-time threshold. |
| 8 | `"how much warning" OR "warning time required" stop hand movement before contact experiment milliseconds` | Generic hand RT (~150–220 ms); saccadic countermanding. Nothing fitted. |
| 9–12 | Earlier same-day searches: psychometric warning lead time for reaching with haptic cue; VR haptic warning lead-time manipulation; SSRT predicting warning timing; tactile stop-signal SSRT | No disconfirming hit. |

**Result: no disconfirming hit.** Nothing found satisfies all four inclusion criteria.

### Three near-neighbours that must be addressed in related work

**1. ISO 13855 — machine-guarding safety distances. The most important one, and it was not previously in §18.**
The standard positions safeguards using `S = K × (t1 + t2) + C`, with `K` = 2000 mm/s for hand approach
(1600 mm/s for whole-body) and `t1` = human reaction time. **This is the field's existing answer to
"how much time does a hand need," and it is a conservative constant rather than a measured distribution.**

Three clean differentiators, and the third is the strongest:
- It yields **no avoidance probability** — no curve, no interval, no operating-point trade-off.
- `K` is a **worst-case constant**, not the measured speed distribution this study reports across tempo.
- **It assumes the *machine* stops while the human keeps moving.** In VR there is nothing to stop but the
  person. Paper 2 measures the human-stopping side that ISO 13855 approximates with a fixed `t1`.

Frame it as *supplying the human-factors number that an existing safety standard currently has to assume.*
That makes the paper **more** relevant to practice, not less — and it is a much better framing than pretending
the standard does not exist.

**2. Driving TTC and crash-probability curves.** The *concept* of a probability-versus-TTC curve exists in the
driving literature, which describes it as difficult to define because it is individualistic and
situation-dependent. Differentiate on timescale (**100–800 ms versus 2–5 s**), effector (limb mid-trajectory
versus a practised single-degree-of-freedom pedal response), and on the fact that this curve is **fitted from
data** rather than assumed. Already partly covered in §18 under Abe & Richardson; add the curve point.

**3. Teleoperation time-to-impact haptics.** Haptic force scaled to time-to-impact exists and reduces operator
collisions. It is a **continuous force gradient**, not a discrete warning at a controlled lead time, and no
threshold is fitted. Differentiate on the manipulation, not the domain.

### Frozen novelty statement — PROVISIONAL, dated 2026-09-14

> We report the first psychometric characterisation of **warning lead time** for arresting an ongoing upper-limb
> reach before a registered hazard: the lead time at which avoidance succeeds, its dependence on movement
> tempo, its variability between people, and the avoidance-versus-unnecessary-warning trade-off it implies.

**Provisional until the database pass below is complete.** If a disconfirming hit appears, §1 step 5 applies:
reframe rather than argue — the tempo dependence, the operating-point surface, the counterfactual-validation
method, and the threshold-reliability result all survive as contributions even if the curve itself does not.

### Database pass with recorded counts — RUN 2026-09-14

**Databases queried programmatically:** OpenAlex (~250M works; indexes ACM, IEEE, Springer, Elsevier),
PubMed E-utilities, Crossref. OpenAlex field: `title_and_abstract.search`. Counts are live API results.

**Round 1 (five-facet Boolean) was discarded.** OpenAlex relevance-ranks rather than strictly AND-ing, so a
five-facet query returned 106 hits dominated by astronomy, volcanology and nephrology — the facet term
"boundary" matched "boundary layer". Round 2 used phrase-anchored queries tight enough that **every hit could
be screened**. Recording this because a reader should know the first attempt failed and why.

**Round 2 — targeted queries, all hits screened:**

| # | OpenAlex query (`title_and_abstract.search`) | OpenAlex | PubMed |
|---|---|---|---|
| Q1 | `"warning lead time"` | 346 | 10 |
| Q2 | `"lead time" AND collision AND reaching` | 7 | 1 |
| Q3 | `"time to contact" AND warning AND hand` | **0** | 0 |
| Q4 | `vibrotactile AND warning AND "time to collision"` | 3 | 2 |
| Q5 | `"psychometric function" AND warning` | **0** | 1 |
| Q6 | `"virtual reality" AND (collision OR obstacle) AND (warning OR alert) AND timing` | 2 | 2 |
| Q7 | `"collision warning" AND "reaction time" AND arm` | **0** | 0 |
| Q8 | `warning AND "movement onset" AND threshold AND reach` | 1 | 1 |
| Q9 | `haptic AND warning AND advance AND obstacle AND threshold` | 1 | 0 |
| Q10 | `"stopping distance" AND hand AND safety AND warning` | **0** | 0 |

*(Crossref was queried but its `query.bibliographic` endpoint OR-matches and returned ≥ 10⁶ for every query. It
is unusable at this granularity and its counts are excluded rather than reported as if meaningful.)*

**Four zeros carry the argument.** Q5 in particular: **no work in OpenAlex fits a psychometric function to a
warning manipulation, in any domain.** Q3, Q7 and Q10 close the limb-scale variants.

**Q1 is a terminology finding, not a hit.** All 25 screened titles are **natural-hazard early warning** —
tornado, flood, tsunami, earthquake — operating at minutes-to-hours. **The phrase "warning lead time" is owned
by that field.** Two human-factors exceptions appear, both connected-vehicle driving (see below).
*Practical consequence:* the manuscript should define the term on first use and consider
"warning lead time for limb avoidance" in the title/keywords, or it will be mis-indexed alongside flood
forecasting.

### The closest hit found, fully screened — Zhang & Wu (2018)

**Zhang, Y. & Wu, C. (2018).** "Modeling the effects of warning lead time, warning reliability and warning
style on human performance under connected vehicle settings." *Proc. HFES 62nd Annual Meeting.*

Manipulated **warning lead time (2.5 s vs 4.5 s) × reliability (73% vs 89%) × style (command vs notification)**.
Found command warnings beat notification warnings at 2.5 s but not at 4.5 s.

**Screening verdict: NOT disconfirming.** It fails two of the four inclusion criteria — **two levels, not a
fitted curve**, and a **pedal response in driving**, not a limb already in flight. Timescale is 2.5–4.5 s
against this study's 100–800 ms.

**But cite it prominently.** It is the closest existing manipulation of warning lead time in human factors, it
establishes the construct's legitimacy, and its lead-time × style interaction is the direct precedent for this
study's lead-time × tempo interaction. It is also the strongest existing support for the deferred false-alarm
study in `docs/FOLLOW_UP_STUDY_IDEAS.md` §2, since it crosses lead time with reliability.

### ISO 13855 citation chase — RUN 2026-09-14, and it found something useful

| Query | OpenAlex count |
|---|---|
| `"ISO 13855" OR "EN 999"` | 35 — the standard documents themselves (ISO 13855:2010/2022/2025, EN 999:1998) |
| `"approach speed" AND (machinery OR safeguard OR "light curtain")` | 7 — standards again |
| `"hand speed" AND "safety distance"` | **0** |
| `"approach speed" AND human AND measurement AND guard` | **0** |

**The provenance of `K` = 2000 mm/s is not traceable through open bibliographic databases.** Vendor
documentation describes it as based on "empirical data," but no primary study appears in OpenAlex or PubMed.
It likely sits in the standard's own annexes and in German institute grey literature (IFA/BGIA), which is
outside these databases.

**That is a finding, and it strengthens the paper.** The number industrial safety uses for how fast a hand
approaches a hazard is an **engineering convention whose empirical basis is not in the open literature**.
Paper 2 supplies a measured, published, interval-bounded alternative for the adjacent quantity — how much
warning that hand needs. Say exactly that, and do not overclaim: this study measures warning-response
capacity, not approach speed, so it complements rather than replaces `K`.

**Outstanding:** obtain ISO 13855:2010 Annex A and EN 999:1998 directly and check whether they cite primary
sources. A library copy settles it; open databases cannot.

### Adversarial second screen — RUN 2026-09-14

§1 requires a second screener on borderline hits. **There is one screener on this project, so a genuine
independent second screen was not possible.** The substitute, recorded as a substitute: eight further queries
written deliberately to *disconfirm* the claim — phrasings a hostile reviewer would use — rather than to
confirm it.

`how early / advance notice + avoidance` (10,160) · `cue latency + stop + movement + threshold` (33) ·
`warning timing + dose-response/sigmoid/logistic` (24) · `"minimum warning time" OR "required warning time"`
(25) · `time budget + avoid + limb` (112) · `wearable proximity alert + timing + threshold` (1) ·
`psychophysics + alarm + threshold` (476) · `"lead time" + avoidance + probability + curve`.

**No disconfirming hit.** High-count queries resolve to natural-hazard warning, spacecraft collision-avoidance
manoeuvres, driving takeover time, saccadic countermanding, and unrelated noise. The one adjacent item worth
noting is *"The timing of control signals underlying fast point-to-point arm movements"* (**Exp Brain Res**,
2001) — internal control-signal timing, not warning response, but a reasonable methodological anchor for the
kinematic section.

### ACM DL / IEEE Xplore pass — RUN 2026-09-14, with a failure to report

**Direct programmatic access to both is blocked**, verified 2026-09-14: `dl.acm.org` returns **HTTP 403**,
`ieeexplore.ieee.org` returns **HTTP 418**, DBLP serves a bot challenge, and Semantic Scholar rate-limits
without an API key. A true interface search is therefore a **manual action item**, not something that was
completed here.

**The attempted substitute failed its own positive controls, and the result is discarded.** OpenAlex can
filter by publisher lineage, which looked like an ACM-DL / Xplore equivalent. Against eight core queries it
returned **0 hits for ACM on every one** and 0–3 for IEEE. Before reporting that as evidence, it was checked
against papers known to exist:

| Positive control | OpenAlex, all | filtered to ACM | filtered to IEEE |
|---|---|---|---|
| `redirected walking` | 849 | **12** | 79 |
| `haptic feedback virtual reality` | 4499 | **31** | 241 |
| `cybersickness` | 2518 | **28** | 105 |
| `belt whistles lower body collision awareness` (a **CHI 2026** paper) | 1 | **0** | 0 |

**The publisher filter misses most ACM conference content** — OpenAlex frequently attributes proceedings to a
per-year conference source without resolving publisher lineage. Twelve redirected-walking papers in ACM, and
zero for a paper that is definitively in the ACM DL, make the zeros uninterpretable. **They are not reported
as evidence of absence.**

**What the controls did establish is more useful: unfiltered OpenAlex covers this literature.** It found the
CHI 2026 paper, returns a plausible 849 for redirected walking, and its result venues include *IEEE
Transactions on Haptics*, *IEEE TVCG*, *IEEE VR* proceedings and *Frontiers in Virtual Reality*. The Q1–Q10
pass above therefore already searched ACM and IEEE content — just not through those publishers' own
interfaces.

### The closest hit in the entire search — found only by venue inspection

Re-running the core concept unfiltered and **reading the venue of every hit** surfaced a paper that the
keyword queries had not:

**Bajpai, A., Powell, J. C., Young, A. J. & Mazumdar, A. (2020).** "Enhancing Physical Human Evasion of Moving
Threats Using Tactile Cues." *IEEE Transactions on Haptics* 13(1), 32–37. doi:10.1109/TOH.2019.2962664

A VR environment simulates objects moving rapidly toward the participant, who must **physically move their
body out of the path before collision**. Tactile, audio and visual cues are compared on **failure rate and
reaction time**.

**Screening verdict: NOT disconfirming.** It fails three of the four inclusion criteria:
- **(a) controlled lead time** — no. It manipulates *modality*, not warning timing.
- **(b) limb already in motion** — no. Whole-body evasion from a starting posture, not arresting a limb in
  flight.
- **(d) fitted threshold** — no. A modality comparison, not a curve.

**It must nonetheless be cited prominently, and the scenario inversion is the cleanest differentiator in the
paper.** Bajpai et al. have a **threat moving toward a stationary person**; this study has a **person moving
toward a stationary hazard**. Those are different control problems: theirs is reaction-time-limited
initiation, this one is arrest of an already-committed movement. State it that way.

**This is also the strongest argument for finishing the manual pass.** The single closest paper in the entire
search was invisible to ten keyword queries and appeared only on venue inspection. Assume others are hiding
the same way.

### ACM / IEEE publisher-scoped pass — COMPLETED 2026-09-14 by a route that works

**Crossref DOI-prefix filtering.** ACM registers DOIs under **10.1145**, IEEE under **10.1109**, and Crossref
is where both register them — so `filter=prefix:` gives **true publisher-scoped coverage** of the same
catalogues the two interfaces serve. Unlike the OpenAlex publisher filter, this needs no positive-control
caveat: it is the registration record itself.

**Routes attempted, in order, with outcomes recorded:**

| Route | Result |
|---|---|
| `dl.acm.org` direct | **HTTP 403** |
| `ieeexplore.ieee.org` direct | **HTTP 418** |
| DBLP API | bot challenge |
| Semantic Scholar API | **HTTP 429**, twice, with backoff |
| OpenAlex `publisher_lineage` | **failed positive controls — discarded** (see above) |
| OpenAlex venue sources | fragmented; ISMAR indexed at only 65–215 works per source — unreliable |
| **Crossref `filter=prefix:`** | ✅ **worked** |
| **Crossref `query.container-title`** (venue browse) | ✅ **worked — and this is what actually found things** |

**Ten core queries × two publishers, top 50 by relevance screened each.** Crossref is relevance-ranked with no
Boolean, so counts are not meaningful and are not reported; the method is to screen every returned title.

**Venue browse across ISMAR, IEEE VR/3DUI, TVCG, IEEE ToH, CHI, UIST, SUI and VRST.** This replicates the
manual proceedings scan in `docs/PAPER2_MANUAL_SEARCH_PROCEDURE.md` Part 3 and is where every new find came
from — exactly as predicted after Bajpai.

### What the venue pass found — 15+ papers, none disconfirming

**The single most important observation, and it is worth stating once plainly:**

> **Every VR/AR collision-warning paper found manipulates *what the cue is* — modality, pattern, placement,
> visualization — or *whether it appears at all*. Not one manipulates *when it appears*, across multiple
> levels, with a fitted threshold.** That is the gap this paper occupies, and the gap is now evidenced rather
> than asserted.

**Screened and cleared (selection):**

| Work | Venue | Why it does not disconfirm |
|---|---|---|
| **Bloomfield & Badler (2007)**, *Collision Awareness Using Vibrotactile Arrays* | **IEEE VR**, Honorable Mention | Tactor sleeve on the **arm**; fires **on collision**, not in advance. No lead time, no threshold |
| **Ahmed, N. (2025)**, *Context-Aware Collision Warning Modalities on Dual-Task Performance* | **ISMAR-Adjunct** | A **framework proposal**, not a completed study; manipulates modality, not timing |
| Bachmann, Holm, Zmuda & Hodgson (2013) | IEEE VR | Collision **prediction algorithm** for two co-located users; no human timing study |
| Kim, Song, Lim & Yoon (2024) | UIST Adjunct | Peripheral-vision visualization for diminished reality |
| Barros & Lindeman (2012) | 3DUI Poster | Vibrotactile **modes** for proximity in robot teleoperation |
| SafeAR (2019) · Obstacle Avoidance in Real Space (2018) | ISMAR | Detection and display methods |
| Dynamic FoV + Physical Obstacle Avoidance (2019) · Improving Obstacle Awareness (2020) | IEEE VR | Visualization and FoV manipulation |
| Unobtrusive Obstacle Detection (2018) · RealityAlert (2018) | SUI | Detection and notification design |
| 2D Stylized Visualization for Obstacle Avoidance (2021) | VRST | Visualization |
| Asymmetric Design … Room-scale Multiple Users (2016) | UIST | Multi-user layout design |
| Warning Drivers … Vibrotactile Flow (2016) · Tactile & Multisensory Spatial Warnings (2008) · Vibrotactile Patterns Encoding Obstacle Distance (2015) | IEEE ToH | Driving or distance-encoding; cue **form**, not lead time |

### Two findings that change what you should do

**1. Bloomfield & Badler (2007) must be cited, and it belongs to Paper 1 as much as Paper 2.** A vibrotactile
sleeve delivering **arm-localized** collision feedback in VR, at IEEE VR with an Honorable Mention, is the
direct ancestor of Paper 1's somatotopic-versus-generic contrast. Neither paper currently cites it. It does
not threaten either claim — its cue fires *on contact*, so it is feedback rather than warning — but a reviewer
who knows the VR haptics literature will expect to see it.

**2. ⚠ Someone is working this space at ISMAR right now.** *Ahmed, N. (2025)*, ISMAR-Adjunct: "Investigating
the Impact of Context-Aware Collision Warning Modalities on Dual-Task Performance in Immersive Environments."
It is a framework proposal in the adjunct track, so it is **not** prior art that blocks anything — but adjunct
framework papers are how full papers start.

**Checked 2026-09-14, and the result is reassuring.** The author is **Nasim Ahmed (Kennesaw State
University)**, and the ISMAR 2025 item is a **Doctoral Consortium** submission — a proposed thesis direction,
not a completed study. A second, related item was found:

> Ahmed, N., Choi, T., Zhang, X., Islam, S. & Islam, R. (2025). "Towards Safer Mixed Reality with Auditory
> Warnings for Uncontrolled Indoor Settings." *IEEE VR Abstracts and Workshops (VRW)* 2025, pp. 307–309,
> doi:10.1109/VRW66409.2025.00071.

**No full-paper version was found at ISMAR 2026.** Both items to date are workshop or consortium length.

**Their axis is modality, not timing.** Auditory versus visual versus vibrotactile, evaluated under dual-task
load for situational awareness. That is a **neighbouring** dissertation, not a competing one — and its
existence argues the problem space is real while leaving the timing question open.

**Two things to do, neither urgent:**
1. **Set a citation alert** on Nasim Ahmed and on Rifatul Islam (the apparent advisor). A full paper from that
   group is plausible at ISMAR 2026–2027 or IEEE VR 2027, and a concurrent submission would need disclosing.
2. **Cite the VRW 2025 paper** in related work as current activity on warning *modality* in MR, immediately
   before stating that timing is unaddressed. Positioning against live work is stronger than positioning
   against a decade-old citation.

**And the crowding is now measurable.** The ISMAR 2025 proceedings alone contain at least six papers on
situational awareness, visual cue density, spatial-awareness guidance, scene awareness, and MR safety zones.
**Awareness and modality are busy; warning timing is empty.** That is the single clearest argument for leading
with timing in the title, the abstract, and the first paragraph of the introduction.

### What still remains
1. **Eyeball confirmation on `dl.acm.org` and `ieeexplore.ieee.org` themselves.** The Crossref pass covers the
   same catalogues and found what venue browsing was supposed to find, so this is now **confirmation rather
   than discovery** — an hour, not a day. Procedure and paste-ready queries:
   `docs/PAPER2_MANUAL_SEARCH_PROCEDURE.md`.
2. **Scopus / Web of Science** if institutional access is available, for counts a reviewer may expect.
3. **ISO 13855:2010 Annex A and EN 999:1998** read directly for the provenance of `K`.
4. **Re-run before submission** (§1 step 4) and record the date.
5. **A genuine second screener** — a colleague screening the Q1–Q10 title lists blind to the hypothesis. The
   adversarial substitute above is weaker and is reported as such.

**Raw logs:** `docs/PAPER2_NOVELTY_SEARCH_LOG_round2.txt`, `docs/PAPER2_NOVELTY_SEARCH_LOG_round3.txt`,
`docs/PAPER2_NOVELTY_SEARCH_LOG_acm_ieee.txt`.

## 2. Program structure and independence from Paper 1

Paper 2 is a standalone experiment with its own preregistration, power analysis, participants, trial data, hypotheses, and inferential tests.

**DECIDED 2026-09-07 — independent cohorts.** Paper 2 recruits a cohort that **does not participate in
Paper 1**. No participant, trial, outcome, or inferential test is shared between the two papers.

This is the strongest available position on two fronts at once. Scientifically, it removes any possibility of
carryover, learning, or condition-selection contamination between the studies. For publication, it lets the
ISMAR disclosure letter state independence outright (Option A in `ISMAR_DISCLOSURE_LETTER.md`) rather than
explaining a shared sample — which is what makes the two papers unambiguously separate studies rather than one
study divided.

**Cost, stated plainly:** total recruitment is the sum, not the maximum — roughly 36–48 for Paper 1 plus
32–40 for Paper 2, each attending a single visit. Budget, ethics application, and timeline must reflect the
full figure.

The previously documented fallback — same participants in separate visits ≥ 48 h apart — is **rejected** and
should not be reintroduced without updating the disclosure letter to Option B and recording the change.

Paper 2 must not select its cue, threshold range, or hypothesis after inspecting Paper 1 results. The localized cue on the tested arm is chosen a priori because it provides an unambiguous stop instruction for that effector.

The earlier proposal to run tSSRT before and after a long Paper 1 session is removed from the confirmatory design. A pre/post change cannot isolate fatigue from learning, habituation, motivation, and order. Fatigue may be described only as exploratory unless a randomized fatigue/control experiment is added.

## 3. Constructs and terminology

The following quantities must never be treated as synonyms:

| Quantity | Operational meaning |
|---|---|
| ~~`tSSRT`~~ | **DEFERRED 2026-09-14** — latent latency of tactile cancellation from the stop-signal race model. Not measured in this paper. |
| Movement onset | First sustained departure from the home position/velocity criterion |
| Kinematic response onset | First sustained, model-detected deceleration or trajectory deviation after physical cue onset |
| Arrest time | Physical cue onset to minimum/zero outward velocity |
| Post-cue travel | Distance traveled along the hazard approach direction after physical cue onset |
| `LT50` | Warning lead time associated with 50% avoidance in the psychometric model |
| `LT80`/`LT90` | Model-estimated lead time associated with 80%/90% avoidance, reported only when supported by the observed range and uncertainty |

tSSRT is an estimate of a hidden stopping process; it is not the literal time at which the arm becomes stationary.

### How E1 and E2 differ — ⚠ DEFERRED AT v2.0, retained for a future two-task study

> E1 is not run in this paper, so there is no comparison to draw and nothing to defend in the methods. The
> table below is kept because it is the clearest statement of *why* the two tasks measure different things —
> which is exactly what a revived tSSRT study would need to argue on day one.

The two experiments are **deliberately matched** on apparatus, cue waveform, cue site, intensity, home region,
and warning prevalence. Everything except the manipulated dimension is held constant. That is what makes the
comparison interpretable — and it also makes the tasks look alike at a glance. Spell out the difference
explicitly:

| | E1 | E2 |
|---|---|---|
| Cue can fire **before** movement onset | **Yes** — often does | **No.** Eligible only after confirmed movement onset |
| What the cue demands | Withhold or abort initiation | Arrest and withdraw a limb already in flight |
| Hazard present | No — target only | Yes — invisible volume intersecting the direct reach |
| Measured response | **Movement onset** (leaving the home region) | **Boundary crossing** (target hand enters its volume) |
| Timing referenced to | Target onset (SSD) | Predicted contact (lead time) |
| Timing schedule | Adaptive staircase → ~50% stop success | 5–7 fixed levels spanning ~10–90% avoidance |
| Estimand | `tSSRT`, a latent latency | Psychometric threshold (LT50, LT80) |

**The decisive row is the first.** E1 measures suppression of a *prepared but unexecuted* motor program.
E2 measures deceleration of a limb *carrying momentum*. These are plausibly distinct processes, and **H2 is
the test of whether they are related** — that is the hypothesis, not an assumption behind it.

**E1 cannot be replaced by E2.** E2 contains no pre-onset cues, so there is no go/stop race to model; and
E2's warning trials are the dependent variable, so deriving the predictor from them would be circular. E1 is
the independent measurement, and that is its only job.

**Anticipated objection, and the answer.** A reader can attack either outcome — a strong H2 as "the same task
twice," a null H2 as "two unrelated tasks." Both objections dissolve once the tasks are shown to differ on a
single named dimension that *is* the construct under test. Present this table in the methods so the framing is
established before results, rather than defended after them.

## 4. Participants, sampling, and stopping rule

### Population

- Adults permitted by the approved ethics protocol.
- Normal or corrected-to-normal vision.
- Able to detect the calibrated tactile cue at the tested site.
- Able to perform repeated reaches without pain or a relevant upper-limb limitation.
- Additional HMD, cybersickness, skin-sensitivity, implanted-device, and haptic contraindications as required by the ethics board and device guidance.

The confirmatory scope is healthy adults performing reaches with one tested upper limb. Handedness and tested arm must be recorded. The default is the dominant arm unless the preregistration specifies another sampling strategy.

### Sample size

No sample size is inherited from Paper 1. Determine it by simulation for the **hierarchical psychometric
threshold and its tempo dependence** — H1 and H3a.

**v2.0 — the binding constraint has changed, and loosened.** It was H2, a between-participant regression whose
power was governed by participant count and by the reliability of two noisy estimates. With H2 deferred, the
confirmatory targets are **within-participant**: a psychometric slope and threshold estimated from 80–100
warning trials per person, plus a tempo contrast estimated within the same participants. Those are well
determined at modest sample sizes.

Power the simulation on **threshold interval width**, not on detecting a correlation: choose N so that the
LT50 (and LT80 where the range supports it) credible interval is narrow enough to be useful as a design
number. That is the deliverable, so that is what the sample size should buy.

**Implemented 2026-09-14: `Analysis/e2_power_analysis.R`.** Targets the LT80 interval width for H1 and
conventional power for H3a, over N = 12–40. Two details worth knowing before reading its output:

- **It simulates on *achieved* leads, not assigned ones.** Delivery runs systematically short and the achieved
  spread is ~18% narrower than the assigned spread (`Core/E2/E2SessionPlan.cs`), which costs leverage on the
  curve. Simulating on assigned leads would overstate precision.
- **Its interval is a delta-method linearisation**, because a power simulation needs thousands of fits. The
  confirmatory analysis uses bootstrap or posterior intervals (§10), which are usually wider — so a marginal
  N from this script should be read as optimistic.

Every parameter in it is a guess, `SD_THRESHOLD` most of all. Re-run from pilot estimates before freezing N.

Workflow:

1. Run an engineering/usability pilot that is not used for hypothesis testing.
2. Estimate plausible trial loss, psychometric slope, and between-participant threshold variance.
3. Simulate the exact hierarchical model and missing-data rules.
4. **Target an interval, not a power level, for H1** — choose N so the LT80 credible interval is narrower
   than a preregistered useful width (a sensible starting point: **± 75 ms**, i.e. narrow enough to separate
   candidate operating points). Choose ≥ 80% power for **H3a**. Round upward for expected exclusions.
5. Pre-register a fixed analyzable sample target and a maximum recruitment target. Do not use optional stopping based on significance.

Because individual prediction is central, the design must prioritize reliable participant measurements. For planning, E1 should contain approximately 75–100 stop trials; the final count is set by the pilot and simulation.

### Provisional planning figure — for ethics and budget only

Simulation sets the final number, but the ethics application and budget need one now. Use a **provisional
planning target of 32 analyzable participants, with a maximum recruitment target of 40**, labeled provisional
in the ethics application.

**v2.0 — the binding constraint is now LT80 interval width and H3a, not H2.** Both are driven largely by
*trials per participant* rather than by participant count, because both are estimated within person. That is
the practical consequence of the cut: **more trials per person buys more than more people**, up to the session
ceiling.

Two things still argue for not shrinking N too far: the between-participant threshold SD (§10) needs enough
participants to be estimated at all, and the tempo contrast borrows strength across participants. A
**provisional planning target of 24 analyzable, 30 maximum** is a reasonable starting point for the ethics
application — lower than the old 32/40 because the between-participant regression is gone — but it is
provisional until the simulation runs, and it must be labelled as such.

Two consequences for the simulation — ⚠ **SUPERSEDED AT v2.0**, retained as the record of why H2 was cut.
The attenuation argument below is exactly the reasoning that led to deferring H2; it is no longer a live
simulation requirement because there is no between-participant predictor to attenuate:

- model tSSRT and threshold as noisy estimates rather than known values, or H2 power will be materially
  overstated; and
- expect the required N for H2 to exceed intuition. If the simulation returns a requirement well above 40,
  treat H2 as the study's scope decision — either commit to the larger sample, or demote H2 and H4 to
  exploratory and reframe the paper around H1 and H3 with the kinematic mechanism.

Do not report 32 as a powered target, and do not discover an H2 power shortfall after collection.

#### Measured tSSRT reliability — numbers for the simulation, ADDED 2026-09-14

The attenuation argument above is right, but it was stated without figures. The literature supplies them, and
they are not comfortable.

- **Test–retest reliability of SSRT is moderate at best**, typically reported in the **r ≈ 0.5–0.7** band — the
  range at which a measure is usable for group comparisons but marginal as an individual-difference predictor.
- **Hall, Jenkinson & MacDonald (2022)**, *Exp Brain Res* 240(11):3061–3072, ran the anticipatory response
  inhibition task twice. Non-selective SSRT was stable between sessions, but the authors describe that
  consistency as supported by **weak evidence**; and **selective-stopping SSRT improved by 98 ± 17 ms between
  sessions** — a practice effect larger than most effects this study hopes to detect.

Three consequences, all of which belong in the preregistration:

1. **Attenuation caps what H2 can observe.** With predictor reliability ≈0.6 and threshold reliability ≈0.7,
   the ceiling on an observable correlation is √(0.6 × 0.7) ≈ **0.65**. A true moderate association of 0.4 would
   be observed near **0.26** — which at 80% power needs roughly **110 participants**, not 32–40. Feed measured
   reliabilities into the simulation rather than assuming the predictor is clean.
2. **Measure reliability in-session, do not assume it.** Compute **split-half reliability of tSSRT** (odd/even
   stop trials, Spearman–Brown corrected) and of the **E2 threshold**, and report a **disattenuated**
   correlation alongside the raw one. Without this, a null H2 cannot be distinguished from an unreliable
   predictor — which is the single most likely way this paper produces an uninterpretable result.
3. **Guard the practice effect.** tSSRT is measured once, in E1, and used to predict thresholds measured later
   in the same session. If stopping speeds up with practice, the predictor is systematically mismatched to the
   window it predicts. Either run a **short second E1 block after E2** (giving both a reliability estimate and a
   drift check, at a few minutes' cost), or preregister the drift as an untested assumption and say so.

**This does not change the verdict on the design** — §4 already models both quantities as noisy and already
names the fallback (demote H2/H4, reframe around H1 and H3). It supplies the numbers that decision needs.



## 5. Apparatus and timing validation

### Common apparatus

- HMD and visual targets.
- **Tracking system decided by paired pilot comparison (revised 2026-09-08, PI discussion).** Both Vicon and
  commodity trackers are logged **simultaneously** during pilot sessions, and the choice is made on measured
  evidence against criteria fixed beforehand. Procedure and Paper 1's criteria are in
  `PAPER1_STUDY_DESIGN.md` §11.

  **Paper 2 sets its own criteria, and they are stricter.** A split outcome — commodity trackers adequate for
  Paper 1, inadequate here — is a coherent and genuinely likely result, and must not be overridden for the
  convenience of running one system across both papers.

  | Metric | Candidate threshold | Why |
  |---|---|---|
  | Velocity error, 95th pct, at 1–3 m/s reach speeds | < 10% of reach speed | Lead-time delivery and TTC forecasting |
  | Simulated LT80 credible-interval width under measured error | ≤ the width the §4 simulation shows H1 needs | Threshold precision must survive the sensor |
  | Movement-onset detection agreement against reference | ≥ 95% within ±10 ms | **v2.0:** no longer for E1's race model. Still required — §8 makes the warning *eligible only after confirmed movement onset*, so a mis-timed onset shifts every delivered lead time |
  | Kinematic detector agreement against reference | Pre-registered | Response onset and arrest latency are derivative-based |

  Thresholds are illustrative; set each from **what the analysis requires**, via the §4 simulation, and record
  them dated **before the first pilot session**. Choosing after seeing which system produced tidier data is a
  researcher degree of freedom, not a decision.

  **The paired pilot also answers this directly.** Simulating the psychometric fit under *measured* commodity
  error — rather than assumed error — is exactly the check this section previously demanded, and the paired
  data supplies it.

  **The analytical prediction, now under test.** Paper 2 is **substantially more sensitive to tracking quality
  than Paper 1**, for the reasons below. The pilot tests that prediction rather than assuming it.

  Every one of Paper 2's contributions depends on **velocity**, not merely position:

  - Lead time is delivered against a forecast of when the hand *would* cross the boundary — a velocity
    computation.
  - `tSSRT` requires movement-onset detection defined by "outward velocity above a threshold for a minimum
    sustained duration" (§7).
  - The kinematic account — response onset, arrest latency, post-cue travel, path deviation — is entirely
    derivative-based.

  Differentiating position amplifies noise by roughly `1/(Δt·√2)`, about **70× at 100 Hz**. Against reach
  speeds of 1–3 m/s, centimetre-level positional error yields velocity error on the order of half the signal.
  Filtering cannot rescue it: noise falls as `√N` while group delay grows as `~N/2`, so reaching usable
  precision costs hundreds of milliseconds of lag **inside an experiment whose entire manipulated range is
  100–800 ms**.

  At sub-millimetre accuracy and ≥ 200 Hz, velocity noise is roughly an order of magnitude smaller and
  effectively negligible against the signal.

  **Still verify rather than assume, whichever is selected.** Confirm accuracy and frame rate in the actual
  capture volume during fast reaches. Under Vicon, confirm marker labelling survives full-speed movement —
  mislabelling produces confident wrong data, which is worse than a gap. Under commodity trackers, confirm
  dropout and motion-blur behaviour at reach speed. Report the chosen system's measured accuracy, since it
  bounds every conclusion drawn downstream.

  **Paper 2 is the easier of the two to instrument.** One arm, a confined volume, a fixed home region, and
  fewer markers than Paper 1's five-site room-scale setup. If marker robustness proves difficult anywhere, it
  will be in Paper 1, not here.
- One arm-mounted tactor at the same site in E1 and E2.
- The same three-pulse warning waveform used in the associated warning project: 3 × 100 ms with two 60 ms gaps.
- Headphones with masking noise sufficient to prevent the tactor's sound from acting as an auditory stop signal.

### Physical onset requirement

All behavioral timing is referenced to the **first physical vibration**, not the software command.

Before participant testing:

1. Instrument the tactor with an accelerometer or contact sensor.
2. Measure command-to-onset latency and jitter over at least 100 activations.
3. Measure tracking-to-application latency and frame-time distribution.
4. Store the compensation value and device/firmware/software versions.
5. Repeat a shortened timing check at the start of every collection day.

The raw log must retain both command time and estimated/measured physical onset time. Any session exceeding the preregistered latency or jitter tolerance is invalidated before outcome inspection.

### Perceptual check

Before E1, verify that the participant detects the warning at the study intensity. Use criterion-based familiarization, not an unvalidated subjective question. The intensity may be adjusted only through a preregistered calibration procedure and must then remain fixed for E1 and E2.

## 6. Session flow

**Single visit, single task (v2.0).** E1 is deferred, so the task-order machinery below is no longer needed —
which also removes the fatigue confound that dominated this section. That simplification is one of the larger
practical wins from the scope cut.

1. Consent, screening, demographics, handedness, and safety briefing.
2. HMD fit, marker/tracker placement, and co-registration with residual check.
3. Tactor detection check and cue familiarization. **The tactor then stays mounted for the whole visit.**
4. Practice for **E2** until criterion — no measured threshold trials.
5. **E2** with scheduled breaks and between-block discomfort ratings.
6. Post-session comfort, sickness, and adverse-event check.

**Session length falls substantially.** The old flow ran ~300 E1 trials plus ~280 E2 trials plus two practice
phases and a mandatory inter-task break. E2 alone is roughly half of that, which relieves pressure on the
duration ceiling and the shoulder-discomfort rule, and makes it realistic to *increase* E2's warning-trial
count if the §4 simulation asks for a tighter threshold interval. **Spend the recovered time on H1's
precision** rather than banking it.

*(Stage numbering changed at v2.0. The previous nine-stage two-task flow is superseded.)*

The session-duration ceiling, minimum breaks, operator stop rules, and shoulder-discomfort rule must be
approved before piloting. **Paper 2 uses an independent cohort (§2), so this visit is never appended to a
Paper 1 session.**

### Task order — blocking confound in the single-visit design — ⚠ MOOT AT v2.0

> **This confound disappears with E1.** There is only one task now, so nothing is measured with a fresh arm
> and predicted with a fatigued one. Retained as the design record, and as a warning for anyone reviving E1:
> the problem described below is real and must be re-solved before a two-task design is run again.

The flow above fixes the order E1 → E2 for every participant. Combined with the trial counts in §7 and §8, this
means **tSSRT is measured with a fresh arm and the lead-time threshold is measured with a fatigued one, for
every participant, by design.**

That is a direct threat to **H2**, the study's central individual-differences claim. If arm fatigue both slows
stopping and raises the required lead time, a positive H2 result is consistent with fatigue rather than a stable
trait, and the design contains no way to separate them. Removing the pre/post tSSRT comparison (§2) correctly
eliminated one confound but left this one in place.

### DECIDED 2026-09-07 (revised) — single visit, task order randomized

**E1 and E2 run in one visit. Task order is randomized across participants:** half complete E1 → E2, half
E2 → E1. Order is recorded and entered in the model.

**Randomized order is not optional under a single-visit design.** With a fixed order, tSSRT would be measured
rested and the threshold fatigued for every participant, so a positive H2 would be equally consistent with
fatigue driving both measures. Randomizing does not remove fatigue — it removes the *systematic alignment*
between fatigue and measurement type, which is what turns a confound into noise.

**Cost, stated plainly.** For half the sample, tSSRT is measured on a fatigued arm. That inflates
between-person variance in exactly the term H2 depends on, and H2 is already the weakest-powered claim in the
paper. The single-visit design makes the study's most novel hypothesis harder to detect. Accept this
knowingly, and reflect it in the §4 power simulation rather than discovering it afterwards.

**What this decision requires:**

1. **Randomize and record task order**, and preregister an order effect check plus an **order × tSSRT
   interaction** check. Randomized order that is never inspected is not a control. If the interaction is
   substantial, H2 must be reported separately by order rather than pooled.
2. **A substantial mandatory break between tasks** — a preregistered objective minimum, plus participant-
   reported readiness before the second task begins. Not a token pause.
3. **An objective within-session fatigue index.** Track drift in go-trial response time and movement
   kinematics across blocks, and collect shoulder/arm discomfort ratings between blocks. Enter elapsed task
   time and trial index as covariates. *Do not* use a pre/post tSSRT comparison for this — it cannot separate
   fatigue from learning, habituation, and motivation (§2).
4. **Protect E1's stop-trial count over total session length.** Individual tSSRT reliability is H2's
   foundation; trimming stop trials to shorten the visit attacks the hypothesis at its weakest point. Keep
   **≥ 75 stop trials**. Trim elsewhere if the visit must shorten.
5. **Tactor stays mounted for the whole visit** — one advantage of the single-visit design. No re-mounting, so
   no cross-visit cue-calibration drift, and one marker placement and co-registration rather than two.

**Indicative single-visit trial budget**, to be confirmed by §6.1 pilot timing and §4 simulation:

| Component | Trials | Notes |
|---|---|---|
| E1 | ~300 (75 stop @ 25%) | Stop-trial floor protects tSSRT reliability |
| E2 | ~280 (70 warning @ 25%) | Prevalence is locked at 25% to prevent strategic slowing, so total scales with warning count |
| **Total** | **~580** | ≈ 40–45 min of pure trial time |

Projected visit: **≈ 95–110 min** including consent, setup, calibration, both practices, breaks, and
post-session measures. This must clear the approved duration ceiling before piloting.

Rejected alternatives, recorded:

| Alternative | Status |
|---|---|
| **Two visits ≥ 48 h apart, order randomized** | Scientifically cleanest — both measures rested, H2 uncontaminated, and each visit only 60–80 min. Rejected on participant burden and between-visit attrition. Revisit if the pilot shows the single visit exceeds the duration ceiling or produces unacceptable fatigue drift. |
| **Single visit, fixed E1 → E2** | Rejected: H2 becomes correlational with a known systematic confound and would have to be labelled exploratory, forfeiting the paper's most novel claim. |

### 6.1 Session-duration feasibility — resolve at pilot

The planning ranges in §7 and §8 total **620–800 speeded reaches in one visit**, plus practice, calibration,
consent, and questionnaires. At a realistic 3.5–4.5 s per trial including return-to-home, that is roughly
60–75 minutes of pure trial time and a **plausible total visit of 110–140 minutes in an HMD**.

This is a genuine feasibility risk, not a formality. It threatens the study through shoulder and shoulder-girdle
fatigue, cybersickness attrition, declining task compliance in late blocks, and ethics-board duration limits.

The pilot must report, before the trial counts are frozen:

- measured mean and worst-case trial duration for E1 and E2, including return-to-home;
- total visit duration with the planned break schedule;
- shoulder/arm discomfort ratings across blocks;
- drift in go-response time and movement kinematics across blocks as an objective fatigue index; and
- dropout and adverse events.

If the projected visit exceeds the approved ceiling, reduce trial counts only with simulation evidence that
threshold precision and H2 power survive, or adopt the two-visit design in the table above. Do not preserve
trial counts by shortening breaks.

## 7. E1 — Tactile stop-signal task — ⚠ DEFERRED, NOT RUN IN THIS PAPER

> **This entire section is deferred as of v2.0 (2026-09-14) and is retained as the design record for a future
> study.** It is not implemented, not run, and contributes no hypothesis to Paper 2. See the v2.0 banner for
> the reasoning: SSRT's measurement reliability made H2 unlikely to be answerable at a feasible sample size,
> and E1 carried essentially all of the implementation risk.
>
> **If this is revived**, three things must be added that the text below does not yet specify: split-half
> reliability of tSSRT (odd/even stop trials, Spearman–Brown corrected) reported alongside a disattenuated
> correlation; a second short stop-signal block to estimate within-session drift against the 98 ms practice
> effect reported by Hall et al. (2022); and a power simulation run on **measured** rather than assumed
> reliabilities.

### Purpose

Estimate tSSRT independently of the collision-warning trials.

### Trial

1. The participant holds the tracked hand inside a visible home region for a stable, randomly jittered foreperiod.
2. One of two targets appears, requiring a speeded reach in one of two directions.
3. On 75% of trials, the participant reaches to the target as quickly and accurately as possible.
4. On 25% of trials, the tactile stop signal appears after an adaptive stop-signal delay (SSD); the participant attempts to cancel the reach.
5. The hand returns to the home region before the next trial can begin.

Two target directions prevent a rhythmic, fully predictable single response. Target distance and size remain fixed after the pilot.

### Trial count

- Planning range: 300–400 total trials.
- Stop probability: 25%.
- Four or five blocks with mandatory breaks.
- At least 75 stop trials is the planning target for individual prediction; the final number is simulation-determined.

### Staircase

- One-up/one-down adaptive tracking targeting approximately 50% stop success.
- A fixed preregistered step, initially expected to be 40–50 ms.
- SSD increases after a successful stop and decreases after a failed stop.
- SSD is bounded at zero and by a preregistered upper limit derived from practice go-response times.
- Starting SSD is determined by a preregistered practice rule, not changed manually by the operator.
- If direction-specific performance differs materially in the pilot, use two interleaved direction-specific staircases.

Do not introduce an improvised shrinking step schedule during collection.

### Response definitions

The race-model response is **movement onset**, operationalized as leaving the home region with outward velocity above a pilot-validated threshold for a minimum sustained duration. The exact radius, velocity, and duration are engineering parameters frozen after blinded pilot validation and before preregistration.

- Go-response time is target onset to movement onset.
- A failed stop is movement onset before the stop-trial deadline.
- A successful stop requires remaining within the home-region criterion through the deadline.
- Target contact and movement completion are secondary measures and are not substituted for the race-model response.

This follows the established reach-countermanding logic and makes E1 a test of cancelling a prepared reach. It does not pretend that E1 directly measures mid-flight physical braking; E2 measures that construct.

### Participant instructions and feedback

- Emphasize that fast, accurate go responses and successful stops are equally important.
- Explicitly instruct participants not to wait for a possible warning.
- Give block-level feedback on median go time, omissions, and stop success.
- Do not give feedback that reveals the next SSD or encourages deliberate slow reaching.

### tSSRT estimation

Primary estimator: integration method with replacement of go omissions.

Report at minimum:

- complete go-response distribution;
- go omissions and direction errors;
- SSD distribution and mean SSD;
- `p(response | stop)` overall and by SSD;
- tSSRT with uncertainty;
- successful- and failed-stop response times;
- movement-onset timing relative to the stop signal;
- proactive slowing over blocks and following stop trials; and
- split-half/bootstrap or posterior reliability of the individual estimate.

### E1 validity gates

Pre-register thresholds informed by the pilot and the stop-signal consensus guidance. At minimum verify:

- stop success is near the tracking target and not at floor/ceiling;
- the inhibition function is monotonic enough to support the model;
- failed-stop responses are faster than ordinary go responses;
- omissions and wrong-direction responses are acceptably low;
- tSSRT is positive and plausible relative to the go distribution;
- proactive slowing does not invalidate the go process; and
- individual tSSRT reliability supports H2.

If the final gate fails at the sample level, H2 becomes non-confirmatory; E2 may still support the lead-time findings.

## 8. E2 — Warning lead-time × tempo experiment

### Purpose

Estimate how much physically realized warning time is required to prevent an already-moving hand from crossing an invisible virtual hazard boundary.

### Trial geometry

1. The participant starts in the same home region.
2. A visible target appears beyond a trial-specific invisible hazard volume.
3. The target direction and hazard boundary are procedurally varied so their location cannot be memorized.
4. The natural direct reach intersects the hidden volume.
5. Ordinary trials require a fast target touch. Warning trials require the participant to cancel and withdraw the reaching hand immediately after the vibration.
6. No visual, auditory, score, or collision feedback reveals the hazard position or whether a boundary crossing occurred.

All volumes remain virtual. The real room is cleared and the virtual work area remains within the approved physical safety boundary.

### Warning prevalence and trial count

- Warning trials: 25%.
- Ordinary go trials: 75%.
- Planning range: 320–400 total trials, producing 80–100 warning trials.
- Use mandatory breaks and a predeclared session-duration ceiling.

If the final trial count is reduced, the simulation must show adequate **threshold precision** (v2.0 — H2 power
no longer applies). Do not compensate by making warnings common enough to induce strategic slowing.

### Lead-time manipulation

- Select 5–7 lead-time levels using pilot data.
- The levels must span approximately 10%–90% avoidance rather than being chosen from intuition.
- A warning becomes eligible only after confirmed movement onset.
- On each warning trial, a clean tracking stream estimates remaining time to the first boundary crossing.
- Send the haptic command early enough that the **estimated first physical vibration** occurs at the assigned remaining lead time.
- At physical onset, freeze and log the pre-cue counterfactual trajectory estimate; do not redefine lead time using the participant's post-warning movement.
- Log requested lead, predicted lead, physically corrected lead, pre-cue speed, direction, tracking quality, and trigger error.

Trials outside the preregistered timing-tolerance band are not silently deleted. Mark them as protocol deviations and handle them using the preregistered intention-to-deliver and per-protocol sensitivity analyses.

### Tempo manipulation

Tempo is randomized at the trial level, not assigned to late blocks.

- Two target-response windows create normal and urgent reaches.
- The cue waveform, prevalence, hazard geometry distribution, and lead-time allocation are identical across tempo.
- Practice teaches the response windows before measurement.
- Block-level pace feedback may be given for ordinary trials only.
- Confirm the manipulation with pre-cue hand velocity and movement time.

Actual pre-cue speed is retained as a continuous variable. Tempo assignment provides causal evidence; measured speed explains within-condition movement variation.

#### Implemented 2026-09-14 — and the response window is a BAND, not a ceiling

Until this date the tempo manipulation **did not exist in code**. `Tempo` was assigned by `E2SessionPlan` and
written to the CSV, and nothing acted on it: no differential response window, no prompt to the participant, no
pace feedback, no movement-time measurement. A session run then would have produced a `tempo` column that was
pure noise, and **H3a — one of only two confirmatory hypotheses — would have estimated nothing.** §15 gate 7
could not have been evaluated at all.

Now implemented: per-tempo response windows (`E2Params.NormalWindowSeconds` / `UrgentWindowSeconds`), a
NORMAL/FAST prompt shown before each target as the instruction script promises, pace feedback on **go trials
only**, and movement time measured from confirmed onset to target contact and logged.

**The window for Normal has a lower bound, and that is not a detail.** An upper bound alone does not
manipulate tempo: a participant reaching at 0.6 s on every trial satisfies "≤ 1.20 s" and "≤ 0.70 s"
simultaneously, so Normal and Urgent become behaviourally identical — while every compliance check still
reads green, because both windows were met. H3a would then compare Urgent against Urgent and return a null
for a reason invisible in the data. So Normal is a band (default 0.85–1.20 s) and **too fast is a miss**,
matching the instruction script's "not slower, not faster". Urgent keeps no lower bound.

**`NormalWindowMinSeconds` must sit above `UrgentWindowSeconds`** or the bands touch and the separation is not
enforced. A unit test asserts this.

All four values are pilot-set (§15 gate 7). The manipulation check — movement time and pre-cue speed by tempo,
plus window compliance — runs in `Analysis/e2_analysis.R` §2e, which warns if urgent reaches were not faster
or if compliance falls below 60% (instruction failure rather than tempo manipulation).

### Outcomes

**Primary outcome**

- Binary target-hand entry into its designated hazard volume during the trial window.

**Secondary outcomes**

- minimum clearance;
- maximum penetration depth;
- post-cue distance traveled toward the hazard;
- kinematic response-onset latency;
- physical arrest/reversal latency;
- target contact;
- warning detection failures; and
- subjective cue timeliness/urgency collected after blocks, not after individual trials.

The primary event must be target hand × target hazard. Entry by another tracked joint or into another volume is logged separately and cannot be substituted into the primary numerator.

### Validating the counterfactual — ADDED 2026-09-14

**This closes the sharpest methodological attack on the paper.** Lead time is defined against a *predicted*
contact that, on successful trials, never happens. A reviewer will ask how you know the prediction was any
good.

**Answer it with data you already collect.** On trials where the participant **failed** to avoid, contact
actually occurred — so the true contact time is known. Compare it against the contact time predicted from the
frozen pre-cue trajectory at physical cue onset.

- Report the **distribution of prediction error** (bias and spread), not a single summary.
- Report it **as a function of lead time and tempo** — error will be worst for long leads and fast reaches,
  because the constant-velocity assumption has longest to go wrong.
- Preregister a **sensitivity analysis** refitting H1 with lead times corrected by the measured bias.
- If the error is large relative to the psychometric transition width, **say so and widen the reported
  interval** rather than presenting a precision the method cannot support.

This converts the study's largest unquantified assumption into a reported number, at the cost of one extra
logged column. It is also a reusable methodological contribution: **we are not aware of prior work that
reports counterfactual prediction error for warning-timing studies.**

> **Softened 2026-09-23** from "no prior work reports…". A universal claim about a *method detail* cannot be
> verified by any search available to us — a title-and-abstract sweep cannot see what a paper logged, which
> is exactly how Valkov & Linsen's internal trigger comparison was missed for weeks. "We are not aware of"
> costs nothing rhetorically and cannot be refuted by a single counterexample.
> See `docs/NOVELTY_SEARCH_ROUND5.md` (claim C5: search returned noise, so it is **unverified, not passed**).

### The no-warning baseline anchors the curve — state it explicitly

75% of trials carry no warning but **do** carry a hazard intersecting the direct reach. Their crossing rate is
the avoidance probability at **zero warning** — the curve's lower asymptote, measured rather than assumed.

Report it as such. It also doubles as the engagement and geometry check: if participants avoid the hazard at a
high rate *without* any warning, the task is not producing genuine near-approaches and the whole curve is
compressed into a range where nothing can be estimated.

### What the estimate is and is not — the capacity framing

§8 instructs participants to **cancel and withdraw immediately** on warning trials. That is deliberate: it
measures what the motor system *can* do, not what a user *chooses* to do. Two consequences, and both belong in
the manuscript rather than in a reviewer's report:

1. **The estimated requirement is a lower bound.** An uninstructed user must also notice, interpret, and decide
   to act. Real deployment requirements are longer than LT80, not shorter.
2. **It is the right estimand for a design floor.** A system cannot be safe if it warns later than the motor
   system can possibly respond, whatever the user's intent. LT80 is that floor, and a floor is a useful thing
   to publish precisely because it is not strategy-dependent.

Frame it as *capacity*, and the instructed response stops being a limitation and becomes the reason the number
is portable.

### Participant instruction script — ADDED 2026-09-14

**Full script: `docs/E2_INSTRUCTION_SCRIPT.md`. Read verbatim from print, once, after the tactor detection
check (§6 stage 3) and before practice.**

E2 cannot reuse Paper 1's script, and the difference is not cosmetic. **Paper 1 tells participants to stay out
of the hazard zones. E2 must not.** 75% of E2 trials carry a hazard on the direct reach with no warning, and
their crossing rate *is* the zero-warning anchor above. A participant who reaches cautiously around where
zones might be raises that anchor toward ceiling, compresses the psychometric curve into a range where nothing
is estimable, and silently changes what LT50 and LT80 mean. E2's instruction is therefore a **stop-signal**
instruction, not an avoidance instruction: reach normally, and stop only when the cue arrives.

Four clauses in the script are load-bearing, each against a named threat:

| Clause | Threat |
|---|---|
| You cannot predict where the zones are, and are not expected to | Proactive avoidance destroying the zero-warning anchor |
| Do not slow down waiting for a vibration | **Strategic slowing** — the reason §8 locks prevalence at 25%. Slowing inflates realized lead on every trial and shifts the whole curve left |
| Sometimes you will not be able to stop, and that is expected | Short leads are designed to be unstoppable — that is the floor of the curve. Without this, failures read as personal error and participants compensate by slowing |
| You will get no feedback about the zones | Prevents inferring hazard geometry across trials |

The third is the one an operator is most likely to drop as discouraging. It is the most load-bearing sentence
in the script, and the delivery rules require it to be **re-read verbatim** rather than paraphrased if a
participant becomes discouraged — improvised reassurance is an uncontrolled instruction, and the natural
improvisation is a direct instruction to change strategy.

**One decision this raises, still open.** Practice trials run identically to measured trials — verified in
code: `E2SessionPlan` has no practice concept, and `E2SessionRunner` merely suppresses the log for the first
`practiceTrials` rows. Practice therefore draws from the full lead range, so a participant can meet the
impossible-to-stop leads before forming a stable response — precisely the condition the third clause exists to
defuse, arriving before the reassurance can work. **Recommendation: practice warning trials at the longest
lead only, then switch to the full range.** This requires a practice prefix emitted by `E2SessionPlan`, and
must be decided and recorded before piloting; it is not an operator judgement call on the day.

An **expectancy probe** is also specified there (§E), which Paper 2 previously lacked. Preregistered as
descriptive, never as a covariate.

### Kinematic response detector

Pre-register and freeze the detector before confirmatory data collection.

- Use no-warning trajectories to establish the expected path and velocity envelope for each geometry/tempo.
- Detect a sustained deviation, deceleration, or reversal relative to that counterfactual envelope.
- Specify filtering, window lengths, thresholds, and minimum sustained duration.
- Validate against manually labeled pilot trials with the labeler blinded to lead-time condition.
- Report false-positive/false-negative performance.
- Do not average latency only among detected responders. Use a time-to-event/censoring model or a joint response-incidence/latency model.

## 9. Confirmatory hypotheses and estimands

### H1 — Lead-time response curve

Longer physically realized warning lead time increases the probability of avoiding the designated hazard.

Primary estimand: population psychometric lead-time slope and population LT50.

**H1 is an estimation target, not a meaningful directional test.** That more warning time helps is not in
scientific doubt, and a paper framed as having "confirmed" it invites the question of what a failure would have
meant. The contribution is the **parameters** — where the curve sits, how steep it is, and how precisely either
can be pinned down. Report and headline it that way: *"LT50 = X ms, 95% CI [a, b]"*, not *"warning lead time
significantly predicted avoidance."*

**Check monotonicity at the long end before assuming it.** The logistic form assumes avoidance rises without
limit as lead time grows. That may fail at the upper end: a warning delivered very early can arrive before the
participant has committed to a movement direction, may be judged premature, and may be discounted — an effect
documented in the alarm and driver-warning literature. Preregister an inspection of the empirical
avoidance-by-level function for non-monotonicity, and preregister the fallback (a non-monotonic or
spline-based fit) rather than deciding after seeing the data. A downturn at long lead times would be a
substantive finding and directly relevant to the §16.4 earliness trade-off.

### H2 — Individual stopping capacity — ⚠ DEFERRED, NOT TESTED IN THIS PAPER

> Removed from the confirmatory family in v2.0 (2026-09-14). Retained as the design record for a future study.
> It is **not** demoted to "exploratory" here — tSSRT is not measured at all, so there is nothing to report.

Longer independently measured tSSRT is associated with a longer warning lead time required to reach a common avoidance probability.

Primary estimand: change in participant threshold per standard-deviation increase in tSSRT.

**State what H2 actually bets on.** Per §7, E1's race-model response is **movement onset** — it measures
cancelling a reach that is prepared but not yet launched. E2 measures arresting a reach that is **already in
flight**. These are not the same construct, and §7 says so explicitly. H2 therefore tests whether
**cancellation capacity generalizes from pre-movement inhibition to mid-flight braking**, and the paper must
frame it that way rather than treating tSSRT as self-evidently the right individual-difference measure.

Framing it correctly also repairs the null case. If H2 fails, the finding is *not* "individuals do not differ
in their warning-time requirement" — it is "pre-movement inhibitory latency does not predict mid-flight
braking," which is a substantive result about the organization of motor inhibition and remains publishable.
Preregister that interpretation now so it cannot be read as a post-hoc rescue.

**Do not preregister the sign as certain.** The expected direction is that slower cancellation requires more
lead time, and that remains the hypothesis. But inhibitory-control measures have shown counterintuitive
relationships with applied stopping performance elsewhere — the automated-driving takeover literature reports
higher measured inhibitory control associating with *worse* takeover outcomes. Use a two-sided test, and
preregister an interpretation for a reversed association rather than treating one as impossible.

### H3 — Movement tempo

**Split into a primary and a secondary claim.** The previous single-sentence formulation — "require greater lead
time **or** produce greater post-cue travel" — was a compound hypothesis confirmable by either of two different
outcomes, which inflates the effective false-positive rate without any correction and will be flagged by any
reviewer who reads the preregistration closely.

**Preregister the kinematic null before testing H3a — ADDED 2026-09-14.** "Faster reaches need more warning"
is a weak claim tested bare, because a reviewer can dismiss it as arithmetic. It is not arithmetic, and saying
why makes it a real result:

- If stopping *latency* were constant regardless of speed, a faster limb would need the **same** lead time and
  would simply travel further. Required lead time would not move with tempo.
- If braking from higher momentum takes **longer**, the requirement rises with speed.
- Compute the lead time predicted under the constant-latency assumption from the observed kinematics, and
  report the tempo effect **relative to that prediction**: greater than, equal to, or less than the kinematic
  expectation.

That converts H3a from a directional test anyone could guess into a quantitative statement about how braking
scales with momentum — and it costs nothing but preregistering the comparison.

**H3a (confirmatory).** Urgent/faster reaches require a **greater warning lead time** to reach a common
avoidance probability than normal-tempo reaches.

Primary estimand: tempo effect on the threshold; measured pre-cue velocity provides a secondary continuous
account.

**H3b (secondary, descriptive).** Faster reaches produce greater post-cue travel toward the hazard.

H3b is reported as mechanism, not as evidence for H3a, and it is **close to arithmetically guaranteed**:
post-cue travel is distance covered after physical cue onset, and a faster hand covers more distance in the
same interval. Confirming it demonstrates little beyond the measurement working. Its value is quantitative —
how much extra travel, and whether it scales as a constant-velocity account predicts or departs from it
because braking dynamics themselves change with speed. A departure would be the interesting result.

Only H3a enters the confirmatory family and the multiplicity correction.

### H4 — Predictive utility — ⚠ DEFERRED, NOT TESTED IN THIS PAPER

> Removed with H2 in v2.0 (2026-09-14), for the same reason and by the same argument the section itself makes:
> "If H2 is attenuated by measurement error, H4 will almost certainly be null."


Adding tSSRT improves held-out prediction of warning-trial avoidance beyond lead time, tempo, pre-cue speed, and target geometry alone.

Primary estimand: preregistered out-of-sample change in log score or Brier score with calibration assessment.

**Cross-validation must be leave-one-participant-out.** tSSRT is a participant-level constant, so trial-level or
random-fold cross-validation leaks participant identity into the held-out set and will report an improvement
even when tSSRT carries no generalizable information. Hold out **all trials from each participant** in turn,
fit on the remainder, and predict that participant's warning-trial outcomes using only their tSSRT, tempo,
pre-cue speed, and geometry.

Preregister the comparison model explicitly — lead time + tempo + pre-cue speed + geometry, without tSSRT and
without a participant random effect — so that the reported gain is attributable to tSSRT rather than to
participant-level flexibility that the baseline lacks. Report the fold-level distribution of the score
difference, not only its mean, and assess calibration on the pooled held-out predictions.

With a realistic sample the fold-level distribution will be wide. A small mean improvement whose fold
distribution straddles zero is a null result for H4 and must be reported as one.

**Set the expectation before collection, not after.** Leave-one-participant-out cross-validation of a single
participant-level predictor at N ≈ 32 is a **low-powered procedure**: each fold contributes one participant's
worth of evidence, and the fold-to-fold spread will be wide even when the underlying effect is real. The most
likely outcome for H4 is *inconclusive* rather than clearly positive or clearly negative.

Two consequences to preregister:

- Include H4 in the §4 power simulation explicitly, not only H1 and H2. If the simulation shows H4 cannot
  reach a useful conclusion at the achievable sample size, **demote it to a preregistered secondary analysis**
  and remove it from the confirmatory family. That protects the multiplicity budget for H1, H2, and H3a, and
  it prevents an underpowered null from being read as evidence against personalization.
- State in the manuscript that an inconclusive H4 does not adjudicate the personalization question either way.
  Only the prospective validation in §11 can do that.

Note that H4 is a strictly harder test than H2 and is powered by the same between-participant variation. If H2
is attenuated by measurement error, H4 will almost certainly be null. Reporting them as two independent
failures would overstate the evidence against individualization; report them as one linked result.

### Mechanism

Longer lead time permits an earlier kinematic response relative to boundary crossing and reduces post-cue travel/penetration. This is a secondary mechanistic analysis, not an automatically causal mediation claim.

## 10. Primary statistical model

Analyze warning trials at the trial level. Do not estimate a noisy threshold for each participant and then run a simple correlation.

Preferred hierarchical psychometric parameterization:

```text
avoid_ij ~ Bernoulli(p_ij)
logit(p_ij) = slope_i × (realized_lead_ij - threshold_ij)

threshold_ij =
    gamma_0
  + gamma_1 × tempo_ij
  + gamma_2 × pre_cue_speed_ij
  + gamma_3 × target_geometry_ij
  + participant_threshold_i
```

*(v2.0 — the `z(tSSRT_i)` term is removed with H2. Participant-level variation in the threshold is now carried
entirely by `participant_threshold_i`, which is reported as a variance component and is itself a result: how
much do people differ in what they need? That question survives the cut even though its predictor does not.)*

Include participant variation in threshold and, when supported, slope. **Report the between-participant
threshold SD explicitly** — with the tSSRT predictor gone, that variance component is the paper's statement
about individual differences, and a large SD is itself the argument that personalization is worth pursuing.
Do not split participants into “fast” and “slow” groups.

Model extensions and simplifications must be specified before confirmatory analysis:

- nonlinear lead-time function if a logistic curve is inadequate;
- direction/handedness nuisance effects;
- trial-number and preceding-stop effects;
- lapse/trigger-failure component;
- convergence and singular-fit procedure; and
- missing/tracking-invalid trial handling.

### Two specification decisions, forced by a dry run — ADDED 2026-09-14

`Analysis/e2_analysis.R` (the confirmatory script) was run against `Analysis/e2_simulate_dataset.R`, which
generates the real 26-column CSV from known ground truth. Two items above stopped being open questions.

**1. The lapse component is required, not optional.** The list above says "lapse/trigger-failure component"
without committing. Omitting it is not neutral. On data generated with a 3% lapse rate, a plain logistic
returned **LT50 270 ms (+20) and LT80 344 ms (+15)** against truth. An unmodelled ceiling below 1.0 drags the
fitted curve rightward, and LT80 — the number §16.2 makes the deliverable — is precisely what it inflates.

*Decision:* fit with a fixed-lapse link over the preregistered grid `{0, .01, .02, .03, .05, .08}`, selected
by AIC. On the dry run AIC recovered the true lapse exactly and LT80 bias fell to **+1 ms**. If the selected
lapse lands at the grid edge, report that rather than silently extending the grid.

**2. Pre-cue speed is a MEDIATOR of tempo, and must not be adjusted for in H3a.** The model above lists
`gamma_2 × pre_cue_speed_ij` among the threshold predictors. But the tempo manipulation *works by* making
people move faster — in the dry run `r(urgent, pre_cue_speed) = 0.80`. Controlling for speed therefore closes
the very causal path H3a is about, and biases the tempo effect toward zero: **37 ms recovered against a true
50 ms.** Dropping the speed term recovered 46 ms.

*Decision:* the confirmatory H3a model does **not** adjust for pre-cue speed. The speed-adjusted fit is
retained and reported as a **secondary mechanism decomposition** — the direct effect not carried by speed —
which answers a different and genuinely interesting question ("how much of the tempo cost is just speed?").
It must never be quoted as H3a. This is a deliberate departure from the literal model above: that list was
written to reduce nuisance variance in the threshold, and the rationale does not survive the variable turning
out to be the manipulation's own mechanism.

**A third finding that changes how the pilot is read.** The fitted between-participant threshold SD is a
**~18% under-estimate** — partial pooling recovered 49 ms from data generated at 60 ms. Since §4's sample
size is driven almost entirely by that SD (see `Analysis/e2_power_analysis.R` §6), the pilot's *reported* SD
must be inflated before it is read into the N table, or N will come out too small. Relatedly, the parametric
bootstrap interval measured **1.03–1.07×** the delta-method width, so the power script's widths are mildly
optimistic rather than badly wrong.

Report estimates and intervals, not only p-values. Control the confirmatory family using a preregistered
hierarchical testing sequence or multiplicity procedure.

### Two secondary estimands that carry real weight now — ADDED 2026-09-14

With no individual-difference predictor, these are what the paper says about people differing. Both are free —
they need no extra trials, only preregistered analysis.

**1. Between-participant threshold SD.** Reported as a variance component from §10. This is the paper's
statement on individualization: *"participants' required lead times differed by SD = X ms, so a single
population setting over- or under-serves a typical user by roughly that much."* A large SD motivates the
follow-up in §11; a small SD is an equally publishable finding that one setting suffices.

**2. Threshold split-half reliability.** Split each participant's warning trials (odd/even, or first/second
half with a trial-order check), estimate the threshold twice, and report the correlation with a
Spearman–Brown correction.

This matters more than it looks. **It is the precondition for the entire personalization programme.** If a
person's threshold is not stable within a single session, no predictor can ever predict it, and the deferred
tSSRT study (§7) should not be run. Reporting this reliability tells the field whether personalized warning
timing is a coherent goal — which is a contribution in its own right, obtained from data collected anyway.

If first-half and second-half thresholds differ systematically rather than just noisily, that is a **learning
or fatigue effect**, and it must be reported as such and entered as a trial-order term.

**The confirmatory family is H1 and H3a** (v2.0 — H2 and H4 are deferred and not tested). H3b is secondary
and descriptive and does not enter the correction. Fix the family membership in the preregistration; it cannot
be revised after seeing results.

With only two confirmatory tests and no between-participant predictor, the multiplicity burden is small — one
of the real benefits of the cut. Because H1 is an estimation target rather than a meaningful directional test,
do not gate H3a on H1 "passing": H1 will pass. Report both, corrected, with intervals.

## 11. What is needed to claim personalization — ⚠ FUTURE WORK, NOT THIS PAPER

> v2.0 (2026-09-14): with tSSRT deferred, this paper makes **no personalization claim at all** and does not
> need to defend one. The section is retained because it is the design for the follow-up study, and because
> the manuscript should still *name* it: "the between-participant threshold SD reported in §10 indicates
> whether individualization is worth pursuing; establishing that it works requires the prospective validation
> below." That is a stronger position than the old paper had — it points at future work from a measured
> variance component rather than from an underpowered correlation.

H2 and H4 support **personalization potential**, not superiority of a personalized system.

To claim that personalization works, conduct an additional prospectively powered validation experiment using a held-out cohort or a model frozen from an earlier training cohort:

1. `Fixed`: every participant receives the preregistered population warning setting.
2. `Personalized`: warning timing is computed from the participant's tSSRT and current movement state using the frozen rule.
3. Randomize the two policies within participant and balance order.
4. Compare boundary violations while also quantifying unnecessarily early warnings and acceptability.
5. Define a superiority or non-inferiority framework for the safety–earliness trade-off before collection.

Until this validation succeeds, the paper may say “supports future individualization” but not “proves personalized warnings are better.”

## 12. Tracking robustness analysis

This analysis is secondary and must not compete with H1–H4.

### Restored to its original form by the Vicon decision — 2026-09-07

A brief inside-out tracking decision removed the reference stream this analysis depends on, forcing a
bench-characterized error model as a substitute. The Vicon decision (§5) restores the reference stream, and
this analysis returns to its original and stronger form.

**Degradation is applied to real recorded movement**, with Vicon as ground truth, rather than reconstructed
from an offline error model. That is a materially better sensitivity analysis: it preserves the actual
temporal structure, speed dependence, and spatial distribution of the movement being degraded, none of which
a synthetic error model reproduces faithfully.

**REVISED 2026-09-08 — the pilot now supplies a measured commodity error model.** The earlier decision ruled
out simultaneous logging entirely, leaving degradation purely synthetic. The paired pilot comparison
(`PAPER1_STUDY_DESIGN.md` §11) reverses that as a by-product: commodity-tracker error is measured against a
reference on real task movement.

Two consequences:

- **If Vicon is selected**, degrade the reference stream using the **measured** commodity error structure
  rather than synthetic Gaussian noise. Real temporal correlation, real speed dependence, real dropout
  behaviour — a materially better sensitivity analysis, and a defensible answer to "would this work on
  affordable hardware?"
- **If commodity trackers are selected**, the pilot's paired data becomes the reference-versus-deployed
  comparison directly, and the confirmatory study reports its own tracking quality against it.

Simultaneous logging remains a **pilot-only** activity. The mounting complexity is acceptable across a handful
of sessions and was rejected for 36+ confirmatory ones; that judgement stands.

Still not a device certification. Only state a device requirement when errors were measured against the
reference and the success criterion was fixed in advance.

**Hard constraint, unchanged.** Never run the sweep with one stream as both reference and input — degrading a
stream against itself measures nothing.

**What this may be called.** A sensitivity analysis of the warning algorithm under simulated sensor error.
Explicitly **not** a device certification, and not a statement that any particular tracker is adequate or
inadequate for deployment. Only state a device requirement when errors have been measured empirically against
the reference system and the success criterion was defined in advance.

Treat clean reference trajectories as ground truth and degrade a separate warning-input stream. Manipulate one error family at a time and then selected combinations:

- spatial bias/miscalibration;
- zero-mean jitter with empirically justified temporal structure;
- transport/processing latency;
- frame-rate reduction;
- burst dropout;
- frozen frames; and
- velocity-filter delay.

Report warning-timing error, probability of delivery before LT50/LT80, miss rate, and false-alert rate. A synthetic degradation study establishes algorithm sensitivity; it does not certify a commercial tracker.

Only state a device requirement when errors have been measured empirically against the reference system and the success criterion was defined in advance.

## 13. Exclusions, missingness, and adverse events

Pre-register separate rules for:

- participant exclusion from E1;
- participant exclusion from E2;
- trial-level timing invalidity;
- tracking dropout;
- failure to follow go/stop instructions;
- early session termination;
- discomfort or cybersickness; and
- equipment failure.

Never keep a planned denominator after an unpresented or invalid trial. Preserve all raw rows with status/reason fields. Report recruitment, exclusions, adverse events, and usable trials in a flow diagram.

## 14. Data and reproducibility

Store each run in a unique immutable `Paper2/P<id>/<session_id>/` directory with overwrite protection.

| Required output | Minimum contents |
|---|---|
| `session.json` | protocol/code/parameter versions, device and firmware identifiers, tested arm, visit/order, start/end, operator, completion status |
| `timing_calibration.csv` | command and sensed physical onset for bench/day checks; latency and jitter summary |
| `cue_calibration.csv` | detection/familiarization trials, chosen fixed intensity, deviations |
| `e1_trials.csv` | trial assignment, direction, go/stop, SSD, command/physical onset, movement onset, target contact, success, omissions/errors, quality flags |
| `e2_trials.csv` | lead level, tempo, geometry, requested/predicted/realized lead, pre-cue state, boundary outcome, clearance/penetration, response measures, validity/reason |
| `trajectories_<block>.csv` | raw reference tracking with source/receive timestamps, sequence, positions, and tracking quality |
| `events.csv` | one chronological event stream for target, movement, warning, physical-onset estimate, boundary, response, tracking, and operator events |
| `questionnaires.csv` | item-level comfort, sickness, timeliness/urgency, and scored values |
| `deviations.csv` | every invalid/aborted trial, equipment failure, operator stop, and adverse event with reason |
| `analysis_manifest.json` | raw-data hashes, exclusions, software/package versions, and generated analysis files |

Store, with deidentified participant IDs:

- frame-level raw reference tracking;
- software command and corrected physical cue times;
- targets, home region, and hazard geometry per trial;
- trial assignments, SSD, lead-time level, and tempo;
- response classifications and quality flags;
- device/software/calibration metadata;
- analysis-ready data and immutable raw data hashes; and
- complete analysis scripts, simulation-based power code, and synthetic example data.

The preregistration must freeze hypotheses, primary outcome, threshold definition, trial counts, exclusions, timing tolerances, model, multiplicity handling, and confirmatory/secondary separation.

Raw full-body or limb trajectories may be identifying behavioral data. Consent, access control, encryption, retention, sharing transformations, and deletion procedures require ethics approval.

## 15. Pilot and launch gates

Human confirmatory collection cannot begin until all gates pass:

1. Ethics approval covers **E2**, motion tracking, haptics, raw trajectories, and stopping rules.
   **Updated at v2.0 — E1 removed.** If the ethics application was drafted against the old two-task design,
   it must be revised before submission: one task, one visit, roughly half the session length, and a
   provisional cohort of **24 analyzable / 30 maximum** rather than 32/40 (§4). Submitting the old scope and
   then narrowing it is harmless; submitting the old scope and then *changing* the task is not.
2. Physical cue-onset latency and jitter meet the frozen tolerance.
3. Tactor sound is masked and cue detection meets criterion.
4. ~~E1 produces a valid inhibition function.~~ **VOID at v2.0** — E1 is not run.
5. ~~Individual tSSRT precision/reliability is adequate for H2.~~ **VOID at v2.0** — tSSRT is not measured.
6. E2 lead levels cover the psychometric response range without floor/ceiling.
7. Tempo changes pre-cue speed without unacceptable instruction failure.
8. The kinematic detector passes blinded pilot validation.
9. Tracking loss, emergency stop, and operator procedures pass rehearsal.
10. End-to-end logs reconstruct every trial and preserve invalid-trial reasons.
11. Simulation supports the final participant/trial counts, targeting **LT80 interval width** (§4), with the
    threshold modeled as a noisy estimate.
12. ~~The §6 task-order decision is recorded.~~ **VOID at v2.0** — one task, so no order to counterbalance.
    Retained requirements that still apply: between-block discomfort ratings and a go-RT drift index.
13. Projected visit duration is within the approved ceiling under measured pilot trial timing (§6.1), with
    **warning trials held at ≥ 80** rather than trimmed to fit.
14. Lead-time levels are confirmed to bracket the reportable operating range (§16.1).
15. **Counterfactual prediction error is characterized** (§8) — predicted versus actual contact time on
    non-avoided trials, reported as a distribution, not assumed.
16. **Threshold split-half reliability is computed and reported** (§10).
17. Protocol, code, analysis, and preregistration versions are frozen and archived.

## 16. Choosing an operating point — the design deliverable

Paper 2 has no conditions to rank. Its decision question is continuous: **what lead time should a warning system
use?** This section is the analogue of Paper 1 §15 and is likewise a decision procedure, not a hypothesis test.
Preregister it.

### 16.1 LT50 is a curve parameter, never a design recommendation

LT50 is where avoidance succeeds half the time. Recommending it as an operating point would specify a system
that fails every second warning. It is reported because it is the best-determined point on the curve and the
natural anchor for between-person comparison — not because anyone should build to it.

Design-relevant quantities are **LT80 and LT90**, and they are reported only when the tested lead-time range
actually brackets them; extrapolating a logistic fit past the sampled range to obtain LT90 is not permitted.
Ensure §8's 5–7 levels reach into the high-avoidance tail during the pilot, or accept that only LT80 is
reportable.

### 16.2 Use the interval bound, not the point estimate

A system built at the LT80 point estimate achieves 80% avoidance only if the estimate is exactly right. The
design-relevant number is the **upper bound of the credible/confidence interval** on the required lead time —
the value beyond which one is confident of clearing the threshold.

Report the point estimate and the interval, and state the upper bound explicitly as the engineering figure.
The gap between them is itself informative: a wide interval means the study cannot yet specify a system, which
is an honest and publishable conclusion.

### 16.3 The deliverable is a surface, not a scalar

H3a establishes that the requirement moves with tempo, and pre-cue speed enters the §10 model as a continuous
predictor. The output is therefore **required lead time as a function of current movement speed**, published as a figure
and a table a system designer can read directly. *(v2.0 — the optional tSSRT conditioning is removed with H2.)*

Report the requirement across the observed speed range rather than a single pooled number. A pooled scalar
averages over the fast movements that most need the warning, and will understate the requirement exactly where
it matters.

### 16.4 Earliness is not free — report the trade-off curve

Warning earlier raises avoidance and simultaneously raises the rate of warnings issued on trials where no
boundary crossing would have occurred. The alarm-reliability literature is unambiguous that unnecessary alerts
erode compliance, so a recommendation that only reports avoidance is incomplete.

For each candidate lead time, plot achieved avoidance probability against the **unnecessary-warning rate**,
estimated from the frozen pre-cue counterfactual trajectory recorded in §8. The resulting curve is the paper's
practical contribution: it lets a designer select an operating point against their own tolerance for
nuisance alerts rather than accepting a single number chosen by the authors.

State plainly that this curve is derived under the study's idealized sensing, and that any real system's
achievable operating points are strictly worse — quantified by the §12 robustness analysis.

### 16.6 The immediate payoff — it settles a parameter in Paper 1

Paper 1 fires its predictive warning at **`T` = 1.00 s**, a value chosen by simulation against a
constant-velocity model, not measured on people (`PAPER1_STUDY_DESIGN.md` §6).

**LT80 tests that choice directly.** If LT80 lands near 450 ms, Paper 1 over-warns and pays unnecessary alerts
for safety it already had. If LT80 exceeds 1.0 s, Paper 1 under-warns and its predictive condition was never
early enough to deliver the advantage it was designed to test.

Say this in the discussion. A paper that **replaces a guessed parameter with a measured one — including in the
authors' own prior work —** demonstrates the number's use rather than merely asserting it, and it is the
cleanest available answer to "so what?"

### 16.5 What the operating point may be called

A recommended lead time from this study is **a requirement for the tested reach class, upper limb, tempo range,
and population, under idealized tracking**. It is not a safety certification, not validated for feet, torso, or
locomotion, and not a demonstration that a system built at that value performs as predicted — that requires the
prospective validation in §11.

## 17. Recommended paper framing

### Working title

**Stopping a Reach in VR: Measuring the Warning Time Required for Vibrotactile Collision Avoidance**

*(v2.0 — "Tactile Stopping Latency" removed from the title with tSSRT. A title naming a construct the paper
does not measure is the first thing a reviewer notices.)*

*(**Retitled 2026-09-15** from "How Early Is Early Enough? Warning Lead-Time Thresholds for Arresting an
Ongoing Reach in Virtual Reality", after measuring 2,465 ISMAR / IEEE VR / VRST titles from 2021–2025.
Three findings forced it. **"Lead time" appears in 0 of 2,465 titles** — it is human-factors vocabulary with
no currency in XR venues. **"Psychometric" also appears in 0.** And **"threshold" is already taken**: all 14
occurrences are perceptual *detection* thresholds, almost all redirected walking, so an XR reader primes for
"when does a manipulation become noticeable?" rather than a performance requirement.

A question form was considered and rejected on one ground: **§16.2 treats "the interval is too wide to
specify an operating point" as an honest, publishable outcome.** A title asking "how early must it arrive?"
promises a number the results may decline to give. "Measuring the warning time required" stays true under
every outcome, and "required" carries the design-floor framing that §16 and the ISO 13855 argument depend on.

**"Lead time" remains the technical term inside the paper** — define it once in the introduction and use it
throughout. The title is where discoverability is decided; the body is where precision is.)*

### One-sentence result template

“Arresting an ongoing reach before a virtual hazard required a vibrotactile warning delivered at least
**[LT80] ms** in advance (95% CI [a, b]); the requirement **[rose/did not rise]** with movement tempo, and
participants differed in their requirement by **[SD] ms**.”

### Publication decision rule

*(Rewritten at v2.0. The H2/H4 branches are void — those hypotheses are not tested.)*

- **H1 is reported as an estimate** (LT50, LT80, slope, intervals), never as a confirmed direction.
- **If the interval on LT80 is too wide to specify a system, say so.** §16.2 already treats that as an honest
  and publishable conclusion. Do not narrow it by extrapolating past the sampled range.
- **If the curve is non-monotonic at long lead times**, report it as a substantive finding about premature
  warnings (§9), not as a fitting problem.
- **H3a is confirmatory; H3b is descriptive** and is never cited as evidence for H3a.
- **Report the tempo effect against the kinematic null** (§9), not merely as a direction.
- **Report the between-participant threshold SD and its reliability** (§10) whatever they are. A small SD is a
  finding: it says one setting serves everyone. A large SD is a finding: it says it does not.
- **Never claim personalization.** This paper does not measure any individual predictor. The threshold SD
  indicates whether individualization is *worth pursuing*; establishing that it works needs §11.
- **State the capacity-estimate framing** (§8): warning trials instruct cancellation, so the estimated
  requirement is a **lower bound** on what an uninstructed user would need.

## 18. Prior art that must be differentiated

The methodological anchors in §19 establish that the *methods* are sound. This section addresses the separate
risk that a reviewer believes the *question* is already answered. Complete a systematic search before freezing
the novelty statement; the items below are known and must each be addressed explicitly in related work.

### The principal threat — warning-timing manipulation in driving

**Abe & Richardson (2004–2006)** manipulated forward-collision-warning alarm timing across multiple levels and
reported two findings that a reviewer will raise against H1 and against the §16 operating-point framing:
alarm promptness influenced **trust** more than it improved braking performance, and drivers who experienced
late alarms became **reluctant to respond to subsequent false alarms**.

Differentiate on three axes, and state them in related work rather than leaving them implicit:

| Axis | Driving FCW | This study |
|---|---|---|
| Timescale | Warnings engineered for 2–5 s time-to-collision | 100–800 ms, inside the window where a reach is already programmed |
| Effector | Foot to pedal: a discrete, highly practised, single-degree-of-freedom response | Whole limb mid-trajectory, competing with an ongoing motor plan |
| Deliverable | Comparisons across a few timing levels | A fitted psychometric threshold with intervals, plus a lead-time requirement as a function of movement speed |

The second Abe & Richardson finding is also a **hypothesis this study cannot test**: trust and compliance
effects require repeated exposure to an imperfect system, and E2's warnings are veridical by construction.
Say so, and cite it as the motivation for a separate reliability experiment rather than as a limitation
discovered late.

### The second threat — personalized warning timing in driving

§18 previously named only Abe & Richardson. There is a whole adjacent line on **personalizing warning timing
to the individual**, and a reviewer from it will raise it before this paper does:

- Wang et al. (2015) and the arXiv line on **customizing collision-warning systems to individual drivers**,
  including real-time estimation of an individual's brake-response-time distribution;
- **Personalized forward collision warning with learning from human preferences**, *Accident Analysis &
  Prevention* (2024);
- **Adaptive FCW considering differential driving behaviour and risk levels**, *Accident Analysis &
  Prevention* (2023).

**The differentiator is real, and it is the whole point of H2.** Every one of these personalizes on
**observed behaviour** — brake response times harvested from naturalistic driving, or learned preferences.
Paper 2 asks something different: whether an **independently measured latent latency**, obtained from a task
that contains no warnings and no hazard, predicts the lead time a person needs. That is a claim about a
transferable individual trait rather than about curve-fitting to a person's own history — and if it holds, it
means a system can be personalized from a two-minute calibration instead of weeks of observation.

State that distinction in related work. It is a stronger position than it looks, but only if it is claimed
first.

### Bloomfield & Badler (2007), IEEE VR — the origin of body-localized collision signalling (added 2026-09-14)

"Collision Awareness Using Vibrotactile Arrays," *IEEE Virtual Reality Conference* 2007, pp. 163–170,
doi:10.1109/VR.2007.352477. Honorable Mention. A tactor sleeve on the arm driven by a real-time human model,
firing when that body segment contacts a virtual object; full-arm vibrotactile feedback outperformed visual
feedback alone.

**Differentiate on one word: *feedback* versus *warning*.** Their tactors fire **on contact**. Nothing is
predicted, and nothing arrives in advance — so there is no lead time to manipulate and no threshold to fit.
This paper asks how far **before** contact the signal must arrive, which is a question their design cannot
pose. Cite it as the origin of body-localized collision signalling in VR, and as evidence that the tactile
channel was established as effective here nearly two decades ago; the open question was always *when*.

### Bajpai et al. (2020), IEEE ToH — the closest work in the XR haptics literature (added 2026-09-14)

"Enhancing Physical Human Evasion of Moving Threats Using Tactile Cues," *IEEE Trans. Haptics* 13(1), 32–37.
Tactile versus audio versus visual cues while participants physically evade objects moving toward them in VR;
outcomes are failure rate and reaction time.

**Differentiate on the scenario inversion, which is clean and easy for a reader to hold onto:** their threat
moves toward a stationary person; this study's person moves toward a stationary hazard. Theirs is
reaction-time-limited *initiation* of an evasive movement; this is *arrest* of a movement already committed.
They compare modalities at a single timing; this fits a threshold across lead times.

### Zhang & Wu (2018) — the closest existing lead-time manipulation (added 2026-09-14)

*Proc. HFES 62nd Annual Meeting.* Warning lead time (2.5 s vs 4.5 s) × reliability × style, connected-vehicle
driving. Two levels rather than a fitted curve, a pedal response rather than a limb in flight, and a
2.5–4.5 s timescale against this study's 100–800 ms.

Cite it in related work as the construct's closest precedent, and note that its **lead time × style**
interaction is the direct analogue of this study's **lead time × tempo** interaction. Differentiating on
"two levels versus a fitted threshold" is a stronger move than differentiating on domain.

### ISO 13855 — the existing standardized answer (added 2026-09-14)

Machine-safety practice already encodes "how much time does a hand need" as
`S = K × (t1 + t2) + C`, with `K` = 2000 mm/s hand approach speed and `t1` a human reaction-time constant.
A reviewer from safety engineering will raise this, so raise it first.

Differentiate on three points — see §1's novelty record for the full argument: the standard yields no
avoidance probability; `K` is a worst-case constant rather than a measured distribution; and it assumes the
**machine** stops while the human continues, whereas in VR nothing stops but the person. Position the paper as
**supplying the human-factors number the standard currently assumes**, which is a stronger and more useful
claim than novelty-by-domain.

### The closest applied precedent to H2

**Aksan, Sager, Hacker, Marini, Dawson, Anderson & Rizzo (2016)**, "Forward Collision Warning: Clues to
Optimal Timing of Advisory Warnings," *SAE Int. J. Transportation Safety* 4(1):107–112. A standardized battery
of visual, motor and cognitive tests was related to pedal behaviour at FCW activation; slower processing speed
was associated with still accelerating when the warning fired (r ≈ −.45 to −.52).

Differentiate on the deliverable, not the question: they correlated ability with **behaviour at a single fixed
warning time** and concluded only that optimal timing "may need to be larger than TTC = 4." **They did not fit
a lead-time curve, and did not estimate a per-person lead-time requirement.** That is exactly the gap H1 and H2
occupy, and citing it strengthens the framing rather than weakening it.

### Related design-canon framing

Automotive design guidance already holds that *the key to acceptance is knowing when **not** to warn* — the
same intuition behind §16.4 — but that principle has never been operationalized as an empirically derived
threshold for body-scale limb collisions. That gap, not the intuition, is the contribution.

### Adjacent XR work

- **"Belt and whistles — adding lower body collision awareness for MR experiences" (CHI 2026)** — haptic belt
  giving directional signals about **virtual** obstacles. Differentiate: it encodes direction on the torso,
  targets plausibility rather than physical safety, and does not manipulate warning timing.
- **Samsel et al. (2025), *Applied Ergonomics*** — vibrotactile **patterns** on a vest for obstacle early
  warning. Manipulates pattern, not lead time.
- **Ring, Tietenberg, Emmerich & Masuch (CHI 2024)** — validated Collision Anxiety Questionnaire, with
  follow-up work through CHI 2025. An active group in this problem space; consider adopting their instrument
  if any anxiety construct is measured.

### What the search did not find — the core gap looks clean (2026-09-14)

Searched several ways for a psychometric warning-lead-time function for **limb** collision avoidance with a
haptic cue — in VR, in motor control, and in the applied warning literature. **Nothing close came back.**
The XR side has cue *form* (patterns, belts, boundary visualizations) and cue *presence*, not cue *timing* as
a fitted threshold. The driving side works at 2–5 s TTC on a highly practised single-degree-of-freedom pedal
response. The motor-control side has online-correction latencies to tactile perturbations (≈88 ms muscle
onsets) but no warning-avoidance threshold.

This is a **provisional** result of targeted searching, not a systematic review. §1's novelty-freeze procedure
still applies in full, and it exists precisely because targeted searching has produced wrong prior-art claims
in this project before — including the two miscitations corrected in §19.

### Cross-check against Paper 1

Paper 1's novelty rests on the policy × mapping factorial; Paper 2's rests on connecting independently measured
tactile action cancellation to a closed-loop lead-time curve. Neither may cite the other as evidence of a gap.
Both novelty statements are frozen before submission, and if a search finds that either claim has been taken,
the affected paper is reframed rather than argued around.

## 19. Methodological anchors

- Verbruggen et al. (2019), stop-signal consensus guide: <https://doi.org/10.7554/eLife.46323>
- **Atsma, Maij, Gu, Medendorp & Corneil (2018)**, "Active Braking of Whole-Arm Reaching Movements Provides
  Single-Trial Neuromuscular Measures of Movement Cancellation," *J. Neurosci.* 38(18):4367–4382:
  <https://doi.org/10.1523/JNEUROSCI.1745-17.2018>
  *(Corrected 2026-09-14 — previously attributed here to "Venkataramani et al." The DOI was right and the
  authors were wrong.)*
- **Brunamonti, Ferraina & Paré (2012)**, "Controlled movement processing: Evidence for a common inhibitory
  control of finger, wrist, and arm movements," *Neuroscience* 215:69–78:
  <https://doi.org/10.1016/j.neuroscience.2012.04.051>
  *(Corrected 2026-09-15 — the DOI previously recorded here ended `.011`, one digit off, and resolves to an
  unrelated rat-epilepsy paper. Caught by resolving every DOI in the bibliography against Crossref. This is
  the **third** citation error found in this project; the other two had right DOIs and wrong authors, this
  one had the right authors and a wrong DOI. Resolve every DOI before submission — do not eyeball them.)*
- **Friehs, Schmalbrock, Merz, Dechant, Hartwigsen & Frings (2024)**, "A touching advantage: cross-modal
  stop-signals improve reactive response inhibition," *Exp. Brain Res.* 242(3):599–618:
  <https://doi.org/10.1007/s00221-023-06767-7>
  *(Corrected 2026-09-14 — previously attributed here to "Wadsley et al." Wadsley et al. is a real and
  different group working on selective stopping; the DOI was right and the authors were wrong.)*

  **This is motivation, not just a method anchor.** Friehs et al. report that **tactile stop-signals produce
  better reactive inhibition than visual ones**, and that stop-signal location matters. That is direct support
  for a tactile warning cue delivered on the at-risk effector, and it belongs in the introduction rather than
  buried in an anchors list.

- Hall, Jenkinson & MacDonald (2022), SSRT across two sessions (reliability and practice effects):
  <https://doi.org/10.1007/s00221-022-06480-x>
- Aksan et al. (2016), cognitive/motor predictors of FCW response:
  <https://doi.org/10.4271/2016-01-1439>
