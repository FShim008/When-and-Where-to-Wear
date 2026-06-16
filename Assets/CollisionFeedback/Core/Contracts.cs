namespace CollisionFeedback.Core
{
    /// <summary>
    /// Source of body-tracking frames. The seam between the camera rig and the brain.
    /// Real impl = UDP/LSL receiver (Runtime); dev/test impl = <see cref="MockKeypointSource"/>.
    /// </summary>
    public interface IKeypointSource
    {
        /// <summary>Returns the next available frame, if any. Non-blocking; call until it returns false.</summary>
        bool TryGetFrame(out PoseFrame frame);
    }

    /// <summary>
    /// Destination for feedback. The seam between the brain and the devices.
    /// Real impl = bHaptics / in-HMD visual (Runtime); test impl = a recording sink.
    /// </summary>
    public interface IFeedbackSink
    {
        void Fire(in FeedbackCommand command);
    }

    /// <summary>
    /// Wall-clock seam, injected so logging/replay is deterministic in tests.
    /// Core decision logic is driven by <see cref="PoseFrame.Timestamp"/> (the data clock) and never
    /// reads wall time directly; the Runtime logger uses this for real-world timestamps.
    /// </summary>
    public interface IClock
    {
        double Now { get; }
    }

    /// <summary>Default monotonic real-time clock for the Runtime layer.</summary>
    public sealed class SystemClock : IClock
    {
        private readonly System.Diagnostics.Stopwatch _sw = System.Diagnostics.Stopwatch.StartNew();
        public double Now => _sw.Elapsed.TotalSeconds;
    }
}
