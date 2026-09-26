# =============================================================================
# PAPER 1 — power for the PRIMARY OPPORTUNITY-LEVEL BINOMIAL MODEL
#   "What Makes a Collision Warning Work?"   [PAPER1_STUDY_DESIGN §3, §9; CODE_GAP #12]
#
# Run:  Rscript power_analysis.R 25       # quick look, ~6 min
#       Rscript power_analysis.R          # full run, nsim = 200 (slow; background it)
# Writes: p1_power_curve.csv, p1_power_h3_mde.csv
#
# -----------------------------------------------------------------------------
# WHAT CHANGED, AND WHY THE OLD VERSION WAS ANSWERING A DIFFERENT QUESTION
# -----------------------------------------------------------------------------
# The previous script simulated BLOCK-LEVEL COLLISION COUNTS and fitted a negative-binomial
# GLMM with offset(log(opportunities)) - the old primary model. The primary outcome is now a
# PER-OPPORTUNITY BINARY VIOLATION (§5, §9). Different unit, different denominator, different
# family, and different power. A count model's power figure does not transfer.
#
# This version simulates and fits what `paper1_analysis.R` actually runs:
#     violation ~ Policy * Mapping + (1 + Policy + Mapping | participant) + (1 | layout)
#
# -----------------------------------------------------------------------------
# THE HEADLINE, STATED UP FRONT  —  FULL GRID, nsim = 120, run 2026-09-23
# -----------------------------------------------------------------------------
# Confirmatory family: H1 (trigger policy), H2 (localization), H4' (cue form, PB vs PBC).
# H3 (interaction) is EXPLORATORY and gates nothing (§3, resolved 2026-09-23).
#
#     N  opps    H1     H2     H3    H4'
#    ------------------------------------
#    36    12    99%    68%    30%    79%
#    36    16   100%    74%    30%    88%
#    36    18   100%    73%    48%    95%
#    36    20   100%    82%    43%    93%
#    36    24   100%    86%    48%    99%
#    48    12    99%    74%    37%    88%
#    48    16   100%    89%    48%    96%
#    48    18   100%    93%    57%    96%   <- RECOMMENDED
#    48    20   100%    93%    57%    97%
#    48    24   100%    98%    67%   100%
#    60    12   100%    84%    40%    94%
#    60    16   100%    94%    56%    98%
#    60    18   100%    94%    63%   100%
#    60    20   100%    97%    63%   100%
#    60    24   100%    98%    76%   100%
#
#     80% power reached at:  H1   N=36 x 12
#                            H2   N=36 x 20
#                            H4'  N=36 x 12 (79%, so effectively N=36 x 16)
#                            H3   NOWHERE IN THE GRID - 76% at N=60 x 24 is the ceiling
#
# ►► DESIGN SET 2026-09-23: N = 48 ANALYSABLE x 18 OPPORTUNITIES PER BLOCK ◄◄
# [PAPER1_STUDY_DESIGN §4; OpportunitySchedules.TargetOpportunitiesPerBlock]
# All three confirmatory hypotheses clear with margin (H1 100%, H2 93%, H4' 96%). H3 lands at
# 57% and is reported as exploratory with the MDE table below. Sizing is on H2, the weakest
# CONFIRMATORY test - H3 is exploratory and constrains nothing.
# Recruit to 54 to absorb exclusions, then analyse the first 48 complete balanced sets (48 is
# divisible by 6 so the Williams square balances exactly; 54 is not). Preregister that rule.
# Rejected: N=36 x 20 (H2 82%, clears by two points with no room for the Holm caveat below);
#           N=48 x 24 (H2 98%, but 144 opportunities per participant - 2x the current session).
#
# ⚠ THE ENGINE CANNOT DELIVER 18 YET. OpportunitySchedules.Layout1() authors 12 events on a
# 180 s block; six more per layout are needed, across six layouts. Not a constant change: the
# block grows to about 240 s, obstacle balance must hold, and the geometry audit must re-run over
# all 18. (O2 re-siting is already done - the current 12 pass 12/12.) AssertMeetsTarget() throws
# until that lands.
#
# ⚠ THESE ARE UNCORRECTED PER-TEST POWERS. The analysis applies HOLM across the three
# confirmatory hypotheses (paper1_analysis.R §6A), which this script does not simulate.
# Holm is step-down, so the practical cost is small: H1 will almost always be the smallest
# p-value and H4' the second, leaving H2 tested at the full alpha = 0.05. But that is
# CONDITIONAL on H1 and H4' both passing - if H4' fails, H2 faces alpha/2. Read the H2
# column as slightly optimistic and choose a cell with margin rather than one that just
# clears 80%. Simulating Holm directly is the honest fix if H2 becomes decision-critical.
#
# WHY OPPORTUNITIES AND NOT PARTICIPANTS. 12 -> 20 opportunities at N=36 moves H2 by 14
# points and H4' by 14; going N=36 -> 60 at 12 opportunities moves H2 by 16 and costs 24
# whole sessions. Opportunities are the cheap axis - but the session-length ceiling is
# UNMEASURED (documented nowhere), so the pilot must time a block before N is frozen.
#
# H3 DOES NOT RESPOND TO EITHER AXIS in the useful range (30% -> 48% across all of N=36).
# That is the empirical case for de-gating it, and it is stronger than the convenience
# argument: no affordable design rescues it.
#
# H4' was added 2026-09-23. A single dry-run draw gave p = 0.18 and looked discouraging;
# the grid says 79-100%. One draw is not a power estimate - which is why the grid exists.
#
# (An earlier 8-replicate study put H3 near 34% at N=36 x 12 and suggested N=60 x 24 would
# clear 80%. The nsim=200 grid said 76%; this nsim=120 grid says 76%. Trust the grids.)
#
# -----------------------------------------------------------------------------
# WHAT SIZE OF INTERACTION H3 CAN RESOLVE  (section 6, nsim = 30, 24 opps/block)
# -----------------------------------------------------------------------------
#    interaction   OR      N=36   N=48   N=60   N=80
#   --------------------------------------------------
#      +0.20      1.22      37%    13%    27%    57%   <- NOISE, see below
#      +0.30      1.35      43%    57%    83%    83%
#      +0.45      1.57      93%    93%   100%    97%
#      +0.60      1.82     100%   100%   100%   100%
#      +0.80      2.23     100%   100%   100%   100%
#
# READ THIS AS: the design resolves an interaction of OR ~1.57 or larger at any N in the
# grid. OR 1.35 - which is the value ASSUMED in section 1 - needs N = 60. OR 1.22 is out
# of reach entirely.
#
# ⚠ THE +0.20 ROW IS NOISE, NOT A RESULT. Power cannot fall as N rises, so 37/13/27/57 is
# sampling error: at nsim = 30 the standard error is about 9 points. Report that row as
# "undetectable at any N tested" and never quote its individual cells. The same caution
# applies to the 93%/93%/100%/97% non-monotonicity in the +0.45 row.
#
# This table is what the preregistration should state about H3: not "we expect an
# interaction" but "this design can rule out interactions smaller than OR ~1.57."
#
# -----------------------------------------------------------------------------
# EVERY NUMBER IN SECTION 1 IS A GUESS. RE-RUN FROM PILOT ESTIMATES BEFORE FREEZING N.
# =============================================================================

