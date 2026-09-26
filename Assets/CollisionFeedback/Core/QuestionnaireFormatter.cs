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
        /// <summary>
        /// CSV contract. <c>kind</c> distinguishes a RAW ITEM RESPONSE from a SCORED MEASURE.
        ///
        /// ADDED 2026-09-25 — §12 requires "questionnaire **item-level and scored** data" and only scored
        /// data was written. The panel collected per-item responses and then discarded them at the callback
        /// boundary, so every raw response was lost at the moment of collection.
        ///
        /// That loss is IRREVERSIBLE: subscale scores cannot be decomposed back into items. A session run
        /// without this could never be re-scored, could not support an item-level reliability check, and
        /// could not be reanalysed under a different scoring convention — and nothing in the data would
        /// reveal that the items had ever existed.
        /// </summary>
        public const string HeaderLine = "participant,block,condition,instrument,kind,measure,value";

        /// <summary>A raw response to one questionnaire item, as the participant gave it.</summary>
        public const string KindItem = "item";

        /// <summary>A subscale or total computed from the items by <c>Questionnaire.Score</c>.</summary>
        public const string KindMeasure = "measure";

        public static string Header() => HeaderLine;

        /// <summary>Scored measures (subscales and totals).</summary>
        public static IEnumerable<string> Rows(int participant, int block, string condition, string instrument,
                                               IReadOnlyDictionary<string, float> measures)
        {
            var inv = CultureInfo.InvariantCulture;
            foreach (var kv in measures)
            {
                string v = (float.IsNaN(kv.Value) || float.IsInfinity(kv.Value)) ? "NA" : kv.Value.ToString("R", inv);
                yield return Row(participant, block, condition, instrument, KindMeasure, kv.Key, v);
            }
        }

        /// <summary>
        /// Raw item responses. Emit these ALONGSIDE <see cref="Rows"/>, never instead of them: the scored
        /// values are what the analysis reads, and the items are what makes the scoring auditable.
        /// </summary>
        public static IEnumerable<string> ItemRows(int participant, int block, string condition, string instrument,
                                                   IReadOnlyDictionary<string, int> responses)
        {
            var inv = CultureInfo.InvariantCulture;
            foreach (var kv in responses)
                yield return Row(participant, block, condition, instrument, KindItem, kv.Key,
                                 kv.Value.ToString(inv));
        }

        private static string Row(int participant, int block, string condition, string instrument,
                                  string kind, string measure, string value)
        {
            var inv = CultureInfo.InvariantCulture;
            return string.Concat(
                participant.ToString(inv), ",",
                block.ToString(inv), ",",
                Esc(condition), ",",
                Esc(instrument), ",",
                kind, ",",
                Esc(measure), ",",
                value);
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
