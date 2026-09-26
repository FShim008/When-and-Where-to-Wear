using System;
using System.Collections.Generic;

namespace CollisionFeedback.Core
{
    /// <summary>
    /// A transformed up/down adaptive staircase for a 2AFC "is the TEST stronger than the REFERENCE?" judgment
    /// [Plan Task 3.1 / E2]. It searches for the test-stimulus level whose PERCEIVED magnitude equals the
    /// reference's — the Point of Subjective Equality (PSE) — which is exactly what cross-site cue-intensity
    /// matching needs (equal perceived salience at chest vs a 3-motor Tactosy).
    ///
    /// WHY 1-up/1-down BY DEFAULT: a symmetric one-up/one-down rule brackets the 50% point of the psychometric
    /// function — the PSE — which is the equal-magnitude match we want. The 2-down/1-up rule the plan mentions
    /// converges on the ~70.7% point, i.e. a DISCRIMINATION THRESHOLD (a just-noticeable difference), not a
    /// match; it's exposed via <see cref="nUp"/>/<see cref="nDown"/> for flexibility, but the default targets
    /// the PSE. (Levitt 1971; García-Pérez 1998.)
    ///
    /// Pure + deterministic (no wall-clock, no RNG — the Runtime runner randomizes interval order and feeds
    /// booleans in), so it is fully unit-tested against a synthetic observer. Levels are unitless 0..1 device
    /// drives here, but the class is agnostic to what the level means.
    /// </summary>
    public sealed class Staircase
    {
        // Response bookkeeping: we accumulate consecutive same-sense judgments and only step once the transform
        // rule is satisfied (nDown "test stronger" in a row → step down; nUp "test weaker" in a row → step up).
        private readonly int _nDown;
        private readonly int _nUp;
        private readonly float _stepFactor;   // multiply the step by this on each reversal (0<f<1 => shrink)
        private readonly float _minStep;
        private readonly float _minLevel;
        private readonly float _maxLevel;
        private readonly int _reversalsToStop;
        private readonly int _reversalsToAverage;
        private readonly int _maxTrials;      // hard cap so a level pinned at a bound can't loop forever

        private readonly List<float> _reversalLevels = new();

        private float _level;
        private float _step;
        private int _consecDown;              // consecutive "test stronger" (want to go DOWN)
        private int _consecUp;                // consecutive "test weaker"   (want to go UP)
        private int _lastDir;                 // -1 down, +1 up, 0 none (last actual move direction)
        private int _trials;
        private bool _done;

        /// <summary>
        /// Build a staircase. Defaults implement a PSE-seeking 1-up/1-down rule with a step that halves at each
        /// reversal down to <paramref name="minStep"/>, stopping after <paramref name="reversalsToStop"/>
        /// reversals; the estimate is the mean of the last <paramref name="reversalsToAverage"/> reversal levels.
        /// </summary>
        /// <param name="startLevel">initial test level (clamped to [minLevel,maxLevel]).</param>
        /// <param name="initialStep">initial step size in level units.</param>
        /// <param name="minStep">smallest step the reversal-shrink schedule reaches (measurement resolution).</param>
        /// <param name="stepFactor">step multiplier per reversal (0.5 = halve).</param>
        /// <param name="nDown">consecutive "test stronger" judgments needed to step DOWN (1 = PSE).</param>
        /// <param name="nUp">consecutive "test weaker" judgments needed to step UP (1 = PSE).</param>
        /// <param name="reversalsToStop">stop after this many reversals.</param>
        /// <param name="reversalsToAverage">average the last this-many reversals for the estimate (use an even count).</param>
        /// <param name="minLevel">lower clamp on the level.</param>
        /// <param name="maxLevel">upper clamp on the level.</param>
        /// <param name="maxTrials">safety cap: force <see cref="Done"/> after this many responses even without
        /// enough reversals (happens when the true match is beyond a clamp and the level pins to a bound).</param>
        public Staircase(float startLevel = 0.5f, float initialStep = 0.2f, float minStep = 0.02f,
                         float stepFactor = 0.5f, int nDown = 1, int nUp = 1,
                         int reversalsToStop = 10, int reversalsToAverage = 6,
                         float minLevel = 0f, float maxLevel = 1f, int maxTrials = 80)
        {
            _minLevel = minLevel;
            _maxLevel = Math.Max(minLevel, maxLevel);
            _step = Math.Max(1e-4f, initialStep);
            _minStep = Math.Max(1e-4f, Math.Min(minStep, _step));
            _stepFactor = (stepFactor > 0f && stepFactor < 1f) ? stepFactor : 0.5f;
            _nDown = Math.Max(1, nDown);
            _nUp = Math.Max(1, nUp);
            _reversalsToStop = Math.Max(2, reversalsToStop);
            _reversalsToAverage = Math.Max(1, Math.Min(reversalsToAverage, _reversalsToStop));
            _maxTrials = Math.Max(_reversalsToStop, maxTrials);
            _level = Clamp(startLevel);
        }

