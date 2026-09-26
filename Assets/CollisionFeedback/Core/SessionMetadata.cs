using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace CollisionFeedback.Core
{
    /// <summary>
    /// `session.json` — the provenance record that lets a dataset explain itself
    /// [PAPER1_STUDY_DESIGN §12 "session and block metadata"; PAPER2_STUDY_DESIGN §14].
    ///
    /// WHY IT IS FIRST IN §12'S LIST. Without it a session directory is a pile of CSVs with no record of
    /// what produced them: which code version, which devices, which operator, which parameter values. Every
    /// other file describes the participant's behaviour; this one describes the apparatus. A result that
    /// cannot say what `D`, `T` and the cue floor were set to at collection time cannot be reproduced after
    /// those defaults change — and they will change, because the pilot exists to change them.
    ///
    /// Paper 2 already wrote its own; this is the shared shape so Paper 1 stops being the odd one out and
    /// both papers' archives can be read by the same tooling.
    ///
    /// Pure: the caller supplies every value, keys are emitted in sorted order, and the output is
    /// deterministic so two runs of the same session differ only where the session differed.
    /// </summary>
    public static class SessionMetadata
    {
        public const int SchemaVersion = 1;
        public const string FileName = "session.json";

        /// <summary>
        /// Build the JSON. <paramref name="sections"/> groups values so a reader can tell apparatus from
        /// protocol from parameters; an empty section is omitted rather than written as an empty object,
        /// because an empty "devices" block reads as "no devices" rather than "not recorded".
        /// </summary>
        public static string Build(
            string study,
            int participantId,
            string startedUtcIso,
            string endedUtcIso,
            string completionStatus,
            IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sections)
        {
            var inv = CultureInfo.InvariantCulture;
            var sb = new StringBuilder(1024);

            sb.Append("{\n");
            sb.Append("  \"schema_version\": ").Append(SchemaVersion.ToString(inv)).Append(",\n");
            sb.Append("  \"study\": ").Append(Str(study)).Append(",\n");
            sb.Append("  \"participant\": ").Append(participantId.ToString(inv)).Append(",\n");
            sb.Append("  \"started_utc\": ").Append(Str(startedUtcIso)).Append(",\n");
            sb.Append("  \"ended_utc\": ").Append(Str(endedUtcIso)).Append(",\n");
            sb.Append("  \"completion\": ").Append(Str(completionStatus));

            if (sections != null)
            {
                var names = new List<string>(sections.Keys);
                names.Sort(System.StringComparer.Ordinal);
                foreach (string name in names)
                {
                    IReadOnlyDictionary<string, string> body = sections[name];
                    if (body == null || body.Count == 0) continue;   // omit, do not write {}

                    sb.Append(",\n  ").Append(Str(name)).Append(": {\n");
                    var keys = new List<string>(body.Keys);
                    keys.Sort(System.StringComparer.Ordinal);
                    for (int i = 0; i < keys.Count; i++)
                    {
                        sb.Append("    ").Append(Str(keys[i])).Append(": ").Append(Str(body[keys[i]]));
                        sb.Append(i < keys.Count - 1 ? ",\n" : "\n");
                    }
                    sb.Append("  }");
                }
            }

            sb.Append("\n}\n");
            return sb.ToString();
        }

        /// <summary>Convenience for building a section without ceremony at the call site.</summary>
        public static Dictionary<string, string> Section(params (string key, string value)[] pairs)
        {
            var d = new Dictionary<string, string>(pairs.Length);
            foreach (var (k, v) in pairs) d[k] = v ?? "";
            return d;
        }

        /// <summary>Format a float for the parameters section without culture surprises.</summary>
        public static string Num(float v) => v.ToString("R", CultureInfo.InvariantCulture);

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
