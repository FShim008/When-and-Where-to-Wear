using System.Collections.Generic;
using System.Globalization;

namespace CollisionFeedback.Core
{
    /// <summary>
    /// Long-format (tidy) CSV for post-block / per-session questionnaires (IPQ presence, NASA-TLX workload,
    /// SSQ sickness): one row per (participant, block, condition, instrument, measure). Pure + testable; the
    /// Runtime <c>QuestionnaireLogWriter</c> persists it and <c>Analysis/analysis.R</c> pivots it wide and
    /// joins on participant+block. Instrument/measure names are free strings, so adding an item or subscale
    /// needs no code change. [Plan Task 5.5 groundwork — the administration UI/items are a study-design
    /// decision and live elsewhere.]
    ///
    /// Convention the analysis expects: instrument ∈ {"IPQ","NASA_TLX","SSQ"}; canonical measures include
    /// IPQ "presence", NASA_TLX "overall", SSQ "total" (plus any subscales you also record). Use block = -1
    /// for session-level instruments (e.g. SSQ "pre_total"/"post_total").
    /// </summary>
    public static class QuestionnaireFormatter
    {
        public const string HeaderLine = "participant,block,condition,instrument,measure,value";

        public static string Header() => HeaderLine;

        public static IEnumerable<string> Rows(int participant, int block, string condition, string instrument,
                                               IReadOnlyDictionary<string, float> measures)
        {
            var inv = CultureInfo.InvariantCulture;
            foreach (var kv in measures)
            {
                string v = (float.IsNaN(kv.Value) || float.IsInfinity(kv.Value)) ? "NA" : kv.Value.ToString("R", inv);
                yield return string.Concat(
                    participant.ToString(inv), ",",
                    block.ToString(inv), ",",
                    Esc(condition), ",",
                    Esc(instrument), ",",
                    Esc(kv.Key), ",",
                    v);
            }
        }

        // Minimal RFC-4180 escaping for the free-text labels.
        private static string Esc(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            if (s.IndexOfAny(new[] { ',', '"', '\n', '\r' }) < 0) return s;
            return "\"" + s.Replace("\"", "\"\"") + "\"";
        }
    }
}
