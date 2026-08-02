using UnityEngine;

/// <summary>
/// 적 AI(쫄몹/보스 공용) 수치의 단일 기준. EnemyAI/WorldHealthBar는 이 값만 참조하고
/// 숫자 리터럴을 직접 갖지 않는다.
/// </summary>
[CreateAssetMenu(fileName = "EnemyAIConfig", menuName = "NAN2026/Enemy AI Config")]
public sealed class EnemyAIConfig : ScriptableObject
{
    [Header("탐지 범위")]
    [Tooltip("이 거리 안에 플레이어가 들어오면 추적을 시작한다")]
    public float aggroRange = 8f;
    [Tooltip("이 거리 안이면 공격을 시도한다")]
    public float attackRange = 1.5f;
    [Tooltip("추적을 시작한 뒤 이 거리를 넘어서면 추적을 포기하고 순찰로 복귀한다")]
    public float chaseStopDistance = 12f;

    [Header("이동")]
    public float patrolSpeed = 1f;
    public float chaseSpeed = 3f;
    [Tooltip("순찰 지점을 지정하지 않았을 때 스폰 위치 기준 좌우 순찰 반경")]
    public float patrolRadius = 3f;
    [Tooltip("플레이어가 이 높이 이상 위에 있으면 점프해서 따라간다")]
    public float jumpYThreshold = 1.2f;
    [Tooltip("점프를 확정하기 전에 높이차가 유지돼야 하는 시간(초). 짧으면 플레이어의 제자리 점프에도 따라 뛴다")]
    public float jumpConfirmDuration = 0.35f;

    [Header("공격")]
    public float attackCooldown = 1.2f;
    public float attackDamage = 1f;

    [Header("체력바 (UI Canvas 미사용, SpriteRenderer 기반)")]
    public Vector2 healthBarSize = new Vector2(1.2f, 0.16f);
    public Vector3 healthBarOffset = new Vector3(0f, 1.6f, 0f);
    public Color healthBarBackground = new Color(0f, 0f, 0f, 0.75f);
    public Color healthBarFill = new Color(0.85f, 0.15f, 0.15f, 1f);
}
