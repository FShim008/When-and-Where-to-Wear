# =============================================================================
# PAPER 2 (E2) - CONFIRMATORY ANALYSIS
#   "Stopping a Reach in VR"   [PAPER2_STUDY_DESIGN §8, §9, §10, §13, §16]
#
# Run:  Rscript e2_analysis.R <path/to/e2_trials.csv> [nsim_boot]
#       Rscript e2_analysis.R sessions/E2_P901/e2_trials.csv 200
#
# Writes: e2_estimates.csv, e2_flow.csv, e2_diagnostics.txt, e2_curve.png
#
# THIS IS PREREGISTRATION MATERIAL. Fix it before data collection. Everything that could be decided
# after seeing results - covariates, exclusions, the multiplicity family - is decided here instead.
#
# -----------------------------------------------------------------------------
# WHAT IS CONFIRMATORY AND WHAT IS NOT
# -----------------------------------------------------------------------------
# Confirmatory family (§10): H1 (lead-time curve, an ESTIMATION target) and H3a (tempo, directional).
# Holm-corrected across those two only. H3b and everything in section 5 below are secondary and
# descriptive; they do not enter the correction and must not be reported as if they did.
#
# §10 is explicit that H3a is NOT gated on H1 "passing" - H1 will pass; that is not informative.
# =============================================================================

suppressMessages(library(lme4))

args      <- commandArgs(trailingOnly = TRUE)
CSV_PATH  <- if (length(args) >= 1) args[1] else "e2_trials.csv"
NSIM_BOOT <- if (length(args) >= 2) as.integer(args[2]) else 200L
set.seed(20260914)

if (!file.exists(CSV_PATH)) stop("No such file: ", CSV_PATH)

# ---------------------------------------------------------------------------
# 0. THE CSV CONTRACT
# ---------------------------------------------------------------------------
# These are the 26 columns emitted by E2TrialOutcomeFormatter.HeaderLine (Core/E2/E2TrialOutcome.cs).
# Checked exactly, because a silently renamed column would otherwise surface as a puzzling model error
# several hundred lines later.
REQUIRED <- c("participant","session","trial_id","target_limb","hazard","tempo","warning_trial","valid",
              "invalid_reason","crossed","assigned_lead_s","realized_lead_s","timing_deviation",
              "command_time_s","physical_onset_s","precue_speed_ms","distance_at_cue_m",
              "predicted_contact_s","actual_contact_s","prediction_error_s","min_clearance_m",
              "max_penetration_m","post_cue_travel_m","response_onset_s","arrest_s","reached_target",
              # Tempo manipulation check, added 2026-09-14 with the response-window implementation.
              "movement_onset_s","target_contact_s","movement_time_s","response_window_s",
              "response_window_min_s","within_window")

d <- read.csv(CSV_PATH, stringsAsFactors = FALSE, na.strings = c("NA", "", "NaN", "Infinity"))
missing_cols <- setdiff(REQUIRED, names(d))
extra_cols   <- setdiff(names(d), REQUIRED)
if (length(missing_cols)) stop("CSV is missing required columns: ", paste(missing_cols, collapse = ", "))
if (length(extra_cols))
  cat("NOTE: unexpected extra columns (ignored):", paste(extra_cols, collapse = ", "), "\n")

cat(sprintf("E2 confirmatory analysis\nFile: %s\nRows: %d   Participants: %d\n\n",
            CSV_PATH, nrow(d), length(unique(d$participant))))

# ---------------------------------------------------------------------------
# 1. EXCLUSIONS AND THE FLOW DIAGRAM  [§13]
# ---------------------------------------------------------------------------
# §13: "Never keep a planned denominator after an unpresented or invalid trial. Preserve all raw rows
# with status/reason fields." So nothing is deleted here - rows are CLASSIFIED, and every count is
# reported so the flow diagram can be drawn from this table rather than reconstructed by hand.
#
# ! timing_deviation is NOT an exclusion. §8 is explicit: the psychometric fit uses the REALIZED lead,
# so a mistimed trial is still a legitimate point on the curve. It is a protocol-deviation counter and
# an input to the intention-to-deliver sensitivity analysis in section 6, nothing more.
d$avoid <- 1L - d$crossed          # the modelled outcome: did the participant avoid the hazard

