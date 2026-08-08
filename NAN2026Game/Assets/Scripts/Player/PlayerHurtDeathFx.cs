using UnityEngine;
using UnityEngine.InputSystem;
using NAN2026.Core;

namespace NAN2026
{
    /// 플레이어 피격·사망 스프라이트 연출. RealPlayer 프리팹에 부착하면 모든 씬에 자동 적용된다.
    /// Animator 가 매 프레임 sprite 를 덮어쓰므로, 연출 중에는 Animator 를 꺼서 소유권을 뺏는다.
    [RequireComponent(typeof(SpriteRenderer))]
    public class PlayerHurtDeathFx : MonoBehaviour
    {
        public PlayerFxConfig config;
        public Sprite[] hurtFrames;
        public Sprite[] deathFrames;

        private SpriteRenderer sr;
        private Animator anim;
        private PlayerHealth health;
        private int prevHealth = int.MinValue;

        private int mode;          // 0 없음 / 1 hurt / 2 death
        private float t;
        private bool previewOnly;  // 디버그 키로 재생한 경우 부활·입력락을 건드리지 않는다

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            anim = GetComponent<Animator>();
            health = GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.SuppressDeathHide = true;   // 사망 연출을 보여야 하므로 즉시 숨김을 막는다
                health.OnHealthChanged += HandleHealthChanged;
                health.OnPlayerDied += HandleDied;
                health.OnPlayerRespawned += HandleRespawned;
            }
        }

        private void OnDestroy()
        {
            if (health != null)
            {
                health.OnHealthChanged -= HandleHealthChanged;
                health.OnPlayerDied -= HandleDied;
                health.OnPlayerRespawned -= HandleRespawned;
            }
        }

        private void HandleHealthChanged(int cur, int max)
        {
            if (prevHealth == int.MinValue) { prevHealth = cur; return; }
            if (PlayerFxLogic.ShouldPlayHurt(prevHealth, cur)) Play(1, false);
            prevHealth = cur;
        }

        private void HandleDied() { Play(2, false); }

        private void HandleRespawned()
        {
            Stop();
            prevHealth = health != null ? health.CurrentHealth : prevHealth;
        }

        private void Update()
        {
            if (config == null) return;

            if (config.enableDebugKeys)
            {
                var kb = Keyboard.current;
                if (kb != null)
                {
                    if (kb.digit4Key.wasPressedThisFrame) Play(1, true);
                    if (kb.digit5Key.wasPressedThisFrame) Play(2, true);
                }
            }

            if (mode == 0) return;

            t += Time.deltaTime;
            var frames = mode == 1 ? hurtFrames : deathFrames;
            float fps = mode == 1 ? config.hurtFps : config.deathFps;
            float hold = mode == 1 ? config.hurtHold : config.deathHold;
            if (frames == null || frames.Length == 0) { Stop(); return; }

            sr.sprite = frames[EnemyStateLogic.AnimIndex(t, fps, frames.Length, false)];

            if (t >= PlayerFxLogic.Duration(frames.Length, fps, hold))
            {
                // 실제 사망은 PlayerHealth 가 부활시키며 Stop 을 호출한다. 미리보기와 피격은 여기서 종료.
                if (mode == 1 || previewOnly) Stop();
            }
        }

        private void Play(int m, bool preview)
        {
            var frames = m == 1 ? hurtFrames : deathFrames;
            if (frames == null || frames.Length == 0) return;
            if (mode == 2 && m == 1) return;      // 사망 연출이 피격에 밀리지 않게
            mode = m; t = 0f; previewOnly = preview;
            if (anim != null) anim.enabled = false;    // 스프라이트 소유권 확보
            sr.sprite = frames[0];
            bool lockInput = m == 1 ? config.lockInputOnHurt : config.lockInputOnDeath;
            if (lockInput && !preview) PlayerController2D.InputLocked = true;
        }

        private void Stop()
        {
            if (mode == 0) return;
            bool wasLocking = (mode == 1 ? config.lockInputOnHurt : config.lockInputOnDeath) && !previewOnly;
            mode = 0; t = 0f;
            if (anim != null) anim.enabled = true;
            if (wasLocking) PlayerController2D.InputLocked = false;
            previewOnly = false;
        }

        /// 사망 연출 길이(초). PlayerHealth 가 부활 지연을 이 값 이상으로 맞추는 데 쓴다.
        public float DeathDuration
        {
            get
            {
                if (config == null || deathFrames == null) return 0f;
                return PlayerFxLogic.Duration(deathFrames.Length, config.deathFps, config.deathHold);
            }
        }
    }
}
