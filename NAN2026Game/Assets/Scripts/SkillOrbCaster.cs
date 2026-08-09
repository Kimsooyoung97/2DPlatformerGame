using UnityEngine;
using UnityEngine.InputSystem;

namespace NAN2026
{
    // 7번 키: 회전하는 원형 투사체(나선환). 적중 시 대미지 후 소멸.
    public class SkillOrbCaster : MonoBehaviour
    {
        public SkillSlotConfig config;
        public Sprite orbSprite;      // 단일 이미지(폴백)
        public Sprite[] orbFrames;    // 나선환 프레임 시트
        public float spinSpeed = 360f;
        private float lastCast = -999f;
        private PlayerMana mana;
        private SpriteRenderer sr;

        private void Awake()
        {
            mana = GetComponent<PlayerMana>();
            sr = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            var kb = PlayerController2D.InputLocked ? null : Keyboard.current;
            if (kb == null || !kb.digit3Key.wasPressedThisFrame) return;
            bool hasFrames = orbFrames != null && orbFrames.Length > 0;
            if (config == null || (orbSprite == null && !hasFrames)) return;
            if (!SkillGate.IsUnlocked(2)) return;              // 세 번째 아이콘 필요
            if (Time.time - lastCast < config.cooldown) return;
            if (mana != null && !mana.TryUseMp(config.mpCost)) return;
            lastCast = Time.time;
            SkillGate.Report(2, config.cooldown);
            float dir = (sr != null && sr.flipX) ? -1f : 1f;
            var go = new GameObject("SkillOrb");
            go.transform.position = transform.position + new Vector3(dir * config.spawnForward, config.spawnHeight, 0f);
            go.transform.localScale = Vector3.one * config.scale;
            var osr = go.AddComponent<SpriteRenderer>();
            osr.sprite = hasFrames ? orbFrames[0] : orbSprite;
            osr.sortingOrder = 60;
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = config.hitboxSize;
            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.useFullKinematicContacts = true; // FAIL: Kinematic 트리거 접촉 보장
            var fly = go.AddComponent<SkillOrbFlight>();
            fly.Init(new Vector2(dir * config.speed, 0f), config.life, config.damage, spinSpeed, hasFrames ? orbFrames : null, config.fps);
        }
    }

    // 비행·명중 처리 (보스 폴백 포함 — 미노/데몬/일반 몬스터)
    public class SkillOrbFlight : MonoBehaviour
    {
        private Vector2 vel;
        private float life, spin, animT, fps;
        private int damage;
        private Sprite[] frames;
        private SpriteRenderer sr;

        public void Init(Vector2 velocity, float lifeSec, int dmg, float spinSpeed, Sprite[] animFrames, float animFps)
        {
            vel = velocity; life = lifeSec; damage = dmg; spin = spinSpeed;
            frames = animFrames; fps = animFps;
            sr = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            transform.position += (Vector3)(vel * Time.deltaTime);
            if (frames != null && frames.Length > 0 && sr != null)
            {
                animT += Time.deltaTime * fps;
                sr.sprite = frames[((int)animT) % frames.Length];
            }
            else transform.Rotate(0f, 0f, spin * Time.deltaTime); // 단일 이미지면 회전으로 대체
            life -= Time.deltaTime;
            if (life <= 0f) Destroy(gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other == null) return;
            if (other.GetComponentInParent<PlayerHealth>() != null) return; // 시전자 무시
            var mino = other.GetComponentInParent<MinoBoss>();
            if (mino != null) { mino.TakeDamage(damage); Destroy(gameObject); return; }
            var demon = other.GetComponentInParent<DemonBoss>();
            if (demon != null) { demon.TakeDamage(damage); Destroy(gameObject); return; }
            // 신규 잡몹(EnemyBase 등) 공통 창구 — FAIL#24
            var dmgTarget = other.GetComponentInParent<IPlayerDamageable>();
            if (dmgTarget != null) { dmgTarget.TakeDamage(damage); Destroy(gameObject); return; }

            var mon = other.GetComponentInParent<NHNDemo.MonsterHealth>();
            if (mon != null)
            {
                // SendMessage 는 인자를 1개만 넘긴다. TakeDamage(int, Vector2) 는 2개라
                // "Failed to call function" 예외가 나고 그 프레임 로직이 끊겼다.
                // EffectProjectile 과 동일하게 직접 호출한다.
                mon.TakeDamage(damage, new Vector2(Mathf.Sign(vel.x), 0f));
                Destroy(gameObject);
                return;
            }
            if (other.GetComponent<UnityEngine.Tilemaps.TilemapCollider2D>() != null || other.GetComponent<CompositeCollider2D>() != null)
                Destroy(gameObject); // 벽 충돌 소멸
        }
    }
}
