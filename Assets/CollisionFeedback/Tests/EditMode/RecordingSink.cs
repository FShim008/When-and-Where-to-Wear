using System.Collections.Generic;
using CollisionFeedback.Core;

namespace CollisionFeedback.Tests
{
    /// <summary>Test double for <see cref="IFeedbackSink"/> - records every command for assertions.</summary>
    internal sealed class RecordingSink : IFeedbackSink
    {
        public readonly List<FeedbackCommand> Fired = new();
        public void Fire(in FeedbackCommand command) => Fired.Add(command);
    }
}
