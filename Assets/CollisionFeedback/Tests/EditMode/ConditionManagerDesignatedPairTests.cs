using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CollisionFeedback.Core;
using Joint = CollisionFeedback.Core.Joint; // disambiguate from UnityEngine.Joint (physics component)

namespace CollisionFeedback.Tests
{
    /// <summary>
    /// Cues must describe the DESIGNATED target limb × target hazard of an open opportunity
    /// [PAPER1_STUDY_DESIGN §6 "Required corrections"].
    ///
    /// Two defects are pinned here. First, the reactive rule used to combine "closing toward ANY obstacle"
    /// with the distance to the NEAREST one, so a limb closing on a distant hazard could fire an alert about a
    /// different hazard it merely happened to be near. Second — and worse — the global re-fire debounce meant
    /// one alert about an undesignated hazard could SWALLOW the designated warning, so the manipulation was
    /// silently not delivered on that opportunity and nothing in the data said so.
    /// </summary>
    public class ConditionManagerDesignatedPairTests
    {
        private const float Hz = 90f;
        private const float Dt = 1f / Hz;
        private static readonly Vector3 Far = new(50f, 50f, 50f);
        private static readonly Joint Limb = Joint.RightHand;

        private static List<Joint> Limbs() => new()
        {
            Joint.Chest, Joint.LeftHand, Joint.RightHand, Joint.LeftFoot, Joint.RightFoot,
        };

        // O1 is designated. O9 is an undesignated decoy sitting near the limb's start.
        private static List<Obstacle> Obstacles() => new()
        {
            new Obstacle("O1", new Vector3(0f, 1f, 2f), new Vector3(0.2f, 0.2f, 0.2f)),   // near face z = 1.8
            new Obstacle("O9", new Vector3(0f, 1f, -1f), new Vector3(0.2f, 0.2f, 0.2f)),  // near face z = -0.8
        };

        private static PoseFrame Frame(double t, Vector3 where)
        {
            var joints = new Vector3[JointInfo.Count];
            for (int j = 0; j < joints.Length; j++) joints[j] = Far;
            joints[(int)Limb] = where;
            return new PoseFrame { Timestamp = t, Joints = joints };
        }

        private static List<Opportunity> Designated(string hazard, double onset, double window) => new()
        {
            new Opportunity("E1", onset, window, Limb, hazard),
        };

        /// <summary>Straight-line approach along +z from <paramref name="fromZ"/> at a constant speed.</summary>
        private static RecordingSink Approach(IReadOnlyList<Opportunity> schedule, Condition condition,
                                              float fromZ, float toZ, float speed, double startT = 0.0)
        {
            var sink = new RecordingSink();
            var cm = new ConditionManager(condition, new OracleParams(), Obstacles(), Limbs(), sink, schedule);
            double t = startT;
            for (float z = fromZ; z <= toZ; z += speed * Dt, t += Dt)
                cm.Tick(Frame(t, new Vector3(0f, 1f, z)));
            return sink;
        }

        [Test]
        public void Fires_for_the_designated_hazard_inside_its_window()
        {
            RecordingSink sink = Approach(Designated("O1", onset: 0.0, window: 10.0),
                                          Condition.RB, fromZ: 0.0f, toZ: 1.75f, speed: 1.0f);

            Assert.That(sink.Fired.Count, Is.EqualTo(1));
            Assert.That(sink.Fired[0].ObstacleId, Is.EqualTo("O1"));
            Assert.That(sink.Fired[0].Limb, Is.EqualTo(Limb));
        }

        [Test]
        public void Never_fires_about_an_UNDESIGNATED_hazard_the_limb_walks_straight_into()
        {
            // The schedule designates O1, but this limb closes on O9 and ends up touching it. Before the fix
            // the participant would have been warned about a hazard that carries no measured opportunity.
            var schedule = Designated("O1", onset: 0.0, window: 10.0);
            var sink = new RecordingSink();
            var cm = new ConditionManager(Condition.RB, new OracleParams(), Obstacles(), Limbs(), sink, schedule);

            double t = 0;
            for (float z = 0f; z >= -0.75f; z -= 1.0f * Dt, t += Dt)   // travelling toward O9
                cm.Tick(Frame(t, new Vector3(0f, 1f, z)));

            Assert.That(sink.Fired, Is.Empty,
                "a cue about O9 would describe a hazard with no scheduled opportunity");
        }

        [Test]
        public void Never_fires_before_the_opportunity_window_opens()
        {
            // Designated hazard is right, limb approaches it — but the window has not opened yet.
            RecordingSink sink = Approach(Designated("O1", onset: 5.0, window: 2.0),
                                          Condition.RB, fromZ: 0.0f, toZ: 1.75f, speed: 1.0f);

            Assert.That(sink.Fired, Is.Empty);
        }

