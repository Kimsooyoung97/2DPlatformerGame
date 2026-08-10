using UnityEngine;
using UnityEngine.InputSystem;
namespace NAN2026
{
    // 발소리(속도 관찰)·점프(입력 관찰)는 기존 방식 유지.
    // 공격(ComboV1/ComboV2)·대시·스킬1/2/3은 각 스크립트의 "실제 발동" 이벤트를 구독한다.
    //   (PlayerController2D.OnAttackPerformed/OnDashPerformed, PlayerSkill.OnSkill1Performed,
    //    SkillSlashCaster.OnSkill2Performed, SkillOrbCaster.OnSkill3Performed)
    // 피격/사망은 같은 오브젝트의 PlayerHealth 인스턴스 이벤트(OnDamaged/OnPlayerDied)를 구독한다.
    // 모두 "실제로 확정된 시점"에만 불리는 이벤트라 연타·중복 트리거로 소리가 겹치지 않는다.
    public class PlayerSoundPlayer : MonoBehaviour
    {
        public SoundConfig config;
        public AudioSource source;
        public AudioSource attackSource; // 검기/스킬 전용 (피치 독립)
        Rigidbody2D rb;
        PlayerHealth health;
        float stepT;
        int stepIdx;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            health = GetComponent<PlayerHealth>();
        }

        void OnEnable()
        {
            PlayerController2D.OnAttackPerformed += HandleAttackPerformed;
            PlayerController2D.OnDashPerformed += HandleDashPerformed;
            PlayerSkill.OnSkill1Performed += HandleSkill1Performed;
            SkillSlashCaster.OnSkill2Performed += HandleSkill2Performed;
            SkillOrbCaster.OnSkill3Performed += HandleSkill3Performed;
            PlayerController2D.OnJumpPerformed += HandleJumpPerformed; // 추가

            if (health != null)
            {
                health.OnDamaged += HandleDamaged;
                health.OnPlayerDied += HandleDied;
            }
        }

        void OnDisable()
        {
            PlayerController2D.OnAttackPerformed -= HandleAttackPerformed;
            PlayerController2D.OnDashPerformed -= HandleDashPerformed;
            PlayerSkill.OnSkill1Performed -= HandleSkill1Performed;
            SkillSlashCaster.OnSkill2Performed -= HandleSkill2Performed;
            SkillOrbCaster.OnSkill3Performed -= HandleSkill3Performed;
            PlayerController2D.OnJumpPerformed -= HandleJumpPerformed; // 추가

            if (health != null)
            {
                health.OnDamaged -= HandleDamaged;
                health.OnPlayerDied -= HandleDied;
            }
        }

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
            if (kb != null && kb.upArrowKey.wasPressedThisFrame && config.jumpClip != null)
                source.PlayOneShot(config.jumpClip, config.jumpVolume);
            // 공격/대시/스킬/피격/사망 사운드는 여기서 키 입력을 보지 않는다 — 아래 이벤트 핸들러 참고
        }

        // 실제로 공격이 발동된 프레임에만 정확히 1번 호출된다 (이펙트 스폰과 동일 시점).
        // ComboV1/ComboV2(Z키 2단 콤보)만 검기 사운드를 낸다.
        void HandleAttackPerformed(string attackName)
        {
            if (config == null || config.attackClip == null) return;
            if (attackName != "ComboV1" && attackName != "ComboV2") return;
            var asrc = attackSource != null ? attackSource : source;
            asrc.pitch = config.attackPitch;
            asrc.PlayOneShot(config.attackClip, config.attackVolume);
        }
        void HandleJumpPerformed()
        {
            if (config == null || config.jumpClip == null || source == null) return;
            source.PlayOneShot(config.jumpClip, config.jumpVolume);
        }
        void HandleDashPerformed()
        {
            if (config == null || config.dashClip == null || source == null) return;
            source.pitch = config.dashPitch;
            source.PlayOneShot(config.dashClip, config.dashVolume);
            source.pitch = 1f;
        }

        // 1키: 내려찍기 스킬 / 2키: 검기 / 3키: 나선환
        void HandleSkill1Performed() => PlaySkillClip(config != null ? config.skill1Clip : null);
        void HandleSkill2Performed() => PlaySkillClip(config != null ? config.skill2Clip : null);
        void HandleSkill3Performed() => PlaySkillClip(config != null ? config.skill3Clip : null);

        void PlaySkillClip(AudioClip clip)
        {
            if (config == null || clip == null) return;
            var asrc = attackSource != null ? attackSource : source;
            asrc.pitch = config.skillPitch;
            asrc.PlayOneShot(clip, config.skillVolume);
        }

        // 모든 무적/그레이스 판정을 통과해 실제로 체력이 깎인 순간에만 호출된다 (회복 시엔 안 불림).
        void HandleDamaged()
        {
            if (config == null || source == null) return;
            var clip = config.RandomClip(config.hitClips);
            if (clip == null) return;
            source.PlayOneShot(clip, config.hitVolume);
        }

        // Kill() 진입 시 정확히 1번 — 전투사·낙사·해저드사 전부 포함.
        void HandleDied()
        {
            if (config == null || source == null) return;
            var clip = config.RandomClip(config.deathClips);
            if (clip == null) return;
            source.PlayOneShot(clip, config.deathVolume);
        }
    }
}