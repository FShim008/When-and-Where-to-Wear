# =============================================================================
# PAPER 1 — CONFIRMATORY ANALYSIS (opportunity-level)
#   "What Makes a Collision Warning Work?"   [PAPER1_STUDY_DESIGN §3, §5, §9; CODE_GAP #1]
#
# Run:  Rscript paper1_analysis.R <opportunities.csv> [block_summary.csv]
#
# Writes: p1_estimates.csv, p1_flow.csv, p1_diagnostics.txt
#
# THIS REPLACES analysis.R AS THE PRIMARY MODEL.
#
# analysis.R fits a negative-binomial GLMM on BLOCK-LEVEL collision counts with an
# offset(log(opportunities)). That was the old design. The primary outcome is now a
# PER-OPPORTUNITY BINARY VIOLATION (§5, §9), which is a different unit, a different
# denominator, and a different family. `analysis.R` is retained as the block-level
# SENSITIVITY analysis and is run from here when its file is supplied.
#
# -----------------------------------------------------------------------------
# WHY THE UNIT CHANGED, AND WHY THE DENOMINATOR IS NOT 12
# -----------------------------------------------------------------------------
# A block does not present a fixed number of usable opportunities. Some are never
# presented; some are cut short by an abort or a tracking dropout. Dividing by a
# planned 12 would credit the participant with avoiding opportunities that never
# happened, and the bias is not random - it follows whatever went wrong that block.
#
# So the denominator is "presented AND valid", counted per row, and the model is
# binomial on those rows.
# =============================================================================

suppressMessages(library(lme4))

args <- commandArgs(trailingOnly = TRUE)
OPP  <- if (length(args) >= 1) args[1] else "opportunities.csv"
BLK  <- if (length(args) >= 2) args[2] else NA_character_
set.seed(20260921)

if (!file.exists(OPP)) stop("No such file: ", OPP)

# ---------------------------------------------------------------------------
# 0. THE CSV CONTRACT — OpportunityOutcomeFormatter.HeaderLine
# ---------------------------------------------------------------------------
REQUIRED <- c("participant","block","condition","layout","opportunity_id","target_limb",
              "target_obstacle","planned_onset_s","planned_close_s","presented","valid",
              "invalid_reason","violation","entry_episodes","first_entry_s","min_clearance_m",
              "max_penetration_m","unattributed_contacts",
              "trigger_prox_s","trigger_pred_s","policy_lead_s","timing_inverted",
              "delivered_trigger_s","approach_at_onset_m","closing_speed_prox_ms",
              "closing_speed_pred_ms")

d <- read.csv(OPP, stringsAsFactors = FALSE, na.strings = c("NA", "", "NaN", "Infinity"))
miss <- setdiff(REQUIRED, names(d))
if (length(miss)) stop("opportunities.csv is missing columns: ", paste(miss, collapse = ", "))

cat(sprintf("Paper 1 confirmatory analysis\nFile: %s\nRows: %d   Participants: %d\n",
            OPP, nrow(d), length(unique(d$participant))))

# ---------------------------------------------------------------------------
# 1. FLOW AND EXCLUSIONS
# ---------------------------------------------------------------------------
flow <- data.frame(
  stage = c("scheduled opportunities",
            "  not presented",
            "  presented but window not completed / block aborted",
            "ANALYSED (presented and valid)",
            "  of which violations"),
  n = c(nrow(d),
        sum(d$presented == 0),
        sum(d$presented == 1 & d$valid == 0),
        sum(d$presented == 1 & d$valid == 1),
        sum(d$presented == 1 & d$valid == 1 & d$violation == 1)))
print(flow, row.names = FALSE)
write.csv(flow, "p1_flow.csv", row.names = FALSE)

a <- subset(d, presented == 1 & valid == 1)
if (nrow(a) < 100) stop("Only ", nrow(a), " analysable opportunities. Check the session.")

a$participant <- factor(a$participant)
a$layout      <- factor(a$layout)
a$condition   <- factor(a$condition)

