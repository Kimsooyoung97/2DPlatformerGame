using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 고대의 가시 기사 오르칸(Orkan) - 1페이즈(체력 100%~50%) 보스 스크립트.
///
/// [핵심 매커니즘: 가시 껍질 &amp; 과부하]
///  - 평소에는 등/정면에 가시 껍질이 있어 받는 데미지가 30%로 감소.
///  - 돌진 공격(가시 껍질 구르기)이 벽에 부딪히면 3초간 Groggy(기절) 상태가 되어
///    배가 노출, 이 동안은 받는 데미지가 200%로 증가.
///  - (확장) 패링/반사 스킬로 투사체를 튕겨 보스에게 맞히면 짧게 껍질이 파괴되어
///    정상 데미지(100%)를 받는 창구가 열림.
///
/// [공격 3종류]
///  1. 기본공격   (BasicAttack)   - 근접 사거리, 철퇴로 바닥을 내리쳐 좌우로 충격파
///  2. 돌진공격   (ChargeAttack)  - 몸을 웅크렸다 직선 돌진, 벽에 부딪히면 Groggy
///  3. 원거리 돌가시 투척공격 (RockSpikeThrow) - 조준해서 돌가시를 연속으로 투척
///     (돌가시 프리팹은 추후 아트가 준비되면 rockSpikePrefab에 할당하면 됩니다.
///      비워두면 임시 오브젝트로 자동 대체됩니다.)
///
/// Animator 파라미터(기존 Boss_0 컨트롤러 기준):
///  - int  State  : 0 = Idle, 1 = Attack, 2 = Move
///  - trigger Groggy
///  - trigger Death
///
/// 2페이즈(체력 50% 이하) 패턴은 아직 컨셉이 전달되지 않아 구현하지 않았습니다.
/// </summary>
[RequireComponent(typeof(Animator))]
public class OrkanBoss : MonoBehaviour
{
    private enum BossState { Idle, Chase, Attack, Groggy, Dead }

    private enum AttackType { BasicAttack, ChargeAttack, RockSpikeThrow }

    [Header("Health / Shell")]
    [SerializeField] private float maxHealth = 1000f;
    [SerializeField] private float currentHealth;
    [Tooltip("평소 가시 껍질 상태에서 받는 데미지 배율 (30% 피해)")]
    [SerializeField] private float shellDamageMultiplier = 0.3f;
    [Tooltip("과부하(Groggy) 상태에서 받는 데미지 배율 (200% 피해)")]
    [SerializeField] private float groggyDamageMultiplier = 2.0f;
    [Tooltip("반사된 투사체에 맞아 껍질이 일시 파괴됐을 때 데미지 배율")]
    [SerializeField] private float shellBrokenDamageMultiplier = 1.0f;
    [SerializeField] private float shellBrokenDuration = 3f;

    [Header("Detection / Movement")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float aggroRange = 9f;
    [SerializeField] private float chaseStopDistance = 1.6f;
    [SerializeField] private float moveSpeed = 2.2f;

    [Header("Attack Loop")]
    [SerializeField] private float minAttackCooldown = 1.2f;
    [SerializeField] private float maxAttackCooldown = 2.2f;
    [Tooltip("이 거리 이하일 때만 기본공격(근접) 사용")]
    [SerializeField] private float meleeRange = 2.6f;
    [Tooltip("이 거리 이상일 때만 돌진/투척 같은 원거리성 패턴 사용")]
    [SerializeField] private float rangedMinDistance = 1.8f;

    [Header("패턴 1 - 기본공격 (가시 철퇴 내리치기)")]
    [SerializeField] private float slamWindup = 1.0f;
    [SerializeField] private float shockwaveSpeed = 7f;
    [SerializeField] private float shockwaveRange = 6f;
    [SerializeField] private float shockwaveDamage = 15f;
    [SerializeField] private GameObject shockwavePrefab; // 비워두면 코드가 임시 오브젝트 생성

    [Header("패턴 2 - 돌진공격 (가시 껍질 구르기)")]
    [SerializeField] private float chargeWindup = 0.7f;
    [SerializeField] private float chargeSpeed = 11f;
    [SerializeField] private float chargeMaxDistance = 10f;
    [SerializeField] private float wallCheckDistance = 0.6f;
    [SerializeField] private LayerMask wallLayerMask; // Inspector에서 Wall(혹은 Ground) 레이어 지정
    [SerializeField] private float chargeDamage = 20f;
    [SerializeField] private float groggyDuration = 3f;

    [Header("패턴 3 - 원거리 돌가시 투척공격")]
    [SerializeField] private float throwWindup = 0.8f;
    [SerializeField] private int throwCount = 3;
    [SerializeField] private float throwInterval = 0.25f;
    [SerializeField] private float rockSpikeSpeed = 9f;
    [SerializeField] private float rockSpikeDamage = 10f;
    [Tooltip("돌가시 프리팹 (추후 아트 적용 예정, 비워두면 임시 오브젝트 사용)")]
    [SerializeField] private GameObject rockSpikePrefab;

    [Header("Feedback")]
    [SerializeField] private Color windupColor = new Color(1f, 0.35f, 0.35f); // 붉은 이펙트 대용
    [SerializeField] private Color groggyColor = new Color(1f, 0.85f, 0.3f);

    private Animator animator;
    private Rigidbody2D body;
    private SpriteRenderer spriteRenderer;
    private Collider2D selfCollider;
    private Transform player;
    private Collider2D playerCollider;

    private BossState state = BossState.Idle;
    private AttackType lastAttack = AttackType.RockSpikeThrow;
    private float attackCooldownTimer = 0f;
    private float groggyTimer = 0f;
    private float shellBrokenTimer = 0f;
    private Color baseColor;

    public bool IsDead => state == BossState.Dead;
    public bool IsGroggy => state == BossState.Groggy;
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;

    void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) baseColor = spriteRenderer.color;

