using UnityEngine;

namespace NAN2026.Showroom
{
    /// <summary>
    /// The round projectile the boss stand-in throws at you.
    /// Blocked by a held guard, sent back by a well timed parry, lethal otherwise.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CircleCollider2D))]
    public sealed class OrbProjectile : MonoBehaviour
    {
        [SerializeField] private Vector2 velocity;
        [SerializeField] private float lifetime = 6f;
        [SerializeField] private float spin = 220f;
        [SerializeField] private float reflectBoost = 1.4f;

        private SpriteRenderer spriteRenderer;
        private bool reflected;

        public bool Reflected { get { return reflected; } }

        public void Launch(Vector2 startVelocity, float life)
        {
            velocity = startVelocity;
            lifetime = life;
        }

        private void Awake()
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            GetComponent<CircleCollider2D>().isTrigger = true;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            transform.position += (Vector3)(velocity * dt);
            transform.Rotate(0f, 0f, spin * dt);

            lifetime -= dt;
            if (lifetime <= 0f)
                Destroy(gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other == null || reflected)
                return;

            PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
            if (health == null)
                return;

            PlayerParry parry = other.GetComponentInParent<PlayerParry>();

            if (parry != null && parry.ParryReady)
            {
                Reflect();
                parry.NotifyParry();
                return;
            }

            if (parry != null && parry.IsGuarding)
            {
                parry.NotifyBlock();
                Destroy(gameObject);
                return;
            }

            //health.Kill();
            Destroy(gameObject);
        }

        private void Reflect()
        {
            reflected = true;
            velocity = -velocity * reflectBoost;
            lifetime = Mathf.Max(lifetime, 3f);

            if (spriteRenderer != null)
                spriteRenderer.color = new Color(0.55f, 0.95f, 1f);

            transform.localScale *= 1.15f;
        }
    }
}
