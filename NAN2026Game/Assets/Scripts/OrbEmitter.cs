using System.Collections.Generic;
using UnityEngine;

namespace NAN2026.Showroom
{
    /// <summary>
    /// Boss stand-in. Sits in the level and lobs orbs at the player whenever they come
    /// within range, so the parry timing has something to practise against.
    /// Clears its live shots whenever the level resets.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class OrbEmitter : MonoBehaviour, ITrapResettable
    {
        [SerializeField] private Sprite orbSprite;
        [SerializeField] private float fireInterval = 1.5f;
        [SerializeField] private float orbSpeed = 6f;
        [SerializeField] private float activationRange = 14f;
        [SerializeField] private float orbLifetime = 6f;
        [SerializeField] private float orbScale = 1f;
        [SerializeField] private float aimHeight = 0.72f;

        private readonly List<GameObject> live = new List<GameObject>();
        private PlayerHealth player;
        private float nextFire;

        public void Configure(Sprite sprite, float interval, float speed, float range)
        {
            orbSprite = sprite;
            fireInterval = interval;
            orbSpeed = speed;
            activationRange = range;
        }

        private void Awake()
        {
            ResetTrap();
        }

        public void ResetTrap()
        {
            for (int i = 0; i < live.Count; i++)
                if (live[i] != null)
                    Destroy(live[i]);
            live.Clear();

            nextFire = Time.time + fireInterval;
        }

        private void Update()
        {
            if (player == null)
            {
                player = FindFirstObjectByType<PlayerHealth>();
                if (player == null)
                    return;
            }

            live.RemoveAll(delegate (GameObject orb) { return orb == null; });

            Vector3 target = player.transform.position + Vector3.up * aimHeight;
            if (Vector3.Distance(target, transform.position) > activationRange)
                return;

            if (Time.time < nextFire)
                return;

            nextFire = Time.time + fireInterval;
            Fire(target);
        }

        private void Fire(Vector3 target)
        {
            if (orbSprite == null)
                return;

            GameObject orb = new GameObject("Orb");
            orb.transform.position = transform.position;
            orb.transform.localScale = Vector3.one * orbScale;

            SpriteRenderer renderer = orb.AddComponent<SpriteRenderer>(); renderer.sharedMaterial = NAN2026.FxUnlit.Mat;
            renderer.sprite = orbSprite;
            renderer.sortingOrder = 12;

            CircleCollider2D collider = orb.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.34f;

            OrbProjectile projectile = orb.AddComponent<OrbProjectile>();
            Vector2 direction = (target - transform.position).normalized;
            projectile.Launch(direction * orbSpeed, orbLifetime);

            live.Add(orb);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.9f, 0.4f, 0.9f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, activationRange);
        }
    }
}
