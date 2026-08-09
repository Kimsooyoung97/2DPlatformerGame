using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using NAN2026.Core;

namespace NAN2026
{
    /// 지정한 y 구간(1·2단)에 배치된 몬스터를 **전부** 처치하면 돌무더기 방어막을 무너뜨린다.
    /// 기존 KeyMonsterGate(특정 1마리 사망)를 대체한다.
    ///
    /// 두 체력 체계가 섞여 있지만(우리 EnemyBase / 팀 NHNDemo.MonsterHealth)
    /// 둘 다 최종적으로 Destroy(gameObject) 로 끝나므로, 이벤트를 각각 구독하지 않고
    /// **살아남은 오브젝트 수를 세는 방식**으로 통일했다. 새 적 종류가 늘어도 수집 규칙만 타면 된다.
    [DisallowMultipleComponent]
    public class AreaClearGate : MonoBehaviour
    {
        [Header("Config (수치는 전부 여기 소유)")]
        public GateConfig config;

        [Header("Refs (비우면 자동 탐색)")]
        public GateCollapseSequencer sequencer;
        [Tooltip("sequencer 가 없을 때만 쓰는 폴백 — 이 오브젝트를 그냥 끈다")]
        public GameObject gateObject;

        private readonly List<GameObject> watched = new List<GameObject>();
        private bool collected;
        private bool opened;
        private float tick;
        private Text label;
        private int lastShown = -1;

        private void Start()
        {
            if (config == null) { Debug.LogError("[" + name + "] GateConfig 미배선", this); enabled = false; return; }
            if (sequencer == null) sequencer = FindAnyObjectByType<GateCollapseSequencer>();
            if (config.showRemainingLabel) CreateLabel();
        }

        private void Update()
        {
            // 수집은 첫 Update 에서 한다. Start 순서가 보장되지 않아
            // EnemyBase 들이 아직 자기 등록을 끝내지 않았을 수 있기 때문.
            if (!collected) { Collect(); collected = true; Refresh(); return; }
            if (opened) return;

            tick += Time.deltaTime;
            if (!GateCollapseLogic.TickDue(tick, config.clearCheckInterval)) return;
            tick = 0f;
            Refresh();
        }

        private void Collect()
        {
            watched.Clear();
            foreach (var e in FindObjectsByType<EnemyBase>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (e == null) continue;
                if (!GateCollapseLogic.InClearBand(e.transform.position.y, config.clearMinY, config.clearMaxY)) continue;
                if (!watched.Contains(e.gameObject)) watched.Add(e.gameObject);
            }
            // 팀 몬스터(MonsterHealth). 비활성 개체는 제외 — 꺼둔 몬스터 때문에 영영 안 열리는 일을 막는다.
            foreach (var m in FindObjectsByType<NHNDemo.MonsterHealth>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (m == null) continue;
                if (!GateCollapseLogic.InClearBand(m.transform.position.y, config.clearMinY, config.clearMaxY)) continue;
                if (!watched.Contains(m.gameObject)) watched.Add(m.gameObject);
            }
            Debug.Log("[AreaClearGate] 감시 대상 " + watched.Count + "마리 (y " + config.clearMinY + " ~ " + config.clearMaxY + ")", this);
        }

        private int Remaining()
        {
            int n = 0;
            for (int i = 0; i < watched.Count; i++)
                if (watched[i] != null && watched[i].activeInHierarchy) n++;
            return n;
        }

        private void Refresh()
        {
            int remaining = Remaining();
            UpdateLabel(remaining);
            if (!GateCollapseLogic.ShouldOpen(remaining, watched.Count, opened)) return;
            opened = true;
            Open();
        }

        private void Open()
        {
            if (sequencer != null) sequencer.Play();
            else if (gateObject != null) gateObject.SetActive(false);
            if (label != null) StartCoroutine(FadeLabel());
        }

        private void CreateLabel()
        {
            var canvas = GameObject.Find("UI Canvas");
            if (canvas == null) return;
            var go = new GameObject("AreaClearLabel");
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

        private void UpdateLabel(int remaining)
        {
            if (label == null || remaining == lastShown) return;
            lastShown = remaining;
            label.text = remaining > 0 ? "남은 적  " + remaining : "길이 열렸다!";
        }

        private IEnumerator FadeLabel()
        {
            yield return new WaitForSeconds(config.labelFadeSeconds);
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
