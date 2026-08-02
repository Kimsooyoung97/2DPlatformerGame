using UnityEngine;

/// <summary>
/// MiddleBoss의 돌진/투사체 공격 패턴 수치. MiddleBossAttackPatterns는 이 값만 참조하고
/// 숫자 리터럴을 직접 갖지 않는다.
/// </summary>
[CreateAssetMenu(fileName = "MiddleBossAttackConfig", menuName = "NAN2026/Middle Boss Attack Config")]
public sealed class MiddleBossAttackConfig : ScriptableObject
{
    [Header("패턴 선택")]
    [Tooltip("이 거리 이상일 때만 돌진/투사체를 쓴다. 더 가까우면 EnemyAI 기본 근접 공격에 맡긴다")]
    public float rangedMinDistance = 2.5f;
    [Tooltip("패턴 실행 후 다음 패턴까지 최소 대기 시간")]
    public float minPatternCooldown = 2.5f;
    [Tooltip("패턴 실행 후 다음 패턴까지 최대 대기 시간")]
    public float maxPatternCooldown = 4.5f;

    [Header("돌진 공격")]
    public float chargeWindup = 0.5f;
    public float chargeSpeed = 9f;
    public float chargeMaxDistance = 8f;
    [Tooltip("이 거리 안으로 들어오면 돌진 중 플레이어에게 명중 처리")]
    public float chargeHitDistance = 1.0f;
    public float chargeDamage = 2f;
    [Tooltip("벽 감지용 레이캐스트 거리")]
    public float wallCheckDistance = 0.6f;
    [Tooltip("레이캐스트 시작점을 자기 몸 밖으로 미리 밀어내는 거리 (자기 자신의 콜라이더를 벽으로 오인하는 것 방지)")]
    public float wallCheckOriginOffset = 0.5f;
    public LayerMask wallLayerMask;

    [Header("투사체 공격")]
    public float throwWindup = 0.6f;
    public int throwCount = 3;
    public float throwInterval = 0.25f;
    public float throwSpawnHeight = 1.2f;
    public float rockSpikeSpeed = 8f;
    public float rockSpikeDamage = 1f;
}
