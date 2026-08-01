using UnityEngine;

namespace NAN2026.Showroom
{
    /// <summary>
    /// The crescent of sword energy the player throws. Plays through its frames while it
    /// travels, cuts down monsters, and swats the boss orbs out of the air.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class SlashProjectile : MonoBehaviour
    {
        [SerializeField] private Sprite[] frames;
        [SerializeField] private float framesPerSecond = 14f;
        [SerializeField] private float speed = 11f;
        [SerializeField] private int damage = 1;
        [SerializeField] private float lifetime = 1.4f;
        [SerializeField] private bool loopFrames = true;

        private SpriteRenderer spriteRenderer;
        private float direction = 1f;
        private float age;

        public void Launch(Sprite[] animationFrames, float facing, float moveSpeed, int hitDamage, float fps)
        {
            frames = animationFrames;
            direction = Mathf.Sign(facing == 0f ? 1f : facing);
            speed = moveSpeed;
            damage = hitDamage;
            framesPerSecond = fps;

            if (spriteRenderer == null)
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = direction < 0f;
                if (frames != null && frames.Length > 0)
                    spriteRenderer.sprite = frames[0];
            }
        }

        private void Awake()
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            GetComponent<BoxCollider2D>().isTrigger = true;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            age += dt;

            transform.position += Vector3.right * (direction * speed * dt);

            if (frames != null && frames.Length > 0 && spriteRenderer != null)
            {
                int index = Mathf.FloorToInt(age * framesPerSecond);
                if (loopFrames)
                {
                    index %= frames.Length;
                }
                else if (index >= frames.Length)
                {
                    Destroy(gameObject);
                    return;
                }
                spriteRenderer.sprite = frames[index];
            }

            if (age >= lifetime)
                Destroy(gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other == null)
                return;

            // Never hit the person who threw it.
            if (other.GetComponentInParent<PlayerHealth>() != null)
                return;

            NHNDemo.MonsterHealth monster = other.GetComponentInParent<NHNDemo.MonsterHealth>();
            if (monster != null)
            {
                monster.TakeDamage(damage, Vector2.right * direction);
                return;
            }

            // Slice incoming boss orbs out of the air.
            OrbProjectile orb = other.GetComponentInParent<OrbProjectile>();
            if (orb != null)
                Destroy(orb.gameObject);
        }
    }
}
