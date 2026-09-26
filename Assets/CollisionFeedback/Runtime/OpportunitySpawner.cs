using System;
using System.Collections.Generic;
using UnityEngine;
using CollisionFeedback.Core;
using Joint = CollisionFeedback.Core.Joint; // disambiguate from UnityEngine.Joint (physics component)

namespace CollisionFeedback.Runtime
{
    /// <summary>
    /// Drives the Layout-L1 12-event timeline (<see cref="Layout1Stimuli"/>): at each event's onset it spawns
    /// the orb and/or launches the projectile that induces that opportunity, aims projectiles at the
    /// participant, and keeps the motivating score (+1 per orb delivered, -1 per projectile hit — Protocol §4).
    ///
    /// Hardware-free + XRI-free to compile. Works with NO prefabs assigned (it just logs the timeline and
    /// fires <see cref="Spawned"/>), so you can verify the 12-event schedule before building any art; assign
    /// the orb / projectile prefabs + an aim target in the Inspector to make it physical. NOTE: the science DV
    /// is computed by the BlockRunner from tracking, NOT from this score — the score only drives committed motion.
    ///
    /// Timing: self-paces from enable when <see cref="autoStart"/> is true; otherwise drive it from the session
    /// loop via <see cref="Tick"/> with block-relative time, so it shares the BlockRunner's clock.
    /// </summary>
    public sealed class OpportunitySpawner : MonoBehaviour
    {
        [Header("Prefabs (optional — leave empty to spawn primitive placeholders)")]
        [SerializeField] private GameObject orbPrefab;
        [SerializeField] private GameObject projectilePrefab;
        [Tooltip("If a prefab is null, spawn a primitive sphere so the dodge/orb task is playable WITHOUT authored art (pilot/dev). Turn off to just log the timeline.")]
        [SerializeField] private bool spawnPrimitivesIfNoPrefab = true;
        [SerializeField] private float orbSize = 0.12f;        // m diameter of the placeholder orb
        [SerializeField] private float projectileSize = 0.12f; // m diameter of the placeholder projectile

        [Header("Projectiles")]
        [SerializeField] private Transform aimTarget;          // projectiles fly toward this (default: HMD / start pad)
        [SerializeField] private float projectileSpeed = 6f;   // m/s

        [Header("Lifetimes (s; <= 0 = never auto-despawn)")]
        [SerializeField] private float orbLifetime = 8f;
        [SerializeField] private float projectileLifetime = 4f;

        [Header("Interaction radii (m) — tracking-driven, no physics colliders needed")]
        [Tooltip("A hand within this distance of an orb collects it (+1). Covers the orb radius + hand size.")]
        [SerializeField] private float orbTouchRadius = 0.15f;
        [Tooltip("A projectile within this distance of the head or chest counts as a hit (-1).")]
        [SerializeField] private float projectileHitRadius = 0.25f;

        [Header("Timing")]
        [SerializeField] private bool autoStart = true;        // false => drive via Tick() from the session loop

        public int Deliveries { get; private set; }
        public int Hits { get; private set; }
        public int Score => Deliveries - Hits;

        /// <summary>Fired when an event's stimulus spawns (for the event log / SFX / the metrics layer).</summary>
        public event Action<Layout1Stimulus> Spawned;

        private List<Layout1Stimulus> _events;
        private bool[] _fired;
        private float _startTime;
        private bool _running;
        private bool _external;
        private Transform _container;

        // Live stimuli, tested against the tracked body each frame by CheckInteractions.
        private readonly List<GameObject> _activeOrbs = new();
        private readonly List<GameObject> _activeProjectiles = new();

        private void Awake() => EnsureInitialized();

        private void EnsureInitialized()
        {
            if (_events != null) return;
            _events = Layout1Stimuli.All();
            _fired = new bool[_events.Count];
            _container = new GameObject("SpawnedStimuli").transform;
            _container.SetParent(transform, worldPositionStays: false);
        }

        private void Start()
        {
            EnsureInitialized();
            if (autoStart && !_external) Begin();
        }

