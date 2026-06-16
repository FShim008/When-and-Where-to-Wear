using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using CollisionFeedback.Core;

namespace CollisionFeedback.Runtime
{
    /// <summary>
    /// Real keypoint source [3C.1]: receives newline-delimited CSV keypoint lines over UDP on a background
    /// thread, parses them (<see cref="KeypointDeserializer"/>), and hands frames to the main thread via
    /// <see cref="TryGetFrame"/>. Compiles and runs with NO hardware — it simply produces no frames until
    /// the tracking pipeline starts sending. Swap it in for `MockKeypointSource` behind `IKeypointSource`.
    /// </summary>
    public sealed class UdpKeypointSource : IKeypointSource, IDisposable
    {
        private readonly UdpClient _client;
        private readonly Thread _thread;
        private readonly ConcurrentQueue<PoseFrame> _queue = new();
        private volatile bool _running;

        public UdpKeypointSource(int port)
        {
            _client = new UdpClient(port);
            _running = true;
            _thread = new Thread(ReceiveLoop) { IsBackground = true, Name = "UdpKeypointSource" };
            _thread.Start();
        }

        public bool TryGetFrame(out PoseFrame frame) => _queue.TryDequeue(out frame);

        private void ReceiveLoop()
        {
            var remote = new IPEndPoint(IPAddress.Any, 0);
            while (_running)
            {
                try
                {
                    byte[] data = _client.Receive(ref remote);
                    string text = Encoding.ASCII.GetString(data);
                    foreach (string line in text.Split('\n'))
                        if (KeypointDeserializer.TryParse(line.Trim(), out PoseFrame f))
                            _queue.Enqueue(f);
                }
                catch (SocketException) { if (!_running) break; }
                catch (ObjectDisposedException) { break; }
            }
        }

        public void Dispose()
        {
            _running = false;
            try { _client?.Close(); } catch { /* shutting down */ }
            try { _thread?.Join(200); } catch { /* shutting down */ }
        }
    }
}
