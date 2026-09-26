using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CollisionFeedback.Core;
using Joint = CollisionFeedback.Core.Joint; // disambiguate from UnityEngine.Joint (physics component)

namespace CollisionFeedback.Tests
{
    /// <summary>
    /// Verifies the v2 layout-variant engine [Design v2 §6]: every variant is a true isometry of L1 (all
    /// pairwise distances preserved → identical difficulty by construction), mirrors swap left/right target
    /// limbs while preserving the limb-count balance, rotations leave limbs untouched, and the schedule's
    /// non-geometric content (ids, onsets, windows, obstacle targets) is invariant across variants.
    /// </summary>
    public class LayoutVariantsTests
    {
        private static readonly string[] AllIds = { "L1", "L2", "L3", "L4", "L5", "L6", LayoutVariants.PracticeId };

        // Every geometry-bearing point of the L1 stimulus set, in a fixed order.
        private static List<Vector3> Points(string layoutId)
        {
            var pts = new List<Vector3>();
            foreach (Layout1Stimulus s in LayoutVariants.Stimuli(layoutId))
            {
                if (s.HasOrb) pts.Add(s.OrbPosition);
                if (s.HasProjectile) pts.Add(s.ProjectileOrigin);
            }
            return pts;
        }

        [Test]
        public void All_seven_ids_resolve_to_distinct_transforms()
        {
            var seen = new HashSet<(int, bool)>();
            foreach (string id in AllIds)
            {
                Assert.That(LayoutVariants.TryGet(id, out int q, out bool m), Is.True, $"{id} must resolve");
                Assert.That(seen.Add((((q % 4) + 4) % 4, m)), Is.True, $"{id} duplicates another variant");
            }
            Assert.That(LayoutVariants.TryGet("BOGUS", out _, out _), Is.False);
            Assert.That(LayoutVariants.StudyIds, Has.Length.EqualTo(6));
            Assert.That(LayoutVariants.StudyIds, Does.Not.Contain(LayoutVariants.PracticeId));
        }

        [Test]
        public void Every_variant_preserves_all_pairwise_stimulus_distances()
        {
            List<Vector3> l1 = Points("L1");
            foreach (string id in AllIds)
            {
                List<Vector3> v = Points(id);
                Assert.That(v.Count, Is.EqualTo(l1.Count), $"{id}: point count changed");
                for (int i = 0; i < l1.Count; i++)
                    for (int j = i + 1; j < l1.Count; j++)
                        Assert.That(Vector3.Distance(v[i], v[j]),
                                    Is.EqualTo(Vector3.Distance(l1[i], l1[j])).Within(1e-4f),
                                    $"{id}: distance ({i},{j}) not preserved — not an isometry");
            }
        }

        [Test]
        public void Variants_preserve_distance_to_the_arena_center()
        {
            List<Vector3> l1 = Points("L1");
            foreach (string id in AllIds)
            {
                List<Vector3> v = Points(id);
                for (int i = 0; i < l1.Count; i++)
                    Assert.That(v[i].magnitude, Is.EqualTo(l1[i].magnitude).Within(1e-4f),
                                $"{id}: point {i} moved relative to the center — transform is not about the origin");
            }
        }

        [Test]
        public void Rotations_keep_limbs_mirrors_swap_them_and_counts_are_preserved()
        {
            List<Opportunity> l1 = OpportunitySchedules.Layout1();

            foreach (string id in new[] { "L2", "L3", "L4" })    // pure rotations: chirality preserved
            {
                List<Opportunity> v = LayoutVariants.Schedule(id);
                for (int i = 0; i < l1.Count; i++)
                    Assert.That(v[i].TargetLimb, Is.EqualTo(l1[i].TargetLimb), $"{id} E{i + 1}: rotation changed a limb");
            }

            foreach (string id in new[] { "L5", "L6", LayoutVariants.PracticeId })   // mirrors: chirality flips
            {
                List<Opportunity> v = LayoutVariants.Schedule(id);
                var l1Counts = new Dictionary<Joint, int>();
                var vCounts = new Dictionary<Joint, int>();
                for (int i = 0; i < l1.Count; i++)
                {
                    Assert.That(v[i].TargetLimb, Is.EqualTo(LayoutVariants.Limb(l1[i].TargetLimb, true)),
                                $"{id} E{i + 1}: mirror did not swap the limb");
                    l1Counts[l1[i].TargetLimb] = l1Counts.GetValueOrDefault(l1[i].TargetLimb) + 1;
                    vCounts[v[i].TargetLimb] = vCounts.GetValueOrDefault(v[i].TargetLimb) + 1;
                }
                // L1 is left-right balanced, so the mirrored histogram must be IDENTICAL, not merely permuted.
                foreach (var kv in l1Counts)
                    Assert.That(vCounts.GetValueOrDefault(kv.Key), Is.EqualTo(kv.Value),
                                $"{id}: limb-count balance broken for {kv.Key}");
            }
        }

