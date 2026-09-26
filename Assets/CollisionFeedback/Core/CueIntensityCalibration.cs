using System;
using System.Collections.Generic;

namespace CollisionFeedback.Core
{
    /// <summary>
    /// Per-site perceptual cue-intensity matching [Plan Task 3.1 / E2] — the procedure that DERIVES the gains
    /// <see cref="CueIntensityTable"/> stores and the live cue applies. It closes the localization confound:
    /// the chest TactSuit X40 (40 motors) at a flat drive feels far more salient than a 3-motor hand/foot
    /// Tactosy, so without this the Localization factor is confounded with raw stimulus energy.
    ///
    /// METHOD. One <see cref="HapticSite"/> is the fixed REFERENCE (a standard perceived magnitude); every other
    /// site is matched to it with its own <see cref="Staircase"/> (a 2AFC "which felt stronger?" PSE search).
    /// The reference keeps its chosen drive; each test site's gain is the staircase's matched estimate. Choose
    /// the reference so all sites can reach it: default = a hand Tactosy at a high-but-sub-ceiling drive, so the
    /// stronger chest is ATTENUATED down to the localized level (directly answering "the chest just buzzes
    /// harder"). The reference site + level is decision D3 — both are configurable.
    ///
    /// Pure state machine (Core): it EMITS the next trial (which site to test, at what drive, against the
    /// reference) and CONSUMES a boolean judgment; the Runtime runner does the actual bHaptics playback,
    /// randomizes interval order, collects the response, and persists the result. Deterministic + unit-tested.
    /// </summary>
    public sealed class CueIntensityCalibration
    {
        /// <summary>Tuning for the per-site staircases (mirrors <see cref="Staircase"/>'s knobs).</summary>
        public sealed class Options
        {
            public HapticSite ReferenceSite = HapticSite.LeftHand; // a 3-motor Tactosy: reachable by every site
            public float ReferenceIntensity = 0.8f;                // the standard drive (0..1); sub-ceiling headroom
            public float StartLevel = 0.5f;
            public float InitialStep = 0.2f;
            public float MinStep = 0.02f;
            public int ReversalsToStop = 10;
            public int ReversalsToAverage = 6;
            public int NDown = 1;   // 1-up/1-down => PSE (equal-magnitude match). See Staircase remarks.
            public int NUp = 1;
            public float MinLevel = 0f;
            public float MaxLevel = 1f;
        }

        private readonly Options _o;
        private readonly List<HapticSite> _testSites = new();  // every site except the reference, in enum order
        private readonly Dictionary<HapticSite, Staircase> _stairs = new();
        private int _index;                                    // which test site is active

        public CueIntensityCalibration(Options options = null)
        {
            _o = options ?? new Options();
            foreach (HapticSite s in Enum.GetValues(typeof(HapticSite)))
            {
                if (s == _o.ReferenceSite) continue;
                _testSites.Add(s);
                _stairs[s] = new Staircase(
                    startLevel: _o.StartLevel, initialStep: _o.InitialStep, minStep: _o.MinStep,
                    nDown: _o.NDown, nUp: _o.NUp,
                    reversalsToStop: _o.ReversalsToStop, reversalsToAverage: _o.ReversalsToAverage,
                    minLevel: _o.MinLevel, maxLevel: _o.MaxLevel);
            }
        }

        /// <summary>The standard stimulus everything is matched against.</summary>
        public HapticSite ReferenceSite => _o.ReferenceSite;

        /// <summary>The reference's fixed drive (also its gain in the produced table).</summary>
        public float ReferenceIntensity => Clamp01(_o.ReferenceIntensity);

        /// <summary>Every site being matched (reference excluded), in the order they're calibrated.</summary>
        public IReadOnlyList<HapticSite> TestSites => _testSites;

        /// <summary>True once every test site's staircase has finished.</summary>
        public bool IsComplete => _index >= _testSites.Count;

        /// <summary>The site currently being matched. Valid only when <see cref="IsComplete"/> is false.</summary>
        public HapticSite CurrentSite => _testSites[Math.Min(_index, _testSites.Count - 1)];

        /// <summary>The test-site drive to play for THIS trial (the reference is always played at
        /// <see cref="ReferenceIntensity"/>).</summary>
        public float CurrentTestLevel => IsComplete ? 0f : _stairs[CurrentSite].CurrentLevel;

        /// <summary>Zero-based index of the active test site (== <see cref="TestSites"/>.Count when complete).</summary>
        public int SiteIndex => _index;

        /// <summary>The active site's staircase (for the runner's per-trial logging / progress read-outs).</summary>
        public Staircase CurrentStaircase => IsComplete ? null : _stairs[CurrentSite];

        /// <summary>
        /// Feed the judgment for the current trial: <paramref name="testStronger"/> = the participant perceived
        /// the CURRENT TEST SITE's pulse as more intense than the reference's. Advances the active staircase and,
        /// when it finishes, moves to the next site. No-op once complete.
        /// </summary>
        public void Respond(bool testStronger)
        {
            if (IsComplete) return;
            Staircase s = _stairs[CurrentSite];
            s.Respond(testStronger);
            if (s.Done) _index++;
        }

        /// <summary>The matched estimate for a finished site (or its current level mid-run).</summary>
        public float EstimateFor(HapticSite site) =>
            site == _o.ReferenceSite ? ReferenceIntensity :
            (_stairs.TryGetValue(site, out Staircase s) ? s.Estimate : 1f);

        /// <summary>True if <paramref name="site"/> could not reach the reference (pinned at a clamp) — its cue
        /// stays maximally different and the match for that site is only best-effort.</summary>
        public bool PinnedAtBound(HapticSite site) =>
            site != _o.ReferenceSite && _stairs.TryGetValue(site, out Staircase s) && s.PinnedAtBound;

        /// <summary>
        /// Build the per-site gain table: the reference site at its fixed drive, every other site at its matched
        /// estimate. Callable mid-run (unfinished sites contribute their current estimate) but intended at
        /// completion. Feed the result to <c>Runtime/CueIntensityFile.Save</c>; <c>SessionRunner</c> then loads it.
        /// </summary>
        public CueIntensityTable BuildTable()
        {
            var table = new CueIntensityTable();
            foreach (HapticSite s in Enum.GetValues(typeof(HapticSite)))
                table.Set(s, EstimateFor(s));
            return table;
        }

        private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    }
}
