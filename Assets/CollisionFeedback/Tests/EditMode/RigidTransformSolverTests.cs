using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CollisionFeedback.Core;

namespace CollisionFeedback.Tests
{
    /// <summary>
    /// Verifies the Vicon→Unity alignment solver against synthetic transforms with known answers: exact
    /// recovery when noise-free, a residual that tracks injected noise (the calibration-quality signal the
    /// original notebook never reported), invertibility, degenerate-input rejection, and round-tripping through
    /// the notebook-compatible 4×4 matrix text.
    /// </summary>
    public class RigidTransformSolverTests
    {
        private System.Random _rng;

        [SetUp]
        public void SetUp() => _rng = new System.Random(20260716);

        private Vector3 Rand(float s) => new(
            (float)(_rng.NextDouble() * 2 - 1) * s,
            (float)(_rng.NextDouble() * 2 - 1) * s,
            (float)(_rng.NextDouble() * 2 - 1) * s);

        // Rotation matrix rows for R = Rz*Ry*Rx.
        private static (Vector3 r0, Vector3 r1, Vector3 r2) Rot(double rx, double ry, double rz)
        {
            double cx = Math.Cos(rx), sx = Math.Sin(rx);
            double cy = Math.Cos(ry), sy = Math.Sin(ry);
            double cz = Math.Cos(rz), sz = Math.Sin(rz);
            return (new Vector3((float)(cz * cy), (float)(cz * sy * sx - sz * cx), (float)(cz * sy * cx + sz * sx)),
                    new Vector3((float)(sz * cy), (float)(sz * sy * sx + cz * cx), (float)(sz * sy * cx - cz * sx)),
                    new Vector3((float)(-sy), (float)(cy * sx), (float)(cy * cx)));
        }

        private static Vector3 Apply((Vector3 r0, Vector3 r1, Vector3 r2) R, Vector3 p, Vector3 t) => new(
            R.r0.x * p.x + R.r0.y * p.y + R.r0.z * p.z + t.x,
            R.r1.x * p.x + R.r1.y * p.y + R.r1.z * p.z + t.y,
            R.r2.x * p.x + R.r2.y * p.y + R.r2.z * p.z + t.z);

        [Test]
        public void Recovers_a_known_rotation_and_translation_exactly()
        {
            for (int trial = 0; trial < 5; trial++)
            {
                var R = Rot(_rng.NextDouble() * 2 - 1, _rng.NextDouble() * 2 - 1, _rng.NextDouble() * 2 - 1);
                Vector3 t = Rand(2f);
                var src = new List<Vector3>();
                var dst = new List<Vector3>();
                for (int i = 0; i < 40; i++)
                {
                    Vector3 p = Rand(1.75f);        // spread across a 3.5 m arena
                    src.Add(p);
                    dst.Add(Apply(R, p, t));
                }

                RigidAlignment fit = RigidTransformSolver.Solve(src, dst);

                Assert.That(fit.Valid, Is.True);
                Assert.That(fit.RmsError, Is.LessThan(1e-4f), $"trial {trial} rms");
                for (int i = 0; i < src.Count; i++)
                    Assert.That(Vector3.Distance(fit.Apply(src[i]), dst[i]), Is.LessThan(1e-3f));
            }
        }

        [Test]
        public void Reported_residual_tracks_injected_noise()
        {
            foreach (float noise in new[] { 0.001f, 0.005f, 0.02f })
            {
                var R = Rot(0.4, -0.7, 1.1);
                var t = new Vector3(1.2f, -0.4f, 2.3f);
                var src = new List<Vector3>();
                var dst = new List<Vector3>();
                for (int i = 0; i < 200; i++)
                {
                    Vector3 p = Rand(1.75f);
                    src.Add(p);
                    dst.Add(Apply(R, p, t) + Rand(noise));
                }

                RigidAlignment fit = RigidTransformSolver.Solve(src, dst);
                Assert.That(fit.RmsError, Is.GreaterThan(noise * 0.4f).And.LessThan(noise * 1.8f),
                            $"noise {noise}: reported rms {fit.RmsError}");
                Assert.That(fit.MaxError, Is.GreaterThanOrEqualTo(fit.RmsError));
            }
        }

