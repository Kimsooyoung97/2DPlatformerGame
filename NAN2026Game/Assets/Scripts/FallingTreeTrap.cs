using System.Collections;
using UnityEngine;

namespace NAN2026.Showroom
{
    /// <summary>
    /// A tree that has been standing there as harmless scenery the whole level, until
    /// you walk close enough - then it topples straight at you and crushes you.
    ///
    /// It rotates around a pivot placed at the trunk base, always falling toward the
    /// side the player approached from. The canopy carries the kill box, so the danger
    /// is the sweeping treetop rather than the trunk you are standing next to.
    /// Once it has landed it is just a fallen log, and it stands back up on reset.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class FallingTreeTrap : MonoBehaviour, ITrapResettable
    {
        [SerializeField] private Transform pivot;
        [SerializeField] private BoxCollider2D canopyHitbox;
        [SerializeField] private Hazard2D canopyHazard;

        [Tooltip("How far over the tree goes. 85 leaves it lying just above the ground.")]
        [SerializeField] private float fallAngle = 85f;
        [SerializeField] private float fallTime = 0.45f;
        [Tooltip("Creak before it commits - just enough for the player to notice, not to escape.")]
        [SerializeField] private float warning = 0.08f;
        [SerializeField] private float leanBack = 4f;

        private bool fired;

        public void Configure(Transform pivotTransform, BoxCollider2D hitbox, Hazard2D hazard)
        {
            pivot = pivotTransform;
            canopyHitbox = hitbox;
            canopyHazard = hazard;

            ResetTrap();
        }

        private void Reset()
        {
            GetComponent<BoxCollider2D>().isTrigger = true;
        }

        private void Awake()
        {
            ResetTrap();
        }

        public void ResetTrap()
        {
            StopAllCoroutines();
            fired = false;

            if (pivot != null)
                pivot.localRotation = Quaternion.identity;
            if (canopyHitbox != null)
                canopyHitbox.enabled = false;
            if (canopyHazard != null)
                canopyHazard.enabled = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (fired || other == null || pivot == null)
                return;

            PlayerHealth player = other.GetComponentInParent<PlayerHealth>();
            if (player == null)
                return;

            fired = true;
            StartCoroutine(Fall(player.transform.position.x));
        }

        private IEnumerator Fall(float playerX)
        {
            // Fall toward whichever side the player is on.
            float target = playerX < pivot.position.x ? fallAngle : -fallAngle;

            // Tiny lean the other way first, the way a real tree hinges before it goes.
            float elapsed = 0f;
            while (elapsed < warning)
            {
                elapsed += Time.deltaTime;
                float k = warning > 0f ? Mathf.Clamp01(elapsed / warning) : 1f;
                pivot.localRotation = Quaternion.Euler(0f, 0f, -Mathf.Sign(target) * leanBack * k);
                yield return null;
            }

            if (canopyHitbox != null)
                canopyHitbox.enabled = true;

            float start = -Mathf.Sign(target) * leanBack;
            elapsed = 0f;
            while (elapsed < fallTime)
            {
                elapsed += Time.deltaTime;
                float k = Mathf.Clamp01(elapsed / fallTime);
                float eased = k * k;                       // accelerates like real gravity
                pivot.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(start, target, eased));
                yield return null;
            }

            pivot.localRotation = Quaternion.Euler(0f, 0f, target);

            // Landed: now it is just a log lying across the ground.
            if (canopyHazard != null)
                canopyHazard.enabled = false;
        }
    }
}
