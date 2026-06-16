using System.Collections.Generic;
using UnityEngine;
using CollisionFeedback.Core;
using Joint = CollisionFeedback.Core.Joint; // disambiguate from UnityEngine.Joint (physics component)

namespace CollisionFeedback.Runtime
{
    /// <summary>
    /// Camera-rig -> VR-frame calibration capture [3C.2]. Run on the STUDY PC in a small calibration scene
    /// while the capture PC streams keypoints (same UDP 9000). At several marked points spread across the
    /// volume AND at varied heights, hold a tracked VR controller against the participant's
    /// <see cref="sampleJoint"/> (default right hand/wrist) and click "Capture point". After >= 4 points,
    /// click "Solve + Save": it fits the optimal rigid transform (<see cref="RigidTransformSolver"/>) and
    /// writes it via <see cref="CameraVrCalibrationFile"/>, which LiveSessionController loads on Start.
    ///
    /// IMPORTANT: do NOT run this at the same time as LiveSessionController — both bind UDP 9000.
    /// The "source" point is the streamed joint (camera frame); the "target" is <see cref="vrProbe"/>'s
    /// world position (VR frame). Keep the controller as close to the sampled joint as possible — any
    /// constant joint↔controller offset that varies in direction across points degrades the fit (watch RMS).
    /// </summary>
    public sealed class CameraVrCalibration : MonoBehaviour
    {
        [Header("Tracking (UDP) — the port the bridge streams to")]
        [SerializeField] private int udpPort = 9000;

        [Header("Probe")]
        [Tooltip("Drag your tracked VR controller's Transform here; its world position is the VR-frame target point.")]
        [SerializeField] private Transform vrProbe;
        [Tooltip("Which streamed joint is held against the controller at each marked point.")]
        [SerializeField] private Joint sampleJoint = Joint.RightHand;

        [Header("Output (blank = persistentDataPath/cam_to_vr_calib.txt)")]
        [SerializeField] private string outputPath = "";

        private UdpKeypointSource _source;
        private PoseFrame _latest;
        private bool _hasFrame;
        private readonly List<Vector3> _camPts = new();
        private readonly List<Vector3> _vrPts = new();
        private string _status = "Waiting for stream…";
        private float _lastRmsMeters = -1f;

        private void OnEnable()
        {
            try { _source = new UdpKeypointSource(udpPort); }
            catch (System.Exception e) { Debug.LogError($"[CameraVrCalibration] UDP {udpPort} failed: {e.Message}"); }
            Debug.Log($"[CameraVrCalibration] Listening UDP {udpPort}. Hold the controller against the {sampleJoint} at " +
                      ">=4 well-spread points, then Solve + Save.");
        }

        private void OnDisable()
        {
            _source?.Dispose();
            _source = null;
        }

        private void Update()
        {
            if (_source == null) return;
            while (_source.TryGetFrame(out PoseFrame f)) { _latest = f; _hasFrame = true; }
        }

        private void Capture()
        {
            if (!_hasFrame) { _status = "No tracking frame yet — is the capture PC streaming?"; return; }
            if (vrProbe == null) { _status = "Assign the VR controller Transform to 'vrProbe'."; return; }
            _camPts.Add(_latest.Get(sampleJoint)); // camera-frame source point
            _vrPts.Add(vrProbe.position);          // VR-frame target point
            _status = $"Captured point {_camPts.Count}.";
        }

        private void SolveAndSave()
        {
            if (_camPts.Count < 4) { _status = $"Need >= 4 non-collinear points (have {_camPts.Count})."; return; }

            RigidTransform t = RigidTransformSolver.Solve(_camPts, _vrPts);

            double sse = 0;
            for (int i = 0; i < _camPts.Count; i++)
                sse += (t.Apply(_camPts[i]) - _vrPts[i]).sqrMagnitude;
            _lastRmsMeters = Mathf.Sqrt((float)(sse / _camPts.Count));

            string path = string.IsNullOrWhiteSpace(outputPath) ? CameraVrCalibrationFile.DefaultPath : outputPath;
            CameraVrCalibrationFile.Save(t, path);

            _status = $"Saved ({_camPts.Count} pts, RMS {_lastRmsMeters * 1000f:F1} mm) -> {path}";
            Debug.Log($"[CameraVrCalibration] {_status}");
            if (_lastRmsMeters > 0.05f)
                Debug.LogWarning($"[CameraVrCalibration] RMS {_lastRmsMeters * 1000f:F0} mm is high — recapture with the " +
                                 $"controller held tighter to the {sampleJoint} and points spread more in 3D (incl. height).");
        }

        private void Clear()
        {
            _camPts.Clear(); _vrPts.Clear(); _lastRmsMeters = -1f; _status = "Cleared.";
        }

        // IMGUI so there is no dependency on which input backend the project uses.
        private void OnGUI()
        {
            var style = new GUIStyle(GUI.skin.label) { fontSize = 14, alignment = TextAnchor.UpperLeft, richText = true };
            string rms = _lastRmsMeters >= 0 ? $"{_lastRmsMeters * 1000f:F1} mm" : "—";
            GUI.Label(new Rect(12, 12, 940, 90),
                $"<b>Camera→VR Calibration</b>   stream: {(_hasFrame ? "OK" : "waiting")}   probe joint: {sampleJoint}   " +
                $"points: {_camPts.Count}   last RMS: {rms}\n{_status}", style);

            if (GUI.Button(new Rect(12, 96, 150, 30), "Capture point")) Capture();
            if (GUI.Button(new Rect(170, 96, 150, 30), "Solve + Save")) SolveAndSave();
            if (GUI.Button(new Rect(328, 96, 100, 30), "Clear")) Clear();
        }
    }
}
