using UnityEngine;
using System.Collections;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace NAN2026
{
    public static class SpikeParryEvents
    {
        public static int Count; // 정적 누계 — 구독 유실과 무관

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStaticsOnPlay() { Count = 0; OnParry = null; } // DisableDomainReload 대응
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
        private TextMesh pips;

        private void Start()
        {
            SpikeParryEvents.Count = 0;
            var p = GameObject.Find("Player");
            if (p != null) player = p.transform;
            var b = GameObject.Find("MinoBoss");
            if (b != null) boss = b.transform;
            foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
                if (mb.GetType().Name == "CinemachineCamera") { cmCam = mb; followProp = mb.GetType().GetProperty("Follow"); break; }
            BuildTopLabel();
            BuildPips();
            if (config != null && config.debugSkipToBoss)
            {
                SpikeParryEvents.Count = config.parryGoal;
                if (player != null && boss != null)
                    player.position = boss.position + new Vector3(-config.debugSpawnOffsetX, 0.5f, 0f);
            }
        }

        private void Update()
        {
            if (done || config == null) return;
            if (SpikeParryEvents.Count != count)
            {
                count = Mathf.Min(SpikeParryEvents.Count, config.parryGoal);
                RefreshPips();
                UpdateTopLabel();
                if (count >= config.parryGoal) { done = true; StartCoroutine(Brighten()); }
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

        private void BuildPips()
        {
            if (boss == null || config == null) return;
            var go = new GameObject("ParryPips");
            go.transform.SetParent(boss, false);
            go.transform.localPosition = new Vector3(0f, config.pipOffsetY, 0f);
            pips = go.AddComponent<TextMesh>();
            pips.fontSize = 48; pips.characterSize = 0.08f;
            pips.anchor = TextAnchor.MiddleCenter;
            pips.color = new Color(1f, 0.85f, 0.2f);
            go.GetComponent<MeshRenderer>().sortingOrder = 900;
            RefreshPips();
        }

        private void RefreshPips()
        {
            if (pips == null || config == null) return;
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < config.parryGoal; i++) sb.Append(i < count ? '\u25c6' : '\u25c7');
            pips.text = sb.ToString();
        }

        private void SetPlayerControl(bool on)
        {
            // 입력 게이트 방식: 컨트롤러는 계속 구동(내부 상태·애니 안전), 입력만 차단
            PlayerController2D.InputLocked = !on;
            if (player == null) return;
            var rb = player.GetComponent<Rigidbody2D>();
            if (rb != null && !on) rb.linearVelocity = Vector2.zero;
        }

        private IEnumerator Brighten()
        {
            Time.timeScale = 1f;          // 잔여 히트스톱 청소
            SetPlayerControl(false);      // 컷신 락
            foreach (var l in FindObjectsByType<ThrownWeaponLauncher>(FindObjectsSortMode.None)) l.enabled = false;
            foreach (var pr in FindObjectsByType<ThrownProjectile>(FindObjectsSortMode.None)) Destroy(pr.gameObject);
            foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
                if (mb.GetType().Name == "SpikeBallTrap") mb.enabled = false;
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
