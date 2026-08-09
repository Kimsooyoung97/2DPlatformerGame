using UnityEngine;
using UnityEngine.InputSystem;

namespace NAN2026
{
    // 발소리(속도 관찰)·점프(입력 관찰)·공격(입력 관찰) — 기존 컨트롤러 무수정
    public class PlayerSoundPlayer : MonoBehaviour
    {
        public SoundConfig config;
        public AudioSource source;
        public AudioSource attackSource; // 검기 전용 (피치 독립)
        Rigidbody2D rb;
        float stepT;
        int stepIdx;

        void Awake() { rb = GetComponent<Rigidbody2D>(); }

        void Update()
        {
            if (config == null || source == null || rb == null) return;

            bool walking = Mathf.Abs(rb.linearVelocity.x) > config.walkVelThreshold
                        && Mathf.Abs(rb.linearVelocity.y) < Mathf.Abs(rb.linearVelocity.x) * 0.8f + 0.1f; // 경사 보행 허용
            if (walking)
            {
                stepT += Time.deltaTime;
                if (stepT >= config.stepInterval && config.walkClips != null && config.walkClips.Length > 0)
                {
                    stepT = 0f;
                    stepIdx = (stepIdx + 1) % config.walkClips.Length;
                    if (config.walkClips[stepIdx] != null)
                        source.PlayOneShot(config.walkClips[stepIdx], config.stepVolume);
                }
            }
            else stepT = config.stepInterval; // 재개 즉시 첫 발소리

            var kb = Keyboard.current;
            if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame) && config.jumpClip != null)
                source.PlayOneShot(config.jumpClip, config.jumpVolume);

            if (kb != null && kb.zKey.wasPressedThisFrame && config.attackClip != null)
            {
                var asrc = attackSource != null ? attackSource : source;
                asrc.pitch = config.attackPitch;
                asrc.PlayOneShot(config.attackClip, config.attackVolume);
            }

            // 대시: Left Shift (공중에서만 실제 발동)
            if (kb != null && kb.leftShiftKey.wasPressedThisFrame && config.dashClip != null)
            {
                source.pitch = config.dashPitch;
                source.PlayOneShot(config.dashClip, config.dashVolume);
                source.pitch = 1f;
            }
        }
    }
}
