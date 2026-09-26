using System.Collections.Generic;
using UnityEngine;

namespace CollisionFeedback.Core
{
    /// <summary>
    /// The six isometric layout variants [Design v2 §6] plus the practice variant. Each variant is a rigid
    /// isometry of the authored Layout-1 geometry about the arena center (the play-space origin): an optional
    /// mirror across the x-axis (x → −x) followed by k quarter-turn rotations about the vertical axis.
    ///
    /// WHY ISOMETRIES: rotations/reflections preserve every pairwise distance, approach angle, and event demand
    /// BY CONSTRUCTION, so all variants are identical in difficulty and differ only in what a participant could
    /// memorize across blocks — this is what defeats obstacle-location learning (foam could never rotate).
    /// The square arena makes all four rotations valid. Mirrors flip chirality, so mirrored variants swap the
    /// LEFT/RIGHT target limbs (L1's schedule is left-right balanced, so limb COUNTS are preserved exactly).
    ///
    /// One function family transforms everything — obstacle volumes, orb positions, projectile origins, and the
    /// scene transforms (SceneObstacles applies the same math to the collider children) — so the detector, the
    /// spawner, and the visual alert can never disagree about where a variant's geometry is.
    /// </summary>
    public static class LayoutVariants
    {
        /// <summary>The six study variants, in the order fed to <see cref="SessionPlan"/>.</summary>
        public static readonly string[] StudyIds = { "L1", "L2", "L3", "L4", "L5", "L6" };

        /// <summary>The practice-block variant (the 7th dihedral element) — never one of the six study layouts,
        /// so practice cannot teach any test layout [v2 §7].</summary>
        public const string PracticeId = "LP";

        /// <summary>
        /// Resolve a layout id to its transform: mirror first (x → −x), then <paramref name="quarterTurns"/>
        /// 90° rotations about the vertical axis. False for unknown ids (callers fall back to L1 + warn).
        /// L1 = identity · L2/L3/L4 = 90/180/270° · L5 = mirror · L6 = mirror+90° · LP = mirror+180°.
        /// </summary>
        public static bool TryGet(string layoutId, out int quarterTurns, out bool mirror)
        {
            switch (layoutId)
            {
                case "L1": quarterTurns = 0; mirror = false; return true;
                case "L2": quarterTurns = 1; mirror = false; return true;
                case "L3": quarterTurns = 2; mirror = false; return true;
                case "L4": quarterTurns = 3; mirror = false; return true;
                case "L5": quarterTurns = 0; mirror = true;  return true;
                case "L6": quarterTurns = 1; mirror = true;  return true;
                case PracticeId: quarterTurns = 2; mirror = true; return true;
                default:   quarterTurns = 0; mirror = false; return false;
            }
        }

        /// <summary>Transform a world/arena point: optional mirror across x, then k quarter turns about the
        /// vertical (y untouched). One quarter turn maps (x, z) → (z, −x).</summary>
        public static Vector3 Point(Vector3 p, int quarterTurns, bool mirror)
        {
            float x = mirror ? -p.x : p.x;
            float z = p.z;
            for (int i = 0; i < ((quarterTurns % 4) + 4) % 4; i++)
            {
                float nx = z;
                z = -x;
                x = nx;
            }
            return new Vector3(x, p.y, z);
        }

        /// <summary>Transform axis-aligned box extents (or a Transform's localScale): odd quarter turns swap
        /// the x/z spans; mirrors leave sizes unchanged. Values stay positive.</summary>
        public static Vector3 Extents(Vector3 e, int quarterTurns)
        {
            bool swap = (((quarterTurns % 4) + 4) % 4) % 2 == 1;
            float x = swap ? e.z : e.x;
            float z = swap ? e.x : e.z;
            return new Vector3(x < 0 ? -x : x, e.y < 0 ? -e.y : e.y, z < 0 ? -z : z);
        }

        /// <summary>Mirrors flip chirality: swap left↔right hands and feet. Head/Chest are midline — unchanged.</summary>
        public static Joint Limb(Joint limb, bool mirror)
        {
            if (!mirror) return limb;
            return limb switch
            {
                Joint.LeftHand  => Joint.RightHand,
                Joint.RightHand => Joint.LeftHand,
                Joint.LeftFoot  => Joint.RightFoot,
                Joint.RightFoot => Joint.LeftFoot,
                _               => limb,
            };
        }

        /// <summary>
        /// The opportunity schedule for a variant: identical ids, onsets, windows, and obstacle targets as
        /// Layout-1 (obstacle VOLUMES move with the variant under the same ids), with target limbs swapped
        /// left↔right on mirrored variants. Unknown ids return the L1 schedule unchanged.
        /// </summary>
        public static List<Opportunity> Schedule(string layoutId, double windowSeconds = 6.0)
        {
            TryGet(layoutId, out _, out bool mirror);
            List<Opportunity> baseSchedule = OpportunitySchedules.Layout1(windowSeconds);
            if (!mirror) return baseSchedule;

            var list = new List<Opportunity>(baseSchedule.Count);
            foreach (Opportunity op in baseSchedule)
                list.Add(new Opportunity(op.Id, op.OnsetTime, op.WindowSeconds,
                                         Limb(op.TargetLimb, true), op.TargetObstacleId));
            return list;
        }

        /// <summary>The stimulus set for a variant: Layout-1 events with orb positions and projectile origins
        /// carried through the same rigid transform as the obstacle volumes. Unknown ids return L1.</summary>
        public static List<Layout1Stimulus> Stimuli(string layoutId)
        {
            TryGet(layoutId, out int q, out bool m);
            List<Layout1Stimulus> baseStimuli = Layout1Stimuli.All();
            if (q == 0 && !m) return baseStimuli;

            var list = new List<Layout1Stimulus>(baseStimuli.Count);
            foreach (Layout1Stimulus s in baseStimuli)
                list.Add(new Layout1Stimulus(s.Id, s.Onset, s.Kind,
                                             Point(s.OrbPosition, q, m),
                                             Point(s.ProjectileOrigin, q, m)));
            return list;
        }
    }
}
