using System.Collections;
using UnityEngine;
using Bhaptics.SDK2;                 // BhapticsLibrary, PositionType (from the installed plugin)
using CollisionFeedback.Core;        // BHapticsDevice, TactorTarget, IFeedbackSink
using CollisionFeedback.Runtime;     // BHapticsSink

namespace CollisionFeedback.Integration
{
    /// <summary>
    /// Live bHaptics binding — the one piece that touches the SDK. Deliberately placed OUTSIDE the
    /// CollisionFeedback.Runtime asmdef (so it compiles into Assembly-CSharp) because an asmdef assembly
    /// cannot reference Assembly-CSharp, where Asset Store plugins often live; from Assembly-CSharp we can
    /// see BOTH the bHaptics SDK and our auto-referenced Core/Runtime assemblies.
    ///
    /// Turns each safety <see cref="FeedbackCommand"/> into a bHaptics PlayMotors pulse on the at-risk
    /// limb's device. Body-localization = which DEVICE fires (vest / hand L|R / foot L|R).
    /// </summary>
    public static class HapticDeviceBinding
    {
        /// <summary>
        /// DIAGNOSTIC ONLY — a single-pulse sink for the localization self-test sweep
        /// (<see cref="HapticSelfTest"/>). This is NOT the study cue: live sessions must use
        /// <see cref="CreateThreePulseSink"/>, which plays the fixed 3-pulse waveform [Protocol 2.1].
        /// </summary>
        public static BHapticsSink CreateDiagnosticSink(float intensity = 1f, int pulseMillis = 100)
        {
            return new BHapticsSink(t =>
            {
                // Whole-device pulse: body-localization is per-DEVICE (vest / hand L|R / foot L|R), so we
                // fire every motor on the at-risk limb's device. The array length MUST match what the
                // bHaptics PlayMotors API expects per position, or the device ignores the call.
                int count = MotorCountFor(t.Device);
                var motors = new int[count];
                int val = Mathf.Clamp(Mathf.RoundToInt(t.Intensity * 100f), 0, 100);
                for (int i = 0; i < count; i++) motors[i] = val;
                BhapticsLibrary.PlayMotors((int)PositionFor(t.Device), motors, pulseMillis);
            }, intensity);
        }

        /// <summary>
        /// Live sink that plays the study's FIXED 3-pulse cue (identical across all conditions — only WHEN
        /// and WHERE differ). Needs a MonoBehaviour <paramref name="host"/> to run the pulse-timing coroutine,
        /// since a plain sink can't. Default: 3 pulses of 100 ms with 60 ms gaps (≈480 ms total) [Protocol 2.1].
        /// </summary>
        public static BHapticsSink CreateThreePulseSink(MonoBehaviour host, float intensity = 1f,
                                                        int pulseMillis = 100, int gapMillis = 60, int pulses = 3)
        {
            return new BHapticsSink(
                t => host.StartCoroutine(ThreePulseRoutine(t, pulseMillis, gapMillis, pulses)), intensity);
        }

        /// <summary>
        /// As <see cref="CreateThreePulseSink(MonoBehaviour,float,int,int,int)"/> but with PER-SITE calibrated
        /// gains [Plan Task 3.1 / E1] — chest / hands / feet each drive at their own intensity so perceived
        /// salience is matched (load the table via Runtime <c>CueIntensityFile</c>). Waveform/timing identical.
        /// </summary>
        public static BHapticsSink CreateThreePulseSink(MonoBehaviour host, CueIntensityTable intensity,
                                                        int pulseMillis = 100, int gapMillis = 60, int pulses = 3)
        {
            return new BHapticsSink(
                t => host.StartCoroutine(ThreePulseRoutine(t, pulseMillis, gapMillis, pulses)), intensity);
        }