# ---------------------------------------------------------------------------
# 2. FACTOR CODING — the 2x2 is four of the six conditions
# ---------------------------------------------------------------------------
# None and PBC are NOT cells of the factorial. None is the floor (H5 validity gate); PBC is
# the cue-form comparator for H4'. Including either in the 2x2 would be a category error —
# PBC is not a level of Policy or Mapping, it is PB with a different cue form.
cell <- subset(a, condition %in% c("RG", "RB", "PG", "PB"))
cell$Policy  <- factor(ifelse(cell$condition %in% c("PG", "PB"), "Predictive", "Proximity"),
                       levels = c("Proximity", "Predictive"))
cell$Mapping <- factor(ifelse(cell$condition %in% c("RB", "PB"), "Localized", "Generic"),
                       levels = c("Generic", "Localized"))

diag_lines <- character(0)
say <- function(...) { s <- sprintf(...); cat(s, "\n"); diag_lines <<- c(diag_lines, s) }

# ---------------------------------------------------------------------------
# 3. MANIPULATION CHECK FIRST — §15 gate 27, policy separation
# ---------------------------------------------------------------------------
# H1 assumes the predictive policy actually fires EARLIER than the proximity policy.
# `PolicyTriggerProbe` logs both counterfactually on every opportunity, so this is
# measured, not assumed. If it fails, H1 is uninterpretable and nothing below matters.
cat("\n--- MANIPULATION CHECK (gate 27) ---\n")
lead <- a$policy_lead_s[!is.na(a$policy_lead_s)]
if (length(lead)) {
  say("Predictive led proximity by a median of %+.0f ms (IQR %+.0f to %+.0f), over %d opportunities.",
      1000 * median(lead), 1000 * quantile(lead, .25), 1000 * quantile(lead, .75), length(lead))
  inv <- mean(a$timing_inverted == 1, na.rm = TRUE)
  say("Timing INVERTED on %.1f%% of opportunities (predictive fired later than proximity).", 100 * inv)
  if (inv > 0.05)
    say("  WARNING: above 5%%. The timing factor runs backwards on those opportunities and H1 averages")
  if (inv > 0.05)
    say("  over opposite manipulations. Report the rate and consider excluding them a priori.")
  if (median(lead) <= 0)
    say("  CRITICAL: the predictive policy did NOT lead on average. H1 cannot be interpreted.")
} else say("WARNING: no policy_lead_s values. The counterfactual probe did not run; gate 27 is unmet.")

# How much warning each policy actually delivered. Reviewers ask this about T = 1.0 s.
dl <- a$delivered_trigger_s[!is.na(a$delivered_trigger_s)]
if (length(dl)) {
  for (p in c("Proximity", "Predictive")) {
    v <- if (p == "Predictive") a$trigger_pred_s else a$trigger_prox_s
    v <- v[!is.na(v)]
    if (length(v)) say("%s policy fired a median of %.0f ms before contact (n=%d).",
                       p, 1000 * median(v), length(v))
  }
}

# ---------------------------------------------------------------------------
# 3B. PARAMETER SENSITIVITY — would other D and T have given the same manipulation?
# ---------------------------------------------------------------------------
# THE REVIEWER QUESTION THIS ANSWERS: "Where did D = 0.30 m and T = 1.0 s come from?
# You assumed them."
#
# We do not claim these values are optimal. They are representative operating points of
# two policy CLASSES — fire-on-distance vs fire-on-forecast — chosen so the contrast stays
# valid across the speeds this task produces [PAPER1_STUDY_DESIGN §6; the derivation table
# and the rejected alternatives are in docs/PAPER1_PARAMETER_JUSTIFICATION.md].
#
# This section shows the choice is not load-bearing: across a wide region of (D, T) the
# predictive policy still fires first, so the DIRECTION of the manipulation does not
# depend on the particular pair we froze.
#
# WHAT IT DOES NOT SHOW: the effect SIZE at other settings. Nobody experienced those, so
# their outcomes are not recoverable. Claiming otherwise is how this analysis backfires.
# Report it as manipulation robustness, never as evidence the effect generalises.
#
# THE ARITHMETIC. trigger_prox_s and trigger_pred_s are LEAD TIMES BEFORE CONTACT.
#   Proximity fires at distance D  -> its lead is D / v_prox.
#   Predictive fires when TTC = T  -> its lead is T plus oracle error, and that error is a
#                                     property of the estimator, not of T, so changing T
#                                     shifts the lead one-for-one.
# So for a counterfactual pair (D', T'):
#   lead_prox' = D' / closing_speed_prox_ms
#   lead_pred' = trigger_pred_s + (T' - T_FROZEN)
# Both are exact under locally constant closing speed. §3B-i MEASURES that assumption
# against the recorded triggers instead of asserting it.

