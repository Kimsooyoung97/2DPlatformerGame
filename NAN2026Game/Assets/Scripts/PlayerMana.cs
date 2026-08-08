using UnityEngine;
using UnityEngine.UI;

namespace NAN2026
{
    // MP 본체: 총량 10, 패링 성공 시 +1 (팀 명세). 파란 하트 HUD.
    public class PlayerMana : MonoBehaviour
    {
        public ManaConfig config;
        private int mp;
        private Image[] hearts;

        public int Mp { get { return mp; } }
        public int MaxMp { get { return config != null ? config.maxMp : 10; } }

        private void Start()
        {
            mp = config != null ? Mathf.Clamp(config.startMp, 0, config.maxMp) : 0;
            BuildHud();
            Refresh();
        }

        // 전 패링 훅(SendMessage \"AddMp\")이 이 메서드로 들어온다.
        // 훅마다 보내는 수치가 달라도 팀 명세대로 '성공 1회 = +1'로 통일.
        public void AddMp(int ignoredAmount)
        {
            if (config == null) return;
            mp = Mathf.Min(config.maxMp, mp + config.parryGain);
            Refresh();
        }

        public static void RewardParry(Component playerContext)
        {
            if (playerContext == null) return;
            PlayerMana mana = playerContext.GetComponentInParent<PlayerMana>();
            if (mana != null) mana.AddMp(1);
        }


        // 스킬 소모용 API — 소모량·연동은 팀 결정 대기, 아직 아무도 호출 안 함
        public bool TryUseMp(int amount)
        {
            if (mp < amount) return false;
            mp -= amount;
            Refresh();
            return true;
        }

        private void BuildHud()
        {
            if (config == null || config.heartFull == null) return;
            // 독립 루트 캔버스 (플레이어 자식 X — 렌더 안정성) + 해상도 스케일러
            var cgo = new GameObject("MpHud");
            var canvas = cgo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            var scaler = cgo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            hearts = new Image[config.maxMp];
            for (int i = 0; i < config.maxMp; i++)
            {
                var h = new GameObject("MpHeart_" + (i + 1));
                h.transform.SetParent(cgo.transform, false);
                var img = h.AddComponent<Image>();
                img.sprite = config.heartFull;
                img.raycastTarget = false;
                var rt = img.rectTransform;
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.sizeDelta = new Vector2(config.heartSize, config.heartSize);
                rt.anchoredPosition = new Vector2(config.hudOffset.x + i * config.heartSpacing, config.hudOffset.y);
                hearts[i] = img;
            }
        }

        private void Refresh()
        {
            if (hearts == null) return;
            for (int i = 0; i < hearts.Length; i++)
            {
                if (hearts[i] == null) continue;
                bool full = i < mp;
                if (config.heartEmpty != null)
                    hearts[i].sprite = full ? config.heartFull : config.heartEmpty;
                else
                    hearts[i].color = full ? Color.white : new Color(0.25f, 0.25f, 0.3f, 0.9f);
            }
        }
    }
}