        [Test]
        public void Inverse_round_trips()
        {
            var R = Rot(-0.9, 0.3, 0.6);
            var t = new Vector3(-0.7f, 1.4f, 0.2f);
            var src = new List<Vector3>();
            var dst = new List<Vector3>();
            for (int i = 0; i < 30; i++) { Vector3 p = Rand(1.5f); src.Add(p); dst.Add(Apply(R, p, t)); }

            RigidAlignment fit = RigidTransformSolver.Solve(src, dst);
            foreach (Vector3 p in src)
            {
                Assert.That(Vector3.Distance(fit.ApplyInverse(fit.Apply(p)), p), Is.LessThan(1e-3f));
                Assert.That(Vector3.Distance(fit.Inverted().Apply(fit.Apply(p)), p), Is.LessThan(1e-3f));
            }
        }

        [Test]
        public void Three_non_collinear_points_are_enough()
        {
            var R = Rot(0.5, 0.5, 0.5);
            var t = new Vector3(1f, 2f, 3f);
            var src = new List<Vector3> { new(0, 0, 0), new(1, 0, 0), new(0, 1, 0) };
            var dst = new List<Vector3>();
            foreach (Vector3 p in src) dst.Add(Apply(R, p, t));

            RigidAlignment fit = RigidTransformSolver.Solve(src, dst);
            Assert.That(fit.Valid, Is.True);
            Assert.That(fit.RmsError, Is.LessThan(1e-3f));
            Assert.That(fit.SampleCount, Is.EqualTo(3));
        }

        [Test]
        public void Rejects_degenerate_input()
        {
            Assert.That(RigidTransformSolver.Solve(null, null).Valid, Is.False);
            Assert.That(RigidTransformSolver.Solve(new List<Vector3> { Vector3.zero },
                                                   new List<Vector3> { Vector3.zero }).Valid, Is.False);
            Assert.That(RigidTransformSolver.Solve(new List<Vector3> { Vector3.zero, Vector3.one, Vector3.up },
                                                   new List<Vector3> { Vector3.zero, Vector3.one }).Valid, Is.False);
        }

        [Test]
        public void Identity_alignment_is_a_no_op()
        {
            var p = new Vector3(0.35f, 1.2f, -0.8f);
            Assert.That(Vector3.Distance(RigidAlignment.Identity.Apply(p), p), Is.LessThan(1e-6f));
            Assert.That(RigidAlignment.Identity.Valid, Is.True);
        }

        [Test]
        public void Matrix_text_round_trips()
        {
            var R = Rot(0.2, 0.9, -0.5);
            var t = new Vector3(0.3f, -1.1f, 2.0f);
            var src = new List<Vector3>();
            var dst = new List<Vector3>();
            for (int i = 0; i < 20; i++) { Vector3 p = Rand(1.5f); src.Add(p); dst.Add(Apply(R, p, t)); }
            RigidAlignment fit = RigidTransformSolver.Solve(src, dst);

            Assert.That(RigidAlignment.TryParseMatrix(fit.ToMatrixText(), out RigidAlignment back), Is.True);
            foreach (Vector3 p in src)
                Assert.That(Vector3.Distance(back.Apply(p), fit.Apply(p)), Is.LessThan(1e-3f));
        }

        [Test]
        public void Parses_the_numpy_matrix_style_from_the_calibration_notebook()
        {
            // Exactly what transformation_matrix_new_vicon_vive.ipynb prints.
            string numpy = "[[ 0.6292178  -0.64570664 -0.43260594  2.21457465]\n" +
                           " [ 0.61071103  0.06646771  0.78905899 -0.23986469]\n" +
                           " [-0.4807463  -0.76068718  0.43616283  2.18952958]\n" +
                           " [ 0.          0.          0.          1.        ]]";

            Assert.That(RigidAlignment.TryParseMatrix(numpy, out RigidAlignment a), Is.True);
            Assert.That(a.Translation.x, Is.EqualTo(2.21457465f).Within(1e-5f));
            Assert.That(a.Translation.y, Is.EqualTo(-0.23986469f).Within(1e-5f));
            Assert.That(a.Translation.z, Is.EqualTo(2.18952958f).Within(1e-5f));
            Assert.That(a.Row0.x, Is.EqualTo(0.6292178f).Within(1e-5f));

            Assert.That(RigidAlignment.TryParseMatrix("garbage", out _), Is.False);
            Assert.That(RigidAlignment.TryParseMatrix(null, out _), Is.False);
        }
    }
}