D_FROZEN <- 0.30   # PAPER1_STUDY_DESIGN §6 — frozen 2026-09-14, before any data
T_FROZEN <- 1.00
D_GRID   <- c(0.20, 0.25, 0.30, 0.35, 0.40, 0.45)
T_GRID   <- c(0.70, 0.85, 1.00, 1.15, 1.30, 1.50)
INVERSION_TOLERANCE <- 0.05   # §3's threshold: above this, H1 averages over opposite manipulations

cat("\n--- 3B. PARAMETER SENSITIVITY (D x T) ---\n")
param_sweep <- NULL

sw <- subset(a, !is.na(trigger_prox_s) & !is.na(trigger_pred_s) &
               !is.na(closing_speed_prox_ms) & closing_speed_prox_ms > 0.05 &
               trigger_prox_s > 0)

if (nrow(sw) < 50) {
  say("3B: only %d opportunities carry both counterfactual triggers and a usable closing", nrow(sw))
  say("    speed. The sweep is not reportable — check PolicyTriggerProbe.")
} else {
  # 3B-i. VALIDATE THE RECONSTRUCTION BEFORE TRUSTING THE SWEEP.
  # If D_FROZEN / v_prox reproduces the recorded proximity lead, the model is sound at the
  # operating point and can be trusted a short way either side of it.
  recon <- D_FROZEN / sw$closing_speed_prox_ms
  resid <- recon - sw$trigger_prox_s
  rel   <- median(abs(resid) / pmax(sw$trigger_prox_s, 1e-3))
  say("3B-i reconstruction check: median |error| %.0f ms (%.1f%% of the proximity lead), n = %d.",
      1000 * median(abs(resid)), 100 * rel, nrow(sw))
  if (rel > 0.25) {
    say("  WARNING: above 25%%. Closing speed is not locally constant over the sampled")
    say("  distances, so the sweep is INDICATIVE ONLY. Say so if you report it.")
  } else {
    say("  Within tolerance — the constant-speed reconstruction holds at the operating point.")
  }

  grid <- expand.grid(D = D_GRID, T = T_GRID)
  grid$pct_predictive_leads <- NA_real_
  grid$median_lead_ms       <- NA_real_
  grid$pct_inverted         <- NA_real_
  for (i in seq_len(nrow(grid))) {
    gap <- (sw$trigger_pred_s + (grid$T[i] - T_FROZEN)) - (grid$D[i] / sw$closing_speed_prox_ms)
    grid$pct_predictive_leads[i] <- 100 * mean(gap > 0)
    grid$median_lead_ms[i]       <- 1000 * median(gap)
    grid$pct_inverted[i]         <- 100 * mean(gap <= 0)
  }
  param_sweep <- grid

  cat("\nInversion rate (%) — predictive fires LATER than proximity would have:\n")
  print(matrix(round(grid$pct_inverted, 1), nrow = length(D_GRID),
               dimnames = list(sprintf("D=%.2f", D_GRID), sprintf("T=%.2f", T_GRID))))
  cat("\nMedian lead the predictive policy buys (ms):\n")
  print(matrix(round(grid$median_lead_ms, 0), nrow = length(D_GRID),
               dimnames = list(sprintf("D=%.2f", D_GRID), sprintf("T=%.2f", T_GRID))))

  robust <- subset(grid, pct_inverted <= 100 * INVERSION_TOLERANCE)
  say("3B: the manipulation holds (inversion <= %.0f%%) at %d of %d (D,T) pairs tested.",
      100 * INVERSION_TOLERANCE, nrow(robust), nrow(grid))
  if (nrow(robust))
    say("    Robust region spans D %.2f-%.2f m and T %.2f-%.2f s — the frozen pair is not a knife edge.",
        min(robust$D), max(robust$D), min(robust$T), max(robust$T))
  else
    say("    NO pair clears the tolerance, including the frozen one. Read §3 before going further.")

  fr <- subset(grid, abs(D - D_FROZEN) < 1e-9 & abs(T - T_FROZEN) < 1e-9)
  if (nrow(fr) == 1)
    say("3B: at the FROZEN pair (D=%.2f, T=%.2f): predictive led on %.1f%% of opportunities, median %+.0f ms.",
        D_FROZEN, T_FROZEN, fr$pct_predictive_leads, fr$median_lead_ms)
}

