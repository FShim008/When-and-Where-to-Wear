# Pre-registration — *When and Where to Warn: Predictive, Body-Localized Tactile Cues for Real-Obstacle Collision Avoidance in VR*

> **DRAFT (Plan Task 0.4 / A4).** Claude-authored to mirror the implemented analysis (`Analysis/analysis.R`)
> and design. The PI makes the final scientific calls on the bracketed `[…]` items (decisions D1–D8), fills N
> from the power analysis (`Analysis/power_analysis.R`) seeded by the pilot, and submits to [OSF / AsPredicted]
> **before** confirmatory data collection. Everything below is *confirmatory* unless flagged exploratory.

## 1. Study information
- **Title:** When and Where to Warn: Predictive, Body-Localized Tactile Cues for Real-Obstacle Collision Avoidance in VR.
- **Authors:** [PI + collaborators].
- **Description:** A controlled VR user study testing whether **predictive timing** and **body-localized**
  vibrotactile cues reduce collisions with **real** physical obstacles while preserving presence.
- **Hypothesis-generating vs confirmatory:** confirmatory; primary analyses and N fixed before data collection.

## 2. Design
- **Type:** within-subjects, repeated measures. Each participant completes **6 condition blocks** + 1 excluded practice block.
- **Core factorial:** **2 × 2 — Timing {Reactive, Predictive} × Localization {Generic, Body-localized}**, realized by the four cells **RG, RB, PG, PB**.
- **Anchors outside the 2×2:** **None** (no-feedback floor) and **Visual** (best-practice, detection-matched visual reference).
- **Conditions (6):** None, RG, RB, PG, PB (= full technique), Visual.
- **Counterbalancing:** condition order via a **6×6 Williams Latin square** (`SessionPlan.For`, implemented); obstacle **layout** rotated and treated as a random effect.
- **Task:** room-scale reach/dodge driven by a scripted **12-opportunity** timeline per block (onsets 8–152 s; `OpportunityScheduler` / Layout-L1 storyboard); opportunities are **script-and-clock driven, never derived from the participant's own motion** (removes opportunity-circularity).

