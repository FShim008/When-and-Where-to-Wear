using System.Collections.Generic;
using System.IO;
using System.Text;
using CollisionFeedback.Core;

namespace CollisionFeedback.Runtime
{
    /// <summary>Persists the long-format per-event log [3G.2]. Formatting lives in the pure Core
    /// <see cref="EventLogFormatter"/>; this is the file I/O.</summary>
    public sealed class EventLogWriter
    {
        private readonly string _path;
        private bool _headerWritten;

        public EventLogWriter(string path) { _path = path; }

        public void WriteBlock(BlockContext ctx,
                               IReadOnlyList<FeedbackCommand> alerts,
                               IReadOnlyList<OutcomeEvent> outcomes,
                               IReadOnlyList<OpportunityActivation> opportunities)
        {
            if (!_headerWritten)
            {
                File.AppendAllText(_path, EventLogFormatter.Header() + "\n");
                _headerWritten = true;
            }

            var sb = new StringBuilder();
            foreach (var a in alerts) sb.Append(EventLogFormatter.AlertRow(ctx, a)).Append('\n');
            foreach (var o in outcomes) sb.Append(EventLogFormatter.OutcomeRow(ctx, o)).Append('\n');
            foreach (var op in opportunities) sb.Append(EventLogFormatter.OpportunityRow(ctx, op)).Append('\n');
            File.AppendAllText(_path, sb.ToString());
        }
    }
}
