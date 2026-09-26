# Analysis pipeline

**Paper 1** scripts consume the per-block summary CSV (`CsvFileWriter` / `CsvFormatter`).
**Paper 2 (E2)** scripts consume the per-trial CSV (`E2TrialOutcomeFormatter`, 26 columns).

R 4.6.1 + `lme4` are installed on this machine. If `Rscript` is not on PATH:
`"C:\Program Files\R\R-4.6.1\bin\Rscript.exe"`.

---

## Paper 2 — E2 (`e2_*.R`)

| Script | What it does |
|---|---|
| `e2_power_analysis.R` | Sample size. Targets **LT80 interval width** (H1 is estimation, not a test) plus power for H3a. Writes `e2_power_curve.csv`, `e2_power_sensitivity.csv`, `e2_power_tempo_sensitivity.csv`. |
| `e2_analysis.R` | **The confirmatory analysis — preregistration material.** Flow diagram, §8 diagnostics, lapse-aware hierarchical psychometric fit, LT50/LT80 with bootstrap intervals, threshold SD, split-half reliability, H3a. |
| `e2_simulate_dataset.R` | Synthetic dataset in the real 26-column contract, from known ground truth. A test fixture — **nothing it produces may appear in the paper.** |

```sh
Rscript e2_power_analysis.R 25                    # quick look (~3 min); omit arg for nsim=400 (~1.5 h)
Rscript e2_simulate_dataset.R 24 e2_trials_sim.csv
Rscript e2_analysis.R e2_trials_sim.csv 200       # dry run; real data: point at sessions/E2_Pxxx/e2_trials.csv
```

### Read this before trusting a number

- **The N grid does not justify N=24.** Under the assumed truth both H1 and H3a clear at N=12. N must be
  argued from the SD_THRESHOLD sensitivity table (§6) and the tempo MDE table (§7), not from the grid.
- **The fitted threshold SD runs ~18% low** (partial pooling: 49 ms recovered from 60 ms). Inflate the
  pilot's SD before reading it into the N table, or N comes out too small.
- **Bootstrap intervals are 1.03–1.07× the delta-method width** (measured, not assumed). The power script
  uses the delta method; inflate its widths ~7%.
- **Synthetic outputs are deleted after each dry run on purpose.** They are indistinguishable from real
  results at a glance. Regenerate with the two commands above whenever you need them.

---

## Paper 1

| Script | Role |
|---|---|
| **`paper1_analysis.R`** | ⭐ **PRIMARY.** Binomial mixed model on `opportunities.csv` — per-opportunity binary violation, denominator = presented & valid. Gate-27 manipulation check, **§3B parameter sweep**, §3 interpretation order, floor and H4 contrasts, **§6B realized-lead dose-response**, alert burden. |

### ⚠ The `C:\cc` dotnet harness has a blind spot — verify in Unity before trusting a test

Found 2026-09-23. `ContinuousCueTests` compiled and passed in the harness but **failed to compile in
Unity**: `Is.GreaterThanOrEqualTo(x).Within(tol)` exists in the NuGet NUnit the harness uses and **not** in
the NUnit Unity bundles. The harness also covers **Core + Tests only** — `Runtime` and `Integration` are
never compiled there, so a break in either is invisible to it.

Run the real thing before believing a green harness:

```sh
"/c/Program Files/Unity/Hub/Editor/6000.3.16f1/Editor/Unity.exe" \
  -runTests -batchmode -nographics -projectPath "<project>" \
  -testPlatform EditMode -testResults results.xml -logFile unity.log
```

Unity must be **closed** or it cannot lock the project. Last run: **239 tests, 238 passed, 0 failed,
1 skipped, 0 compile errors.**

### §3B and §6B — the parameter defense

Added 2026-09-23 to answer *"where did `D` = 0.30 m and `T` = 1.0 s come from?"*. Full
argument, paste-ready paper text and the verified bibliography:
**`docs/PAPER1_PARAMETER_JUSTIFICATION.md`**.

- **§3B** reconstructs both policies' trigger times across `D` × `T` from the counterfactual
  probe. Writes `p1_param_sensitivity.csv`. It validates the constant-speed reconstruction
  against the recorded triggers *before* sweeping, and downgrades itself to "indicative
  only" if the median error exceeds 25%.
- **§6B** replaces the categorical `Policy` factor with the lead time actually delivered.
  Writes `p1_lead_dose_response.csv`.

**Two things not to overclaim** — checklist items 6 and 7 print these at every run:
§3B is robustness of the **manipulation**, not generality of the **effect size**; nobody
experienced the other settings. §6B is **observational and speed-confounded by
construction** (`lead = D/v`), so it restates §4 in parameter-free terms rather than
replacing it, and it never predicts outside the observed lead range.

> `paper1_simulate_opportunities.R` was corrected the same day: it generated `trigger_prox_s`
> from an unrelated draw that overwrote the physical calculation, so the fixture did not obey
> `D`, `T` or closing speed and could not have tested §3B. It now derives both counterfactual
> triggers from the frozen parameters. Reconstruction error on the fixture: **10 ms (3.3%)**.
| `paper1_simulate_opportunities.R` | Fixture in the real 26-column contract. Test only. |
| `analysis.R` | **SENSITIVITY only.** Negative-binomial GLMM on block counts. Was the primary under the old design; the unit and denominator both changed. |
| `power_analysis.R` | ✅ **Rewritten for the binomial model; H4′ added 2026-09-23.** nsim=200: **H3 reaches 80% only at N=80 × 24 opps**; at N=36 × 12 it is **32%** — which is why H3 was made exploratory. First nsim=120 cell with H4′: **N=36 × 12 → H1 99%, H2 68%, H3 30%, H4′ 79%**. Writes `p1_power_curve.csv`, `p1_power_h3_mde.csv`. |

### The hypothesis structure changed on 2026-09-23

| | Hypotheses | Error control |
|---|---|---|
| **Confirmatory** | H1 policy, H2 mapping, **H4′ cue form (PB vs PBC)** | Holm across the three (§6A) |
| **Validity gate** | H5 feedback vs None | Not in the family — it is a precondition |
| **Exploratory** | H3 interaction | Interval + MDE only; **no decision attaches** |

**H3 no longer gates H1 and H2.** It has 32% power; letting it govern two well-powered tests meant a coin
flip decided the paper's narrative. §5 of `paper1_analysis.R` prints this warning at every run.

**`Visual` was replaced by `PBC`** in the scheduled conditions. H4 (modality) became **H4′ (cue form)**:
PBC shares PB's trigger, site, information and onset, and differs only in delivering a *continuous,
modulable* intensity instead of one discrete pulse. This answers Valkov & Linsen (IEEE VR 2019), who found
forecast triggering produced *more* collisions but confounded the trigger with exactly such a mapping. See
`docs/PAPER1_PARAMETER_JUSTIFICATION.md` §5B and `docs/PAPER1_RELATED_WORK.md` §2.4.

> **Do not fold PBC into the 2×2.** It is not a level of Policy or Mapping — it is PB with a different cue
> form. It is also excluded from the §6B lead dose-response, or the lead coefficient would absorb the H4′
> effect. Both exclusions are enforced in the script.

```sh
Rscript paper1_simulate_opportunities.R 36 sim.csv
Rscript paper1_analysis.R sim.csv
```

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
