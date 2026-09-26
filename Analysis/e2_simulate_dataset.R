# =============================================================================
# PAPER 2 (E2) - SYNTHETIC DATASET IN THE REAL CSV CONTRACT
#
# Run:  Rscript e2_simulate_dataset.R [n_participants] [out.csv]
#       Rscript e2_simulate_dataset.R 24 e2_trials_sim.csv
#
# WHY THIS EXISTS. e2_analysis.R is preregistration material, so it has to be proven to RUN before any
# participant is recruited - a confirmatory script that crashes on first contact with real data is a
# preregistration in name only. This emits all 26 columns of
# E2TrialOutcomeFormatter.HeaderLine (Core/E2/E2TrialOutcome.cs) with realistic values, including the
# messy ones: invalid trials, NA leads on go trials, delivery shortfall, and prediction error.
#
# It is a TEST FIXTURE, not evidence. Nothing produced here may appear in the paper.
# =============================================================================

args <- commandArgs(trailingOnly = TRUE)
N    <- if (length(args) >= 1) as.integer(args[1]) else 24L
OUT  <- if (length(args) >= 2) args[2] else "e2_trials_sim.csv"
set.seed(20260914)

# Ground truth - deliberately the same assumptions as e2_power_analysis.R section 1, so the analysis
# script can be checked for RECOVERY: it should return LT50 near 250 ms and a threshold SD near 60 ms.
TRUE_LT50 <- 0.25; TRUE_SLOPE <- 17.6; SD_THRESHOLD <- 0.060; SD_SLOPE <- 4.0
TEMPO_EFFECT <- 0.050; LAPSE <- 0.03
ASSIGNED <- c(0.08, 0.15, 0.25, 0.35, 0.50)
ACHIEVED <- c(0.056, 0.119, 0.206, 0.282, 0.401)   # delivery runs short by design (E2SessionPlan)
ACHIEVED_SD <- 0.015

TOTAL_TRIALS <- 360L
WARN_FRAC    <- 0.25
GO_CROSS_P   <- 0.85        # zero-warning anchor: most unwarned reaches hit the hazard
INVALID_P    <- 0.04        # tracking dropout / no movement onset
TOL          <- 0.05        # timing tolerance band (s)

LIMBS   <- c("RightHand", "LeftHand")
HAZARDS <- sprintf("H%02d", 1:12)

