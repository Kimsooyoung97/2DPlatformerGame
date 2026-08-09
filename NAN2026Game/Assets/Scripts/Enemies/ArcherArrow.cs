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
        private Transform player;
        private Component parryController;
        private System.Reflection.MethodInfo tryParry;

        public void Launch(Sprite spr, Vector2 direction, float spd, float lifeSec, int damage, int sortingOrder, SpikeBallConfig clashConfig = null)
        {
            dir = direction.normalized; speed = spd; life = lifeSec; dmg = damage; clash = clashConfig;
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
            var sr = gameObject.AddComponent<SpriteRenderer>();
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

        private void Update()
        {
            transform.position += (Vector3)(dir * speed * Time.deltaTime);
            life -= Time.deltaTime;
            if (life <= 0f) Destroy(gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other == null) return;
            if (other.GetComponentInParent<ArcherEnemy>() != null) return;   // 시전자 무시
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