suppressMessages(library(lme4))

args  <- commandArgs(trailingOnly = TRUE)
NSIM  <- if (length(args) >= 1) as.integer(args[1]) else 200L
ALPHA <- 0.05
set.seed(20260921)

# Second argument switches to the HOLM-CORRECTED check (section 4B) and skips the uncorrected grid.
#   Rscript power_analysis.R 200 holm
# The grids above report UNCORRECTED per-test power; the analysis applies Holm across H1/H2/H4'
# (paper1_analysis.R §6A). Those are not the same number, and the difference falls on H2 - the test
# the design was sized on. This mode measures the real thing instead of reasoning about it.
HOLM_MODE <- length(args) >= 2 && tolower(args[2]) %in% c("holm", "true", "1")

# ---------------------------------------------------------------------------
# 1. ASSUMED TRUTH — log-odds scale (replace from pilot)
# ---------------------------------------------------------------------------
# Base cell is RG: proximity timing, generic torso cue.
BASE_LOGIT   <- 0.20    # ~55% violation rate with a late, undifferentiated cue
EFF_POLICY   <- -0.70   # predictive helps.  OR 0.50
EFF_MAPPING  <- -0.35   # localized helps.   OR 0.70
EFF_INTERACT <- 0.30    # redundancy: localization helps LESS under prediction. OR 1.35

