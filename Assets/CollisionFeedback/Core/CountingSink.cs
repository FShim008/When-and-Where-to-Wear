using System.Collections.Generic;

namespace CollisionFeedback.Core
{
    /// <summary>
    /// Decorator <see cref="IFeedbackSink"/>: records and counts every command, then forwards to an
    /// optional inner sink (the real device at runtime; null in tests). This is how we get the
    /// alert-rate covariate (number of alerts fired) that rules out the alert-rate confound.
    /// </summary>
    public sealed class CountingSink : IFeedbackSink
    {
        private readonly IFeedbackSink _inner;
        private readonly List<FeedbackCommand> _commands = new();

        public CountingSink(IFeedbackSink inner = null) { _inner = inner; }

        public int Count => _commands.Count;
        public IReadOnlyList<FeedbackCommand> Commands => _commands;

        public void Fire(in FeedbackCommand command)
        {
            _commands.Add(command);
            _inner?.Fire(command);
        }
    }
}
