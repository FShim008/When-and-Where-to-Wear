using System;
using System.Collections.Generic;
using System.IO;
using CollisionFeedback.Core;

namespace CollisionFeedback.Runtime
{
    /// <summary>
    /// Appends to `deviations.csv` [PAPER2 §14; PAPER1 §12].
    ///
    /// Append-only and flushed per write, because the events it records are exactly the ones that happen
    /// when a session is going wrong — an operator stop, a device failure, a participant reporting
    /// discomfort. A buffered writer would lose precisely the rows that matter if the process died, and a
    /// deviation log that survives only clean shutdowns is not a safety record.
    /// </summary>
    public sealed class DeviationLogWriter
    {
        private readonly string _path;
        private readonly string _study;
        private readonly int _participant;
        private readonly int _session;

        public DeviationLogWriter(string path, string study, int participant, int session = 0)
        {
            _path = path; _study = study; _participant = participant; _session = session;
        }

        public string Path => _path;
        public int Count { get; private set; }

        private void EnsureHeader()
        {
            if (File.Exists(_path)) return;
            string dir = System.IO.Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(_path, DeviationFormatter.Header() + "\n");
        }

        public void Append(in DeviationRecord d)
        {
            try
            {
                EnsureHeader();
                using var w = new StreamWriter(_path, append: true);
                w.WriteLine(DeviationFormatter.Row(_study, _participant, _session, d));
                Count++;
            }
            catch (Exception e)
            {
                // Never let a logging failure take down a running session, but never hide it either.
                UnityEngine.Debug.LogError($"[DeviationLog] Failed to record {d.Kind}/{d.Reason}: {e.Message}");
            }
        }

        /// <summary>Convenience for the common in-session cases.</summary>
        public void Record(string kind, string reason, string detail = "", string scope = "session",
                           string id = "", double dataTimeSeconds = double.NaN) =>
            Append(new DeviationRecord(scope, id, kind, reason, detail,
                                       DateTime.UtcNow.ToString("o"), dataTimeSeconds));

        public void AppendRange(IEnumerable<DeviationRecord> records)
        {
            foreach (DeviationRecord d in records) Append(d);
        }
    }
}
