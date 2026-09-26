using System;
using System.Collections.Generic;

namespace CollisionFeedback.Core
{
    /// <summary>One Likert/rating item. <see cref="Id"/> is the stable key used in scoring + logs.</summary>
    public sealed class QuestionnaireItem
    {
        public readonly string Id;
        public readonly string Prompt;
        public readonly int Min;
        public readonly int Max;
        public readonly bool Reverse;       // negatively-worded → flipped before aggregation
        public readonly string Subscale;    // grouping label (IPQ: G/SP/INV/REAL; TLX: dim; SSQ: display only)
        public readonly string AnchorLow;
        public readonly string AnchorHigh;

        public QuestionnaireItem(string id, string prompt, int min, int max, bool reverse = false,
                                 string subscale = "", string anchorLow = "", string anchorHigh = "")
        {
            Id = id; Prompt = prompt; Min = min; Max = max; Reverse = reverse;
            Subscale = subscale; AnchorLow = anchorLow; AnchorHigh = anchorHigh;
        }

        /// <summary>Reverse-correct a raw response so higher always means "more of the construct".</summary>
        public int Corrected(int raw) => Reverse ? (Max + Min - raw) : raw;
    }

    /// <summary>
    /// A scored questionnaire instrument (IPQ presence / NASA-TLX workload / SSQ sickness) [Plan Task 5.5 / F1].
    /// Pure + hardware-free: items + a scoring function, both unit-tested. The Runtime QuestionnairePanel renders
    /// the items and calls <see cref="Score"/>; the result (measure→value) flows through
    /// <c>SessionRunner.RecordQuestionnaire</c> → <c>QuestionnaireFormatter</c> → <c>analysis.R</c>.
    ///
    /// The canonical measures the analysis reads are emitted exactly: IPQ "presence", NASA_TLX "overall",
    /// SSQ "total" (subscales are also recorded). <see cref="Instrument"/> matches {"IPQ","NASA_TLX","SSQ"}.
    ///
    /// IMPORTANT [Plan Task F2]: the prompt WORDING here approximates the published instruments so the UI is
    /// usable today; replace it with the official licensed wording before data collection. The STRUCTURE
    /// (item counts, scales, reverse-keys, subscale membership, scoring formulae) is correct and tested.
    /// </summary>
    public sealed class Questionnaire
    {
        public readonly string Instrument;   // "IPQ" / "NASA_TLX" / "SSQ"
        public readonly string Title;
        public readonly IReadOnlyList<QuestionnaireItem> Items;
        private readonly Func<IReadOnlyDictionary<string, int>, Dictionary<string, float>> _score;

        private Questionnaire(string instrument, string title, IReadOnlyList<QuestionnaireItem> items,
                              Func<IReadOnlyDictionary<string, int>, Dictionary<string, float>> score)
        {
            Instrument = instrument; Title = title; Items = items; _score = score;
        }

        /// <summary>True once every item has a response in <paramref name="responses"/>.</summary>
        public bool AllAnswered(IReadOnlyDictionary<string, int> responses)
        {
            foreach (var it in Items) if (!responses.ContainsKey(it.Id)) return false;
            return true;
        }

        /// <summary>Score raw responses (item id → chosen value) into measures (measure name → value).</summary>
        public Dictionary<string, float> Score(IReadOnlyDictionary<string, int> responses) => _score(responses);

        private static int Get(IReadOnlyDictionary<string, int> r, string id, int dflt) =>
            r != null && r.TryGetValue(id, out int v) ? v : dflt;