        /// <summary>
        /// THE SINK THE STUDY RUNS. Discrete conditions get the fixed 3-pulse cue; <see cref="Condition.PBC"/>
        /// gets the continuous, intensity-modulated cue that carries H4'
        /// [docs/PAPER1_PARAMETER_JUSTIFICATION.md §5B].
        ///
        /// Use this rather than <see cref="CreateThreePulseSink(MonoBehaviour,CueIntensityTable,int,int,int)"/>
        /// for any session that schedules PBC. A three-pulse-only sink THROWS on a continuous command rather
        /// than downgrading it — deliberately, because a silent downgrade would make PBC identical to PB and
        /// H4' would compare a condition against itself while every log still looked correct.
        /// </summary>
        /// <param name="continuousMillis">
        /// Duration of each continuous submission. Must EXCEED <paramref name="continuousIntervalMillis"/> or
        /// the cue gaps audibly between refreshes. It is also how long the cue lingers after the last frame:
        /// <c>ConditionManager</c> signals "engaged" by emitting and "disengaged" by ceasing to emit, so
        /// nothing explicitly stops the cue — it expires. Keep it short.
        /// </param>
        /// <param name="continuousIntervalMillis">
        /// Minimum wall-clock gap between submissions. <c>ConditionManager</c> emits every frame (~90 Hz);
        /// bHaptics devices are driven over BLE and will not sustain that, so submissions are throttled.
        /// </param>
        public static BHapticsSink CreateStudySink(MonoBehaviour host, float intensity,
                                                   int pulseMillis = 100, int gapMillis = 60, int pulses = 3,
                                                   int continuousMillis = 60, int continuousIntervalMillis = 45)
            => CreateStudySink(host, CueIntensityTable.Uniform(intensity), pulseMillis, gapMillis, pulses,
                               continuousMillis, continuousIntervalMillis);

        /// <inheritdoc cref="CreateStudySink(MonoBehaviour,float,int,int,int,int,int)"/>
        public static BHapticsSink CreateStudySink(MonoBehaviour host, CueIntensityTable intensity,
                                                   int pulseMillis = 100, int gapMillis = 60, int pulses = 3,
                                                   int continuousMillis = 60, int continuousIntervalMillis = 45)
        {
            var gate = new ContinuousCueGate(continuousMillis, continuousIntervalMillis);
            LastContinuousGate = gate;
            return new BHapticsSink(
                t => host.StartCoroutine(ThreePulseRoutine(t, pulseMillis, gapMillis, pulses)),
                gate.Submit,
                intensity);
        }

        /// <summary>
        /// The gate created by the most recent <see cref="CreateStudySink"/>, exposed so the session runner
        /// can log its counters per block. The throttle below is a real approximation and must be MEASURED,
        /// not assumed — see <see cref="ContinuousCueGate"/>.
        /// </summary>
        public static ContinuousCueGate LastContinuousGate { get; private set; }

        /// <summary>
        /// Rate-limits continuous-cue submissions to the device and records what it dropped.
        ///
        /// WHY A THROTTLE AT ALL. <c>ConditionManager</c> emits a continuous command every frame (~90 Hz).
        /// bHaptics units are driven over BLE and do not sustain that rate; submitting anyway risks queueing
        /// or dropped packets inside the SDK, either of which would distort the cue that H4' measures.
        ///
        /// ⚠ WHAT IS NOT KNOWN WITHOUT HARDWARE. It is unverified whether <c>BhapticsLibrary.PlayMotors</c>
        /// REPLACES an in-flight command on the same position or QUEUES behind it. This gate assumes replace,
        /// with <c>continuousMillis</c> slightly exceeding the interval so consecutive submissions overlap
        /// rather than gap. If the SDK queues, that overlap would accumulate latency across an approach and
        /// the cue would lag the participant's motion — which is precisely the kind of distortion that would
        /// invalidate H4' while still producing a plausible log.
        ///
        /// VERIFY ON HARDWARE BEFORE COLLECTING: drive a slow approach, watch the tactor, and confirm the
        /// level tracks TTC and stops within <c>continuousMillis</c> of the limb disengaging. The latency
        /// bench is the natural place to do it.
        ///
        /// The counters exist so the throttle is measurable rather than invisible: if
        /// <see cref="Dropped"/> dwarfs <see cref="Submitted"/>, the cue is steppier than designed and
        /// <see cref="ContinuousCueMapping"/>'s smooth ramp is not what reached the skin.
        /// </summary>
        public sealed class ContinuousCueGate
        {
            private readonly int _durationMillis;
            private readonly float _intervalSeconds;
            private float _nextAllowedTime = float.NegativeInfinity;
            private int _lastValue = -1;

