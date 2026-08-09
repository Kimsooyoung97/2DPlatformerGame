using UnityEngine;

public enum AugmentType
{
    DamageUp,
    Heal,
    ManaHeal,
    ManaUp,
    UnlockSkill1, // 기존 Z키(구 Slash) 공격이 스킬화된 것 — 처음 얻는 스킬이면 SkillImage도 갱신됨
    UnlockSkill2  // 기존 X키(구 Combo2) 공격이 스킬화된 것
}

/// <summary>
/// 증강 6종 × 등급(브론즈/실버/골드) 3단계의 수치 단일 기준.
/// 등급별 배열은 항상 [0]=브론즈, [1]=실버, [2]=골드 순서.
/// </summary>
[CreateAssetMenu(fileName = "AugmentConfig", menuName = "NAN2026/Augment Config")]
public sealed class AugmentConfig : ScriptableObject
{
    [Header("공격 데미지 증가 (정수, 슬래시/콤보 데미지에 가산)")]
    public int[] damageUpByTier = new int[] { 1, 2, 3 };

    [Header("체력 회복 (즉시 1회, 정수 HP)")]
    public int[] healByTier = new int[] { 1, 2, 3 };

    [Header("패링 쿨타임 감소 (초, 값만큼 쿨타임에서 차감)")]
    public float[] manaHealByTier = new float[] { 1, 2, 3 };

    [Header("패링 지속시간 증가 (초, parryWindow에 가산)")]
    public float[] manaUpByTier = new float[] { 1, 1, 1 };
    public float GetMagnitude(AugmentType type, int tier)
    {
        switch (type)
        {
            case AugmentType.DamageUp: return damageUpByTier[tier];
            case AugmentType.Heal: return healByTier[tier];
            case AugmentType.ManaHeal: return manaHealByTier[tier];
            case AugmentType.ManaUp: return manaUpByTier[tier];
            default: return 0f;
        }
    }
}