        // ── IPQ — igroup Presence Questionnaire (14 items, 0..6) ──────────────────────────────
        // Subscales: G (general), SP (spatial presence), INV (involvement), REAL (realism).
        // "presence" = mean of all 14 reverse-corrected items. (Replace wording per F2.)
        public static Questionnaire Ipq()
        {
            var items = new List<QuestionnaireItem>
            {
                new("G1",   "In the virtual world I had a sense of \"being there\".",                      0, 6, false, "G",   "not at all", "very much"),
                new("SP1",  "Somehow I felt that the virtual world surrounded me.",                        0, 6, false, "SP",  "fully disagree", "fully agree"),
                new("SP2",  "I felt like I was just perceiving pictures.",                                 0, 6, true,  "SP",  "fully disagree", "fully agree"),
                new("SP3",  "I did NOT feel present in the virtual space.",                                0, 6, true,  "SP",  "did not feel", "felt present"),
                new("SP4",  "I had a sense of acting in the virtual space rather than operating from outside.", 0, 6, false, "SP", "fully disagree", "fully agree"),
                new("SP5",  "I felt present in the virtual space.",                                        0, 6, false, "SP",  "fully disagree", "fully agree"),
                new("INV1", "How aware were you of the real world around you (sounds, room, people)?",     0, 6, true,  "INV", "extremely aware", "not aware"),
                new("INV2", "I was not aware of my real environment.",                                     0, 6, false, "INV", "fully disagree", "fully agree"),
                new("INV3", "I still paid attention to the real environment.",                             0, 6, true,  "INV", "fully disagree", "fully agree"),
                new("INV4", "I was completely captivated by the virtual world.",                          0, 6, false, "INV", "fully disagree", "fully agree"),
                new("REAL1","How real did the virtual world seem to you?",                                 0, 6, false, "REAL","not real", "completely real"),
                new("REAL2","How much did your experience seem consistent with the real world?",          0, 6, false, "REAL","not consistent", "fully consistent"),
                new("REAL3","How real did the virtual world seem to you (compared with an imagined world)?",0, 6, false, "REAL","imagined", "real"),
                new("REAL4","The virtual world seemed more real than the real world.",                     0, 6, false, "REAL","fully disagree", "fully agree"),
            };
            return new Questionnaire("IPQ", "Presence (IPQ)", items, r =>
            {
                var bySub = new Dictionary<string, (float sum, int n)>();
                float total = 0f; int count = 0;
                foreach (var it in items)
                {
                    int c = it.Corrected(Get(r, it.Id, it.Min));
                    total += c; count++;
                    bySub.TryGetValue(it.Subscale, out var e); // missing key → default (0,0)
                    bySub[it.Subscale] = (e.sum + c, e.n + 1);
                }
                var m = new Dictionary<string, float> { { "presence", count > 0 ? total / count : float.NaN } };
                foreach (var kv in bySub) m[SubscaleName(kv.Key)] = kv.Value.n > 0 ? kv.Value.sum / kv.Value.n : float.NaN;
                return m;
            });
        }

        private static string SubscaleName(string code) => code switch
        {
            "G" => "general", "SP" => "spatial", "INV" => "involvement", "REAL" => "realism", _ => code.ToLowerInvariant()
        };

        // ── NASA-TLX — Raw/unweighted (6 dimensions, 0..20 → ×5 = 0..100) ─────────────────────
        // "overall" = mean of the 6 dimensions on a 0..100 scale (Raw TLX; no pairwise weighting).
        public static Questionnaire NasaTlx()
        {
            var items = new List<QuestionnaireItem>
            {
                new("MENTAL",      "Mental demand — how mentally demanding was the task?",        0, 20, false, "TLX", "very low", "very high"),
                new("PHYSICAL",    "Physical demand — how physically demanding was the task?",    0, 20, false, "TLX", "very low", "very high"),
                new("TEMPORAL",    "Temporal demand — how hurried or rushed was the pace?",        0, 20, false, "TLX", "very low", "very high"),
                new("PERFORMANCE", "Performance — how successful were you (good = low workload)?",   0, 20, false, "TLX", "good", "poor"),
                new("EFFORT",      "Effort — how hard did you have to work?",                      0, 20, false, "TLX", "very low", "very high"),
                new("FRUSTRATION", "Frustration — how insecure, irritated, or stressed were you?", 0, 20, false, "TLX", "very low", "very high"),
            };
            return new Questionnaire("NASA_TLX", "Workload (NASA-TLX)", items, r =>
            {
                float sum = 0f; int n = 0;
                var m = new Dictionary<string, float>();
                foreach (var it in items)
                {
                    float v100 = it.Corrected(Get(r, it.Id, it.Min)) * 5f; // 0..20 → 0..100
                    m[it.Id.ToLowerInvariant()] = v100;
                    sum += v100; n++;
                }
                m["overall"] = n > 0 ? sum / n : float.NaN;
                return m;
            });
        }

