# Analysis pipeline

R scripts for the primary + secondary analysis. They consume the **per-block summary CSV** produced by
the Unity pipeline (`CsvFileWriter` / `CsvFormatter`) — the same schema the `ExperimentRunner` demo and
the real study both emit.

## Files
- **`simulate_mock_data.R`** — writes `mock_block_summary.csv` (48 participants × 6 conditions) matching
  the CSV schema exactly, so the analysis can be validated before real data exists (checklist 5.6).
- **`analysis.R`** — the negative-binomial GLMM (primary DV = collisions, offset = log(opportunities),
  Timing × Localization, alert covariate, participant + layout random effects), the floor contrast vs
  None, the PB-vs-Visual contrast, diagnostics, and the interaction figure.

## Run
```sh
# one-time:
#   install.packages(c("glmmTMB","emmeans","lme4"))   # optional: performance, ggplot2

Rscript simulate_mock_data.R            # -> mock_block_summary.csv
Rscript analysis.R mock_block_summary.csv
```

On real data, point `analysis.R` at the summary CSV the study wrote:
```sh
Rscript analysis.R path/to/real_block_summary.csv
```

## Notes
- Decimals are written invariant-culture (`.`) by the Unity formatter, so `read.csv` is locale-safe.
- `min_clearance_m` and `mean_avoidance_latency_s` may be `NA` (blocks with no engagement / no avoidance).
- Presence (IPQ) / SSQ / NASA-TLX come from the questionnaire export and are joined by `participant`+`block`;
  `analysis.R` has commented LMM templates for them.
- The `Timing`/`Localization` factors are derived from `condition` inside `analysis.R` (RG/RB/PG/PB cells);
  `None` and `Visual` are handled as separate reference contrasts.
