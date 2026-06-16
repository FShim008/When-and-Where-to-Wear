using UnityEngine;

namespace CollisionFeedback.Core
{
    /// <summary>
    /// A foam obstacle modeled as an axis-aligned box volume in the VR world frame.
    /// (Box is enough for the surveyed foam props; swap for a richer volume later if needed.)
    /// </summary>
    public readonly struct Obstacle
    {
        public readonly string Id;
        public readonly Vector3 Center;
        public readonly Vector3 HalfExtents;

        public Obstacle(string id, Vector3 center, Vector3 halfExtents)
        {
            Id = id;
            Center = center;
            HalfExtents = halfExtents;
        }

        /// <summary>Closest point on the box to <paramref name="p"/> (equals p if p is inside).</summary>
        public readonly Vector3 ClosestPoint(Vector3 p)
        {
            return new Vector3(
                Mathf.Clamp(p.x, Center.x - HalfExtents.x, Center.x + HalfExtents.x),
                Mathf.Clamp(p.y, Center.y - HalfExtents.y, Center.y + HalfExtents.y),
                Mathf.Clamp(p.z, Center.z - HalfExtents.z, Center.z + HalfExtents.z));
        }

        /// <summary>Euclidean distance from <paramref name="p"/> to the box surface (0 if inside).</summary>
        public readonly float DistanceTo(Vector3 p) => Vector3.Distance(p, ClosestPoint(p));
    }
}