        [Test]
        public void An_undesignated_approach_can_no_longer_swallow_the_designated_warning()
        {
            // THE IMPORTANT ONE. The limb first brushes past the undesignated O9, then turns and approaches
            // its designated O1 — all inside one global debounce window (1.0 s by default). Previously the O9
            // alert consumed the debounce and the designated O1 warning was never delivered, so that
            // opportunity silently ran without its manipulation.
            var schedule = Designated("O1", onset: 0.0, window: 10.0);
            var sink = new RecordingSink();
            var cm = new ConditionManager(Condition.RB, new OracleParams(), Obstacles(), Limbs(), sink, schedule);

            double t = 0;
            // Leg 1: dip toward O9 (undesignated), coming within the reactive distance of it.
            for (float z = -0.3f; z >= -0.65f; z -= 1.2f * Dt, t += Dt)
                cm.Tick(Frame(t, new Vector3(0f, 1f, z)));
            // Leg 2: immediately turn and drive at O1 (designated), well within the same debounce window.
            for (float z = -0.65f; z <= 1.75f; z += 1.2f * Dt, t += Dt)
                cm.Tick(Frame(t, new Vector3(0f, 1f, z)));

            Assert.That(sink.Fired.Count, Is.EqualTo(1), "exactly the designated warning, nothing else");
            Assert.That(sink.Fired[0].ObstacleId, Is.EqualTo("O1"));
            Assert.That(sink.Fired[0].DataTime, Is.LessThan(t),
                "and it was delivered, not consumed by the debounce");
        }

        [Test]
        public void Predictive_conditions_also_honour_the_designated_pair()
        {
            var schedule = Designated("O1", onset: 0.0, window: 10.0);
            var sink = new RecordingSink();
            var cm = new ConditionManager(Condition.PB, new OracleParams(), Obstacles(), Limbs(), sink, schedule);

            double t = 0;
            for (float z = 0f; z >= -0.75f; z -= 1.0f * Dt, t += Dt)   // closing hard on undesignated O9
                cm.Tick(Frame(t, new Vector3(0f, 1f, z)));

            Assert.That(sink.Fired, Is.Empty);
        }

        [Test]
        public void A_limb_with_no_scheduled_opportunity_is_never_cued()
        {
            // The schedule designates the RIGHT hand; the LEFT hand does the approaching.
            var schedule = Designated("O1", onset: 0.0, window: 10.0);
            var sink = new RecordingSink();
            var cm = new ConditionManager(Condition.RB, new OracleParams(), Obstacles(), Limbs(), sink, schedule);

            double t = 0;
            for (float z = 0f; z <= 1.75f; z += 1.0f * Dt, t += Dt)
            {
                var joints = new Vector3[JointInfo.Count];
                for (int j = 0; j < joints.Length; j++) joints[j] = Far;
                joints[(int)Joint.LeftHand] = new Vector3(0f, 1f, z);
                cm.Tick(new PoseFrame { Timestamp = t, Joints = joints });
            }

            Assert.That(sink.Fired, Is.Empty);
        }

        // ── The hazard-agnostic path (no schedule): still used by bench tools and the older tests ──────────

        [Test]
        public void Without_a_schedule_a_reactive_cue_needs_the_limb_to_be_closing_on_the_NEAREST_hazard()
        {
            // This is the original consistency defect. The limb sits well inside the reactive distance of O9
            // (its nearest hazard) but is moving AWAY from it, toward the distant O1. The old rule combined
            // "closing toward any obstacle" (true, because of O1) with "distance to the nearest" (O9) and
            // fired an alert about O9 — a hazard the limb was actively retreating from.
            var sink = new RecordingSink();
            var cm = new ConditionManager(Condition.RB, new OracleParams(), Obstacles(), Limbs(), sink);

            double t = 0;
            // Start 0.15 m clear of O9's near face (z = -0.8) and move toward O1, i.e. away from O9.
            for (float z = -0.65f; z <= -0.35f; z += 1.0f * Dt, t += Dt)
                cm.Tick(Frame(t, new Vector3(0f, 1f, z)));

            Assert.That(sink.Fired, Is.Empty,
                "a reactive cue must describe a hazard the limb is actually closing on");
        }

        [Test]
        public void Without_a_schedule_the_nearest_hazard_being_approached_still_fires()
        {
            // The same geometry, approached rather than retreated from: the cue must still work.
            var sink = new RecordingSink();
            var cm = new ConditionManager(Condition.RB, new OracleParams(), Obstacles(), Limbs(), sink);

            double t = 0;
            for (float z = -0.2f; z >= -0.75f; z -= 1.0f * Dt, t += Dt)
                cm.Tick(Frame(t, new Vector3(0f, 1f, z)));

            Assert.That(sink.Fired.Count, Is.EqualTo(1));
            Assert.That(sink.Fired[0].ObstacleId, Is.EqualTo("O9"));
        }

        [Test]
        public void The_None_condition_stays_silent_with_a_schedule()
        {
            RecordingSink sink = Approach(Designated("O1", onset: 0.0, window: 10.0),
                                          Condition.None, fromZ: 0.0f, toZ: 1.75f, speed: 1.0f);
            Assert.That(sink.Fired, Is.Empty);
        }

        [Test]
        public void A_designated_hazard_id_missing_from_the_scene_fires_nothing()
        {
            // Rather than silently falling back to some other hazard. SessionRunner validates hazard ids at
            // startup; this is the belt-and-braces behaviour if one slips through.
            RecordingSink sink = Approach(Designated("NOPE", onset: 0.0, window: 10.0),
                                          Condition.RB, fromZ: 0.0f, toZ: 1.75f, speed: 1.0f);
            Assert.That(sink.Fired, Is.Empty);
        }
    }
}
