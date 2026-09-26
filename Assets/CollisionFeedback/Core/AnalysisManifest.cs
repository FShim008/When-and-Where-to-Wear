using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace CollisionFeedback.Core
{
    /// <summary>One raw file, its size, and its content hash.</summary>
    public readonly struct ManifestEntry
    {
        public readonly string RelativePath;
        public readonly long SizeBytes;
        public readonly string Sha256;     // lowercase hex, or "" when unreadable

        public ManifestEntry(string relativePath, long sizeBytes, string sha256)
        {
            RelativePath = relativePath;
            SizeBytes = sizeBytes;
            Sha256 = sha256 ?? "";
        }
    }

    /// <summary>A trial, opportunity or participant removed from analysis, and why.</summary>
    public readonly struct ManifestExclusion
    {
        public readonly string Scope;      // "participant" | "block" | "trial" | "opportunity"
        public readonly string Id;
        public readonly string Reason;

        public ManifestExclusion(string scope, string id, string reason)
        {
            Scope = scope; Id = id; Reason = reason ?? "";
        }
    }

    /// <summary>
    /// Builds `analysis_manifest.json` — the record that makes a dataset checkable by someone who was not
    /// in the room [PAPER1_STUDY_DESIGN §12 "analysis-ready derivations with scripts and checksums";
    /// PAPER2_STUDY_DESIGN §14 "raw-data hashes, exclusions, software/package versions, and generated
    /// analysis files"].
    ///
    /// WHY IT EXISTS. Without it a session directory is a pile of CSVs with no way to tell whether they are
    /// the ones the analysis ran on. Hashes catch silent corruption and accidental re-writes; the exclusion
    /// list makes the gap between rows collected and rows analysed explicit rather than inferable; and the
    /// version block is what lets a result be reproduced after the code has moved on. Paper 2's gate 17
    /// ("freeze and archive") cannot pass without it.
    ///
    /// Pure and deterministic: entries are emitted in sorted order so two runs over the same directory
    /// produce byte-identical manifests and a diff means something changed. File reading and hashing live
    /// in the Runtime writer, per the Core architecture rule.
    /// </summary>
    public static class AnalysisManifest
    {
        /// <summary>Bumped when the manifest's own shape changes, so an old manifest is recognisable.</summary>
        public const int SchemaVersion = 1;

        /// <summary>
        /// The manifest cannot contain its own hash, so it must be identifiable by name and excluded from
        /// the walk. Anything else in the directory is hashed.
        /// </summary>
        public const string FileName = "analysis_manifest.json";

        public static string Build(
            string studyId,
            int participantId,
            string generatedUtcIso,
            IReadOnlyDictionary<string, string> versions,
            IReadOnlyList<ManifestEntry> rawFiles,
            IReadOnlyList<ManifestExclusion> exclusions,
            IReadOnlyList<string> notes = null)
        {
            var inv = CultureInfo.InvariantCulture;
            var sb = new StringBuilder(1024);

            sb.Append("{\n");
            sb.Append("  \"schema_version\": ").Append(SchemaVersion.ToString(inv)).Append(",\n");
            sb.Append("  \"study\": ").Append(Str(studyId)).Append(",\n");
            sb.Append("  \"participant\": ").Append(participantId.ToString(inv)).Append(",\n");
            sb.Append("  \"generated_utc\": ").Append(Str(generatedUtcIso)).Append(",\n");

            sb.Append("  \"versions\": {");
            if (versions != null && versions.Count > 0)
            {
                var keys = new List<string>(versions.Keys);
                keys.Sort(StringComparer.Ordinal);
                sb.Append('\n');
                for (int i = 0; i < keys.Count; i++)
                {
                    sb.Append("    ").Append(Str(keys[i])).Append(": ").Append(Str(versions[keys[i]]));
                    sb.Append(i < keys.Count - 1 ? ",\n" : "\n");
                }
                sb.Append("  ");
            }
            sb.Append("},\n");

            // Sorted so the manifest is stable across runs and a diff is meaningful.
            var files = new List<ManifestEntry>(rawFiles ?? new List<ManifestEntry>());
            files.Sort((a, b) => string.CompareOrdinal(a.RelativePath, b.RelativePath));

            sb.Append("  \"raw_files\": [");
            for (int i = 0; i < files.Count; i++)
            {
                ManifestEntry e = files[i];
                sb.Append("\n    {\"path\": ").Append(Str(e.RelativePath))
                  .Append(", \"bytes\": ").Append(e.SizeBytes.ToString(inv))
                  .Append(", \"sha256\": ").Append(Str(e.Sha256)).Append('}');
                if (i < files.Count - 1) sb.Append(',');
            }
            sb.Append(files.Count > 0 ? "\n  ],\n" : "],\n");

            sb.Append("  \"exclusions\": [");
            var ex = exclusions ?? new List<ManifestExclusion>();
            for (int i = 0; i < ex.Count; i++)
            {
                sb.Append("\n    {\"scope\": ").Append(Str(ex[i].Scope))
                  .Append(", \"id\": ").Append(Str(ex[i].Id))
                  .Append(", \"reason\": ").Append(Str(ex[i].Reason)).Append('}');
                if (i < ex.Count - 1) sb.Append(',');
            }
            sb.Append(ex.Count > 0 ? "\n  ],\n" : "],\n");

            sb.Append("  \"notes\": [");
            var nn = notes ?? new List<string>();
            for (int i = 0; i < nn.Count; i++)
            {
                sb.Append("\n    ").Append(Str(nn[i]));
                if (i < nn.Count - 1) sb.Append(',');
            }
            sb.Append(nn.Count > 0 ? "\n  ]\n" : "]\n");

            sb.Append("}\n");
            return sb.ToString();
        }

        /// <summary>
        /// True when every entry carries a hash. A missing hash means a file could not be read at manifest
        /// time — which is exactly the condition the manifest exists to surface, so callers should refuse to
        /// archive rather than quietly shipping an unverifiable dataset.
        /// </summary>
        public static bool AllHashed(IReadOnlyList<ManifestEntry> entries)
        {
            if (entries == null) return false;
            for (int i = 0; i < entries.Count; i++)
                if (string.IsNullOrEmpty(entries[i].Sha256)) return false;
            return true;
        }

        private static string Str(string s)
        {
            if (s == null) return "null";
            var sb = new StringBuilder(s.Length + 2);
            sb.Append('"');
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"':  sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\b': sb.Append("\\b");  break;
                    case '\f': sb.Append("\\f");  break;
                    case '\n': sb.Append("\\n");  break;
                    case '\r': sb.Append("\\r");  break;
                    case '\t': sb.Append("\\t");  break;
                    default:
                        if (c < ' ') sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
            return sb.ToString();
        }
    }
}
