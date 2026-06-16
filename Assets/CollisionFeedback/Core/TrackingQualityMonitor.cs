using System.Collections.Generic;
using UnityEngine;

namespace CollisionFeedback.Core
{
    /// <summary>Pass/fail bar for the M4 tracking-with-gear bench check (Pilot_Design Stage 0 / Risk R1).</summary>
    public sealed class TrackingQualityThresholds
    {
        public float MinDeliveryRateHz = 30f;    // frames must reach Unity at >= the oracle's 30 Hz need
        public float MaxFrameGapSeconds = 0.10f;  // a delivery gap longer than this is a dropout
        public float MaxJitterMeters = 0.01f;     // <= 1 cm noise floor when the limb is held still
        public float MaxFreezeSeconds = 0.25f;    // a joint unchanged this long (while frames arrive) = frozen/occluded
        public float JumpMeters = 0.50f;          // a single-frame move beyond this is a tracking glitch
    }

    /// <summary>Per-joint tracking-quality summary over the current measurement window.</summary>
    public readonly struct JointTrackingStats
    {
        public readonly Joint Joint;
        public readonly int Samples;
        public readonly float JitterMeters;      // RMS 3D deviation from the running mean (read during a still hold)
        public readonly float MaxFreezeSeconds;  // longest run the position stayed identical (occlusion/dropout)
        public readonly int Jumps;               // single-frame moves beyond the jump threshold
        public readonly int InvalidSamples;      // NaN/Inf positions
        public readonly bool Pass;               // invalid == 0 && jumps == 0 && freeze <= bar && seen (phase-independent)

        public JointTrackingStats(Joint joint, int samples, float jitterMeters, float maxFreezeSeconds,
                                  int jumps, int invalidSamples, bool pass)
        {
            Joint = joint;
            Samples = samples;
            JitterMeters = jitterMeters;
            MaxFreezeSeconds = maxFreezeSeconds;
            Jumps = jumps;
            InvalidSamples = invalidSamples;
            Pass = pass;
        }
    }

    /// <summary>Frame-level + per-joint snapshot of tracking quality over the current window.</summary>
    public readonly struct TrackingQualityReport
    {
        public readonly double WindowSeconds;
        public readonly int Frames;
        public readonly float DeliveryRateHz;
        public readonly float MaxFrameGapSeconds;
        public readonly float MeanLagSeconds;    // wall - data timestamp (absolute only if the clocks are synced)
        public readonly IReadOnlyList<JointTrackingStats> Joints;
        public readonly bool Pass;

        public TrackingQualityReport(double windowSeconds, int frames, float deliveryRateHz, float maxFrameGapSeconds,
                                     float meanLagSeconds, IReadOnlyList<JointTrackingStats> joints, bool pass)
        {
            WindowSeconds = windowSeconds;
            Frames = frames;
            DeliveryRateHz = deliveryRateHz;
            MaxFrameGapSeconds = maxFrameGapSeconds;
            MeanLagSeconds = meanLagSeconds;
            Joints = joints;
            Pass = pass;
        }
    }

    /// <summary>
    /// Pure, hardware-free metrics for the M4 "does tracking survive with the gear on?" bench check
    /// (Pilot_Design Stage 0 / Risk R1). Feed it every delivered <see cref="PoseFrame"/> with the wall-clock
    /// time it reached Unity; it accumulates per-joint jitter / freeze / jump / validity and frame-level
    /// delivery rate + worst gap + lag, and reports a quantitative PASS/FAIL against
    /// <see cref="TrackingQualityThresholds"/>. Deterministic + unit-testable; the Runtime TrackingBench is
    /// the thin MonoBehaviour that feeds it the UDP stream and renders it.
    ///
    /// Interpretation: read JITTER during a deliberate still-hold (it is the noise floor only when the limb
    /// is NOT moving); read RATE / FREEZE / JUMPS during fast motion (the dodge stress test). Because the
    /// wire format always carries all 6 joints, an occluded limb shows up as a FROZEN run (the pipeline
    /// repeats its last value), not as a missing joint — which is exactly what <see cref="JointTrackingStats.MaxFreezeSeconds"/>
    /// catches.
    /// </summary>
    public sealed class TrackingQualityMonitor
    {
        private const float UnchangedEpsilon = 1e-6f; // identical position across frames => the joint is frozen

        private sealed class JointState
        {
            public int Samples;
            public Vector3 Mean;
            public double M2;            // Welford accumulator: sum of squared 3D distances from the running mean
            public Vector3 LastPos;
            public bool HasLast;
            public double LastChangeWall;
            public double MaxFreeze;
            public int Jumps;
            public int Invalid;
        }

