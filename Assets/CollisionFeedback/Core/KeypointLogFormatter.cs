using System.Globalization;
using System.Text;
using UnityEngine;

namespace CollisionFeedback.Core
{
    /// <summary>
    /// Raw per-frame keypoint log [3G.2 / oracle-robustness]: lets the analysis re-derive alerts offline
    /// under injected tracking noise + latency. Pure; the Runtime writer persists it. Column order matches
    /// <see cref="Joint"/>.
    /// </summary>
    public static class KeypointLogFormatter
    {
        private static readonly string[] JointNames = { "head", "chest", "lhand", "rhand", "lfoot", "rfoot" };

        public static string Header()
        {
            var sb = new StringBuilder("participant,block,time_s");
            foreach (string j in JointNames)
                sb.Append(',').Append(j).Append("_x,").Append(j).Append("_y,").Append(j).Append("_z");
            return sb.ToString();
        }

        public static string Row(int participant, int block, in PoseFrame frame)
        {
            var inv = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();
            sb.Append(participant.ToString(inv)).Append(',')
              .Append(block.ToString(inv)).Append(',')
              .Append(frame.Timestamp.ToString("F4", inv));
            for (int j = 0; j < JointInfo.Count; j++)
            {
                Vector3 p = frame.Joints[j];
                sb.Append(',').Append(p.x.ToString("F4", inv))
                  .Append(',').Append(p.y.ToString("F4", inv))
                  .Append(',').Append(p.z.ToString("F4", inv));
            }
            return sb.ToString();
        }
    }
}
