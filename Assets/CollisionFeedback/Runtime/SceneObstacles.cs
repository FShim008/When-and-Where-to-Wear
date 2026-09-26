using System.Collections.Generic;
using UnityEngine;
using CollisionFeedback.Core;

namespace CollisionFeedback.Runtime
{
    /// <summary>
    /// Bridges the SCENE to the Core obstacle model: reads every <see cref="BoxCollider"/> under this
    /// GameObject into a <see cref="List{Obstacle}"/>, so the scene is the single source of truth for
    /// obstacle geometry (no coordinates duplicated in code). <see cref="Obstacle.Id"/> = the child
    /// GameObject's name (e.g. "O1", "O2", "O3", "O4a", "O4b").
    ///
    /// v2 additions [Design v2 §4/§6]:
    ///  • On Awake it SPAWNS the two O4 wall volumes (front + left) that replaced the old chaperone-boundary
    ///    targets, so events E5/E11 hit registered virtual walls and the system chaperone never appears —
    ///    keep the whole arena ≥ 0.5 m inside the physical boundary. (Scene-authored children named O4a/O4b
    ///    take precedence; nothing is spawned then.)
    ///  • <see cref="ApplyVariant"/> moves every obstacle child to a layout variant's isometry (rotation /
    ///    mirror about the arena center) using the SAME <see cref="LayoutVariants"/> math the schedule and
    ///    stimuli use — detector, spawner, and visual alert can never disagree about the geometry.
    ///
    /// Keep obstacle objects AXIS-ALIGNED (no rotation) so the box AABB is exact — variants never rotate the
    /// transforms; 90° turns are realized as position moves + x/z size swaps, which keeps boxes exact.
    /// Runs early (negative execution order) so the walls exist before anything collects or hides them.
    /// </summary>
    [DefaultExecutionOrder(-150)]
    public sealed class SceneObstacles : MonoBehaviour
    {
        [Header("v2 wall volumes (O4a front, O4b left) — spawned in code; scene children of the same name win")]
        [SerializeField] private bool addWallSegments = true;
        [Tooltip("Distance of each wall's centerline from the arena center (m). Keep ≥ 0.5 m inside the chaperone.")]
        [SerializeField] private float wallDistance = 1.75f;
        [SerializeField] private float wallLength = 3.5f;
        [SerializeField] private float wallHeight = 2.0f;
        [SerializeField] private float wallThickness = 0.1f;

        private struct BaselinePose
        {
            public Transform T;
            public Vector3 LocalPos;
            public Vector3 LocalScale;
        }

        private readonly List<BaselinePose> _baseline = new(); // the authored L1 pose of every obstacle child
        private bool _initialized;

        private void Awake() => EnsureInitialized();

        private void EnsureInitialized()
        {
            if (_initialized) return;
            _initialized = true;

            if (addWallSegments)
            {
                SpawnWall("O4a", new Vector3(0f, wallHeight * 0.5f, wallDistance),
                                  new Vector3(wallLength, wallHeight, wallThickness));   // front wall (E5)
                SpawnWall("O4b", new Vector3(-wallDistance, wallHeight * 0.5f, 0f),
                                  new Vector3(wallThickness, wallHeight, wallLength));   // left wall (E11)
            }

            // Capture the authored L1 pose of every collider child — ApplyVariant always transforms FROM this
            // baseline, so applying variants in any order is stable.
            foreach (BoxCollider c in GetComponentsInChildren<BoxCollider>())
                _baseline.Add(new BaselinePose
                {
                    T = c.transform,
                    LocalPos = c.transform.localPosition,
                    LocalScale = c.transform.localScale,
                });
        }

        private void SpawnWall(string id, Vector3 localPos, Vector3 size)
        {
            foreach (Transform child in transform)
                if (child.name == id) return; // scene-authored override wins

            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = id;                                   // Obstacle.Id + the visual-alert lookup key
            go.transform.SetParent(transform, worldPositionStays: false);
            go.transform.localPosition = localPos;
            go.transform.localScale = size;
            go.GetComponent<BoxCollider>().isTrigger = true; // volume only — never deflects projectiles/bodies
            // The renderer stays attached so the Visual condition can glow the wall; VisualObstacleAlert hides
            // every obstacle renderer at startup (walls included) and shows only the Visual-condition veil.
        }

        /// <summary>
        /// Pose every obstacle child for <paramref name="layoutId"/> (L1–L6 / LP): position mirrored/rotated
        /// about the arena center, x/z sizes swapped on 90°/270° variants. Unknown ids warn and use L1.
        /// Call before collecting obstacles for a block (SessionRunner does).
        /// </summary>
        public void ApplyVariant(string layoutId)
        {
            EnsureInitialized();
            if (!LayoutVariants.TryGet(layoutId, out int quarterTurns, out bool mirror))
                Debug.LogWarning($"[SceneObstacles] Unknown layout '{layoutId}' — using L1 geometry.");

            foreach (BaselinePose b in _baseline)
            {
                if (b.T == null) continue;
                b.T.localPosition = LayoutVariants.Point(b.LocalPos, quarterTurns, mirror);
                b.T.localScale = LayoutVariants.Extents(b.LocalScale, quarterTurns);
            }
        }

        public List<Obstacle> Collect()
        {
            EnsureInitialized();
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
