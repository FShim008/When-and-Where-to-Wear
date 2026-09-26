using System.Globalization;
using System.Text;
using UnityEngine;

namespace CollisionFeedback.Core
{
    /// <summary>
    /// `trajectories_&lt;block&gt;.csv` — raw reference tracking, one row per delivered frame
    /// [PAPER2_STUDY_DESIGN §14; gate 10 "end-to-end logs reconstruct every trial"].
    ///
    /// §14 asks for "source/receive timestamps, sequence, positions, and tracking quality". Three of those
    /// four are here. The fourth is **not available and is deliberately not faked**:
    ///
    ///   source_time_s  the tracking source's own monotonic clock (`PoseFrame.Timestamp`)
    ///   recv_utc       wall clock at the moment the logger received the frame
    ///   seq            monotonic counter assigned by the logger
    ///   gap_s          source-clock interval since the previous frame
    ///   positions      six joints
    ///
    /// **There is no tracking-quality column.** `PoseFrame` carries no confidence or validity field, and
    /// `TrackerKeypointSource` does not surface one from SteamVR. Writing a constant "good" would be worse
    /// than omitting it: a reader would take it for a measurement. `gap_s` is the honest substitute — it is
    /// a property of *delivery*, not of tracker confidence, and a spike in it is the observable symptom of
    /// the dropout that `events.csv` records separately.
    ///
    /// WHY SEQUENCE AND RECEIVE TIME LIVE IN THE LOGGER, NOT IN `PoseFrame`.
    /// They describe the logging pipeline rather than the pose. A frame does not have a receive time until
    /// something receives it, and two consumers of the same frame would legitimately assign different ones.
    /// Putting them on the struct would have meant one consumer's bookkeeping travelling inside everyone
    /// else's data.
    ///
    /// The pairing of `seq` with `gap_s` is what makes a dropout provable after the fact: a contiguous
    /// sequence with a large gap means frames were never produced, while a jump in `seq` would mean the
    /// logger itself dropped them. Those are different faults and the analysis should not confuse them.
    /// </summary>
    public static class TrajectoryFormatter
    {
        private static readonly string[] JointNames = { "head", "chest", "lhand", "rhand", "lfoot", "rfoot" };

        public static string Header()
        {
            var sb = new StringBuilder("participant,session,block,trial_id,seq,source_time_s,recv_utc,gap_s");
            foreach (string j in JointNames)
                sb.Append(',').Append(j).Append("_x,").Append(j).Append("_y,").Append(j).Append("_z");
            return sb.ToString();
        }

        public static string Row(int participant, int session, int block, string trialId,
                                 long seq, in PoseFrame frame, string recvUtc, double gapSeconds)
        {
            var inv = CultureInfo.InvariantCulture;
            var sb = new StringBuilder(256);
            sb.Append(participant.ToString(inv)).Append(',')
              .Append(session.ToString(inv)).Append(',')
              .Append(block.ToString(inv)).Append(',')
              .Append(Csv(trialId)).Append(',')
              .Append(seq.ToString(inv)).Append(',')
              .Append(frame.Timestamp.ToString("F4", inv)).Append(',')
              .Append(Csv(recvUtc)).Append(',')
              .Append(double.IsNaN(gapSeconds) ? "NA" : gapSeconds.ToString("F4", inv));

            for (int j = 0; j < JointInfo.Count; j++)
            {
                Vector3 p = frame.Joints[j];
                sb.Append(',').Append(p.x.ToString("F4", inv))
                  .Append(',').Append(p.y.ToString("F4", inv))
                  .Append(',').Append(p.z.ToString("F4", inv));
            }
            return sb.ToString();
        }

        private static string Csv(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return (s.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0)
                ? "\"" + s.Replace("\"", "\"\"") + "\"" : s;
        }
    }
}