        [Test]
        public void Schedule_ids_times_windows_and_obstacles_are_invariant_across_variants()
        {
            List<Opportunity> l1 = OpportunitySchedules.Layout1();
            foreach (string id in AllIds)
            {
                List<Opportunity> v = LayoutVariants.Schedule(id);
                Assert.That(v.Count, Is.EqualTo(l1.Count));
                for (int i = 0; i < l1.Count; i++)
                {
                    Assert.That(v[i].Id, Is.EqualTo(l1[i].Id));
                    Assert.That(v[i].OnsetTime, Is.EqualTo(l1[i].OnsetTime));
                    Assert.That(v[i].WindowSeconds, Is.EqualTo(l1[i].WindowSeconds));
                    Assert.That(v[i].TargetObstacleId, Is.EqualTo(l1[i].TargetObstacleId),
                                "obstacle ids never change — the volumes MOVE under the same ids");
                }
            }
        }

        [Test]
        public void Point_transforms_compose_correctly()
        {
            var p = new Vector3(0.35f, 0.2f, 1.25f);

            // Mirror is an involution.
            Vector3 twiceMirrored = LayoutVariants.Point(LayoutVariants.Point(p, 0, true), 0, true);
            Assert.That(Vector3.Distance(twiceMirrored, p), Is.LessThan(1e-5f));

            // Four quarter turns = identity.
            Vector3 q = p;
            for (int i = 0; i < 4; i++) q = LayoutVariants.Point(q, 1, false);
            Assert.That(Vector3.Distance(q, p), Is.LessThan(1e-5f));

            // Height is never touched.
            foreach (string id in AllIds)
            {
                LayoutVariants.TryGet(id, out int turns, out bool mirror);
                Assert.That(LayoutVariants.Point(p, turns, mirror).y, Is.EqualTo(p.y));
            }
        }

        [Test]
        public void Extents_swap_xz_on_odd_turns_and_stay_positive()
        {
            var e = new Vector3(0.1f, 1.5f, 0.6f); // the O3 panel

            Vector3 odd = LayoutVariants.Extents(e, 1);
            Assert.That(odd.x, Is.EqualTo(0.6f).Within(1e-6f));
            Assert.That(odd.z, Is.EqualTo(0.1f).Within(1e-6f));
            Assert.That(odd.y, Is.EqualTo(1.5f).Within(1e-6f));

            Vector3 even = LayoutVariants.Extents(e, 2);
            Assert.That(even, Is.EqualTo(e));

            Vector3 abs = LayoutVariants.Extents(new Vector3(-1f, -2f, -3f), 0);
            Assert.That(abs.x > 0f && abs.y > 0f && abs.z > 0f, Is.True, "extents must stay positive");
        }

        [Test]
        public void Wall_targets_present_in_every_variant_schedule()
        {
            foreach (string id in AllIds)
            {
                List<Opportunity> v = LayoutVariants.Schedule(id);
                Assert.That(v.FindAll(o => o.TargetObstacleId == "O4a").Count, Is.EqualTo(1), id);
                Assert.That(v.FindAll(o => o.TargetObstacleId == "O4b").Count, Is.EqualTo(1), id);
                Assert.That(v.Exists(o => o.TargetObstacleId == "BOUNDARY"), Is.False, id);
            }
        }
    }
}