## 3. Variables
### 3.1 Manipulated
- **Timing** (Reactive = fire on current distance < D; Predictive = fire on forecast TTC < T).
- **Localization** (Generic = chest cue; Body-localized = cue the at-risk limb's site).
- (Cue **waveform is identical** across all haptic conditions — only *when* and *where* differ; the same 3-pulse cue, with per-site **perceived-intensity equalization** applied so salience is matched, Plan Task 3.1.)

### 3.2 Outcomes
- **PRIMARY DV:** **collisions per opportunity** (count of real-obstacle collisions / 12 opportunities per block).
- **Secondary (confirmatory):** **presence** (IPQ overall), **workload** (NASA-TLX overall).
- **Secondary (safety/quality):** **simulator sickness** (SSQ total), near-misses, avoidance count + latency, minimum clearance.
- **Manipulation checks:** alert counts per condition; verification that predictive alerts precede reactive ones; cue-intensity equalization log.

## 4. Hypotheses
- **H1 (Timing).** Predictive cues reduce collisions per opportunity vs reactive (main effect of Timing).
- **H2 (Localization).** Body-localized cues reduce collisions vs generic (main effect of Localization).
- **H3 (Interaction / full technique).** The combination **PB** yields the lowest collision rate; tested as the
  Timing×Localization interaction and the **PB-best** pairwise pattern (PB < each of RG, RB, PG).
- **H4 (vs best practice).** PB reduces collisions relative to the **Visual** reference.
- **Floor.** Each feedback condition reduces collisions relative to **None** (anchors absolute reduction).
- **Presence (H5, secondary).** Haptic conditions preserve presence relative to Visual / do not reduce presence vs None [directional call: [non-inferiority vs None? PI decides]].
- **Workload (H6, secondary, exploratory-leaning).** [predicted direction for NASA-TLX, PI decides].

## 5. Analysis plan (matches `Analysis/analysis.R`)
- **Primary model (negative-binomial GLMM, `glmmTMB`, `nbinom2`):**
  `collisions ~ Timing * Localization + alerts_c + offset(log(opportunities)) + (1 | participant) + (1 | layout)`
  on the RG/RB/PG/PB cells. `alerts_c` = mean-centered alert count (covariate). H1/H2 = main effects; H3 = the interaction term.
- **Estimated marginal means + contrasts:** `emmeans` on the response scale; **PB-best** pairwise tests **Holm-adjusted**.
- **Floor model:** `collisions ~ condition (ref = None) + offset(log(opportunities)) + (1|participant) + (1|layout)`, `nbinom2`, over all 6 conditions.
- **H4 model:** `collisions ~ condition (ref = Visual) + offset(...) + (1|participant) + (1|layout)` on {PB, Visual}.
- **Secondary measures** (`lme4::lmer`): `presence ~ Timing*Localization + (1|participant)+(1|layout)`; `overall (TLX) ~ Timing*Localization + (1|participant)+(1|layout)`; `SSQ total ~ condition + block + (1|participant)` (block = order covariate).
- **Overdispersion:** checked (`performance::check_overdispersion`); NB chosen a priori for the bounded ≤12 counts and expected zero-inflation toward strong cues.
- **Alpha:** .05, two-tailed. **Multiple comparisons:** Holm within the primary pairwise family; secondary measures reported with [correction/family per PI].
- **Inference target:** the fixed-effect coefficients/contrasts above; random intercepts for participant and layout.

## 6. Sample size & stopping rule
- **N:** **[N from `power_analysis.R`]**, the smallest N giving **≥ 80% power** for the **Timing×Localization interaction** under pilot-estimated rates/dispersion (primary power target). Power for the floor and H4 contrasts reported alongside.
- **Opportunities/block:** **[12, or raised per the power sim]** (extend `OpportunityScheduler` if increased).
- **Stopping rule:** collect to the fixed N (no optional stopping / no peeking that affects inference). Replacement rule below.
- **Pilot:** a small pilot (Phase 8) estimates rates + dispersion to finalize N; pilot data are **not** included in the confirmatory dataset [unless pre-specified otherwise].

## 7. Data exclusion & handling rules (pre-specified)
- **Practice block excluded** from all analyses (written to separate `practice_*` files).
- **Participant exclusion / replacement:** a participant who withdraws or is **stopped for simulator sickness** before completing all blocks is **replaced** to preserve the counterbalance; their partial data are reported but excluded from confirmatory models [PI confirms].
- **Collision attribution:** a detected collision is attributed to the opportunity open for that limb at the time (`OpportunityScheduler.ActiveFor`); unattributed collisions are logged separately and **[excluded from / included in]** the primary count [decision].
- **Multi-limb / simultaneous events (E12 compound):** [policy — cue lowest-TTC limb; collisions counted per-limb? PI decides].
- **Outliers:** no removal of individual counts; influence checked via standard GLMM diagnostics, reported but not used to drop data unless pre-specified.
- **Missing questionnaires:** a block missing an instrument contributes to the count models but not that secondary model (listwise per model).

## 8. Confounds controlled (a priori)
- **Cue salience:** identical waveform across haptic conditions + **per-site perceived-intensity equalization** (chest X40 40-motor vs 3-motor Tactosy) so Localization is not confounded with stimulus energy.
- **Opportunity circularity:** opportunities are scripted (clock-driven), independent of behavior.
- **Detection matching:** the Visual reference uses the **same** reactive distance D and tracking as the haptic conditions.
- **Alert frequency:** included as a covariate (`alerts_c`).
- **Order/learning:** Williams-square counterbalancing + excluded practice + layout as a random effect.

## 9. Decisions to finalize before lock (D1–D8)
- D1 IRB/safety (see `docs/SAFETY_PROTOCOL.md`) · D2 **N** + #opportunities (power sim) · D3 cue-intensity matching method · D4 obstacle coordinates (design docs) · D5 Visual-modality match details · D6 limb-collision definition + `LimbContactRadius` · D7 this pre-registration · D8 multi-limb policy.

## 10. Exploratory (clearly labeled, non-confirmatory)
- Per-limb / per-obstacle collision patterns; time-course of avoidance latency; presence×workload trade-off; individual-difference moderators. Reported as exploratory; not part of the confirmatory error budget.

*Draft created 2026-06-22 (A4). Models verified against `Analysis/analysis.R`; N pending `Analysis/power_analysis.R` + pilot.*