# H4' — CUE FORM [added 2026-09-23; docs/PAPER1_PARAMETER_JUSTIFICATION.md §5B].
# PBC shares PB's trigger, site and onset and differs only in delivering a CONTINUOUS,
# modulable intensity. Valkov & Linsen (IEEE VR 2019) found their continuous speed-scaled
# cue produced significantly more collisions than a fixed-distance one at N = 40
# (Z = -2.23, p = 0.024), so the sign is theirs. The magnitude is a design assumption: a
# collision-count difference on a different task does not convert cleanly to an odds ratio,
# so 0.40 (OR 1.49) is a deliberately MODEST guess. If the true effect is smaller, the
# H4' row of the grid below is the honest statement of what this design can detect.
EFF_PBC      <- 0.40    # continuous mapping is WORSE than discrete. OR 1.49

# Random effects. Slopes are included deliberately: the design's model has them, and pretending
# they are zero inflates power. This is the main reason the numbers here are lower than a
# naive calculation would give.
SD_PARTICIPANT     <- 0.55
SD_SLOPE_POLICY    <- 0.30
SD_SLOPE_MAPPING   <- 0.25
SD_LAYOUT          <- 0.20

# Trial loss, from the validity rules (§5). These shrink the denominator.
NOT_PRESENTED <- 0.03
ABORTED       <- 0.04

# ---------------------------------------------------------------------------
# 2. ONE SIMULATED STUDY
# ---------------------------------------------------------------------------
# Only the four factorial cells are simulated. None and PBC are not part of the 2x2 - they
# are the floor and the H4 benchmark - so including them would inflate the apparent sample.
CELLS <- data.frame(
  condition = c("RG", "RB", "PG", "PB"),
  policy    = c(0, 0, 1, 1),      # 1 = predictive
  mapping   = c(0, 1, 0, 1))      # 1 = localized

simulate_study <- function(n_participants, opps_per_block, interact = EFF_INTERACT) {
  lay_re <- rnorm(3, 0, SD_LAYOUT)

  rows <- lapply(seq_len(n_participants), function(pid) {
    p_int  <- rnorm(1, 0, SD_PARTICIPANT)
    p_pol  <- rnorm(1, 0, SD_SLOPE_POLICY)
    p_map  <- rnorm(1, 0, SD_SLOPE_MAPPING)

    do.call(rbind, lapply(seq_len(nrow(CELLS)), function(k) {
      pol <- CELLS$policy[k]; map <- CELLS$mapping[k]
      lay <- ((pid + k) %% 3) + 1

      eta <- BASE_LOGIT + p_int + lay_re[lay] +
             (EFF_POLICY  + p_pol) * pol +
             (EFF_MAPPING + p_map) * map +
             interact * pol * map

      # Validity attrition: an opportunity that was not presented, or whose window did not
      # complete, leaves the denominator entirely (§5).
      keep <- runif(opps_per_block) > NOT_PRESENTED
      keep <- keep & (runif(opps_per_block) > ABORTED)
      n <- sum(keep)
      if (n == 0) return(NULL)

      data.frame(pid = pid, layout = lay, Policy = pol, Mapping = map,
                 violation = rbinom(n, 1, plogis(eta)))
    }))
  })

  out <- do.call(rbind, rows)
  out$pid <- factor(out$pid); out$layout <- factor(out$layout)
  out
}

# ---------------------------------------------------------------------------
# 3. FIT — the same model and the same fallback as paper1_analysis.R
# ---------------------------------------------------------------------------
# Matching the fallback matters. If power were computed on the full random-slope model but the
# analysis usually falls back to random intercepts, the power figure would describe a model the
# study rarely fits.
FULL <- violation ~ Policy * Mapping + (1 + Policy + Mapping | pid) + (1 | layout)
RI   <- violation ~ Policy * Mapping + (1 | pid) + (1 | layout)

fit_one <- function(dat) {
  f <- function(form) suppressWarnings(suppressMessages(tryCatch(
    glmer(form, data = dat, family = binomial,
          control = glmerControl(optimizer = "bobyqa", calc.derivs = FALSE,
                                 optCtrl = list(maxfun = 2e4))),
    error = function(e) NULL)))
  m <- f(FULL)
  if (is.null(m) || isSingular(m, tol = 1e-5)) { alt <- f(RI); if (!is.null(alt)) m <- alt }
  m
}

