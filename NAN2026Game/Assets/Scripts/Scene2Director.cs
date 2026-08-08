using UnityEngine;
using System.Collections;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace NAN2026
{
    public static class SpikeParryEvents
    {
        public static int Count; // 정적 누계 — 구독 유실과 무관하게 항상 오른다
        public static System.Action OnParry;
        public static void Report() { Count++; if (OnParry != null) OnParry(); }
    }

    // Scene2 연출 감독: 환경 투사체 패링 진행도를 상단에 표시하고,
    // 목표 달성 시 화면을 밝힌 뒤 스파이크를 종료한다.
    public class Scene2Director : MonoBehaviour
    {
        public Scene2DirectorConfig config;
        private int count;
        private bool done;
        private Transform player;
        private Transform boss;
        private Text topLabel;

        private void Start()
        {
            var playerObject = GameObject.Find("Player");
            if (playerObject != null) player = playerObject.transform;

            var bossObject = GameObject.Find("MinoBoss");
            if (bossObject != null) boss = bossObject.transform;

            SpikeParryEvents.OnParry += HandleParry;
            BuildTopLabel();

            if (config != null && config.debugSkipToBoss)
            {
                count = config.parryGoal;
                done = true;
                UpdateTopLabel();
                if (player != null && boss != null)
                    player.position = boss.position + new Vector3(-config.debugSpawnOffsetX, 0.5f, 0f);
                StartCoroutine(Brighten());
            }
        }

        private void OnDestroy()
        {
            SpikeParryEvents.OnParry -= HandleParry;
        }

        private void BuildTopLabel()
        {
            var canvas = GameObject.Find("UI Canvas");
            if (canvas == null || config == null) return;

            var labelObject = new GameObject("SpikeParryLabel");
            labelObject.transform.SetParent(canvas.transform, false);

            var rectTransform = labelObject.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 1f);
            rectTransform.anchorMax = new Vector2(0.5f, 1f);
            rectTransform.pivot = new Vector2(0.5f, 1f);
            rectTransform.anchoredPosition = new Vector2(0f, -28f);
            rectTransform.sizeDelta = new Vector2(640f, 60f);

            topLabel = labelObject.AddComponent<Text>();
            topLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            topLabel.fontSize = 34;
            topLabel.alignment = TextAnchor.MiddleCenter;
            topLabel.color = new Color(1f, 0.9f, 0.35f);
            UpdateTopLabel();
        }

        private void UpdateTopLabel()
        {
            if (topLabel == null || config == null) return;
            topLabel.text = count >= config.parryGoal
                ? "어둠이 걷혔다!"
                : "스파이크 패링  " + count + " / " + config.parryGoal;
        }

        private void HandleParry()
        {
            if (done || config == null) return;

            count++;
            UpdateTopLabel();

            if (count < config.parryGoal) return;
            done = true;
            StartCoroutine(Brighten());
        }

        private IEnumerator Brighten()
        {
            foreach (var launcher in FindObjectsByType<ThrownWeaponLauncher>())
                launcher.enabled = false;

            foreach (var projectile in FindObjectsByType<ThrownProjectile>())
                Destroy(projectile.gameObject);

            foreach (var behaviour in FindObjectsByType<MonoBehaviour>())
            {
                if (behaviour.GetType().Name == "SpikeBallTrap")
                    behaviour.enabled = false;
            }

            Light2D globalLight = null;
            foreach (var light2D in FindObjectsByType<Light2D>())
            {
                if (light2D.lightType != Light2D.LightType.Global) continue;
                globalLight = light2D;
                break;
            }

            if (globalLight != null)
            {
                float startIntensity = globalLight.intensity;
                float duration = Mathf.Max(0f, config.brightenTime);
                float elapsed = 0f;

                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float progress = duration > 0f ? elapsed / duration : 1f;
                    globalLight.intensity = Mathf.Lerp(
                        startIntensity,
                        config.brightenTarget,
                        progress);
                    yield return null;
                }

                globalLight.intensity = config.brightenTarget;
            }

            if (config.brightenHold > 0f)
                yield return new WaitForSecondsRealtime(config.brightenHold);
        }
    }
}
