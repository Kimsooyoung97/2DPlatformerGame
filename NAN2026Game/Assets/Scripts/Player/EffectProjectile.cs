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
        // 적을 맞힌 뒤에도 계속 날아갈지(관통, Skill2) 아니면 그 자리에서 사라질지(단일
        // 타격, Skill1). 기존 사용처(Slash/Combo2 콤보 등)의 클리브 동작을 안 깨려고
        // 기본값은 true(관통)로 둔다 — Launch를 호출하는 쪽에서 명시적으로 지정한다.
        private bool piercing = true;

        public void Launch(float direction, float moveSpeed, float life, Sprite[] animFrames, float fps,
            int hitDamage, Vector2 hitboxDimensions, bool piercingHit = true)
        {
            dir = direction;
            speed = moveSpeed;
            lifetime = life;
            frames = animFrames;
            frameRate = fps;
            damage = hitDamage;
            hitboxSize = hitboxDimensions;
            piercing = piercingHit;

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

            // 트리거끼리는 최소 한쪽이 non-static Rigidbody2D를 가져야 접촉 이벤트가 발생한다
            // (땅/벽 같은 정적 콜라이더는 보통 Rigidbody2D가 없다 — FAIL.md #6).
            if (GetComponent<Rigidbody2D>() == null)
            {
                Rigidbody2D rb = gameObject.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.gravityScale = 0f;
            }
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
            if (other == null) return;

            // 자기 자신(플레이어)은 절대 맞지 않는다.
            if (other.GetComponentInParent<PlayerHealth>() != null)
                return;

            var exec = other.GetComponentInParent<NAN2026.Showroom.ExecutionerBoss>();
            if (exec != null) { exec.TakeHit(damage, dir); return; }

            var minoHit0 = other.GetComponentInParent<NAN2026.SecondSceneBoss>();
            if (minoHit0 != null) { minoHit0.TakeDamage(damage); return; }
            var demonHit = other.GetComponentInParent<NAN2026.DemonBoss>();
            if (demonHit != null) { demonHit.TakeDamage(damage); return; } // Scene4 데몬 — 미등재 시 무음 통과
            var dmgTarget = other.GetComponentInParent<NAN2026.IPlayerDamageable>();
            if (dmgTarget != null) { dmgTarget.TakeDamage(damage); return; } // 신규 잡몹 공통 창구(FAIL#24)
            NHNDemo.MonsterHealth monster = other.GetComponentInParent<NHNDemo.MonsterHealth>();
            if (monster != null)
            {
                // 스윙 하나로 여러 적을 동시에 맞힐 수 있다(클리브, 관통 시). 같은 적을
                // 두 번 맞히는 일은 OnTriggerEnter2D가 겹침 시작 시 한 번만 호출되므로
                // 자연히 방지된다.
                if (damage > 0)
                    monster.TakeDamage(damage, Vector2.right * dir);

                if (!piercing)
                    Destroy(gameObject); // 단일 타격(Skill1): 첫 적을 맞히면 즉시 사라짐
                return;
            }

            // 플레이어도 몬스터도 아닌, 트리거가 아닌(=단단한) 콜라이더는 땅·벽으로 간주해
            // 관통 여부와 무관하게 즉시 사라진다.
            if (!other.isTrigger)
            {
                Destroy(gameObject);
            }
        }
    }
}
