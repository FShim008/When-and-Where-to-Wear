using System.IO;
using CollisionFeedback.Core;

namespace CollisionFeedback.Runtime
{
    /// <summary>
    /// Appends <see cref="BlockResult"/> rows to a CSV file. The ONLY file-I/O in the pipeline -
    /// formatting lives in the pure Core <see cref="CsvFormatter"/> (and is unit-tested); this just
    /// persists the strings.
    /// </summary>
    public sealed class CsvFileWriter
    {
        private readonly string _path;

        public CsvFileWriter(string path) { _path = path; }

        /// <summary>Writes the header row if the file is new or empty.</summary>
        public void EnsureHeader()
        {
            if (!File.Exists(_path) || new FileInfo(_path).Length == 0)
                File.AppendAllText(_path, CsvFormatter.Header() + "\n");
        }

        public void Append(BlockResult result)
        {
            File.AppendAllText(_path, CsvFormatter.Row(result) + "\n");
        }
    }
}
