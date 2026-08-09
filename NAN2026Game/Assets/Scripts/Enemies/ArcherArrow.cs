using UnityEngine;

namespace NAN2026
{
    /// 아처가 쏘는 화살. 수평 직진, 플레이어 명중 또는 타일맵 충돌 시 소멸.
    public class ArcherArrow : MonoBehaviour
    {
        private float speed, life;
        private int dmg;
        private Vector2 dir;
        private SpikeBallConfig clash;
        private SpriteRenderer sr;
        private bool reflected;                 // 패링으로 되돌아가는 중
        private bool canReflect;
        private float reflectMul, reflectMin;
        private Transform player;
        private Component parryController;
        private System.Reflection.MethodInfo tryParry;
        private System.Reflection.MethodInfo parryActive;   // IsParryWindowActive() — 방향 검사 없는 판정
        private float parryZone, parryBodyHeight;

        public void Launch(Sprite spr, Vector2 direction, float spd, float lifeSec, int damage, int sortingOrder, SpikeBallConfig clashConfig = null,
                           bool reflectOnParry = false, float reflectSpeedMul = 1.4f, float reflectMinLife = 1.5f,
                           float arrowParryZone = 0f, float arrowParryHeight = 2f)
        {
            dir = direction.normalized; speed = spd; life = lifeSec; dmg = damage; clash = clashConfig;
            canReflect = reflectOnParry; reflectMul = reflectSpeedMul; reflectMin = reflectMinLife;
            parryZone = arrowParryZone; parryBodyHeight = arrowParryHeight;
            var pgo = PlayerLocator.Find();
            if (pgo != null)
            {
                player = pgo.transform;
                foreach (var mb in pgo.GetComponents<MonoBehaviour>())
                {
                    var m = mb.GetType().GetMethod("TryParry",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (m != null)
                    {
                        parryController = mb; tryParry = m;
                        // 화살도 기사와 같은 계약을 쓴다: 방향을 보지 않는 IsParryWindowActive 우선
                        parryActive = mb.GetType().GetMethod("IsParryWindowActive",
                            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        break;
                    }
                }
            }
            sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = spr;
            sr.sortingOrder = sortingOrder;
            sr.flipX = dir.x < 0f;
            var col = gameObject.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            if (spr != null) col.size = spr.bounds.size;
            var rb = gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.useFullKinematicContacts = true; // FAIL#6
        }

        /// 전방위 패링 판정. IsParryWindowActive 가 있으면 방향을 보지 않는다(등 뒤 화살도 인정).
        private bool TryParryNow()
        {
            if (parryController == null) return false;
            if (parryActive != null)
            {
                object r = parryActive.Invoke(parryController, null);
                return r is bool && (bool)r;
            }
            if (tryParry != null)
            {
                object r = tryParry.Invoke(parryController, new object[] { gameObject });
                return r is bool && (bool)r;
            }
            return false;
        }

        /// 패링 성공 시 공통 처리. 접근 존에서 걸리든 접촉에서 걸리든 같은 결과.
        private void OnParried()
        {
            if (clash != null && player != null)
                ParryClashFx.Play((transform.position + player.position) * 0.5f + Vector3.up * 0.8f, clash);
            PlayerMana.RewardParry(player);
            if (canReflect) { Reflect(); return; }
            Destroy(gameObject);
        }

        /// 닿기 전 parryZone 안에서는 매 프레임 패링을 접수한다.
        /// OnTriggerEnter2D 한 프레임에만 묻던 것을 '접근 구간 내내' 로 넓힌 것 —
        /// 기사에 적용한 '타격창 끝 확정' 과 같은 발상이다.
        private void TryEarlyParry()
        {
            if (player == null) return;
            float gap = player.position.x - transform.position.x;
            if (gap * dir.x <= 0f) return;                       // 이미 지나쳤다
            if (Mathf.Abs(gap) > parryZone) return;
            if (!NAN2026.Core.EnemyStateLogic.WithinBodyHeight(transform.position.y, player.position.y, parryBodyHeight)) return;
            if (!TryParryNow()) return;
            OnParried();
        }

        /// 패링 성공 — 온 길로 되돌려 쏜 적에게 꽂는다.
        private void Reflect()
        {
            reflected = true;
            dir = new Vector2(-dir.x, 0f).normalized;
            speed = NAN2026.Core.EnemyStateLogic.ReflectSpeed(speed, reflectMul);
            life = NAN2026.Core.EnemyStateLogic.ReflectLife(life, reflectMin);
            if (sr != null) sr.flipX = dir.x < 0f;
        }

        private void Update()
        {
            transform.position += (Vector3)(dir * speed * Time.deltaTime);
            life -= Time.deltaTime;
            if (life <= 0f) { Destroy(gameObject); return; }
            if (!reflected && parryZone > 0f) TryEarlyParry();
        }

        // Enter 는 겹침 시작 한 프레임뿐이다. 겹쳐 있는 0.18초 동안 패링을 눌러도 인정되도록,
        // 그리고 지척 패링 시 반사 화살이 시전자와 이미 겹쳐 있어 Enter 가 안 오는 것도 함께 해소한다.
        private void OnTriggerStay2D(Collider2D other) { OnTriggerEnter2D(other); }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other == null) return;
            // 반사 전에는 시전자를 무시하지만, 되돌아올 때는 반드시 맞아야 한다
            if (!reflected && other.GetComponentInParent<ArcherEnemy>() != null) return;

            if (reflected)
            {
                var foe = other.GetComponentInParent<EnemyBase>();
                if (foe != null) { foe.TakeDamage(dmg); Destroy(gameObject); return; }
                if (other.GetComponent<UnityEngine.Tilemaps.TilemapCollider2D>() != null || other.GetComponent<CompositeCollider2D>() != null)
                    Destroy(gameObject);
                return;   // 되돌아가는 화살은 플레이어를 다시 때리지 않는다
            }

            var ph = other.GetComponentInParent<PlayerHealth>();
            if (ph != null)
            {
                if (TryParryNow()) { OnParried(); return; }
                ph.SendMessage("TakeDamage", (float)dmg, SendMessageOptions.DontRequireReceiver);
                Destroy(gameObject);
                return;
            }
            if (other.GetComponent<UnityEngine.Tilemaps.TilemapCollider2D>() != null || other.GetComponent<CompositeCollider2D>() != null)
                Destroy(gameObject);
        }
    }
}
