# Paper 2 — Related Work

**"Stopping a Reach in VR: Measuring the Warning Time Required for Vibrotactile Collision Avoidance"**

**Compiled** 2026-09-14 · **Metadata completed and reviewer-audited** 2026-09-15
**Sources:** `../PAPER2_STUDY_DESIGN.md` §1, §18, §19 · `PAPER2_NOVELTY_SEARCH_LOG_*.txt` · `VIRTUAL_HAZARD_VALIDITY.md`
**Verification:** every citation below resolved against Crossref on 2026-09-15. Status per entry in **Part IV**.

| Part | Contents |
|---|---|
| **I** | **Manuscript-ready Related Work** — ISMAR prose style, drop into the paper |
| **II** | Reviewer audit — what was missing, and what changed |
| **III** | Near-neighbour differentiation (rebuttal prep) |
| **IV** | Bibliography with verification status |
| **V** | Search record and stated limitations |

---

# PART I — Manuscript-ready Related Work

> Citation keys are `[Author Year]`; swap for numeric keys at typesetting.
> **Rev 3, 2026-09-16** — revised against J. Quarles' ten comments. Flesch-Kincaid grade 14.5 → 7.8; longest
> sentence 51 → 29 words; word count essentially unchanged (943 → 907), so the section is plainer, not shorter.
> Full comment-by-comment response in `PAPER2_RELATED_WORK_TEXT.txt`.

## 2 RELATED WORK

### 2.1 Physical Safety in Immersive Environments

A VR user cannot see the real room, so keeping them safe has long been a concern. Most systems handle this by managing space. Boundary systems mark out a safe area and show it when the user gets close. The magic barrier tape introduced this idea [Cirio 2009], and guardian and chaperone systems have made it standard. Wu et al. [Wu 2023] found that how a boundary is shown changes whether users stay inside it. Other work prevents collisions instead of warning about them. Redirected walking steers the user away from real obstacles [Kim 2023, Langbehn 2018], and substitutional reality maps virtual objects onto real furniture [Simeone 2015].

A second group of systems finds real obstacles and shows them to the user. These use peripheral vision [Kim 2024], stylized overlays [Kim 2021], a narrowed field of view [Wu 2019], or mixed-reality insets [Valentini 2020, Kanamori 2018]. Related work covers collisions between two users in one room [Bachmann 2013, Sra 2016] and alerts for obstacles that are detected but not drawn [Wozniak 2018].

The difficulty is treated as getting information to the user. Once the boundary is drawn or the obstacle is shown, the problem is considered solved. No one has tested whether the warning arrives in time for the user to act on it.

### 2.2 Vibrotactile Cues for Collision Signalling

Vibration is a well-established way to signal collisions in VR. Body-worn cues came before most of the visual methods above. Bloomfield and Badler [Bloomfield 2007] drove a sleeve of tactors from a real-time body model, so the vibration reached the arm segment that touched a virtual object. Arm- localized vibration beat visual feedback alone. Later work varied the cue itself. Examples include patterns that encode distance [Kim 2015], pattern comparisons for warning vests [Samsel 2025], proximity modes in robot teleoperation [deBarros 2012], and belts that signal the direction of low obstacles [Abdlkarim 2026]. Driving research has studied tactile and multisensory warnings in depth [Spence 2008], including moving patterns that show where a collision will come from [Ahtamad 2016]. Ahmed [Ahmed 2025] recently proposed choosing between warning modalities based on context.

The closest work to ours is Bajpai et al. [Bajpai 2020]. They compared tactile, audio, and visual cues while people dodged objects flying toward them in VR. Their setup is the reverse of ours. Their threat moves and the person stands still; our person moves toward a hazard that stays put. They measure how fast someone starts a dodge. We measure whether someone can stop a movement they have already begun. More important, all of this work changes what the cue is, or whether there is a cue at all. None changes when it arrives.

### 2.3 Warning Timing in Applied Human Factors

