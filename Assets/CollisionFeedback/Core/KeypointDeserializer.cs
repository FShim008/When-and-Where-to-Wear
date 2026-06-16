using System.Globalization;
using System.Text;
using UnityEngine;

namespace CollisionFeedback.Core
{
    /// <summary>
    /// Wire format for body-tracking frames: one CSV line per frame,
    /// <c>timestamp, x0,y0,z0, x1,y1,z1, ... x5,y5,z5</c> (joint order = <see cref="Joint"/>),
    /// invariant-culture decimals. Pure + testable; the Runtime UdpKeypointSource does the socket I/O.
    /// </summary>
    public static class KeypointDeserializer
    {
        public const int FieldCount = 1 + 3 * JointInfo.Count; // timestamp + xyz per joint

        public static bool TryParse(string line, out PoseFrame frame)
        {
            frame = default;
            if (string.IsNullOrEmpty(line)) return false;

            string[] parts = line.Split(',');
            if (parts.Length != FieldCount) return false;

            var inv = CultureInfo.InvariantCulture;
            if (!double.TryParse(parts[0], NumberStyles.Float, inv, out double t)) return false;

            var joints = new Vector3[JointInfo.Count];
            for (int j = 0; j < JointInfo.Count; j++)
            {
                int b = 1 + j * 3;
                if (!float.TryParse(parts[b], NumberStyles.Float, inv, out float x)) return false;
                if (!float.TryParse(parts[b + 1], NumberStyles.Float, inv, out float y)) return false;
                if (!float.TryParse(parts[b + 2], NumberStyles.Float, inv, out float z)) return false;
                joints[j] = new Vector3(x, y, z);
            }

            frame = new PoseFrame { Timestamp = t, Joints = joints };
            return true;
        }

        public static string Serialize(in PoseFrame frame)
        {
            var inv = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();
            sb.Append(frame.Timestamp.ToString("R", inv));
            for (int j = 0; j < JointInfo.Count; j++)
            {
                Vector3 p = frame.Joints[j];
                sb.Append(',').Append(p.x.ToString("R", inv))
                  .Append(',').Append(p.y.ToString("R", inv))
                  .Append(',').Append(p.z.ToString("R", inv));
            }
            return sb.ToString();
        }
    }
}
