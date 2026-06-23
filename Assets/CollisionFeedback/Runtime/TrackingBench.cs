using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using CollisionFeedback.Core;
using Joint = CollisionFeedback.Core.Joint; // disambiguate from UnityEngine.Joint (physics component)

namespace CollisionFeedback.Runtime
{
    /// <summary>
    /// M4 bench check (Pilot_Design Stage 0 / Risk R1): "do the VIVE Ultimate Trackers still track every limb
    /// with the HMD + bHaptics suit on, under fast motion?" Drop this on ONE GameObject in a scene with a
    /// BodyTrackerRig, press Play, and:
    ///   - SEE the 6 tracked joints render as spheres that should follow your body 1:1 (green = healthy,
    ///     orange = FROZEN/occluded, red = NaN/invalid, grey = stream stalled). Do fast dodges/reaches and
    ///     go to the corners / crouch (the occlusion-prone cases) and watch for joints freezing or jumping.
    ///   - READ the HUD (on the desktop mirror) for the quantitative bar: delivery rate (Hz), worst delivery
    ///     gap, per-joint jitter / freeze / jumps, and an overall PASS / FAIL vs the thresholds.
    ///   - Hold a still T-pose, click "Reset window", then read each limb's JITTER (the noise floor) —
    ///     jitter is only meaningful while you hold still.
    ///   - Click "Log CSV" (or Stop) to write a summary to persistentDataPath for the record.
    /// Hardware-free to compile / enter Play; it simply shows 0 Hz until the trackers report.
    /// The metrics themselves live in <see cref="TrackingQualityMonitor"/> (Core, unit-tested); this is glue.
    /// </summary>
    public sealed class TrackingBench : MonoBehaviour
    {
        [Header("Source (VIVE Ultimate Trackers — auto-found if empty)")]
        [SerializeField] private BodyTrackerRig trackerRig;

        [Header("Pass/fail thresholds (the M4 bar)")]
        [SerializeField] private float minDeliveryRateHz = 30f;
        [SerializeField] private float maxFrameGapSeconds = 0.10f;
        [SerializeField] private float maxJitterMeters = 0.01f;
        [SerializeField] private float maxFreezeSeconds = 0.25f;
        [SerializeField] private float jumpMeters = 0.50f;

        [Header("Visualization")]
        [SerializeField] private float jointSphereRadius = 0.05f;

        private static readonly List<Joint> Joints = new()
        {
            Joint.Head, Joint.Chest, Joint.LeftHand, Joint.RightHand, Joint.LeftFoot, Joint.RightFoot,
        };
        // Rough skeleton bones (no hip is tracked, so the feet hang off the chest).
        private static readonly (Joint a, Joint b)[] Bones =
        {
            (Joint.Head, Joint.Chest),
            (Joint.Chest, Joint.LeftHand), (Joint.Chest, Joint.RightHand),
            (Joint.Chest, Joint.LeftFoot), (Joint.Chest, Joint.RightFoot),
        };
        private static readonly Color Orange = new Color(1f, 0.5f, 0f);

        private IKeypointSource _source;
        private TrackingQualityMonitor _monitor;
        private readonly Dictionary<Joint, Renderer> _spheres = new();
        private readonly List<LineRenderer> _bones = new();
        private PoseFrame _latest;
        private bool _hasFrame;
        private double _lastFrameWall;
        private TrackingQualityReport _report;
        private double _nextReportWall;
        private string _hud = "M4 Tracking Bench — starting…";

        private static double Now => Time.unscaledTimeAsDouble;

        private void Start()
        {
            _monitor = new TrackingQualityMonitor(Joints, new TrackingQualityThresholds
            {
                MinDeliveryRateHz = minDeliveryRateHz,
                MaxFrameGapSeconds = maxFrameGapSeconds,
                MaxJitterMeters = maxJitterMeters,
                MaxFreezeSeconds = maxFreezeSeconds,
                JumpMeters = jumpMeters,
            });
            _monitor.Reset(Now);

            if (trackerRig == null) trackerRig = FindFirstObjectByType<BodyTrackerRig>();
            if (trackerRig != null && trackerRig.IsComplete) _source = trackerRig.CreateSource();
            else Debug.LogError("[TrackingBench] No complete BodyTrackerRig — assign the HMD + 5 Ultimate Tracker Transforms.");

            BuildVisuals();
            Debug.Log("[TrackingBench] Reading VIVE Ultimate Trackers. Move around and read the HUD on the desktop mirror.");
        }

        private void Update()
        {
            // Drain everything that arrived since the last frame; feed the monitor; keep the newest to render.
            if (_source != null)
            {
                while (_source.TryGetFrame(out PoseFrame f))
                {
                    _monitor.Observe(f, Now);
                    _latest = f;
                    _hasFrame = true;
                    _lastFrameWall = Now;
                }
            }

            // Refresh the report ~5x/sec (it allocates); reuse it for the HUD text and the sphere colours.
            if (Now >= _nextReportWall)
            {
                _report = _monitor.Report(Now);
                _nextReportWall = Now + 0.2;
                RebuildHud();
            }

            UpdateVisuals();
        }

        // ---- visuals -----------------------------------------------------------------------------------

        private void BuildVisuals()
        {
            foreach (Joint j in Joints)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = $"kp_{j}";
                go.transform.SetParent(transform, worldPositionStays: false);
                go.transform.localScale = Vector3.one * (jointSphereRadius * 2f);
                if (go.TryGetComponent<Collider>(out var col)) Destroy(col); // never interfere with physics
                var rend = go.GetComponent<Renderer>();
                rend.material = Unlit(Color.green); // unlit so colours read even in an unlit bench scene
                _spheres[j] = rend;
                go.SetActive(false);
            }