        private readonly IReadOnlyList<Joint> _joints;
        private readonly TrackingQualityThresholds _t;
        private readonly Dictionary<Joint, JointState> _states = new();

        private int _frames;
        private double _firstWall, _lastWall;
        private double _maxFrameGap;
        private double _lagSum;
        private bool _hasFrame;

        public TrackingQualityMonitor(IReadOnlyList<Joint> joints, TrackingQualityThresholds thresholds = null)
        {
            _joints = joints;
            _t = thresholds ?? new TrackingQualityThresholds();
            Reset(0.0);
        }

        /// <summary>Clears the window. Call at the start of a still-hold to read jitter, or to re-arm the check.</summary>
        public void Reset(double wallTime)
        {
            _frames = 0;
            _firstWall = wallTime;
            _lastWall = wallTime;
            _maxFrameGap = 0;
            _lagSum = 0;
            _hasFrame = false;
            _states.Clear();
            for (int i = 0; i < _joints.Count; i++) _states[_joints[i]] = new JointState();
        }

        /// <summary>Record one delivered frame and the wall-clock time it reached Unity.</summary>
        public void Observe(in PoseFrame frame, double wallTime)
        {
            if (_hasFrame)
            {
                double gap = wallTime - _lastWall;
                if (gap > _maxFrameGap) _maxFrameGap = gap;
            }
            else
            {
                _firstWall = wallTime;
            }
            _lastWall = wallTime;
            _frames++;
            _hasFrame = true;
            _lagSum += wallTime - frame.Timestamp;

            for (int i = 0; i < _joints.Count; i++)
            {
                Joint j = _joints[i];
                Vector3 p = frame.Get(j);
                JointState s = _states[j];

                if (float.IsNaN(p.x) || float.IsNaN(p.y) || float.IsNaN(p.z) ||
                    float.IsInfinity(p.x) || float.IsInfinity(p.y) || float.IsInfinity(p.z))
                {
                    s.Invalid++;
                    continue; // don't fold a garbage sample into mean / freeze / jump
                }

                // Welford running mean + sum of squared distances -> jitter RMS.
                s.Samples++;
                Vector3 delta = p - s.Mean;
                s.Mean += delta / s.Samples;
                Vector3 delta2 = p - s.Mean;
                s.M2 += delta.x * delta2.x + delta.y * delta2.y + delta.z * delta2.z;

                if (s.HasLast)
                {
                    float moved = Vector3.Distance(p, s.LastPos);
                    if (moved > _t.JumpMeters) s.Jumps++;
                    if (moved < UnchangedEpsilon)
                    {
                        double freeze = wallTime - s.LastChangeWall; // position unchanged since last real move
                        if (freeze > s.MaxFreeze) s.MaxFreeze = freeze;
                    }
                    else
                    {
                        s.LastChangeWall = wallTime;
                    }
                }
                else
                {
                    s.LastChangeWall = wallTime;
                }

                s.LastPos = p;
                s.HasLast = true;
            }
        }

        /// <summary>Snapshot the metrics. <paramref name="wallTime"/> lets a still-ongoing gap/freeze count to "now".</summary>
        public TrackingQualityReport Report(double wallTime)
        {
            double window = _hasFrame ? wallTime - _firstWall : 0.0;
            float rate = window > 1e-6 ? (float)(_frames / window) : 0f;
            double liveGap = _hasFrame ? wallTime - _lastWall : 0.0;   // no frame since _lastWall counts too
            float maxGap = (float)System.Math.Max(_maxFrameGap, liveGap);
            float meanLag = _frames > 0 ? (float)(_lagSum / _frames) : 0f;

            var stats = new List<JointTrackingStats>(_joints.Count);
            bool allPass = _hasFrame && rate >= _t.MinDeliveryRateHz && maxGap <= _t.MaxFrameGapSeconds;

            for (int i = 0; i < _joints.Count; i++)
            {
                JointState s = _states[_joints[i]];

                float jitter = s.Samples > 1 ? Mathf.Sqrt((float)(s.M2 / (s.Samples - 1))) : 0f;
                double liveFreeze = s.HasLast ? wallTime - s.LastChangeWall : 0.0;
                float maxFreeze = (float)System.Math.Max(s.MaxFreeze, liveFreeze);

                bool jointPass = s.Samples > 0 && s.Invalid == 0 && s.Jumps == 0 && maxFreeze <= _t.MaxFreezeSeconds;
                if (!jointPass) allPass = false;

                stats.Add(new JointTrackingStats(_joints[i], s.Samples, jitter, maxFreeze, s.Jumps, s.Invalid, jointPass));
            }

            return new TrackingQualityReport(window, _frames, rate, maxGap, meanLag, stats, allPass);
        }
    }
}