# ---------------------------------------------------------------------------
# 4. PRIMARY MODEL — binomial, opportunity level [§9]
# ---------------------------------------------------------------------------
# Random slopes for both factors, per the design: (1 + Policy + Mapping | participant).
# Layout enters as its own random intercept - the same participant sees several layouts.
FORM_FULL <- violation ~ Policy * Mapping + (1 + Policy + Mapping | participant) + (1 | layout)
FORM_RI   <- violation ~ Policy * Mapping + (1 | participant) + (1 | layout)

fit_it <- function(f) suppressWarnings(suppressMessages(tryCatch(
  glmer(f, data = cell, family = binomial,
        control = glmerControl(optimizer = "bobyqa", calc.derivs = FALSE,
                               optCtrl = list(maxfun = 2e4))),
  error = function(e) NULL)))

fit <- fit_it(FORM_FULL)
model_used <- "Policy * Mapping + (1 + Policy + Mapping | participant) + (1 | layout)"
if (is.null(fit) || isSingular(fit, tol = 1e-5)) {
  alt <- fit_it(FORM_RI)
  if (!is.null(alt)) {
    fit <- alt
    model_used <- "random intercepts only (full random-slope model singular or non-convergent)"
    say("NOTE: fell back to random intercepts. Preregistered deviation - report it.")
  }
}
if (is.null(fit)) stop("Primary model did not converge. Do not proceed to interpretation.")

cat("\n--- PRIMARY MODEL (binomial, opportunity level) ---\n")
cat("Structure:", model_used, "\n\n")
co <- summary(fit)$coefficients
print(co, digits = 4)

# H1, H2, H3 read straight off the fixed effects.
lab <- rownames(co)
pick <- function(pattern) { i <- grep(pattern, lab); if (length(i)) i[1] else NA_integer_ }
iH1 <- pick("^PolicyPredictive$")
iH2 <- pick("^MappingLocalized$")
iH3 <- pick(":")

hyp <- data.frame(
  hypothesis = c("H1 policy main effect", "H2 mapping main effect", "H3 interaction"),
  term = lab[c(iH1, iH2, iH3)],
  log_odds = co[c(iH1, iH2, iH3), "Estimate"],
  se = co[c(iH1, iH2, iH3), "Std. Error"],
  z = co[c(iH1, iH2, iH3), "z value"],
  p = co[c(iH1, iH2, iH3), "Pr(>|z|)"])
hyp$odds_ratio <- exp(hyp$log_odds)
hyp$or_lo <- exp(hyp$log_odds - 1.96 * hyp$se)
hyp$or_hi <- exp(hyp$log_odds + 1.96 * hyp$se)

cat("\n--- H1 / H2 / H3 ---\n")
print(hyp[, c("hypothesis", "odds_ratio", "or_lo", "or_hi", "z", "p")], row.names = FALSE, digits = 4)

# ---------------------------------------------------------------------------
# 5. CONFIRMATORY FAMILY AND ERROR CONTROL [§3]
# ---------------------------------------------------------------------------
# CHANGED 2026-09-23 — H3 NO LONGER GATES H1 AND H2.
#
# The old rule was: if the interaction is supported, read H1 and H2 as simple effects.
# That rule is statistically orthodox and was wrong HERE, because the measured power for
# H3 at the planned design is 32% (power_analysis.R, nsim = 200; 80% needs N = 80 with 24
# opportunities, roughly 240 participant-hours). Letting a 32%-powered test govern how two
# well-powered tests are read means a coin flip decides the paper's interpretation, and a
# reviewer who checks the power table will say so.
#
# H3 is now EXPLORATORY: estimated, reported with its interval and its minimum detectable
# effect, and never used to reinterpret anything.
#
# CONFIRMATORY FAMILY = H1 (trigger policy), H2 (localization), H4' (cue form).
# Three tests, so family-wise error is controlled with Holm — uniformly more powerful than
# Bonferroni and requiring no independence assumption.

