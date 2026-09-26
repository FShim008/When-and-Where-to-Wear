using System.Collections.Generic;
using System.IO;
using UnityEngine;
using CollisionFeedback.Core;

namespace CollisionFeedback.Runtime
{
    /// <summary>
    /// Writes the PRIMARY dataset: one row per scheduled opportunity [PAPER1_STUDY_DESIGN §5, §12].
    /// This is the file the confirmatory logistic model consumes; <c>summary.csv</c> becomes the
    /// block-level sensitivity analysis.
    ///
    /// The header is written exactly once, when the file is created. Repeated headers mid-file are named as a
    /// release blocker in §12, so appending a later block must not re-emit one.
    /// </summary>
    public sealed class OpportunityLogWriter
    {
        private readonly string _path;

        public OpportunityLogWriter(string path)
        {
            _path = path;
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
        }

        /// <summary>Append every opportunity row for one block.</summary>
        public void WriteBlock(BlockContext ctx, IReadOnlyList<OpportunityOutcome> outcomes)
        {
            try
            {
                bool needHeader = !File.Exists(_path) || new FileInfo(_path).Length == 0;
                using var w = new StreamWriter(_path, append: true);
                if (needHeader) w.WriteLine(OpportunityOutcomeFormatter.Header());
                foreach (string row in OpportunityOutcomeFormatter.Rows(ctx, outcomes)) w.WriteLine(row);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[OpportunityLog] failed to write {_path}: {e.Message}");
            }
        }
    }
}
