using System;
using System.Collections.Generic;
using UnityEngine;
using CollisionFeedback.Core;

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
        [Header("Prefabs (optional — leave empty to just log the timeline)")]
        [SerializeField] private GameObject orbPrefab;
        [SerializeField] private GameObject projectilePrefab;

        [Header("Projectiles")]
        [SerializeField] private Transform aimTarget;          // projectiles fly toward this (default: HMD / start pad)
        [SerializeField] private float projectileSpeed = 6f;   // m/s

        [Header("Lifetimes (s; <= 0 = never auto-despawn)")]
        [SerializeField] private float orbLifetime = 8f;
        [SerializeField] private float projectileLifetime = 4f;

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
            if (orbPrefab == null) return;
            GameObject orb = Instantiate(orbPrefab, e.OrbPosition, Quaternion.identity, _container);
            orb.AddComponent<SpawnedStimulus>().IsOrb = true;
            if (orbLifetime > 0f) Destroy(orb, orbLifetime);
        }

        private void SpawnProjectile(Layout1Stimulus e)
        {
            if (projectilePrefab == null) return;
            GameObject proj = Instantiate(projectilePrefab, e.ProjectileOrigin, Quaternion.identity, _container);
            proj.AddComponent<SpawnedStimulus>().IsOrb = false;

            Vector3 dir = AimPoint() - e.ProjectileOrigin;
            if (dir.sqrMagnitude > 1e-6f && proj.TryGetComponent(out Rigidbody rb))
                rb.linearVelocity = dir.normalized * projectileSpeed;

            if (projectileLifetime > 0f) Destroy(proj, projectileLifetime);
        }

        private Vector3 AimPoint()
        {
            if (aimTarget != null) return aimTarget.position;
            if (Camera.main != null) return Camera.main.transform.position;
            return new Vector3(0f, 1.2f, -1.0f); // start/goal pad, ~chest height
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