Driving research has tested warning timing directly. Abe and Richardson measured how the timing of forward-collision alarms affects braking. They found it affects trust even more: drivers who received late alarms stopped responding to later ones [Abe 2004, Abe 2005, Abe 2006]. Zhang and Wu [Zhang 2018] crossed warning lead time with reliability and wording. Command warnings beat notifications at a 2.5 s lead, but not at 4.5 s. This is the closest test of warning lead time we found, and the direct precedent for our lead time x tempo comparison. Aksan et al. [Aksan 2016] linked cognitive and motor test scores to what drivers were doing when the alarm fired. Slower drivers were often still accelerating. Other work tunes warning timing to the individual driver [Xie 2024, Shao 2023].

Three things separate that work from ours. First, it warns 2-5 s before impact. That is about ten times longer than the window for stopping a reach. Second, it studies a foot moving to a pedal, which is a simple and well- practiced response. We study a whole arm in mid-flight, competing with a movement the person has already planned. Third, it compares a few fixed timing levels instead of measuring a curve. Zhang and Wu used two levels, and Aksan et al. used one.

### 2.4 Stopping a Movement Already Underway

Motor-control research already knows how to measure whether a person can stop a movement in progress. The stop-signal task is the standard method, and its instructions are known to change the result [Verbruggen 2019]. Atsma et al. [Atsma 2018] showed that stopping a whole-arm reach leaves a clear muscle signature on single trials. Stopping a reach is therefore a measurable event, not a vague idea. The same inhibitory control appears to serve fingers, wrists, and arms [Brunamonti 2012]. Friehs et al. [Friehs 2024] found that touch-based stop signals work better than visual ones, and that where the signal is felt matters. That is why we deliver the warning by vibration, on the limb at risk.

This work does not give a warning threshold. It measures how fast a person can cancel an action when cancelling is the only task. It does not measure how much advance notice a person needs while still trying to reach a target.

### 2.5 Safety Standards and the Missing Number

Industrial machine safety already asks our question in a different form: how far from a hazard must a guard sit so that a hand cannot reach it in time? ISO 13855 [ISO13855] answers with S = K(t1 + t2) + C, where K = 2000 mm/s is a fixed hand speed and t1 is a human reaction time. This gives a distance, not a probability of avoiding the hazard. It treats hand speed as one worst-case number rather than a measured range. It also assumes the machine stops while the person keeps moving. In VR the reverse is true, because nothing stops but the person. We were not able to find any published study that reports how K was measured.

We therefore measure the missing number directly. We treat warning time as the input to a psychometric function [Wichmann 2001a], and time the cue against predicted time-to-contact [Lee 1976]. The result is the probability of stopping a reach before it crosses a hazard boundary, as a function of how much warning the person actually received.

---

# PART II — Reviewer audit (2026-09-15)

Read as an ISMAR reviewer against the previous draft. Four findings, in severity order.

### 🔴 R1 — VR physical-safety literature was absent entirely

The previous draft opened with body-localized vibrotactile feedback and never cited **guardian/boundary
systems, redirected walking, or substitutional reality.** For an ISMAR paper about keeping a VR user from
colliding with something, that is the reviewer's own home literature, and its absence reads as not knowing
the field.

**This was the most likely single cause of a reject.** §2.1 is new and now opens the section, citing
Cirio 2009, Wu 2023, Kim 2023, Langbehn 2018, Simeone 2015, plus the detection-and-display line. It also does
useful work: it lets us name the field's shared assumption — *that the problem is informing the user* — and
locate our contribution against it, which is a stronger opening than starting with haptics.

### 🔴 R2 — No psychometric-fitting methodology cited

The paper's entire deliverable is **a fitted psychometric threshold with an interval**, and the previous draft
cited nothing on how such functions are fitted or how their intervals are obtained. A psychophysics-literate
reviewer would flag this immediately, and it undercuts the analysis plan's credibility.

Added: Wichmann & Hill 2001a/2001b (fitting, goodness of fit, bootstrap intervals), Treutwein & Strasburger
1999, Prins 2023. This also **retroactively justifies `e2_analysis.R`'s choice of bootstrap over delta-method
intervals** — that decision now has a citable basis rather than being an implementation preference.

### 🟠 R3 — Time-to-contact had no anchor

The oracle times the cue against a predicted contact. Lee's tau formulation [Lee 1976] is the canonical
reference for visual control of braking from time-to-collision information and is conspicuous by its absence
in a paper built on a TTC estimator. Added in §2.5.

### 🟠 R4 — The false-alarm bound was asserted without a citation

