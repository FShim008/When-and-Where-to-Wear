using System.Globalization;

namespace CollisionFeedback.Core
{
    /// <summary>
    /// Long-format per-event log [3G.2]: one row per alert / outcome / opportunity activation, so the
    /// full timeline of a block is auditable and alerts can be re-derived under injected noise. Pure;
    /// the Runtime EventLogWriter persists the rows.
    /// </summary>
    public static class EventLogFormatter
    {
        private static readonly string[] Columns =
        {
            "participant", "block", "condition", "time_s", "event", "limb", "site", "obstacle", "value",
        };

        public static string Header() => string.Join(",", Columns);

        public static string AlertRow(BlockContext ctx, in FeedbackCommand c)
        {
            string ev = (c.Modality == Modality.Visual ? "visual_" : "alert_")
                        + (c.Trigger == TriggerKind.Predictive ? "predictive" : "reactive");
            return Join(ctx, c.DataTime, ev, c.Limb.ToString(), c.Site.ToString(), c.ObstacleId,
                        c.Distance.ToString("F4", CultureInfo.InvariantCulture));
        }

        public static string OutcomeRow(BlockContext ctx, in OutcomeEvent e)
        {
            string ev = e.Kind == OutcomeKind.Collision ? "collision" : "near_miss";
            return Join(ctx, e.DataTime, ev, e.Limb.ToString(), "", e.ObstacleId,
                        e.Clearance.ToString("F4", CultureInfo.InvariantCulture));
        }

        public static string OpportunityRow(BlockContext ctx, in OpportunityActivation a)
        {
            string ev = a.Phase == OpportunityPhase.Open ? "opportunity_open" : "opportunity_close";
            return Join(ctx, a.DataTime, ev, a.TargetLimb.ToString(), "", a.TargetObstacleId, "");
        }

        private static string Join(BlockContext ctx, double t, string ev, string limb, string site,
                                   string obstacle, string value)
        {
            var inv = CultureInfo.InvariantCulture;
            return string.Join(",", new[]
            {
                ctx.ParticipantId.ToString(inv), ctx.BlockIndex.ToString(inv), ctx.Condition.ToString(),
                t.ToString("F4", inv), ev, limb, site, obstacle ?? "", value,
            });
        }
    }
}
