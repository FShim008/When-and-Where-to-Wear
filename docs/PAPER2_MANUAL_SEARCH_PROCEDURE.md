# Paper 2 — Manual ACM DL / IEEE Xplore Search

**Print or keep open. Fill in the counts as you go — they are required by `PAPER1_STUDY_DESIGN.md` §1 step 2.**

**Why this pass exists:** programmatic access is blocked (ACM 403, IEEE 418) and the OpenAlex publisher-filter
substitute **failed its positive controls** — it returned 0 hits for a paper known to be in the ACM DL. The
automated record is in `PAPER2_STUDY_DESIGN.md` §1. This is the part a machine could not do.

**You do not need full-text access.** Titles and abstracts are free on both platforms without a login, and
abstracts are enough to screen. Only fetch full text for the handful that survive.

---

## The screening rule — apply it identically every time

A hit **disconfirms** the novelty claim only if it has **all four**:

| | Criterion |
|---|---|
| **a** | A warning or cue delivered at a **controlled time before a predicted contact** |
| **b** | The effector is a **limb already in motion** — not a pedal, button, or whole-body locomotion |
| **c** | The outcome is **avoidance success** |
| **d** | A **threshold or curve is fitted across lead times** — not 2–3 levels compared |

**Three or fewer = a citation, not a threat.** Record it in the "cite" column and move on. Both prior close
calls failed on (d): Zhang & Wu had two levels; Bajpai compared modalities, not timings.

---

## Part 1 — ACM Digital Library (~45 min)

Go to **`dl.acm.org/search/advanced`**. Type queries directly into the search box; ACM uses `Field:(terms)`
with `AND` / `OR` / `NOT` and quoted phrases.

| # | Query — paste verbatim | Count | Screened | Any disconfirm? |
|---|---|---|---|---|
| A1 | `Abstract:("warning lead time")` | | | |
| A2 | `Abstract:("lead time" AND (collision OR obstacle))` | | | |
| A3 | `Abstract:("time to collision" AND (warning OR cue OR alert))` | | | |
| A4 | `Abstract:((haptic OR vibrotactile OR tactile) AND warning AND timing)` | | | |
| A5 | `Abstract:(warning AND timing AND (reach OR reaching OR arm OR limb OR hand))` | | | |
| A6 | `Abstract:(collision AND warning AND "virtual reality" AND (haptic OR vibrotactile))` | | | |
| A7 | `Abstract:(psychometric AND (warning OR alert OR cue))` | | | |
| A8 | `Abstract:("how early" OR "advance warning" OR earliness) AND Abstract:(collision OR obstacle)` | | | |

**Tips:** set date range **2010–present** if a query returns more than ~200 (older work will not have the
apparatus). Sort by **Relevance**, then re-sort by **Most Recent** and scan the first two pages again — the
rankings differ and each surfaces different things.

---

## Part 2 — IEEE Xplore (~45 min)

Go to **`ieeexplore.ieee.org/search/advanced`** and pick the **Command Search** tab. IEEE syntax is
`("Field":terms)` with `AND` / `OR` / `NOT` and `NEAR/n`.

| # | Query — paste verbatim | Count | Screened | Any disconfirm? |
|---|---|---|---|---|
| I1 | `("Abstract":"warning lead time")` | | | |
| I2 | `("Abstract":"lead time") AND ("Abstract":collision OR "Abstract":obstacle)` | | | |
| I3 | `("Abstract":"time to collision") AND ("Abstract":warning OR "Abstract":cue)` | | | |
| I4 | `("Abstract":haptic OR "Abstract":vibrotactile) AND ("Abstract":warning) AND ("Abstract":timing)` | | | |
| I5 | `("Abstract":warning NEAR/5 timing) AND ("Abstract":reach OR "Abstract":limb OR "Abstract":arm)` | | | |
| I6 | `("Abstract":"virtual reality") AND ("Abstract":collision) AND ("Abstract":warning OR "Abstract":alert)` | | | |
| I7 | `("Abstract":psychometric OR "Abstract":psychophysical) AND ("Abstract":warning OR "Abstract":alarm)` | | | |
| I8 | `("All Metadata":"warning time") AND ("All Metadata":avoidance)` | | | |