The design document repeatedly (and correctly) notes that trust and compliance effects cannot be tested here
because the warnings are veridical. That argument is much stronger with the cry-wolf literature attached:
Bliss et al. 1995 is now cited to bound what the estimate claims.

### Two citations removed

| Removed | Why |
|---|---|
| **"Wang et al. (2015)"**, personalizing collision warning | **Could not be verified.** Four query formulations returned nothing matching. It came from §18 without a DOI. Xie 2024 and Shao 2023 cover the same point and are verified. **Do not reinstate without a DOI.** |
| **"RealityAlert (2018)", SUI** | **Could not be verified.** No Crossref match under that or related titles. Possibly a garbled title in the venue-browse notes. Wozniak 2018 covers SUI-venue obstacle notification and is verified. |

### Corrections to citations that survived

| Was | Now |
|---|---|
| "Barros & Lindeman (2012)" | **de Barros, P. & Lindeman, R.** — the surname particle is part of the name |
| "Samsel et al. (2025)" | **Samsel, Z., Gunia, A., Jäger, M. & Schöning, J.** — full author list recovered |
| "Belt and whistles (CHI 2026)" | **Abdlkarim, D., Mukherjee, D., Giunchi, D., Di Luca, M. et al.** — and the DOI matches the one already in `VIRTUAL_HAZARD_VALIDITY.md` |
| "Ring, Tietenberg, Emmerich & Masuch (CHI 2024)" | ✅ **author list was already correct** — full title and DOI now added |
| "Abe & Richardson (2004–2006)" | **Three distinct papers**, now separated: 2004 (Transp. Res. F), 2005 (Safety Science), 2006 (Applied Ergonomics) |

### Style changes

Rewritten to match recent ISMAR related-work sections: continuous prose rather than bullet lists; prior work in
past tense; each subsection closing with an explicit differentiation rather than a standalone "gap" paragraph;
comparative tables moved out of the manuscript text into Part III; the working apparatus (verification,
search limitations) separated from the paper text entirely. First-person plural, no second person, no
boldface emphasis in the prose.

### Still open

- **Ring et al.** also have a German-language validation [Ring 2024b] and an earlier VRW short [Ring 2023].
  Cite the CHI paper; the others only if an anxiety measure is actually used.
- **ISO 13855 Annex A** still needs a library copy to check for primary sources (§V).
- `Bliss 1995` is a 1995 paper on a 1990s alarm paradigm. If a reviewer wants something current, a modern
  automation-trust review would serve; not yet searched.

---

# PART III — Near-neighbour differentiation

For the rebuttal file. Each row is a paper a reviewer may raise as "already done".

| Work | Their claim | Why it does not pre-empt this |
|---|---|---|
| **Bloomfield 2007** | Arm-localized vibrotactile collision feedback beats visual | Fires **on contact** — no lead time exists to manipulate, no threshold to fit |
| **Bajpai 2020** | Tactile cues improve evasion of moving threats in VR | **Scenario inversion** — threat moves, person stationary. Starting a dodge, not stopping a reach. Modality, not timing |
| **Zhang 2018** | Lead time × reliability × style in driving | **Two levels, not a curve.** Pedal, not limb. 2.5–4.5 s, not 100–800 ms |
| **Abe 2004/05/06** | Alarm timing shapes trust more than braking | 2–5 s TTC; trust effects require an *unreliable* system, which ours is not by construction |
| **Aksan 2016** | Cognitive ability predicts FCW response | **Single fixed warning time**; no fitted curve, no per-person requirement |
| **Wu 2023** | Guardian awareness techniques improve VR safety | Boundary *presentation*, not warning *timing*; no threshold |
| **ISO 13855** | `S = K(t₁+t₂) + C` already specifies hand safety distance | No avoidance probability; `K` is a worst-case constant; **assumes the machine stops** |
| **Ahmed 2025** | Context-aware collision warning modalities | A **framework proposal**, not a completed study; modality, not timing |
| **Samsel 2025** | Vibrotactile patterns for obstacle early warning | Manipulates **pattern**, not lead time |
| **Kanamori 2018 / Valentini 2020 / Kim 2021** | Obstacle display methods for VR | Display design; cue timing is a constant |

## ⚠ Live competitive risk

