# Opportunity Schedule — Approach-Distance Audit

**Run:** 2026-09-14 · **Gate:** `PAPER1_STUDY_DESIGN.md` §14.27 · **Rationale:** §6
**Method:** the real `Obstacle` geometry math, the real `OpportunitySchedules.Layout1()` schedule, and the real
`Layout1Stimuli.All()` orb positions, compiled and executed outside Unity against a minimal UnityEngine shim.

---

## Verdict

> ## ✅ FIXED AND RE-AUDITED 2026-09-14 — 12/12 pass
>
> O2 was moved in `Assets/Scenes/Dryrun.unity` from **(0.40, 0.70, 0.00)** to **(0.95, 0.70, 0.60)**; size and
> rotation untouched, one line changed, backup at `Dryrun.unity.bak-20260914-031750`. §7 records the post-fix
> numbers. **The original finding is kept below in full** — it is the reason the parameter, the gate and the
> geometry-ownership item exist, and it must not read as though the schedule was always sound.

### Original finding (pre-fix)

**4 of 12 opportunities failed — E1, E6, E7, E12, every one of them targeting O2.** The cause was a single
misplaced hazard, and the consequence was larger than the timing problem that prompted the audit.

| | |
|---|---|
| Pass (≥ 0.60 m approach) | 8 — E2, E3, E4, E5, E8, E9, E10, E11 |
| Marginal (0.40–0.60 m) | 0 |
| **Fail (< 0.40 m)** | **4 — E1, E6, E7, E12** |

---

## 1. Why approach distance matters

`PAPER1_STUDY_DESIGN.md` §6: the proximity policy fires at a fixed distance `D` = 0.30 m; the predictive policy
fires at a fixed time `T` = 1.00 s, i.e. at distance `v·T`. If a limb **begins its movement closer to the
hazard than `D`**, the proximity cue fires at movement onset and the predictive cue cannot lead it at any
speed. That opportunity then contributes a reversed or null timing manipulation to H1.

Simulation floor: **0.40 m** (`OracleParams.MinApproachDistanceForValidTiming`).
Design target: **0.60 m** (`OracleParams.RecommendedApproachDistance`).

---

## 2. Geometry audited

Read from `Assets/Scenes/Dryrun.unity` (position + `localScale`); O4a/O4b from `SceneObstacles` defaults.

*These are the values **as audited**, i.e. before the fix. O2 has since moved — see §7.*

| Id | Centre (x, y, z) | Size (x, y, z) | Role |
|---|---|---|---|
| O1 | 0.35, 0.20, 1.25 | 0.40 × 0.40 × 0.40 | low block |
| **O2** | **0.40, 0.70, 0.00** | 0.35 × 1.40 × 0.35 | **pillar — the problem** |
| O3 | −1.00, 0.75, −0.30 | 0.10 × 1.50 × 0.60 | panel |
| O4a | 0.00, 1.00, 1.75 | 3.50 × 2.00 × 0.10 | front wall volume |
| O4b | −1.75, 1.00, 0.00 | 0.10 × 2.00 × 3.50 | left wall volume |

**Model:** the limb starts at a neutral home posture with the participant standing at the arena origin facing
+z — right hand (0.20, 1.10, 0.20), left hand (−0.20, 1.10, 0.20), chest (0, 1.30, 0), feet at (±0.10, 0.06, 0).

---

## 3. Results

```
  ev   limb        haz  | approach | orb->haz | verdict
  ---- ----------- ---- +----------+----------+--------
  E1   RightHand   O2   |    0.04m |    1.24m | *** FAIL ***
  E2   LeftHand    O3   |    0.78m |     --   | ok
  E3   RightFoot   O1   |    1.05m |    0.05m | ok
  E4   LeftHand    O3   |    0.78m |    0.53m | ok
  E5   Chest       O4a  |    1.70m |    0.05m | ok
  E6   RightHand   O2   |    0.04m |     --   | *** FAIL ***
  E7   RightHand   O2   |    0.04m |    1.04m | *** FAIL ***
  E8   LeftFoot    O1   |    1.08m |     --   | ok
  E9   LeftHand    O3   |    0.78m |    0.36m | ok
  E10  LeftHand    O3   |    0.78m |     --   | ok
  E11  Chest       O4b  |    1.70m |    0.15m | ok
  E12  RightHand   O2   |    0.04m |    1.17m | *** FAIL ***
```

