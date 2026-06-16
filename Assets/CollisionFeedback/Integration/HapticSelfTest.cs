using System;
using System.Collections;
using UnityEngine;
using Bhaptics.SDK2;                          // BhapticsLibrary, PositionType (connectivity diagnostics)
using CollisionFeedback.Core;
using Joint = CollisionFeedback.Core.Joint;   // disambiguate from UnityEngine.Joint

namespace CollisionFeedback.Integration
{
    /// <summary>
    /// Localization check (Protocol Part E): on Play, buzzes each tactor site in turn (~1.2 s apart) so
    /// you can physically confirm the CORRECT device fires — catches Left/Right swaps and the motor-count
    /// guesses in <see cref="HapticDeviceBinding"/>. Requires the <c>[bHaptics]</c> prefab in the scene and
    /// the bHaptics Player running with all devices paired. Attach to any GameObject and press Play.
    /// </summary>
    public sealed class HapticSelfTest : MonoBehaviour
    {
        [SerializeField] private float intensity = 1f;
        [SerializeField] private float gapSeconds = 1.2f;
        [SerializeField] private int pulseMillis = 400; // longer than the study cue, just to be easy to feel while testing

        private IEnumerator Start()
        {
            IFeedbackSink sink = HapticDeviceBinding.CreateDiagnosticSink(intensity, pulseMillis);
            yield return new WaitForSeconds(3f); // give the SDK<->Player WebSocket time to sync the device list

            // --- connectivity diagnostics -------------------------------------------------
            Debug.Log($"[HapticSelfTest] bHaptics Player installed: {BhapticsLibrary.IsBhapticsAvailable(false)}");

            var all = BhapticsLibrary.GetDevices();
            Debug.Log($"[HapticSelfTest] SDK device list has {all.Count} entr(ies):");
            foreach (var dev in all)
                Debug.Log($"[HapticSelfTest]   name='{dev.DeviceName}' position={dev.Position} connected={dev.IsConnected} addr={dev.Address}");

            // Match the positions our cues actually fire on (arm units are Tactosy-for-Hands @ HandL/HandR).
            var positions = new[]
            {
                PositionType.Vest, PositionType.HandL, PositionType.HandR,
                PositionType.FootL, PositionType.FootR,
            };
            foreach (var pos in positions)
                Debug.Log($"[HapticSelfTest]   IsConnect({pos}) = {BhapticsLibrary.IsConnect(pos)}");

            Debug.Log("[HapticSelfTest] Ping each CONNECTED device:");
            foreach (var pos in positions)
                if (BhapticsLibrary.IsConnect(pos)) BhapticsLibrary.Ping(pos);
            yield return new WaitForSeconds(gapSeconds);
            // ------------------------------------------------------------------------------

            foreach (HapticSite site in Enum.GetValues(typeof(HapticSite)))
            {
                Debug.Log($"[HapticSelfTest] firing {site} -> confirm the expected device buzzes");
                sink.Fire(new FeedbackCommand(site, Modality.Haptic, TriggerKind.Reactive,
                                              Joint.Chest, "selftest", 0.0, 0f, 0f));
                yield return new WaitForSeconds(gapSeconds);
            }
            Debug.Log("[HapticSelfTest] sweep complete");
        }
    }
}
