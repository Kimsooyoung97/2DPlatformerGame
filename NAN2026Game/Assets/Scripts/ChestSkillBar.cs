using UnityEngine;
using UnityEngine.UI;
using NAN2026.Core;

namespace NAN2026
{
    // 좌하단 스킬 슬롯 바. 상자를 부술 때마다 한 칸씩 채워진다.
    // 팀 UI(UI Canvas/Skill)는 건드리지 않고 별도 오브젝트로 산다.
    [RequireComponent(typeof(RectTransform))]
    public class ChestSkillBar : MonoBehaviour
    {
        public ChestRewardConfig config;

        private Image[] slots;
        private float[] popT;
        private bool[] filled;

        private void Awake() { Build(); }

        private void OnEnable()
        {
            ChestRewardEvents.OnCollected += Fill;
            // 이 오브젝트보다 먼저 수집이 보고됐을 수 있다 — 현재 누계를 되살린다
            for (int i = 0; i < ChestRewardEvents.Collected; i++) Fill(i);
        }

        private void OnDisable() { ChestRewardEvents.OnCollected -= Fill; }

        private void Build()
        {
            if (config == null) { Debug.LogWarning("[ChestSkillBar] config 가 비어 있습니다.", this); return; }
            int n = config.slotCapacity;
            if (n <= 0) return;

            var rt = (RectTransform)transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = Vector2.zero;
            rt.anchoredPosition = new Vector2(config.marginX, config.marginY);
            rt.sizeDelta = new Vector2(n * config.slotSize + (n - 1) * config.slotSpacing, config.slotSize);

            slots = new Image[n];
            popT = new float[n];
            filled = new bool[n];

            for (int i = 0; i < n; i++)
            {
                var go = new GameObject("Slot" + i, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                var r = (RectTransform)go.transform;
                r.SetParent(transform, false);
                r.anchorMin = Vector2.zero;
                r.anchorMax = Vector2.zero;
                r.pivot = new Vector2(0.5f, 0.5f);
                r.sizeDelta = new Vector2(config.slotSize, config.slotSize);
                r.anchoredPosition = new Vector2(
                    config.slotSize * 0.5f + i * (config.slotSize + config.slotSpacing),
                    config.slotSize * 0.5f);

                var img = go.GetComponent<Image>();
                img.sprite = config.icon;
                img.color = config.slotEmptyTint;
                img.raycastTarget = false;
                img.preserveAspect = true;
                go.SetActive(config.showEmptySlots);
                slots[i] = img;
            }
        }

        private void Fill(int index)
        {
            if (slots == null || index < 0 || index >= slots.Length) return;
            if (filled[index]) return;
            filled[index] = true;
            popT[index] = 0f;
            slots[index].gameObject.SetActive(true);
            slots[index].color = config.tint;
        }

        private void Update()
        {
            if (slots == null || config == null) return;
            for (int i = 0; i < slots.Length; i++)
            {
                if (!filled[i] || popT[i] >= config.popTime) continue;
                popT[i] += Time.unscaledDeltaTime;
                float s = ChestRewardLogic.PopScale(popT[i], config.popTime, config.popPeak);
                slots[i].rectTransform.localScale = Vector3.one * s;
            }
        }
    }
}