### Clearance from the participant's standing position

| Hazard | to chest | to R hand | to L hand | used by |
|---|---|---|---|---|
| O1 | 1.39 m | 1.10 m | 1.16 m | E3, E8 |
| **O2** | **0.23 m** | **0.04 m** | 0.43 m | E1, E6, E7, E12 |
| O3 | 0.95 m | 1.17 m | 0.78 m | E2, E4, E9, E10 |
| O4a | 1.70 m | 1.50 m | 1.50 m | E5 |
| O4b | 1.70 m | 1.90 m | 1.50 m | E11 |

---

## 4. ⚠ The finding is bigger than the timing manipulation

O2's surface sits **0.04 m from the right hand at rest.** Against the provisional contact model:

```
  RightHand  surface  0.04 m - radius 0.08 = effective  0.00 m   <-- ALREADY IN CONTACT AT REST
  Chest      surface  0.23 m - radius 0.12 = effective  0.11 m
  LeftHand   surface  0.43 m - radius 0.08 = effective  0.35 m
```

**A participant standing neutrally at the arena origin is already violating O2 with their right hand.** Three
consequences, none of which is about timing:

1. **The primary outcome is corrupted for a third of all opportunities.** E1, E6, E7 and E12 would record
   `violation = 1` from posture alone, independent of condition. That is a ceiling, and it is the mirror image
   of the floor risk in `VIRTUAL_HAZARD_VALIDITY.md` §6b — arriving from the opposite direction.
2. **It contaminates the `None` baseline.** A violation that occurs regardless of warning shrinks every
   condition contrast toward zero, and it does so *asymmetrically*, since the conditions differ in how much
   they encourage the participant to move away.
3. **The timing manipulation is void on those four opportunities**, which is how the audit found it.

**This would very likely not have been visible in the pilot console line.** A block reporting
`PRIMARY: 4/12 violated (33%)` looks healthy — off the floor, off the ceiling, exactly what gate §14.10 asks
for. The four unavoidable O2 violations sit inside a plausible-looking number.

---

## 5. Recommended fix — move O2

Keep the pillar's size (0.35 × 1.40 × 0.35) and y centre (0.70). Move it outward along the reach path toward
its orbs, so it remains a genuine hazard *en route* rather than a fixture in the participant's personal space.

**Recommended: O2 centre → (0.95, 0.70, 0.60)** — in the Inspector, Position `x = 0.95`, `z = 0.60`.

| Check | Result |
|---|---|
| Approach from right hand at rest | **0.62 m** — clears the 0.60 m target |
| Max distance from the three E1/E7/E12 reach paths | **0.17 m** — still squarely en route |
| Effective clearance at rest (0.08 m hand radius) | 0.54 m — no resting contact |
| Overlap with O1 (x 0.15–0.55, z 1.05–1.45) | none — new O2 spans x 0.775–1.125, z 0.425–0.775 |
| Wall clearance | 0.80 m from O4a, 2.70 m from O4b |
| Blocks the E3 orb (0.35, 0.35, 1.50)? | no |

Alternatives within tolerance, if the scene needs adjustment for other reasons:

```
    x      z   | approach | max dist to the 3 reach paths
  ------+------+----------+------------------------------
   0.95 |  0.60 |    0.62m |   0.17 m     <-- recommended
   1.00 |  0.65 |    0.68m |   0.17 m
   0.95 |  0.65 |    0.64m |   0.18 m
   1.00 |  0.60 |    0.66m |   0.19 m
   1.05 |  0.65 |    0.73m |   0.19 m
```

**Re-run this audit after moving it.** The layouts L2–L6 and LP are rigid isometries of L1, which preserve every
distance by construction, so **fixing L1 fixes all seven layouts** — but only if the change is made to the
authored L1 baseline that `SceneObstacles` captures, not to a variant.

---

## 6. Secondary observations

