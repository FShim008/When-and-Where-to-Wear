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

d$condition   <- factor(d$condition, levels = c("None","RG","RB","PG","PB","PBC"))
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

# ---- H4': PB vs PBC (cue form, trigger-matched) ------------------------------
pbvis <- subset(d, condition %in% c("PB","PBC"))
if (nrow(pbvis) > 0) {
  pbvis$cond <- relevel(droplevels(pbvis$condition), ref = "PBC")
  m_pbvis <- glmmTMB(collisions ~ cond + offset(log(pmax(opportunities, 1))) +
                       (1 | participant) + (1 | layout), family = nbinom2, data = pbvis)
  cat("\n===== H4': PB vs PBC (cue form) =====\n");  print(summary(m_pbvis))
}

# ---- MECHANISM: avoidance latency (headline secondary) ------------------------
# WHY THIS IS PROMOTED: collisions-per-opportunity says WHETHER the warning worked; avoidance latency (alert ->
# the limb starts retreating) says WHY. If predictive timing helps because it buys reaction time, that shows up
# here as a shorter latency for Predictive, and the two results corroborate each other. None is already excluded
# from `cells` (no alerts -> no latency defined).
if (requireNamespace("lme4", quietly = TRUE)) {
  suppressMessages(library(lme4))
  lat <- cells[is.finite(cells$mean_avoidance_latency_s), ]
  if (nrow(lat) > 0) {
    cat("\n===== MECHANISM: avoidance latency ~ Timing x Localization =====\n")
    print(summary(lmer(mean_avoidance_latency_s ~ Timing * Localization +
                         (1 | participant) + (1 | layout), data = lat)))
    cat("\n-- mean avoidance latency (s) by cell --\n")
    print(aggregate(mean_avoidance_latency_s ~ Timing + Localization, lat, mean))
  } else {
    cat("\n[avoidance latency] no finite values — skipping.\n")
  }
} else {
  cat("\n[avoidance latency] lme4 not installed — skipping.\n")
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

# ---- SECONDARY measures: presence (IPQ), workload (NASA-TLX), sickness (SSQ), acceptability (CUE) --
# Reads the long-format questionnaire export written by Unity's QuestionnaireLogWriter / SessionRunner
#   columns: participant, block, condition, instrument, kind, measure, value
# `kind` (added 2026-09-25) is "item" for a raw per-item response or "measure" for a scored subscale.
# Every model below reads SCORED rows only; the item rows exist so the scoring can be audited and the
# instrument re-scored later. Exports written before that date have no `kind` column and are treated as
# all-measure, which is what they were.
# Canonical schema: instrument in {IPQ, NASA_TLX, SSQ, CUE}; measures IPQ "presence", NASA_TLX "overall",
# SSQ "total" (block = -1 for session-level SSQ pre/post), CUE "acceptability" + "timely" (warning
# conditions only; absent for None by design). Default path: questionnaire.csv beside the summary;
# override with a 2nd arg. Skips gracefully until the data + lme4 are present.
qcsv <- if (length(args) >= 2) args[[2]] else file.path(dirname(csv), "questionnaire.csv")
if (file.exists(qcsv) && requireNamespace("lme4", quietly = TRUE)) {
  suppressMessages(library(lme4))
  q <- read.csv(qcsv, stringsAsFactors = FALSE)
  q$participant <- factor(q$participant)
  if (!"kind" %in% names(q)) q$kind <- "measure"   # pre-2026-09-25 export
  n_item <- sum(q$kind == "item")
  if (n_item > 0)
    cat(sprintf("[questionnaires] %d item-level rows present (not modelled here; they exist so the
"
                , n_item),
        "                 scoring can be audited and the instruments re-scored).
", sep = "")

  pick <- function(inst, meas) {
    # Scored rows only. An item id could otherwise collide with a measure name and be silently pooled.
    s <- subset(q, instrument == inst & measure == meas & kind == "measure",
                select = c(participant, block, value))
    if (nrow(s) == 0) return(NULL)
    names(s)[names(s) == "value"] <- meas
    s
  }
  fit <- function(df, formula, title) {
    if (is.null(df) || nrow(df) == 0) { cat("\n[", title, "] no rows merged — skipping.\n"); return(invisible()) }
    cat("\n=====", title, "=====\n"); print(summary(lmer(formula, data = df)))
  }

  # Presence (IPQ overall) and workload (NASA-TLX overall) on the 2x2 cells.
  pres <- pick("IPQ", "presence")
  if (!is.null(pres)) fit(merge(cells, pres, by = c("participant","block")),
                          presence ~ Timing * Localization + (1|participant) + (1|layout),
                          "PRESENCE (IPQ): Timing x Localization")
  tlx <- pick("NASA_TLX", "overall")
  if (!is.null(tlx)) fit(merge(cells, tlx, by = c("participant","block")),
                         overall ~ Timing * Localization + (1|participant) + (1|layout),
                         "NASA-TLX overall: Timing x Localization")
  # SSQ total across all conditions, with block-order covariate.
  ssq <- pick("SSQ", "total")
  if (!is.null(ssq)) fit(merge(d, ssq, by = c("participant","block")),
                         total ~ condition + block + (1|participant),
                         "SSQ total: condition + block order")

  # ---- WARNING ACCEPTABILITY (instrument "CUE") -------------------------------
  # Adoption argument: a warning that reduces collisions but is distrusted or resented will not be used.
  # All items are keyed so higher = better (ANNOYING is reverse-scored in Core/Questionnaire.cs).
  # Administered only for conditions that deliver a warning, so None is absent by design.
  acc <- pick("CUE", "acceptability")
  if (!is.null(acc)) fit(merge(cells, acc, by = c("participant","block")),
                         acceptability ~ Timing * Localization + (1|participant) + (1|layout),
                         "CUE acceptability: Timing x Localization")

  # SUBJECTIVE MANIPULATION CHECK for the Timing factor: predictive cues should be rated as arriving in time
  # more often than reactive ones. If TIMELY does not favour Predictive, the manipulation did not land
  # perceptually and the objective lead-time result needs re-examination before it is interpreted.
  tmly <- pick("CUE", "timely")
  if (!is.null(tmly)) {
    fit(merge(cells, tmly, by = c("participant","block")),
        timely ~ Timing * Localization + (1|participant) + (1|layout),
        "CUE timeliness (manipulation check): Timing x Localization")
    cat("\n-- mean perceived timeliness by Timing --\n")
    print(aggregate(timely ~ Timing, merge(cells, tmly, by = c("participant","block")), mean))
  }
} else {
  cat("\n[questionnaires] no", qcsv, "or lme4 not installed — skipping presence/SSQ/TLX.\n")
}

cat("\nDone.\n")
