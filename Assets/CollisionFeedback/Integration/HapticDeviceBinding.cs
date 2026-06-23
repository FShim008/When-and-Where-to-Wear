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

        private static PositionType PositionFor(BHapticsDevice d) => d switch
        {
            BHapticsDevice.VestFront    => PositionType.Vest,
            BHapticsDevice.VestBack     => PositionType.Vest,
            BHapticsDevice.HandLeft     => PositionType.HandL,   // Tactosy for Hands @ HandL (back of hand)
            BHapticsDevice.HandRight    => PositionType.HandR,
            BHapticsDevice.FootLeft     => PositionType.FootL,
            BHapticsDevice.FootRight    => PositionType.FootR,
            _                           => PositionType.Vest,
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
