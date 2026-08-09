using UnityEngine;
using UnityEngine.InputSystem;

namespace NAN2026
{
    // 7번 키: 회전하는 원형 투사체(나선환). 적중 시 대미지 후 소멸.
    public class SkillOrbCaster : MonoBehaviour
    {
        public SkillSlotConfig config;
        public Sprite orbSprite;
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
            if (kb == null || !kb.digit7Key.wasPressedThisFrame) return;
            if (config == null || orbSprite == null) return;
            if (Time.time - lastCast < config.cooldown) return;
            if (mana != null && !mana.TryUseMp(config.mpCost)) return;
            lastCast = Time.time;
            float dir = (sr != null && sr.flipX) ? -1f : 1f;
            var go = new GameObject("SkillOrb");
            go.transform.position = transform.position + new Vector3(dir * config.spawnForward, config.spawnHeight, 0f);
            go.transform.localScale = Vector3.one * config.scale;
            var osr = go.AddComponent<SpriteRenderer>();
            osr.sprite = orbSprite;
            osr.sortingOrder = 60;
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = config.hitboxSize;
            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.useFullKinematicContacts = true; // FAIL: Kinematic 트리거 접촉 보장
            var fly = go.AddComponent<SkillOrbFlight>();
            fly.Init(new Vector2(dir * config.speed, 0f), config.life, config.damage, spinSpeed);
        }
    }

    // 비행·명중 처리 (보스 폴백 포함 — 미노/데몬/일반 몬스터)
    public class SkillOrbFlight : MonoBehaviour
    {
        private Vector2 vel;
        private float life, spin;
        private int damage;

        public void Init(Vector2 velocity, float lifeSec, int dmg, float spinSpeed)
        { vel = velocity; life = lifeSec; damage = dmg; spin = spinSpeed; }

        private void Update()
        {
            transform.position += (Vector3)(vel * Time.deltaTime);
            transform.Rotate(0f, 0f, spin * Time.deltaTime);
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
            var mon = other.GetComponentInParent<NHNDemo.MonsterHealth>();
            if (mon != null) { mon.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver); Destroy(gameObject); return; }
            if (other.GetComponent<UnityEngine.Tilemaps.TilemapCollider2D>() != null || other.GetComponent<CompositeCollider2D>() != null)
                Destroy(gameObject); // 벽 충돌 소멸
        }
    }
}
