using System.IO;
using UnityEngine;
using CollisionFeedback.Core;

namespace CollisionFeedback.Runtime
{
    /// <summary>
    /// File seam for the Vicon→Unity rigid alignment. Parsing/formatting lives in <see cref="RigidAlignment"/>
    /// (Core, unit-tested); this is the thin Runtime IO around it, mirroring <see cref="CueIntensityFile"/>.
    ///
    /// Relative names resolve under <c>Application.persistentDataPath</c>; absolute paths are used as-is. The
    /// format is a 4×4 homogeneous matrix, one row per line, which is ALSO what
    /// <c>transformation_matrix_new_vicon_vive.ipynb</c> prints — so a matrix produced in the notebook can be
    /// pasted into this file verbatim (brackets and commas are tolerated).
    ///
    /// A missing or unreadable file falls back to IDENTITY and logs an ERROR — unlike the cue-intensity table,
    /// an unset alignment is NOT a safe default: it silently means "Vicon coordinates are Unity coordinates",
    /// which they are not, so every collision measurement would be wrong. Never throws.
    /// </summary>
    public static class ViconAlignmentFile
    {
        public const string DefaultName = "vicon_alignment.txt";

        public static string DefaultPath => Path.Combine(Application.persistentDataPath, DefaultName);

        private static string Resolve(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return DefaultPath;
            return Path.IsPathRooted(path) ? path : Path.Combine(Application.persistentDataPath, path);
        }

        /// <summary>Load the Vicon→Unity transform. Returns identity (and logs an error) when absent/garbled.</summary>
        public static RigidAlignment Load(string path = null)
        {
            string resolved = Resolve(path);
            try
            {
                if (File.Exists(resolved))
                {
                    if (RigidAlignment.TryParseMatrix(File.ReadAllText(resolved), out RigidAlignment a))
                    {
                        Debug.Log($"[ViconAlignment] loaded from {resolved}");
                        return a;
                    }
                    Debug.LogError($"[ViconAlignment] {resolved} did not contain a readable 4x4 matrix — " +
                                   "using IDENTITY. Vicon limb positions will be WRONG until this is fixed.");
                    return RigidAlignment.Identity;
                }
                Debug.LogError($"[ViconAlignment] no alignment file at {resolved} — using IDENTITY. Run the " +
                               "ViconCalibrationRecorder first; Vicon limb positions are WRONG until you do.");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ViconAlignment] failed to read {resolved} ({e.Message}) — using IDENTITY.");
            }
            return RigidAlignment.Identity;
        }

        /// <summary>Persist the transform (what the calibration recorder writes). Never throws.</summary>
        public static bool Save(RigidAlignment alignment, string path = null)
        {
            string resolved = Resolve(path);
            try
            {
                File.WriteAllText(resolved, alignment.ToMatrixText());
                Debug.Log($"[ViconAlignment] saved to {resolved} " +
                          $"(rms {alignment.RmsError * 1000f:F1} mm over {alignment.SampleCount} samples)");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ViconAlignment] failed to write {resolved}: {e.Message}");
                return false;
            }
        }
    }
}