# Two-sided p-values for the three confirmatory terms.
hits <- function(m) {
  if (is.null(m)) return(NULL)
  co <- summary(m)$coefficients
  need <- c("Policy", "Mapping", "Policy:Mapping")
  if (!all(need %in% rownames(co))) return(NULL)
  c(H1 = co["Policy", 4], H2 = co["Mapping", 4], H3 = co["Policy:Mapping", 4])
}

# ---------------------------------------------------------------------------
# 3B. H4' — PB vs PBC, a two-condition contrast outside the 2x2
# ---------------------------------------------------------------------------
# H4' is NOT a factorial term. It compares two conditions that are identical except for cue
# form, so it gets its own simulation and its own model - the same one paper1_analysis.R
# uses for planned contrasts. Folding it into the 2x2 would be a category error and would
# also overstate its N, since only two of the six blocks contribute.
simulate_h4prime <- function(n_participants, opps_per_block) {
  lay_re <- rnorm(3, 0, SD_LAYOUT)
  rows <- lapply(seq_len(n_participants), function(pid) {
    p_int <- rnorm(1, 0, SD_PARTICIPANT)
    # PB is the reference; PBC adds EFF_PBC. Both carry the predictive + localized effects,
    # which are therefore absorbed into the shared intercept and cancel in the contrast.
    do.call(rbind, lapply(c("PB", "PBC"), function(cond) {
      lay <- ((pid + match(cond, c("PB", "PBC"))) %% 3) + 1
      eta <- BASE_LOGIT + p_int + lay_re[lay] + EFF_POLICY + EFF_MAPPING + EFF_INTERACT +
             (if (cond == "PBC") EFF_PBC else 0)
      keep <- (runif(opps_per_block) > NOT_PRESENTED) & (runif(opps_per_block) > ABORTED)
      n <- sum(keep)
      if (n == 0) return(NULL)
      data.frame(pid = pid, layout = lay, cond = cond, violation = rbinom(n, 1, plogis(eta)))
    }))
  })
  out <- do.call(rbind, rows)
  if (is.null(out)) return(NULL)
  out$pid <- factor(out$pid); out$layout <- factor(out$layout)
  out$cond <- relevel(factor(out$cond), ref = "PB")
  out
}

power_h4prime <- function(n, opps, nsim) {
  ok <- 0L; sig <- 0L
  for (s in seq_len(nsim)) {
    d <- simulate_h4prime(n, opps)
    if (is.null(d)) next
    m <- suppressWarnings(suppressMessages(tryCatch(
      glmer(violation ~ cond + (1 | pid) + (1 | layout), data = d, family = binomial,
            control = glmerControl(optimizer = "bobyqa", calc.derivs = FALSE)),
      error = function(e) NULL)))
    if (is.null(m)) next
    co <- summary(m)$coefficients
    r <- grep("^cond", rownames(co))[1]
    if (is.na(r)) next
    ok <- ok + 1L
    if (co[r, 4] < ALPHA) sig <- sig + 1L
  }
  if (ok == 0) return(NA_real_)
  100 * sig / ok
}

# ---------------------------------------------------------------------------
# 3C. HOLM-CORRECTED POWER — the number the study is actually decided on
# ---------------------------------------------------------------------------
# The grids elsewhere in this file fit H1/H2/H3 and H4' from SEPARATE simulated studies, which is fine
# for uncorrected per-test power but cannot produce a family-wise figure: Holm operates on the three
# p-values TOGETHER, from one study, and H1 and H4' are correlated because they share the PB cell.
#
# So this simulates one participant sample providing ALL SIX conditions, fits the factorial on the four
# 2x2 cells and the cue-form contrast on PB vs PBC, and applies Holm exactly as paper1_analysis.R §6A does.
#
# WHY IT MATTERS. Holm is step-down: the smallest p is tested at alpha/3, the next at alpha/2, the last at
# alpha. H1 is near-certain to be smallest and H4' next, which would leave H2 at the full alpha - but that
# is CONDITIONAL on both passing. When H4' fails, H2 faces alpha/2. The design was sized on H2, so the
# question "how much power does H2 really have" is the one the sample size rests on.
simulate_six_conditions <- function(n_participants, opps_per_block) {
  lay_re <- rnorm(3, 0, SD_LAYOUT)
  conds <- c("RG", "RB", "PG", "PB", "PBC")   # None is the floor; it is not in the family
  rows <- lapply(seq_len(n_participants), function(pid) {
    p_int <- rnorm(1, 0, SD_PARTICIPANT)
    p_pol <- rnorm(1, 0, SD_SLOPE_POLICY)
    p_map <- rnorm(1, 0, SD_SLOPE_MAPPING)
    do.call(rbind, lapply(seq_along(conds), function(k) {
      cd  <- conds[k]
      pol <- as.integer(cd %in% c("PG", "PB", "PBC"))
      map <- as.integer(cd %in% c("RB", "PB", "PBC"))
      lay <- ((pid + k) %% 3) + 1
      eta <- BASE_LOGIT + p_int + lay_re[lay] +
             (EFF_POLICY + p_pol) * pol + (EFF_MAPPING + p_map) * map +
             EFF_INTERACT * pol * map +
             (if (cd == "PBC") EFF_PBC else 0)
      keep <- (runif(opps_per_block) > NOT_PRESENTED) & (runif(opps_per_block) > ABORTED)
      nk <- sum(keep); if (nk == 0) return(NULL)
      data.frame(pid = pid, layout = lay, condition = cd,
                 Policy = pol, Mapping = map, violation = rbinom(nk, 1, plogis(eta)))
    }))
  })
  out <- do.call(rbind, rows)
  if (is.null(out)) return(NULL)
  out$pid <- factor(out$pid); out$layout <- factor(out$layout)
  out
}

