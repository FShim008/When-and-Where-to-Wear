using UnityEngine;

namespace CollisionFeedback.Runtime
{
    /// <summary>
    /// Fills a <see cref="BodyTrackerRig"/> from whatever real devices are assigned, synthesizing a proxy for
    /// any joint left empty. Despite the historical name this is the **sanctioned Paper 1 rig**, not a
    /// temporary hack.
    ///
    /// PAPER 1 PRODUCTION CONFIGURATION [PAPER1_STUDY_DESIGN §11, decided 2026-09-11]:
    ///  • head  = the XR camera (REAL)
    ///  • hands = the two held controllers (REAL) — retained through measured blocks for ecological validity,
    ///            since room-scale VR gameplay is performed holding controllers. Because a controller sits at
    ///            the hand there is no wrist-to-hand offset to define, unlike a wrist-mounted tracker.
    ///  • chest, both feet = Ultimate Trackers (REAL) via <see cref="chestTracker"/> /
    ///            <see cref="leftFootController"/> / <see cref="rightFootController"/>
    ///
    /// That is three trackers, and no proxies. The per-joint startup log states REAL or PROXY for each joint
    /// precisely so a silent fallback cannot masquerade as a working session: a proxy joint is not a
    /// measurement, and proxy foot data is meaningless for foot-collision outcomes.
    ///
    /// PROXY FALLBACKS (protocol validation only, never confirmatory data): chest becomes a point a fixed drop
    /// below the HMD, and feet become floor points under the participant's ground position. Useful for running
    /// the pipeline end to end before hardware is available.
    ///
    /// NOTE FOR PAPER 2: the controllers-in-hand decision is Paper 1 only. Paper 2 estimates mid-flight braking
    /// at millisecond resolution, where added hand mass acts on the quantity being measured, and it must record
    /// its own choice.
    ///
    /// Runs early (negative execution order) so the rig is populated before the session drivers read it.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(BodyTrackerRig))]
    public sealed class ControllerTrackerStandIn : MonoBehaviour
    {
        [Header("Real devices — drag these in")]
        [Tooltip("XR camera (head). Falls back to Camera.main if left empty.")]
        public Transform head;
        [Tooltip("Left-hand controller Transform (the XR Origin's Left Controller).")]
        public Transform leftHand;
        [Tooltip("Right-hand controller Transform (the XR Origin's Right Controller).")]
        public Transform rightHand;

        [Header("Real trackers — assign what you have; anything left empty is synthesized")]
        [Tooltip("Chest VIVE Ultimate Tracker. Empty = a proxy below the HMD (interim only).")]
        public Transform chestTracker;
        [Tooltip("Left-ankle Ultimate Tracker (or a controller strapped to the foot). Empty = floor proxy.")]
        public Transform leftFootController;
        [Tooltip("Right-ankle Ultimate Tracker (or a controller strapped to the foot). Empty = floor proxy.")]
        public Transform rightFootController;

        [Header("Proxy geometry (m)")]
        [Tooltip("Chest proxy sits this far below the HMD.")]
        public float chestDropFromHead = 0.45f;
        [Tooltip("Half the distance between the two feet.")]
        public float footHalfWidth = 0.18f;
        [Tooltip("Feet sit this far in front of your ground position.")]
        public float footForward = 0.08f;
        [Tooltip("Floor height in world space (y). Usually 0 at the XR Origin floor.")]
        public float floorY = 0f;

        private BodyTrackerRig _rig;
        private Transform _chestProxy, _leftFootProxy, _rightFootProxy;

