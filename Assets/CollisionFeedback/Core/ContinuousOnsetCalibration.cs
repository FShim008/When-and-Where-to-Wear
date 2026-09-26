using System;
using System.Collections.Generic;

namespace CollisionFeedback.Core
{
    /// <summary>
    /// Matches the ONSET SALIENCE of the continuous cue (<see cref="Condition.PBC"/>) to that of the
    /// discrete cue (<see cref="Condition.PB"/>), and produces the value that freezes
    /// <see cref="OracleParams.ContinuousCueMinIntensity"/>.
    ///
    /// THE CONFOUND THIS EXISTS TO CLOSE [docs/PAPER1_READINESS.md §1].
    /// H4' claims to vary CUE FORM with trigger, site, information and onset held constant. The first three
    /// are guaranteed structurally (<see cref="ConditionManager"/> puts PB and PBC on one trigger branch, and
    /// <see cref="ContinuousCueMapping"/> derives its band from the same effective threshold). ONSET IS NOT.
    ///
    /// PB delivers a sharp-onset pulse train at the site's calibrated drive. PBC steps to
    /// <see cref="ContinuousCueParams.MinPerceptibleIntensity"/> and then ramps. A sharp onset is more
    /// detectable than a gradual one — elementary psychophysics, not a subtle effect. So if PBC produces
    /// more violations there are two readings:
    ///
    ///   1. the hypothesis  — a continuous cue can be modulated by slowing down, so it is obeyed less;
    ///   2. an artefact     — PBC's onset is simply harder to notice, so its EFFECTIVE warning is later.
    ///
    /// Reading 2 is the same class of confound H4' exists to remove from the literature. Shipping it would
    /// be self-defeating: we would have replaced Valkov and Linsen's trigger/mapping confound with a
    /// mapping/onset one and claimed to have resolved something.
    ///
    /// WHY 2AFC MAGNITUDE MATCHING IS THE RIGHT PROXY.
    /// The natural target is detection LATENCY, but reaction-time matching is noisy, needs many trials, and
    /// has no standard adaptive rule. Perceived magnitude AT ONSET is a direct proxy here, because the
    /// confound is precisely that PBC's first moment is weaker than PB's. If the first moment of PBC feels
    /// as strong as the first moment of PB, the two effective onsets coincide — which is the claim H4' needs.
    /// The judgment reuses <see cref="Staircase"/> unchanged (1-up/1-down, PSE-seeking).
    ///
    /// WHAT THE RUNNER MUST PRESENT. Two intervals in randomised order:
    ///   REFERENCE — PB's onset: the fixed pulse train at the site's calibrated drive.
    ///   TEST      — PBC's onset ONLY: a step to <see cref="CurrentTestLevel"/> held for
    ///               <see cref="Options.OnsetWindowMillis"/>, with NO ramp.
    /// The ramp is deliberately excluded. Including it would let the later, stronger part of the cue carry
    /// the judgment, and the participant would be matching the whole approach rather than its onset — the
    /// one moment that decides whether the warning arrives on time.
    ///
    /// Pure and deterministic, like <see cref="CueIntensityCalibration"/>: no wall clock, no RNG. The
    /// Runtime runner randomises interval order and feeds booleans in.
    /// </summary>
    public sealed class ContinuousOnsetCalibration
    {
        public sealed class Options
        {
            /// <summary>
            /// Sites to measure. PBC is body-localized, so it never cues the chest — only limb sites can
            /// carry it. Measuring several is a CHECK on the assumption that one frozen floor suffices:
            /// the E1 per-site gains are supposed to have equalised salience already, so the estimates
            /// should agree. If they do not, see <see cref="Spread"/>.
            /// </summary>
            public HapticSite[] TestSites =
            {
                HapticSite.LeftHand, HapticSite.RightHand, HapticSite.LeftShin, HapticSite.RightShin,
            };

            /// <summary>
            /// How long the TEST onset is held, in ms. Should match one reference pulse so the comparison is
            /// between two equal-duration first moments rather than between a pulse and a sustained tone.
            /// </summary>
            public int OnsetWindowMillis = 100;

            /// <summary>Start of the search, in the same 0..1 units as the cue floor.</summary>
            public float StartLevel = 0.15f;      // the current unmeasured default — a starting guess, not a claim

            public float InitialStep = 0.10f;
            public float MinStep = 0.01f;
            public int ReversalsToStop = 10;
            public int ReversalsToAverage = 6;
            public int NDown = 1;   // 1-up/1-down => PSE (equal-magnitude match), as in E1
            public int NUp = 1;
            public float MinLevel = 0f;
            public float MaxLevel = 1f;
        }

