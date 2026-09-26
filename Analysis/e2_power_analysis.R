# =============================================================================
# PAPER 2 (E2) - sample size for the warning lead-time curve
#   "Stopping a Reach in VR"   [PAPER2_STUDY_DESIGN §4, §9, §10]
#
# Run:  Rscript e2_power_analysis.R          # full run, nsim = 400 (~1.5 h; run it in the background)
#       Rscript e2_power_analysis.R 25       # quick look, ~3 min
# Writes: e2_power_curve.csv, e2_power_sensitivity.csv
#
# Install once: install.packages(c("lme4"))
#
# -----------------------------------------------------------------------------
# WHAT THIS TARGETS, AND WHY IT IS NOT "POWER"
# -----------------------------------------------------------------------------
# H1 is an ESTIMATION target, not a directional test (§9). "More warning helps" is not in scientific
# doubt, so there is no meaningful power to compute for it. The deliverable is a NUMBER WITH AN
# INTERVAL - "LT80 = X ms, 95% CI [a, b]" - and §16.2 goes further: the engineering figure is the
# INTERVAL'S UPPER BOUND, not the point estimate.
#
# So the sample size question is: how many participants until the LT80 interval is narrow enough to
# specify a system? This script answers that, and computes conventional power only for H3a (tempo),
# which IS a directional test.
#
# §16.2 also states the honest fallback: if the interval stays too wide to specify an operating point,
# say so. That is a publishable conclusion, not a failure - but it is much better discovered here.
#
# -----------------------------------------------------------------------------
# ⚠ EVERY PARAMETER BELOW IS A GUESS. THAT IS THE POINT OF THE PILOT.
# -----------------------------------------------------------------------------
# The provisional targets in §4 (24 analyzable / 30 maximum) were reasoned, not simulated. Re-run this
# with pilot estimates before freezing N, exactly as Paper 1's power_analysis.R demands for its own.
# =============================================================================

suppressMessages(library(lme4))

args   <- commandArgs(trailingOnly = TRUE)
NSIM   <- if (length(args) >= 1) as.integer(args[1]) else 400
set.seed(20260914)

# ---------------------------------------------------------------------------
# 1. ASSUMED TRUTH  (replace from pilot)
# ---------------------------------------------------------------------------
# LT50 central guess. Simple hand reaction time is ~150-220 ms; arresting a reach already in flight
# should need at least that plus the braking itself, so 250 ms is a deliberately mid-range guess.
TRUE_LT50      <- 0.25      # s
# Slope, on the logit scale per second of lead. Chosen so avoidance runs ~10% -> ~90% across ~250 ms,
# i.e. a curve steep enough to be worth fitting but not a step function.
TRUE_SLOPE     <- 17.6      # logit / s
# Between-participant SD of the threshold. THE LEAST KNOWN NUMBER HERE, and also a headline secondary
# estimand (§10): it is what the paper says about whether one setting can serve everyone.
SD_THRESHOLD   <- 0.060     # s
# Between-participant SD of the slope. Smaller effect on the interval; kept modest.
SD_SLOPE       <- 4.0
# H3a: urgent reaches are expected to need MORE warning. Size unknown; 50 ms is a guess.
TEMPO_EFFECT   <- 0.050     # s added to the threshold on urgent trials
# Lapse rate: attention failures and cue misses put a ceiling below 1.0. Ignoring it inflates precision.
LAPSE          <- 0.03

# ---------------------------------------------------------------------------
# 2. DELIVERY MODEL  (measured, not assumed)
# ---------------------------------------------------------------------------
# Simulating on ASSIGNED leads would overstate precision, because delivery is systematically short and
# the achieved spread is ~18% narrower than the assigned spread - less leverage on the curve.
# These come from driving the real E2TrialRunner with synthetic minimum-jerk reaches
# (see Core/E2/E2SessionPlan.cs, "Delivered leads run short").
ASSIGNED_LEVELS <- c(0.08, 0.15, 0.25, 0.35, 0.50)
ACHIEVED_MEAN   <- c(0.056, 0.119, 0.206, 0.282, 0.401)   # measured medians
ACHIEVED_SD     <- 0.015    # trial-to-trial scatter: frame quantisation + latency jitter

