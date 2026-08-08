using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;
using Unity.Cinemachine;

namespace NAN2026.Showroom
{
    // 스파이크 구체 패링 카운터.
    // 목표 횟수 달성 시: 맵 전체 점등 → 카메라가 보스로 팬(홀드) → 플레이어 복귀 → 보스전 시작.
    // 연출 구조는 GateCollapseSequencer(씬3 벽 붕괴)의 vcam TrackingTarget 스왑 방식을 따른다.
    public class ParryMeter : MonoBehaviour
    {
        public static ParryMeter Instance { get; private set; }

        [Header("Refs (비우면 자동 탐색)")]
        public ExecutionerBoss boss;
        public Light2D globalLight;
        public CinemachineCamera vcam;

        [Header("Settings")]
        public int targetParries = 10;
        public float brightIntensity = 1f;
        public float brightenTime = 1.2f;
        public float panHold = 2.0f;
        public float returnWait = 0.9f;

        private int count;
        private bool fired;
        private Text label;
        private Transform playerTarget;

        private void Awake()
        {
            Instance = this;
            if (boss == null) boss = FindAnyObjectByType<ExecutionerBoss>();
            if (globalLight == null)
            {
                var gl = GameObject.Find("Global Light 2D");
                if (gl != null) globalLight = gl.GetComponent<Light2D>();
            }
            if (vcam == null)
            {
                var cm = GameObject.Find("CM_PlayerCamera");
                if (cm != null) vcam = cm.GetComponent<CinemachineCamera>();
            }
            CreateLabel();
            UpdateLabel();
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }

        public static void ReportSpike()
        {
            if (Instance != null) Instance.OnSpikeParried();
        }

        private void OnSpikeParried()
        {
            if (fired) return;
            count++;
            UpdateLabel();
            if (count >= targetParries)
            {
                fired = true;
                StartCoroutine(RevealSequence());
            }
        }

        private void CreateLabel()
        {
            var canvas = GameObject.Find("UI Canvas");
            if (canvas == null) return;
            var go = new GameObject("ParryCountLabel");
            go.transform.SetParent(canvas.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -18f);
            rt.sizeDelta = new Vector2(700f, 56f);
            label = go.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 30;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = new Color(1f, 0.92f, 0.5f);
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            outline.effectDistance = new Vector2(2f, -2f);
        }

        private void UpdateLabel()
        {
            if (label == null) return;
            label.text = count >= targetParries
                ? "봉인 해제!"
                : "스파이크 패링  " + count + " / " + targetParries;
        }

        private IEnumerator RevealSequence()
        {
            // 1) 맵 전체 점등
            if (globalLight != null)
            {
                float from = globalLight.intensity;
                float t = 0f;
                while (t < brightenTime)
                {
                    t += Time.deltaTime;
                    globalLight.intensity = Mathf.Lerp(from, brightIntensity, t / brightenTime);
                    yield return null;
                }
                globalLight.intensity = brightIntensity;
            }

            // 2) 플레이어 조작 잠금 + 카메라 보스로 팬
            var pc = FindAnyObjectByType<PlayerController2D>();
            Rigidbody2D prb = null;
            if (pc != null)
            {
                prb = pc.GetComponent<Rigidbody2D>();
                pc.enabled = false;
                if (prb != null) prb.linearVelocity = new Vector2(0f, prb.linearVelocity.y);
            }
            if (vcam != null && boss != null)
            {
                playerTarget = vcam.Target.TrackingTarget;
                vcam.Target.TrackingTarget = boss.transform;
            }
            yield return new WaitForSeconds(panHold);

            // 3) 카메라 복귀 → 보스전 시작
            if (vcam != null && playerTarget != null)
                vcam.Target.TrackingTarget = playerTarget;
            yield return new WaitForSeconds(returnWait);
            if (pc != null) pc.enabled = true;
            if (boss != null) boss.ForceCombat();
            if (label != null) StartCoroutine(FadeLabel());
        }

        private IEnumerator FadeLabel()
        {
            yield return new WaitForSeconds(1.5f);
            float t = 0f;
            var c0 = label.color;
            while (t < 1f)
            {
                t += Time.deltaTime;
                var c = c0; c.a = 1f - t; label.color = c;
                yield return null;
            }
            label.gameObject.SetActive(false);
        }
    }
}
