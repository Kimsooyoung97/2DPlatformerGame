using System.Collections;
using UnityEngine;

namespace NAN2026.Showroom
{
    /// <summary>
    /// A block hanging quietly above the path. Walk underneath and it drops on your head.
    /// Once landed it turns harmless and solid, so it becomes a platform - and it snaps
    /// back up to its perch whenever the level resets.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class FallingBlockTrap : MonoBehaviour, ITrapResettable
    {
        [SerializeField] private Transform block;
        [SerializeField] private BoxCollider2D blockCollider;
        [SerializeField] private Hazard2D blockHazard;
        [SerializeField] private float delay = 0.1f;
        [SerializeField] private float gravity = 34f;
        [SerializeField] private float restWorldY;

        // Serialized at build time so a reset works in edit mode too, not only after Awake.
        [SerializeField] private Vector3 parkedLocalPosition;
        [SerializeField] private bool parkedCached;

        private bool fired;

        public void Configure(
            Transform blockTransform,
            BoxCollider2D collider,
            Hazard2D hazard,
            float restY)
        {
            block = blockTransform;
            blockCollider = collider;
            blockHazard = hazard;
            restWorldY = restY;

            if (blockTransform != null)
            {
                parkedLocalPosition = blockTransform.localPosition;
                parkedCached = true;
            }
        }

        private void Reset()
        {
            GetComponent<BoxCollider2D>().isTrigger = true;
        }

        private void Awake()
        {
            if (!parkedCached && block != null)
            {
                parkedLocalPosition = block.localPosition;
                parkedCached = true;
            }

            ResetTrap();
        }

        public void ResetTrap()
        {
            StopAllCoroutines();
            fired = false;

            if (block != null && parkedCached)
                block.localPosition = parkedLocalPosition;
            if (blockCollider != null)
                blockCollider.isTrigger = true;
            if (blockHazard != null)
                blockHazard.enabled = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (fired || other == null)
                return;
            if (other.GetComponentInParent<PlayerHealth>() == null)
                return;

            fired = true;
            StartCoroutine(Fall());
        }

        private IEnumerator Fall()
        {
            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            float speed = 0f;
            float guard = 0f;

            while (block != null && block.position.y > restWorldY && guard < 4f)
            {
                speed += gravity * Time.deltaTime;
                block.position += Vector3.down * (speed * Time.deltaTime);
                guard += Time.deltaTime;
                yield return null;
            }

            if (block != null)
                block.position = new Vector3(block.position.x, restWorldY, block.position.z);

            // Landed: stop being lethal, become an ordinary solid block.
            if (blockHazard != null)
                blockHazard.enabled = false;
            if (blockCollider != null)
                blockCollider.isTrigger = false;
        }
    }
}
