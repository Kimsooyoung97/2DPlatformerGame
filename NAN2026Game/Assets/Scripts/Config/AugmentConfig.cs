using UnityEngine;

public enum AugmentType
{
    ParryCooldownDown,
    ParryDurationUp,
    DamageUp,
    Heal,
    MaxHealthUp,
    AttackRangeUp
}

/// <summary>
/// 증강 6종 × 등급(브론즈/실버/골드) 3단계의 수치 단일 기준.
/// 등급별 배열은 항상 [0]=브론즈, [1]=실버, [2]=골드 순서.
/// </summary>
[CreateAssetMenu(fileName = "AugmentConfig", menuName = "NAN2026/Augment Config")]
public sealed class AugmentConfig : ScriptableObject
{
    [Header("패링 쿨타임 감소 (초, 값만큼 쿨타임에서 차감)")]
    public float[] parryCooldownDownByTier = new float[] { 0.2f, 0.4f, 0.7f };

    [Header("패링 지속시간 증가 (초, parryWindow에 가산)")]
    public float[] parryDurationUpByTier = new float[] { 0.05f, 0.1f, 0.2f };

    [Header("공격 데미지 증가 (정수, 슬래시/콤보 데미지에 가산)")]
    public int[] damageUpByTier = new int[] { 1, 2, 4 };

    [Header("체력 회복 (즉시 1회, 정수 HP)")]
    public int[] healByTier = new int[] { 1, 2, 4 };

    [Header("최대 체력 증가 (정수, 최대 HP에 가산 + 즉시 같은 양만큼 회복)")]
    public int[] maxHealthUpByTier = new int[] { 1, 2, 3 };

    [Header("공격 사거리 증가 (배율 가산, 예: 0.15 = 사거리 15% 증가)")]
    public float[] attackRangeUpByTier = new float[] { 0.15f, 0.3f, 0.5f };

    public float GetMagnitude(AugmentType type, int tier)
    {
        switch (type)
        {
            case AugmentType.ParryCooldownDown: return parryCooldownDownByTier[tier];
            case AugmentType.ParryDurationUp: return parryDurationUpByTier[tier];
            case AugmentType.DamageUp: return damageUpByTier[tier];
            case AugmentType.Heal: return healByTier[tier];
            case AugmentType.MaxHealthUp: return maxHealthUpByTier[tier];
            case AugmentType.AttackRangeUp: return attackRangeUpByTier[tier];
            default: return 0f;
        }
    }
}
