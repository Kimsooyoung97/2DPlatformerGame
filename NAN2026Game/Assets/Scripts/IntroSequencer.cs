using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.InputSystem;
using NAN2026.Core;

namespace NAN2026
{
    // 인트로: 암전 -> 촛불 점화 -> 전역 확장 -> BGM. 아무 키나 누르면 스킵.
    public class IntroSequencer : MonoBehaviour
    {
        public IntroConfig config;
        public Light2D globalLight;
        public Light2D[] candleLights;
        public SpriteRenderer[] candleGlows;
        public GameObject[] candleLitNodes;
        public GameObject[] hiddenDuringIgnite; // 촛불 반경 내 이웃 소품 — 확장 전까지 숨김 // 파티클(Flame/Glow) 묶음 — 점화 전 완전 소등용
        public AudioSource bgm;

        float t;
        float bgmT;
        bool bgmStarted;
        float[] glowBaseAlpha;

        void Awake()
        {
            glowBaseAlpha = new float[candleGlows.Length];
            for (int i = 0; i < candleGlows.Length; i++)
                glowBaseAlpha[i] = candleGlows[i] != null ? candleGlows[i].color.a : 1f;
            Apply(0f);
        }

        void Update()
        {
            t += Time.deltaTime;
            bool skip = Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame;
            float total = IntroSequenceLogic.TotalDuration(config.blackSeconds, config.igniteSeconds, config.expandSeconds);
            if (skip && t < total) t = total;
            Apply(t);

            if (bgmStarted && bgm != null)
            {
                bgmT += Time.deltaTime;
                bgm.volume = config.bgmVolume * IntroSequenceLogic.Clamp01(bgmT / config.bgmFadeSeconds);
                if (t >= total && bgmT >= config.bgmFadeSeconds)
                    enabled = false; // 연출·페이드 완료 — 이후 프레임 비용 0
            }
        }

        void Apply(float time)
        {
            float cf = IntroSequenceLogic.CandleFactor(time, config.blackSeconds, config.igniteSeconds);
            float gf = IntroSequenceLogic.GlobalFactor(time, config.blackSeconds, config.igniteSeconds, config.expandSeconds);

            if (candleLights != null)
                for (int i = 0; i < candleLights.Length; i++)
                    if (candleLights[i] != null) candleLights[i].intensity = config.candleIntensity * cf;

            if (candleLitNodes != null)
            {
                bool lit = cf > 0f;
                for (int i = 0; i < candleLitNodes.Length; i++)
                    if (candleLitNodes[i] != null && candleLitNodes[i].activeSelf != lit)
                        candleLitNodes[i].SetActive(lit);
            }

            if (hiddenDuringIgnite != null)
            {
                bool show = gf > 0f;
                for (int i = 0; i < hiddenDuringIgnite.Length; i++)
                    if (hiddenDuringIgnite[i] != null && hiddenDuringIgnite[i].activeSelf != show)
                        hiddenDuringIgnite[i].SetActive(show);
            }

            if (candleGlows != null)
                for (int i = 0; i < candleGlows.Length; i++)
                    if (candleGlows[i] != null)
                    {
                        var c = candleGlows[i].color;
                        c.a = glowBaseAlpha[i] * cf;
                        candleGlows[i].color = c;
                    }

            if (globalLight != null) globalLight.intensity = config.globalMaxIntensity * gf;

            if (!bgmStarted && bgm != null && IntroSequenceLogic.BgmShouldPlay(time, config.blackSeconds, config.igniteSeconds, config.expandSeconds))
            {
                bgmStarted = true;
                bgm.volume = 0f;
                bgm.Play();
            }
        }
    }
}
