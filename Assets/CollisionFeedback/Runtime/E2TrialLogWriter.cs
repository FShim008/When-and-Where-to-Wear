using System.Collections.Generic;
using System.IO;
using UnityEngine;
using CollisionFeedback.Core.E2;

namespace CollisionFeedback.Runtime
{
    /// <summary>
    /// Writes the E2 PRIMARY dataset: one row per trial [PAPER2_STUDY_DESIGN §8, §14].
    ///
    /// The header is written exactly once, when the file is created. Repeated headers mid-file are a release
    /// blocker in Paper 1 §12 and the same rule applies here, so appending later trials must not re-emit one.
    ///
    /// Trials are flushed in batches rather than at the end of the session: a session that crashes or is
    /// e-stopped after 300 trials should still leave 300 usable rows on disk.
    /// </summary>
    public sealed class E2TrialLogWriter
    {
        private readonly string _path;
        private readonly int _participantId;
        private readonly int _sessionIndex;

        public E2TrialLogWriter(string path, int participantId, int sessionIndex)
        {
            _path = path;
            _participantId = participantId;
            _sessionIndex = sessionIndex;
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
        }

        /// <summary>Append one trial. Called as each trial closes, so partial sessions survive a crash.</summary>
        public void Write(in E2TrialOutcome outcome)
        {
            try
            {
                bool needHeader = !File.Exists(_path) || new FileInfo(_path).Length == 0;
                using var w = new StreamWriter(_path, append: true);
                if (needHeader) w.WriteLine(E2TrialOutcomeFormatter.Header());
                w.WriteLine(E2TrialOutcomeFormatter.Row(_participantId, _sessionIndex, outcome));
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[E2TrialLog] failed to write {_path}: {e.Message}");
            }
        }

        public void WriteAll(IReadOnlyList<E2TrialOutcome> outcomes)
        {
            for (int i = 0; i < outcomes.Count; i++) Write(outcomes[i]);
        }
    }
}