**Ahmed, N. (2025)** — *Investigating the Impact of Context-Aware Collision Warning Modalities on Dual-Task
Performance*, ISMAR-Adjunct 2025, pp. 1010–1011, doi:10.1109/ISMAR-Adjunct68609.2025.00293.

Someone is working this space at ISMAR **now**. It is a two-page adjunct framework proposal manipulating
modality rather than timing, so it does not disconfirm — but **cite it**. Check for a full version at ISMAR
2026, set an author alert, and treat a concurrent ISMAR 2027 submission as live.

---

# PART IV — Bibliography

All entries resolved against Crossref on **2026-09-15** unless marked. DOIs are as registered (lowercase in
places); normalize at typesetting.

### XR collision awareness and physical safety

| Key | Citation | DOI |
|---|---|---|
| Cirio 2009 | Cirio, G., Marchal, M., Regia-Corte, T. & Lécuyer, A. The magic barrier tape. *VRST 2009*, 155–162. | 10.1145/1643928.1643965 |
| Wu 2023 | Wu, S., Li, J., Sousa, M. & Grossman, T. Investigating Guardian Awareness Techniques to Promote Safety in Virtual Reality. *IEEE VR 2023*, 631–640. | 10.1109/vr55154.2023.00078 |
| Kim 2023 | Kim, D. & Woo, W. Edge-Centric Space Rescaling with Redirected Walking for Dissimilar Physical-Virtual Space Registration. *ISMAR 2023*, 829–838. | 10.1109/ismar59233.2023.00098 |
| Langbehn 2018 | Langbehn, E., Lubos, P. & Steinicke, F. Evaluation of Locomotion Techniques for Room-Scale VR. *VRIC 2018*, 1–9. | 10.1145/3234253.3234291 |
| Simeone 2015 | Simeone, A., Velloso, E. & Gellersen, H. Substitutional Reality. *CHI 2015*, 3307–3316. | 10.1145/2702123.2702389 |
| Bachmann 2013 | Bachmann, E., Holm, J., Zmuda, M. & Hodgson, E. Collision prediction and prevention in a simultaneous two-user immersive virtual environment. *IEEE VR 2013*, 89–90. | 10.1109/vr.2013.6549377 |
| Sra 2016 | Sra, M. Asymmetric Design Approach and Collision Avoidance Techniques for Room-scale Multiplayer VR. *UIST 2016 Adjunct*, 29–32. | 10.1145/2984751.2984788 |
| Kanamori 2018 | Kanamori, K., Sakata, N., Tominaga, T., Hijikata, Y., Harada, K. et al. Obstacle Avoidance Method in Real Space for Virtual Reality Immersion. *ISMAR 2018*, 80–89. | 10.1109/ismar.2018.00033 |
| Valentini 2020 | Valentini, I., Ballestin, G., Bassano, C., Solari, F. et al. Improving Obstacle Awareness to Enhance Interaction in Virtual Reality. *IEEE VR 2020*, 44–52. | 10.1109/vr46266.2020.00022 |
| Wu 2019 | Wu, F. & Rosenberg, E. Combining Dynamic Field of View Modification with Physical Obstacle Avoidance. *IEEE VR 2019*. | 10.1109/vr.2019.8798015 |
| Kim 2024 | Kim, M., Song, K., Lim, Y. & Yoon, S. Collision Prevention in Diminished Reality through the Use of Peripheral Vision. *UIST 2024 Adjunct*, 1–3. | 10.1145/3672539.3686346 |
| Kim 2021 | Kim, J., Jeong, H. & Kim, G. The Effect of 2D Stylized Visualization of the Real World for Obstacle Avoidance and Safety. *VRST 2021*, 1–3. | 10.1145/3489849.3489943 |
| Wozniak 2018 | Wozniak, P., Capobianco, A., Javahiraly, N. & Curticapean, D. Towards Unobtrusive Obstacle Detection and Notification for VR. *SUI 2018*, 188. | 10.1145/3267782.3274682 |
| Kang 2019 | Kang, H., Lee, G. & Han, J. SafeAR: AR Alert System Assisting Obstacle Avoidance for Pedestrians. *ISMAR-Adjunct 2019*, 81–82. | 10.1109/ismar-adjunct.2019.00035 |
| Ahmed 2025 | Ahmed, N. Investigating the Impact of Context-Aware Collision Warning Modalities on Dual-Task Performance. *ISMAR-Adjunct 2025*, 1010–1011. | 10.1109/ismar-adjunct68609.2025.00293 |
| Ring 2024 | Ring, P., Tietenberg, J., Emmerich, K. & Masuch, M. Development and Validation of the Collision Anxiety Questionnaire for VR Applications. *CHI 2024*, 1–13. | 10.1145/3613904.3642408 |

