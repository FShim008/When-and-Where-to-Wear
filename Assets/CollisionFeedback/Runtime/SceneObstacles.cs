using System.Collections.Generic;
using UnityEngine;
using CollisionFeedback.Core;

namespace CollisionFeedback.Runtime
{
    /// <summary>
    /// Bridges the SCENE to the Core obstacle model: reads every <see cref="BoxCollider"/> under this
    /// GameObject into a <see cref="List{Obstacle}"/>, so the scene is the single source of truth for
    /// obstacle geometry (no coordinates duplicated in code). <see cref="Obstacle.Id"/> = the child
    /// GameObject's name (e.g. "O1", "O2", "O3").
    ///
    /// Keep obstacle objects AXIS-ALIGNED (no rotation) so the box AABB is exact — a rotated BoxCollider's
    /// world <c>bounds</c> is the enclosing AABB (slightly larger than the box).
    /// </summary>
    public sealed class SceneObstacles : MonoBehaviour
    {
        public List<Obstacle> Collect()
        {
            var list = new List<Obstacle>();
            foreach (var c in GetComponentsInChildren<BoxCollider>())
            {
                Bounds b = c.bounds; // world-space AABB
                list.Add(new Obstacle(c.gameObject.name, b.center, b.extents));
            }
            return list;
        }
    }
}