            public int Submitted { get; private set; }
            public int Dropped { get; private set; }

            /// <summary>Fraction of continuous commands that actually reached the device.</summary>
            public float SubmitRate => (Submitted + Dropped) == 0 ? 1f : Submitted / (float)(Submitted + Dropped);

            public ContinuousCueGate(int durationMillis, int intervalMillis)
            {
                _durationMillis = durationMillis;
                _intervalSeconds = intervalMillis / 1000f;
            }

            public void ResetCounters() { Submitted = 0; Dropped = 0; }

            public void Submit(TactorTarget t)
            {
                int count = MotorCountFor(t.Device);
                int val = Mathf.Clamp(Mathf.RoundToInt(t.Intensity * 100f), 0, 100);

                // Throttle on wall time, not the data clock: this is a device-rate limit, not a study timing
                // decision. The Core cue logic stays driven by PoseFrame.Timestamp and is unaffected.
                float now = Time.realtimeSinceStartup;
                bool due = now >= _nextAllowedTime;

                // Always push a CHANGE TO SILENCE immediately. Waiting out the throttle to stop a cue would
                // leave the tactor buzzing after the limb disengaged, which participants would feel as the
                // cue being "stuck" and which would corrupt the engagement-duration measure.
                bool goingSilent = val == 0 && _lastValue != 0;

                if (!due && !goingSilent) { Dropped++; return; }

                var motors = new int[count];
                for (int i = 0; i < count; i++) motors[i] = val;
                BhapticsLibrary.PlayMotors((int)PositionFor(t.Device), motors, _durationMillis);

                _lastValue = val;
                _nextAllowedTime = now + _intervalSeconds;
                Submitted++;
            }
        }

        /// <summary>
        /// Play the study's fixed 3-pulse cue ONCE on a single <paramref name="site"/> at an explicit drive
        /// (0..1) — the playback primitive for the E2 cue-intensity calibration
        /// (<c>CueIntensityCalibrationRunner</c>), which must fire one site at a chosen trial level OUTSIDE the
        /// alert pipeline. Start it as a coroutine on a MonoBehaviour host. Waveform/timing identical to the live
        /// cue, so the perceptual match is made on the same stimulus the study delivers.
        /// </summary>
        public static IEnumerator PlayThreePulse(HapticSite site, float intensity,
                                                 int pulseMillis = 100, int gapMillis = 60, int pulses = 3)
            => ThreePulseRoutine(BHapticsTactorMap.For(site, intensity), pulseMillis, gapMillis, pulses);

        /// <summary>
        /// Play a SINGLE held step at <paramref name="intensity"/> for <paramref name="millis"/> — the first
        /// moment of the continuous cue, with no ramp. This is the TEST stimulus for
        /// <see cref="CollisionFeedback.Core.ContinuousOnsetCalibration"/>.
        ///
        /// The ramp is deliberately absent. Including it would let the later, stronger part of the cue carry
        /// the participant's judgment, and they would be matching the whole approach instead of its onset —
        /// the one moment that decides whether the warning effectively arrives on time.
        /// </summary>
        public static IEnumerator PlayHeldOnset(HapticSite site, float intensity, int millis)
        {
            TactorTarget t = BHapticsTactorMap.For(site, intensity);
            int count = MotorCountFor(t.Device);
            var motors = new int[count];
            int val = Mathf.Clamp(Mathf.RoundToInt(t.Intensity * 100f), 0, 100);
            for (int i = 0; i < count; i++) motors[i] = val;

            BhapticsLibrary.PlayMotors((int)PositionFor(t.Device), motors, millis);
            yield return new WaitForSeconds(millis / 1000f);
        }

