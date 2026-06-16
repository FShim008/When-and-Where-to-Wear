namespace CollisionFeedback.Core
{
    /// <summary>Identifying metadata for one block (who, which block, which condition + layout).</summary>
    public sealed class BlockContext
    {
        public int ParticipantId;
        public int BlockIndex;      // 0-based index among the 6 condition blocks
        public Condition Condition;
        public string LayoutId;
    }

    /// <summary>
    /// One analysis-ready row: the per-block outcome the negative-binomial GLMM consumes.
    /// Primary DV = <see cref="Collisions"/> with offset log(<see cref="Opportunities"/>).
    /// </summary>
    public sealed class BlockResult
    {
        public BlockContext Context;

        public int Opportunities;            // denominator (scheduled, fixed per block)
        public int Collisions;               // numerator (count)
        public int OpportunitiesHit;         // opportunities with >= 1 attributed collision
        public int OpportunitiesAvoided;     // Opportunities - OpportunitiesHit
        public int CollisionsAttributed;     // collisions inside an open opportunity window for that limb
        public int CollisionsUnattributed;   // collisions with no opportunity open (spontaneous)
        public int NearMisses;
        public int Alerts;                   // feedback events fired (alert-rate covariate)
        public float MinClearance;           // smallest clearance observed (+Inf if no engagement occurred)
        public double DurationSeconds;
        public int AvoidanceCount;           // alerts followed by a detected avoidance turnaround
        public double MeanAvoidanceLatencySeconds; // mean alert->avoidance latency (NaN if none)

        public float CollisionsPerOpportunity =>
            Opportunities > 0 ? (float)Collisions / Opportunities : 0f;
    }
}
