using UnityEngine;
using NAN2026.Showroom;

namespace NAN2026
{
    public class EffectProjectile : MonoBehaviour
    {
        [SerializeField] private Sprite[] frames;
        [SerializeField] private float frameRate;
        [SerializeField] private float speed;
        [SerializeField] private float lifetime;
        [SerializeField] private int damage;
        [SerializeField] private Vector2 hitboxSize;

        private SpriteRenderer sr;
        private BoxCollider2D hitbox;
        private float age;
        private float dir = 1f;

        public void Launch(float direction, float moveSpeed, float life, Sprite[] animFrames, float fps,
            int hitDamage, Vector2 hitboxDimensions)
        {
            dir = direction;
            speed = moveSpeed;
            lifetime = life;
            frames = animFrames;
            frameRate = fps;
            damage = hitDamage;
            hitboxSize = hitboxDimensions;

            var s = GetComponent<SpriteRenderer>();
            s.flipX = direction < 0f;

            if (hitbox != null)
                hitbox.size = hitboxSize;
        }

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();

            hitbox = GetComponent<BoxCollider2D>();
            if (hitbox == null) hitbox = gameObject.AddComponent<BoxCollider2D>();
            hitbox.isTrigger = true;
            if (hitboxSize != Vector2.zero) hitbox.size = hitboxSize;
        }

        private void Update()
        {
            age += Time.deltaTime;
            if (age >= lifetime) { Destroy(gameObject); return; }
            transform.position += new Vector3(dir * speed * Time.deltaTime, 0f, 0f);
            if (frames != null && frames.Length > 0 && frameRate > 0f)
            {
                int idx = (int)(age * frameRate) % frames.Length;
                sr.sprite = frames[idx];
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // 스윙 하나로 여러 적을 동시에 맞힐 수 있다(클리브). 같은 적을 두 번 맞히는 일은
            // OnTriggerEnter2D가 겹침 시작 시 한 번만 호출되므로 자연히 방지된다.
            if (other == null || damage <= 0)
                return;

            // 자기 자신(플레이어)은 절대 맞지 않는다.
            if (other.GetComponentInParent<PlayerHealth>() != null)
                return;

            NHNDemo.MonsterHealth monster = other.GetComponentInParent<NHNDemo.MonsterHealth>();
            if (monster == null)
                return;

            monster.TakeDamage(damage, Vector2.right * dir);
        }
    }
}
