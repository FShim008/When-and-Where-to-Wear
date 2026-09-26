using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using CollisionFeedback.Core;
using Joint = CollisionFeedback.Core.Joint; // disambiguate from UnityEngine.Joint (physics component)

namespace CollisionFeedback.Runtime
{
    /// <summary>
    /// Offline tracking-noise robustness analysis [paper: oracle-idealization rebuttal]. Replays a recorded
    /// <c>keypoints_*.csv</c> through the real warning logic at increasing levels of injected tracking error and
    /// reports how the warning signal degrades, writing <c>robustness_analysis.csv</c>.
    ///
    /// WHAT IT ANSWERS: the predictive conditions run on a near-perfect tracking oracle, which is deliberate
    /// (an upper bound on what predictive timing can add) but invites *"of course it works with perfect
    /// tracking."* Because sessions log raw keypoints at frame rate, that can be answered with data: the same
    /// recorded motion, the same warning logic, progressively worse tracking.
    ///
    /// WHAT IT DOES NOT ANSWER: replay cannot simulate how a participant would have MOVED differently under
    /// degraded warnings, so this measures <b>warning fidelity</b> (detection rate, lead time, false alarms) —
    /// never collision reduction. See <see cref="WarningFidelity"/> for the matching rule and the phrasing to
    /// use when reporting it.
    ///
    /// USAGE: drop on a GameObject in a scene that has a <see cref="SceneObstacles"/> (so hazard geometry
    /// matches the recorded block's layout), set the CSV path and the layout the block used, press Play, click
    /// <b>Run analysis</b>. No headset, trackers, or haptics required.
    /// </summary>
    public sealed class RobustnessAnalysisRunner : MonoBehaviour
    {
        [Header("Input")]
        [Tooltip("keypoints_*.csv from a recorded block. Relative names resolve under persistentDataPath.")]
        [SerializeField] private string keypointCsv = "sessions/P901/keypoints_B0_PB.csv";
        [Tooltip("The layout variant that block actually ran (L1–L6, or LP for practice) — the hazard geometry " +
                 "must match the recording or the ground-truth approaches will be wrong.")]
        [SerializeField] private string layoutId = "L1";

        [Header("Noise sweep")]
        [Tooltip("Per-axis jitter σ in metres. 0 reproduces the study's own oracle; 0.03 equals the contact band.")]
        [SerializeField] private float[] sigmas = { 0f, 0.005f, 0.01f, 0.02f, 0.03f, 0.05f, 0.10f };
        [Tooltip("Fixed per-joint offset magnitude (m), modelling registration / mount error that never averages out.")]
        [SerializeField] private float biasMagnitude = 0f;
        [Tooltip("Noise seeds averaged per level, so one unlucky draw cannot drive the curve.")]
        [SerializeField] private int repeatsPerLevel = 5;
        [Tooltip("How long before an approach an alert still counts as having warned about it (s).")]
        [SerializeField] private double maxLeadSeconds = 3.0;

        [Header("Output")]
        [SerializeField] private string outputCsv = "robustness_analysis.csv";

        private string _status = "Idle — set the CSV path and click Run.";
        private bool _busy;
        private GUIStyle _h, _p;

        private static string Resolve(string path) =>
            string.IsNullOrWhiteSpace(path) ? null :
            (Path.IsPathRooted(path) ? path : Path.Combine(Application.persistentDataPath, path));

