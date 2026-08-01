using System.Collections;
using UnityEngine;

namespace NAN2026.Showroom
{
    /// <summary>
    /// Cat-Mario style trap: innocent looking flat ground that shoots spikes up
    /// the moment the player walks over it. Resets on death so every attempt
    /// starts from the same level - that is what makes it learnable.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class PopUpSpikeTrap : MonoBehaviour, ITrapResettable
    {
        [SerializeField] private Transform spike;
        [SerializeField] private BoxCollider2D spikeHitbox;
        [SerializeField] private SpriteRenderer spikeRenderer;
        [SerializeField] private float hiddenLocalY = -0.5f;
        [SerializeField] private float raisedLocalY = 0.5f;
        [SerializeField] private float delay = 0.04f;
        [SerializeField] private float riseTime = 0.09f;
        [SerializeField] private int raisedSortingOrder = 6;

        private int hiddenSortingOrder = -2;
        private bool fired;

        public void Configure(
            Transform spikeTransform,
            BoxCollider2D hitbox,
            SpriteRenderer renderer,
            float hiddenY,
            float raisedY)
        {
            spike = spikeTransform;
            spikeHitbox = hitbox;
            spikeRenderer = renderer;
            hiddenLocalY = hiddenY;
            raisedLocalY = raisedY;
        }

        private void Reset()
        {
            GetComponent<BoxCollider2D>().isTrigger = true;
        }

        private void Awake()
        {
            if (spikeRenderer != null)
                hiddenSortingOrder = spikeRenderer.sortingOrder;

            ResetTrap();
        }

        public void ResetTrap()
        {
            StopAllCoroutines();
            fired = false;

            if (spike != null)
                spike.localPosition = new Vector3(0f, hiddenLocalY, 0f);
            if (spikeRenderer != null)
                spikeRenderer.sortingOrder = hiddenSortingOrder;
            if (spikeHitbox != null)
                spikeHitbox.enabled = false;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (fired || other == null)
                return;
            if (other.GetComponentInParent<PlayerHealth>() == null)
                return;

            fired = true;
            StartCoroutine(Rise());
        }

        private IEnumerator Rise()
        {
            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            if (spikeRenderer != null)
                spikeRenderer.sortingOrder = raisedSortingOrder;

            float elapsed = 0f;
            while (elapsed < riseTime)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / riseTime);
                if (spike != null)
                    spike.localPosition = new Vector3(0f, Mathf.Lerp(hiddenLocalY, raisedLocalY, t), 0f);
                yield return null;
            }

            if (spike != null)
                spike.localPosition = new Vector3(0f, raisedLocalY, 0f);
            if (spikeHitbox != null)
                spikeHitbox.enabled = true;
        }
    }
}