flow <- data.frame(
  stage = c("rows in file",
            "invalid (any reason)",
            "  no_movement_onset",
            "  cue_never_eligible",
            "  hazard_id_not_in_scene",
            "valid trials",
            "  valid go trials (zero-warning anchor)",
            "  valid warning trials (the curve)",
            "  warning trials with usable realized lead",
            "flagged timing_deviation (KEPT, not excluded)"),
  n = c(nrow(d),
        sum(d$valid == 0),
        sum(d$invalid_reason %in% "no_movement_onset"),
        sum(d$invalid_reason %in% "cue_never_eligible"),
        sum(d$invalid_reason %in% "hazard_id_not_in_scene"),
        sum(d$valid == 1),
        sum(d$valid == 1 & d$warning_trial == 0),
        sum(d$valid == 1 & d$warning_trial == 1),
        sum(d$valid == 1 & d$warning_trial == 1 & !is.na(d$realized_lead_s)),
        sum(d$timing_deviation == 1, na.rm = TRUE)))
print(flow, row.names = FALSE)
write.csv(flow, "e2_flow.csv", row.names = FALSE)

warn <- subset(d, valid == 1 & warning_trial == 1 & !is.na(realized_lead_s))
go   <- subset(d, valid == 1 & warning_trial == 0)

if (nrow(warn) < 50) stop("Only ", nrow(warn), " usable warning trials. Too few to fit. Check the pilot.")
warn$pid    <- factor(warn$participant)
warn$urgent <- as.integer(warn$tempo == "Urgent")

# ---------------------------------------------------------------------------
# 2. DIAGNOSTICS THAT RUN BEFORE THE FIT  [§8]
# ---------------------------------------------------------------------------
# These are deliberately first. Each one can invalidate the curve above it, and finding that out after
# reporting an estimate is much worse than finding it out before.
diag_lines <- character(0)
say <- function(...) { s <- sprintf(...); cat(s, "\n"); diag_lines <<- c(diag_lines, s) }

cat("\n--- DIAGNOSTICS (section 8) ---\n")

# 2a. Did the five assigned levels actually separate after delivery?
# If they collapse, the design has less leverage on the curve than planned and the interval widens.
say("Achieved lead by assigned level (s):")
lev <- sort(unique(warn$assigned_lead_s))
for (L in lev) {
  v <- warn$realized_lead_s[warn$assigned_lead_s == L]
  say("  assigned %.3f -> realized median %.3f  IQR [%.3f, %.3f]  n=%d  (shortfall %+.0f ms)",
      L, median(v), quantile(v, .25), quantile(v, .75), length(v), 1000 * (median(v) - L))
}
if (length(lev) >= 2) {
  meds <- sapply(lev, function(L) median(warn$realized_lead_s[warn$assigned_lead_s == L]))
  say("Assigned spread %.3f s -> achieved spread %.3f s (%.0f%% of planned).",
      max(lev) - min(lev), max(meds) - min(meds), 100 * (max(meds) - min(meds)) / (max(lev) - min(lev)))
  if (is.unsorted(meds))
    say("  WARNING: achieved medians are NOT monotone in the assigned level. Delivery is broken.")
}

# 2b. The zero-warning anchor [§8]. If people avoid the hazard with no warning at all, the geometry is
# not producing genuine near-approaches and every avoidance rate above it is compressed toward ceiling.
if (nrow(go)) {
  gr <- mean(go$crossed)
  say("Go-trial crossing rate (zero-warning anchor): %.1f%% over %d trials.", 100 * gr, nrow(go))
  if (gr < 0.50) {
    say("  WARNING: below 50%%. Hazards are being avoided without any warning, so the curve floor is")
    say("  not near zero and LT50/LT80 are NOT interpretable as plain avoidance thresholds.")
  }
} else say("WARNING: no valid go trials - the curve has no zero-warning anchor.")

