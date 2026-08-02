using UnityEngine;

/// <summary>
/// 플레이어 체력·피격 수치의 단일 기준. PlayerHealth는 이 값만 참조하고
/// 숫자 리터럴을 직접 갖지 않는다 (신규 추가분에 한함 — 기존 필드는 유지).
/// </summary>
[CreateAssetMenu(fileName = "PlayerCombatConfig", menuName = "NAN2026/Player Combat Config")]
public sealed class PlayerCombatConfig : ScriptableObject
{
    [Header("체력")]
    public int maxHealth = 5;

    [Header("피격")]
    [Tooltip("피격 후 이 시간(초) 동안은 다시 데미지를 받지 않는다 (연속 히트 방지)")]
    public float hitInvulnerabilityDuration = 0.6f;
    [Tooltip("피격 시 공격자 반대 방향으로 밀려나는 거리")]
    public float knockbackDistance = 0.25f;
}