CONFIRMATORY <- c("H1 policy main effect", "H2 mapping main effect")   # H4' appended in §6

cat("\n--- CONFIRMATORY FAMILY (§3) ---\n")
cat("H1 (trigger policy), H2 (localization), H4' (cue form) are confirmatory, Holm-corrected.\n")
cat("H3 (interaction) is EXPLORATORY - 32% power at the planned design. It is reported\n")
cat("with its interval and never used to reinterpret H1 or H2.\n\n")

h3_p <- hyp$p[3]
cat("--- H3, EXPLORATORY ---\n")
cat(sprintf("Interaction OR = %.3f [%.3f, %.3f], p = %.4f (descriptive; no decision attaches).\n",
            hyp$odds_ratio[3], hyp$or_lo[3], hyp$or_hi[3], h3_p))
cellmeans <- aggregate(violation ~ Policy + Mapping, cell, mean)
cellmeans$violation <- round(cellmeans$violation, 3)
print(cellmeans, row.names = FALSE)
cat("The H3 contrast is the difference-in-differences (PB - PG) - (RB - RG).\n")
cat("'PB is lowest' is NOT evidence of an interaction, whatever the cell means look like.\n")
if (!is.na(h3_p) && h3_p < .05)
  cat("NOTE: H3 reached p < .05. It remains exploratory - it was not powered, and a\n",
      "significant result from a 32%-powered test is as likely to be noise as signal.\n", sep = "")

# ---------------------------------------------------------------------------
# 6. PLANNED CONTRASTS — the floor, and H4
# ---------------------------------------------------------------------------
contrast_vs <- function(ref, other, label) {
  s <- subset(a, condition %in% c(ref, other))
  if (length(unique(s$condition)) < 2 || nrow(s) < 50) {
    cat(sprintf("\n%s: not enough data.\n", label)); return(NULL)
  }
  s$cond <- relevel(factor(s$condition), ref = ref)
  m <- suppressWarnings(suppressMessages(tryCatch(
    glmer(violation ~ cond + (1 | participant) + (1 | layout), data = s, family = binomial,
          control = glmerControl(optimizer = "bobyqa", calc.derivs = FALSE)),
    error = function(e) NULL)))
  if (is.null(m)) { cat(sprintf("\n%s: model did not converge.\n", label)); return(NULL) }
  cc <- summary(m)$coefficients
  r <- grep("^cond", rownames(cc))[1]
  cat(sprintf("\n%s: OR = %.3f [%.3f, %.3f], p = %.4f\n", label,
              exp(cc[r, 1]), exp(cc[r, 1] - 1.96 * cc[r, 2]), exp(cc[r, 1] + 1.96 * cc[r, 2]), cc[r, 4]))
  data.frame(contrast = label, odds_ratio = exp(cc[r, 1]),
             or_lo = exp(cc[r, 1] - 1.96 * cc[r, 2]), or_hi = exp(cc[r, 1] + 1.96 * cc[r, 2]),
             p = cc[r, 4])
}

cat("\n--- PLANNED CONTRASTS ---")
c_floor <- contrast_vs("None", "PB", "Floor: PB vs None (does the full technique beat no feedback?)")

# H4' — THE CUE-FORM CONTRAST [docs/PAPER1_PARAMETER_JUSTIFICATION.md §5B].
# Valkov & Linsen (IEEE VR 2019) found forecast-triggered warning produced MORE collisions
# than distance-triggered warning, and attributed it to their CONTINUOUS intensity mapping
# rather than to the trigger: slowing down lowered the vibration, so participants crept
# forward modulating it. Their design varied trigger and cue form together.
#
# H1 and H4' are the two halves of that confound:
#   H1  = PB vs RB   -> trigger varies,  cue form constant
#   H4' = PB vs PBC  -> cue form varies, trigger constant
# PBC shares PB's trigger, site, and onset, enforced in ConditionManager and asserted in
# ContinuousCueTests. If H4' shows continuous mapping is worse, their null is explained.
c_h4    <- contrast_vs("PBC", "PB", "H4': PB vs PBC (cue form, trigger- and site-matched)")