holm_power <- function(n, opps, nsim) {
  raw  <- matrix(NA_real_, nsim, 3, dimnames = list(NULL, c("H1", "H2", "H4")))
  ok <- 0L
  for (s in seq_len(nsim)) {
    d <- simulate_six_conditions(n, opps)
    if (is.null(d)) next

    cell <- subset(d, condition %in% c("RG", "RB", "PG", "PB"))
    mf <- fit_one(cell)
    if (is.null(mf)) next
    cf <- summary(mf)$coefficients
    if (!all(c("Policy", "Mapping") %in% rownames(cf))) next

    pbc <- subset(d, condition %in% c("PB", "PBC"))
    pbc$cond <- relevel(factor(pbc$condition), ref = "PB")
    mc <- suppressWarnings(suppressMessages(tryCatch(
      glmer(violation ~ cond + (1 | pid) + (1 | layout), data = pbc, family = binomial,
            control = glmerControl(optimizer = "bobyqa", calc.derivs = FALSE)),
      error = function(e) NULL)))
    if (is.null(mc)) next
    cc <- summary(mc)$coefficients
    r <- grep("^cond", rownames(cc))[1]
    if (is.na(r)) next

    ok <- ok + 1L
    raw[ok, ] <- c(cf["Policy", 4], cf["Mapping", 4], cc[r, 4])
  }
  if (ok == 0) return(NULL)
  raw <- raw[seq_len(ok), , drop = FALSE]
  adj <- t(apply(raw, 1, p.adjust, method = "holm"))

  data.frame(
    N = n, opportunities = opps, reps = ok,
    H1_raw  = 100 * mean(raw[, "H1"] < ALPHA), H1_holm = 100 * mean(adj[, 1] < ALPHA),
    H2_raw  = 100 * mean(raw[, "H2"] < ALPHA), H2_holm = 100 * mean(adj[, 2] < ALPHA),
    H4_raw  = 100 * mean(raw[, "H4"] < ALPHA), H4_holm = 100 * mean(adj[, 3] < ALPHA),
    all_three_holm = 100 * mean(adj[, 1] < ALPHA & adj[, 2] < ALPHA & adj[, 3] < ALPHA))
}

if (HOLM_MODE) {
  cat(sprintf("HOLM-CORRECTED POWER (one sample, all conditions, Holm over H1/H2/H4'). nsim = %d.\n\n", NSIM))
  cat("The design is sized on H2. Compare H2_raw with H2_holm: that gap is the cost the\n")
  cat("uncorrected grid does not show.\n\n")
  HOLM_GRID <- data.frame(N = c(48, 36, 48, 60), opps = c(18, 20, 24, 18))
  res <- do.call(rbind, lapply(seq_len(nrow(HOLM_GRID)), function(i) {
    r <- holm_power(HOLM_GRID$N[i], HOLM_GRID$opps[i], NSIM)
    if (!is.null(r))
      cat(sprintf("  N=%2d opps=%2d | H1 %3.0f->%3.0f | H2 %3.0f->%3.0f | H4' %3.0f->%3.0f | all three %3.0f%%\n",
                  r$N, r$opportunities, r$H1_raw, r$H1_holm, r$H2_raw, r$H2_holm,
                  r$H4_raw, r$H4_holm, r$all_three_holm))
    r
  }))
  cat("\n"); print(res, row.names = FALSE, digits = 4)
  write.csv(res, "p1_power_holm.csv", row.names = FALSE)
  cat("\nWrote p1_power_holm.csv\n")
  cat("\nIf H2_holm at the chosen design drops below 80%, the frozen N is wrong and\n")
  cat("PAPER1_STUDY_DESIGN §4 must be revised - not the other way round.\n")
  quit(save = "no")
}

