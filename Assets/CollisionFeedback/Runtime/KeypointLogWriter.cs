using System;
using System.IO;
using CollisionFeedback.Core;

namespace CollisionFeedback.Runtime
{
    /// <summary>
    /// Persists the raw per-frame keypoint stream to one CSV per block via the pure Core
    /// <see cref="KeypointLogFormatter"/> [Plan Task 5.6 / oracle-robustness]. Frames are logged in the VR
    /// world frame (i.e. AFTER the camera→VR transform the driver applies), with block-relative timestamps —
    /// exactly the frames fed to the <c>BlockRunner</c> — so the analysis can re-derive alerts offline under
    /// injected noise/latency. Open once per block, <see cref="Write"/> each frame, <see cref="Dispose"/> to
    /// flush + close.
    /// </summary>
    public sealed class KeypointLogWriter : IDisposable
    {
        private readonly int _participant;
        private readonly int _block;
        private StreamWriter _w;

        public KeypointLogWriter(string path, int participant, int block)
        {
            _participant = participant;
            _block = block;

            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            _w = new StreamWriter(path, append: false) { AutoFlush = true }; // flush each frame so a crash/abort never loses data
            _w.WriteLine(KeypointLogFormatter.Header());
        }

        public void Write(in PoseFrame frame)
        {
            if (_w == null) return;
            _w.WriteLine(KeypointLogFormatter.Row(_participant, _block, frame));
        }

        public void Dispose()
        {
            try { _w?.Flush(); _w?.Dispose(); } catch { /* shutting down */ }
            _w = null;
        }
    }
}
