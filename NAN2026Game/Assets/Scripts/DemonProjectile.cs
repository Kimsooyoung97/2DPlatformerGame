using UnityEngine;

namespace NAN2026
{
    // 데몬 투사체: 비행(1~3프레임 루프) → 플레이어 명중/패링/벽·계단 충돌 시 폭발(잔여 프레임) 후 소멸
    // 패링 성공 시 소멸 대신 반사(Reflect)되어 발사자(owner)에게 되돌아가 데미지를 입힌다.
    public class DemonProjectile : MonoBehaviour
    {
        private Sprite[] fly, boom;
        private float speed, fps, dmg;
        private Vector2 dir;
        private float t, life;
        private bool booming;
        private bool isReflected;
        private SpriteRenderer sr;
        private Transform player;
        private Component controller;
        private System.Reflection.MethodInfo tryParry;
        private SpikeBallConfig clashCfg;
        private DemonBoss owner;

        public void Launch(Sprite[] flyF, Sprite[] boomF, Vector2 d, float spd, float animFps, int damage, float lifeSec, SpikeBallConfig clash, DemonBoss ownerBoss)
        {
            fly = flyF; boom = boomF; dir = d.normalized; speed = spd; fps = animFps; dmg = damage; life = lifeSec; clashCfg = clash; owner = ownerBoss;
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

            var hitBoss = other.GetComponentInParent<DemonBoss>();
            if (hitBoss != null)
            {
                // 반사된 투사체가 발사자 본인에게 맞으면 데미지. 반사 전엔 시전자 자신이므로 항상 무시.
                if (isReflected && hitBoss == owner)
                {
                    hitBoss.TakeDamage(Mathf.Max(1, Mathf.RoundToInt(dmg)));
                    Boom();
                }
                return;
            }

            if (isReflected) return; // 반사된 뒤엔 플레이어와 다시 상호작용하지 않음(재패링/피격 없음)

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
                    if (owner != null) owner.RegisterParrySuccess(); // 근접 패링과 같은 그로기 카운터 공유
                    Reflect();
                    return;
                }
                ph.SendMessage("TakeDamage", (float)dmg, SendMessageOptions.DontRequireReceiver);
                Boom();
                return;
            }
            // 벽·계단(타일맵) 충돌
            if (other.GetComponent<UnityEngine.Tilemaps.TilemapCollider2D>() != null || other.GetComponent<CompositeCollider2D>() != null)
                Boom();
        }

        private void Reflect()
        {
            isReflected = true;
            dir = -dir;
            speed *= 1.3f;              // 반사되면 살짝 더 빠르게 되돌아감
            life = Mathf.Max(life, 3f); // 보스까지 되돌아갈 시간 확보
            sr.flipX = dir.x < 0f;
        }

        private void Boom()
        {
            booming = true; t = 0f; speed = 0f;
            sr.flipX = false;
        }
    }
}