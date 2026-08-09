using UnityEngine;
using UnityEngine.UI;

namespace NAN2026
{
    // 게임오버 패널 등장 연출. GameOverController가 timeScale=0으로 멈추므로
    // 모든 시간 계산은 unscaledDeltaTime을 쓴다.
    public class GameOverPanelFx : MonoBehaviour
    {
        public Image background;
        public Image logo;
        public CanvasGroup hintGroup;
        public float bgFadeSeconds = 0.7f;
        public float logoDelay = 0.35f;
        public float logoFadeSeconds = 0.8f;
        public float logoRise = 40f;      // 아래에서 위로 떠오르는 거리(px)
        public float hintDelay = 1.4f;
        public float hintBlinkSpeed = 2.2f;

        private float t;
        private Vector2 logoHome;

        private void OnEnable()
        {
            t = 0f;
            if (logo != null)
            {
                logoHome = logo.rectTransform.anchoredPosition;
                logo.rectTransform.anchoredPosition = logoHome - new Vector2(0f, logoRise);
                SetAlpha(logo, 0f);
            }
            if (background != null) SetAlpha(background, 0f);
            if (hintGroup != null) hintGroup.alpha = 0f;
        }

        private void Update()
        {
            t += Time.unscaledDeltaTime;
            if (background != null)
                SetAlpha(background, Mathf.Clamp01(t / bgFadeSeconds));
            if (logo != null)
            {
                float lt = Mathf.Clamp01((t - logoDelay) / logoFadeSeconds);
                SetAlpha(logo, lt);
                logo.rectTransform.anchoredPosition = logoHome - new Vector2(0f, logoRise * (1f - lt));
            }
            if (hintGroup != null && t >= hintDelay)
                hintGroup.alpha = 0.45f + 0.55f * Mathf.Abs(Mathf.Sin((t - hintDelay) * hintBlinkSpeed));
        }

        private static void SetAlpha(Image img, float a)
        {
            var c = img.color; c.a = a; img.color = c;
        }
    }
}
