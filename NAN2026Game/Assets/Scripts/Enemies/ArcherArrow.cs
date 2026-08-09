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

        public void Launch(Sprite spr, Vector2 direction, float spd, float lifeSec, int damage, int sortingOrder, SpikeBallConfig clashConfig = null,
                           bool reflectOnParry = false, float reflectSpeedMul = 1.4f, float reflectMinLife = 1.5f)
        {
            dir = direction.normalized; speed = spd; life = lifeSec; dmg = damage; clash = clashConfig;
            canReflect = reflectOnParry; reflectMul = reflectSpeedMul; reflectMin = reflectMinLife;
            var pgo = PlayerLocator.Find();
            if (pgo != null)
            {
                player = pgo.transform;
                foreach (var mb in pgo.GetComponents<MonoBehaviour>())
                {
                    var m = mb.GetType().GetMethod("TryParry",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (m != null) { parryController = mb; tryParry = m; break; }
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
            if (life <= 0f) Destroy(gameObject);
        }

        // 지척에서 패링하면 반사 시점에 이미 시전자와 겹쳐 있어 Enter 가 다시 오지 않는다.
        // 되돌아가는 동안만 Stay 로도 판정한다.
        private void OnTriggerStay2D(Collider2D other) { if (reflected) OnTriggerEnter2D(other); }

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
                bool parried = false;
                if (parryController != null && tryParry != null)
                {
                    object r = tryParry.Invoke(parryController, new object[] { gameObject });
                    parried = r is bool && (bool)r;
                }
                if (parried)
                {
                    if (clash != null && player != null)
                        ParryClashFx.Play((transform.position + player.position) * 0.5f + Vector3.up * 0.8f, clash);
                    PlayerMana.RewardParry(player);
                    if (canReflect) { Reflect(); return; }   // 파괴하지 않고 되돌린다
                }
                else ph.SendMessage("TakeDamage", (float)dmg, SendMessageOptions.DontRequireReceiver);
                Destroy(gameObject);
                return;
            }
            if (other.GetComponent<UnityEngine.Tilemaps.TilemapCollider2D>() != null || other.GetComponent<CompositeCollider2D>() != null)
                Destroy(gameObject);
        }
    }
}
