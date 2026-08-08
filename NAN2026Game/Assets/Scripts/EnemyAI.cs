using UnityEngine;
using NAN2026.Core;
using NAN2026.Showroom;
using Assets.PixelFantasy.PixelMonsters.Common.Scripts.ExampleScripts;

/// <summary>
/// 씬에 미리 배치된(동적 생성 아님) 적 유닛 공용 AI.
/// 순찰(usePatrol=true인 경우) → aggroRange 진입 시 추적 → attackRange 진입 시 공격.
/// 플레이어가 위 층에 있으면(jumpYThreshold 이상) 점프해서 따라간다.
/// 이동/점프 물리는 기존 MonsterController2D를, 애니메이션 트리거는 MonsterAnimation을 그대로 재사용한다.
/// 모든 수치는 EnemyAIConfig가 소유하며 이 클래스에 숫자 리터럴은 없다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(MonsterController2D))]
[RequireComponent(typeof(MonsterAnimation))]
public sealed class EnemyAI : MonoBehaviour
{
    [Header("설정 (필수)")]
    [SerializeField] private EnemyAIConfig config;

    [Header("순찰 지점 (비우면 스폰 위치 기준 config.patrolRadius로 자동 생성)")]
    [SerializeField] private Transform patrolPointA;
    [SerializeField] private Transform patrolPointB;

    [Header("순찰 사용 여부 (보스류는 끄고 제자리 대기만 하게 둘 수 있다)")]
    [SerializeField] private bool usePatrol = true;

    [Header("플레이어 태그")]
    [SerializeField] private string playerTag = "Player";

    private MonsterController2D controller;
    private MonsterAnimation animation;
    private Transform player;
    private IEnemyAttackOverride attackOverride;
    private NHNDemo.MonsterHealth selfHealthForXp;

    private EnemyAIState state = EnemyAIState.Patrol;
    private bool engaged;
    private float patrolDir = 1f;
    private float attackTimer;
    private float leftBoundX;
    private float rightBoundX;
    private float heightGapTimer;

    public EnemyAIState CurrentState => state;

    private void Awake()
    {
        controller = GetComponent<MonsterController2D>();
        animation = GetComponent<MonsterAnimation>();
        attackOverride = GetComponent<IEnemyAttackOverride>();

        // 씬 편집용 데모 컨트롤(키보드 입력) 스크립트가 같이 있으면 Input을 서로 덮어써서
        // 충돌하므로, AI가 대신 조종함을 명시적으로 끈다. (컴포넌트 자체는 지우지 않는다)
        MonsterControls demoControls = GetComponent<MonsterControls>();
        if (demoControls != null) demoControls.enabled = false;

        RecomputePatrolBounds();

        GameObject playerGO = GameObject.FindGameObjectWithTag(playerTag);
        if (playerGO != null)
        {
            player = playerGO.transform;
            IgnorePlayerPhysicalCollision(playerGO);
        }

        IgnoreOtherMonstersPhysicalCollision();

        selfHealthForXp = GetComponent<NHNDemo.MonsterHealth>();
        if (selfHealthForXp != null)
        {
            selfHealthForXp.OnDied += HandleDied;
            if (config != null) selfHealthForXp.SetMaxHealth(config.maxHealth);
        }
    }

    private void OnDestroy()
    {
        if (selfHealthForXp != null) selfHealthForXp.OnDied -= HandleDied;
    }

    private void HandleDied()
    {
        if (player == null || config == null) return;
        PlayerProgression progression = player.GetComponentInParent<PlayerProgression>();
        if (progression != null) progression.AddXp(config.xpReward);
    }

    // 몬스터와 플레이어가 서로 몸으로 밀거나 막지 않고 통과하도록 물리 충돌만 무시한다.
    // (바닥/벽/공격 판정용 트리거 콜라이더에는 영향 없음)
    private void IgnorePlayerPhysicalCollision(GameObject playerGO)
    {
        Collider2D selfCollider = GetComponent<Collider2D>();
        Collider2D playerCollider = playerGO.GetComponent<Collider2D>();
        if (selfCollider != null && playerCollider != null)
            Physics2D.IgnoreCollision(selfCollider, playerCollider, true);
    }

    // 몬스터끼리도 서로 몸으로 밀거나 막지 않도록 물리 충돌을 무시한다.
    // EnemyAI를 가진 모든 몬스터(DeathDog/Lich/보스 등 종류 불문)끼리 전부 적용된다.
    private void IgnoreOtherMonstersPhysicalCollision()
    {
        Collider2D selfCollider = GetComponent<Collider2D>();
        if (selfCollider == null) return;

        EnemyAI[] others = FindObjectsByType<EnemyAI>();
        foreach (EnemyAI other in others)
        {
            if (other == this) continue;
            Collider2D otherCollider = other.GetComponent<Collider2D>();
            if (otherCollider != null)
                Physics2D.IgnoreCollision(selfCollider, otherCollider, true);
        }
    }