        /// <summary>Drive the timeline from an EXTERNAL clock (the session loop) via <see cref="Tick"/> instead
        /// of self-pacing, so stimuli stay in lockstep with the BlockRunner's opportunity windows.</summary>
        public void DriveExternally()
        {
            _external = true;
            Begin();
        }

        /// <summary>
        /// Claim the spawner WITHOUT starting the timeline. The session driver calls this at session start so
        /// the autoStart self-pacing never fires stimuli during gates, the cue tour, breaks, or questionnaires —
        /// events must only ever run on a block's clock (via <see cref="DriveExternally"/> + <see cref="Tick"/>).
        /// </summary>
        public void StandBy()
        {
            EnsureInitialized();
            _external = true;
            _running = false;
        }

        /// <summary>
        /// Load the stimulus set for a layout variant [Design v2 §6]: the L1 events with orb positions and
        /// projectile origins carried through the variant's isometry — the SAME transform SceneObstacles applies
        /// to the obstacle volumes, so stimuli and hazards move together. Call BEFORE the block starts
        /// (SessionRunner does, right before <see cref="DriveExternally"/>). Resets the fired flags.
        /// </summary>
        public void SetLayout(string layoutId)
        {
            EnsureInitialized();
            _events = LayoutVariants.Stimuli(layoutId);
            _fired = new bool[_events.Count];
        }

        /// <summary>(Re)start the timeline from t = 0.</summary>
        public void Begin()
        {
            EnsureInitialized();
            for (int i = 0; i < _fired.Length; i++) _fired[i] = false;
            _startTime = Time.time;
            _running = true;
        }

        private void Update()
        {
            if (_running && autoStart && !_external) Tick(Time.time - _startTime);
        }

        /// <summary>Advance the timeline to <paramref name="blockTime"/> (s, block-relative); fires any due events.</summary>
        public void Tick(double blockTime)
        {
            if (_events == null) return;
            for (int i = 0; i < _events.Count; i++)
            {
                if (_fired[i] || blockTime < _events[i].Onset) continue;
                Fire(_events[i]);
                _fired[i] = true;
            }
        }

        private void Fire(Layout1Stimulus e)
        {
            if (e.HasOrb) SpawnOrb(e);
            if (e.HasProjectile) SpawnProjectile(e);
            Spawned?.Invoke(e);
            Debug.Log($"[OpportunitySpawner] {e.Id} @ t={e.Onset:F0}s -> {e.Kind}");
        }

        private void SpawnOrb(Layout1Stimulus e)
        {
            GameObject orb;
            if (orbPrefab != null) orb = Instantiate(orbPrefab, e.OrbPosition, Quaternion.identity, _container);
            else if (spawnPrimitivesIfNoPrefab) orb = MakePrimitive(e.OrbPosition, orbSize, new Color(0.2f, 0.9f, 1f), isOrb: true);
            else return;

            orb.AddComponent<SpawnedStimulus>().IsOrb = true;
            _activeOrbs.Add(orb);
            if (orbLifetime > 0f) Destroy(orb, orbLifetime);
        }

        private void SpawnProjectile(Layout1Stimulus e)
        {
            GameObject proj;
            if (projectilePrefab != null) proj = Instantiate(projectilePrefab, e.ProjectileOrigin, Quaternion.identity, _container);
            else if (spawnPrimitivesIfNoPrefab) proj = MakePrimitive(e.ProjectileOrigin, projectileSize, new Color(1f, 0.3f, 0.1f), isOrb: false);
            else return;

            proj.AddComponent<SpawnedStimulus>().IsOrb = false;
            _activeProjectiles.Add(proj);

            Vector3 dir = AimPoint() - e.ProjectileOrigin;
            if (dir.sqrMagnitude > 1e-6f && proj.TryGetComponent(out Rigidbody rb))
                rb.linearVelocity = dir.normalized * projectileSpeed;

            if (projectileLifetime > 0f) Destroy(proj, projectileLifetime);
        }