        private static IEnumerator ThreePulseRoutine(TactorTarget t, int pulseMillis, int gapMillis, int pulses)
        {
            int count = MotorCountFor(t.Device);
            var motors = new int[count];
            int val = Mathf.Clamp(Mathf.RoundToInt(t.Intensity * 100f), 0, 100);
            for (int i = 0; i < count; i++) motors[i] = val;
            int position = (int)PositionFor(t.Device);

            float wait = (pulseMillis + gapMillis) / 1000f;
            for (int p = 0; p < pulses; p++)
            {
                BhapticsLibrary.PlayMotors(position, motors, pulseMillis);
                yield return new WaitForSeconds(wait);
            }
        }

        /// <summary>
        /// Immediately silence every bHaptics motor — call on emergency stop / block end [Plan Task 4.5 / D5].
        /// Safe to call when the SDK isn't initialized (nothing is playing).
        /// </summary>
        public static void StopAll()
        {
            try { BhapticsLibrary.StopAll(); }
            catch (System.Exception e) { Debug.LogWarning($"[HapticDeviceBinding] StopAll failed: {e.Message}"); }
        }

        /// <summary>
        /// Logical device -> bHaptics SDK position.
        ///
        /// ⚠ **The default arm used to be `PositionType.Vest`.** That silently undid the H2 fix: Core
        /// correctly routes the generic cue to <see cref="BHapticsDevice.TactosyTorso"/>, and this layer
        /// then sent it to the vest anyway, re-creating the device confound one level down where no Core
        /// test could see it. Unmapped devices now throw (2026-09-21).
        /// </summary>
        private static PositionType PositionFor(BHapticsDevice d) => d switch
        {
            BHapticsDevice.VestFront    => PositionType.Vest,
            BHapticsDevice.VestBack     => PositionType.Vest,
            BHapticsDevice.HandLeft     => PositionType.HandL,   // Tactosy for Hands @ HandL (back of hand)
            BHapticsDevice.HandRight    => PositionType.HandR,
            BHapticsDevice.FootLeft     => PositionType.FootL,
            BHapticsDevice.FootRight    => PositionType.FootR,

            // ⛔ HARDWARE DECISION NOT YET MADE. The sternum unit is not bought or paired, so which
            // PositionType it enumerates as is unknown. The SDK has no torso-Tactosy position - the
            // available slots are ForearmL/R, HandL/R, FootL/R, Head, Vest - so the sternum unit will
            // register as whichever slot it is paired into, most likely ForearmL or ForearmR.
            //
            // Set this from the actual paired device, then run the §2 confusability check. Do NOT map it
            // to Vest: the vest carries projectile-hit feedback and must never carry a warning cue.
            BHapticsDevice.TactosyTorso => throw new System.NotImplementedException(
                "TactosyTorso has no PositionType yet. Pair the sternum Tactosy, see which slot it "
                + "enumerates as (likely ForearmL/ForearmR), and set it here. Mapping it to Vest would "
                + "re-create the H2 device confound - PAPER1_STUDY_DESIGN §2."),

            _ => throw new System.ArgumentOutOfRangeException(
                     nameof(d), d, "No SDK position mapped for this device. Add one explicitly; do not "
                                 + "fall back to the vest."),
        };

        // Motor-array length per position. X40 vest = 40 motors (probe confirmed it fires at 40, so we use
        // all of them); hand/foot Tactosy take 3. (PlayMotors tolerates length; matching the device fires
        // every motor.)
        private static int MotorCountFor(BHapticsDevice d) => d switch
        {
            BHapticsDevice.VestFront or BHapticsDevice.VestBack => 40,
            _ => 3,
        };
    }
}
