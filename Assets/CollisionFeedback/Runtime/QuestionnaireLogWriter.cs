using System.Collections.Generic;
using System.IO;
using CollisionFeedback.Core;

namespace CollisionFeedback.Runtime
{
    /// <summary>
    /// Appends questionnaire responses to one long-format CSV (header written once) via the pure Core
    /// <see cref="QuestionnaireFormatter"/>. [Plan Task 5.5 groundwork.] The administration UI and the actual
    /// IPQ/NASA-TLX/SSQ item wording + scoring are a study-design decision and are NOT included here — call
    /// <see cref="Append"/> with the scored subscales once they are collected (e.g. from
    /// <c>SessionRunner.RecordQuestionnaire</c>).
    /// </summary>
    public sealed class QuestionnaireLogWriter
    {
        private readonly string _path;

        public QuestionnaireLogWriter(string path) { _path = path; }

        public void EnsureHeader()
        {
            if (File.Exists(_path)) return;
            string dir = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(_path, QuestionnaireFormatter.Header() + "\n");
        }

        /// <summary>One row per measure. block = -1 for session-level instruments (e.g. SSQ pre/post).</summary>
        public void Append(int participant, int block, string condition, string instrument,
                           IReadOnlyDictionary<string, float> measures)
        {
            EnsureHeader();
            using var w = new StreamWriter(_path, append: true);
            foreach (string row in QuestionnaireFormatter.Rows(participant, block, condition, instrument, measures))
                w.WriteLine(row);
        }
    }
}