        private void Awake()
        {
            _rig = GetComponent<BodyTrackerRig>();

            // Auto-discover the XR rig transforms whenever a slot is left empty, so this works as a pure DROP-IN
            // on the standard XRI "XR Origin (XR Rig)" with NO manual wiring: head = the main camera, hands = the
            // "Left/Right Controller" objects. Anything you DO assign by hand takes priority over the search.
            if (head == null) head = Camera.main != null ? Camera.main.transform : FindByName("Main Camera");
            if (leftHand == null)  leftHand  = FindByName("Left Controller", "LeftHand Controller", "Left Hand");
            if (rightHand == null) rightHand = FindByName("Right Controller", "RightHand Controller", "Right Hand");

            _rig.head = head;
            _rig.leftHand = leftHand;
            _rig.rightHand = rightHand;

            // Real tracker wins over the proxy. With the 3+1 Ultimate Tracker kit the intended baseline is
            // head = HMD, hands = controllers, chest + both ankles = real trackers, which leaves no proxies.
            if (chestTracker != null) _rig.chest = chestTracker;
            else { _chestProxy = NewProxy("ChestProxy (stand-in)"); _rig.chest = _chestProxy; }

            if (leftFootController != null) _rig.leftFoot = leftFootController;
            else { _leftFootProxy = NewProxy("LeftFootProxy (controller stand-in)"); _rig.leftFoot = _leftFootProxy; }

            if (rightFootController != null) _rig.rightFoot = rightFootController;
            else { _rightFootProxy = NewProxy("RightFootProxy (controller stand-in)"); _rig.rightFoot = _rightFootProxy; }

            if (!_rig.IsComplete)
                Debug.LogWarning("[ControllerTrackerStandIn] Rig still incomplete — " +
                                 $"head={(head ? head.name : "MISSING")}, leftHand={(leftHand ? leftHand.name : "MISSING")}, " +
                                 $"rightHand={(rightHand ? rightHand.name : "MISSING")}. Assign the MISSING slot(s) on the " +
                                 "component, or confirm your XR rig objects are named 'Main Camera' / 'Left Controller' / 'Right Controller'.");
            else
            {
                // Say per joint whether it is REAL or a proxy. During a tracker pilot the operator must be able
                // to confirm at a glance that chest and feet are genuinely tracked, because a silent fallback to
                // proxies would look like a working session while producing meaningless foot and torso data.
                int proxies = (_chestProxy != null ? 1 : 0) + (_leftFootProxy != null ? 1 : 0)
                            + (_rightFootProxy != null ? 1 : 0);
                Debug.Log("[ControllerTrackerStandIn] Rig complete — " +
                          $"head={head.name} (REAL), leftHand={leftHand.name} (REAL), rightHand={rightHand.name} (REAL), " +
                          $"chest={(chestTracker != null ? chestTracker.name + " (REAL)" : "PROXY")}, " +
                          $"leftFoot={(leftFootController != null ? leftFootController.name + " (REAL)" : "PROXY")}, " +
                          $"rightFoot={(rightFootController != null ? rightFootController.name + " (REAL)" : "PROXY")}. " +
                          (proxies == 0
                              ? "All six joints are real devices."
                              : $"{proxies} joint(s) synthesized — those joints are NOT measurements."));
            }

            UpdateProxies();
        }

        // Keep proxies glued to the participant every frame, AFTER the XR poses update.
        private void LateUpdate() => UpdateProxies();

        private void UpdateProxies()
        {
            if (head == null) return;
            Vector3 h = head.position;

            if (_chestProxy != null)
                _chestProxy.position = new Vector3(h.x, h.y - chestDropFromHead, h.z);

            // Feet track your GROUND position (head projected to the floor), split left/right, nudged forward.
            Vector3 fwd = head.forward; fwd.y = 0f;
            fwd = fwd.sqrMagnitude > 1e-4f ? fwd.normalized : Vector3.forward;
            Vector3 right = new Vector3(fwd.z, 0f, -fwd.x);                 // rightward on the floor plane
            Vector3 ground = new Vector3(h.x, floorY, h.z) + fwd * footForward;
            if (_leftFootProxy != null)  _leftFootProxy.position  = ground - right * footHalfWidth;
            if (_rightFootProxy != null) _rightFootProxy.position = ground + right * footHalfWidth;
        }

        private Transform NewProxy(string n)
        {
            var go = new GameObject(n);
            go.transform.SetParent(transform, worldPositionStays: false);
            return go.transform;
        }

        // Find an ACTIVE scene object by any of the given names (first match wins). Used to auto-wire the XR rig
        // when the Inspector slots are left empty, so the stand-in is a true drop-in.
        private static Transform FindByName(params string[] names)
        {
            foreach (string n in names)
            {
                GameObject go = GameObject.Find(n);
                if (go != null) return go.transform;
            }
            return null;
        }
    }
}