    private void RecomputePatrolBounds()
    {
        if (patrolPointA != null && patrolPointB != null)
        {
            leftBoundX = Mathf.Min(patrolPointA.position.x, patrolPointB.position.x);
            rightBoundX = Mathf.Max(patrolPointA.position.x, patrolPointB.position.x);
        }
        else
        {
            float radius = config != null ? config.patrolRadius : 0f;
            leftBoundX = transform.position.x - radius;
            rightBoundX = transform.position.x + radius;
        }
    }

    private void Update()
    {
        if (config == null || controller == null || animation == null)
            return;

        if (attackOverride != null && attackOverride.IsBusy)
        {
            controller.Input = Vector2.zero;
            return;
        }

        if (attackTimer > 0f)
            attackTimer -= Time.deltaTime;

        if (player == null)
        {
            GameObject playerGO = GameObject.FindGameObjectWithTag(playerTag);
            if (playerGO != null) player = playerGO.transform;
        }

        if (player == null)
        {
            Patrol();
            return;
        }

        float distance = Mathf.Abs(player.position.x - transform.position.x);
        state = EnemyAILogic.DetermineState(distance, engaged, config.aggroRange, config.attackRange, config.chaseStopDistance);
        engaged = state != EnemyAIState.Patrol;

        switch (state)
        {
            case EnemyAIState.Chase:
                Chase();
                break;
            case EnemyAIState.Attack:
                AttackPlayer();
                break;
            default:
                Patrol();
                break;
        }
    }

    private void Patrol()
    {
        heightGapTimer = 0f;

        if (!usePatrol)
        {
            controller.Input = Vector2.zero;
            return;
        }

        patrolDir = EnemyAILogic.PatrolDirection(transform.position.x, leftBoundX, rightBoundX, patrolDir);
        controller.Input = new Vector2(patrolDir, 0f);
    }

    private void Chase()
    {
        // 보스류는 추적 중에도 돌진/투사체 같은 원거리성 패턴을 끼워 넣을 수 있다.
        if (attackOverride != null && attackOverride.TryStartAttack(player))
            return;

        float dir = player.position.x > transform.position.x ? 1f : -1f;

        // 높이차를 매 프레임 즉시 점프 판정에 쓰면, 플레이어가 같은 층에서 제자리
        // 점프만 해도 그 순간의 높이차 때문에 따라 뛰게 된다. jumpConfirmDuration만큼
        // 높이차가 '유지'된 경우에만 실제로 위층에 있다고 보고 점프한다.
        bool aboveThresholdNow = (player.position.y - transform.position.y) >= config.jumpYThreshold;
        heightGapTimer = EnemyAILogic.UpdateHeightGapTimer(aboveThresholdNow, heightGapTimer, Time.deltaTime);
        bool needsJump = controller.IsGrounded && EnemyAILogic.ShouldJumpNow(heightGapTimer, config.jumpConfirmDuration);

        controller.Input = new Vector2(dir, needsJump ? 1f : 0f);
    }

    private void AttackPlayer()
    {
        heightGapTimer = 0f;
        controller.Input = Vector2.zero;

        if (attackTimer > 0f)
            return;

        attackTimer = config.attackCooldown;

        if (attackOverride != null && attackOverride.TryStartAttack(player))
            return;

        animation.Attack();

        if (player == null) return;

        // 패링 성공 시: 플레이어는 무피해, 공격한 이 몬스터가 대신 반격 데미지를 받는다.
        IParryReflector reflector = player.GetComponentInParent<IParryReflector>();
        if (reflector != null && reflector.TryParry(gameObject))
        {
            NAN2026.PlayerMana.RewardParry(player);
            NHNDemo.MonsterHealth selfHealth = GetComponent<NHNDemo.MonsterHealth>();
            PlayerHealth parriedPh = player.GetComponentInParent<PlayerHealth>();
            int counterDamage = parriedPh != null ? parriedPh.ParryCounterDamage : 0;
            if (selfHealth != null && counterDamage > 0)
                selfHealth.TakeDamage(counterDamage, (Vector2)(transform.position - player.position));
            return;
        }

        PlayerHealth playerHealth = player.GetComponentInParent<PlayerHealth>();
        if (playerHealth != null)
            playerHealth.TakeDamage(config.attackDamage);
    }

    private void OnDrawGizmosSelected()
    {
        if (config == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, config.aggroRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, config.attackRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, config.chaseStopDistance);

        if (!usePatrol) return;

        float lx = leftBoundX, rx = rightBoundX;
        if (!Application.isPlaying)
        {
            if (patrolPointA != null && patrolPointB != null)
            {
                lx = Mathf.Min(patrolPointA.position.x, patrolPointB.position.x);
                rx = Mathf.Max(patrolPointA.position.x, patrolPointB.position.x);
            }
            else
            {
                lx = transform.position.x - config.patrolRadius;
                rx = transform.position.x + config.patrolRadius;
            }
        }

        Gizmos.color = Color.green;
        Vector3 a = new Vector3(lx, transform.position.y, 0f);
        Vector3 b = new Vector3(rx, transform.position.y, 0f);
        Gizmos.DrawLine(a, b);
        Gizmos.DrawSphere(a, 0.15f);
        Gizmos.DrawSphere(b, 0.15f);
    }
}