# 2c. Counterfactual validation [§8]. How good was the constant-velocity contact prediction? This is
# the x-axis's largest assumption. Positive = the prediction was late.
pe <- d$prediction_error_s[!is.na(d$prediction_error_s)]
if (length(pe)) {
  say("Prediction error (predicted - actual contact), n=%d: median %+.0f ms, IQR [%+.0f, %+.0f], |P90| %.0f ms.",
      length(pe), 1000 * median(pe), 1000 * quantile(pe, .25), 1000 * quantile(pe, .75),
      1000 * quantile(abs(pe), .90))
  say("  Compare against the smallest lead level (%.0f ms). Error of that order means the x-axis is",
      1000 * min(lev))
  say("  not resolving the short end of the curve, and LT50 is the estimate most affected.")
} else say("WARNING: no prediction_error_s values - the counterfactual is unvalidated (section 8 requires it).")

# 2d. Delivery precision.
say("Timing deviations: %.1f%% of warning trials (%d/%d).",
    100 * mean(warn$timing_deviation == 1), sum(warn$timing_deviation == 1), nrow(warn))

# 2e. THE TEMPO MANIPULATION CHECK  [§8; §15 gate 7]
# "Confirm the manipulation with pre-cue hand velocity and movement time." Gate 7 is "tempo changes pre-cue
# speed WITHOUT unacceptable instruction failure" - two separate questions, and both are asked here.
#
# Movement time is measured on GO trials: on warning trials a successful stop never reaches the target, so
# conditioning movement time on warning trials would select for failures.
gonorm <- subset(go, tempo == "Normal" & !is.na(movement_time_s))
gourg  <- subset(go, tempo == "Urgent" & !is.na(movement_time_s))
if (nrow(gonorm) >= 10 && nrow(gourg) >= 10) {
  say("Tempo manipulation (go trials): movement time normal %.0f ms vs urgent %.0f ms (difference %.0f ms).",
      1000 * median(gonorm$movement_time_s), 1000 * median(gourg$movement_time_s),
      1000 * (median(gonorm$movement_time_s) - median(gourg$movement_time_s)))
  if (median(gourg$movement_time_s) >= median(gonorm$movement_time_s))
    say("  WARNING: urgent reaches were NOT faster. The tempo manipulation failed and H3a is uninterpretable.")

  wn <- subset(warn, tempo == "Normal"); wu <- subset(warn, tempo == "Urgent")
  if (nrow(wn) >= 10 && nrow(wu) >= 10)
    say("Pre-cue closing speed (warning trials): normal %.2f m/s vs urgent %.2f m/s.",
        median(wn$precue_speed_ms, na.rm = TRUE), median(wu$precue_speed_ms, na.rm = TRUE))
} else say("WARNING: too few go trials with a movement time to check the tempo manipulation.")

# The other half of gate 7: instruction failure. A window that is missed most of the time is not a tempo
# manipulation, it is a source of non-random trial loss.
if (nrow(go)) {
  for (tp in c("Normal", "Urgent")) {
    sub <- subset(go, tempo == tp & reached_target == 1)
    if (nrow(sub) < 10) next
    say("Window compliance, %s go trials: %.0f%% inside the %.0f ms window (n=%d).",
        tp, 100 * mean(sub$within_window == 1), 1000 * median(sub$response_window_s), nrow(sub))
    if (mean(sub$within_window == 1) < 0.60)
      say("  WARNING: below 60%%. That is instruction failure, not a tempo manipulation (§15 gate 7).")
  }
}

