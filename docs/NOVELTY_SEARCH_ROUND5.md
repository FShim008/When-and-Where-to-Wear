# Novelty re-check — round 5 (both papers)

**Run 2026-09-23.** Targeted at the **five surviving novelty claims**, via Crossref + OpenAlex
(ACM DL, IEEE Xplore, DBLP and Semantic Scholar are all unreachable from this environment).

---

## Why this round exists, and what it can and cannot do

Paper 1's novelty claim was *"no prior work compares distance-triggered and forecast-triggered cues in VR."*
It survived a title-and-abstract sweep and **died on the first PDF.** Valkov & Linsen (IEEE VR 2019) is
titled *"Vibro-tactile Feedback for Real-world Awareness in Immersive Virtual Environments"* — nothing in
the title or abstract mentions trigger policy. The comparison was an internal factor, visible only in
§3.2.2 of the paper.

**A title/abstract sweep cannot refute a claim about what a study compared internally.** No amount of
re-running one will.

So this round does **not** attempt to prove the claims. It produces a **ranked full-text reading list**:
papers close enough that they could plausibly contain the comparison, ordered so the most dangerous are
read first. Verification happens when the PDFs are read, not here.

---

## Verdicts

| Claim | Where | Verdict |
|---|---|---|
| **C1** — no prior work crosses trigger policy with somatotopic mapping | P1 §2.5 | ✅ **HOLDS** — Meng et al. 2014 read 2026-09-24; timing was a constant, not a factor |
| **C2** — no published study reports how ISO 13855's `K` = 2000 mm/s was measured | P2 §2 | ⚠️ **1 paper must be read** |
| **C3/C4** — first psychometric estimate of warning lead time to arrest a reach | P2 abstract, §210 | ✅ **No refutation found**; one literature must be *addressed*, not cited away |
| **C5** — no prior work logs counterfactual trigger times | P2 §1114 | ❓ **Search was uninformative** — see below |

**Nothing was refuted outright.** Four papers now need pulling.

---

## C1 — Paper 1's localization-crossing claim

> *"We also cross trigger policy with somatotopic mapping … which no prior work has done."*

### ✅ READ 2026-09-24 — **C1 SURVIVES.** Meng, Ho, Gray & Spence (2014)
**"Dynamic vibrotactile warning signals for frontal collision avoidance: towards the torso versus towards
the head."** *Ergonomics* 58, 411–425. DOI `10.1080/00140139.2014.976278` · 26 citations

The title alone contains a body-location contrast for collision warning, and **"dynamic"** may mean the
signal varies over time — which would put both of Paper 1's factors in one 2014 study.

**What would refute C1:** if they crossed *warning timing* with *body location* factorially. **What would
not:** comparing two warning *directions* at one fixed timing, which is the likelier reading.

Either way **this must be cited.** Spence is the most-cited author in tactile warning signals; a reviewer
from that community will notice its absence immediately.

### 🟠 PULL SECOND — Petermeijer, Abbink, Mulder & de Winter (2015)
**"The Effect of Haptic Support Systems on Driver Performance: A Literature Survey."**
*IEEE Transactions on Haptics* 8, 467–479. DOI `10.1109/toh.2015.2437871` · 126 citations

A **survey** is the cheapest way to find out whether the policy × localization crossing already exists
somewhere in the driving literature. Read its taxonomy before the primary sources.

### 🟠 PULL THIRD — Spence & Ho (2008)
**"Tactile and Multisensory Spatial Warning Signals for Drivers."**
*IEEE Transactions on Haptics* 1, 121–129. DOI `10.1109/toh.2008.14` · 108 citations

The canonical review of *spatial* tactile warnings. Paper 1's H2 is a spatial-mapping hypothesis and
currently cites none of this literature.

### Also surfaced, lower priority
- TactiHelm — tactile feedback in a cycling helmet for collision avoidance. *CHI EA 2021*, `10.1145/3411763.3451580`
- Vibrotactile spatial acuity on the torso. *WHC 2005*, `10.1109/whc.2005.144` — relevant to whether a torso cue can be localized at all

> **Note the pattern:** every one of these is from **driving / applied ergonomics**, not VR. Paper 1's
> related work already leans on automotive FCW for the *timing* half (§2.3). The *localization* half has
> the same adjacent literature and currently cites none of it. That is a gap independent of whether C1
> survives.

