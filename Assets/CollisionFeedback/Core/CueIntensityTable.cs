using System;
using System.Globalization;
using System.Text;

namespace CollisionFeedback.Core
{
    /// <summary>
    /// Per-<see cref="HapticSite"/> intensity gains (0..1) for the haptic cue [Plan Task 3.1 / E1].
    ///
    /// WHY THIS EXISTS: the cue waveform is identical across conditions — only WHEN and WHERE differ
    /// [StudyDesign]. But the chest TactSuit X40 fires 40 motors while a hand/foot Tactosy fires 3, so at a
    /// flat drive level the chest cue is far more *salient*. That confounds the Localization manipulation with
    /// raw stimulus energy. This table holds a per-site gain so a one-time perceptual-matching pass [E2] can
    /// equalize PERCEIVED intensity across sites; the gains are persisted (Runtime/CueIntensityFile) and applied
    /// in <see cref="BHapticsTactorMap.For(HapticSite,float)"/> at fire time.
    ///
    /// Pure + hardware-free (Core): parse / format / lookup are unit-tested. Default is a uniform 1.0
    /// (identical to the pre-E1 flat behavior), so an unset table changes nothing.
    /// </summary>
    public sealed class CueIntensityTable
    {
        /// <summary>Number of <see cref="HapticSite"/> values (Chest, L/R hand, L/R shin).</summary>
        public static readonly int SiteCount = Enum.GetValues(typeof(HapticSite)).Length;

        private readonly float[] _gain;

        /// <summary>All sites at gain 1.0 — identical to the pre-E1 flat behavior.</summary>
        public CueIntensityTable()
        {
            _gain = new float[SiteCount];
            for (int i = 0; i < SiteCount; i++) _gain[i] = 1f;
        }

        /// <summary>Every site at the same gain (drop-in for the old single-float intensity).</summary>
        public static CueIntensityTable Uniform(float intensity)
        {
            var t = new CueIntensityTable();
            float v = Clamp01(intensity);
            for (int i = 0; i < SiteCount; i++) t._gain[i] = v;
            return t;
        }

        /// <summary>Gain for a site (0..1). Always defined; defaults to 1.0.</summary>
        public float For(HapticSite site) => _gain[(int)site];

        /// <summary>Set a site's gain (clamped to 0..1). Returns this for chaining.</summary>
        public CueIntensityTable Set(HapticSite site, float intensity)
        {
            _gain[(int)site] = Clamp01(intensity);
            return this;
        }

        /// <summary>
        /// Parse "&lt;site&gt;,&lt;gain&gt;" lines. Blank lines and lines starting with '#' are ignored; an
        /// optional "site,intensity" header is skipped (any row whose site field is not a known site name).
        /// Unknown / out-of-range site names and unparseable numbers are skipped. Sites not listed keep the
        /// default 1.0. Tolerant by design — never throws on a malformed row.
        /// </summary>
        public static CueIntensityTable Parse(string text)
        {
            var table = new CueIntensityTable();
            if (string.IsNullOrEmpty(text)) return table;

            foreach (var raw in text.Split('\n'))
            {
                var line = raw.Trim();
                if (line.Length == 0 || line[0] == '#') continue;

                int comma = line.IndexOf(',');
                if (comma <= 0) continue;

                string name = line.Substring(0, comma).Trim();
                string val = line.Substring(comma + 1).Trim();

                // tolerate extra columns / trailing fields: keep only up to the next comma
                int extra = val.IndexOf(',');
                if (extra >= 0) val = val.Substring(0, extra).Trim();

                if (!Enum.TryParse(name, ignoreCase: true, out HapticSite site)) continue;     // header or junk
                if (!Enum.IsDefined(typeof(HapticSite), site)) continue;                        // numeric out-of-range guard
                if (!float.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out float gain)) continue;

                table.Set(site, gain);
            }
            return table;
        }

        /// <summary>Serialize to a "site,intensity" CSV (with header) — what the E2 calibration writes back.</summary>
        public string ToCsv()
        {
            var sb = new StringBuilder();
            sb.Append("site,intensity\n");
            foreach (HapticSite site in Enum.GetValues(typeof(HapticSite)))
                sb.Append(site).Append(',')
                  .Append(_gain[(int)site].ToString("0.###", CultureInfo.InvariantCulture)).Append('\n');
            return sb.ToString();
        }

        private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    }
}
