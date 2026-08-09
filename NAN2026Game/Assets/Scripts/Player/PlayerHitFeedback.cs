using UnityEngine;
using NAN2026.Core;

namespace NAN2026
{
    /// 플레이어 피격 피드백: 적색 플래시 → 무적 동안 깜빡임 / 히트스톱 / 넉백 / 화면 흔들림.
    /// RealPlayer 프리팹에 부착하면 모든 씬에 자동 적용된다.
    /// 수치는 전부 FeelConfig(SPEC 단일 기준 모듈) 소유.
    public class PlayerHitFeedback : MonoBehaviour
    {
        public FeelConfig feel;
        public Color flashColor = new Color(1f, 0.35f, 0.35f);

        private SpriteRenderer sr;
        private PlayerHealth health;
        private Component impulseSource;      // CinemachineImpulseSource (리플렉션 호출)
        private System.Reflection.MethodInfo generateImpulse;
        private int prevHealth = int.MinValue;

        // 깜빡임
        private float blinkT = -1f;
        // 히트스톱
        private float restoreAt = -1f;
        // 넉백
        private float knockT = -1f, knockSign, knockDist;

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            health = GetComponent<PlayerHealth>();
            foreach (var c in GetComponents<Component>())
            {
                if (c == null) continue;
                if (c.GetType().Name == "CinemachineImpulseSource")
                {
                    impulseSource = c;
                    generateImpulse = c.GetType().GetMethod("GenerateImpulseWithForce", new System.Type[] { typeof(float) })
                                      ?? c.GetType().GetMethod("GenerateImpulse", new System.Type[0]);
                    break;
                }
            }
            if (health != null) health.OnHealthChanged += HandleHealthChanged;
        }

        private void OnDestroy()
        {
            if (health != null) health.OnHealthChanged -= HandleHealthChanged;
            if (restoreAt > 0f) Time.timeScale = 1f;   // 안전핀: 히트스톱 중 소멸해도 시간 복원 (FAIL: timeScale 영구 0)
        }

        private void HandleHealthChanged(int cur, int max)
        {
            if (prevHealth == int.MinValue) { prevHealth = cur; return; }
            bool hurt = PlayerFxLogic.ShouldPlayHurt(prevHealth, cur);
            prevHealth = cur;
            if (!hurt || feel == null) return;
            BeginFeedback();
        }

        private void BeginFeedback()
        {
            blinkT = 0f;

            float stop = HitFeedbackLogic.ClampHitStop(feel.hitStopDuration, feel.invincibilityDuration);
            if (stop > 0f) { Time.timeScale = 0f; restoreAt = Time.unscaledTime + stop; }

            if (feel.knockbackForce > 0f && feel.knockbackDuration > 0f)
            {
                float facing = sr != null && sr.flipX ? -1f : 1f;
                knockSign = HitFeedbackLogic.KnockbackSign(false, transform.position.x, 0f, facing);
                knockDist = feel.knockbackForce;
                knockT = 0f;
            }

            if (generateImpulse != null && feel.screenShakeAmplitude > 0f)
            {
                if (generateImpulse.GetParameters().Length == 1)
                    generateImpulse.Invoke(impulseSource, new object[] { feel.screenShakeAmplitude });
                else
                    generateImpulse.Invoke(impulseSource, new object[0]);
            }
        }

        private void Update()
        {
            // 히트스톱 복구는 unscaled 로 (timeScale 0 에서도 흐른다)
            if (restoreAt > 0f && HitFeedbackLogic.HitStopFinished(Time.unscaledTime, restoreAt))
            { Time.timeScale = 1f; restoreAt = -1f; }

            if (feel == null || sr == null) return;

            // 깜빡임 (무적 시간과 동일 길이)
            if (blinkT >= 0f)
            {
                blinkT += Time.unscaledDeltaTime;
                if (HitFlashBlinker.IsFinished(blinkT, feel.hitFlashDuration))
                {
                    blinkT = -1f;
                    sr.color = Color.white;
                    sr.enabled = true;
                }
                else
                {
                    bool visible = HitFlashBlinker.IsVisible(blinkT, feel.hitFlashInterval);
                    sr.enabled = visible;
                    sr.color = flashColor;
                }
            }

            // 넉백
            if (knockT >= 0f)
            {
                float step = HitFeedbackLogic.KnockbackStep(knockDist, knockT, feel.knockbackDuration, Time.unscaledDeltaTime);
                transform.position += new Vector3(knockSign * step, 0f, 0f);
                knockT += Time.unscaledDeltaTime;
                if (knockT >= feel.knockbackDuration) knockT = -1f;
            }
        }
    }
}
