using UnityEngine;

namespace CollisionFeedback.Runtime
{
    /// <summary>
    /// Put on the goal-pad trigger. When a spawned ORB enters, it scores a delivery on the
    /// <see cref="OpportunitySpawner"/> and despawns the orb. (Projectile hits are scored from the
    /// projectile/participant side via <see cref="OpportunitySpawner.NotifyHit"/>.)
    /// Needs a Collider with <c>Is Trigger</c> on this object (added/enabled automatically).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class ScoreZone : MonoBehaviour
    {
        [SerializeField] private OpportunitySpawner spawner;

        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
            if (spawner == null) spawner = FindFirstObjectByType<OpportunitySpawner>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (spawner == null) return;
            var marker = other.GetComponentInParent<SpawnedStimulus>();
            if (marker != null && marker.IsOrb) spawner.NotifyDelivered(marker.gameObject);
        }
    }
}