**6.1 O3 is acceptable but not generous.** 0.78 m from the left hand at rest, which clears the target. Under
the sensitivity sweep (participant displaced ±0.6 m in the floor plane) 13% of standing positions put the four
O3 opportunities below the 0.40 m floor. Acceptable; worth noting in the preregistration as a known source of
per-trial variation rather than discovering it in the data.

**6.2 O2 is over-subscribed.** Four of twelve opportunities target a single hazard, and all four target the
**right hand**. Combined with the §4 finding that handedness is not currently recorded or balanced, a
right-dominant sample would concentrate a third of the primary outcome on one hazard and one dominant limb.
Consider redistributing E6 or E12 to another hazard when the geometry is revised.

**6.3 ⚠ The hazard geometry has no single source of truth.** O1/O2/O3 exist only as transforms inside
`Dryrun.unity`; O4a/O4b only as `SerializeField` defaults in `SceneObstacles`. Nothing in Core, and nothing
tested, defines the volumes that produce the primary outcome. That is why a hazard could sit 4 cm from the
participant's hand without anything complaining.

**Recommended:** move the authored L1 geometry into Core as data, have `SceneObstacles` build from it (or
assert the scene matches it), and add an EditMode test asserting every opportunity clears
`MinApproachDistanceForValidTiming`. Deliberately **not** done in this pass — writing the geometry into a test
file while it still lives in the scene would create a second source of truth for exactly the thing being
criticised. Sequence it as: single source first, then the test.

---

## 7. Post-fix results — re-audited 2026-09-14

O2 centre now **(0.95, 0.70, 0.60)**, read back from the scene file by the audit rather than transcribed.

```
  ev   limb        haz  | approach | orb->haz | verdict
  E1   RightHand   O2   |    0.62m |    0.43m | ok
  E2   LeftHand    O3   |    0.78m |     --   | ok
  E3   RightFoot   O1   |    1.05m |    0.05m | ok
  E4   LeftHand    O3   |    0.78m |    0.53m | ok
  E5   Chest       O4a  |    1.70m |    0.05m | ok
  E6   RightHand   O2   |    0.62m |     --   | ok
  E7   RightHand   O2   |    0.62m |    0.38m | ok
  E8   LeftFoot    O1   |    1.08m |     --   | ok
  E9   LeftHand    O3   |    0.78m |    0.36m | ok
  E10  LeftHand    O3   |    0.78m |     --   | ok
  E11  Chest       O4b  |    1.70m |    0.15m | ok
  E12  RightHand   O2   |    0.62m |    0.37m | ok

  => 12 ok, 0 marginal, 0 FAIL  (of 12)
```

**Contact model, right hand at rest:** surface 0.62 m − 0.08 m radius = **0.54 m effective**. No resting
contact; the ceiling artefact is gone.

**Sensitivity improved but did not vanish.** Under the ±0.6 m standing-position sweep, the four O2
opportunities went from **59% → 22%** of positions below the 0.40 m floor. The residual is a participant
standing beside the pillar, which is a real posture rather than a geometry defect, and it is now in the same
band as O3's 13%. **Preregister this** as a known source of per-trial variation, and consider it an argument
for the counterfactual dual-policy logging in §6: with both trigger times recorded, per-trial timing validity
becomes measurable instead of assumed.

**The orbs did not need moving.** Orb-to-hazard clearance for the O2 events is now 0.37–0.43 m — the pillar
sits squarely en route without the orb being inside or against it.

**Unverified in Unity.** The scene was edited on disk with the Editor closed, as a single scoped line change
with a line-count assertion and an independent re-scan. It has not been opened in Unity since. **Open the
scene and confirm the pillar looks right before running a participant.**

## 8. Reproducing this audit

The harness compiles Core standalone against a minimal `UnityEngine` shim (`Vector3` + `Mathf` only — Core
touches nothing else by architectural rule) and runs under `dotnet`. Hazard geometry is **generated from
`Dryrun.unity` and the `SceneObstacles` defaults at run time**, so the audit cannot be run against stale
positions. Working copy at `C:\cc` at time of writing; the program is throwaway, the method is not. Re-run
whenever hazard positions, orb positions, the schedule, `D`, or `T` change.