        body = GetComponent<Rigidbody2D>();
        if (body == null)
        {
            body = gameObject.AddComponent<Rigidbody2D>();
        }
        body.gravityScale = 0f;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;

        selfCollider = GetComponent<Collider2D>();
        if (selfCollider == null)
        {
            var box = gameObject.AddComponent<BoxCollider2D>();

            // 스프라이트가 Bottom-pivot이 아니라 Center-pivot이라 고정값(offset 0,1.6)을 쓰면
            // 콜라이더가 실제 캐릭터보다 훨씬 위쪽 허공에 뜨는 문제가 있었음.
            // sprite.bounds는 피벗 기준 로컬 좌표라, 이 값을 그대로 써주면
            // 실제로 눈에 보이는 스프라이트 크기/위치와 정확히 일치하는 콜라이더가 만들어짐.
            if (spriteRenderer != null && spriteRenderer.sprite != null)
            {
                Bounds b = spriteRenderer.sprite.bounds;
                box.size = b.size;
                box.offset = b.center;
            }
            else
            {
                box.size = new Vector2(2.4f, 3.2f);
                box.offset = new Vector2(0f, 0f);
            }
            selfCollider = box;
        }

        // 보스는 자체 중력/지면 충돌이 필요 없고(항상 gravityScale 0, 수평 이동만),
        // 벽 감지는 Raycast로 따로 처리하기 때문에 콜라이더를 Trigger로 둡니다.
        // Trigger가 아니면 (특히 스프라이트 실제 크기로 맞춘 콜라이더가 바닥 타일 콜라이더와
        // 겹치면서) 바닥에 물리적으로 끼어 velocity를 줘도 실제로는 못 움직이는 문제가 생깁니다.
        // 근접 공격 판정(OverlapCircleAll)이나 Raycast는 Trigger 여부와 상관없이 정상 동작합니다.
        selfCollider.isTrigger = true;

        currentHealth = maxHealth;