# ---------------------------------------------------------------------------
# 3. PRIMARY MODEL  [§10]
# ---------------------------------------------------------------------------
# §10's parameterisation is threshold-form: logit(p) = slope_i * (lead - threshold_ij), with threshold
# carrying tempo and pre-cue speed. Fitted here in the algebraically equivalent linear-predictor form
# (b0 + b1*lead + ...), because glmer fits that directly; the thresholds are recovered in section 4.
#
# Random slope on lead is specified. If it produces a singular fit, the fallback below fires and SAYS
# SO - §10 requires the convergence/singular-fit procedure to be fixed in advance, so it is fixed here.
#
# TWO SPECIFICATION DECISIONS, BOTH FORCED BY A DRY RUN AGAINST SIMULATED GROUND TRUTH
# (e2_simulate_dataset.R, truth LT50 250 ms / LT80 329 ms / tempo +50 ms):
#
# (1) A LAPSE TERM IS REQUIRED. §10 lists "lapse/trigger-failure component" as a model extension to fix
#     in advance. Omitting it is not neutral: on data generated with a 3% lapse, the plain logistic
#     returned LT50 270 ms (+20) and LT80 344 ms (+15). An unmodelled ceiling below 1.0 drags the
#     fitted curve rightward, and LT80 - the reported operating point - is exactly what it inflates.
#     Fitted here with a fixed-lapse link on a preregistered grid, selected by AIC. On the dry run AIC
#     recovered the true lambda (0.03) and the LT80 bias fell to +4.5 ms.
#
# (2) PRE-CUE SPEED IS A MEDIATOR OF TEMPO, NOT A COVARIATE FOR IT. The tempo manipulation WORKS BY
#     making people move faster: in the dry run cor(urgent, pre_cue_speed) = 0.80. Controlling for
#     speed therefore removes the very path H3a is about and biases the tempo effect toward zero -
#     recovered 37 ms against a true 50 ms. Dropping it recovered 46 ms.
#
#     So the confirmatory H3a model does NOT adjust for speed; the speed-adjusted fit is reported
#     separately in section 6 as a mechanism decomposition (the direct effect not carried by speed).
#     This is a departure from a literal reading of §10, which lists speed among the threshold
#     predictors. It is deliberate: §10's list is about reducing nuisance variance in the THRESHOLD,
#     and that rationale does not survive the variable turning out to be the manipulation's own
#     mechanism. Recorded here so the change is visible rather than silent.
warn$speed_c <- as.numeric(scale(warn$precue_speed_ms, scale = FALSE))   # centred: keeps b0 interpretable

# p = (1 - lambda) * plogis(eta): a psychometric function with an upper asymptote below 1.
lapse_logit <- function(lambda) structure(list(
  linkfun  = function(mu)  qlogis(pmin(pmax(mu / (1 - lambda), 1e-10), 1 - 1e-10)),
  linkinv  = function(eta) (1 - lambda) * plogis(eta),
  mu.eta   = function(eta) (1 - lambda) * dlogis(eta),
  valideta = function(eta) TRUE,
  name     = sprintf("lapse_logit(%.3f)", lambda)), class = "link-glm")

LAPSE_GRID <- c(0, 0.01, 0.02, 0.03, 0.05, 0.08)   # preregistered; do not widen after seeing data

FORM_FULL <- avoid ~ realized_lead_s + urgent + (1 + realized_lead_s | pid)
FORM_RI   <- avoid ~ realized_lead_s + urgent + (1 | pid)

fit_glmer <- function(form, lambda = 0) suppressWarnings(suppressMessages(tryCatch(
  glmer(form, data = warn, family = binomial(link = lapse_logit(lambda)),
        control = glmerControl(optimizer = "bobyqa", calc.derivs = FALSE,
                               optCtrl = list(maxfun = 2e4))),
  error = function(e) NULL)))

# Select lambda by AIC over the preregistered grid, on the full random-slope form.
lapse_aic <- sapply(LAPSE_GRID, function(l) { f <- fit_glmer(FORM_FULL, l); if (is.null(f)) NA_real_ else AIC(f) })
LAMBDA <- if (all(is.na(lapse_aic))) 0 else LAPSE_GRID[which.min(lapse_aic)]
cat("\n--- LAPSE SELECTION (§10) ---\n")
print(data.frame(lambda = LAPSE_GRID, AIC = lapse_aic), row.names = FALSE, digits = 6)
cat(sprintf("Selected lapse = %.3f by AIC.\n", LAMBDA))
if (LAMBDA == max(LAPSE_GRID))
  cat("  WARNING: lambda selected at the grid edge. The ceiling may be lower than the grid allows;\n  report this and do not silently extend the grid.\n")

fit <- fit_glmer(FORM_FULL, LAMBDA)
model_used <- "random intercept + random slope on lead"
if (is.null(fit) || isSingular(fit, tol = 1e-5)) {
  altfit <- fit_glmer(FORM_RI, LAMBDA)
  if (!is.null(altfit)) {
    fit <- altfit
    model_used <- "random intercept ONLY (random-slope model was singular or failed to converge)"
    say("NOTE: random-slope model singular/non-convergent; fell back to random intercept (a deviation).")
  }
}
if (is.null(fit)) stop("Primary model did not converge in either form. Do not proceed to interpretation.")
cat("\n--- PRIMARY MODEL ---\nStructure:", model_used, "\n\n")
print(summary(fit)$coefficients, digits = 4)