        // ── SSQ — Simulator Sickness Questionnaire (16 symptoms, 0..3), Kennedy et al. 1993 ────
        // Subscale raw sums × weights; "total" = (N+O+D) × 3.74.
        private static readonly string[] SsqNausea = { "DISCOMFORT", "SALIVATION", "SWEATING", "NAUSEA", "CONCENTRATION", "STOMACH", "BURPING" };
        private static readonly string[] SsqOculo  = { "DISCOMFORT", "FATIGUE", "HEADACHE", "EYESTRAIN", "FOCUS", "CONCENTRATION", "BLURRED" };
        private static readonly string[] SsqDisor  = { "FOCUS", "NAUSEA", "HEAD_FULL", "BLURRED", "DIZZY_OPEN", "DIZZY_CLOSED", "VERTIGO" };

        public static Questionnaire Ssq()
        {
            var items = new List<QuestionnaireItem>
            {
                new("DISCOMFORT",    "General discomfort",        0, 3, false, "SSQ", "none", "severe"),
                new("FATIGUE",       "Fatigue",                   0, 3, false, "SSQ", "none", "severe"),
                new("HEADACHE",      "Headache",                  0, 3, false, "SSQ", "none", "severe"),
                new("EYESTRAIN",     "Eyestrain",                 0, 3, false, "SSQ", "none", "severe"),
                new("FOCUS",         "Difficulty focusing",       0, 3, false, "SSQ", "none", "severe"),
                new("SALIVATION",    "Increased salivation",      0, 3, false, "SSQ", "none", "severe"),
                new("SWEATING",      "Sweating",                  0, 3, false, "SSQ", "none", "severe"),
                new("NAUSEA",        "Nausea",                    0, 3, false, "SSQ", "none", "severe"),
                new("CONCENTRATION", "Difficulty concentrating",  0, 3, false, "SSQ", "none", "severe"),
                new("HEAD_FULL",     "Fullness of head",          0, 3, false, "SSQ", "none", "severe"),
                new("BLURRED",       "Blurred vision",            0, 3, false, "SSQ", "none", "severe"),
                new("DIZZY_OPEN",    "Dizziness (eyes open)",     0, 3, false, "SSQ", "none", "severe"),
                new("DIZZY_CLOSED",  "Dizziness (eyes closed)",   0, 3, false, "SSQ", "none", "severe"),
                new("VERTIGO",       "Vertigo",                   0, 3, false, "SSQ", "none", "severe"),
                new("STOMACH",       "Stomach awareness",         0, 3, false, "SSQ", "none", "severe"),
                new("BURPING",       "Burping",                   0, 3, false, "SSQ", "none", "severe"),
            };
            return new Questionnaire("SSQ", "Simulator Sickness (SSQ)", items, r =>
            {
                int n = SumOf(r, SsqNausea), o = SumOf(r, SsqOculo), d = SumOf(r, SsqDisor);
                return new Dictionary<string, float>
                {
                    { "nausea",        n * 9.54f },
                    { "oculomotor",    o * 7.58f },
                    { "disorientation",d * 13.92f },
                    { "total",         (n + o + d) * 3.74f },
                };
            });
        }

        private static int SumOf(IReadOnlyDictionary<string, int> r, string[] ids)
        {
            int s = 0;
            foreach (var id in ids) s += Get(r, id, 0);
            return s;
        }

