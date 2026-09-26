using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;
using CollisionFeedback.Core;

namespace CollisionFeedback.Runtime
{
    /// <summary>
    /// Walks a session directory, hashes every raw file, and writes `analysis_manifest.json`
    /// [PAPER1 §12; PAPER2 §14].
    ///
    /// Call this ONCE at the end of a session, after every other writer has closed. Running it earlier
    /// hashes half-written files, which is worse than not hashing at all: the manifest would certify a
    /// state that never existed on disk.
    ///
    /// Hashing is deliberately here rather than in Core — Core does no file I/O by architectural rule.
    /// Core owns the JSON shape and the ordering, so the manifest stays pure and testable.
    /// </summary>
    public static class AnalysisManifestWriter
    {
        /// <summary>
        /// Hash every file in <paramref name="sessionDir"/> (recursively) and write the manifest into it.
        /// Returns the manifest path, or null if the directory does not exist.
        /// </summary>
        public static string Write(string sessionDir, string studyId, int participantId,
                                   IReadOnlyList<ManifestExclusion> exclusions = null,
                                   IReadOnlyList<string> notes = null,
                                   IReadOnlyDictionary<string, string> extraVersions = null)
        {
            if (string.IsNullOrEmpty(sessionDir) || !Directory.Exists(sessionDir))
            {
                Debug.LogWarning($"[AnalysisManifest] No such session directory: {sessionDir}");
                return null;
            }

            var entries = new List<ManifestEntry>();
            int unreadable = 0;

            foreach (string full in Directory.GetFiles(sessionDir, "*", SearchOption.AllDirectories))
            {
                string rel = full.Substring(sessionDir.Length).TrimStart(Path.DirectorySeparatorChar,
                                                                         Path.AltDirectorySeparatorChar)
                                 .Replace(Path.DirectorySeparatorChar, '/');

                // The manifest cannot contain its own hash. Skipping a stale one also stops a re-run
                // hashing the previous manifest and burying that fact in the new one.
                if (string.Equals(rel, AnalysisManifest.FileName, StringComparison.OrdinalIgnoreCase)) continue;

                long size = 0; string hash = "";
                try
                {
                    var fi = new FileInfo(full);
                    size = fi.Length;
                    hash = Sha256(full);
                }
                catch (Exception e)
                {
                    unreadable++;
                    Debug.LogWarning($"[AnalysisManifest] Could not hash {rel}: {e.Message}");
                }
                entries.Add(new ManifestEntry(rel, size, hash));
            }

            var versions = new Dictionary<string, string>
            {
                ["unity"] = Application.unityVersion,
                ["application"] = Application.version,
                ["platform"] = Application.platform.ToString(),
                ["manifest_schema"] = AnalysisManifest.SchemaVersion.ToString(),
            };
            if (extraVersions != null)
                foreach (var kv in extraVersions) versions[kv.Key] = kv.Value;

            string json = AnalysisManifest.Build(
                studyId, participantId,
                DateTime.UtcNow.ToString("o"),
                versions, entries, exclusions, notes);

            string path = Path.Combine(sessionDir, AnalysisManifest.FileName);
            File.WriteAllText(path, json);

            if (unreadable > 0)
                Debug.LogError($"[AnalysisManifest] {unreadable} file(s) could not be hashed. " +
                               "DO NOT ARCHIVE this session as verified — the manifest certifies only what " +
                               "it could read, and an unhashed file is exactly the case it exists to catch.");
            else
                Debug.Log($"[AnalysisManifest] {entries.Count} files hashed -> {path}");

            return path;
        }

        private static string Sha256(string path)
        {
            using var sha = SHA256.Create();
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            byte[] h = sha.ComputeHash(fs);
            var sb = new System.Text.StringBuilder(h.Length * 2);
            foreach (byte b in h) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }
}
