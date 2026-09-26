using UnityEngine;

namespace CollisionFeedback.Runtime
{
    /// <summary>
    /// Renders a minimal self-representation — by default a small marker at each tracked ANKLE — so the
    /// participant has visual information about where their own limbs are.
    ///
    /// WHY THIS EXISTS [docs/VIRTUAL_HAZARD_VALIDITY.md §3.5, decided 2026-09-14]:
    ///
    /// 1. **Exproprioceptive symmetry.** Paper 1 runs with controllers in both hands (§11), and XRI renders
    ///    controller models, so the participant SEES their hands. Feet and torso are tracked but invisible.
    ///    That asymmetry means a hand-targeted opportunity is avoidable with visual self-knowledge while a
    ///    foot-targeted one is not — a nuisance difference between target limbs that has nothing to do with
    ///    the manipulated factors. Ankle markers remove it.
    ///
    /// 2. **It shrinks the real-vs-virtual gap a reviewer would attack.** Occluding the limbs IN THE REAL
    ///    WORLD already increases toe clearance and leaves baseline gait unchanged; VR without a limb
    ///    representation amplifies the same adaptation (greater clearance, wider base of support, slower
    ///    walking). A meaningful share of "VR behaves differently" is missing limb vision rather than
    ///    virtuality — and limb vision is under our control.
    ///
    /// WHAT THIS IS NOT: it is not a hazard cue and not a condition. It renders IDENTICALLY in all six
    /// conditions including None, is never driven by <see cref="CollisionFeedback.Core.ConditionManager"/>,
    /// and must be frozen before data collection. A self-representation that varied by condition would be a
    /// second uncontrolled feedback channel.
    ///
    /// MARKER SIZE IS DELIBERATELY NOT THE CONTACT RADIUS. The marker indicates where the limb IS. Drawing it
    /// at the capsule contact radius would disclose the collision geometry itself, which is closer to a
    /// permanent proximity aid than to ordinary body awareness. Keep <see cref="markerDiameter"/> below the
    /// smallest contact radius and say so in the manuscript.
    ///
    /// HANDS AND CHEST default OFF. Hands are already visible as controller models; enable
    /// <see cref="showHands"/> only if controller models are disabled. A sternum marker sits in the centre of
    /// the downward field of view and can occlude the task, so <see cref="showChest"/> stays off unless piloting
    /// shows torso opportunities need it — and if it goes on, it goes on before data collection, not during.
    ///
    /// Runs after <see cref="ControllerTrackerStandIn"/> (-100) so the rig is populated, and follows the poses
    /// in LateUpdate.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public sealed class SelfRepresentation : MonoBehaviour
    {
        [Header("Which limbs to represent (FREEZE before data collection)")]
        [Tooltip("Ankle markers. ON by default — this is the decided Paper 1 configuration.")]
        [SerializeField] private bool showFeet = true;
        [Tooltip("Hand markers. OFF by default: held controllers already render. Enable ONLY if you have " +
                 "disabled controller models.")]
        [SerializeField] private bool showHands = false;
        [Tooltip("Sternum marker. OFF by default: it occludes the centre of the downward view.")]
        [SerializeField] private bool showChest = false;

        [Header("Appearance")]
        [Tooltip("Marker diameter (m). Keep BELOW the smallest limb contact radius so the marker indicates " +
                 "limb position rather than disclosing contact geometry.")]
        [SerializeField] private float markerDiameter = 0.07f;
        [Tooltip("Neutral marker colour. Deliberately NOT the green→red of the Visual condition glow, so a " +
                 "marker can never be mistaken for a hazard cue.")]
        [SerializeField] private Color markerColor = new(0.62f, 0.68f, 0.78f, 0.85f);

        [Header("Rig")]
        [Tooltip("Leave empty to find the BodyTrackerRig automatically.")]
        [SerializeField] private BodyTrackerRig rig;

        private Transform _leftFoot, _rightFoot, _leftHand, _rightHand, _chest;
        private Material _material;

        private void Awake()
        {
            if (rig == null) rig = GetComponent<BodyTrackerRig>();
            if (rig == null) rig = FindFirstObjectByType<BodyTrackerRig>();
            if (rig == null)
            {
                Debug.LogWarning("[SelfRepresentation] No BodyTrackerRig found — no self-representation will " +
                                 "be drawn. If that is intended, remove this component so the configuration " +
                                 "is explicit rather than accidental.");
                return;
            }

            _material = BuildMaterial();

            if (showFeet)
            {
                _leftFoot = NewMarker("SelfMarker_LeftFoot");
                _rightFoot = NewMarker("SelfMarker_RightFoot");
            }
            if (showHands)
            {
                _leftHand = NewMarker("SelfMarker_LeftHand");
                _rightHand = NewMarker("SelfMarker_RightHand");
            }
            if (showChest) _chest = NewMarker("SelfMarker_Chest");

            // State this at startup so the operator can confirm the configuration matches the frozen protocol.
            // A session that silently ran a different self-representation than the rest of the sample is a
            // between-participant confound that would be invisible in the logs otherwise.
            Debug.Log($"[SelfRepresentation] feet={(showFeet ? "ON" : "off")}, " +
                      $"hands={(showHands ? "ON" : "off (controller models)")}, " +
                      $"chest={(showChest ? "ON" : "off")}, marker={markerDiameter:F3} m. " +
                      "Identical in every condition — verify this matches the frozen protocol.");

            UpdateMarkers();
        }

        private void LateUpdate() => UpdateMarkers();

        private void UpdateMarkers()
        {
            if (rig == null) return;
            Place(_leftFoot, rig.leftFoot);
            Place(_rightFoot, rig.rightFoot);
            Place(_leftHand, rig.leftHand);
            Place(_rightHand, rig.rightHand);
            Place(_chest, rig.chest);
        }

        // Hide rather than strand a marker when its joint is missing, so a marker can never sit frozen at the
        // origin looking like a tracked limb.
        private static void Place(Transform marker, Transform joint)
        {
            if (marker == null) return;
            bool ok = joint != null;
            if (marker.gameObject.activeSelf != ok) marker.gameObject.SetActive(ok);
            if (ok) marker.position = joint.position;
        }

        private Transform NewMarker(string n)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = n;
            go.transform.SetParent(transform, worldPositionStays: true);
            go.transform.localScale = Vector3.one * markerDiameter;

            // Markers are visual only. A collider here would participate in scene physics and could disturb
            // the task objects the participant is reaching for.
            Collider c = go.GetComponent<Collider>();
            if (c != null) Destroy(c);

            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = _material;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return go.transform;
        }

        // Unlit transparent, URP first with a built-in fallback — same approach VisualObstacleAlert uses for
        // its veil, so the markers render correctly whichever pipeline the scene is on.
        private Material BuildMaterial()
        {
            // Explicit null check rather than ?? — Unity's Object equality override makes null-coalescing on a
            // UnityEngine.Object a trap worth avoiding even where it happens to work.
            Shader s = Shader.Find("Universal Render Pipeline/Unlit");
            if (s == null) s = Shader.Find("Unlit/Color");
            if (s == null) s = Shader.Find("Sprites/Default");
            var m = new Material(s) { name = "SelfMarkerMaterial" };
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", markerColor);
            if (m.HasProperty("_Color")) m.SetColor("_Color", markerColor);

            // Transparent surface so a marker overlapping the participant's view of a task object dims it
            // rather than hiding it.
            if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            return m;
        }

        private void OnDestroy()
        {
            if (_material != null) Destroy(_material);
        }
    }
}
