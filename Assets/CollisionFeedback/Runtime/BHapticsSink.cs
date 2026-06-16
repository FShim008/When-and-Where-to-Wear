using System;
using UnityEngine;
using CollisionFeedback.Core;

namespace CollisionFeedback.Runtime
{
    /// <summary>
    /// Routes safety cues to bHaptics tactors [3F.2/3F.3]. Fully wired EXCEPT the one SDK playback call:
    /// it maps each haptic <see cref="FeedbackCommand"/> to a <see cref="TactorTarget"/> and invokes a
    /// pluggable <c>play</c> action. The default action logs (so this works with no SDK); to go live,
    /// install the bHaptics Unity package and pass an action that submits the motor array, e.g.:
    ///
    ///   new BHapticsSink(t => bHaptics.Submit(t.Device.ToString(), t.Motors, durationMs: 100));
    ///
    /// Visual-modality commands are ignored here (the in-HMD visual renderer handles those).
    /// </summary>
    public sealed class BHapticsSink : IFeedbackSink
    {
        private readonly Action<TactorTarget> _play;
        private readonly float _intensity;

        public BHapticsSink(Action<TactorTarget> play = null, float intensity = 1f)
        {
            _play = play ?? (t =>
                Debug.Log($"[bHaptics STUB] {t.Device} motors=[{string.Join(",", t.Motors)}] @ {t.Intensity:F2}"));
            _intensity = intensity;
        }

        public void Fire(in FeedbackCommand command)
        {
            if (command.Modality != Modality.Haptic) return; // visual handled elsewhere
            _play(BHapticsTactorMap.For(command.Site, _intensity));
        }
    }
}