---

## C2 — Paper 2's ISO 13855 claim

> *"We were not able to find any published study that reports how `K` was measured."*

### 🔴 PULL — "Hand movement times and machine guarding" (1982)
*Applied Ergonomics* 13, 306. DOI `10.1016/0003-6870(82)90087-4`

Crossref lists **no authors and 0 citations**, and a single page number — so this may be a short note,
abstract or book review rather than a study. But the title is close to a direct hit on the provenance of
the hand-speed figure, and the claim is specifically about provenance.

**If it reports measured hand movement times feeding the guarding standard, C2 is refuted** and the
sentence must be rewritten. Cheap to check, expensive to have missed.

Also worth a look: "Hong Kong female hand dimensions and machine guarding," *Applied Ergonomics* 1985,
`10.1016/0003-6870(85)90240-6`.

---

## C3/C4 — Paper 2's headline claim

> *"We report the first psychometric estimate of that deadline."*

**No refutation found.** The search returned no study fitting a psychometric function to realized warning
lead time.

**But it returned the stop-signal literature in force**, and that is the field a reviewer will come from:

- Verbruggen & Logan (2010), *Biological Psychiatry*, `10.1016/j.biopsych.2010.07.024` · 1,279 cites
- Aron & Poldrack (2006), *J. Neuroscience*, `10.1523/jneurosci.4682-05.2006` · 1,687 cites

SSRT and Paper 2's LT80 are **different estimands** — SSRT is a latency recovered from a race model,
LT80 is a lead time read off a fitted psychometric curve — but they answer adjacent questions about
arresting an initiated movement.

**✅ ALREADY DONE — checked 2026-09-23, this recommendation was wrong.** Paper 2 §2.4 *"Stopping a Movement
Already Underway"* cites Verbruggen 2019, Atsma 2018, Brunamonti 2012 and Friehs 2024, and closes with
precisely the estimand distinction:

> *"This work does not give a warning threshold. It measures how fast a person can cancel an action when
> cancelling is the only task. It does not measure how much advance notice a person needs while still
> trying to reach a target."*

No action. Recorded because a search that recommends work already done is itself a small failure — the
novelty sweep looked at the claim without checking whether the paper already answered it.

---

## C5 — the counterfactual-logging claim

> *"No prior work reports counterfactual [trigger times]."*

**The search was uninformative, and that is not the same as passing.** The queries were too abstract —
results were pure noise (statistical methodology, field experiments, deepfakes), meaning the query never
engaged the topic rather than that the topic is empty.

**Recorded honestly: C5 is unverified.** It is also the lowest-risk of the five — a methodological detail
rather than a headline — so the cheapest fix is to soften the wording rather than fund a better search:
*"we are not aware of prior work that logs both policies' trigger times on every trial"* costs nothing and
cannot be refuted by a single counterexample.

---

## What to do next

1. ~~Pull Meng 2014~~ — **done 2026-09-24, C1 survives.**
2. Remaining PDFs, all lower priority: Petermeijer 2015 and Spence & Ho 2008 (cited from metadata — confirm
   the characterisations), and *Hand movement times and machine guarding* 1982 (could refute Paper 2's C2).
3. **Add the spatial-tactile-warning literature to Paper 1 §2.2 regardless.** That gap exists whether or
   not C1 survives.
4. ~~Add an SSRT paragraph to Paper 2's Related Work~~ — **already present in §2.4.** No action.
5. ~~**Soften C5**~~ — **DONE 2026-09-23.** `PAPER2_STUDY_DESIGN.md` now reads "we are not aware of prior
   work that reports counterfactual prediction error."

## Method, recorded so this is reproducible

Script: `scratchpad/novelty_v2.py`. Crossref `query.bibliographic` + OpenAlex `search`, 4 queries per
claim, deduplicated by DOI, ranked in-venue-first then by citation count — because an in-venue paper is
far likelier to hide the comparison in its method section than an out-of-field one is.

**Coverage limits, stated plainly:** ACM DL (403), IEEE Xplore (418), DBLP (bot challenge) and Semantic
Scholar (429) are unreachable from this environment. Crossref and OpenAlex index the *metadata* of those
venues but not their full text, so anything stated only in a method section is invisible here — which is
precisely how Valkov & Linsen was missed the first time.
