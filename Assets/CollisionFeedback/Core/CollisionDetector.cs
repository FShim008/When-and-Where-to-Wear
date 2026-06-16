using System.Collections.Generic;
using UnityEngine;

namespace CollisionFeedback.Core
{
    public enum OutcomeKind
    {
        Collision,
        NearMiss,
    }

    /// <summary>One measured outcome of a limb engaging an obstacle.</summary>
    public readonly struct OutcomeEvent
    {
        public readonly OutcomeKind Kind;
        public readonly Joint Limb;
        public readonly string ObstacleId;
        public readonly double DataTime;   // time of contact (Collision) or of engagement end (NearMiss)
        public readonly float Clearance;   // 0..Contact at a hit; the minimum distance achieved for a near-miss

        public OutcomeEvent(OutcomeKind kind, Joint limb, string obstacleId, double dataTime, float clearance)
        {
            Kind = kind;
            Limb = limb;
            ObstacleId = obstacleId;
            DataTime = dataTime;
            Clearance = clearance;
        }
    }

    public sealed class DetectorParams
    {
        public float ContactDistance = 0.03f;    // <= this counts as a COLLISION (limb essentially touching the foam)
        public float NearMissDistance = 0.12f;    // entering this band without contact = a NEAR-MISS
        public float ExitMargin = 0.05f;          // must clear NearMiss + this for an engagement to end (hysteresis)

        // Plan Task 7.3: per-limb effective contact radius (m) subtracted from the raw keypoint→obstacle
        // distance, to model the segment between the tracked joint and the real contact surface
        // (wrist→hand, ankle→foot). DEFAULT: null ⇒ 0 for every limb ⇒ behavior unchanged. When enabled
        // (Plan decision D6), mirror the same reach in the oracle so cues and outcomes stay consistent.
        public Dictionary<Joint, float> LimbContactRadius;

        public float RadiusFor(Joint limb) =>
            (LimbContactRadius != null && LimbContactRadius.TryGetValue(limb, out float r)) ? r : 0f;
    }

    /// <summary>
    /// Measures the OUTCOME side of the study: per-limb collisions, near-misses, and min clearance.
    /// Collisions are the numerator of the primary DV (collisions-per-opportunity); the denominator
    /// comes from the scripted opportunities (separate scheduler).
    ///
    /// Edge-triggered per ENGAGEMENT (one approach = one outcome): a limb "engages" when it enters the
    /// near-miss band and "disengages" once it clears NearMiss + ExitMargin. While engaged we track the
    /// minimum distance; a contact promotes the engagement to a collision. Hardware-free and
    /// deterministic (driven by frame timestamps).
    /// </summary>
    public sealed class CollisionDetector
    {
        private sealed class Engagement
        {
            public bool Active;
            public bool Hit;
            public float MinDistance = float.PositiveInfinity;
            public string ObstacleId;
        }

        private readonly IReadOnlyList<Obstacle> _obstacles;
        private readonly IReadOnlyList<Joint> _limbs;
        private readonly DetectorParams _p;
        private readonly Dictionary<Joint, Engagement> _eng = new();
        private readonly List<OutcomeEvent> _events = new();
        private readonly Dictionary<Joint, float> _currentDistance = new();

        public int Collisions { get; private set; }
        public int NearMisses { get; private set; }
        public IReadOnlyList<OutcomeEvent> Events => _events;

        /// <summary>Each limb's distance to its nearest obstacle as of the last <see cref="Tick"/>
        /// (feeds the avoidance-latency detector).</summary>
        public IReadOnlyDictionary<Joint, float> CurrentDistances => _currentDistance;

        public CollisionDetector(IReadOnlyList<Obstacle> obstacles, IReadOnlyList<Joint> limbs, DetectorParams p)
        {
            _obstacles = obstacles;
            _limbs = limbs;
            _p = p;
            for (int i = 0; i < limbs.Count; i++) _eng[limbs[i]] = new Engagement();
        }

        public void Tick(in PoseFrame frame)
        {
            for (int i = 0; i < _limbs.Count; i++)
            {
                Joint limb = _limbs[i];
                Vector3 pos = frame.Get(limb);

                float dist = float.PositiveInfinity;
                string nearestId = null;
                for (int o = 0; o < _obstacles.Count; o++)
                {
                    float d = _obstacles[o].DistanceTo(pos);
                    if (d < dist)
                    {
                        dist = d;
                        nearestId = _obstacles[o].Id;
                    }
                }
                float reach = _p.RadiusFor(limb);
                if (reach > 0f) dist = Mathf.Max(0f, dist - reach); // Task 7.3: effective limb reach (default 0 ⇒ no-op)
                _currentDistance[limb] = dist;

                Engagement e = _eng[limb];

                if (dist < _p.NearMissDistance)
                {
                    if (!e.Active)
                    {
                        e.Active = true;
                        e.Hit = false;
                        e.MinDistance = dist;
                        e.ObstacleId = nearestId;
                    }
                    else if (dist < e.MinDistance)
                    {
                        e.MinDistance = dist;
                        e.ObstacleId = nearestId;
                    }

                    if (!e.Hit && dist <= _p.ContactDistance)
                    {
                        e.Hit = true;
                        Collisions++;
                        _events.Add(new OutcomeEvent(OutcomeKind.Collision, limb, e.ObstacleId, frame.Timestamp, dist));
                    }
                }
                else if (e.Active && dist > _p.NearMissDistance + _p.ExitMargin)
                {
                    if (!e.Hit)
                    {
                        NearMisses++;
                        _events.Add(new OutcomeEvent(OutcomeKind.NearMiss, limb, e.ObstacleId, frame.Timestamp, e.MinDistance));
                    }
                    e.Active = false;
                    e.Hit = false;
                    e.MinDistance = float.PositiveInfinity;
                }
            }
        }

        /// <summary>
        /// Closes any engagement still open at block end, emitting a near-miss (with its captured
        /// minimum clearance) for non-hit engagements so they are not silently dropped. Call once
        /// when a block/recording ends. Already-counted collisions are NOT re-counted.
        /// </summary>
        public void Flush(double dataTime)
        {
            for (int i = 0; i < _limbs.Count; i++)
            {
                Engagement e = _eng[_limbs[i]];
                if (!e.Active) continue;

                if (!e.Hit)
                {
                    NearMisses++;
                    _events.Add(new OutcomeEvent(OutcomeKind.NearMiss, _limbs[i], e.ObstacleId, dataTime, e.MinDistance));
                }
                e.Active = false;
                e.Hit = false;
                e.MinDistance = float.PositiveInfinity;
            }
        }
    }
}