rows <- list()
for (pid in seq_len(N)) {
  thr_i   <- rnorm(1, TRUE_LT50, SD_THRESHOLD)
  slope_i <- max(4, rnorm(1, TRUE_SLOPE, SD_SLOPE))

  n_warn <- round(TOTAL_TRIALS * WARN_FRAC)
  is_warn <- c(rep(TRUE, n_warn), rep(FALSE, TOTAL_TRIALS - n_warn))
  is_warn <- is_warn[sample.int(TOTAL_TRIALS)]

  lvl <- rep(seq_along(ASSIGNED), length.out = n_warn)
  lvl <- lvl[sample.int(n_warn)]
  wi  <- 0L

  tempo <- rep(c("Normal", "Urgent"), length.out = TOTAL_TRIALS)
  tempo <- tempo[sample.int(TOTAL_TRIALS)]

  t_clock <- 0
  for (k in seq_len(TOTAL_TRIALS)) {
    t_clock <- t_clock + runif(1, 4, 7)          # trial pacing on the block clock
    urgent  <- as.integer(tempo[k] == "Urgent")
    speed   <- rnorm(1, if (urgent) 1.35 else 0.95, 0.15)   # pre-cue closing speed, m/s

    valid <- runif(1) > INVALID_P
    reason <- ""
    assigned <- realized <- cmd <- onset <- dist <- pred <- actual <- NA_real_
    dev <- 0L; crossed <- 0L

    # Tempo manipulation: urgent trials ask for a shorter window and are reached faster. The window is what
    # E2Params.WindowFor() returns; movement time is the participant's response to it.
    window  <- if (urgent) 0.70 else 1.20
    win_min <- if (urgent) 0.00 else 0.85   # Normal is a BAND: too fast is a miss too
    mv_time <- rnorm(1, if (urgent) 0.62 else 0.95, 0.12)
    mv_onset <- t_clock + runif(1, 0.15, 0.35)   # reaction time before the reach begins

    if (!valid) {
      reason  <- sample(c("no_movement_onset", "hazard_id_not_in_scene"), 1, prob = c(.9, .1))
      crossed <- 0L
      minclr  <- NA_real_; maxpen <- NA_real_; travel <- NA_real_
      ronset  <- NA_real_; arrest <- NA_real_; reached <- 0L
    } else if (is_warn[k]) {
      wi <- wi + 1L
      j  <- lvl[wi]
      assigned <- ASSIGNED[j]
      realized <- max(0.01, rnorm(1, ACHIEVED[j], ACHIEVED_SD))
      # ~2% of warning trials never become cue-eligible (reach too slow or off-axis)
      if (runif(1) < 0.02) {
        valid <- FALSE; reason <- "cue_never_eligible"
        realized <- assigned <- NA_real_
        minclr <- NA_real_; maxpen <- NA_real_; travel <- NA_real_
        ronset <- NA_real_; arrest <- NA_real_; reached <- 0L
      } else {
        cmd   <- t_clock
        onset <- cmd + 0.058                       # measured pipeline latency
        dist  <- realized * speed
        pred  <- onset + realized
        p_avoid <- (1 - LAPSE) / (1 + exp(-slope_i * (realized - (thr_i + TEMPO_EFFECT * urgent))))
        crossed <- as.integer(runif(1) > p_avoid)
        dev   <- as.integer(abs(realized - assigned) > TOL)
        # constant-velocity counterfactual error: the prediction runs slightly LATE because reaches
        # decelerate near the target. Only defined when contact actually happened.
        actual  <- if (crossed == 1) pred - rnorm(1, 0.025, 0.030) else NA_real_
        minclr  <- if (crossed == 1) 0 else abs(rnorm(1, 0.06, 0.03))
        maxpen  <- if (crossed == 1) abs(rnorm(1, 0.04, 0.02)) else 0
        travel  <- max(0, dist - minclr)
        ronset  <- onset + rnorm(1, 0.18, 0.05)
        arrest  <- rnorm(1, 0.30, 0.08)
        reached <- as.integer(runif(1) < 0.8)
      }
    } else {
      crossed <- as.integer(runif(1) < GO_CROSS_P)   # the zero-warning anchor
      actual  <- if (crossed == 1) t_clock + runif(1, 0.4, 0.9) else NA_real_
      minclr  <- if (crossed == 1) 0 else abs(rnorm(1, 0.05, 0.03))
      maxpen  <- if (crossed == 1) abs(rnorm(1, 0.05, 0.02)) else 0
      travel  <- NA_real_; ronset <- NA_real_; arrest <- NA_real_
      reached <- as.integer(runif(1) < 0.9)
    }

    rows[[length(rows) + 1L]] <- data.frame(
      participant = pid, session = 1L,
      trial_id = sprintf("T%04d", k),
      target_limb = sample(LIMBS, 1), hazard = sample(HAZARDS, 1),
      tempo = tempo[k], warning_trial = as.integer(is_warn[k]),
      valid = as.integer(valid), invalid_reason = reason,
      crossed = crossed,
      assigned_lead_s = assigned, realized_lead_s = realized, timing_deviation = dev,
      command_time_s = cmd, physical_onset_s = onset,
      precue_speed_ms = if (is_warn[k] && valid) speed else NA_real_,
      distance_at_cue_m = dist,
      predicted_contact_s = pred, actual_contact_s = actual,
      prediction_error_s = if (is.na(pred) || is.na(actual)) NA_real_ else pred - actual,
      min_clearance_m = minclr, max_penetration_m = maxpen, post_cue_travel_m = travel,
      response_onset_s = ronset, arrest_s = arrest, reached_target = reached,
      # Movement time exists only when the target was actually reached — every successful stop has none.
      movement_onset_s  = if (valid) mv_onset else NA_real_,
      target_contact_s  = if (valid && reached == 1) mv_onset + mv_time else NA_real_,
      movement_time_s   = if (valid && reached == 1) mv_time else NA_real_,
      response_window_s     = window,
      response_window_min_s = win_min,
      within_window = as.integer(valid && reached == 1 && mv_time <= window && mv_time >= win_min),
      stringsAsFactors = FALSE)
  }
}

d <- do.call(rbind, rows)
num <- sapply(d, is.numeric)
d[num] <- lapply(d[num], function(v) ifelse(is.finite(v), round(v, 4), NA))
write.csv(d, OUT, row.names = FALSE, na = "NA")

cat(sprintf("Wrote %s: %d rows, %d participants, %d warning trials.\n",
            OUT, nrow(d), N, sum(d$warning_trial == 1)))
cat(sprintf("Ground truth for recovery checks: LT50 = %.0f ms, threshold SD = %.0f ms, tempo = +%.0f ms.\n",
            1000 * TRUE_LT50, 1000 * SD_THRESHOLD, 1000 * TEMPO_EFFECT))
# Two conventions, differing by the lapse ceiling. e2_analysis.R reports (a).
cat(sprintf("Implied LT80 = %.0f ms (a: absolute 80%% avoidance, the reported operating point)\n",
            1000 * (TRUE_LT50 + qlogis(0.80 / (1 - LAPSE)) / TRUE_SLOPE)))
cat(sprintf("             = %.0f ms (b: lapse-free latent 80%%)\n",
            1000 * (TRUE_LT50 + qlogis(0.80) / TRUE_SLOPE)))
cat(sprintf("Lapse rate in the generated data: %.0f%%. AIC should recover it.\n", 100 * LAPSE))