WARNING_TRIALS  <- 90       # 25% of 360 (§8)

# ---------------------------------------------------------------------------
# 3. ONE SIMULATED STUDY
# ---------------------------------------------------------------------------
simulate_study <- function(n_participants, warning_trials = WARNING_TRIALS) {
  per_level <- max(1L, floor(warning_trials / length(ASSIGNED_LEVELS)))

  rows <- lapply(seq_len(n_participants), function(pid) {
    thr_i   <- rnorm(1, TRUE_LT50, SD_THRESHOLD)
    slope_i <- max(4, rnorm(1, TRUE_SLOPE, SD_SLOPE))     # a non-positive slope is not a person

    lvl   <- rep(seq_along(ASSIGNED_LEVELS), each = per_level)
    lead  <- rnorm(length(lvl), ACHIEVED_MEAN[lvl], ACHIEVED_SD)
    # Tempo crossed with level and balanced within participant (§8: allocation identical across tempo).
    tempo <- rep(c(0, 1), length.out = length(lvl))
    tempo <- tempo[sample.int(length(tempo))]

    thr   <- thr_i + TEMPO_EFFECT * tempo
    p     <- (1 - LAPSE) * plogis(slope_i * (lead - thr))
    data.frame(pid = pid, lead = lead, tempo = tempo,
               avoid = rbinom(length(p), 1, p))
  })
  out <- do.call(rbind, rows)
  out$pid <- factor(out$pid)   # factor once, after binding, so levels cannot be coerced per-chunk
  out
}

# ---------------------------------------------------------------------------
# 4. FIT AND EXTRACT LT80
# ---------------------------------------------------------------------------
# LT80 is derived, not a coefficient: logit(0.8) = b0 + b1 * LT80  =>  LT80 = (logit(0.8) - b0) / b1.
# Its SE comes from the delta method on the fixed-effect covariance.
#
# The confirmatory analysis should use a bootstrap or posterior interval instead (§10) - the delta
# method is a linearisation and is used here only because a power simulation needs thousands of fits.
# If this script says N is marginal, treat that as optimistic.
lt_from_fit <- function(fit, target_p = 0.80) {
  b  <- lme4::fixef(fit)
  V  <- as.matrix(vcov(fit))
  b0 <- unname(b["(Intercept)"]); b1 <- unname(b["lead"])
  if (is.na(b1) || b1 <= 0) return(c(est = NA, lo = NA, hi = NA))

  k   <- qlogis(target_p)
  est <- (k - b0) / b1

  # d/db0 = -1/b1 ; d/db1 = -(k - b0)/b1^2
  g   <- c(-1 / b1, -(k - b0) / (b1^2))
  idx <- match(c("(Intercept)", "lead"), colnames(V))
  se  <- sqrt(as.numeric(t(g) %*% V[idx, idx] %*% g))
  c(est = est, lo = est - 1.96 * se, hi = est + 1.96 * se)
}

# The between-participant SD of the THRESHOLD, in seconds - the headline secondary estimand (§10).
#
# ⚠ This is NOT the random-intercept SD. glmer's intercept SD lives on the LOGIT scale and is roughly
# SD_threshold * slope, so reading it as milliseconds overstates the answer by ~20x. It must be
# converted through the slope.
#
#   thr_i = -(b0 + u0_i) / (b1 + u1_i)
#   d/du0 = -1/b1 ;  d/du1 = -thr/b1
#   => var(thr) = [ s0^2 + thr^2 * s1^2 + 2 * thr * cov01 ] / b1^2
#
# Validated against known truth at SD = 30/60/120/200 ms -> recovered 27/60/112/187 ms. The mild
# downward bias at the top is partial pooling doing its job, so treat this as a slight UNDER-estimate
# of participant heterogeneity - the direction that argues for a wider operating margin, not a narrower one.
sd_threshold_from_fit <- function(fit) {
  b  <- lme4::fixef(fit)
  b0 <- unname(b["(Intercept)"]); b1 <- unname(b["lead"])
  if (is.na(b1) || b1 <= 0) return(NA_real_)
  S <- lme4::VarCorr(fit)$pid
  if (is.null(S) || !all(c("(Intercept)", "lead") %in% rownames(S))) return(NA_real_)
  thr <- -b0 / b1
  v   <- (S["(Intercept)", "(Intercept)"] + thr^2 * S["lead", "lead"] +
          2 * thr * S["(Intercept)", "lead"]) / b1^2
  if (is.na(v) || v < 0) return(NA_real_)
  sqrt(v)
}