        // ── CUE — warning acceptability + timing manipulation check (4 items, 0..6) ────────────
        // Standard practice in the warning-systems literature: a warning that works but is distrusted or
        // resented will not be adopted, and reviewers expect the trade-off to be measured rather than assumed.
        // TIMELY doubles as the subjective manipulation check for the Timing factor — predictive cues should be
        // rated as arriving in time more often than reactive ones, corroborating the objective lead-time data.
        // Administered only for conditions that actually deliver a warning (never for None, where the items
        // would be meaningless). ANNOYING is reverse-keyed so every measure points the same way (higher = better).
        /// <summary>
        /// Shoulder / arm discomfort, Borg CR10-style, collected BETWEEN BLOCKS
        /// [PAPER2_STUDY_DESIGN: "collect shoulder/arm discomfort ratings between blocks"; the
        /// shoulder-discomfort stopping rule].
        ///
        /// WHY PAPER 2 NEEDS ITS OWN. E2 is hundreds of repeated reaches with one arm inside a confined
        /// volume, returning to a fixed home region each time. Its foreseeable harm is musculoskeletal
        /// fatigue, not cybersickness, and no existing instrument here measures it.
        ///
        /// **SCORED ON THE MAXIMUM, NOT THE MEAN.** A stopping rule must fire on the worst site. Averaging
        /// would let a severe shoulder complaint be diluted by comfortable fingers and a session continue
        /// past the point the rule exists to catch — the exact failure the rule is written to prevent.
        /// `max` is therefore the value to compare against the threshold; the per-site values are reported
        /// beside it so a rise can be localised.
        ///
        /// Borg CR10 anchors (0 nothing at all, 10 extremely strong) are used because they are the standard
        /// for exertion and discomfort and are interpretable across participants without calibration.
        /// </summary>
        public static Questionnaire ShoulderDiscomfort()
        {
            var items = new List<QuestionnaireItem>
            {
                new("SHOULDER", "Discomfort or fatigue in your SHOULDER right now.",      0, 10, false, "SITE", "nothing at all", "extremely strong"),
                new("UPPERARM", "Discomfort or fatigue in your UPPER ARM right now.",     0, 10, false, "SITE", "nothing at all", "extremely strong"),
                new("FOREARM",  "Discomfort or fatigue in your FOREARM or WRIST now.",    0, 10, false, "SITE", "nothing at all", "extremely strong"),
                new("NECK",     "Discomfort or fatigue in your NECK or UPPER BACK now.",  0, 10, false, "SITE", "nothing at all", "extremely strong"),
            };
            return new Questionnaire("DISCOMFORT", "Shoulder and arm discomfort", items, r =>
            {
                var m = new Dictionary<string, float>();
                float max = 0f;
                foreach (var it in items)
                {
                    int v = Get(r, it.Id, it.Min);
                    m[it.Id.ToLowerInvariant()] = v;
                    if (v > max) max = v;
                }
                // The value the stopping rule reads. Never replace this with a mean.
                m["max"] = max;
                return m;
            });
        }

        /// <summary>
        /// Preregistered stopping threshold on <c>DISCOMFORT.max</c> [PAPER2 safety rules]. At or above
        /// this the operator stops the session rather than continuing into a break.
        ///
        /// The number itself is a PI decision and must be recorded before piloting; 7 on Borg CR10 ("very
        /// strong") is the placeholder, chosen so the rule fires before a participant reaches the point of
        /// asking to stop unprompted.
        /// </summary>
        public const float DiscomfortStopThreshold = 7f;

        public static Questionnaire CueAcceptability()
        {
            var items = new List<QuestionnaireItem>
            {
                new("HELPFUL",  "The warnings helped me avoid the hazards.",                 0, 6, false, "CUE", "fully disagree", "fully agree"),
                new("TIMELY",   "The warnings arrived in time for me to react.",             0, 6, false, "CUE", "always too late", "always in time"),
                new("TRUST",    "I trusted the warnings.",                                   0, 6, false, "CUE", "not at all", "completely"),
                new("ANNOYING", "The warnings were annoying or distracting.",                0, 6, true,  "CUE", "fully disagree", "fully agree"),
            };
            return new Questionnaire("CUE", "Warning acceptability", items, r =>
            {
                var m = new Dictionary<string, float>();
                float sum = 0f; int n = 0;
                foreach (var it in items)
                {
                    int c = it.Corrected(Get(r, it.Id, it.Min));   // ANNOYING flipped: higher = less annoying
                    m[it.Id.ToLowerInvariant()] = c;
                    sum += c; n++;
                }
                m["acceptability"] = n > 0 ? sum / n : float.NaN;  // the composite the analysis models
                return m;
            });
        }
    }
}
