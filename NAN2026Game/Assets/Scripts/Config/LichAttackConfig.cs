using UnityEngine;

/// <summary>
/// Lich(원거리 캐스터) 전용 구체 투척 1패턴 설정.
/// </summary>
[CreateAssetMenu(fileName = "LichAttackConfig", menuName = "NAN2026/Lich Attack Config")]
public sealed class LichAttackConfig : ScriptableObject
{
    [Tooltip("이 거리 이내일 때만 구체를 발사한다")]
    public float attackRange = 5f;
    [Tooltip("선딜(연출 재생 시간)")]
    public float windup = 0.4f;
    public float orbSpeed = 6f;
    public float orbDamage = 1f;
    public float orbSpawnHeight = 1f;
    public float minCooldown = 1.5f;
    public float maxCooldown = 2.5f;
}