fit_one <- function(dat) {
  suppressWarnings(suppressMessages(
    tryCatch(
      glmer(avoid ~ lead + tempo + (1 + lead | pid), data = dat, family = binomial,
            control = glmerControl(optimizer = "bobyqa", calc.derivs = FALSE,
                                   optCtrl = list(maxfun = 2e4))),
      error = function(e) NULL)
  ))
}

# ---------------------------------------------------------------------------
# 5. GRID
# ---------------------------------------------------------------------------
# §4's provisional planning figure is 24 analyzable / 30 maximum. The grid brackets it generously so a
# shortfall is visible rather than assumed away.
N_GRID       <- c(12, 16, 20, 24, 30, 40)
USEFUL_WIDTH <- 0.150   # s. A +/-75 ms interval on LT80 (§4). Narrow enough to separate operating points.

cat(sprintf("E2 power/precision simulation - nsim = %d per N\n", NSIM))
cat(sprintf("Assumed: LT50 %.0f ms, slope %.1f, threshold SD %.0f ms, tempo +%.0f ms, lapse %.0f%%\n",
            TRUE_LT50 * 1000, TRUE_SLOPE, SD_THRESHOLD * 1000, TEMPO_EFFECT * 1000, LAPSE * 100))
cat(sprintf("Target: 95%% CI on LT80 narrower than %.0f ms\n\n", USEFUL_WIDTH * 1000))

out <- do.call(rbind, lapply(N_GRID, function(n) {
  widths <- numeric(0); ests <- numeric(0); tempo_hit <- 0L; converged <- 0L; sd_est <- numeric(0)

  for (s in seq_len(NSIM)) {
    dat <- simulate_study(n)
    fit <- fit_one(dat)
    if (is.null(fit)) next
    converged <- converged + 1L

    lt <- lt_from_fit(fit, 0.80)
    if (!is.na(lt["est"])) { widths <- c(widths, lt["hi"] - lt["lo"]); ests <- c(ests, lt["est"]) }

    # H3a: urgent needs MORE warning -> a positive threshold shift -> a NEGATIVE coefficient on tempo
    # in this parameterisation (avoidance falls at a given lead). One-sided at alpha = .05.
    co <- summary(fit)$coefficients
    if ("tempo" %in% rownames(co)) {
      z <- co["tempo", "z value"]
      if (!is.na(z) && z < qnorm(0.05)) tempo_hit <- tempo_hit + 1L
    }

    s_thr <- sd_threshold_from_fit(fit)
    if (!is.na(s_thr)) sd_est <- c(sd_est, s_thr)

    if (s %% 50 == 0) cat(sprintf("  N=%2d  %d/%d\r", n, s, NSIM))
  }

  safe_med <- function(v) if (length(v)) median(v, na.rm = TRUE) else NA_real_
  safe_pct <- function(v) if (length(v)) 100 * mean(v < USEFUL_WIDTH, na.rm = TRUE) else NA_real_

  data.frame(
    N              = n,
    converged_pct  = 100 * converged / NSIM,
    lt80_median_ms = 1000 * safe_med(ests),
    ci_width_median_ms = 1000 * safe_med(widths),
    pct_within_target  = safe_pct(widths),
    sd_threshold_median_ms = 1000 * safe_med(sd_est),   # the headline secondary estimand (§10)
    power_H3a_pct  = 100 * tempo_hit / max(1L, converged)
  )
}))

cat("\n\n")
print(out, row.names = FALSE, digits = 4)