# ---------------------------------------------------------------------------
# 4. LT50 / LT80 - DELTA METHOD *AND* PARAMETRIC BOOTSTRAP  [§10, §16.2]
# ---------------------------------------------------------------------------
# §16.2: the engineering figure is the INTERVAL'S UPPER BOUND, not the point estimate. So the interval
# is the deliverable and it had better be the right one.
#
# The delta method linearises; e2_power_analysis.R uses it only because a power simulation needs
# thousands of fits. Here we can afford the parametric bootstrap, and BOTH are reported so the gap is
# visible rather than assumed. If they disagree materially, the bootstrap is the one to quote.
# WHICH "80%" IS LT80? Two conventions exist and they differ by the lapse rate:
#   (a) the lead at which the ABSOLUTE avoidance probability reaches 0.80;
#   (b) the lead at which the lapse-free latent function reaches 0.80.
# This script uses (a), because §16.2's deliverable is an engineering operating point and a user with
# a 3% lapse rate genuinely cannot exceed 97% - a figure they cannot achieve is not an operating
# point. Under (a) the target on the latent scale is qlogis(p / (1 - lambda)).
# With lambda = 0 the two coincide, so the choice only bites once a lapse is selected.
lt_target <- function(target_p) {
  if (1 - LAMBDA <= target_p)
    stop(sprintf("Lapse %.3f puts the ceiling (%.3f) at or below the %.0f%% target: LT%.0f does not exist. ",
                 LAMBDA, 1 - LAMBDA, 100 * target_p, 100 * target_p),
         "That is a reportable result, not an error to work around - the population cannot reach this rate.")
  qlogis(target_p / (1 - LAMBDA))
}

lt_point <- function(f, target_p) {
  b <- lme4::fixef(f)
  (lt_target(target_p) - unname(b["(Intercept)"])) / unname(b["realized_lead_s"])
}

lt_delta <- function(f, target_p) {
  b  <- lme4::fixef(f); V <- as.matrix(vcov(f))
  b0 <- unname(b["(Intercept)"]); b1 <- unname(b["realized_lead_s"])
  if (is.na(b1) || b1 <= 0) return(c(NA, NA, NA))
  k <- lt_target(target_p); e <- (k - b0) / b1
  g <- c(-1 / b1, -(k - b0) / (b1^2))
  idx <- match(c("(Intercept)", "realized_lead_s"), colnames(V))
  se <- sqrt(as.numeric(t(g) %*% V[idx, idx] %*% g))
  c(e, e - 1.96 * se, e + 1.96 * se)
}

cat(sprintf("\nParametric bootstrap: %d resamples (paper should use >= 2000)...\n", NSIM_BOOT))
boot_stat <- function(f) c(LT50 = lt_point(f, 0.50), LT80 = lt_point(f, 0.80))
bt <- suppressWarnings(suppressMessages(tryCatch(
  bootMer(fit, boot_stat, nsim = NSIM_BOOT, type = "parametric", use.u = FALSE),
  error = function(e) NULL)))

lt_boot <- function(b, j) {
  if (is.null(b)) return(c(NA, NA, NA))
  v <- b$t[, j]; v <- v[is.finite(v)]
  if (length(v) < 20) return(c(NA, NA, NA))
  unname(c(median(v), quantile(v, .025), quantile(v, .975)))
}

est <- data.frame(
  quantity    = c("LT50", "LT80"),
  point_ms    = 1000 * c(lt_point(fit, .50), lt_point(fit, .80)),
  delta_lo_ms = 1000 * c(lt_delta(fit, .50)[2], lt_delta(fit, .80)[2]),
  delta_hi_ms = 1000 * c(lt_delta(fit, .50)[3], lt_delta(fit, .80)[3]),
  boot_lo_ms  = 1000 * c(lt_boot(bt, 1)[2], lt_boot(bt, 2)[2]),
  boot_hi_ms  = 1000 * c(lt_boot(bt, 1)[3], lt_boot(bt, 2)[3]))