run_cell <- function(n, opps, nsim, interact = EFF_INTERACT) {
  keep <- matrix(NA_real_, nrow = nsim, ncol = 3,
                 dimnames = list(NULL, c("H1", "H2", "H3")))
  ok <- 0L
  for (s in seq_len(nsim)) {
    d <- simulate_study(n, opps, interact)
    if (is.null(d)) next
    p <- hits(fit_one(d))
    if (is.null(p)) next
    ok <- ok + 1L
    keep[ok, ] <- p
  }
  if (ok == 0) return(c(H1 = NA, H2 = NA, H3 = NA, converged = 0))
  k <- keep[seq_len(ok), , drop = FALSE]
  c(H1 = 100 * mean(k[, "H1"] < ALPHA),
    H2 = 100 * mean(k[, "H2"] < ALPHA),
    H3 = 100 * mean(k[, "H3"] < ALPHA),
    converged = 100 * ok / nsim)
}

# ---------------------------------------------------------------------------
# 4. GRID — participants x opportunities per block
# ---------------------------------------------------------------------------
# Both are levers, and opportunities are far cheaper than participants. Section 5 reads the
# result off this grid: both help, and H3 needs both.
N_GRID    <- c(24, 36, 48, 60, 80)
# Finer opportunity grid, added 2026-09-23. The earlier grid tested only 12 and 24, which
# hid where the knee is. Opportunities are the cheap axis: at ~8-14 s between opportunities,
# going 12 -> 18 adds roughly 7 minutes of task time to a session whose length is dominated
# by questionnaires and breaks, whereas each extra participant costs a whole session.
# MEASURE THE REAL BLOCK DURATION AT PILOT - it is documented nowhere, so the argument above
# is arithmetic on the inter-opportunity spacing, not an observation.
OPPS_GRID <- c(12, 16, 18, 20, 24)

cat(sprintf("Paper 1 power - opportunity-level binomial model. nsim = %d per cell.\n", NSIM))
cat(sprintf("Assumed: policy %+.2f, mapping %+.2f, interaction %+.2f (log-odds); ",
            EFF_POLICY, EFF_MAPPING, EFF_INTERACT))
cat(sprintf("participant SD %.2f.\n\n", SD_PARTICIPANT))

grid <- expand.grid(opportunities = OPPS_GRID, N = N_GRID)
out <- do.call(rbind, lapply(seq_len(nrow(grid)), function(i) {
  r  <- run_cell(grid$N[i], grid$opportunities[i], NSIM)
  h4 <- power_h4prime(grid$N[i], grid$opportunities[i], NSIM)
  cat(sprintf("  N=%2d opps=%2d  H1 %3.0f%%  H2 %3.0f%%  H3 %3.0f%%  H4' %3.0f%%\n",
              grid$N[i], grid$opportunities[i], r["H1"], r["H2"], r["H3"], h4))
  data.frame(N = grid$N[i], opportunities = grid$opportunities[i],
             power_H1 = r["H1"], power_H2 = r["H2"], power_H3 = r["H3"],
             power_H4prime = h4, converged_pct = r["converged"])
}))

cat("\n")
print(out, row.names = FALSE, digits = 4)
write.csv(out, "p1_power_curve.csv", row.names = FALSE)

smallest <- function(col) {
  ok <- out[out[[col]] >= 80, ]
  if (!nrow(ok)) return(NA)
  ok[which.min(ok$N * 1000 + ok$opportunities), ]
}
for (h in c("power_H1", "power_H2", "power_H3")) {
  s <- smallest(h)
  if (is.data.frame(s)) cat(sprintf("-> %s reaches 80%% at N = %d with %d opportunities/block\n",
                                    sub("power_", "", h), s$N, s$opportunities))
  else cat(sprintf("-> %s does NOT reach 80%% anywhere in the grid\n", sub("power_", "", h)))
}

