using UnityEngine;

namespace CollisionFeedback.Core
{
    /// <summary>One frame's visual-alert state for an obstacle.</summary>
    public readonly struct VisualAlertLevel
    {
        public readonly bool Active;      // is the obstacle within the alert range?
        public readonly float Intensity;  // 0..1: 0 at the alert edge, 1 at contact (the depth cue)
        public readonly float PulseHz;     // pulse rate; rises (interpulse interval shrinks) as the limb nears

        public VisualAlertLevel(bool active, float intensity, float pulseHz)
        {
            Active = active;
            Intensity = intensity;
            PulseHz = pulseHz;
        }

        public static readonly VisualAlertLevel Off = new(false, 0f, 0f);
    }

    /// <summary>
    /// Best-practice VISUAL obstacle-alert grading for the Visual reference condition [Protocol 2.3] —
    /// detection-matched to the haptic conditions (same reactive distance D, same oracle/tracking). Synthesizes
    /// the most-effective elements from the literature into one transparent curve: proximity-graded pulsing
    /// whose interpulse interval shrinks as the limb nears (SafeXR), plus a depth-cue intensity (Huang/Kanamori)
    /// the renderer maps to colour. Pure + testable; the Runtime <c>VisualObstacleAlert</c> turns a level into
    /// the on-screen highlight (presence-aware, no audio).
    /// </summary>
    public static class VisualAlertModel
    {
        /// <param name="distance">min distance from any tracked limb to the obstacle surface (m).</param>
        /// <param name="alertDistance">the reactive distance D (same value the reactive haptic conditions use).</param>
        public static VisualAlertLevel Evaluate(float distance, float alertDistance,
                                                float minPulseHz = 1.5f, float maxPulseHz = 6f)
        {
            if (alertDistance <= 0f || distance >= alertDistance) return VisualAlertLevel.Off;
            float t = Mathf.Clamp01(1f - distance / alertDistance); // 0 at the edge -> 1 at contact
            float pulseHz = Mathf.Lerp(minPulseHz, maxPulseHz, t);
            return new VisualAlertLevel(true, t, pulseHz);
        }
    }
}