        var bar = gameObject.AddComponent<HealthBarUI>();
        bar.Init(() => currentHealth, () => maxHealth, Color.red);
    }

    void Start()
    {
        var playerObj = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerCollider = playerObj.GetComponent<Collider2D>();
            IgnorePlayerCollision();
        }
    }

    // 플레이어와 보스가 서로 몸으로 밀리거나 막히지 않고 통과하도록 물리 충돌만 무시.
    // (바닥/벽 등 다른 콜라이더와의 충돌에는 영향 없음)
    private void IgnorePlayerCollision()
    {
        if (selfCollider != null && playerCollider != null)
            Physics2D.IgnoreCollision(selfCollider, playerCollider, true);
    }

    void Update()
    {
        if (state == BossState.Dead) return;

        if (player == null)
        {
            var playerObj = GameObject.FindGameObjectWithTag(playerTag);
            if (playerObj != null)
            {
                player = playerObj.transform;
                playerCollider = playerObj.GetComponent<Collider2D>();
                IgnorePlayerCollision();
            }
        }

        if (shellBrokenTimer > 0f)
        {
            shellBrokenTimer -= Time.deltaTime;
        }

        switch (state)
        {
            case BossState.Groggy:
                TickGroggy();
                break;
            case BossState.Attack:
                // 공격 코루틴이 상태 복귀까지 전부 처리
                break;
            default:
                TickIdleAndChase();
                break;
        }
    }

    // ------------------------------------------------------------------
    // 기본 행동 (대기 / 추격 / 공격 선택)
    // ------------------------------------------------------------------
    private void TickIdleAndChase()
    {
        if (attackCooldownTimer > 0f)
            attackCooldownTimer -= Time.deltaTime;

        if (player == null)
        {
            SetMoving(false);
            SetBossAnimState(0);
            return;
        }

        FacePlayer();

        float distance = Mathf.Abs(player.position.x - transform.position.x);

        if (distance > aggroRange)
        {
            SetMoving(false);
            SetBossAnimState(0); // Idle
            return;
        }

        if (attackCooldownTimer <= 0f)
        {
            AttackType next = ChooseNextAttack(distance);
            StartCoroutine(RunAttack(next));
            return;
        }

        if (distance > chaseStopDistance)
        {
            state = BossState.Chase;
            SetMoving(true);
            SetBossAnimState(2); // Move
            float dir = Mathf.Sign(player.position.x - transform.position.x);
            body.linearVelocity = new Vector2(dir * moveSpeed, body.linearVelocity.y);
        }
        else
        {
            state = BossState.Idle;
            SetMoving(false);
            SetBossAnimState(0); // Idle
        }
    }

    // 플레이어와의 거리에 맞는 공격만 후보로 두고, 직전 패턴은 되도록 피해서 무작위 선택.
    private AttackType ChooseNextAttack(float distance)
    {
        var valid = new List<AttackType>();

        if (distance <= meleeRange)
            valid.Add(AttackType.BasicAttack);

        if (distance >= rangedMinDistance)
        {
            valid.Add(AttackType.ChargeAttack);
            valid.Add(AttackType.RockSpikeThrow);
        }

        if (valid.Count == 0)
            valid.Add(AttackType.BasicAttack);

        var filtered = valid.FindAll(a => a != lastAttack);
        if (filtered.Count == 0) filtered = valid;

        var choice = filtered[Random.Range(0, filtered.Count)];
        lastAttack = choice;
        return choice;
    }

    private void FacePlayer()
    {
        if (player == null || spriteRenderer == null) return;
        spriteRenderer.flipX = player.position.x < transform.position.x;
    }

    private void SetMoving(bool moving)
    {
        if (!moving && body != null)
            body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
    }

    private void SetBossAnimState(int stateValue)
    {
        if (animator != null) animator.SetInteger("State", stateValue);
    }

    // ------------------------------------------------------------------
    // 공격 실행
    // ------------------------------------------------------------------
    private IEnumerator RunAttack(AttackType type)
    {
        state = BossState.Attack;
        SetMoving(false);
        SetBossAnimState(1); // Attack

        switch (type)
        {
            case AttackType.BasicAttack:
                yield return StartCoroutine(DoBasicAttack());
                break;
            case AttackType.ChargeAttack:
                yield return StartCoroutine(DoChargeAttack());
                break;
            case AttackType.RockSpikeThrow:
                yield return StartCoroutine(DoRockSpikeThrow());
                break;
        }

        // Groggy로 상태가 바뀌었으면 공격 종료 처리에서 쿨다운/Idle 복귀를 건드리지 않는다.
        if (state == BossState.Attack)
        {
            state = BossState.Idle;
            SetBossAnimState(0);
            attackCooldownTimer = Random.Range(minAttackCooldown, maxAttackCooldown);
        }
    }

    // 1) 기본공격 - 가시 철퇴 내리치기: 1초 선딜 후 좌우로 충격파
    private IEnumerator DoBasicAttack()
    {
        yield return StartCoroutine(FlashColor(windupColor, slamWindup));

        SpawnShockwave(1);
        SpawnShockwave(-1);

        yield return new WaitForSeconds(0.4f); // 후딜(recovery)
    }

    private void SpawnShockwave(int direction)
    {
        GameObject go = shockwavePrefab != null
            ? Instantiate(shockwavePrefab, transform.position, Quaternion.identity)
            : new GameObject("SpikeShockwave");

        if (shockwavePrefab == null)
            go.transform.position = transform.position;

        var wave = go.GetComponent<SpikeShockwave>();
        if (wave == null) wave = go.AddComponent<SpikeShockwave>();
        wave.Init(direction, shockwaveSpeed, shockwaveRange, shockwaveDamage);
    }

    // 2) 돌진공격 - 가시 껍질 구르기: 웅크림 선딜 -> 직선 돌진 -> 벽 충돌 시 Groggy
    private IEnumerator DoChargeAttack()
    {
        yield return StartCoroutine(FlashColor(windupColor, chargeWindup));

        if (player == null) yield break;

        float chargeDir = Mathf.Sign(player.position.x - transform.position.x);
        Vector3 startPos = transform.position;
        bool hitWall = false;
        bool hitPlayerOnce = false;

        while (Vector3.Distance(startPos, transform.position) < chargeMaxDistance)
        {
            // 벽 감지 (Inspector에서 wallLayerMask에 Wall/Ground 레이어 지정 필요)
            RaycastHit2D hit = Physics2D.Raycast(transform.position, new Vector2(chargeDir, 0f), wallCheckDistance, wallLayerMask);
            if (hit.collider != null)
            {
                hitWall = true;
                break;
            }

            body.linearVelocity = new Vector2(chargeDir * chargeSpeed, body.linearVelocity.y);

            // 돌진 중 플레이어와 겹치면 데미지 (플레이어를 뚫고 계속 돌진)
            if (!hitPlayerOnce && player != null &&
                Vector3.Distance(player.position, transform.position) < 1.0f)
            {
                var hp = player.GetComponent<PlayerHealth>();
                if (hp != null) hp.TakeDamage(chargeDamage, transform.position);
                hitPlayerOnce = true;
            }

            yield return null;
        }

        body.linearVelocity = new Vector2(0f, body.linearVelocity.y);

        if (hitWall)
        {
            EnterGroggy(groggyDuration);
        }
        else
        {
            yield return new WaitForSeconds(0.3f); // 후딜
        }
    }

    // 3) 원거리 돌가시 투척공격
    private IEnumerator DoRockSpikeThrow()
    {
        yield return StartCoroutine(FlashColor(windupColor, throwWindup));

        for (int i = 0; i < throwCount; i++)
        {
            ThrowRockSpike();
            yield return new WaitForSeconds(throwInterval);
        }

        yield return new WaitForSeconds(0.3f); // 후딜
    }

    private void ThrowRockSpike()
    {
        if (player == null) return;

        Vector3 spawnPos = transform.position + Vector3.up * 1.6f;
        Vector2 dir = ((Vector2)player.position - (Vector2)spawnPos).normalized;

        GameObject go = rockSpikePrefab != null
            ? Instantiate(rockSpikePrefab, spawnPos, Quaternion.identity)
            : new GameObject("SpikeProjectile");

        if (rockSpikePrefab == null)
            go.transform.position = spawnPos;

        var proj = go.GetComponent<SpikeProjectile>();
        if (proj == null) proj = go.AddComponent<SpikeProjectile>();
        proj.Init(dir, rockSpikeSpeed, rockSpikeDamage, this);
    }

    // ------------------------------------------------------------------
    // Groggy(과부하) 상태
    // ------------------------------------------------------------------
    private void EnterGroggy(float duration)
    {
        StopAllCoroutines();
        state = BossState.Groggy;
        groggyTimer = duration;
        SetMoving(false);
        if (animator != null) animator.SetTrigger("Groggy");
        if (spriteRenderer != null) spriteRenderer.color = groggyColor;
    }

    private void TickGroggy()
    {
        groggyTimer -= Time.deltaTime;
        if (groggyTimer <= 0f)
        {
            state = BossState.Idle;
            SetBossAnimState(0);
            attackCooldownTimer = Random.Range(minAttackCooldown, maxAttackCooldown);
            if (spriteRenderer != null) spriteRenderer.color = baseColor;
        }
    }

    // 패링으로 반사된 투사체가 보스 본체에 명중했을 때 (SpikeProjectile에서 호출)
    public void OnProjectileReflectedHit()
    {
        shellBrokenTimer = shellBrokenDuration;
    }

    // ------------------------------------------------------------------
    // 피격 / 사망
    // ------------------------------------------------------------------
    public void TakeDamage(float rawDamage)
    {
        if (state == BossState.Dead) return;

        float multiplier;
        if (state == BossState.Groggy)
            multiplier = groggyDamageMultiplier;
        else if (shellBrokenTimer > 0f)
            multiplier = shellBrokenDamageMultiplier;
        else
            multiplier = shellDamageMultiplier;

        float finalDamage = rawDamage * multiplier;
        currentHealth -= finalDamage;

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        StopAllCoroutines();
        state = BossState.Dead;
        SetMoving(false);
        if (animator != null) animator.SetTrigger("Death");
        if (selfCollider != null) selfCollider.enabled = false;
        body.linearVelocity = Vector2.zero;
        body.bodyType = RigidbodyType2D.Kinematic;
    }

    private IEnumerator FlashColor(Color flashColor, float duration)
    {
        if (spriteRenderer == null)
        {
            yield return new WaitForSeconds(duration);
            yield break;
        }

        spriteRenderer.color = flashColor;
        yield return new WaitForSeconds(duration);
        if (state != BossState.Groggy) // Groggy로 인터럽트됐으면 groggyColor를 덮어쓰지 않음
            spriteRenderer.color = baseColor;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, aggroRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, chaseStopDistance);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, meleeRange);
    }
}
