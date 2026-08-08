using UnityEngine;
using System.Collections;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace NAN2026
{
    public static class SpikeParryEvents
    {
        public static System.Action OnParry;
        public static void Report() { if (OnParry != null) OnParry(); }
    }

    // Scene2 연출 감독: 스파이크 패링마다 보스 컷 + ◆ 핍, 목표 달성 시 화면이 밝아지고 스파이크 종료
    public class Scene2Director : MonoBehaviour
    {
        public Scene2DirectorConfig config;
        private int count;
        private bool done;
        private Transform player, boss;
        private Component cmCam;
        private System.Reflection.PropertyInfo followProp;
        private TextMesh pips;
        private Text topLabel;

        private void Start()
        {
            var p = GameObject.Find("Player");
            if (p != null) player = p.transform;
            var b = GameObject.Find("MinoBoss");
            if (b != null) boss = b.transform;
            foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
                if (mb.GetType().Name == "CinemachineCamera") { cmCam = mb; followProp = mb.GetType().GetProperty("Follow"); break; }
            SpikeParryEvents.OnParry += HandleParry;
            BuildPips();
            BuildTopLabel();
            if (config != null && config.debugSkipToBoss)
            {
                count = config.parryGoal; done = true;
                RefreshPips();
                UpdateTopLabel();
                if (player != null && boss != null)
                    player.position = boss.position + new Vector3(-config.debugSpawnOffsetX, 0.5f, 0f);
                StartCoroutine(Brighten());
            }
        }

        private void OnDestroy() { SpikeParryEvents.OnParry -= HandleParry; }

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
            for (int i = 0; i < config.parryGoal; i++) sb.Append(i < count ? '◆' : '◇');
            pips.text = sb.ToString();
        }

        private void HandleParry()
        {
            if (done || config == null) return;
            count++;
            RefreshPips();
            UpdateTopLabel();
            if (count >= config.parryGoal) { done = true; StartCoroutine(Brighten()); }
        }

        private IEnumerator FocusBoss()
        {
            if (cmCam == null || followProp == null || boss == null || player == null) yield break;
            followProp.SetValue(cmCam, boss, null);
            yield return new WaitForSeconds(config.camHold);
            if (!doneFocusHoldExtended()) followProp.SetValue(cmCam, player, null);
        }

        private bool doneFocusHoldExtended() { return false; }

        private void SetPlayerControl(bool on)
        {
            if (player == null) return;
            foreach (var mb in player.GetComponents<MonoBehaviour>())
            {
                string n = mb.GetType().Name;
                if (n == "PlayerController2D" || n == "PlayerSkill") ((Behaviour)mb).enabled = on;
            }
            var rb = player.GetComponent<Rigidbody2D>();
            if (rb != null && !on) rb.linearVelocity = Vector2.zero;
        }

        private IEnumerator Brighten()
        {
            SetPlayerControl(false); // 컷신 락: 밝아지는 동안 캐릭터 정지·입력 무시
            Time.timeScale = 1f; // 잔여 히트스톱 청소 (정지 방어)
            // 스파이크 전면 정지
            foreach (var l in FindObjectsByType<ThrownWeaponLauncher>(FindObjectsSortMode.None)) l.enabled = false;
            foreach (var pr in FindObjectsByType<ThrownProjectile>(FindObjectsSortMode.None)) Destroy(pr.gameObject);
            foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
                if (mb.GetType().Name == "SpikeBallTrap") mb.enabled = false;
            // 전역광 상승
            Light2D global = null;
            foreach (var l2 in FindObjectsByType<Light2D>(FindObjectsSortMode.None))
                if (l2.lightType == Light2D.LightType.Global) { global = l2; break; }
            if (global == null) yield break;
            float from = global.intensity, t = 0f;
            while (t < config.brightenTime)
            {
                t += Time.unscaledDeltaTime;
                global.intensity = Mathf.Lerp(from, config.brightenTarget, t / config.brightenTime);
                yield return null;
            }
            global.intensity = config.brightenTarget;
            yield return new WaitForSecondsRealtime(config.brightenHold);
            SetPlayerControl(true); // 연출 종료 → 조작 복귀
        }
    }
}
