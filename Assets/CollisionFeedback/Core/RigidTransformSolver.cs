using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace CollisionFeedback.Core
{
    /// <summary>
    /// A solved rigid-body transform (rotation + translation, NO scale) between two tracking frames —
    /// e.g. Vicon room space → Unity/SteamVR space. Stored as three rotation-matrix ROWS plus a translation,
    /// deliberately avoiding quaternions so there is no Hamilton/left-handed convention ambiguity: applying it
    /// is plain linear algebra on coordinate triples.
    ///
    /// <see cref="RmsError"/> / <see cref="MaxError"/> are the fit residuals in metres and are the number that
    /// matters for this study: the collision detector decides contact at a <b>0.03 m</b> band
    /// (<see cref="DetectorParams.ContactDistance"/>), so an alignment whose residual approaches that value is
    /// injecting error the size of the measurement itself. Treat RMS ≤ ~0.005 m as good, ≤ 0.01 m as usable,
    /// and anything above ~0.015 m as a failed calibration to redo.
    /// </summary>
    public readonly struct RigidAlignment
    {
        public readonly Vector3 Row0, Row1, Row2;   // rotation matrix, row-major
        public readonly Vector3 Translation;         // metres
        public readonly float RmsError;              // metres, over the calibration samples
        public readonly float MaxError;              // metres, worst single sample
        public readonly int SampleCount;
        public readonly bool Valid;

        public RigidAlignment(Vector3 row0, Vector3 row1, Vector3 row2, Vector3 translation,
                              float rmsError, float maxError, int sampleCount, bool valid)
        {
            Row0 = row0; Row1 = row1; Row2 = row2; Translation = translation;
            RmsError = rmsError; MaxError = maxError; SampleCount = sampleCount; Valid = valid;
        }

        /// <summary>The do-nothing transform — used when no calibration is loaded (source frame == target frame).</summary>
        public static RigidAlignment Identity => new(
            new Vector3(1f, 0f, 0f), new Vector3(0f, 1f, 0f), new Vector3(0f, 0f, 1f),
            Vector3.zero, 0f, 0f, 0, true);

        /// <summary>Map a point from the SOURCE frame (e.g. Vicon) into the TARGET frame (e.g. Unity/SteamVR).</summary>
        public Vector3 Apply(Vector3 p) => new(
            Row0.x * p.x + Row0.y * p.y + Row0.z * p.z + Translation.x,
            Row1.x * p.x + Row1.y * p.y + Row1.z * p.z + Translation.y,
            Row2.x * p.x + Row2.y * p.y + Row2.z * p.z + Translation.z);

        /// <summary>Map a point back from TARGET to SOURCE (rotation inverse = transpose).</summary>
        public Vector3 ApplyInverse(Vector3 p)
        {
            Vector3 d = p - Translation;
            return new Vector3(
                Row0.x * d.x + Row1.x * d.y + Row2.x * d.z,
                Row0.y * d.x + Row1.y * d.y + Row2.y * d.z,
                Row0.z * d.x + Row1.z * d.y + Row2.z * d.z);
        }

        /// <summary>The inverse transform as its own alignment (source↔target swapped).</summary>
        public RigidAlignment Inverted()
        {
            var c0 = new Vector3(Row0.x, Row1.x, Row2.x);
            var c1 = new Vector3(Row0.y, Row1.y, Row2.y);
            var c2 = new Vector3(Row0.z, Row1.z, Row2.z);
            var t = new Vector3(
                -(c0.x * Translation.x + c0.y * Translation.y + c0.z * Translation.z),
                -(c1.x * Translation.x + c1.y * Translation.y + c1.z * Translation.z),
                -(c2.x * Translation.x + c2.y * Translation.y + c2.z * Translation.z));
            return new RigidAlignment(c0, c1, c2, t, RmsError, MaxError, SampleCount, Valid);
        }

        /// <summary>
        /// Serialize as a 4×4 homogeneous matrix, one row per line — the same shape the
        /// <c>transformation_matrix_new_vicon_vive.ipynb</c> notebook prints, so matrices move both ways.
        /// A leading comment line records the fit quality.
        /// </summary>
        public string ToMatrixText()
        {
            // NOTE: build the header with explicit invariant formatting — Unity's .NET Standard 2.1 profile has
            // no StringBuilder.Append(IFormatProvider, ...) interpolated-string overload (that is .NET 6+).
            var sb = new StringBuilder();
            sb.Append("# rigid alignment  samples=")
              .Append(SampleCount.ToString(CultureInfo.InvariantCulture))
              .Append("  rms=").Append(RmsError.ToString("F5", CultureInfo.InvariantCulture))
              .Append(" m  max=").Append(MaxError.ToString("F5", CultureInfo.InvariantCulture))
              .Append(" m\n");
            AppendRow(sb, Row0, Translation.x);
            AppendRow(sb, Row1, Translation.y);
            AppendRow(sb, Row2, Translation.z);
            sb.Append("0 0 0 1\n");
            return sb.ToString();
        }

        private static void AppendRow(StringBuilder sb, Vector3 r, float t) =>
            sb.Append(r.x.ToString("R", CultureInfo.InvariantCulture)).Append(' ')
              .Append(r.y.ToString("R", CultureInfo.InvariantCulture)).Append(' ')
              .Append(r.z.ToString("R", CultureInfo.InvariantCulture)).Append(' ')
              .Append(t.ToString("R", CultureInfo.InvariantCulture)).Append('\n');

        /// <summary>
        /// Parse a 4×4 homogeneous matrix. Tolerant of the notebook/NumPy styles: '#' comments, blank lines,
        /// and '[', ']', ',' as noise — so a matrix pasted straight out of the Python print works. Needs at
        /// least 12 numbers (the last row is assumed 0 0 0 1). Returns false rather than throwing.
        /// </summary>
        public static bool TryParseMatrix(string text, out RigidAlignment alignment)
        {
            alignment = Identity;
            if (string.IsNullOrWhiteSpace(text)) return false;

            var nums = new List<float>(16);
            foreach (string rawLine in text.Split('\n'))
            {
                string line = rawLine.Trim();
                if (line.Length == 0 || line[0] == '#') continue;
                line = line.Replace('[', ' ').Replace(']', ' ').Replace(',', ' ');
                foreach (string tok in line.Split(' ', '\t', '\r'))
                {
                    if (tok.Length == 0) continue;
                    if (float.TryParse(tok, NumberStyles.Float, CultureInfo.InvariantCulture, out float v))
                        nums.Add(v);
                }
            }
            if (nums.Count < 12) return false;

            alignment = new RigidAlignment(
                new Vector3(nums[0], nums[1], nums[2]),
                new Vector3(nums[4], nums[5], nums[6]),
                new Vector3(nums[8], nums[9], nums[10]),
                new Vector3(nums[3], nums[7], nums[11]),
                0f, 0f, 0, true);
            return true;
        }
    }

    /// <summary>
    /// Solves the optimal rigid transform between two corresponding point sets (Horn's unit-quaternion method,
    /// equivalent to the Kabsch/SVD fit in <c>transformation_matrix_new_vicon_vive.ipynb</c> cell 0, but
    /// implemented without an SVD dependency and — critically — <b>reporting the fit residual</b>).
    ///
    /// USE: co-register a Vicon room frame with the Unity/SteamVR frame. Put a marker cluster on the HMD, then
    /// sample matched pairs (HMD position as SteamVR reports it, HMD cluster position as Vicon reports it) while
    /// moving the headset around the capture volume. Solve → a Vicon→Unity transform whose residual tells you
    /// whether the registration is tight enough to trust at the study's 0.03 m contact band.
    ///
    /// Pure, deterministic, hardware-free (Core) — unit-tested against synthetic transforms with known answers.
    /// Rotation only, NO scale: convert units (Vicon commonly streams millimetres) BEFORE solving, or the fit
    /// is meaningless. Needs ≥ 3 non-collinear pairs; spread them over the whole volume, not one corner.
    /// </summary>
    public static class RigidTransformSolver
    {
        /// <summary>Minimum correspondences for a determined fit (3 non-collinear points fix a rigid body).</summary>
        public const int MinSamples = 3;

        /// <summary>
        /// Solve for the transform mapping <paramref name="source"/> onto <paramref name="target"/>
        /// (target ≈ R·source + t), minimizing squared residuals.
        /// Returns an invalid alignment (Valid = false) for too-few, mismatched, or degenerate inputs.
        /// </summary>
        public static RigidAlignment Solve(IReadOnlyList<Vector3> source, IReadOnlyList<Vector3> target)
        {
            if (source == null || target == null) return Invalid();
            int n = source.Count;
            if (n != target.Count || n < MinSamples) return Invalid();

            // Centroids.
            Vector3 cs = Vector3.zero, ct = Vector3.zero;
            for (int i = 0; i < n; i++) { cs += source[i]; ct += target[i]; }
            cs /= n; ct /= n;

            // Cross-covariance S = Σ (source-cs)(target-ct)^T.
            double sxx = 0, sxy = 0, sxz = 0, syx = 0, syy = 0, syz = 0, szx = 0, szy = 0, szz = 0;
            for (int i = 0; i < n; i++)
            {
                Vector3 p = source[i] - cs, q = target[i] - ct;
                sxx += (double)p.x * q.x; sxy += (double)p.x * q.y; sxz += (double)p.x * q.z;
                syx += (double)p.y * q.x; syy += (double)p.y * q.y; syz += (double)p.y * q.z;
                szx += (double)p.z * q.x; szy += (double)p.z * q.y; szz += (double)p.z * q.z;
            }

            // Horn's symmetric 4×4; its dominant eigenvector is the optimal rotation quaternion (w,x,y,z).
            var nMat = new double[4, 4];
            nMat[0, 0] = sxx + syy + szz;
            nMat[0, 1] = nMat[1, 0] = syz - szy;
            nMat[0, 2] = nMat[2, 0] = szx - sxz;
            nMat[0, 3] = nMat[3, 0] = sxy - syx;
            nMat[1, 1] = sxx - syy - szz;
            nMat[1, 2] = nMat[2, 1] = sxy + syx;
            nMat[1, 3] = nMat[3, 1] = szx + sxz;
            nMat[2, 2] = -sxx + syy - szz;
            nMat[2, 3] = nMat[3, 2] = syz + szy;
            nMat[3, 3] = -sxx - syy + szz;

            if (!JacobiEigen(nMat, out double[] eigenvalues, out double[,] eigenvectors)) return Invalid();

            int best = 0;
            for (int i = 1; i < 4; i++) if (eigenvalues[i] > eigenvalues[best]) best = i;

            double qw = eigenvectors[0, best], qx = eigenvectors[1, best],
                   qy = eigenvectors[2, best], qz = eigenvectors[3, best];
            double norm = Math.Sqrt(qw * qw + qx * qx + qy * qy + qz * qz);
            if (norm < 1e-12) return Invalid();
            qw /= norm; qx /= norm; qy /= norm; qz /= norm;

            // Quaternion → rotation matrix rows (standard Hamilton form; pure linear algebra on the triples,
            // so it is independent of the engine's handedness convention).
            var row0 = new Vector3(
                (float)(1 - 2 * (qy * qy + qz * qz)), (float)(2 * (qx * qy - qw * qz)), (float)(2 * (qx * qz + qw * qy)));
            var row1 = new Vector3(
                (float)(2 * (qx * qy + qw * qz)), (float)(1 - 2 * (qx * qx + qz * qz)), (float)(2 * (qy * qz - qw * qx)));
            var row2 = new Vector3(
                (float)(2 * (qx * qz - qw * qy)), (float)(2 * (qy * qz + qw * qx)), (float)(1 - 2 * (qx * qx + qy * qy)));

            // t = ct − R·cs
            var rotatedCs = new Vector3(
                row0.x * cs.x + row0.y * cs.y + row0.z * cs.z,
                row1.x * cs.x + row1.y * cs.y + row1.z * cs.z,
                row2.x * cs.x + row2.y * cs.y + row2.z * cs.z);
            Vector3 t = ct - rotatedCs;

            var fit = new RigidAlignment(row0, row1, row2, t, 0f, 0f, n, true);

            // Residuals — the number that decides whether this calibration is usable.
            double sumSq = 0; float worst = 0f;
            for (int i = 0; i < n; i++)
            {
                float e = Vector3.Distance(fit.Apply(source[i]), target[i]);
                sumSq += (double)e * e;
                if (e > worst) worst = e;
            }
            float rms = (float)Math.Sqrt(sumSq / n);

            return new RigidAlignment(row0, row1, row2, t, rms, worst, n, true);
        }

        private static RigidAlignment Invalid()
        {
            RigidAlignment id = RigidAlignment.Identity;
            return new RigidAlignment(id.Row0, id.Row1, id.Row2, id.Translation, float.NaN, float.NaN, 0, false);
        }

        // Jacobi eigenvalue iteration for a symmetric 4×4 (Numerical Recipes form). Returns eigenvalues in w
        // and eigenvectors as COLUMNS of v. Small, dependency-free, and plenty accurate at this size.
        private static bool JacobiEigen(double[,] a, out double[] w, out double[,] v)
        {
            const int n = 4;
            w = new double[n];
            v = new double[n, n];
            for (int i = 0; i < n; i++) { v[i, i] = 1.0; w[i] = a[i, i]; }

            var b = new double[n];
            var z = new double[n];
            for (int i = 0; i < n; i++) b[i] = w[i];

            for (int sweep = 0; sweep < 64; sweep++)
            {
                double sm = 0;
                for (int p = 0; p < n - 1; p++)
                    for (int q = p + 1; q < n; q++) sm += Math.Abs(a[p, q]);
                if (sm < 1e-18) return true;               // converged

                double tresh = (sweep < 3) ? 0.2 * sm / (n * n) : 0.0;

                for (int p = 0; p < n - 1; p++)
                {
                    for (int q = p + 1; q < n; q++)
                    {
                        double g = 100.0 * Math.Abs(a[p, q]);
                        if (sweep > 3 && Math.Abs(w[p]) + g == Math.Abs(w[p])
                                      && Math.Abs(w[q]) + g == Math.Abs(w[q]))
                        {
                            a[p, q] = 0.0;
                        }
                        else if (Math.Abs(a[p, q]) > tresh)
                        {
                            double h = w[q] - w[p], t;
                            if (Math.Abs(h) + g == Math.Abs(h)) t = a[p, q] / h;
                            else
                            {
                                double theta = 0.5 * h / a[p, q];
                                t = 1.0 / (Math.Abs(theta) + Math.Sqrt(1.0 + theta * theta));
                                if (theta < 0.0) t = -t;
                            }
                            double c = 1.0 / Math.Sqrt(1 + t * t);
                            double s = t * c;
                            double tau = s / (1.0 + c);
                            h = t * a[p, q];
                            z[p] -= h; z[q] += h; w[p] -= h; w[q] += h;
                            a[p, q] = 0.0;

                            for (int j = 0; j < p; j++) Rot(a, j, p, j, q, s, tau);
                            for (int j = p + 1; j < q; j++) Rot(a, p, j, j, q, s, tau);
                            for (int j = q + 1; j < n; j++) Rot(a, p, j, q, j, s, tau);
                            for (int j = 0; j < n; j++) Rot(v, j, p, j, q, s, tau);
                        }
                    }
                }
                for (int i = 0; i < n; i++) { b[i] += z[i]; w[i] = b[i]; z[i] = 0.0; }
            }
            return true; // hit the sweep cap; the result is still the best estimate found
        }

        private static void Rot(double[,] m, int i, int j, int k, int l, double s, double tau)
        {
            double g = m[i, j], h = m[k, l];
            m[i, j] = g - s * (h + g * tau);
            m[k, l] = h + s * (g - h * tau);
        }
    }
}
