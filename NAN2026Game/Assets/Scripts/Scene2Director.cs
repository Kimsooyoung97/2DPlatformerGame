using UnityEngine;
using System.Collections;
using UnityEngine.Rendering.Universal;

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
            if (config != null && config.debugSkipToBoss)
            {
                count = config.parryGoal; done = true;
                RefreshPips();
                if (player != null && boss != null)
                    player.position = boss.position + new Vector3(-config.debugSpawnOffsetX, 0.5f, 0f);
                StartCoroutine(Brighten());
            }
        }

        private void OnDestroy() { SpikeParryEvents.OnParry -= HandleParry; }

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
            StartCoroutine(FocusBoss());
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

        private IEnumerator Brighten()
        {
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
                t += Time.deltaTime;
                global.intensity = Mathf.Lerp(from, config.brightenTarget, t / config.brightenTime);
                yield return null;
            }
            global.intensity = config.brightenTarget;
        }
    }
}