# ---------------------------------------------------------------------------
# 6A. FAMILY-WISE ERROR CONTROL OVER THE CONFIRMATORY FAMILY [§3]
# ---------------------------------------------------------------------------
# Three confirmatory tests: H1, H2, H4'. Holm is uniformly more powerful than Bonferroni
# and assumes nothing about independence, which matters because H1 and H4' share the PB
# cell and are therefore correlated.
#
# The floor contrast (PB vs None) is NOT in the family. It is a validity gate (H5) — it
# admits the study to interpretation rather than testing a claim — and folding a gate into
# the family would spend alpha on a question whose answer is already a precondition.
# H3 is NOT in the family either: it is exploratory (§5).

cat("\n--- 6A. CONFIRMATORY FAMILY, HOLM-CORRECTED ---\n")
fam <- data.frame(
  hypothesis = c("H1 trigger policy (PB/PG vs RB/RG)",
                 "H2 localization (RB/PB vs RG/PG)",
                 "H4' cue form (PB vs PBC)"),
  odds_ratio = c(hyp$odds_ratio[1], hyp$odds_ratio[2],
                 if (!is.null(c_h4)) c_h4$odds_ratio else NA_real_),
  p_raw = c(hyp$p[1], hyp$p[2], if (!is.null(c_h4)) c_h4$p else NA_real_),
  stringsAsFactors = FALSE)
fam$p_holm <- p.adjust(fam$p_raw, method = "holm")
fam$supported <- ifelse(is.na(fam$p_holm), NA, fam$p_holm < .05)
print(fam, row.names = FALSE, digits = 4)

if (any(is.na(fam$p_raw)))
  say("6A: a confirmatory test is missing. Holm over an incomplete family is not valid - fix before reporting.")

say("6A: %d of 3 confirmatory hypotheses supported after Holm correction.",
    sum(fam$supported, na.rm = TRUE))

# ---------------------------------------------------------------------------
# 6B. REALIZED-LEAD DOSE-RESPONSE — the finding stated without the parameters
# ---------------------------------------------------------------------------
# THE REVIEWER QUESTION THIS ANSWERS: "Your result is an artefact of T = 1.0 s."
#
# Here the categorical Policy factor is REPLACED by the continuous lead time actually
# delivered (delivered_trigger_s — trigger_prox_s in RG/RB, trigger_pred_s in PG/PB).
# The conclusion then attaches to WARNING TIME rather than to the settings that produced
# it, and a reader can map it onto whatever lead times their own system achieves.
#
# Policy is deliberately ABSENT from this model: the delivered lead IS the policy's
# effect, so fitting both is collinear by construction.
#
# THREE LIMITS, stated because this analysis is weaker than it looks:
#  1. OBSERVATIONAL. Lead was not randomised within condition — it varies with limb speed
#     and opportunity geometry. This is an association, not a causal dose-response, and
#     it does not supersede the §4 model.
#  2. CONFOUNDED BY SPEED. For the proximity policy lead = D/v is a deterministic function
#     of closing speed, so lead and speed are near-collinear. The adjusted fit is reported
#     WITH that correlation, so the instability is visible instead of hidden.
#  3. NO EXTRAPOLATION. Predictions are reported only across the observed lead range —
#     the same discipline Paper 2 applies to its psychometric curve.

cat("\n--- 6B. REALIZED-LEAD DOSE-RESPONSE ---\n")
dose_fit <- NULL; dose_curve <- NULL

