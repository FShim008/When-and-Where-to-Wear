using System.Collections.Generic;
using UnityEngine;
using CollisionFeedback.Core;

namespace CollisionFeedback.Runtime
{
    /// <summary>
    /// Live Vicon↔Unity co-registration — the in-engine replacement for the CSV-export →
    /// <c>transformation_matrix_new_vicon_vive.ipynb</c> → Colab workflow.
    ///
    /// METHOD (same as the notebook's cell 0, Horn instead of SVD): put a marker cluster on the HMD, then walk
    /// the headset around the capture volume. Every time it moves <see cref="minSampleSpacing"/> from the last
    /// sample, this records a matched pair — HMD position as SteamVR reports it, and as Vicon reports it. With
    /// enough well-spread pairs, <see cref="RigidTransformSolver"/> solves the Vicon→Unity transform.
    ///
    /// WHAT THE NOTEBOOK WAS MISSING, and why this matters: it never reported a fit RESIDUAL. The study's
    /// collision threshold is 0.03 m, so an alignment you cannot certify is tighter than that is an alignment
    /// that may be manufacturing your primary outcome. This tool shows RMS/max residual live and colour-codes it,
    /// so calibration becomes a per-session gate with a pass/fail number instead of an act of faith.
    /// (It also fixes the notebook's cells 2–3, which computed "local marker offsets" by subtracting a
    /// Vive-space position from a Vicon-space marker — two unaligned frames — to derive the very alignment
    /// being solved. Those offsets, and any transform built on them, are not trustworthy.)
    ///
    /// SPREAD MATTERS AS MUCH AS COUNT: samples bunched in one corner give a well-fitting but badly conditioned
    /// transform that degrades across the arena. Cover the whole play space, at several heights, including
    /// crouches — the readout shows the sampled bounding box so you can see the coverage.
    ///
    /// Drop on any GameObject, assign the two Transforms, press Play, hold the headset, and walk the volume.
    /// </summary>
    public sealed class ViconCalibrationRecorder : MonoBehaviour
    {
        [Header("The two views of the SAME physical point (the HMD)")]
        [Tooltip("HMD in Unity/SteamVR space — the XR camera. Falls back to Camera.main.")]
        public Transform hmdUnity;
        [Tooltip("The Vicon rigid body / marker cluster mounted on the HMD, as the DataStream plugin exposes it.")]
        public Transform hmdVicon;

        [Header("Sampling")]
        [Tooltip("Multiplier applied to Vicon coordinates. 1 = plugin streams metres, 0.001 = millimetres. " +
                 "MUST match ViconBodyRig.unitScale — a rigid fit cannot absorb a unit error.")]
        [SerializeField] private float unitScale = 1f;
        [Tooltip("Minimum movement (m) between auto-recorded samples — enforces spatial spread.")]
        [SerializeField] private float minSampleSpacing = 0.15f;
        [SerializeField] private int targetSamples = 60;
        [Tooltip("Where to write the matrix (relative names resolve under persistentDataPath).")]
        [SerializeField] private string outputFile = ViconAlignmentFile.DefaultName;

        [Header("Quality gate (m)")]
        [Tooltip("RMS at or below this is a good calibration (green).")]
        [SerializeField] private float goodRms = 0.005f;
        [Tooltip("RMS above this is a failed calibration — redo it (red). The contact band is 0.030 m.")]
        [SerializeField] private float badRms = 0.010f;

        private readonly List<Vector3> _vicon = new();
        private readonly List<Vector3> _unity = new();
        private bool _recording;
        private RigidAlignment _fit = RigidAlignment.Identity;
        private bool _solved;
        private string _message = "";
        private GUIStyle _h, _p, _small;

        private void Awake()
        {
            if (hmdUnity == null && Camera.main != null) hmdUnity = Camera.main.transform;
        }

        private void Update()
        {
            if (!_recording || hmdUnity == null || hmdVicon == null) return;

            Vector3 u = hmdUnity.position;
            Vector3 v = hmdVicon.position * unitScale;

            // Spacing gate: only take a sample once the headset has actually moved somewhere new.
            if (_unity.Count > 0 && Vector3.Distance(u, _unity[^1]) < minSampleSpacing) return;

            _unity.Add(u);
            _vicon.Add(v);
            if (_unity.Count >= RigidTransformSolver.MinSamples) Solve();
        }