        // Code-only sphere placeholder so the dodge/orb task is playable without authored art (pilot/dev).
        // Orb = trigger sphere (a reach target); projectile = a no-gravity rigidbody that flies straight.
        private GameObject MakePrimitive(Vector3 pos, float size, Color color, bool isOrb)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = isOrb ? "OrbPrimitive" : "ProjectilePrimitive";
            go.transform.SetParent(_container, worldPositionStays: false);
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * Mathf.Max(0.02f, size);

            Collider col = go.GetComponent<Collider>();
            if (isOrb)
            {
                if (col != null) col.isTrigger = true; // reach target, not a physics body
            }
            else
            {
                Rigidbody rb = go.AddComponent<Rigidbody>();
                rb.useGravity = false;                 // constant-velocity flight toward the aim point
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            }

            Renderer rend = go.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material.color = color; // material instance — fine for a placeholder
                if (rend.material.HasProperty("_EmissionColor"))
                {
                    rend.material.EnableKeyword("_EMISSION");
                    rend.material.SetColor("_EmissionColor", color * 0.6f);
                }
            }
            return go;
        }

        private Vector3 AimPoint()
        {
            if (aimTarget != null) return aimTarget.position;
            if (Camera.main != null) return Camera.main.transform.position;
            return new Vector3(0f, 1.2f, -1.0f); // start/goal pad, ~chest height
        }

        /// <summary>
        /// Score the TASK against the tracked body: a hand within <see cref="orbTouchRadius"/> of an orb
        /// collects it (+1); a projectile within <see cref="projectileHitRadius"/> of the head or chest counts
        /// as a hit (−1). Call once per frame from the session loop with the current pose.
        ///
        /// Distance-based on purpose: the tracked limbs are plain Transforms (controller / Vicon / tracker
        /// proxies) with no colliders, so physics triggers would never fire. This keeps scoring identical
        /// across every tracking backend.
        ///
        /// IMPORTANT [Design v2 §5]: collecting an orb or taking a projectile hit is TASK feedback — identical
        /// in every condition and carrying no information about hazard locations. Never attach a haptic pulse,
        /// alarm, or hazard-revealing effect here: that would add a feedback channel outside the manipulated
        /// conditions and would contaminate the None condition.
        /// </summary>
        public void CheckInteractions(in PoseFrame frame)
        {
            Vector3 lh = frame.Get(Joint.LeftHand), rh = frame.Get(Joint.RightHand);
            for (int i = _activeOrbs.Count - 1; i >= 0; i--)
            {
                GameObject orb = _activeOrbs[i];
                if (orb == null) { _activeOrbs.RemoveAt(i); continue; }   // expired by lifetime
                Vector3 p = orb.transform.position;
                if (Vector3.Distance(p, lh) <= orbTouchRadius || Vector3.Distance(p, rh) <= orbTouchRadius)
                {
                    _activeOrbs.RemoveAt(i);
                    NotifyDelivered(orb);
                }
            }

            Vector3 head = frame.Get(Joint.Head), chest = frame.Get(Joint.Chest);
            for (int i = _activeProjectiles.Count - 1; i >= 0; i--)
            {
                GameObject proj = _activeProjectiles[i];
                if (proj == null) { _activeProjectiles.RemoveAt(i); continue; }
                Vector3 p = proj.transform.position;
                if (Vector3.Distance(p, head) <= projectileHitRadius ||
                    Vector3.Distance(p, chest) <= projectileHitRadius)
                {
                    _activeProjectiles.RemoveAt(i);
                    NotifyHit(proj);
                }
            }
        }

        /// <summary>Call when an orb reaches the goal (e.g. from <see cref="ScoreZone"/>).</summary>
        public void NotifyDelivered(GameObject orb)
        {
            Deliveries++;
            if (orb != null) Destroy(orb);
        }

        /// <summary>Call when a projectile contacts the participant.</summary>
        public void NotifyHit(GameObject projectile)
        {
            Hits++;
            if (projectile != null) Destroy(projectile);
        }
    }

    /// <summary>Code-only marker the spawner attaches to spawned objects so <see cref="ScoreZone"/> can tell an
    /// orb from a projectile without project-wide tags. Added via code only — never in the Editor.</summary>
    public sealed class SpawnedStimulus : MonoBehaviour
    {
        public bool IsOrb;
    }
}