### Vibrotactile collision cues

| Key | Citation | DOI |
|---|---|---|
| Bloomfield 2007 | Bloomfield, A. & Badler, N. Collision Awareness Using Vibrotactile Arrays. *IEEE VR 2007*, 163–170. | 10.1109/vr.2007.352477 |
| Bajpai 2020 | Bajpai, A., Powell, J. C., Young, A. J. & Mazumdar, A. Enhancing Physical Human Evasion of Moving Threats Using Tactile Cues. *IEEE Trans. Haptics* 13(1), 32–37. | 10.1109/toh.2019.2962664 |
| Abdlkarim 2026 | Abdlkarim, D., Mukherjee, D., Giunchi, D., Di Luca, M. et al. Belt and whistles — adding lower body collision awareness for MR experiences. *CHI 2026*, 1–12. | 10.1145/3772318.3793143 |
| Samsel 2025 | Samsel, Z., Gunia, A., Jäger, M. & Schöning, J. A comparison of vibrotactile patterns in an early warning system for obstacle detection. *Applied Ergonomics* 122, 104396. | 10.1016/j.apergo.2024.104396 |
| deBarros 2012 | de Barros, P. & Lindeman, R. Poster: Comparing vibro-tactile feedback modes for collision proximity feedback in USAR virtual robot teleoperation. *IEEE 3DUI 2012*, 137–138. | 10.1109/3dui.2012.6184199 |
| Kim 2015 | Kim, Y., Harders, M. & Gassert, R. Identification of Vibrotactile Patterns Encoding Obstacle Distance Information. *IEEE Trans. Haptics* 8(3), 298–305. | 10.1109/toh.2015.2415213 |
| Spence 2008 | Spence, C. & Ho, C. Tactile and Multisensory Spatial Warning Signals for Drivers. *IEEE Trans. Haptics* 1(2), 121–129. | 10.1109/toh.2008.14 |
| Ahtamad 2016 | Ahtamad, M., Spence, C., Ho, C. & Gray, R. Warning Drivers about Impending Collisions Using Vibrotactile Flow. *IEEE Trans. Haptics* 9(1), 134–141. | 10.1109/toh.2015.2501798 |

### Warning timing in applied human factors

| Key | Citation | DOI |
|---|---|---|
| Abe 2004 | Abe, G. & Richardson, J. The effect of alarm timing on driver behaviour: an investigation of differences in driver trust and response to alarms according to alarm timing. *Transp. Res. F* 7(4–5), 307–322. | 10.1016/j.trf.2004.09.008 |
| Abe 2005 | Abe, G. & Richardson, J. The influence of alarm timing on braking response and driver trust in low speed driving. *Safety Science* 43(9), 639–654. | 10.1016/j.ssci.2005.04.006 |
| Abe 2006 | Abe, G. & Richardson, J. Alarm timing, trust and driver expectation for forward collision warning systems. *Applied Ergonomics* 37(5), 577–586. | 10.1016/j.apergo.2005.11.001 |
| Zhang 2018 | Zhang, Y. & Wu, C. Modeling the Effects of Warning Lead Time, Warning Reliability and Warning Style on Human Performance under Connected Vehicle Settings. *Proc. HFES* 62(1), 701. | 10.1177/1541931218621158 |
| Aksan 2016 | Aksan, N., Sager, L., Hacker, S., Marini, R., Dawson, J., Anderson, S. & Rizzo, M. Forward Collision Warning: Clues to Optimal Timing of Advisory Warnings. *SAE Int. J. Transp. Safety* 4(1), 107–112. | 10.4271/2016-01-1439 |
| Xie 2024 | Xie, N., Yu, R., Sun, W., Qiu, S., Zhong, K. et al. Personalized forward collision warning model with learning from human preferences. *Accid. Anal. Prev.* 208, 107791. | 10.1016/j.aap.2024.107791 |
| Shao 2023 | Shao, Y., Shi, X., Zhang, Y., Zhang, Y., Xu, Y. et al. Adaptive forward collision warning system for hazmat truck drivers: Considering differential driving behavior and risk levels. *Accid. Anal. Prev.* 191, 107221. | 10.1016/j.aap.2023.107221 |
| Bliss 1995 | Bliss, J., Dunn, M. & Fuller, B. Reversal of the Cry-Wolf Effect: An Investigation of Two Methods to Increase Alarm Response Rates. *Percept. Mot. Skills* 80(3 suppl), 1231–1242. | 10.2466/pms.1995.80.3c.1231 |