ok <- out$N[out$pct_within_target >= 80]
cat("\n")
if (length(ok)) {
  cat(sprintf("-> Smallest N where 80%% of studies achieve a CI narrower than %.0f ms: N = %d\n",
              USEFUL_WIDTH * 1000, min(ok)))
} else {
  cat(sprintf("-> NO N in the grid reliably achieves a CI narrower than %.0f ms.\n", USEFUL_WIDTH * 1000))
  cat("   Options, in preference order: more WARNING TRIALS per participant (the threshold is estimated\n")
  cat("   within person, so trials buy more than people here); a wider achieved lead spread; or accept a\n")
  cat("   wider interval and report it honestly per §16.2.\n")
}
ok3 <- out$N[out$power_H3a_pct >= 80]
cat(if (length(ok3)) sprintf("-> Smallest N for 80%% power on H3a (tempo): N = %d\n", min(ok3))
    else "-> H3a does not reach 80% power anywhere in the grid.\n")

write.csv(out, "e2_power_curve.csv", row.names = FALSE)
cat("\nWrote e2_power_curve.csv\n")

# ---------------------------------------------------------------------------
# 6. SENSITIVITY TO SD_THRESHOLD  -  READ THIS TABLE BEFORE THE N GRID ABOVE
# ---------------------------------------------------------------------------
# An independent two-stage simulation (deliberately weaker than glmer, so an upper bound on N) found
# the LT80 interval to be ~40-60 ms at EVERY N from 12 to 40 under the assumed SD_THRESHOLD of 60 ms -
# comfortably inside the 150 ms target, even at N = 12.
#
# The reason is structural. The threshold is estimated WITHIN person from ~90 trials, so each
# participant contributes a precise estimate, and the population interval is governed by
# SD_THRESHOLD / sqrt(N). With SD = 60 ms and N = 12 that is 17 ms, giving a ~68 ms interval.
#
# The consequence is worth stating plainly: **N is not the binding constraint for H1.** It is almost
# entirely set by SD_THRESHOLD - the parameter we know least about, and the one the pilot must report
# first. Double the SD and the interval doubles. So the N grid above answers the wrong question on its
# own; this sweep answers the right one.
SD_GRID <- c(0.03, 0.06, 0.10, 0.15, 0.20, 0.30)
# Scales with NSIM so that a quick look is actually quick: a floor alone made `Rscript ... 20`
# take ~30 min in this block while the main grid took 2. Each glmer fit is ~0.5-1.5 s (N=12-40).
SENS_NSIM <- min(NSIM, max(25L, NSIM %/% 4L))
cat(sprintf("\nSensitivity: LT80 interval width (ms) vs between-participant threshold SD\n"))
cat(sprintf("(target < %.0f ms; nsim = %d per cell)\n\n", USEFUL_WIDTH * 1000, SENS_NSIM))

sens <- do.call(rbind, lapply(SD_GRID, function(sd_thr) {
  old <- SD_THRESHOLD
  SD_THRESHOLD <<- sd_thr
  res <- sapply(N_GRID, function(n) {
    w <- numeric(0)
    for (s in seq_len(SENS_NSIM)) {
      f <- fit_one(simulate_study(n))
      if (is.null(f)) next
      lt <- lt_from_fit(f, 0.80)
      if (!is.na(lt["est"])) w <- c(w, lt["hi"] - lt["lo"])
    }
    if (length(w)) 1000 * median(w) else NA_real_
  })
  SD_THRESHOLD <<- old
  setNames(data.frame(sd_thr * 1000, t(res)), c("SD_threshold_ms", paste0("N", N_GRID)))
}))

print(sens, row.names = FALSE, digits = 4)
write.csv(sens, "e2_power_sensitivity.csv", row.names = FALSE)
cat("\nWrote e2_power_sensitivity.csv\n")
cat("\n-> If the pilot's threshold SD lands near the top of this range, N matters a great deal.\n")
cat("   Near the bottom, almost any N suffices for H1. Note that the first real run falsified the\n")
cat("   obvious follow-on guess too: H3a does not become the binding constraint either - section 7.\n")