est$delta_width_ms <- est$delta_hi_ms - est$delta_lo_ms
est$boot_width_ms  <- est$boot_hi_ms  - est$boot_lo_ms
est$boot_vs_delta  <- est$boot_width_ms / est$delta_width_ms

cat("\n--- H1: LEAD-TIME CURVE (sections 9, 16.2) ---\n")
print(est, row.names = FALSE, digits = 4)
cat(sprintf("\n-> OPERATING POINT per section 16.2 (upper bound of the LT80 interval): %.0f ms\n",
            max(c(est$boot_hi_ms[2], est$delta_hi_ms[2]), na.rm = TRUE)))
if (is.finite(est$boot_vs_delta[2]))
  cat(sprintf("-> Bootstrap interval is %.2fx the delta-method width. e2_power_analysis.R uses the delta\n   method, so multiply its widths by about this for the honest planning figure.\n",
              est$boot_vs_delta[2]))

# ---------------------------------------------------------------------------
# 5. SECONDARY ESTIMANDS  [§10]
# ---------------------------------------------------------------------------
# 5a. Between-participant threshold SD, in SECONDS.
# ! NOT the random-intercept SD. glmer's intercept SD is on the LOGIT scale (roughly SD_threshold x
# slope); reading it as milliseconds overstates the answer by ~20x. Converted through the slope by the
# delta method, validated against known truth at 30/60/120/200 ms -> recovered 27/60/112/187 ms.
sd_threshold_from_fit <- function(f) {
  b  <- lme4::fixef(f)
  b0 <- unname(b["(Intercept)"]); b1 <- unname(b["realized_lead_s"])
  if (is.na(b1) || b1 <= 0) return(NA_real_)
  S <- lme4::VarCorr(f)$pid
  if (is.null(S)) return(NA_real_)
  if (!all(c("(Intercept)", "realized_lead_s") %in% rownames(S)))
    return(sqrt(S["(Intercept)", "(Intercept)"]) / b1)     # random-intercept-only fallback
  thr <- -b0 / b1
  v <- (S["(Intercept)", "(Intercept)"] + thr^2 * S["realized_lead_s", "realized_lead_s"] +
        2 * thr * S["(Intercept)", "realized_lead_s"]) / b1^2
  if (is.na(v) || v < 0) return(NA_real_)
  sqrt(v)
}
sd_thr <- sd_threshold_from_fit(fit)
cat("\n--- SECONDARY (section 10) ---\n")
cat(sprintf("Between-participant threshold SD: %.0f ms\n", 1000 * sd_thr))
cat(sprintf("  -> a single population setting over/under-serves a typical user by about %.0f ms.\n",
            1000 * sd_thr))
cat("  -> partial pooling makes this a mild UNDER-estimate of heterogeneity, which argues for a wider\n")
cat("     operating margin, not a narrower one.\n")

# 5b. Threshold split-half reliability [§10]. The precondition for the whole personalization programme:
# if a threshold is not stable within one session, no predictor can ever predict it.
half_threshold <- function(sub) {
  if (nrow(sub) < 20 || length(unique(sub$avoid)) < 2) return(NA_real_)
  m <- suppressWarnings(glm(avoid ~ realized_lead_s, data = sub, family = binomial))
  b <- coef(m)
  if (is.na(b[2]) || b[2] <= 0) return(NA_real_)
  unname(-b[1] / b[2])
}
sh <- do.call(rbind, lapply(split(warn, warn$pid), function(sub) {
  sub <- sub[order(sub$trial_id), ]
  odd <- seq_len(nrow(sub)) %% 2 == 1
  h   <- floor(nrow(sub) / 2)
  data.frame(a      = half_threshold(sub[odd, ]),
             b      = half_threshold(sub[!odd, ]),
             first  = half_threshold(sub[seq_len(h), ]),
             second = half_threshold(sub[(h + 1):nrow(sub), ]))
}))
ok <- complete.cases(sh[, c("a", "b")])
if (sum(ok) >= 4) {
  r  <- cor(sh$a[ok], sh$b[ok], method = "spearman")
  sb <- 2 * r / (1 + r)                                   # Spearman-Brown
  cat(sprintf("Threshold split-half (odd/even): rho = %.2f, Spearman-Brown corrected = %.2f (n=%d)\n",
              r, sb, sum(ok)))
  if (!is.na(sb) && sb < 0.5)
    cat("  WARNING: below 0.5. Thresholds are not stable within a session, and section 10 says the\n  deferred tSSRT study should NOT be run on this evidence.\n")
  ok2 <- complete.cases(sh[, c("first", "second")])
  if (sum(ok2) >= 4) {
    dd <- sh$second[ok2] - sh$first[ok2]
    cat(sprintf("First vs second half: median shift %+.0f ms (Wilcoxon p = %.3f)\n",
                1000 * median(dd), suppressWarnings(wilcox.test(dd)$p.value)))
    cat("  -> a systematic shift is learning or fatigue (section 10) and must enter as a trial-order term.\n")
  }
} else cat("Split-half reliability: too few participants with estimable halves.\n")