### Movement cancellation and tactile inhibition

| Key | Citation | DOI |
|---|---|---|
| Verbruggen 2019 | Verbruggen, F. et al. A consensus guide to capturing the ability to inhibit actions and impulsive behaviors in the stop-signal task. *eLife* 8, e46323. | 10.7554/eLife.46323 |
| Atsma 2018 | Atsma, J., Maij, F., Gu, C., Medendorp, W. P. & Corneil, B. D. Active Braking of Whole-Arm Reaching Movements Provides Single-Trial Neuromuscular Measures of Movement Cancellation. *J. Neurosci.* 38(18), 4367–4382. | 10.1523/JNEUROSCI.1745-17.2018 |
| Brunamonti 2012 | Brunamonti, E., Ferraina, S. & Paré, M. Controlled movement processing: Evidence for a common inhibitory control of finger, wrist, and arm movements. *Neuroscience* 215, 69–78. | 10.1016/j.neuroscience.2012.04.051 |
| Friehs 2024 | Friehs, M., Schmalbrock, P., Merz, S., Dechant, M., Hartwigsen, G. & Frings, C. A touching advantage: cross-modal stop-signals improve reactive response inhibition. *Exp. Brain Res.* 242(3), 599–618. | 10.1007/s00221-023-06767-7 |
| Hall 2022 | Hall, M., Jenkinson, N. & MacDonald, H. Exp. Brain Res. | 10.1007/s00221-022-06480-x |

### Psychophysical method, time-to-contact, standards

| Key | Citation | DOI |
|---|---|---|
| Wichmann 2001a | Wichmann, F. & Hill, N. The psychometric function: I. Fitting, sampling, and goodness of fit. *Percept. Psychophys.* 63(8), 1293–1313. | 10.3758/bf03194544 |
| Wichmann 2001b | Wichmann, F. & Hill, N. The psychometric function: II. Bootstrap-based confidence intervals and sampling. *Percept. Psychophys.* 63(8), 1314–1329. | 10.3758/bf03194545 |
| Treutwein 1999 | Treutwein, B. & Strasburger, H. Fitting the psychometric function. *Percept. Psychophys.* 61(1), 87–106. | 10.3758/bf03211951 |
| Prins 2023 | Prins, N. Easy, bias-free Bayesian hierarchical modeling of the psychometric function using the Palamedes toolbox. *Behav. Res. Methods* 56(1), 485–499. | 10.3758/s13428-023-02061-0 |
| Lee 1976 | Lee, D. N. A Theory of Visual Control of Braking Based on Information about Time-to-Collision. *Perception* 5(4), 437–459. | 10.1068/p050437 |
| ISO13855 | ISO 13855, *Safety of machinery — Positioning of safeguards with respect to the approach speeds of parts of the human body.* | 10.3403/30160694 (BSI record) |

### Optional / conditional

| Key | Citation | Use when |
|---|---|---|
| Ring 2023 | Ring, P. & Masuch, M. Measuring Collision Anxiety in XR Exergames. *IEEE VRW 2023*, 627–628. doi:10.1109/vrw58643.2023.00156 | only if anxiety is measured |
| Ring 2024b | Ring, P., Tietenberg, J. & Masuch, M. Validation of a German Version of the CAQ. *MuC 2024*, 370–374. doi:10.1145/3670653.3677516 | German-language administration |
| Azadi 2014 | Azadi, M. & Jones, L. Vibrotactile actuators: Effect of load and body site on performance. *IEEE Haptics Symposium 2014*, 351–356. doi:10.1109/haptics.2014.6775480 | if tactor-mounting effects are discussed |
| Wadsley 2023 | Wadsley, C., Cirillo, J., Nieuwenhuys, A. & Byblow, W. A global pause generates nonselective response inhibition during selective stopping. doi:10.1101/2023.03.02.530898 | if selective stopping is discussed |

