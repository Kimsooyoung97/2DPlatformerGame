using UnityEngine;

/// <summary>
/// 레벨업 경험치 곡선 + 증강 등급(브론즈/실버/골드) 확률의 단일 기준.
/// </summary>
[CreateAssetMenu(fileName = "LevelProgressionConfig", menuName = "NAN2026/Level Progression Config")]
public sealed class LevelProgressionConfig : ScriptableObject
{
    [Header("경험치 곡선")]
    [Tooltip("1레벨에서 2레벨로 가는 데 필요한 경험치")]
    public int baseXpToLevel2 = 10;
    [Tooltip("레벨이 오를 때마다 다음 레벨에 필요한 경험치가 늘어나는 양")]
    public int xpIncrementPerLevel = 5;

    [Header("증강 등급 확률 (레벨에 따라 상승)")]
    public float goldBaseChance = 0.1f;
    public float goldChancePerLevel = 0.02f;
    public float goldMaxChance = 0.4f;
    public float silverBaseChance = 0.25f;
    public float silverChancePerLevel = 0.02f;
    public float silverMaxChance = 0.45f;

    [Header("레벨업 시 제시할 증강 선택지 수")]
    public int choicesPerLevelUp = 3;
}
