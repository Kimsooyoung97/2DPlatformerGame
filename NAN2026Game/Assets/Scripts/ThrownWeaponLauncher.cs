using UnityEngine;
using System.Collections;
using UnityEngine.Rendering.Universal;

namespace NAN2026
{
    // 투척 발사기: 접근 시 가로로 하나씩 발사. 전조음 없음(발사음만), 투사체는 발광
    public class ThrownWeaponLauncher : MonoBehaviour
    {
        public ThrownTrapConfig config;
        public ThrownKind kind;
        public Sprite projectileSprite;
        public int dir = -1;
        public bool dropFromCeiling = true; // 천장 낙하 모드
        private Transform player;
        private float nextReady;
        private static int waveBudget; // 이번 파도 허용 발수(1~2)
        private static int reserved;   // 전조 중 예약 수

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStaticsOnPlay() { waveBudget = 0; reserved = 0; } // DisableDomainReload 대응
        private AudioSource src;

        private void Start()
        {
            var p = PlayerLocator.Find();
            if (p != null) player = p.transform;
            src = gameObject.AddComponent<AudioSource>();
            src.spatialBlend = 0f; src.volume = 0.85f;
        }

        private void Update()
        {
            if (config == null || player == null || Time.time < nextReady) return;
            int inFlight = ThrownProjectile.Alive + reserved;
            if (inFlight == 0) waveBudget = Random.value < config.twinChance ? 2 : 1; // 새 파도 예산 추첨
            if (inFlight >= waveBudget) return;
            if (Mathf.Abs(player.position.x - transform.position.x) > config.aggroX) return;
            if (dropFromCeiling && player.position.y > transform.position.y) return; // 위층 통행 중엔 미가동
            float cd = kind == ThrownKind.Arrow ? config.arrowCooldown : kind == ThrownKind.Shuriken ? config.shurikenCooldown : config.axeCooldown;
            nextReady = Time.time + cd;
            reserved++;
            StartCoroutine(FireSeq());
        }

        private IEnumerator FireSeq()
        {
            yield return new WaitForSeconds(config.telegraphTime);
            Fire();
            reserved--;
        }

        private void Fire()
        {
            if (config.sndFire != null) src.PlayOneShot(config.sndFire);
            var go = new GameObject(kind + "_투사체");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = projectileSprite;
            sr.sharedMaterial = NAN2026.FxUnlit.Mat;
            sr.color = config.glowColor;
            sr.sortingOrder = 45;
            go.transform.position = transform.position + (dropFromCeiling ? new Vector3(0f, -0.7f, 0f) : new Vector3(dir * 0.6f, 0.3f, 0f));
            var lt = go.AddComponent<Light2D>();
            lt.lightType = Light2D.LightType.Point;
            lt.intensity = config.glowIntensity;
            lt.pointLightOuterRadius = config.glowRadius;
            lt.color = config.glowColor;
            var pr = go.AddComponent<ThrownProjectile>();
            pr.config = config; pr.kind = kind; pr.launcher = gameObject;
            // 유도형: 발사 순간 플레이어를 조준해 돌진 (기존 돌진 트랩과 동일 문법)
            Vector2 aim = player != null ? ((Vector2)player.position - (Vector2)go.transform.position).normalized : Vector2.down;
            pr.Launch(aim * config.homingSpeed);
        }
    }
}
