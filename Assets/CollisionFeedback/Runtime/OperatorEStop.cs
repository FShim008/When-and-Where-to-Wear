using System;
using System.IO;
using UnityEngine;
using UnityEngine.Events;

namespace CollisionFeedback.Runtime
{
    /// <summary>
    /// Operator emergency stop [Plan Task 4.5 / D5]. An always-visible red button plus a hardware-agnostic
    /// hotkey (read from the IMGUI event stream, so it works under the new Input System with no package
    /// dependency). On trigger it LATCHES a stop state, raises <see cref="OnStop"/> (the session driver aborts
    /// the block + silences haptics), invokes the serialized <see cref="onEmergencyStop"/> UnityEvent — wire
    /// HMD passthrough / "unblank", room lights, or an alarm there — and appends an abort record
    /// (wall-clock UTC + reason + context) to <c>estop_log.csv</c>. Latched until <see cref="ResetStop"/>.
    ///
    /// Drop one on a GameObject in the study scene; <c>SessionRunner</c> auto-finds and subscribes to it.
    /// </summary>
    public sealed class OperatorEStop : MonoBehaviour
    {
        [Tooltip("Hotkey that triggers the stop (in addition to the on-screen button).")]
        [SerializeField] private KeyCode hotkey = KeyCode.Escape;
        [SerializeField] private bool showButton = true;
        [Tooltip("Safety responses to fire on stop: HMD passthrough/unblank, stop haptics, lights, audible alarm.")]
        public UnityEvent onEmergencyStop;

        /// <summary>Raised once per stop with the reason. The session driver aborts + makes safe here.</summary>
        public event Action<string> OnStop;
        public bool Stopped { get; private set; }
        public string LastReason { get; private set; }

        private Func<string> _context = () => "";
        private string _logPath;
        private GUIStyle _banner, _btn;

        /// <summary>Supply a provider so the abort log captures block/condition/time at the moment of stop.</summary>
        public void SetContextProvider(Func<string> ctx) => _context = ctx ?? (() => "");

        /// <summary>Trigger the stop programmatically (e.g. from a watchdog). Idempotent while latched.</summary>
        public void Trigger(string reason)
        {
            if (Stopped) return;
            Stopped = true;
            LastReason = reason;
            string ctx = SafeContext();
            AppendLog(reason, ctx);
            Debug.LogWarning($"[E-STOP] {reason} | {ctx}");
            try { onEmergencyStop?.Invoke(); } catch (Exception e) { Debug.LogException(e); }
            try { OnStop?.Invoke(reason); } catch (Exception e) { Debug.LogException(e); }
        }

        /// <summary>Clear the latch (between participants). Does not undo anything already logged.</summary>
        public void ResetStop() { Stopped = false; LastReason = null; }

        private void OnGUI()
        {
            EnsureStyles();

            // Hotkey via the IMGUI event stream (input-backend agnostic).
            Event e = Event.current;
            if (!Stopped && e != null && e.type == EventType.KeyDown && e.keyCode == hotkey)
            {
                Trigger($"hotkey:{hotkey}");
                e.Use();
            }

            if (!Stopped)
            {
                if (showButton)
                {
                    Color prev = GUI.backgroundColor;
                    GUI.backgroundColor = new Color(0.85f, 0.10f, 0.10f);
                    if (GUI.Button(new Rect(Screen.width - 200f, 12f, 184f, 46f), $"■ E-STOP ({hotkey})", _btn))
                        Trigger("operator button");
                    GUI.backgroundColor = prev;
                }
                return;
            }

            // Latched: red veil + instruction so the operator restores vision and steps the participant out.
            Color pc = GUI.color;
            GUI.color = new Color(0.75f, 0f, 0f, 0.35f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = pc;
            GUI.Label(new Rect(0, Screen.height / 2f - 90f, Screen.width, 180f),
                "EMERGENCY STOP\nBlock ended — vision restored. Help the participant remove the headset.\n" +
                $"({LastReason})", _banner);
        }

        private void EnsureStyles()
        {
            if (_banner != null) return;
            _banner = new GUIStyle(GUI.skin.label)
            {
                fontSize = 28, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter,
                wordWrap = true, normal = { textColor = Color.white }
            };
            _btn = new GUIStyle(GUI.skin.button) { fontSize = 16, fontStyle = FontStyle.Bold };
        }

        private string SafeContext()
        {
            try { return _context() ?? ""; } catch { return ""; }
        }

        private void AppendLog(string reason, string ctx)
        {
            try
            {
                if (string.IsNullOrEmpty(_logPath))
                    _logPath = Path.Combine(Application.persistentDataPath, "estop_log.csv");
                bool header = !File.Exists(_logPath);
                using var w = new StreamWriter(_logPath, append: true);
                if (header) w.WriteLine("utc,reason,context");
                w.WriteLine($"{DateTime.UtcNow:o},{Csv(reason)},{Csv(ctx)}");
            }
            catch (Exception ex) { Debug.LogWarning($"[E-STOP] log write failed: {ex.Message}"); }
        }

        private static string Csv(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return (s.IndexOf(',') >= 0 || s.IndexOf('"') >= 0 || s.IndexOf('\n') >= 0)
                ? "\"" + s.Replace("\"", "\"\"") + "\"" : s;
        }
    }
}
