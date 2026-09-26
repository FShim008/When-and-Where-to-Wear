using System;
using System.IO;
using CollisionFeedback.Core;

namespace CollisionFeedback.Runtime
{
    /// <summary>
    /// Writes `trajectories_&lt;block&gt;.csv` [PAPER2 §14].
    ///
    /// Buffered, unlike the deviation and event writers. Those record rare events where losing one to a
    /// crash would destroy the record; this records ~90 rows a second, where flushing every row would cost
    /// frame time during the measured reach — the one place in the session where added latency would
    /// corrupt the measurement rather than merely annoy. <see cref="Flush"/> is called at trial end, so the
    /// most that can be lost is the tail of one trial, and `events.csv` will show the session died.
    ///
    /// **Duplicate frames are skipped.** The E2 loop polls faster than the tracker produces, so the same
    /// frame is seen repeatedly. Writing it each time would inflate the file several-fold and, worse, make
    /// the inter-frame gap look like zero — destroying the signal that makes a dropout visible.
    /// </summary>
    public sealed class TrajectoryLogWriter : IDisposable
    {
        private readonly int _participant, _session, _block;
        private StreamWriter _w;
        private long _seq;
        private double _lastSourceTime = double.NaN;

        public TrajectoryLogWriter(string path, int participant, int session, int block)
        {
            _participant = participant; _session = session; _block = block;
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            bool header = !File.Exists(path);
            _w = new StreamWriter(path, append: true);
            if (header) _w.WriteLine(TrajectoryFormatter.Header());
        }

        public long FramesWritten => _seq;

        /// <summary>
        /// Record a frame. Returns false when the frame was skipped as a duplicate of the previous one.
        /// </summary>
        public bool Write(in PoseFrame frame, string trialId)
        {
            if (_w == null) return false;

            // Same source timestamp means the tracker has not produced anything new — see the class note.
            if (!double.IsNaN(_lastSourceTime) && frame.Timestamp <= _lastSourceTime) return false;

            double gap = double.IsNaN(_lastSourceTime) ? double.NaN : frame.Timestamp - _lastSourceTime;
            _lastSourceTime = frame.Timestamp;

            try
            {
                _w.WriteLine(TrajectoryFormatter.Row(_participant, _session, _block, trialId,
                                                     _seq, frame, DateTime.UtcNow.ToString("o"), gap));
                _seq++;
                return true;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"[Trajectory] write failed at seq {_seq}: {e.Message}");
                return false;
            }
        }

        public void Flush()
        {
            try { _w?.Flush(); }
            catch (Exception e) { UnityEngine.Debug.LogWarning($"[Trajectory] flush failed: {e.Message}"); }
        }

        public void Dispose()
        {
            try { _w?.Flush(); _w?.Dispose(); }
            catch (Exception e) { UnityEngine.Debug.LogWarning($"[Trajectory] close failed: {e.Message}"); }
            _w = null;
        }
    }
}
