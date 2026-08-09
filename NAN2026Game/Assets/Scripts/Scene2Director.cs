using UnityEngine;
using System.Collections;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace NAN2026
{
    public static class SpikeParryEvents
    {
        public static int Count; // 정적 누계 — 구독 유실과 무관

        /// 패링 목표를 채운 뒤 켜진다. 런처·투사체·트랩이 각자 Update 첫 줄에서 이걸 보고 스스로 멈춘다.
        /// 감독이 한 번 훑어 지우는 방식은 연출 4초 사이에 생긴 것을 놓친다 — 그래서 원천 차단으로 바꿨다.
        public static bool CombatSealed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStaticsOnPlay() { Count = 0; CombatSealed = false; OnParry = null; } // DisableDomainReload 대응
        public static System.Action OnParry;
        public static void Report() { Count++; if (OnParry != null) OnParry(); }
    }

    // Scene2 연출 감독: 스파이크 패링 집계(폴링) → 목표 달성 시 밝아짐+보스 개막 팬 → 복귀
    public class Scene2Director : MonoBehaviour
    {
        public Scene2DirectorConfig config;
        private int count;
        private bool done;
        private Transform player, boss;
        private Component cmCam;
        private System.Reflection.PropertyInfo followProp;
        private Text topLabel;
        private Behaviour bossAi;
        private SpriteRenderer bossSr;
        private Collider2D bossCol;

        private void Start()
        {
            SpikeParryEvents.Count = 0;
            // FAIL: DisableDomainReload 프로젝트라 CombatSealed는 Play 시작 시 딱 한 번만
            // 리셋되고, 씬을 다시 로드해도 안 풀린다. 예전엔 씬2를 한 번 클리어하면 다시
            // 못 돌아오는 선형 진행이라 문제없었는데, 세이브포인트로 자유 왕복이 가능해진
            // 지금은 이미 클리어했던 세션에서 다시 씬2로 오면 CombatSealed=true가 그대로 남아
            // ThrownWeaponLauncher가 영원히 발사를 안 하는(스파이크가 안 생기는) 버그가 됨.
            // 이 씬이 새로 시작될 때마다 반드시 다시 풀어준다.
            SpikeParryEvents.CombatSealed = false;
            var p = PlayerLocator.Find();
            if (p != null) player = p.transform;
            var b = GameObject.Find("MinoBoss");
            if (b != null)
            {
                boss = b.transform;
                CacheBossParts(b);
                SetBossRevealed(false);   // 패링 목표를 채우기 전에는 보스가 없는 것처럼 둔다
            }
            foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
                if (mb.GetType().Name == "CinemachineCamera") { cmCam = mb; followProp = mb.GetType().GetProperty("Follow"); break; }
            BuildTopLabel();
            if (config != null && config.debugSkipToBoss)
            {
                SpikeParryEvents.Count = config.parryGoal;
                if (player != null && boss != null)
                    player.position = boss.position + new Vector3(-config.debugSpawnOffsetX, 0.5f, 0f);
                SetBossRevealed(true);
            }
        }

        private void Update()
        {
            if (done || config == null) return;
            if (SpikeParryEvents.Count != count)
            {
                count = Mathf.Min(SpikeParryEvents.Count, config.parryGoal);
                UpdateTopLabel();
                if (count >= config.parryGoal) { done = true; SpikeParryEvents.CombatSealed = true; StartCoroutine(Brighten()); }
            }
        }

        private void BuildTopLabel()
        {
            var canvas = GameObject.Find("UI Canvas");
            if (canvas == null || config == null) return;
            var go = new GameObject("SpikeParryLabel");
            go.transform.SetParent(canvas.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -28f);
            rt.sizeDelta = new Vector2(640f, 60f);
            topLabel = go.AddComponent<Text>();
            topLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            topLabel.fontSize = 34;
            topLabel.alignment = TextAnchor.MiddleCenter;
            topLabel.color = new Color(1f, 0.9f, 0.35f);
            UpdateTopLabel();
        }

        private void UpdateTopLabel()
        {
            if (topLabel == null || config == null) return;
            topLabel.text = count >= config.parryGoal ? "어둠이 걷혔다!" : "스파이크 패링  " + count + " / " + config.parryGoal;
        }

        private void SetPlayerControl(bool on)
        {
            // 입력 게이트 방식: 컨트롤러는 계속 구동(내부 상태·애니 안전), 입력만 차단
            PlayerController2D.InputLocked = !on;
            if (player == null) return;
            var rb = player.GetComponent<Rigidbody2D>();
            if (rb != null && !on) rb.linearVelocity = Vector2.zero;
        }

        private void CacheBossParts(GameObject b)
        {
            foreach (var mb in b.GetComponents<MonoBehaviour>())
                if (mb.GetType().Name == "MinoBoss") { bossAi = mb; break; }
            bossSr = b.GetComponent<SpriteRenderer>();
            bossCol = b.GetComponent<Collider2D>();
        }

        /// 보스를 숨기거나 드러낸다.
        /// GameObject 자체는 계속 켜둔다 — GameObject.Find 와 보스에 붙인 핍(자식)이 살아 있어야 하기 때문.
        /// 컴포넌트만 끄므로 AI·렌더·충돌이 전부 멈춘다.
        private void SetBossRevealed(bool on)
        {
            if (bossAi != null) bossAi.enabled = on;
            if (bossSr != null) bossSr.enabled = on;
            if (bossCol != null) bossCol.enabled = on;
            // 보스에 붙은 표시물(GroggyPips 등)도 함께. 보스만 숨기면 핍이 허공에 떠 있게 된다.
            if (boss != null)
                foreach (var r in boss.GetComponentsInChildren<Renderer>(true)) r.enabled = on;
        }

        private IEnumerator Brighten()
        {
            Time.timeScale = 1f;          // 잔여 히트스톱 청소
            SetPlayerControl(false);      // 컷신 락
            // 비활성 개체까지 포함해 훑는다. 기본값은 비활성 제외라 꺼져 있던 것이 나중에 되살아났다.
            foreach (var l in FindObjectsByType<ThrownWeaponLauncher>(FindObjectsInactive.Include, FindObjectsSortMode.None)) l.enabled = false;
            foreach (var pr in FindObjectsByType<ThrownProjectile>(FindObjectsInactive.Include, FindObjectsSortMode.None)) Destroy(pr.gameObject);
            foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var n = mb.GetType().Name;
                if (n == "SwingingBladeTrap") mb.enabled = false;   // 이름이 달라 그동안 정리에서 빠져 있었다
            }
            Light2D global = null;
            foreach (var l2 in FindObjectsByType<Light2D>(FindObjectsSortMode.None))
                if (l2.lightType == Light2D.LightType.Global) { global = l2; break; }
            if (global != null)
            {
                float from = global.intensity, t = 0f;
                while (t < config.brightenTime)
                {
                    t += Time.unscaledDeltaTime;
                    global.intensity = Mathf.Lerp(from, config.brightenTarget, t / config.brightenTime);
                    yield return null;
                }
                global.intensity = config.brightenTarget;
            }
            // 어둠이 걷힌 뒤에 보스를 드러낸다. 카메라 팬이 빈 자리를 비추지 않도록 팬보다 먼저.
            SetBossRevealed(true);

            // 보스전 개막: 카메라 보스 팬 → 플레이어 복귀 (씬3 벽붕괴 연출과 동일 문법)
            if (cmCam != null && followProp != null && boss != null)
            {
                followProp.SetValue(cmCam, boss, null);
                yield return new WaitForSecondsRealtime(config.revealHold);
                if (player != null) followProp.SetValue(cmCam, player, null);
            }
            yield return new WaitForSecondsRealtime(config.brightenHold);
            SetPlayerControl(true);
        }
    }
}