# PBC is deliberately EXCLUDED. Its delivered lead is PB's by construction (same trigger,
# same onset), but its cue form differs, so including it would mix two cue forms into one
# dose-response curve and the lead coefficient would absorb the H4' effect.
warned <- subset(a, !is.na(delivered_trigger_s) & condition %in% c("RG", "RB", "PG", "PB"))
if (nrow(warned) < 100 || length(unique(warned$delivered_trigger_s)) < 20) {
  say("6B: too few cued opportunities (%d) or too little lead variation for a dose-response.",
      nrow(warned))
} else {
  warned$Mapping <- factor(ifelse(warned$condition %in% c("RB", "PB"), "Localized", "Generic"),
                           levels = c("Generic", "Localized"))
  # The closing speed that belongs to the trigger that actually fired.
  warned$delivered_speed <- ifelse(warned$condition %in% c("PG", "PB"),
                                   warned$closing_speed_pred_ms, warned$closing_speed_prox_ms)
  lead_med <- median(warned$delivered_trigger_s)
  warned$lead_c <- warned$delivered_trigger_s - lead_med

  qs <- quantile(warned$delivered_trigger_s, c(0, .1, .25, .5, .75, .9, 1))
  say("6B: delivered lead spans %.0f-%.0f ms (median %.0f, IQR %.0f-%.0f) over %d opportunities.",
      1000 * qs[1], 1000 * qs[7], 1000 * qs[4], 1000 * qs[3], 1000 * qs[5], nrow(warned))

  dfit <- function(f) suppressWarnings(suppressMessages(tryCatch(
    glmer(f, data = warned, family = binomial,
          control = glmerControl(optimizer = "bobyqa", calc.derivs = FALSE,
                                 optCtrl = list(maxfun = 2e4))),
    error = function(e) NULL)))

  dose_fit <- dfit(violation ~ lead_c + Mapping + (1 + lead_c | participant) + (1 | layout))
  dose_struct <- "lead_c + Mapping + (1 + lead_c | participant) + (1 | layout)"
  if (is.null(dose_fit) || isSingular(dose_fit, tol = 1e-5)) {
    alt <- dfit(violation ~ lead_c + Mapping + (1 | participant) + (1 | layout))
    if (!is.null(alt)) {
      dose_fit <- alt; dose_struct <- "random intercepts only (slope model singular)"
    }
  }

  if (is.null(dose_fit)) {
    say("6B: the dose-response model did not converge. Report §4 alone.")
  } else {
    dc <- summary(dose_fit)$coefficients
    r  <- grep("^lead_c$", rownames(dc))[1]
    # Per +100 ms of warning, because milliseconds are the unit a reader designs in.
    or100 <- exp(dc[r, 1] * 0.1)
    lo100 <- exp((dc[r, 1] - 1.96 * dc[r, 2]) * 0.1)
    hi100 <- exp((dc[r, 1] + 1.96 * dc[r, 2]) * 0.1)
    cat("Structure:", dose_struct, "\n")
    say("6B: each extra 100 ms of delivered warning changes the violation odds by OR = %.3f [%.3f, %.3f], p = %.4f.",
        or100, lo100, hi100, dc[r, 4])

    # The curve a reader can port to their own system. Fixed effects only, at Mapping =
    # Generic (the reference cell); stated rather than silently averaged.
    b <- lme4::fixef(dose_fit)
    grid_lead <- seq(qs[2], qs[6], length.out = 9)          # 10th-90th percentile only
    dose_curve <- data.frame(
      lead_ms = round(1000 * grid_lead),
      p_violation_generic = round(plogis(b[["(Intercept)"]] +
                                         b[["lead_c"]] * (grid_lead - lead_med)), 4))
    cat("\nPredicted violation probability vs delivered lead (Mapping = Generic,\n")
    cat("fixed effects only, 10th-90th percentile of observed lead — do NOT extrapolate):\n")
    print(dose_curve, row.names = FALSE)

    # LIMIT 2 made visible rather than hidden.
    ok_sp <- !is.na(warned$delivered_speed) & warned$delivered_speed > 0.05
    if (sum(ok_sp) > 100) {
      rr <- cor(warned$delivered_trigger_s[ok_sp], warned$delivered_speed[ok_sp])
      say("6B: cor(delivered lead, closing speed) = %+.2f.", rr)
      if (abs(rr) > 0.6) {
        say("  Strongly collinear, as the geometry predicts. The speed-adjusted estimate below")
        say("  is therefore unstable BY CONSTRUCTION — report it as a caveat, not a correction.")
      } else {
        say("  Moderate. The speed-adjusted estimate below is informative; still not causal.")
      }
      adj <- suppressWarnings(suppressMessages(tryCatch(
        glmer(violation ~ lead_c + Mapping + scale(delivered_speed) + (1 | participant) + (1 | layout),
              data = warned[ok_sp, ], family = binomial,
              control = glmerControl(optimizer = "bobyqa", calc.derivs = FALSE)),
        error = function(e) NULL)))
      if (!is.null(adj)) {
        ac <- summary(adj)$coefficients
        ar <- grep("^lead_c$", rownames(ac))[1]
        say("6B: speed-adjusted, per +100 ms: OR = %.3f, p = %.4f (vs %.3f unadjusted).",
            exp(ac[ar, 1] * 0.1), ac[ar, 4], or100)
      }
    }
  }
}

