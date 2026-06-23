using System.IO;
using UnityEngine;
using CollisionFeedback.Core;

namespace CollisionFeedback.Runtime
{
    /// <summary>
    /// File seam for the per-site cue gains [Plan Task 3.1 / E1]. The parsing/formatting lives in
    /// <see cref="CueIntensityTable"/> (Core, unit-tested); this is the thin Runtime IO around it.
    ///
    /// Relative names resolve under <c>Application.persistentDataPath</c> (so "cue_intensity.csv" lands next
    /// to the session data); absolute paths are used as-is. A missing/empty/garbled file falls back to a
    /// uniform 1.0 (i.e. no equalization applied) and logs a line so the operator knows. Never throws.
    /// </summary>
    public static class CueIntensityFile
    {
        public const string DefaultName = "cue_intensity.csv";

        public static string DefaultPath => Path.Combine(Application.persistentDataPath, DefaultName);

        private static string Resolve(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return DefaultPath;
            return Path.IsPathRooted(path) ? path : Path.Combine(Application.persistentDataPath, path);
        }

        /// <summary>Load per-site gains. Falls back to uniform 1.0 if the file is absent/unreadable.</summary>
        public static CueIntensityTable Load(string path = null)
        {
            string resolved = Resolve(path);
            try
            {
                if (File.Exists(resolved))
                {
                    CueIntensityTable table = CueIntensityTable.Parse(File.ReadAllText(resolved));
                    Debug.Log($"[CueIntensity] loaded per-site gains from {resolved}");
                    return table;
                }
                Debug.Log($"[CueIntensity] no file at {resolved} — using uniform 1.0 (no equalization applied)");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[CueIntensity] failed to read {resolved} ({e.Message}) — using uniform 1.0");
            }
            return CueIntensityTable.Uniform(1f);
        }

        /// <summary>Persist per-site gains (what the E2 perceptual-matching pass writes). Never throws.</summary>
        public static void Save(CueIntensityTable table, string path = null)
        {
            string resolved = Resolve(path);
            try
            {
                File.WriteAllText(resolved, table.ToCsv());
                Debug.Log($"[CueIntensity] saved per-site gains to {resolved}");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[CueIntensity] failed to write {resolved}: {e.Message}");
            }
        }
    }
}
