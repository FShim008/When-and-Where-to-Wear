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
        private readonly Action<TactorTarget> _playContinuous;
        private readonly CueIntensityTable _intensity; // per-site gains; uniform 1.0 unless calibrated [E1]

        /// <summary>Uniform-gain sink — back-compat with the old single-float intensity.</summary>
        public BHapticsSink(Action<TactorTarget> play = null, float intensity = 1f)
            : this(play, CueIntensityTable.Uniform(intensity)) { }

        /// <summary>
        /// Sink that can also render the CONTINUOUS cue of <see cref="Condition.PBC"/> [H4'].
        ///
        /// The two actions exist because the forms need different playback calls, and conflating them is a
        /// silent, study-invalidating bug. A discrete cue is a fixed pulse train of ~100 ms per pulse; a
        /// continuous cue is a level refreshed every frame (~11 ms at 90 Hz) whose amplitude carries the
        /// information. Submitting a continuous command through the discrete action would play overlapping
        /// full-length bursts at the calibrated gain and discard the amplitude — PBC would become PB
        /// delivered 90 times a second, and H4' would compare a condition against itself while still
        /// producing a plausible-looking log.
        ///
        ///   new BHapticsSink(
        ///       t => bHaptics.Submit(t.Device.ToString(), t.Motors, durationMs: 100),
        ///       t => bHaptics.Submit(t.Device.ToString(), t.Motors, durationMs: 20),
        ///       intensityTable);
        /// </summary>
        public BHapticsSink(Action<TactorTarget> play, Action<TactorTarget> playContinuous,
                            CueIntensityTable intensity)
            : this(play, intensity)
        {
            _playContinuous = playContinuous;
        }

        /// <summary>
        /// Per-site-gain sink [Plan Task 3.1 / E1]: each <see cref="HapticSite"/> drives at its own calibrated
        /// intensity so PERCEIVED salience can be matched across the X40 chest (40 motors) and the 3-motor
        /// Tactosys. The waveform/timing is unchanged — only the per-site drive level differs.
        /// </summary>
        public BHapticsSink(Action<TactorTarget> play, CueIntensityTable intensity)
        {
            _play = play ?? (t =>
                Debug.Log($"[bHaptics STUB] {t.Device} motors=[{string.Join(",", t.Motors)}] @ {t.Intensity:F2}"));
            _intensity = intensity ?? CueIntensityTable.Uniform(1f);
        }

        public void Fire(in FeedbackCommand command)
        {
            if (command.Modality != Modality.Haptic) return; // visual handled elsewhere

            // The delivered level is the calibrated per-site gain SCALED BY the command's own intensity.
            // Discrete commands always carry 1.0, so this is a no-op for them and the five discrete
            // conditions are bit-identical to before. For PBC it is the manipulation itself: dropping
            // command.Intensity here would flatten the continuous cue into a constant one and destroy H4'.
            float level = _intensity.For(command.Site) * command.Intensity;
            TactorTarget target = BHapticsTactorMap.For(command.Site, level);

            if (command.Form == CueForm.Continuous)
            {
                // Refuse loudly rather than downgrading. A continuous cue rendered through the discrete
                // action would look correct in every log while silently making PBC identical to PB — the
                // same class of invisible failure as the old vest fallback in BHapticsTactorMap.
                if (_playContinuous == null)
                    throw new InvalidOperationException(
                        "A continuous cue (Condition.PBC) reached a BHapticsSink built without a " +
                        "continuous playback action. Use the three-argument constructor and supply a " +
                        "short-duration submit call. Rendering it through the discrete action would " +
                        "silently turn PBC into PB and invalidate H4'.");
                _playContinuous(target);
                return;
            }

            _play(target);
        }
    }
}