# ---------------------------------------------------------------------------
# 7. ALERT BURDEN — the cost side of a policy [§15 Step 3]
# ---------------------------------------------------------------------------
# A predictive policy fires earlier and more often. That is a real cost, not a hidden
# one, and it belongs beside any H1 benefit.
if ("delivered_trigger_s" %in% names(a)) {
  burden <- aggregate(cbind(fired = !is.na(delivered_trigger_s)) ~ condition, a, mean)
  burden$fired <- round(100 * burden$fired, 1)
  names(burden)[2] <- "pct_opportunities_cued"
  cat("\n--- ALERT BURDEN ---\n"); print(burden, row.names = FALSE)
}

# ---------------------------------------------------------------------------
# 8. SENSITIVITY — the old block-level model, explicitly demoted
# ---------------------------------------------------------------------------
if (!is.na(BLK) && file.exists(BLK)) {
  cat("\n--- SENSITIVITY: block-level counts ---\n")
  cat("Run `Rscript analysis.R", BLK, "` for the negative-binomial GLMM.\n")
  cat("It is a SENSITIVITY analysis. If it disagrees with the model above, the\n")
  cat("opportunity-level result stands: it has the correct unit and denominator.\n")
} else {
  cat("\n(No block summary supplied; skipping the sensitivity pointer.)\n")
}

# ---------------------------------------------------------------------------
# 9. OUTPUTS
# ---------------------------------------------------------------------------
est <- hyp
if (!is.null(c_floor)) est <- merge(est, c_floor, all = TRUE)
if (!is.null(c_h4))    est <- merge(est, c_h4, all = TRUE)
est$model <- model_used
est$n_opportunities <- nrow(cell)
est$n_participants <- length(unique(cell$participant))
write.csv(est, "p1_estimates.csv", row.names = FALSE)
writeLines(diag_lines, "p1_diagnostics.txt")

write.csv(fam, "p1_confirmatory_family.csv", row.names = FALSE)
written <- c("p1_estimates.csv", "p1_flow.csv", "p1_diagnostics.txt", "p1_confirmatory_family.csv")
if (!is.null(param_sweep)) {
  write.csv(param_sweep, "p1_param_sensitivity.csv", row.names = FALSE)
  written <- c(written, "p1_param_sensitivity.csv")
}
if (!is.null(dose_curve)) {
  write.csv(dose_curve, "p1_lead_dose_response.csv", row.names = FALSE)
  written <- c(written, "p1_lead_dose_response.csv")
}

cat("\nWrote", paste(written, collapse = ", "), "\n")
cat("\n--------------------------------------------------------------------------\n")
cat("BEFORE REPORTING:\n")
cat(" 1. Read the manipulation check first. If the timing factor inverted on more than\n")
cat("    a few percent of opportunities, H1 is averaging over opposite manipulations.\n")
cat(" 2. H3 is EXPLORATORY (32% power). Report its interval, never let it reinterpret H1\n")
cat("    or H2, and do not describe a significant H3 as a finding.\n")
cat(" 3. Report alert burden beside any H1 benefit. Earlier warnings are not free. For\n")
cat("    PBC use engagement EPISODES and intensity-seconds, never the raw command count.\n")
cat(" 4. H4' is the cue-form contrast answering Valkov & Linsen (IEEE VR 2019). State in\n")
cat("    the Discussion that PB and PBC share trigger, site and onset - that matching is\n")
cat("    what separates cue form from timing, and it is the paper's core claim to novelty.\n")
cat(" 5. Any fallback to random intercepts is a preregistered deviation. Report it.\n")
cat(" 6. §3B is MANIPULATION robustness, not effect-size generality. It shows the timing\n")
cat("    contrast survives other D and T; it says nothing about effect size at settings\n")
cat("    nobody experienced. Overstating it hands a reviewer the attack it was built to stop.\n")
cat(" 7. §6B is OBSERVATIONAL and speed-confounded by construction. Report it as the\n")
cat("    parameter-free restatement of §4, never as a substitute for it, and never predict\n")
cat("    outside the observed lead range.\n")
cat("--------------------------------------------------------------------------\n")
