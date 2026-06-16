using System.Globalization;

namespace CollisionFeedback.Core
{
    /// <summary>
    /// Formats a <see cref="BlockResult"/> as CSV. PURE (no file I/O - that lives in the Runtime
    /// CsvFileWriter), so it is unit-tested. Uses <see cref="CultureInfo.InvariantCulture"/> so decimals
    /// always use '.', which R / pandas expect regardless of the machine's locale.
    /// </summary>
    public static class CsvFormatter
    {
        private static readonly string[] Columns =
        {
            "participant", "block", "condition", "layout",
            "opportunities", "collisions", "collisions_per_opportunity",
            "opportunities_hit", "opportunities_avoided",
            "collisions_attributed", "collisions_unattributed",
            "near_misses", "alerts", "min_clearance_m", "duration_s",
            "avoidance_count", "mean_avoidance_latency_s",
        };

        public static string Header() => string.Join(",", Columns);

        public static string Row(BlockResult r)
        {
            var inv = CultureInfo.InvariantCulture;
            var fields = new[]
            {
                r.Context.ParticipantId.ToString(inv),
                r.Context.BlockIndex.ToString(inv),
                r.Context.Condition.ToString(),
                r.Context.LayoutId ?? "",
                r.Opportunities.ToString(inv),
                r.Collisions.ToString(inv),
                r.CollisionsPerOpportunity.ToString("F4", inv),
                r.OpportunitiesHit.ToString(inv),
                r.OpportunitiesAvoided.ToString(inv),
                r.CollisionsAttributed.ToString(inv),
                r.CollisionsUnattributed.ToString(inv),
                r.NearMisses.ToString(inv),
                r.Alerts.ToString(inv),
                float.IsPositiveInfinity(r.MinClearance) ? "NA" : r.MinClearance.ToString("F4", inv),
                r.DurationSeconds.ToString("F3", inv),
                r.AvoidanceCount.ToString(inv),
                double.IsNaN(r.MeanAvoidanceLatencySeconds) ? "NA" : r.MeanAvoidanceLatencySeconds.ToString("F3", inv),
            };
            return string.Join(",", fields);
        }
    }
}
