# =============================================================================
# PAPER 1 — SYNTHETIC opportunities.csv IN THE REAL CONTRACT
#
# Run:  Rscript paper1_simulate_opportunities.R [n_participants] [out.csv]
#
# WHY: paper1_analysis.R is preregistration material, so it must be proven to RUN
# before any participant is recruited. This emits all 26 columns of
# OpportunityOutcomeFormatter.HeaderLine with realistic messiness - unpresented
# opportunities, aborted windows, and a few inverted-timing rows.
#
# TEST FIXTURE. Nothing produced here may appear in the paper.
# =============================================================================

args <- commandArgs(trailingOnly = TRUE)
N    <- if (length(args) >= 1) as.integer(args[1]) else 36L   # decided 2026-09-25: 36 x 24
OUT  <- if (length(args) >= 2) args[2] else "p1_opportunities_sim.csv"
SEED <- if (length(args) >= 3) as.integer(args[3]) else 20260921L   # vary it for recovery checks
set.seed(SEED)

CONDITIONS <- c("None", "RG", "RB", "PG", "PB", "PBC")   # PBC replaced Visual 2026-09-23 (H4')
LAYOUTS    <- c("L1", "L2", "L3")
LIMBS      <- c("LeftHand", "RightHand", "LeftFoot", "RightFoot")
# Decided 2026-09-25: 24 per block, 36 analysable participants [PAPER1_STUDY_DESIGN §4].
# 36 because §7 requires the complete 6x6 crossing of condition-order and layout-order rows, so a full
# allocation is a multiple of 36. NOTE the authored schedules still supply 12; twelve more events must
# be authored (once - LayoutVariants derives L2-L6 from L1). The fixture uses the TARGET so the analysis
# is exercised at the planned size.
OPPS_PER_BLOCK <- 24L

# Ground truth, on the log-odds scale. Base = RG (proximity + generic).
BASE_LOGIT    <- 0.20     # ~55% violation with a proximity generic cue
EFF_POLICY    <- -0.70    # predictive helps
EFF_MAPPING   <- -0.35    # localized helps
EFF_INTERACT  <- +0.30    # redundancy: localization helps LESS under prediction
EFF_NONE      <- +0.90    # no feedback is worse than any cue
EFF_PBC       <- +0.40    # H4': continuous mapping worse than discrete, same trigger and site.
                          # Sign follows Valkov & Linsen (IEEE VR 2019) - the modulable cue is worse.
SD_PARTICIPANT <- 0.55
SD_LAYOUT      <- 0.20

NOT_PRESENTED <- 0.03
ABORTED       <- 0.04
INVERTED      <- 0.02     # opportunities where proximity fired first despite T = 1.0 s

# The FROZEN policy parameters [PAPER1_STUDY_DESIGN §6, 2026-09-14]. The counterfactual
# trigger columns are generated FROM these, so paper1_analysis.R §3B is exercised against
# a fixture that obeys the same physics the engine does.
D_PARAM <- 0.30           # proximity trigger distance (m)
T_PARAM <- 1.00           # predictive TTC threshold (s)

layout_re <- setNames(rnorm(length(LAYOUTS), 0, SD_LAYOUT), LAYOUTS)

