using System;
using System.IO;
using CollisionFeedback.Core;

namespace CollisionFeedback.Runtime
{
    /// <summary>
    /// Appends to `events.csv` — the non-trial session chronology [PAPER2 §14].
    ///
    /// Append-and-flush per row, like <see cref="DeviationLogWriter"/>: the events worth having are the
    /// ones that explain an abnormal session, and a buffered writer loses exactly those if the process
    /// dies. A chronology that survives only clean shutdowns cannot explain an unclean one.
    /// </summary>
    public sealed class SessionEventLogWriter
    {
        private readonly string _path;
        private readonly string _study;
        private readonly int _participant;
        private readonly int _session;
        private readonly float _t0;

        public SessionEventLogWriter(string path, string study, int participant, int session, float startTime)
        {
            _path = path; _study = study; _participant = participant; _session = session; _t0 = startTime;
        }

        public string Path => _path;
        public int Count { get; private set; }

        /// <summary>Current phase label, stamped onto every event so the timeline is readable without surrounding context.</summary>
        public string Phase { get; set; } = "";

        private void EnsureHeader()
        {
            if (File.Exists(_path)) return;
            string dir = System.IO.Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(_path, SessionEventFormatter.Header() + "\n");
        }

        /// <param name="sessionTime">
        /// Session clock in seconds. Pass <see cref="float.NaN"/> to have it computed from the start time
        /// supplied at construction.
        /// </param>
        public void Record(string kind, string detail = "", float sessionTime = float.NaN)
        {
            double t = float.IsNaN(sessionTime)
                ? UnityEngine.Time.realtimeSinceStartup - _t0
                : sessionTime;

            var e = new SessionEvent(kind, detail, Phase, DateTime.UtcNow.ToString("o"), t);
            try
            {
                EnsureHeader();
                using var w = new StreamWriter(_path, append: true);
                w.WriteLine(SessionEventFormatter.Row(_study, _participant, _session, e));
                Count++;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[SessionEventLog] Failed to record {kind}: {ex.Message}");
            }
        }
    }
}
