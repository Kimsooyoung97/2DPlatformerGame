using UnityEngine;
using NAN2026.Core;

namespace NAN2026
{
    // 상자 보상 수집 알림. DisableDomainReload 프로젝트라 static 리셋 동봉 (FAIL 규칙)
    public static class ChestRewardEvents
    {
        public static int Collected;
        public static System.Action<int> OnCollected;   // 채워진 슬롯 번호

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStaticsOnPlay() { Collected = 0; OnCollected = null; }

        public static void Report(int capacity)
        {
            int slot = ChestRewardLogic.NextSlot(Collected, capacity);
            if (slot < 0) return;
            Collected = slot + 1;
            if (OnCollected != null) OnCollected(slot);
        }
    }

    // 상자에서 떠올라 플레이어에게 흡수되는 스킬 아이콘
    public class SkillRewardFlyer : MonoBehaviour
    {
        private ChestRewardConfig cfg;
        private Transform target;
        private SpriteRenderer sr;
        private Vector3 origin;
        private float baseScale = 1f;
        private float t;
        private bool reported;

        public static void Spawn(Vector3 pos, Transform player, ChestRewardConfig config)
        {
            if (config == null || config.icon == null)
            {
                // 아이콘이 없으면 연출을 건너뛰고 슬롯만 채운다 — 보상 자체는 잃지 않는다
                if (config != null) ChestRewardEvents.Report(config.slotCapacity);
                return;
            }
            var go = new GameObject("SkillRewardFlyer");
            go.transform.position = pos;
            go.AddComponent<SkillRewardFlyer>().Init(player, config);
        }

        private void Init(Transform player, ChestRewardConfig config)
        {
            cfg = config;
            target = player;
            origin = transform.position;
            sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = cfg.icon;
            sr.color = cfg.tint;
            sr.sortingOrder = cfg.sortingOrder;
            float spriteWorld = cfg.icon.rect.width / cfg.icon.pixelsPerUnit;
            baseScale = spriteWorld > 0f ? cfg.worldSize / spriteWorld : 1f;
            transform.localScale = Vector3.one * baseScale;
        }

        private void Update()
        {
            if (cfg == null || sr == null) { Destroy(gameObject); return; }
            t += Time.deltaTime;

            int phase = ChestRewardLogic.Phase(t, cfg.riseTime, cfg.absorbTime);
            if (phase == ChestRewardLogic.PhaseRise)
            {
                transform.position = origin + Vector3.up * ChestRewardLogic.RiseOffset(t, cfg.riseTime, cfg.riseDistance);
                return;
            }

            Vector3 top = origin + Vector3.up * cfg.riseDistance;
            Vector3 dest = target != null ? target.position + Vector3.up * cfg.targetHeight : top;
            float a = ChestRewardLogic.AbsorbT(t, cfg.riseTime, cfg.absorbTime);

            transform.position = Vector3.Lerp(top, dest, ChestRewardLogic.EaseIn(a));
            transform.localScale = Vector3.one * baseScale * ChestRewardLogic.ScaleAt(a, cfg.scaleFrom, cfg.scaleTo);
            var c = sr.color;
            c.a = ChestRewardLogic.Alpha(a, cfg.fadeStart);
            sr.color = c;

            if (phase == ChestRewardLogic.PhaseDone)
            {
                if (!reported) { reported = true; ChestRewardEvents.Report(cfg.slotCapacity); }
                Destroy(gameObject);
            }
        }
    }

    /// <summary>
    /// 상자를 부수면 스킬 아이콘이 떠올라 플레이어에게 흡수된다.
    /// 피격 판정은 자식 HitBox(MonsterHealth)가 담당한다 — 기존 BOX 상자와 같은 방식.
    /// MonsterHealth 는 죽을 때 자기 GameObject 를 파괴하므로 반드시 자식에 둔다.
    /// </summary>
    [DisallowMultipleComponent]
    public class ChestSkillReward : MonoBehaviour
    {
        [Header("참조")]
        public ChestRewardConfig config;
        [Tooltip("피격 판정을 맡는 자식의 MonsterHealth. 비우면 자식에서 찾는다")]
        public NHNDemo.MonsterHealth hitBox;
        [Tooltip("부서질 때 스프라이트를 바꿀 대상. 비우면 자기 SpriteRenderer")]
        public SpriteRenderer visual;
        [Tooltip("부서진 뒤 보여줄 스프라이트. 비우면 그대로 둔다(애니메이터가 맡는 경우)")]
        public Sprite openedSprite;
        [Tooltip("아이콘이 생겨날 위치. 비우면 자기 위치")]
        public Transform spawnAnchor;
        [Tooltip("흔들 대상. 비우면 visual, 그것도 없으면 자기 자신")]
        public Transform shakeTarget;

        private Transform shaker;
        private Vector3 home;
        private bool opened;

        private void Awake()
        {
            if (visual == null) visual = GetComponent<SpriteRenderer>();
            if (hitBox == null) hitBox = GetComponentInChildren<NHNDemo.MonsterHealth>(true);

            shaker = shakeTarget != null ? shakeTarget
                   : (visual != null ? visual.transform : transform);
            home = shaker.position;

            if (hitBox == null) { Debug.LogWarning("[ChestSkillReward] HitBox 가 없습니다: " + name, this); return; }
            hitBox.OnHealthChanged += HandleHit;
            hitBox.OnDied += HandleBroken;
        }

        private void OnDestroy()
        {
            if (hitBox == null) return;
            hitBox.OnHealthChanged -= HandleHit;
            hitBox.OnDied -= HandleBroken;
        }

        private void HandleHit(int current, int max)
        {
            if (opened || current <= 0 || config == null || config.shakeAmount <= 0f) return;
            StopAllCoroutines();
            StartCoroutine(Shake());
        }

        private System.Collections.IEnumerator Shake()
        {
            float t = 0f;
            while (t < config.shakeSeconds)
            {
                t += Time.deltaTime;
                shaker.position = home + new Vector3(Random.Range(-config.shakeAmount, config.shakeAmount), 0f, 0f);
                yield return null;
            }
            shaker.position = home;
        }

        private void HandleBroken()
        {
            if (opened) return;
            opened = true;

            StopAllCoroutines();
            shaker.position = home;

            if (visual != null && openedSprite != null) visual.sprite = openedSprite;

            var p = PlayerLocator.Find();
            Vector3 from = spawnAnchor != null ? spawnAnchor.position : home;
            SkillRewardFlyer.Spawn(
                from + Vector3.up * (config != null ? config.spawnHeight : 0f),
                p != null ? p.transform : null,
                config);
        }
    }
}
