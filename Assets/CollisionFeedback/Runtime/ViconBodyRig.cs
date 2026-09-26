using UnityEngine;
using CollisionFeedback.Core;

namespace CollisionFeedback.Runtime
{
    /// <summary>
    /// Feeds the study's <see cref="BodyTrackerRig"/> from VICON instead of VIVE Ultimate Trackers, by mapping
    /// Vicon room coordinates into Unity/SteamVR space through a calibrated <see cref="RigidAlignment"/>.
    ///
    /// WHY VICON: the collision detector decides contact at a 0.03 m band. Inside-out tracker error is the same
    /// order as that threshold, so a meaningful share of collision/no-collision calls would be tracking noise;
    /// Vicon's sub-millimetre accuracy leaves two orders of magnitude of headroom and genuinely realizes the
    /// study's "idealized oracle" framing.
    ///
    /// HOW IT PLUGS IN: identical pattern to <see cref="ControllerTrackerStandIn"/> — it creates proxy
    /// Transforms, keeps them glued to the aligned Vicon positions every frame, and drops them into the six
    /// <see cref="BodyTrackerRig"/> slots. Nothing downstream changes: SessionRunner / LiveSessionController /
    /// TrackingBench keep reading the rig exactly as before.
    ///
    /// HEAD IS SPECIAL: the head slot uses the HMD Transform DIRECTLY (it is already in Unity/SteamVR space, so
    /// it carries zero alignment error). Vicon supplies only the five cue-able limbs. The HMD marker cluster is
    /// still needed — but for CALIBRATION (see <see cref="ViconCalibrationRecorder"/>), not for tracking.
    ///
    /// SOURCE TRANSFORMS: assign whatever your Vicon Unity plugin exposes per rigid body / marker (the Vicon
    /// DataStream Unity objects). This component is plugin-agnostic — it only reads <c>Transform.position</c>.
    /// If the plugin streams raw millimetres, set <see cref="unitScale"/> = 0.001; if it already converts to
    /// metres, leave it at 1.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(BodyTrackerRig))]
    public sealed class ViconBodyRig : MonoBehaviour
    {
        [Header("Head — the HMD itself (already in Unity space; no alignment applied)")]
        [Tooltip("XR camera. Falls back to Camera.main if left empty.")]
        public Transform head;

        [Header("Vicon limb sources (raw Vicon-space Transforms from the DataStream plugin)")]
        public Transform viconChest;
        public Transform viconLeftHand;
        public Transform viconRightHand;
        public Transform viconLeftFoot;
        public Transform viconRightFoot;

        [Header("Alignment")]
        [Tooltip("Vicon→Unity matrix file (relative names resolve under persistentDataPath). Produced by " +
                 "ViconCalibrationRecorder, or pasted from the calibration notebook.")]
        [SerializeField] private string alignmentFile = ViconAlignmentFile.DefaultName;
        [Tooltip("Multiplier applied to Vicon coordinates BEFORE the transform. 1 = plugin already streams " +
                 "metres; 0.001 = plugin streams millimetres. Getting this wrong makes the fit meaningless.")]
        [SerializeField] private float unitScale = 1f;
        [Tooltip("Warn once per session if the loaded alignment's residual exceeds this (m). The study's " +
                 "contact band is 0.03 m, so a calibration near that value is not usable.")]
        [SerializeField] private float maxAcceptableRms = 0.010f;

        private BodyTrackerRig _rig;
        private RigidAlignment _alignment;
        private Transform _chest, _leftHand, _rightHand, _leftFoot, _rightFoot;

        /// <summary>The alignment currently in use (for HUDs / logging).</summary>
        public RigidAlignment Alignment => _alignment;

        private void Awake()
        {
            if (head == null && Camera.main != null) head = Camera.main.transform;

            _rig = GetComponent<BodyTrackerRig>();
            _alignment = ViconAlignmentFile.Load(alignmentFile);

            if (_alignment.SampleCount > 0 && _alignment.RmsError > maxAcceptableRms)
                Debug.LogWarning($"[ViconBodyRig] alignment residual {_alignment.RmsError * 1000f:F1} mm exceeds " +
                                 $"the {maxAcceptableRms * 1000f:F0} mm limit — re-run the calibration before " +
                                 "collecting data (contact band is 30 mm).");

            _rig.head = head;
            _rig.chest = _chest = NewProxy("ViconChest");
            _rig.leftHand = _leftHand = NewProxy("ViconLeftHand");
            _rig.rightHand = _rightHand = NewProxy("ViconRightHand");
            _rig.leftFoot = _leftFoot = NewProxy("ViconLeftFoot");
            _rig.rightFoot = _rightFoot = NewProxy("ViconRightFoot");

            WarnIfUnassigned();
            UpdateProxies();

            if (_rig.IsComplete)
                Debug.Log($"[ViconBodyRig] rig complete — head={(head ? head.name : "?")} (HMD), 5 limbs from " +
                          $"Vicon via alignment (rms {_alignment.RmsError * 1000f:F1} mm, " +
                          $"{_alignment.SampleCount} samples, unitScale {unitScale}).");
        }

        // Vicon poses update with the plugin; refresh after it has written this frame.
        private void LateUpdate() => UpdateProxies();

        private void UpdateProxies()
        {
            Place(_chest, viconChest);
            Place(_leftHand, viconLeftHand);
            Place(_rightHand, viconRightHand);
            Place(_leftFoot, viconLeftFoot);
            Place(_rightFoot, viconRightFoot);
        }

        // A missing source holds its last position rather than snapping to the origin — a marker dropout must
        // not read as "the limb teleported to the arena centre", which would fabricate collisions.
        private void Place(Transform proxy, Transform source)
        {
            if (proxy == null || source == null || !source.gameObject.activeInHierarchy) return;
            proxy.position = _alignment.Apply(source.position * unitScale);
        }

        private void WarnIfUnassigned()
        {
            if (head == null) Debug.LogWarning("[ViconBodyRig] no head Transform (HMD camera) assigned.");
            if (viconChest == null || viconLeftHand == null || viconRightHand == null ||
                viconLeftFoot == null || viconRightFoot == null)
                Debug.LogWarning("[ViconBodyRig] one or more Vicon limb Transforms are unassigned — those joints " +
                                 "will not move. Assign the DataStream objects for chest, both hands, both feet.");
        }

        private Transform NewProxy(string n)
        {
            var go = new GameObject(n);
            go.transform.SetParent(transform, worldPositionStays: false);
            return go.transform;
        }
    }
}
