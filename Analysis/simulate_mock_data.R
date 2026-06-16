# =============================================================================
# Simulate a mock per-block SUMMARY csv that matches the Unity CsvFormatter schema EXACTLY, so the
# analysis pipeline (analysis.R) can be validated BEFORE any real data exists  [checklist 5.6].
# The baseline rates below are illustrative placeholders, NOT a hypothesis.
#
# Run:  Rscript simulate_mock_data.R    ->  writes mock_block_summary.csv
# =============================================================================

set.seed(42)
N          <- 48
conditions <- c("None","RG","RB","PG","PB","Visual")
layouts    <- c("L1","L2","L3","L4","L5","L6")

# illustrative collision-per-opportunity baselines by condition
base <- c(None = 0.45, RG = 0.30, RB = 0.22, PG = 0.20, PB = 0.12, Visual = 0.18)

rows <- vector("list", N * length(conditions))
k <- 1
for (p in 0:(N - 1)) {
  for (b in 0:5) {
    cond <- conditions[b + 1]                # (mock only; real order comes from SessionPlan/Williams)
    opp  <- 12L
    rate <- base[[cond]] * runif(1, 0.7, 1.3)
    coll <- rpois(1, rate * opp)
    hit  <- min(coll, opp)
    alerts <- if (cond == "None") 0L else rpois(1, 14)
    avoid  <- if (cond == "None") 0L else rpois(1, 8)

    rows[[k]] <- data.frame(
      participant                = p,
      block                      = b,
      condition                  = cond,
      layout                     = layouts[((b + p) %% 6) + 1],
      opportunities              = opp,
      collisions                 = coll,
      collisions_per_opportunity = round(coll / opp, 4),
      opportunities_hit          = hit,
      opportunities_avoided      = opp - hit,
      collisions_attributed      = coll,
      collisions_unattributed    = 0L,
      near_misses                = rpois(1, 5),
      alerts                     = alerts,
      min_clearance_m            = round(runif(1, 0.0, 0.1), 4),
      duration_s                 = 180.0,
      avoidance_count            = avoid,
      mean_avoidance_latency_s   = round(runif(1, 0.2, 0.6), 3),
      stringsAsFactors           = FALSE)
    k <- k + 1
  }
}

out <- do.call(rbind, rows)
write.csv(out, "mock_block_summary.csv", row.names = FALSE)
cat("wrote mock_block_summary.csv with", nrow(out), "rows\n")
