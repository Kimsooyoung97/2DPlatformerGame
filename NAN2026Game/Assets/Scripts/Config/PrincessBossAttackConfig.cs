using UnityEngine;

/// <summary>
/// Princess_Boss_Knight 전용 3패턴(구체 투척/중범위 공격/전범위 QTE)의 단일 기준.
/// </summary>
[CreateAssetMenu(fileName = "PrincessBossAttackConfig", menuName = "NAN2026/Princess Boss Attack Config")]
public sealed class PrincessBossAttackConfig : ScriptableObject
{
    [Header("패턴 선택")]
    public float minPatternCooldown = 3f;
    public float maxPatternCooldown = 5f;

    [Header("① 구체 투척 (Princess_Trans2)")]
    [Tooltip("선딜(연출 재생 시간)")]
    public float orbWindup = 0.5f;
    [Tooltip("속도가 서로 다른 구체 5개. 개수는 이 배열의 길이를 따른다")]
    public float[] orbSpeeds = new float[] { 4f, 6f, 8f, 10f, 12f };
    public float orbDamage = 1f;
    public float orbSpawnHeight = 1.5f;
    [Tooltip("구체 하나씩 발사되는 간격")]
    public float orbLaunchInterval = 0.12f;

    [Header("② 중범위 공격 (Princess_Trans3)")]
    [Tooltip("선딜(텔레그래프)")]
    public float aoeWindup = 0.6f;
    [Tooltip("보스 정면으로 뻗는 판정 폭")]
    public float aoeForwardRange = 4f;
    [Tooltip("판정 높이")]
    public float aoeHeight = 2.5f;
    public float aoeDamage = 2f;

    [Header("③ 전범위 QTE (Princess_Trans1)")]
    [Tooltip("맞혀야 하는 비트 개수")]
    public int qteBeatCount = 4;
    [Tooltip("비트 사이 간격(초, 일시정지 중에는 실시간 기준으로 흐름)")]
    public float qteBeatInterval = 0.6f;
    [Tooltip("비트당 판정 허용 오차(초)")]
    public float qteHitWindow = 0.18f;
    [Tooltip("QTE 실패 시 데미지(패링 불가)")]
    public float qteFailDamage = 3f;
    [Tooltip("QTE 성공 시 보스가 그로기(무행동)로 묶이는 시간")]
    public float groggyDuration = 3f;
}