        /// <summary>Parse a keypoint log (<c>participant,block,time_s,head_x…rfoot_z</c>) into frames.</summary>
        public static List<PoseFrame> LoadKeypointCsv(string path, out string error)
        {
            error = null;
            var frames = new List<PoseFrame>();
            var inv = CultureInfo.InvariantCulture;
            int expected = 3 + 3 * JointInfo.Count;   // participant, block, time + xyz per joint

            try
            {
                foreach (string raw in File.ReadLines(path))
                {
                    string line = raw.Trim();
                    if (line.Length == 0) continue;
                    string[] f = line.Split(',');
                    if (f.Length < expected) continue;
                    if (!double.TryParse(f[2], NumberStyles.Float, inv, out double t)) continue; // header row

                    var joints = new Vector3[JointInfo.Count];
                    bool ok = true;
                    for (int j = 0; j < JointInfo.Count && ok; j++)
                    {
                        int b = 3 + j * 3;
                        // Parse each axis into its own flag. Folding these into one && chain short-circuits, so
                        // y and z would be unassigned when x fails and the compiler rejects the Vector3.
                        bool okX = float.TryParse(f[b], NumberStyles.Float, inv, out float x);
                        bool okY = float.TryParse(f[b + 1], NumberStyles.Float, inv, out float y);
                        bool okZ = float.TryParse(f[b + 2], NumberStyles.Float, inv, out float z);
                        ok = okX && okY && okZ;
                        if (ok) joints[j] = new Vector3(x, y, z);
                    }
                    if (ok) frames.Add(new PoseFrame { Timestamp = t, Joints = joints });
                }
            }
            catch (System.Exception e) { error = e.Message; }
            return frames;
        }

        /// <summary>Run the sweep across all six conditions and write the results CSV.</summary>
        public void RunAnalysis()
        {
            _busy = true;
            try
            {
                string inPath = Resolve(keypointCsv);
                if (inPath == null || !File.Exists(inPath))
                {
                    _status = $"NOT FOUND: {inPath}";
                    return;
                }

                List<PoseFrame> clean = LoadKeypointCsv(inPath, out string err);
                if (err != null) { _status = $"Read failed: {err}"; return; }
                if (clean.Count < 2) { _status = $"Only {clean.Count} frame(s) parsed — wrong file or format?"; return; }

                var sceneObstacles = FindFirstObjectByType<SceneObstacles>();
                if (sceneObstacles == null) { _status = "No SceneObstacles in the scene — cannot build hazard geometry."; return; }
                sceneObstacles.ApplyVariant(layoutId);
                List<Obstacle> obstacles = sceneObstacles.Collect();
                if (obstacles.Count == 0) { _status = "SceneObstacles returned no volumes."; return; }

                var limbs = new List<Joint> { Joint.Chest, Joint.LeftHand, Joint.RightHand, Joint.LeftFoot, Joint.RightFoot };
                var oracleParams = new OracleParams();
                var detectorParams = new DetectorParams();

                var sb = new StringBuilder();
                sb.Append(FidelityResult.CsvHeader()).Append('\n');
                int rows = 0;

                foreach (Condition c in System.Enum.GetValues(typeof(Condition)))
                {
                    if (c == Condition.None) continue;               // None emits no warnings by definition
                    List<FidelityResult> sweep = WarningFidelity.Sweep(
                        c, clean, obstacles, limbs, oracleParams, detectorParams,
                        sigmas, repeatsPerLevel, 20260811, biasMagnitude, maxLeadSeconds);
                    foreach (FidelityResult r in sweep) { sb.Append(r.ToCsvRow()).Append('\n'); rows++; }
                }

                string outPath = Resolve(outputCsv);
                File.WriteAllText(outPath, sb.ToString());
                _status = $"Done — {clean.Count} frames, {obstacles.Count} volumes, {rows} rows → {outPath}";
                Debug.Log($"[Robustness] {_status}");
            }
            catch (System.Exception e)
            {
                _status = $"FAILED: {e.Message}";
                Debug.LogException(e);
            }
            finally { _busy = false; }
        }

        private void OnGUI()
        {
            if (_h == null)
            {
                _h = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, wordWrap = true };
                _p = new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = true };
            }

            GUILayout.BeginArea(new Rect(24, 24, Mathf.Min(760f, Screen.width - 48f), 240f), GUI.skin.box);
            GUILayout.Label("Tracking-noise robustness analysis", _h);
            GUILayout.Label($"input: {keypointCsv}\nlayout: {layoutId}   levels: {sigmas.Length}   " +
                            $"repeats: {repeatsPerLevel}", _p);
            GUILayout.Space(6);
            GUI.enabled = !_busy;
            if (GUILayout.Button("▶ Run analysis", GUILayout.Height(38), GUILayout.Width(220))) RunAnalysis();
            GUI.enabled = true;
            GUILayout.Space(6);
            GUILayout.Label(_status, _p);
            GUILayout.EndArea();
        }
    }
}