**`NEAR/5` is the reason to use Command Search** — proximity operators are not available in the basic box and
they cut noise sharply on I5.

---

## Part 3 — Venue browse (~2 hours) — **this is the part that actually works**

**Bajpai et al. (2020) was invisible to ten keyword queries and appeared only on venue inspection.** Assume
others are hiding the same way. This section matters more than Parts 1 and 2 combined.

Open each venue's proceedings and **read titles only**, last **6 years**. You are scanning for anything about
warning timing, collision avoidance, proximity alerts, safety cues, or motor interruption.

| Venue | Where | Years | Done |
|---|---|---|---|
| **IEEE ISMAR** | Xplore → Browse Conferences | 2019–2026 | ☐ |
| **IEEE VR / VR&3DUI** | Xplore → Browse Conferences | 2019–2026 | ☐ |
| **IEEE TVCG** (VR/ISMAR special issues) | Xplore → Browse Journals | 2019–2026 | ☐ |
| **IEEE Trans. Haptics** | Xplore → Browse Journals | 2019–2026 | ☐ |
| **ACM CHI** | ACM DL → Proceedings | 2019–2026 | ☐ |
| **ACM UIST** | ACM DL → Proceedings | 2019–2026 | ☐ |
| **ACM SUI** (Spatial User Interaction) | ACM DL → Proceedings | 2019–2026 | ☐ |
| **IEEE World Haptics / Haptics Symposium** | Xplore → Browse Conferences | 2019–2026 | ☐ |

**Shortcut that works:** on each proceedings page use **Ctrl+F** for `warn`, `collision`, `obstacle`,
`proximity`, `safety`, `timing`, `lead`. Catches most of it in a couple of minutes per year.

---

## Part 4 — Citation chase on the three known near-neighbours (~30 min)

For each, open it in ACM DL or Xplore and click **"Cited By"** and scan the reference list:

- **Bajpai et al. (2020)**, *IEEE Trans. Haptics* 13(1):32–37 — closest in XR haptics
- **Zhang & Wu (2018)**, HFES 62 — closest lead-time manipulation
- **Abdlkarim et al. (2026)**, *Belt and Whistles*, CHI — closest apparatus

Anyone citing all three, or citing Bajpai in a VR-safety context, is worth a full read.

---

## Recording — required, and it takes two minutes

Append to `docs/PAPER2_NOVELTY_SEARCH_LOG_manual.md`:

```
Date run:            YYYY-MM-DD
Searcher:            [name]
Platforms:           ACM DL (dl.acm.org), IEEE Xplore (ieeexplore.ieee.org)
Access:              [institutional / abstract-only]
Date filter applied: [e.g. 2010-present on A2, A5]

Counts:  A1..A8 = __ __ __ __ __ __ __ __
         I1..I8 = __ __ __ __ __ __ __ __

Venues browsed:      [list, with year ranges]
Titles screened:     ~___
Full abstracts read: ___
Full texts read:     ___

Disconfirming hits:  [none / list]
Cite-worthy hits:    [list with the criterion each fails]
```

Then update `PAPER2_STUDY_DESIGN.md` §1 — mark the ACM/IEEE item complete with the date, and either confirm
the frozen novelty statement or, if something disconfirms it, apply §1 step 5: **reframe rather than argue.**

---

## Two things to keep in mind while doing it

**Expect near-misses, and welcome them.** Every close paper you find and can differentiate makes the related
work stronger. A paper with an empty related-work section reads as an unsearched one.

**Watch for the terminology trap.** "Warning lead time" is owned by tornado, flood and tsunami forecasting —
Q1 in the automated pass returned 346 hits, all natural-hazard. If an ACM/IEEE query returns a suspiciously
large count, check you have not caught the disaster-warning literature. It also means the eventual manuscript
should define the term on first use.

**Honest expectation:** you will most likely find **nothing disconfirming and two or three worth citing.**
That is the good outcome, and it is only worth anything because it was looked for properly.
