using UnityEngine;
using UnityEngine.UI;

namespace NAN2026
{
    // MP 본체: 총량 10, 패링 성공 시 +1 (팀 명세). 파란 하트 HUD.
    public class PlayerMana : MonoBehaviour
    {
        public ManaConfig config;
        private int mp;
        private const int HardCapMp = 15;   // 최대치 상한(런타임 보너스는 팀 필드 maxBonus 사용)
        private int maxBonus;   // 런타임 최대치 보너스 — ScriptableObject를 직접 고치지 않는다(에셋 오염 방지)
        private Image[] hearts;

        public int Mp { get { return mp; } }
        public int MaxMp { get { return Mathf.Min(15, (config != null ? config.maxMp : 10) + maxBonus); } }

        private void Start()
        {

            config.maxMp = 10;
            config.parryGain = 1;
            config.startMp = 5;
            mp = config != null ? Mathf.Clamp(config.startMp, 0, config.maxMp) : 0;
            BuildHud();
            Refresh();
        }

        // 전 패링 훅(SendMessage \"AddMp\")이 이 메서드로 들어온다.
        // 훅마다 보내는 수치가 달라도 팀 명세대로 '성공 1회 = +1'로 통일.
        public void AddMp(int ignoredAmount)
        {
            if (config == null) return;
            mp = Mathf.Min(MaxMp, mp + config.parryGain);
            Refresh();
        }
        public void MaxUp(int n)
        {
            if (config == null) return;
            maxBonus = Mathf.Min(15 - (config != null ? config.maxMp : 10), maxBonus + n); // 에셋 미수정
            mp = Mathf.Min(MaxMp, mp + n);
            BuildHud();   // 슬롯 수가 늘었으므로 재구성 (중복 가드는 BuildHud 내부)
            Refresh();

        }
        public static void RewardParry(Component playerContext)
        {
            if (playerContext == null) return;
            PlayerMana mana = playerContext.GetComponentInParent<PlayerMana>();
            if (mana != null) mana.AddMp(1);
        }

        /// <summary>현재 마나를 즉시 최대치까지 채운다(세이브포인트 등 완전 회복 지점 전용).
        /// MaxUp()과 달리 최대치 자체는 건드리지 않고 현재값만 채운다.</summary>
        public void RefillToMax()
        {
            mp = MaxMp;
            Refresh();
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
            // 기존 HUD 제거 — 가드가 없으면 캔버스가 겹쳐 생기고, 화면엔 낡은 하트가 남는다
            var old = GameObject.Find("MpHud");
            while (old != null) { DestroyImmediate(old); old = GameObject.Find("MpHud"); }
            // 독립 루트 캔버스 (플레이어 자식 X — 렌더 안정성) + 해상도 스케일러
            var cgo = new GameObject("MpHud");
            DontDestroyOnLoad(cgo);
            var canvas = cgo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1;
            var scaler = cgo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            hearts = new Image[MaxMp];
            for (int i = 0; i < MaxMp; i++)
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