            foreach (var _ in Bones)
            {
                var go = new GameObject("bone");
                go.transform.SetParent(transform, worldPositionStays: false);
                var lr = go.AddComponent<LineRenderer>();
                lr.material = Unlit(Color.gray);
                lr.widthMultiplier = 0.012f;
                lr.positionCount = 2;
                lr.useWorldSpace = true;
                lr.enabled = false;
                _bones.Add(lr);
            }
        }

        private void UpdateVisuals()
        {
            bool stalled = !_hasFrame || (Now - _lastFrameWall) > maxFrameGapSeconds;

            for (int i = 0; i < Joints.Count; i++)
            {
                Joint j = Joints[i];
                Renderer rend = _spheres[j];
                if (!_hasFrame) { rend.gameObject.SetActive(false); continue; }
                rend.gameObject.SetActive(true);
                rend.transform.position = _latest.Get(j);                 // keypoints are already world-frame
                rend.material.color = stalled ? Color.gray : HealthColor(j);
            }

            for (int b = 0; b < Bones.Length; b++)
            {
                LineRenderer lr = _bones[b];
                if (!_hasFrame) { lr.enabled = false; continue; }
                lr.enabled = true;
                lr.SetPosition(0, _latest.Get(Bones[b].a));
                lr.SetPosition(1, _latest.Get(Bones[b].b));
            }
        }

        private Color HealthColor(Joint j)
        {
            if (_report.Joints == null) return Color.green;
            foreach (var s in _report.Joints)
                if (s.Joint == j)
                {
                    if (s.InvalidSamples > 0) return Color.red;
                    if (s.MaxFreezeSeconds > maxFreezeSeconds) return Orange;
                    return Color.green;
                }
            return Color.green;
        }

        private static Material Unlit(Color c)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Unlit");
            if (sh == null) sh = Shader.Find("Unlit/Color");
            if (sh == null) sh = Shader.Find("Sprites/Default");
            return new Material(sh) { color = c };
        }

        // ---- HUD ---------------------------------------------------------------------------------------

        private void RebuildHud()
        {
            var sb = new StringBuilder();
            if (_report.Joints == null || _report.Frames == 0)
            {
                sb.Append("<b>M4 Tracking Bench</b>  —  waiting for tracker frames…  (0 frames). " +
                          "Is the BodyTrackerRig assigned and SteamVR tracking?");
                _hud = sb.ToString();
                return;
            }

            sb.AppendLine($"<b>M4 Tracking Bench</b>   <b>{(_report.Pass ? "PASS" : "FAIL")}</b>");
            sb.AppendLine($"delivery {_report.DeliveryRateHz:F1} Hz (bar {minDeliveryRateHz:F0})    " +
                          $"max gap {_report.MaxFrameGapSeconds * 1000f:F0} ms (bar {maxFrameGapSeconds * 1000f:F0})    " +
                          $"lag {_report.MeanLagSeconds * 1000f:F0} ms*    frames {_report.Frames}");
            foreach (var s in _report.Joints)
                sb.AppendLine($"  {s.Joint,-10} {(s.Pass ? "ok  " : "BAD ")}  " +
                              $"jitter {s.JitterMeters * 1000f,5:F1} mm   " +
                              $"freeze {s.MaxFreezeSeconds * 1000f,5:F0} ms   " +
                              $"jumps {s.Jumps}   invalid {s.InvalidSamples}");
            sb.AppendLine("hold STILL then Reset to read jitter;  stress with fast dodges for rate/freeze.  " +
                          "*lag absolute only if clocks are synced.");
            _hud = sb.ToString();
        }

        private void OnGUI()
        {
            var style = new GUIStyle(GUI.skin.label) { richText = true, fontSize = 14, alignment = TextAnchor.UpperLeft };
            GUI.color = (_report.Frames > 0 && !_report.Pass) ? Color.yellow : Color.white;
            GUI.Label(new Rect(12, 12, 960, 360), _hud, style);

            GUI.color = Color.white;
            if (GUI.Button(new Rect(12, 372, 150, 28), "Reset window")) _monitor.Reset(Now);
            if (GUI.Button(new Rect(170, 372, 150, 28), "Log CSV")) WriteSummary();
        }

        // ---- output ------------------------------------------------------------------------------------

        private void WriteSummary()
        {
            if (_monitor == null) return;
            TrackingQualityReport r = _monitor.Report(Now);
            string path = Path.Combine(Application.persistentDataPath, "m4_tracking_bench.csv");

            var sb = new StringBuilder();
            sb.AppendLine("metric,value");
            sb.AppendLine($"window_s,{r.WindowSeconds:F2}");
            sb.AppendLine($"frames,{r.Frames}");
            sb.AppendLine($"delivery_hz,{r.DeliveryRateHz:F2}");
            sb.AppendLine($"max_frame_gap_ms,{r.MaxFrameGapSeconds * 1000f:F1}");
            sb.AppendLine($"mean_lag_ms,{r.MeanLagSeconds * 1000f:F1}");
            sb.AppendLine($"overall_pass,{r.Pass}");
            sb.AppendLine("joint,samples,jitter_mm,max_freeze_ms,jumps,invalid,pass");
            foreach (var s in r.Joints)
                sb.AppendLine($"{s.Joint},{s.Samples},{s.JitterMeters * 1000f:F2},{s.MaxFreezeSeconds * 1000f:F1}," +
                              $"{s.Jumps},{s.InvalidSamples},{s.Pass}");

            File.WriteAllText(path, sb.ToString());
            Debug.Log($"[TrackingBench] wrote {path}  (overall {(r.Pass ? "PASS" : "FAIL")})");
        }

        private void OnDisable()
        {
            if (_monitor != null) WriteSummary();
            (_source as System.IDisposable)?.Dispose();
            _source = null;
        }
    }
}
