using UnityEngine;

/// <summary>
/// 타격감 관련 수치의 단일 기준. MonoBehaviour는 이 에셋을 참조하고
/// 숫자 리터럴을 직접 갖지 않는다. (SPEC.md — 단일 기준 모듈)
/// 값은 S1 조작감 판정에서 확정한다.
/// </summary>
[CreateAssetMenu(fileName = "FeelConfig", menuName = "NAN2026/Feel Config")]
public class FeelConfig : ScriptableObject
{
    [Header("히트스톱")]
    [Tooltip("피격 성립 시 시간이 멈추는 길이(초)")]
    public float hitStopDuration;

    [Header("넉백")]
    [Tooltip("피격 대상이 밀려나는 세기")]
    public float knockbackForce;

    [Tooltip("넉백이 유지되는 길이(초)")]
    public float knockbackDuration;

    [Header("무적")]
    [Tooltip("피격 후 무적이 지속되는 길이(초)")]
    public float invincibilityDuration;

    [Tooltip("대시 중 무적이 지속되는 길이(초)")]
    public float dashInvincibilityDuration;

    [Header("화면 흔들림")]
    [Tooltip("카메라가 흔들리는 진폭")]
    public float screenShakeAmplitude;

    [Tooltip("화면 흔들림이 지속되는 길이(초)")]
    public float screenShakeDuration;

    [Header("공격 딜레이")]
    [Tooltip("입력 후 판정이 나가기까지의 선딜(초)")]
    public float attackStartupTime;

    [Tooltip("판정 종료 후 다음 행동까지의 후딜(초)")]
    public float attackRecoveryTime;

    [Header("입력 버퍼")]
    [Tooltip("선입력을 기억하는 길이(초)")]
    public float inputBufferTime;

    [Header("피격 깜빡임")]
    [Tooltip("피격 후 깜빡임이 지속되는 길이(초)")]
    public float hitFlashDuration;

    [Tooltip("보임/숨김이 뒤집히는 간격(초)")]
    public float hitFlashInterval;
}