# ---------------------------------------------------------------------------
# 5. BOTH LEVERS WORK, AND H3 NEEDS BOTH
# ---------------------------------------------------------------------------
# An earlier version of this comment predicted that opportunities would NOT substitute for
# participants, on the reasoning that the interaction's standard error is dominated by
# between-participant slope variance. **The first run refuted that**, and the corrected reading
# is below. Left on record because it is the kind of plausible argument that should lose to a
# simulation.
#
# Measured (nsim = 25, so treat single cells as noisy):
#
#     H3 power        N=24  N=36  N=48  N=60  N=80
#       12 opps/block  28%   20%   20%   52%   72%
#       24 opps/block  32%   48%   52%   92%   92%
#
# Doubling opportunities adds roughly 20-40 points at every N from 36 up, and raising N helps at
# both levels. Neither lever alone reaches 80%.
#
# The reason opportunities matter so much: with 12 per cell and a violation rate near 50%, each
# participant's per-condition estimate is a proportion from about eleven usable trials. Its
# standard error is roughly 0.15 on the probability scale, and the interaction is a difference of
# four such proportions. **Within-participant measurement noise dominates**, so it has to be
# reduced before between-participant variance becomes the limiting term.

# ---------------------------------------------------------------------------
# 6. MINIMUM DETECTABLE INTERACTION — the question §3 actually needs answered
# ---------------------------------------------------------------------------
# §3 records two mechanisms predicting OPPOSITE signs for H3 (synergy vs redundancy), so the
# study must be able to resolve an interaction of a plausible size in either direction. This
# sweep says how big it has to be.
MDE_GRID  <- c(0.20, 0.30, 0.45, 0.60, 0.80)
MDE_N     <- c(36, 48, 60, 80)
MDE_NSIM  <- min(NSIM, max(25L, NSIM %/% 4L))   # scales down so a quick look stays quick

cat(sprintf("\nMinimum detectable interaction (nsim = %d per cell, 24 opportunities/block)\n\n", MDE_NSIM))
mde <- do.call(rbind, lapply(MDE_GRID, function(eff) {
  p <- sapply(MDE_N, function(n) run_cell(n, 24, MDE_NSIM, interact = eff)["H3"])
  cat(sprintf("  interaction %+.2f (OR %.2f):  %s\n", eff, exp(eff),
              paste(sprintf("N=%d %3.0f%%", MDE_N, p), collapse = "  ")))
  setNames(data.frame(eff, exp(eff), t(p)), c("interaction_logodds", "odds_ratio", paste0("N", MDE_N)))
}))
print(mde, row.names = FALSE, digits = 4)
write.csv(mde, "p1_power_h3_mde.csv", row.names = FALSE)

cat("\n-> Read this as: the smallest interaction your chosen N can resolve at 80%.\n")
cat("   State THAT in the preregistration as what the study can and cannot rule out.\n")

cat("\n--------------------------------------------------------------------------\n")
cat("BEFORE USING THESE NUMBERS:\n")
cat(" 1. Every value in section 1 is a guess. Re-run from pilot estimates.\n")
cat(" 2. Size the study on H2, the weakest CONFIRMATORY test. H1 is powered everywhere and\n")
cat("    H4' clears from N=36 x 16. H3 is exploratory and is NOT a sizing constraint.\n")
cat(" 3. RESOLVED 2026-09-23: §3 no longer gates H1 and H2 on H3, so an underpowered\n")
cat("    interaction no longer weakens the main effects. H3 is preregistered as exploratory,\n")
cat("    reported with the interval and the MDE table above, and governs nothing.\n")
cat(" 4. These are UNCORRECTED per-test powers. The analysis applies Holm across H1/H2/H4'\n")
cat("    (paper1_analysis.R §6A), which this script does not simulate. Holm is step-down so\n")
cat("    the cost is small, but read H2 as slightly optimistic and choose a cell with margin.\n")
cat(" 5. The MDE rows at small interactions are NOISE at this nsim. A non-monotonic row (power\n")
cat("    falling as N rises) is the tell. Read them as 'undetectable', never as a value.\n")
cat(" 6. Random slopes are simulated. Dropping them would raise every figure here and be wrong.\n")
cat("--------------------------------------------------------------------------\n")
