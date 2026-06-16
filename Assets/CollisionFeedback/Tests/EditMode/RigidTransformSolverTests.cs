using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CollisionFeedback.Core;

namespace CollisionFeedback.Tests
{
    public class RigidTransformSolverTests
    {
        private static readonly List<Vector3> Source = new()
        {
            new Vector3(0f, 0f, 0f), new Vector3(1f, 0f, 0f), new Vector3(0f, 1f, 0f),
            new Vector3(0f, 0f, 1f), new Vector3(1f, 1f, 0f), new Vector3(0.5f, 0.3f, 0.8f),
        };

        [Test]
        public void Recovers_a_known_rotation_and_translation()
        {
            var knownQ = Quaternion.AngleAxis(40f, new Vector3(0.3f, 1f, 0.2f).normalized);
            var knownT = new Vector3(2f, -1f, 0.5f);

            var target = new List<Vector3>();
            foreach (var p in Source) target.Add(knownQ * p + knownT);

            var solved = RigidTransformSolver.Solve(Source, target);

            foreach (var p in Source)
            {
                Vector3 got = solved.Apply(p);
                Vector3 want = knownQ * p + knownT;
                Assert.That((got - want).magnitude, Is.LessThan(1e-3f), $"point {p}");
            }
        }

        [Test]
        public void Identity_correspondence_yields_identity_transform()
        {
            var solved = RigidTransformSolver.Solve(Source, Source);
            foreach (var p in Source)
                Assert.That((solved.Apply(p) - p).magnitude, Is.LessThan(1e-3f));
        }

        [Test]
        public void Recovers_a_pure_180_degree_rotation()
        {
            // 180° rotations are the classic failure case for naive quaternion starts.
            var knownQ = Quaternion.AngleAxis(180f, Vector3.up);
            var knownT = new Vector3(-1f, 0.2f, 3f);

            var target = new List<Vector3>();
            foreach (var p in Source) target.Add(knownQ * p + knownT);

            var solved = RigidTransformSolver.Solve(Source, target);
            foreach (var p in Source)
                Assert.That((solved.Apply(p) - (knownQ * p + knownT)).magnitude, Is.LessThan(1e-3f));
        }
    }
}
