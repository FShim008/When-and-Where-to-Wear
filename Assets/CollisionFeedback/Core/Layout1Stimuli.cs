using System.Collections.Generic;
using UnityEngine;

namespace CollisionFeedback.Core
{
    /// <summary>What an L1 event spawns to induce the collision opportunity.</summary>
    public enum StimulusKind
    {
        Orb,         // a glowing orb to reach/grab (drives a limb toward an obstacle)
        Projectile,  // a projectile that forces a lean/duck/step
        Both,        // compound event (E12): an orb AND a projectile at once
    }

    /// <summary>
    /// The physical spec of one Layout-L1 event — where/what to spawn — paired by Id + Onset with the
    /// metrics-side <see cref="Opportunity"/> (see <see cref="OpportunitySchedules.Layout1"/>). Positions are
    /// in the Unity world frame (study world frame). Kept separate from <see cref="Opportunity"/> so the
    /// metrics denominator stays free of rendering data.
    /// </summary>
    public readonly struct Layout1Stimulus
    {
        public readonly string Id;
        public readonly double Onset;             // s, block-relative (matches the opportunity onset)
        public readonly StimulusKind Kind;
        public readonly Vector3 OrbPosition;        // valid when Kind != Projectile
        public readonly Vector3 ProjectileOrigin;   // valid when Kind != Orb

        public Layout1Stimulus(string id, double onset, StimulusKind kind, Vector3 orbPosition, Vector3 projectileOrigin)
        {
            Id = id;
            Onset = onset;
            Kind = kind;
            OrbPosition = orbPosition;
            ProjectileOrigin = projectileOrigin;
        }

        public bool HasOrb => Kind != StimulusKind.Projectile;
        public bool HasProjectile => Kind != StimulusKind.Orb;
    }

    /// <summary>
    /// The 12 scripted L1 events from IEEEVR2027_Layout1_Storyboard.md, as spawnable stimuli. The storyboard
    /// lists spawn coordinates as <c>(X, Z, Y)</c>; the helpers below convert them to Unity <c>(X, Y, Z)</c>,
    /// so the literals read in the same order as the storyboard table (audit-friendly). Onsets + ids match
    /// <see cref="OpportunitySchedules.Layout1"/> exactly (cross-checked in tests).
    /// </summary>
    public static class Layout1Stimuli
    {
        public static List<Layout1Stimulus> All() => new()
        {
            Orb ("E1",    8.0,  1.45f,  1.05f, 1.35f),  // reach forward-right past the pillar (O2)
            Proj("E2",   22.0,  1.70f,  0.30f, 1.30f),  // projectile from the right -> lean/step left
            Orb ("E3",   35.0,  0.35f,  1.50f, 0.35f),  // low orb -> crouch/reach down (O1)
            Orb ("E4",   48.0, -1.45f,  0.35f, 1.20f),  // reach left around the panel edge (O3)
            Orb ("E5",   61.0, -0.15f,  1.65f, 1.30f),  // orb at the front edge -> step to boundary
            Proj("E6",   74.0, -1.50f,  1.60f, 1.60f),  // projectile from front-left -> duck/step right
            Orb ("E7",   87.0,  1.50f,  0.65f, 1.10f),  // wide right-arm sweep (O2)
            Proj("E8",  100.0,  0.20f,  1.70f, 0.30f),  // low projectile from front -> step back/side (O1)
            Orb ("E9",  113.0, -1.30f,  0.25f, 1.55f),  // reach up-and-over the panel (O3)
            Proj("E10", 126.0,  1.70f,  0.20f, 1.40f),  // fast projectile from the right -> big left lunge
            Orb ("E11", 139.0, -1.55f, -1.00f, 1.20f),  // orb behind-left -> backward/diagonal step to boundary
            Both("E12", 152.0,  1.45f,  0.95f, 1.30f,    // compound: orb to the right...
                                -1.70f,  0.90f, 1.30f),  // ...and a projectile from the left (cue lowest-TTC limb)
        };

        // Storyboard order is (X, Z, Y); Unity Vector3 is (X, Y, Z).
        private static Layout1Stimulus Orb(string id, double t, float x, float z, float y) =>
            new(id, t, StimulusKind.Orb, new Vector3(x, y, z), Vector3.zero);

        private static Layout1Stimulus Proj(string id, double t, float x, float z, float y) =>
            new(id, t, StimulusKind.Projectile, Vector3.zero, new Vector3(x, y, z));

        private static Layout1Stimulus Both(string id, double t,
                                            float ox, float oz, float oy,
                                            float px, float pz, float py) =>
            new(id, t, StimulusKind.Both, new Vector3(ox, oy, oz), new Vector3(px, py, pz));
    }
}