# ---------------------------------------------------------------------------
# 6. H3a - TEMPO  [§9]
# ---------------------------------------------------------------------------
# Urgent reaches are predicted to need MORE warning -> a higher threshold -> at a fixed lead, LOWER
# avoidance -> a NEGATIVE coefficient on `urgent`. One-sided, alpha = .05.
co    <- summary(fit)$coefficients
z     <- co["urgent", "z value"]
p_h3a <- pnorm(z)                                  # one-sided, lower tail
b1    <- unname(lme4::fixef(fit)["realized_lead_s"])
shift_ms <- 1000 * (-unname(lme4::fixef(fit)["urgent"]) / b1)   # threshold shift, ms
cat("\n--- H3a: TEMPO (section 9) ---\n")
cat(sprintf("urgent coefficient: %+.3f logits (z = %.2f), one-sided p = %.4f\n",
            co["urgent", "Estimate"], z, p_h3a))
cat(sprintf("  -> urgent reaches need %.0f ms %s warning than normal.\n",
            abs(shift_ms), ifelse(shift_ms > 0, "MORE", "LESS")))

# Confirmatory family = {H1, H3a}, Holm. H1 is an estimation target with no meaningful null, so it
# enters as its interval, not a p-value; with one testable member Holm reduces to alpha = .05 on H3a.
# Stated explicitly so the preregistration cannot be read as hiding a second test.
cat(sprintf("Confirmatory family (section 10) = {H1 [estimation], H3a [test]}. Holm over the testable\n  members reduces to alpha = .05 here. H3a %s.\n",
            ifelse(p_h3a < .05, "is SUPPORTED", "is NOT supported")))

# MECHANISM DECOMPOSITION - SECONDARY, NOT CONFIRMATORY.
# Adding pre-cue speed asks a different question: how much of the tempo effect is NOT carried by
# speed. Because speed is how the tempo manipulation acts (dry run: r = 0.80), this is a direct
# effect with a mediator held constant, and it is necessarily SMALLER than the total effect above.
# Reporting it as "the" tempo effect would understate H3a; reporting the difference is informative.
med <- suppressWarnings(suppressMessages(tryCatch(
  glmer(avoid ~ realized_lead_s + urgent + speed_c + (1 | pid), data = warn,
        family = binomial(link = lapse_logit(LAMBDA)),
        control = glmerControl(optimizer = "bobyqa", calc.derivs = FALSE)),
  error = function(e) NULL)))
if (!is.null(med)) {
  b <- lme4::fixef(med)
  direct_ms <- 1000 * (-unname(b["urgent"]) / unname(b["realized_lead_s"]))
  cat(sprintf("\nMechanism (secondary): tempo effect adjusted for pre-cue speed = %.0f ms, vs %.0f ms total.\n",
              direct_ms, shift_ms))
  cat(sprintf("  -> about %.0f%% of the tempo effect travels through movement speed. r(urgent, speed) = %.2f.\n",
              100 * max(0, 1 - direct_ms / shift_ms), cor(warn$urgent, warn$precue_speed_ms, use = "complete.obs")))
  cat("  -> this is a DIRECT effect with a mediator held constant. Do not quote it as H3a.\n")
}