rows <- list()
for (pid in seq_len(N)) {
  p_re <- rnorm(1, 0, SD_PARTICIPANT)
  order_conds <- sample(CONDITIONS)                      # counterbalanced in the real study

  for (b in seq_along(order_conds)) {
    cond <- order_conds[b]
    lay  <- LAYOUTS[((pid + b) %% length(LAYOUTS)) + 1]

    eta <- BASE_LOGIT + p_re + layout_re[[lay]]
    if (cond %in% c("PG", "PB"))     eta <- eta + EFF_POLICY
    if (cond %in% c("RB", "PB"))     eta <- eta + EFF_MAPPING
    if (cond == "PB")                eta <- eta + EFF_INTERACT
    if (cond == "None")              eta <- eta + EFF_NONE
    if (cond == "PBC")               eta <- eta + EFF_POLICY + EFF_MAPPING + EFF_INTERACT + EFF_PBC

    t0 <- 0
    for (k in seq_len(OPPS_PER_BLOCK)) {
      t0 <- t0 + runif(1, 8, 14)
      limb <- sample(LIMBS, 1)
      onset <- t0; close <- t0 + runif(1, 2.0, 3.5)

      presented <- runif(1) > NOT_PRESENTED
      valid     <- presented && runif(1) > ABORTED
      reason    <- if (!presented) "not_presented" else if (!valid) "window_not_completed" else ""

      speed_prox <- rnorm(1, 1.05, 0.22)
      approach   <- runif(1, 0.42, 0.75)

      if (valid) {
        viol <- rbinom(1, 1, plogis(eta))
        episodes <- if (viol == 1) 1L + rbinom(1, 2, 0.2) else 0L
        firstentry <- if (viol == 1) onset + runif(1, 0.4, 1.6) else NA_real_
        minclr <- if (viol == 1) 0 else abs(rnorm(1, 0.09, 0.05))
        maxpen <- if (viol == 1) abs(rnorm(1, 0.05, 0.03)) else 0
        unattr <- rbinom(1, 1, 0.06)

        # Counterfactual policy timings, as LEAD TIMES BEFORE CONTACT (the CSV contract).
        # These must honour the frozen policy parameters or the §3B sweep cannot be tested:
        #   proximity fires at distance D  -> lead = D / v   (+ sampling jitter)
        #   predictive fires when TTC = T  -> lead = T       (+ oracle error)
        # A few opportunities invert, as they do in the real data.
        v    <- max(0.25, speed_prox)
        prox <- max(0.05, D_PARAM / v + rnorm(1, 0, 0.015))
        pred <- if (runif(1) < INVERTED) max(0.02, prox - abs(rnorm(1, 0.04, 0.02)))
                else max(0.05, T_PARAM + rnorm(1, 0, 0.06))
        lead <- pred - prox
        deliv <- switch(cond, "None" = NA_real_, "RG" = prox, "RB" = prox,
                        "PG" = pred, "PB" = pred, "PBC" = pred)
      } else {
        viol <- 0L; episodes <- 0L; firstentry <- NA_real_
        minclr <- NA_real_; maxpen <- NA_real_; unattr <- 0L
        prox <- NA_real_; pred <- NA_real_; lead <- NA_real_; deliv <- NA_real_
      }

      rows[[length(rows) + 1L]] <- data.frame(
        participant = pid, block = b, condition = cond, layout = lay,
        opportunity_id = sprintf("P%02d-B%d-O%02d", pid, b, k),
        target_limb = limb, target_obstacle = sprintf("O%d", sample(1:3, 1)),
        planned_onset_s = round(onset, 4), planned_close_s = round(close, 4),
        presented = as.integer(presented), valid = as.integer(valid), invalid_reason = reason,
        violation = as.integer(viol), entry_episodes = episodes,
        first_entry_s = firstentry, min_clearance_m = minclr, max_penetration_m = maxpen,
        unattributed_contacts = unattr,
        trigger_prox_s = prox, trigger_pred_s = pred, policy_lead_s = lead,
        timing_inverted = if (is.na(lead)) NA_integer_ else as.integer(lead < 0),
        delivered_trigger_s = deliv,
        approach_at_onset_m = approach,
        closing_speed_prox_ms = speed_prox,
        closing_speed_pred_ms = speed_prox * runif(1, 0.75, 0.95),
        stringsAsFactors = FALSE)
    }
  }
}

d <- do.call(rbind, rows)
num <- sapply(d, is.numeric)
d[num] <- lapply(d[num], function(v) ifelse(is.finite(v), round(v, 4), NA))
write.csv(d, OUT, row.names = FALSE, na = "NA")

cat(sprintf("Wrote %s: %d rows, %d participants, %d conditions.\n",
            OUT, nrow(d), N, length(CONDITIONS)))
cat("Ground truth (log-odds):\n")
cat(sprintf("  H1 policy   = %+.2f  (OR %.2f)\n", EFF_POLICY, exp(EFF_POLICY)))
cat(sprintf("  H2 mapping  = %+.2f  (OR %.2f)\n", EFF_MAPPING, exp(EFF_MAPPING)))
cat(sprintf("  H3 interact = %+.2f  (OR %.2f)\n", EFF_INTERACT, exp(EFF_INTERACT)))
cat(sprintf("  H4' cueform = %+.2f  (OR %.2f)   PBC vs PB\n", EFF_PBC, exp(EFF_PBC)))
cat(sprintf("  inverted-timing rate = %.0f%%\n", 100 * INVERTED))
