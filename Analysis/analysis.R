# =============================================================================
# Primary + secondary analysis for
#   "When and Where to Warn: Predictive, Body-Localized Tactile Cues ..."
#
# Input: a per-block SUMMARY csv (one row per participant x block) exactly as written by the Unity
#   CsvFormatter / CsvFileWriter (columns: participant, block, condition, layout, opportunities,
#   collisions, collisions_per_opportunity, opportunities_hit, opportunities_avoided,
#   collisions_attributed, collisions_unattributed, near_misses, alerts, min_clearance_m,
#   duration_s, avoidance_count, mean_avoidance_latency_s).
#
# Run:  Rscript analysis.R path/to/summary.csv
#   (defaults to mock_block_summary.csv so you can validate the pipeline on simulated data first.)
#
# Install once: install.packages(c("glmmTMB","emmeans","lme4"))   # + optional: performance, ggplot2
# =============================================================================

suppressMessages({
  library(glmmTMB)   # negative-binomial GLMM
  library(emmeans)   # estimated marginal means / planned contrasts
})

args <- commandArgs(trailingOnly = TRUE)
csv  <- if (length(args) >= 1) args[[1]] else "mock_block_summary.csv"
cat("Reading:", csv, "\n")
d <- read.csv(csv, stringsAsFactors = FALSE)

d$condition   <- factor(d$condition, levels = c("None","RG","RB","PG","PB","Visual"))
d$participant <- factor(d$participant)
d$layout      <- factor(d$layout)

# ---- derive the 2x2 design factors (RG,RB,PG,PB cells only) -------------------
cells <- subset(d, condition %in% c("RG","RB","PG","PB"))
cells$Timing       <- factor(ifelse(cells$condition %in% c("PG","PB"), "Predictive", "Reactive"),
                             levels = c("Reactive","Predictive"))
cells$Localization <- factor(ifelse(cells$condition %in% c("RB","PB"), "BodyLocalized", "Generic"),
                             levels = c("Generic","BodyLocalized"))
cells$alerts_c     <- as.numeric(scale(cells$alerts, center = TRUE, scale = FALSE)) # centered covariate

# ---- PRIMARY: negative-binomial GLMM (collisions per opportunity) -------------
# collisions ~ Timing * Localization + alert covariate, offset = log(opportunities),
# random intercepts for participant AND layout.
m_primary <- glmmTMB(
  collisions ~ Timing * Localization + alerts_c +
    offset(log(pmax(opportunities, 1))) +
    (1 | participant) + (1 | layout),
  family = nbinom2, data = cells)

cat("\n===== PRIMARY: collisions ~ Timing*Localization (neg-binomial GLMM) =====\n")
print(summary(m_primary))

emm <- emmeans(m_primary, ~ Timing * Localization, type = "response")
cat("\n-- estimated marginal means (response scale) --\n");  print(emm)
cat("\n-- pairwise (PB-best check), Holm-adjusted --\n");    print(pairs(emm, adjust = "holm"))

# ---- FLOOR: each feedback condition vs None ----------------------------------
floor_df <- d
floor_df$cond <- relevel(droplevels(floor_df$condition), ref = "None")
m_floor <- glmmTMB(collisions ~ cond + offset(log(pmax(opportunities, 1))) +
                     (1 | participant) + (1 | layout), family = nbinom2, data = floor_df)
cat("\n===== FLOOR: each condition vs None (absolute reduction) =====\n")
print(summary(m_floor))

# ---- H4: PB vs Visual (best-practice) ----------------------------------------
pbvis <- subset(d, condition %in% c("PB","Visual"))
if (nrow(pbvis) > 0) {
  pbvis$cond <- relevel(droplevels(pbvis$condition), ref = "Visual")
  m_pbvis <- glmmTMB(collisions ~ cond + offset(log(pmax(opportunities, 1))) +
                       (1 | participant) + (1 | layout), family = nbinom2, data = pbvis)
  cat("\n===== H4: PB vs Visual =====\n");  print(summary(m_pbvis))
}

# ---- diagnostics (optional package) ------------------------------------------
if (requireNamespace("performance", quietly = TRUE)) {
  cat("\n===== overdispersion check =====\n")
  print(performance::check_overdispersion(m_primary))
}

# ---- interaction figure (optional package) -----------------------------------
if (requireNamespace("ggplot2", quietly = TRUE)) {
  library(ggplot2)
  agg <- aggregate(collisions_per_opportunity ~ Timing + Localization, cells, mean)
  p <- ggplot(agg, aes(Timing, collisions_per_opportunity,
                       group = Localization, color = Localization)) +
    geom_line() + geom_point(size = 3) +
    labs(y = "collisions per opportunity",
         title = "Timing x Localization (lower is better)") +
    theme_minimal()
  ggsave("interaction_plot.png", p, width = 6, height = 4, dpi = 150)
  cat("\nwrote interaction_plot.png\n")
}

# ---- SECONDARY measures: templates (fill in once presence/SSQ are merged) -----
# Presence (IPQ) / breaks-in-presence / NASA-TLX live in the questionnaire export, joined by
# participant+block. Once merged, e.g.:
#   library(lme4)
#   m_pres <- lmer(presence ~ Timing * Localization + (1|participant) + (1|layout), data = cells_q)
# SSQ with block-order covariate:
#   m_ssq  <- lmer(ssq_total ~ condition + block + (1|participant), data = d_q)

cat("\nDone.\n")