> **⚠ THREE citation errors have now been found in this project. Resolve every DOI; never eyeball one.**
>
> | Found | Error | How it presented |
> |---|---|---|
> | 2026-09-14 | `Atsma 2018` attributed to "Venkataramani et al." | **right DOI, wrong authors** |
> | 2026-09-14 | `Friehs 2024` attributed to "Wadsley et al." | **right DOI, wrong authors** |
> | **2026-09-15** | `Brunamonti 2012` recorded as `…2012.04.011` | **right authors, wrong DOI** — one digit off, and it resolves to a rat-epilepsy paper |
>
> The third was caught only by resolving all 48 DOIs in this bibliography against Crossref, which is now the
> required pre-submission check. Wadsley et al. is a real and separate group (see Optional, above).
>
> **Two entries in the verified list were previously miscited in this project** and corrected on
> 2026-09-14: `Atsma 2018` was attributed to "Venkataramani et al." and `Friehs 2024` to "Wadsley et al."
> **In both cases the DOI was right and the author list was wrong.** Wadsley et al. is a real and separate
> group (above). Check author lists against the DOI, never against memory.

---

# PART V — Search record and limitations

**Method.** Ten core queries plus an eight-query adversarial disconfirmation pass; Crossref DOI-prefix
filtering for ACM (10.1145) and IEEE (10.1109); venue browsing across ISMAR, IEEE VR/3DUI, TVCG, IEEE ToH,
CHI, UIST, SUI and VRST. Metadata completion pass 2026-09-15: 33 Crossref lookups, each candidate screened by
title match rather than accepted on rank.

**The core finding:** every VR/AR collision-warning paper found manipulates *what the cue is* or *whether it
appears*. **None manipulates when it appears, across multiple levels, with a fitted threshold.**

### Limitations to state in the paper

1. **One screener.** §1 requires a second screener on borderline hits; there is one person on this project.
   The substitute — eight queries written to *disconfirm* rather than confirm — is recorded as a substitute,
   not an equivalent.
2. **No ACM DL / IEEE Xplore interface search.** Programmatic access is blocked (`dl.acm.org` 403,
   `ieeexplore.ieee.org` 418, DBLP bot challenge, Semantic Scholar 429). Crossref DOI-prefix filtering gave
   true publisher-scoped coverage; **eyeball confirmation through both interfaces remains a manual action.**
3. **A discarded negative result.** OpenAlex publisher-lineage filtering failed its own positive controls —
   0 ACM hits for a paper definitively in the ACM DL — so those zeros were discarded rather than reported as
   evidence of absence. Recorded because a reviewer asking "did you search ACM?" deserves the real answer.
4. **No Scopus or Web of Science pass.** Neither is reachable from this environment; UTSA library access
   would close it.
5. **Two citations were dropped as unverifiable** ("Wang et al. 2015"; "RealityAlert 2018"). Neither claim
   depends on them.
6. **The closest paper in the entire search was found only by venue inspection** [Bajpai 2020] — invisible to
   ten keyword queries. Assume others hide the same way; finish the manual proceedings scan
   (`PAPER2_MANUAL_SEARCH_PROCEDURE.md` Part 3).
7. **ISO 13855 Annex A** still requires a library copy to check whether `K = 2000 mm/s` cites primary sources.
   The manuscript now says only *"we were not able to find any published study that reports how K was
   measured"* (per comment 9 — the earlier speculation about grey literature was cut as verbose). The reason
   for the speculation is kept here because it explains the empty result: `"hand speed" AND "safety distance"`
   returns **0** in OpenAlex, as does `"approach speed" AND human AND measurement AND guard`. Vendor
   documentation calls K "empirical data", which points at the standard's own annexes and at German institute
   grey literature (IFA/BGIA) — outside the databases searched. Do not put that inference in the paper without
   the library copy.
8. **Re-run before submission.** The novelty statement is frozen and dated; if a later search finds the claim
   taken, the paper is reframed rather than argued around.