        /// <summary>The level to present for the NEXT trial (the test-site device drive to play).</summary>
        public float CurrentLevel => _level;

        /// <summary>True once the stop rule (enough reversals) or the trial cap is reached.</summary>
        public bool Done => _done;

        /// <summary>Number of responses fed so far.</summary>
        public int Trials => _trials;

        /// <summary>Number of reversals recorded so far.</summary>
        public int Reversals => _reversalLevels.Count;

        /// <summary>The recorded reversal levels (turnaround points), in order.</summary>
        public IReadOnlyList<float> ReversalLevels => _reversalLevels;

        /// <summary>True if the run ended pinned against a clamp (the match lies beyond [min,max] — the site
        /// could not reach the reference; the estimate is a best-effort bound and equalization is incomplete).</summary>
        public bool PinnedAtBound => _done && Reversals < _reversalsToStop &&
                                     (_level >= _maxLevel - 1e-4f || _level <= _minLevel + 1e-4f);

        /// <summary>
        /// The matched level estimate: the mean of the last <c>reversalsToAverage</c> reversal levels once the
        /// run has reversals; otherwise the current level (e.g. when pinned at a bound with no reversals).
        /// </summary>
        public float Estimate
        {
            get
            {
                if (_reversalLevels.Count == 0) return _level;
                int take = Math.Min(_reversalsToAverage, _reversalLevels.Count);
                float sum = 0f;
                for (int i = _reversalLevels.Count - take; i < _reversalLevels.Count; i++) sum += _reversalLevels[i];
                return sum / take;
            }
        }

        /// <summary>
        /// Feed one judgment: <paramref name="testStrongerThanReference"/> = the observer perceived the TEST
        /// stimulus as more intense than the reference. Advances the staircase (records reversals, shrinks the
        /// step, may set <see cref="Done"/>). No-op once done.
        /// </summary>
        public void Respond(bool testStrongerThanReference)
        {
            if (_done) return;
            _trials++;

            // Which way do we WANT to move once the transform rule is met? -1 down (test too strong),
            // +1 up (test too weak), 0 not yet enough consecutive judgments to move.
            int desired;
            if (testStrongerThanReference)
            {
                _consecUp = 0;
                _consecDown++;
                desired = (_consecDown >= _nDown) ? -1 : 0;
                if (desired != 0) _consecDown = 0;
            }
            else
            {
                _consecDown = 0;
                _consecUp++;
                desired = (_consecUp >= _nUp) ? +1 : 0;
                if (desired != 0) _consecUp = 0;
            }

            if (desired != 0)
            {
                // Reversal = the desired direction flips relative to the last actual move.
                if (_lastDir != 0 && desired != _lastDir)
                {
                    _reversalLevels.Add(_level);                       // turnaround captured at the current level
                    _step = Math.Max(_minStep, _step * _stepFactor);   // sharpen resolution after each reversal
                    if (_reversalLevels.Count >= _reversalsToStop) { _done = true; return; }
                }
                _lastDir = desired;
                _level = Clamp(_level + desired * _step);
            }

            if (_trials >= _maxTrials) _done = true; // pinned-at-bound / degenerate guard
        }

        private float Clamp(float v) => v < _minLevel ? _minLevel : (v > _maxLevel ? _maxLevel : v);
    }
}
