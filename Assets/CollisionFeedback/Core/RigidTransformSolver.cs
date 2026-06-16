using System;
using System.Collections.Generic;
using UnityEngine;

namespace CollisionFeedback.Core
{
    /// <summary>A rigid (rotation + translation) transform. Rotation stored as explicit matrix rows so
    /// application is convention-safe (no dependency on any engine's quaternion handedness).</summary>
    public readonly struct RigidTransform
    {
        public readonly Vector3 Row0, Row1, Row2;   // rotation matrix rows
        public readonly Vector3 Translation;

        public RigidTransform(Vector3 row0, Vector3 row1, Vector3 row2, Vector3 translation)
        {
            Row0 = row0; Row1 = row1; Row2 = row2; Translation = translation;
        }

        public Vector3 Apply(Vector3 p) => new Vector3(
            Vector3.Dot(Row0, p), Vector3.Dot(Row1, p), Vector3.Dot(Row2, p)) + Translation;

        public static RigidTransform Identity =>
            new RigidTransform(new Vector3(1, 0, 0), new Vector3(0, 1, 0), new Vector3(0, 0, 1), Vector3.zero);
    }

    /// <summary>
    /// Solves the camera-rig -> VR-frame rigid transform from N>=3 non-collinear point correspondences
    /// (the "touch a tracked controller to marked points" step) [3C.2]. Least-squares optimal via Horn's
    /// quaternion method: the rotation is the dominant eigenvector of a 4x4 symmetric matrix, found by
    /// shifted power iteration. Pure + deterministic; the physical point-touching stays with you.
    /// </summary>
    public static class RigidTransformSolver
    {
        public static RigidTransform Solve(IReadOnlyList<Vector3> source, IReadOnlyList<Vector3> target)
        {
            int n = source.Count;

            Vector3 cs = Vector3.zero, ct = Vector3.zero;
            for (int i = 0; i < n; i++) { cs += source[i]; ct += target[i]; }
            cs /= n; ct /= n;

            // Cross-covariance S = sum (p-cs)(q-ct)^T.
            double Sxx = 0, Sxy = 0, Sxz = 0, Syx = 0, Syy = 0, Syz = 0, Szx = 0, Szy = 0, Szz = 0;
            for (int i = 0; i < n; i++)
            {
                Vector3 p = source[i] - cs;
                Vector3 q = target[i] - ct;
                Sxx += p.x * q.x; Sxy += p.x * q.y; Sxz += p.x * q.z;
                Syx += p.y * q.x; Syy += p.y * q.y; Syz += p.y * q.z;
                Szx += p.z * q.x; Szy += p.z * q.y; Szz += p.z * q.z;
            }

            // Horn's symmetric 4x4 N; its top eigenvector is the optimal rotation quaternion (w,x,y,z).
            var N = new double[4, 4];
            N[0, 0] = Sxx + Syy + Szz; N[0, 1] = Syz - Szy;        N[0, 2] = Szx - Sxz;        N[0, 3] = Sxy - Syx;
            N[1, 0] = N[0, 1];         N[1, 1] = Sxx - Syy - Szz;  N[1, 2] = Sxy + Syx;        N[1, 3] = Szx + Sxz;
            N[2, 0] = N[0, 2];         N[2, 1] = N[1, 2];          N[2, 2] = -Sxx + Syy - Szz; N[2, 3] = Syz + Szy;
            N[3, 0] = N[0, 3];         N[3, 1] = N[1, 3];          N[3, 2] = N[2, 3];          N[3, 3] = -Sxx - Syy + Szz;

            double[] e = DominantEigenvector(N);
            double w = e[0], x = e[1], y = e[2], z = e[3];
            double inv = 1.0 / Math.Sqrt(w * w + x * x + y * y + z * z);
            w *= inv; x *= inv; y *= inv; z *= inv;

            // Standard (Hamilton) active rotation matrix from the unit quaternion.
            var r0 = new Vector3((float)(1 - 2 * (y * y + z * z)), (float)(2 * (x * y - w * z)),   (float)(2 * (x * z + w * y)));
            var r1 = new Vector3((float)(2 * (x * y + w * z)),   (float)(1 - 2 * (x * x + z * z)), (float)(2 * (y * z - w * x)));
            var r2 = new Vector3((float)(2 * (x * z - w * y)),   (float)(2 * (y * z + w * x)),   (float)(1 - 2 * (x * x + y * y)));

            var rcs = new Vector3(Vector3.Dot(r0, cs), Vector3.Dot(r1, cs), Vector3.Dot(r2, cs));
            return new RigidTransform(r0, r1, r2, ct - rcs);
        }

        /// <summary>Dominant eigenvector of a 4x4 symmetric matrix via power iteration on a positive-shifted
        /// copy (so "largest magnitude" == "largest eigenvalue" of the original).</summary>
        private static double[] DominantEigenvector(double[,] N)
        {
            // Tight positive shift: just enough that N + cI is positive definite (Gershgorin lower bound).
            // A SMALL shift preserves the eigenvalue gap, so power iteration converges fast.
            double minEigLowerBound = double.PositiveInfinity;
            for (int i = 0; i < 4; i++)
            {
                double offDiag = 0;
                for (int j = 0; j < 4; j++) if (j != i) offDiag += Math.Abs(N[i, j]);
                double lb = N[i, i] - offDiag;
                if (lb < minEigLowerBound) minEigLowerBound = lb;
            }
            double c = (minEigLowerBound < 0 ? -minEigLowerBound : 0) + 1e-6;

            var M = new double[4, 4];
            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 4; j++)
                    M[i, j] = N[i, j] + (i == j ? c : 0);

            double[] v = { 1.0, 0.7, 0.5, 0.3 }; // fixed non-degenerate start (deterministic)
            Normalize(v);
            for (int iter = 0; iter < 300; iter++)
            {
                double[] u = MatVec(M, v);
                Normalize(u);
                v = u;
            }
            return v;
        }

        private static double[] MatVec(double[,] m, double[] v)
        {
            var r = new double[4];
            for (int i = 0; i < 4; i++)
                r[i] = m[i, 0] * v[0] + m[i, 1] * v[1] + m[i, 2] * v[2] + m[i, 3] * v[3];
            return r;
        }

        private static void Normalize(double[] v)
        {
            double s = Math.Sqrt(v[0] * v[0] + v[1] * v[1] + v[2] * v[2] + v[3] * v[3]);
            if (s > 0) { v[0] /= s; v[1] /= s; v[2] /= s; v[3] /= s; }
        }
    }
}