# Intention-to-deliver sensitivity: refit on ASSIGNED lead. Section 8 keeps mistimed trials, so this
# shows what the answer would have been had delivery been taken at face value.
alt <- suppressWarnings(suppressMessages(tryCatch(
  glmer(avoid ~ assigned_lead_s + urgent + (1 | pid), data = warn,
        family = binomial(link = lapse_logit(LAMBDA)),
        control = glmerControl(optimizer = "bobyqa", calc.derivs = FALSE)),
  error = function(e) NULL)))
if (!is.null(alt)) {
  b <- lme4::fixef(alt)
  lt80_assigned <- 1000 * (lt_target(.80) - unname(b["(Intercept)"])) / unname(b["assigned_lead_s"])
  cat(sprintf("\nIntention-to-deliver check: LT80 on ASSIGNED lead = %.0f ms vs %.0f ms on realized (%+.0f ms).\n",
              lt80_assigned, est$point_ms[2], lt80_assigned - est$point_ms[2]))
  cat("  -> a large gap means delivery error is doing real work; report both (section 8).\n")
}

# ---------------------------------------------------------------------------
# 7. OUTPUTS
# ---------------------------------------------------------------------------
est$lapse            <- LAMBDA
est$sd_threshold_ms  <- 1000 * sd_thr
est$n_warning_trials <- nrow(warn)
est$n_participants   <- length(unique(warn$participant))
est$model            <- model_used
est$h3a_one_sided_p  <- p_h3a
est$tempo_shift_ms   <- shift_ms
write.csv(est, "e2_estimates.csv", row.names = FALSE)
writeLines(diag_lines, "e2_diagnostics.txt")

png("e2_curve.png", width = 900, height = 650, res = 110)
br  <- quantile(warn$realized_lead_s, seq(0, 1, length.out = 9), na.rm = TRUE)
bin <- cut(warn$realized_lead_s, unique(br), include.lowest = TRUE)
ag  <- aggregate(avoid ~ bin, warn, mean)
mid <- aggregate(realized_lead_s ~ bin, warn, median)
plot(mid$realized_lead_s, ag$avoid, pch = 19, ylim = c(0, 1),
     xlab = "Realized warning lead (s)", ylab = "P(avoid)",
     main = "E2 lead-time response curve")
xs <- seq(min(warn$realized_lead_s), max(warn$realized_lead_s), length.out = 200)
bb <- lme4::fixef(fit)
lines(xs, (1 - LAMBDA) * plogis(unname(bb["(Intercept)"]) + unname(bb["realized_lead_s"]) * xs), lwd = 2)
abline(h = c(.5, .8), lty = 3, col = "grey50")
if (LAMBDA > 0) abline(h = 1 - LAMBDA, lty = 3, col = "orange")   # the lapse ceiling
abline(v = est$point_ms / 1000, lty = 2, col = c("steelblue", "firebrick"))
if (nrow(go)) abline(h = 1 - mean(go$crossed), lty = 4, col = "darkgreen")
legend("bottomright", bty = "n", lty = c(2, 2, 4), col = c("steelblue", "firebrick", "darkgreen"),
       legend = c(sprintf("LT50 %.0f ms", est$point_ms[1]),
                  sprintf("LT80 %.0f ms", est$point_ms[2]),
                  "zero-warning anchor"))
invisible(dev.off())

cat("\nWrote e2_estimates.csv, e2_flow.csv, e2_diagnostics.txt, e2_curve.png\n")
cat("\n--------------------------------------------------------------------------\n")
cat("BEFORE REPORTING:\n")
cat(" 1. Read section 2's diagnostics first. A compressed lead spread, a low go-trial crossing rate,\n")
cat("    or a large prediction error each invalidate the curve above them.\n")
cat(" 2. Quote the BOOTSTRAP interval, and per section 16.2 quote its UPPER BOUND as the operating point.\n")
cat(" 3. Re-run with nsim_boot >= 2000 for the manuscript. The default 200 is for checking, not print.\n")
cat(" 4. The between-participant threshold SD is a headline result (section 10), not a nuisance parameter.\n")
cat(" 5. Any fallback to the random-intercept model is a preregistered deviation - report that it happened.\n")
cat("--------------------------------------------------------------------------\n")