        private readonly Options _o;
        private readonly List<HapticSite> _sites = new();
        private readonly Dictionary<HapticSite, Staircase> _stairs = new();
        private int _index;

        public ContinuousOnsetCalibration(Options options = null)
        {
            _o = options ?? new Options();
            foreach (HapticSite s in _o.TestSites)
            {
                if (s == HapticSite.Chest)
                    throw new ArgumentException(
                        "PBC is body-localized and never cues the chest, so a chest onset match would " +
                        "calibrate a cue the condition cannot deliver.", nameof(options));
                if (_stairs.ContainsKey(s)) continue;
                _sites.Add(s);
                _stairs[s] = new Staircase(
                    startLevel: _o.StartLevel, initialStep: _o.InitialStep, minStep: _o.MinStep,
                    nDown: _o.NDown, nUp: _o.NUp,
                    reversalsToStop: _o.ReversalsToStop, reversalsToAverage: _o.ReversalsToAverage,
                    minLevel: _o.MinLevel, maxLevel: _o.MaxLevel);
            }
            if (_sites.Count == 0) throw new ArgumentException("No test sites.", nameof(options));
        }

        public int OnsetWindowMillis => _o.OnsetWindowMillis;
        public IReadOnlyList<HapticSite> TestSites => _sites;
        public bool IsComplete => _index >= _sites.Count;
        public int SiteIndex => _index;
        public HapticSite CurrentSite => _sites[Math.Min(_index, _sites.Count - 1)];
        public Staircase CurrentStaircase => IsComplete ? null : _stairs[CurrentSite];

        /// <summary>The TEST onset level to present on this trial.</summary>
        public float CurrentTestLevel => IsComplete ? 0f : _stairs[CurrentSite].CurrentLevel;

        /// <summary>
        /// Record one judgment. <paramref name="continuousOnsetFeltStronger"/> is the participant saying the
        /// PBC interval's onset was the stronger of the two — the staircase then steps the level DOWN.
        /// </summary>
        public void Respond(bool continuousOnsetFeltStronger)
        {
            if (IsComplete) return;
            Staircase s = _stairs[CurrentSite];
            s.Respond(continuousOnsetFeltStronger);
            if (s.Done) _index++;
        }

        public float EstimateFor(HapticSite site) =>
            _stairs.TryGetValue(site, out Staircase s) ? s.Estimate : float.NaN;

        /// <summary>
        /// True when a site's staircase ran to its bound instead of converging. At
        /// <see cref="Options.MaxLevel"/> it means PBC's onset could NOT be made to feel as strong as PB's
        /// even at full drive — the onsets are not matchable with this cue form, and H4' must be reported
        /// with that stated rather than claiming cue form was isolated.
        /// </summary>
        public bool PinnedAtBound(HapticSite site) =>
            _stairs.TryGetValue(site, out Staircase s) && s.PinnedAtBound;

        /// <summary>
        /// The single value to freeze into <see cref="OracleParams.ContinuousCueMinIntensity"/>: the mean of
        /// the per-site estimates. One number is correct ONLY IF the sites agree — the per-site E1 gains are
        /// applied downstream in the sink, so the floor is meant to be site-independent. Check
        /// <see cref="Spread"/> before trusting it.
        /// </summary>
        public float PooledEstimate
        {
            get
            {
                float sum = 0f; int n = 0;
                foreach (HapticSite s in _sites)
                {
                    float e = EstimateFor(s);
                    if (!float.IsNaN(e)) { sum += e; n++; }
                }
                return n == 0 ? float.NaN : sum / n;
            }
        }

        /// <summary>
        /// Max minus min across sites. If this is large the E1 per-site gains did NOT equalise onset
        /// salience, and a single floor is the wrong model — the cue would start perceptibly later on some
        /// limbs than others, reintroducing the onset confound for a subset of opportunities.
        /// </summary>
        public float Spread
        {
            get
            {
                float lo = float.PositiveInfinity, hi = float.NegativeInfinity;
                foreach (HapticSite s in _sites)
                {
                    float e = EstimateFor(s);
                    if (float.IsNaN(e)) continue;
                    if (e < lo) lo = e;
                    if (e > hi) hi = e;
                }
                return float.IsInfinity(lo) ? float.NaN : hi - lo;
            }
        }

        /// <summary>
        /// Preregistered read-out. Above this spread a single frozen floor is not defensible and the
        /// decision must be recorded: either per-site floors, or H4' reported with the onset caveat.
        /// </summary>
        public const float MaxDefensibleSpread = 0.10f;

        public bool SpreadIsAcceptable => !float.IsNaN(Spread) && Spread <= MaxDefensibleSpread;
    }
}
