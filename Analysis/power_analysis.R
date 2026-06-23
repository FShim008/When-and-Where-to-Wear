# =============================================================================
# Monte-Carlo power analysis for the PRIMARY negative-binomial GLMM
#   "When and Where to Warn ..."  [Plan Task 0.3 / A3]
#
# Simulates the within-subjects design (6 conditions × #opportunities, random participant + layout),
# fits the SAME models as analysis.R (glmmTMB nbinom2), and estimates power across a grid of N and
# #opportunities for: the Timing×Localization INTERACTION (headline), the two main effects, and the
# PB-vs-None (floor) and PB-vs-Visual (H4) contrasts.
#
# Run:  Rscript power_analysis.R            # full run (slow: a few minutes)
#       Rscript power_analysis.R 50         # quick look with nsim = 50
# Writes: power_curve.csv  (+ prints a table and the smallest N reaching 80% power for the interaction).
#
# Install once: install.packages("glmmTMB")
#
# IMPORTANT: the rates/dispersion below are ILLUSTRATIVE PLACEHOLDERS (seeded from the mock baselines),
# NOT a hypothesis. Re-estimate them from the Phase-8 pilot, then re-run to lock N before data collection.
# =============================================================================

suppressMessages(library(glmmTMB))

args  <- commandArgs(trailingOnly = TRUE)
nsim  <- if (length(args) >= 1) as.integer(args[[1]]) else 200L   # sims per grid cell
alpha <- 0.05

## ---- ASSUMPTIONS — edit from pilot --------------------------------------------
rates <- c(None = 0.45, RG = 0.30, RB = 0.22, PG = 0.20, PB = 0.12, Visual = 0.18) # collisions / opportunity
theta <- 4.0     # NB dispersion (glmmTMB 'size'): smaller => more overdispersion => LESS power
sd_participant <- 0.35   # SD of participant random intercept (log scale) — between-subject variability
sd_layout      <- 0.10   # SD of layout random intercept (log scale)

N_grid   <- c(12, 16, 20, 24, 32)   # participants to evaluate
opp_grid <- c(12, 18, 24)           # opportunities per block (extend OpportunityScheduler if > 12)
set.seed(2027)
## ------------------------------------------------------------------------------

conditions <- names(rates)
layouts    <- paste0("L", 1:6)

# Simulate one full dataset (one row per participant × block; each participant sees all 6 conditions).
simulate_one <- function(N, opp) {
  p_off <- rnorm(N, 0, sd_participant)
  l_off <- setNames(rnorm(length(layouts), 0, sd_layout), layouts)
  df <- expand.grid(participant = 0:(N - 1), b = 0:5, KEEP.OUT.ATTRS = FALSE)
  df$condition <- conditions[df$b + 1]                       # counts are order-free; real order = Williams
  df$layout    <- layouts[((df$b + df$participant) %% 6) + 1]
  mu <- rates[df$condition] * opp * exp(p_off[df$participant + 1] + l_off[df$layout])
  df$opportunities <- opp
  df$collisions    <- rnbinom(nrow(df), size = theta, mu = pmax(mu, 1e-6))
  df
}

pval <- function(m, row) {
  if (is.null(m)) return(NA_real_)
  co <- tryCatch(summary(m)$coefficients$cond, error = function(e) NULL)
  if (is.null(co) || nrow(co) < row) return(NA_real_)
  co[row, 4]   # Pr(>|z|)
}

nb <- function(formula, data)
  tryCatch(suppressWarnings(glmmTMB(formula, family = nbinom2, data = data)), error = function(e) NULL)

# Fit the three models on one dataset and return the five p-values of interest.
fit_pvals <- function(df) {
  cells <- subset(df, condition %in% c("RG", "RB", "PG", "PB"))
  cells$Timing       <- factor(ifelse(cells$condition %in% c("PG", "PB"), "Predictive", "Reactive"),
                               levels = c("Reactive", "Predictive"))
  cells$Localization <- factor(ifelse(cells$condition %in% c("RB", "PB"), "Body", "Generic"),
                               levels = c("Generic", "Body"))

  m <- nb(collisions ~ Timing * Localization + offset(log(pmax(opportunities, 1))) +
            (1 | participant) + (1 | layout), cells)

  fl <- subset(df, condition %in% c("None", "PB")); fl$cond <- relevel(factor(fl$condition), ref = "None")
  mf <- nb(collisions ~ cond + offset(log(pmax(opportunities, 1))) + (1 | participant) + (1 | layout), fl)

  pv <- subset(df, condition %in% c("Visual", "PB")); pv$cond <- relevel(factor(pv$condition), ref = "Visual")
  mp <- nb(collisions ~ cond + offset(log(pmax(opportunities, 1))) + (1 | participant) + (1 | layout), pv)

  c(timing        = pval(m, 2),   # TimingPredictive
    localization  = pval(m, 3),   # LocalizationBody
    interaction   = pval(m, 4),   # TimingPredictive:LocalizationBody
    pb_vs_none    = pval(mf, 2),
    pb_vs_visual  = pval(mp, 2))
}

cat(sprintf("Power sim: nsim=%d, alpha=%.2f, theta=%.1f, sd_part=%.2f\n", nsim, alpha, theta, sd_participant))
cat("Rates (collisions/opportunity):\n"); print(rates)

results <- data.frame()
for (opp in opp_grid) for (N in N_grid) {
  P <- replicate(nsim, fit_pvals(simulate_one(N, opp)))   # 5 × nsim matrix (named rows)
  power <- rowMeans(P < alpha, na.rm = TRUE)
  conv  <- rowMeans(!is.na(P))
  results <- rbind(results, data.frame(
    N = N, opportunities = opp,
    power_interaction  = round(power["interaction"], 3),
    power_timing       = round(power["timing"], 3),
    power_localization = round(power["localization"], 3),
    power_pb_vs_none   = round(power["pb_vs_none"], 3),
    power_pb_vs_visual = round(power["pb_vs_visual"], 3),
    converged          = round(conv["interaction"], 3),
    row.names = NULL))
  cat(sprintf("  N=%2d opp=%2d  interaction power=%.2f (conv %.2f)\n",
              N, opp, power["interaction"], conv["interaction"]))
}

cat("\n===== POWER CURVE =====\n"); print(results, row.names = FALSE)
write.csv(results, "power_curve.csv", row.names = FALSE)
cat("\nwrote power_curve.csv\n")

# Smallest N reaching 80% power for the headline interaction, per opportunity count.
for (opp in opp_grid) {
  sub <- results[results$opportunities == opp & results$power_interaction >= 0.80, ]
  if (nrow(sub) > 0)
    cat(sprintf("→ opp=%d: N≈%d reaches 80%% power for the interaction.\n", opp, min(sub$N)))
  else
    cat(sprintf("→ opp=%d: no tested N reached 80%% power — raise N, #opportunities, or revisit effect sizes.\n", opp))
}

cat("\nReminder: replace the placeholder rates/theta with pilot estimates, then re-run to LOCK N.\n")
