using UnityEngine;
using CollisionFeedback.Core;

namespace CollisionFeedback.Runtime
{
    /// <summary>
    /// Hardware-free sink: logs every feedback event to the Unity console. Lets you watch the brain
    /// fire before any device exists. Replace with a real bHaptics sink later behind the same
    /// <see cref="IFeedbackSink"/> seam (and add a CSV/LSL logger alongside it).
    /// </summary>
    public sealed class UnityLogSink : IFeedbackSink
    {
        public void Fire(in FeedbackCommand c)
        {
            Debug.Log($"[FEEDBACK] {c.Modality} {c.Trigger} site={c.Site} limb={c.Limb} " +
                      $"obstacle={c.ObstacleId} d={c.Distance:F2}m ttc={c.Ttc:F2}s t={c.DataTime:F2}s");
        }
    }
}
