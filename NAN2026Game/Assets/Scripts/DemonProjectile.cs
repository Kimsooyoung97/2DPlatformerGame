using UnityEngine;

namespace NAN2026
{
    // 데몬 투사체: 비행(1~3프레임 루프) → 플레이어 명중/패링/벽·계단 충돌 시 폭발(잔여 프레임) 후 소멸
    public class DemonProjectile : MonoBehaviour
    {
        private Sprite[] fly, boom;
        private float speed, fps, dmg;
        private Vector2 dir;
        private float t, life;
        private bool booming;
        private SpriteRenderer sr;
        private Transform player;
        private Component controller;
        private System.Reflection.MethodInfo tryParry;
        private SpikeBallConfig clashCfg;

        public void Launch(Sprite[] flyF, Sprite[] boomF, Vector2 d, float spd, float animFps, int damage, float lifeSec, SpikeBallConfig clash)
        {
            fly = flyF; boom = boomF; dir = d.normalized; speed = spd; fps = animFps; dmg = damage; life = lifeSec; clashCfg = clash;
            sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 60;
            sr.flipX = dir.x < 0f;
            var col = gameObject.AddComponent<CircleCollider2D>();
            col.isTrigger = true; col.radius = 0.45f;
            var rb = gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.useFullKinematicContacts = true; // FAIL: Kinematic 트리거 이벤트 보장
            var pgo = PlayerLocator.Find();
            if (pgo != null)
            {
                player = pgo.transform;
                foreach (var mb in pgo.GetComponents<MonoBehaviour>())
                {
                    var m = mb.GetType().GetMethod("TryParry");
                    if (m != null) { controller = mb; tryParry = m; break; }
                }
            }
        }

        void Update()
        {
            t += Time.deltaTime;
            if (booming)
            {
                int bi = (int)(t * fps);
                if (bi >= boom.Length) { Destroy(gameObject); return; }
                sr.sprite = boom[bi];
                return;
            }
            transform.position += (Vector3)(dir * speed * Time.deltaTime);
            sr.sprite = fly[(int)(t * fps) % fly.Length];
            life -= Time.deltaTime;
            if (life <= 0f) Boom();
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (booming || other == null) return;
            if (other.GetComponentInParent<DemonBoss>() != null) return; // 시전자 무시
            var ph = other.GetComponentInParent<PlayerHealth>();
            if (ph != null)
            {
                bool parried = false;
                if (controller != null && tryParry != null)
                {
                    object r = tryParry.Invoke(controller, new object[] { gameObject });
                    parried = r is bool && (bool)r;
                }
                if (parried)
                {
                    if (clashCfg != null && player != null)
                        ParryClashFx.Play((transform.position + player.position) * 0.5f + Vector3.up * 0.8f, clashCfg);
                    PlayerMana.RewardParry(player);
                }
                else ph.SendMessage("TakeDamage", (float)dmg, SendMessageOptions.DontRequireReceiver);
                Boom();
                return;
            }
            // 벽·계단(타일맵) 충돌
            if (other.GetComponent<UnityEngine.Tilemaps.TilemapCollider2D>() != null || other.GetComponent<CompositeCollider2D>() != null)
                Boom();
        }

        private void Boom()
        {
            booming = true; t = 0f; speed = 0f;
            sr.flipX = false;
        }
    }
}
