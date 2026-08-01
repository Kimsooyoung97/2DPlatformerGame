using UnityEngine;

namespace NAN2026.Showroom
{
    /// <summary>
    /// Trigger volume that stores a respawn position on the player when touched.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class Checkpoint2D : MonoBehaviour
    {
        [SerializeField] private Vector3 spawnOffset = Vector3.zero;

        private bool reached;

        private void Reset()
        {
            GetComponent<BoxCollider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (reached)
                return;

            PlayerHealth player = other.GetComponentInParent<PlayerHealth>();
            if (player == null)
                return;

            reached = true;
            player.SetCheckpoint(transform.position + spawnOffset);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.3f, 0.9f, 0.5f, 0.55f);
            Gizmos.DrawWireCube(transform.position, new Vector3(1.4f, 3f, 0.1f));
        }
    }
}
