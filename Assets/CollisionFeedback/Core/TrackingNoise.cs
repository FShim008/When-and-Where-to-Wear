using System;
using System.Collections.Generic;
using UnityEngine;

namespace CollisionFeedback.Core
{
    /// <summary>
    /// Injects synthetic tracking error into recorded <see cref="PoseFrame"/>s so the warning logic can be
    /// re-run offline as if it had been driven by a less accurate tracker [robustness analysis].
    ///
    /// WHY THIS EXISTS: the study's predictive conditions run on an idealized oracle (near-perfect tracking),
    /// which is a deliberate upper-bound framing — but it invites the obvious objection *"of course prediction
    /// works when tracking is perfect."* Because every session logs raw keypoints at frame rate, that objection
    /// can be answered empirically: replay the SAME recorded motion through the SAME warning logic at
    /// increasing tracking error and report how far the predictive advantage survives.
    ///
    /// Two error modes, both present in real trackers:
    ///  • <b>Jitter</b> — zero-mean Gaussian noise, independent per joint per frame (σ = <see cref="Sigma"/>).
    ///  • <b>Bias</b> — a fixed per-joint offset drawn once per run (‖·‖ ≈ <see cref="BiasMagnitude"/>), modelling
    ///    registration / mount error that does not average out over time.
    ///
    /// Deterministic for a given seed, so an analysis is exactly reproducible. Pure Core: no I/O, no Unity
    /// runtime dependency beyond the math types.
    /// </summary>
    public sealed class TrackingNoise
    {
        private readonly System.Random _rng;
        private readonly Vector3[] _bias = new Vector3[JointInfo.Count];
        private double _spare;
        private bool _hasSpare;

        /// <summary>Per-axis standard deviation of the per-frame jitter, in metres.</summary>
        public float Sigma { get; }

        /// <summary>Magnitude of the fixed per-joint offset, in metres (0 = no bias).</summary>
        public float BiasMagnitude { get; }

        public TrackingNoise(float sigmaMeters, int seed, float biasMagnitudeMeters = 0f)
        {
            Sigma = Math.Max(0f, sigmaMeters);
            BiasMagnitude = Math.Max(0f, biasMagnitudeMeters);
            _rng = new System.Random(seed);

            // Draw one fixed offset per joint, in a uniformly random direction.
            for (int j = 0; j < JointInfo.Count; j++)
            {
                if (BiasMagnitude <= 0f) { _bias[j] = Vector3.zero; continue; }
                var dir = new Vector3((float)Gaussian(), (float)Gaussian(), (float)Gaussian());
                float len = dir.magnitude;
                _bias[j] = len > 1e-6f ? dir * (BiasMagnitude / len) : Vector3.zero;
            }
        }

        /// <summary>A copy of <paramref name="frame"/> with jitter + bias applied to every joint.
        /// Timestamps are untouched — only the geometry degrades.</summary>
        public PoseFrame Apply(in PoseFrame frame)
        {
            var joints = new Vector3[JointInfo.Count];
            for (int j = 0; j < JointInfo.Count; j++)
            {
                Vector3 p = frame.Joints != null && j < frame.Joints.Length ? frame.Joints[j] : Vector3.zero;
                if (Sigma > 0f)
                    p += new Vector3((float)(Gaussian() * Sigma),
                                     (float)(Gaussian() * Sigma),
                                     (float)(Gaussian() * Sigma));
                joints[j] = p + _bias[j];
            }
            return new PoseFrame { Timestamp = frame.Timestamp, Joints = joints };
        }

        /// <summary>Apply to a whole recorded sequence (one noise stream, in order).</summary>
        public List<PoseFrame> Apply(IReadOnlyList<PoseFrame> frames)
        {
            var outFrames = new List<PoseFrame>(frames.Count);
            for (int i = 0; i < frames.Count; i++) outFrames.Add(Apply(frames[i]));
            return outFrames;
        }

        // Standard normal via Box-Muller, caching the second variate.
        private double Gaussian()
        {
            if (_hasSpare) { _hasSpare = false; return _spare; }
            double u1, u2;
            do { u1 = _rng.NextDouble(); } while (u1 <= double.Epsilon);
            u2 = _rng.NextDouble();
            double mag = Math.Sqrt(-2.0 * Math.Log(u1));
            _spare = mag * Math.Sin(2.0 * Math.PI * u2);
            _hasSpare = true;
            return mag * Math.Cos(2.0 * Math.PI * u2);
        }
    }
}