# ---------------------------------------------------------------------------
# 7. SENSITIVITY TO TEMPO_EFFECT  -  THE ACTUAL SAMPLE-SIZE QUESTION
# ---------------------------------------------------------------------------
# The first real run made both headline answers land at the bottom of the grid: H1's interval clears
# 150 ms at N = 12, and H3a reaches 100% power at N = 12. Neither is close to binding, so under the
# assumed truth the N grid above cannot discriminate between 12 and 40 and is not, on its own, a
# defensible justification for any particular N.
#
# The reason H3a is so easy is the assumed effect size: a 50 ms threshold shift is 0.88 logits at the
# assumed slope, and it is estimated WITHIN person across ~1000 trials. If the true tempo effect is
# much smaller, that collapses. So the question worth simulating is not "is N enough for a 50 ms
# effect" but "how small an effect can this design still resolve" - which is what a reviewer will ask,
# and what the minimum detectable effect in §9 should be set from.
TEMPO_GRID <- c(0.010, 0.020, 0.030, 0.050, 0.080)
cat("\nSensitivity: power for H3a (tempo) vs the TRUE tempo effect\n")
cat(sprintf("(one-sided alpha = .05; nsim = %d per cell)\n\n", SENS_NSIM))

tempo_sens <- do.call(rbind, lapply(TEMPO_GRID, function(te) {
  old <- TEMPO_EFFECT
  TEMPO_EFFECT <<- te
  res <- sapply(N_GRID, function(n) {
    hit <- 0L; ok <- 0L
    for (s in seq_len(SENS_NSIM)) {
      f <- fit_one(simulate_study(n))
      if (is.null(f)) next
      ok <- ok + 1L
      co <- summary(f)$coefficients
      if ("tempo" %in% rownames(co)) {
        z <- co["tempo", "z value"]
        if (!is.na(z) && z < qnorm(0.05)) hit <- hit + 1L
      }
    }
    if (ok) 100 * hit / ok else NA_real_
  })
  TEMPO_EFFECT <<- old
  setNames(data.frame(te * 1000, t(res)), c("tempo_effect_ms", paste0("N", N_GRID)))
}))

print(tempo_sens, row.names = FALSE, digits = 4)
write.csv(tempo_sens, "e2_power_tempo_sensitivity.csv", row.names = FALSE)
cat("\nWrote e2_power_tempo_sensitivity.csv\n")
cat("\n-> Read this as a minimum detectable effect: find the smallest tempo effect your chosen N still\n")
cat("   catches at 80%, and state THAT in §9 as what the study can and cannot rule out.\n")
cat("\n   CAUTION: at low nsim this table is visibly noisy. The 10 ms row ran 16/44/28/28/56/60 across\n")
cat("   N = 12..40, which is not monotone and cannot be - power rises with N. Treat cells near 50%\n")
cat("   as Monte-Carlo scatter, and re-run at nsim >= 400 before quoting any single one of them.\n")


cat("\n--------------------------------------------------------------------------\n")
cat("BEFORE USING THIS NUMBER:\n")
cat(" 1. Every parameter in section 1 is a guess. Re-run with pilot estimates.\n")
cat(" 2. SD_THRESHOLD is the least known and the most influential - it is also a headline\n")
cat("    secondary estimand. The pilot should report it first - BUT SEE THE NEXT LINE.\n")
cat("    ! The FITTED SD is a ~18% UNDER-estimate (partial pooling): a dry run of e2_analysis.R\n")
cat("      recovered 49 ms from data generated at 60 ms. A smaller SD argues for a smaller N, so\n")
cat("      inflate the pilot SD before reading it into the section 6 table, or N comes out too low.\n")
cat(" 3. MEASURED 2026-09-14: the parametric bootstrap interval is 1.03-1.07x the delta-method width\n")
cat("    (e2_analysis.R on a 24-participant simulated dataset). The widths above are therefore mildly\n")
cat("    optimistic, not badly wrong - inflate by about 7% before quoting them.\n")
cat(" 4. Trials per participant are a stronger lever than participants (§4): the threshold and the\n")
cat("    tempo contrast are both estimated WITHIN person. Try raising WARNING_TRIALS before raising N.\n")
cat(" 5. Under the assumed truth NEITHER headline target binds N - both clear at N = 12. Do not quote\n")
cat("    the N grid as the justification for 24/30. Justify N from sections 6 and 7 (how much\n")
cat("    participant heterogeneity and how small a tempo effect you want to remain able to resolve),\n")
cat("    and from the dropout and counterbalancing constraints in §4.\n")
cat("--------------------------------------------------------------------------\n")