        private void Solve()
        {
            _fit = RigidTransformSolver.Solve(_vicon, _unity);   // Vicon → Unity
            _solved = _fit.Valid;
        }

        /// <summary>Bounding-box diagonal of the sampled Unity positions — the coverage indicator.</summary>
        private float SpreadDiagonal()
        {
            if (_unity.Count < 2) return 0f;
            Vector3 min = _unity[0], max = _unity[0];
            foreach (Vector3 p in _unity)
            {
                min = Vector3.Min(min, p);
                max = Vector3.Max(max, p);
            }
            return (max - min).magnitude;
        }

        private void OnGUI()
        {
            EnsureStyles();
            var area = new Rect(24, 24, Mathf.Min(720f, Screen.width - 48f), 430f);
            GUILayout.BeginArea(area, GUI.skin.box);

            GUILayout.Label("Vicon → Unity calibration", _h);
            GUILayout.Label("Mount a marker cluster on the HMD, press Record, then walk the headset around the " +
                            "whole play space at several heights (include crouches). Samples auto-record as you move.", _small);
            GUILayout.Space(6);

            bool ready = hmdUnity != null && hmdVicon != null;
            if (!ready)
                GUILayout.Label("⚠ Assign BOTH the HMD (Unity) and the HMD's Vicon cluster Transform.", _p);

            GUILayout.Label($"Samples: {_unity.Count} / {targetSamples}      " +
                            $"coverage: {SpreadDiagonal():F2} m diagonal", _p);

            if (_solved)
            {
                Color prev = GUI.color;
                GUI.color = _fit.RmsError <= goodRms ? Color.green
                          : _fit.RmsError <= badRms ? Color.yellow : Color.red;
                GUILayout.Label($"RMS residual: {_fit.RmsError * 1000f:F1} mm      " +
                                $"worst: {_fit.MaxError * 1000f:F1} mm", _h);
                GUI.color = prev;
                GUILayout.Label(_fit.RmsError <= goodRms
                        ? "GOOD — well inside the 30 mm contact band."
                        : _fit.RmsError <= badRms
                            ? "USABLE — but tighten it if you can (contact band is 30 mm)."
                            : "FAILED — do not collect data with this. Check unit scale, marker dropouts, and " +
                              "that the cluster is rigid on the HMD; then clear and re-record.", _small);
            }
            else
            {
                GUILayout.Label($"Need at least {RigidTransformSolver.MinSamples} well-spread samples to solve.", _small);
            }

            GUILayout.Space(8);
            GUILayout.BeginHorizontal();
            GUI.enabled = ready;
            if (GUILayout.Button(_recording ? "■ Pause recording" : "● Record", GUILayout.Height(38), GUILayout.Width(190)))
                _recording = !_recording;
            GUI.enabled = ready && _unity.Count >= RigidTransformSolver.MinSamples;
            if (GUILayout.Button("Solve now", GUILayout.Height(38), GUILayout.Width(130))) Solve();
            GUI.enabled = _solved;
            if (GUILayout.Button("💾 Save alignment", GUILayout.Height(38), GUILayout.Width(190)))
                _message = ViconAlignmentFile.Save(_fit, outputFile)
                    ? $"Saved to {outputFile} — set ViconBodyRig.alignmentFile to this name."
                    : "SAVE FAILED — see the Console.";
            GUI.enabled = true;
            GUILayout.EndHorizontal();

            GUILayout.Space(4);
            if (GUILayout.Button("Clear samples", GUILayout.Width(150)))
            {
                _unity.Clear(); _vicon.Clear(); _solved = false; _message = ""; _recording = false;
            }

            if (!string.IsNullOrEmpty(_message))
            {
                GUILayout.Space(6);
                GUILayout.Label(_message, _p);
            }

            GUILayout.Space(6);
            GUILayout.Label($"unit scale {unitScale} (1 = metres, 0.001 = millimetres) — must match ViconBodyRig.", _small);
            GUILayout.EndArea();
        }

        private void EnsureStyles()
        {
            if (_h != null) return;
            _h = new GUIStyle(GUI.skin.label) { fontSize = 19, fontStyle = FontStyle.Bold, wordWrap = true };
            _p = new GUIStyle(GUI.skin.label) { fontSize = 14, wordWrap = true };
            _small = new GUIStyle(GUI.skin.label) { fontSize = 11, wordWrap = true, normal = { textColor = Color.gray } };
        }
    }
}
