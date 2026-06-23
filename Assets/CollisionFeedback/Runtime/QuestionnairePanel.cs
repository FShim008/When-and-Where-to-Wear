using System;
using System.Collections.Generic;
using UnityEngine;
using CollisionFeedback.Core;

namespace CollisionFeedback.Runtime
{
    /// <summary>
    /// In-session questionnaire administration UI [Plan Task 5.5 / F1]. Immediate-mode (IMGUI) so it needs no
    /// Canvas/prefab authoring and renders on the desktop operator mirror — the participant answers between
    /// blocks (HMD may be lifted). Drives any <see cref="Questionnaire"/> (IPQ / NASA-TLX / SSQ): small scales
    /// render as a button row, the 0..20 TLX scale as a slider. On submit it scores via
    /// <see cref="Questionnaire.Score"/> and hands the measures to the caller, which routes them to
    /// <c>SessionRunner.RecordQuestionnaire</c> → <c>analysis.R</c>.
    ///
    /// One questionnaire at a time: call <see cref="Administer"/>, await <see cref="IsBusy"/> going false (the
    /// callback fires on submit). For an in-headset version, swap this for a world-space uGUI Canvas later — the
    /// Core items + scoring are reused unchanged.
    /// </summary>
    public sealed class QuestionnairePanel : MonoBehaviour
    {
        private Questionnaire _q;
        private Dictionary<string, int> _resp;
        private Action<IReadOnlyDictionary<string, float>> _onComplete;
        private Vector2 _scroll;
        private bool _active;
        private GUIStyle _wrap, _anchor, _title;

        public bool IsBusy => _active;

        /// <summary>Show <paramref name="q"/> and call <paramref name="onComplete"/> with scored measures on submit.</summary>
        public void Administer(Questionnaire q, Action<IReadOnlyDictionary<string, float>> onComplete)
        {
            _q = q;
            _resp = new Dictionary<string, int>();
            _onComplete = onComplete;
            _scroll = Vector2.zero;
            _active = q != null;
            if (q == null) onComplete?.Invoke(new Dictionary<string, float>());
        }

        private void EnsureStyles()
        {
            if (_title != null) return;
            _title  = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, wordWrap = true };
            _wrap   = new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = true };
            _anchor = new GUIStyle(GUI.skin.label) { fontSize = 10, normal = { textColor = Color.gray } };
        }

        private void OnGUI()
        {
            if (!_active || _q == null) return;
            EnsureStyles();

            // Centered panel over a dimming backdrop.
            float w = Mathf.Min(940f, Screen.width - 40f);
            float h = Mathf.Min(720f, Screen.height - 40f);
            var panel = new Rect((Screen.width - w) / 2f, (Screen.height - h) / 2f, w, h);
            GUI.Box(new Rect(0, 0, Screen.width, Screen.height), GUIContent.none); // backdrop
            GUI.Box(panel, GUIContent.none);

            GUILayout.BeginArea(new Rect(panel.x + 16, panel.y + 14, panel.width - 32, panel.height - 28));
            int answered = CountAnswered();
            GUILayout.Label(_q.Title, _title);
            GUILayout.Label($"Answer every item — {answered}/{_q.Items.Count} answered.", _anchor);
            GUILayout.Space(6);

            _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.ExpandHeight(true));
            int n = 0;
            foreach (var it in _q.Items)
            {
                n++;
                GUILayout.Label($"{n}. {it.Prompt}", _wrap);
                DrawControl(it);
                GUILayout.Space(10);
            }
            GUILayout.EndScrollView();

            GUILayout.Space(8);
            GUILayout.BeginHorizontal();
            bool complete = _q.AllAnswered(_resp);
            GUI.enabled = complete;
            if (GUILayout.Button(complete ? "Submit ▶" : "Answer all items to submit", GUILayout.Height(34), GUILayout.Width(280)))
                Submit();
            GUI.enabled = true;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void DrawControl(QuestionnaireItem it)
        {
            int range = it.Max - it.Min;
            bool answered = _resp.TryGetValue(it.Id, out int cur);

            if (range <= 10)
            {
                // Button row (IPQ 0..6, SSQ 0..3).
                GUILayout.BeginHorizontal();
                for (int v = it.Min; v <= it.Max; v++)
                {
                    bool sel = answered && cur == v;
                    Color prev = GUI.backgroundColor;
                    if (sel) GUI.backgroundColor = new Color(0.3f, 0.8f, 1f);
                    if (GUILayout.Button(v.ToString(), GUILayout.Width(40), GUILayout.Height(28))) _resp[it.Id] = v;
                    GUI.backgroundColor = prev;
                }
                GUILayout.EndHorizontal();
            }
            else
            {
                // Slider + nudge buttons (NASA-TLX 0..20). Any interaction marks the item answered.
                GUILayout.BeginHorizontal();
                float working = answered ? cur : (it.Min + it.Max) / 2f;
                if (GUILayout.Button("◀", GUILayout.Width(28))) _resp[it.Id] = Mathf.Max(it.Min, Mathf.RoundToInt(working) - 1);
                float nv = GUILayout.HorizontalSlider(working, it.Min, it.Max, GUILayout.Width(520), GUILayout.Height(22));
                int ni = Mathf.RoundToInt(nv);
                if (ni != Mathf.RoundToInt(working)) _resp[it.Id] = ni;
                if (GUILayout.Button("▶", GUILayout.Width(28))) _resp[it.Id] = Mathf.Min(it.Max, Mathf.RoundToInt(working) + 1);
                GUILayout.Label(_resp.ContainsKey(it.Id) ? $"  {_resp[it.Id]}" : "  —", GUILayout.Width(40));
                GUILayout.EndHorizontal();
            }

            // Endpoint anchors.
            if (!string.IsNullOrEmpty(it.AnchorLow) || !string.IsNullOrEmpty(it.AnchorHigh))
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{it.Min} = {it.AnchorLow}", _anchor);
                GUILayout.FlexibleSpace();
                GUILayout.Label($"{it.AnchorHigh} = {it.Max}", _anchor);
                GUILayout.EndHorizontal();
            }
        }

        private int CountAnswered()
        {
            int c = 0;
            foreach (var it in _q.Items) if (_resp.ContainsKey(it.Id)) c++;
            return c;
        }

        private void Submit()
        {
            Dictionary<string, float> measures = _q.Score(_resp);
            _active = false;
            var cb = _onComplete;
            _onComplete = null;
            cb?.Invoke(measures);
        }
    }
}
