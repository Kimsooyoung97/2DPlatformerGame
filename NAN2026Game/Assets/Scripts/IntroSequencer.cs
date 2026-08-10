using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.InputSystem;
using NAN2026.Core;

namespace NAN2026
{
    // 인트로: 암전 -> 촛불 점화 -> 전역 확장 -> BGM. 아무 키나 누르면 스킵.
    // 사운드: 초가 하나 켜질 때마다(candleLitNodes[i]가 false->true로 전환되는 그 프레임)
    // 딱 1번 점화음을 재생한다. 순차 점화(igniteStagger)라 초마다 개별적으로 울린다.
    public class IntroSequencer : MonoBehaviour
    {
        public IntroConfig config;
        public Light2D globalLight;
        public Light2D[] candleLights;
        public SpriteRenderer[] candleGlows;
        public GameObject[] candleLitNodes;
        public GameObject[] hiddenDuringIgnite; // 촛불 반경 내 이웃 소품 — 확장 전까지 숨김 // 파티클(Flame/Glow) 묶음 — 점화 전 완전 소등용
        public AudioSource bgm;
        public AudioSource sfxSource; // 점화음 전용 — bgm 채널과 분리(페이드 영향 안 받음)
        public GameObject introCamera;
        public GameObject playerVisionLight; // 주인공 시야광 — 인트로 동안 꺼두고 연출 완주 시 점등 // 촛불을 비추는 인트로 전용 카메라 — 연출 종료 시 꺼지며 플레이어 카메라로 인계

        float t;
        float bgmT;
        bool bgmStarted;
        float[] glowBaseAlpha;

        float EffIgnite()
        {
            int n = candleLights != null ? candleLights.Length : 1;
            return config.igniteSeconds + config.igniteStagger * (n > 0 ? n - 1 : 0);
        }

        void Awake()
        {
            if (playerVisionLight != null) playerVisionLight.SetActive(false); // 암전 중 주인공이 보이면 안 된다
            // 왼쪽부터 순차 점화: 라이트·Lit 노드를 x 오름차순 동기 정렬
            if (candleLights != null && candleLitNodes != null && candleLights.Length == candleLitNodes.Length)
                for (int a = 0; a < candleLights.Length - 1; a++)
                    for (int b = a + 1; b < candleLights.Length; b++)
                        if (candleLights[b] != null && candleLights[a] != null
                            && candleLights[b].transform.position.x < candleLights[a].transform.position.x)
                        {
                            var tl = candleLights[a]; candleLights[a] = candleLights[b]; candleLights[b] = tl;
                            var tn = candleLitNodes[a]; candleLitNodes[a] = candleLitNodes[b]; candleLitNodes[b] = tn;
                        }

            glowBaseAlpha = new float[candleGlows.Length];
            for (int i = 0; i < candleGlows.Length; i++)
                glowBaseAlpha[i] = candleGlows[i] != null ? candleGlows[i].color.a : 1f;
            Apply(0f);
        }

        private bool introLocked, introLockDone;
        private System.Collections.Generic.List<AudioSource> mutedSrcs = new System.Collections.Generic.List<AudioSource>();
        private System.Collections.Generic.List<float> mutedVols = new System.Collections.Generic.List<float>();
        private void MuteWorldAudio(bool mute)
        {
            if (mute)
            {
                mutedSrcs.Clear(); mutedVols.Clear();
                foreach (var src in FindObjectsByType<AudioSource>())
                {
                    if (src == null || src == bgm) continue; // 인트로 BGM은 살림
                    mutedSrcs.Add(src); mutedVols.Add(src.volume);
                    src.volume = 0f;
                }
            }
            else
            {
                for (int i = 0; i < mutedSrcs.Count; i++)
                    if (mutedSrcs[i] != null) mutedSrcs[i].volume = mutedVols[i];
                mutedSrcs.Clear(); mutedVols.Clear();
            }
        }
        private void SetPlayerControl(bool on)
        {
            PlayerController2D.InputLocked = !on; // 입력 게이트 방식 (컨트롤러 계속 구동)
            var pgo = NAN2026.PlayerLocator.Find();
            if (pgo == null) return;
            var rb = pgo.GetComponent<Rigidbody2D>();
            if (rb != null && !on) rb.linearVelocity = Vector2.zero;
        }

        // clip이 null이거나 sfxSource가 없으면 조용히 무시 — 사운드 미배치 상태에서도 안전.
        private void PlayClip(AudioClip clip, float volume)
        {
            if (sfxSource == null || clip == null) return;
            sfxSource.PlayOneShot(clip, volume);
        }

        void Update()
        {
            t += Time.deltaTime;
            float total = IntroSequenceLogic.TotalDuration(config.blackSeconds, EffIgnite(), config.expandSeconds);
            if (!introLocked && t < total) { introLocked = true; SetPlayerControl(false); MuteWorldAudio(true); } // 연출 중 이동·사운드 잠금
            if (introLocked && !introLockDone && t >= total)
            {
                introLockDone = true;
                SetPlayerControl(true);
                MuteWorldAudio(false);
                if (introCamera != null) introCamera.SetActive(false); // 카메라 인계
                if (playerVisionLight != null) playerVisionLight.SetActive(true); // 주인공 시야광 점등
            }
            // 아무키 스킵 제거 — 토치 점화 연출은 항상 완주 (이동키 오발로 캔슬되던 문제 해소)
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
            float gf = IntroSequenceLogic.GlobalFactor(time, config.blackSeconds, EffIgnite(), config.expandSeconds);

            if (candleLights != null)
                for (int i = 0; i < candleLights.Length; i++)
                    if (candleLights[i] != null)
                        candleLights[i].intensity = config.candleIntensity
                            * IntroSequenceLogic.CandleFactor(time - config.igniteStagger * i, config.blackSeconds, config.igniteSeconds);

            if (candleLitNodes != null)
                for (int i = 0; i < candleLitNodes.Length; i++)
                {
                    if (candleLitNodes[i] == null) continue;
                    bool lit = IntroSequenceLogic.CandleFactor(time - config.igniteStagger * i, config.blackSeconds, config.igniteSeconds) > 0f;
                    if (candleLitNodes[i].activeSelf != lit)
                    {
                        candleLitNodes[i].SetActive(lit);
                        // 꺼짐->켜짐 전환(=이 초가 방금 점화된 순간)에만 재생. 이론상 소등 전환은
                        // 없지만(단조 증가하는 gf 기반) 방어적으로 lit==true일 때만 재생한다.
                        if (lit) PlayClip(config.candleIgniteClip, config.candleIgniteVolume);
                    }
                }

            if (hiddenDuringIgnite != null)
            {
                bool show = gf > 0f;
                for (int i = 0; i < hiddenDuringIgnite.Length; i++)
                    if (hiddenDuringIgnite[i] != null && hiddenDuringIgnite[i].activeSelf != show)
                        hiddenDuringIgnite[i].SetActive(show);
            }



            if (globalLight != null) globalLight.intensity = config.globalMaxIntensity * gf;

            if (!bgmStarted && bgm != null && IntroSequenceLogic.BgmShouldPlay(time, config.blackSeconds, EffIgnite(), config.expandSeconds))
            {
                bgmStarted = true;
                bgm.volume = 0f;
                bgm.Play();
            }
        }
    }
}