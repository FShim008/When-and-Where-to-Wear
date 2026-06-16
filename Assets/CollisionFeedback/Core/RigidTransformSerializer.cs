using System;
using System.Globalization;
using UnityEngine;

namespace CollisionFeedback.Core
{
    /// <summary>
    /// Text (de)serialization for a <see cref="RigidTransform"/>: 12 invariant-culture floats in order
    /// Row0(x y z) Row1(x y z) Row2(x y z) Translation(x y z). Pure + testable; the actual file I/O lives in
    /// Runtime (<c>CameraVrCalibrationFile</c>) so Core never touches the filesystem.
    /// </summary>
    public static class RigidTransformSerializer
    {
        public static string Format(in RigidTransform t)
        {
            var inv = CultureInfo.InvariantCulture;
            float[] v =
            {
                t.Row0.x, t.Row0.y, t.Row0.z,
                t.Row1.x, t.Row1.y, t.Row1.z,
                t.Row2.x, t.Row2.y, t.Row2.z,
                t.Translation.x, t.Translation.y, t.Translation.z,
            };
            var parts = new string[12];
            for (int i = 0; i < 12; i++) parts[i] = v[i].ToString("R", inv);
            return string.Join(" ", parts);
        }

        public static bool TryParse(string text, out RigidTransform transform)
        {
            transform = RigidTransform.Identity;
            if (string.IsNullOrWhiteSpace(text)) return false;

            string[] tok = text.Split(new[] { ' ', ',', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            if (tok.Length != 12) return false;

            var inv = CultureInfo.InvariantCulture;
            var f = new float[12];
            for (int i = 0; i < 12; i++)
                if (!float.TryParse(tok[i], NumberStyles.Float, inv, out f[i])) return false;

            transform = new RigidTransform(
                new Vector3(f[0], f[1], f[2]),
                new Vector3(f[3], f[4], f[5]),
                new Vector3(f[6], f[7], f[8]),
                new Vector3(f[9], f[10], f[11]));
            return true;
        }
    }
}
