using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem; // 신규 Input System 네임스페이스 추가

public class PlayerController : MonoBehaviour, IParryReflector
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 7f;

    [Header("Roll Settings")]
    [SerializeField] private float rollSpeed = 10f;       // 구르기 속도
    [SerializeField] private float rollDuration = 0.45f;   // 구르기 지속 시간(초)
    [SerializeField] private float rollCooldown = 0.5f;   // 구르기 재사용 대기시간

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckRadius = 0.2f;

    [Header("Attack Settings")]
    [Tooltip("공격 판정 데미지")]
    [SerializeField] private float attackDamage = 15f;
    [Tooltip("캐릭터 정면 기준 공격 판정이 생기는 위치까지의 거리")]
    [SerializeField] private float attackRange = 1.0f;
    [Tooltip("공격 판정의 반지름")]
    [SerializeField] private float attackRadius = 0.9f;
    [Tooltip("애니메이션 스윙 타이밍에 맞춰 실제 판정이 발생하기까지의 지연시간")]
    [SerializeField] private float attackHitDelay = 0.15f;
    [Tooltip("이 시간 안에 다시 공격하면 콤보(Attack1->2->3)로 이어짐")]
    [SerializeField] private float attackComboWindow = 1.0f;
    [Tooltip("공격 간 최소 간격 (연타 방지)")]
    [SerializeField] private float attackMinInterval = 0.25f;

    [Header("Parry Settings")]
    [Tooltip("Parry 입력 시 이 시간 동안 패링 판정이 유효함")]
    [SerializeField] private float parryWindowDuration = 0.3f;
    [Tooltip("패링 성공 후 다시 패링하기까지 대기시간")]
    [SerializeField] private float parryCooldown = 0.6f;

    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer spriteRenderer;

    private float horizontalInput;
    private bool isGrounded;
    private bool isDead = false;

    // 구르기 관련 변수
    private bool isRolling = false;
    private float rollDirection = 1f;
    private float rollTimer = 0f;
    private float lastRollTime = -999f;

    // 공격 관련 변수
    private int currentAttackCombo = 0;
    private float timeSinceAttack = 999f;

    // 패링 관련 변수
    private float parryTimer = 0f;
    private float lastParryTime = -999f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (isDead) return;

        // 바닥 체크
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        // 구르기 타이머 처리
        if (isRolling)
        {
            rollTimer -= Time.deltaTime;
            if (rollTimer <= 0)
            {
                isRolling = false;
            }
        }

        // 공격/패링 타이머 처리
        timeSinceAttack += Time.deltaTime;
        if (parryTimer > 0f)
            parryTimer -= Time.deltaTime;

        // 스프라이트 반전 및 애니메이션 업데이트
        Flip();
        UpdateAnimations();
    }

    void FixedUpdate()
    {
        if (isDead) return;

        // 구르기 중일 때는 이동 키와 상관없이 구르기 방향으로 빠른 속도 유지
        if (isRolling)
        {
            rb.linearVelocity = new Vector2(rollDirection * rollSpeed, rb.linearVelocity.y);
        }
        else
        {
            // 일반 좌우 물리 이동 처리
            rb.linearVelocity = new Vector2(horizontalInput * moveSpeed, rb.linearVelocity.y);
        }
    }

    #region Input System Event Messages
    // Player Input 컴포넌트의 "OnMove" 이벤트에 의해 자동으로 호출됨
    public void OnMove(InputValue value)
    {
        if (isDead) return;

        Vector2 inputVector = value.Get<Vector2>();
        horizontalInput = inputVector.x;
    }

    // Player Input 컴포넌트의 "OnJump" 이벤트에 의해 자동으로 호출됨
    public void OnJump(InputValue value)
    {
        if (isDead) return;

        // 구르기 중에는 점프 불가
        if (value.isPressed && isGrounded && !isRolling)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }
    }

    // Player Input 컴포넌트의 "OnRoll" 이벤트에 의해 자동으로 호출됨 (C 키 등록 필요)
    public void OnRoll(InputValue value)
    {
        if (isDead) return;

        // 버튼 누름 && 바닥에 있음 && 구르는 중이 아님 && 쿨타임 종료
        if (value.isPressed && isGrounded && !isRolling && Time.time >= lastRollTime + rollCooldown)
        {
            StartRoll();
        }
    }

    // Player Input 컴포넌트의 "OnAttack" 이벤트에 의해 자동으로 호출됨
    // (Input Actions 에셋에 기본 포함된 Attack 액션: 좌클릭 / Enter 등)
    public void OnAttack(InputValue value)
    {
        if (isDead || isRolling) return;
        if (!value.isPressed) return;
        if (timeSinceAttack < attackMinInterval) return;

        currentAttackCombo++;
        if (currentAttackCombo > 3 || timeSinceAttack > attackComboWindow)
            currentAttackCombo = 1;

        anim.SetTrigger("Attack" + currentAttackCombo);
        timeSinceAttack = 0f;

        StartCoroutine(PerformMeleeHit());
    }

    // Player Input 컴포넌트의 "OnParry" 이벤트에 의해 자동으로 호출됨
    // (Input Actions 에셋에 Parry 액션 추가: 우클릭 / Q 키)
    public void OnParry(InputValue value)
    {
        if (isDead || isRolling) return;
        if (!value.isPressed) return;
        if (Time.time < lastParryTime + parryCooldown) return;

        anim.SetTrigger("Block");
        parryTimer = parryWindowDuration;
        lastParryTime = Time.time;
    }
    #endregion

    // 공격 애니메이션 스윙 타이밍에 맞춰 실제 데미지 판정을 수행
    private IEnumerator PerformMeleeHit()
    {
        yield return new WaitForSeconds(attackHitDelay);

        float dir = spriteRenderer.flipX ? -1f : 1f;
        Vector2 origin = (Vector2)transform.position + new Vector2(dir * attackRange, 0.5f);

        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, attackRadius);
        foreach (var hit in hits)
        {
            // 보스 이외의 다른 적이 추가되면 여기에 타입을 늘려주면 됩니다.
            var boss = hit.GetComponent<OrkanBoss>();
            if (boss != null)
            {
                boss.TakeDamage(attackDamage);
            }
        }
    }

    // IParryReflector 구현 - SpikeProjectile 등 보스 투사체가 이 함수를 호출해서
    // 지금이 패링 타이밍인지 물어봅니다.
    public bool TryParry(GameObject attacker)
    {
        return !isDead && parryTimer > 0f;
    }

    private void StartRoll()
    {
        isRolling = true;
        rollTimer = rollDuration;
        lastRollTime = Time.time;

        // 이동 입력 방향이 있다면 그 방향으로, 없으면 캐릭터가 바라보는 방향으로 구름
        if (horizontalInput != 0)
        {
            rollDirection = horizontalInput > 0 ? 1f : -1f;
        }
        else
        {
            rollDirection = spriteRenderer.flipX ? -1f : 1f;
        }

        // 애니메이터 트리거 발동
        anim.SetTrigger("Roll");
    }

    void Flip()
    {
        // 구르는 중간에 바라보는 방향이 뒤집히는 것을 방지
        if (isRolling) return;

        if (horizontalInput > 0)
        {
            spriteRenderer.flipX = false;
        }
        else if (horizontalInput < 0)
        {
            spriteRenderer.flipX = true;
        }
    }

    void UpdateAnimations()
    {
        bool isRunning = Mathf.Abs(horizontalInput) > 0.01f;
        anim.SetBool("IsRunning", isRunning);
        anim.SetBool("IsGrounded", isGrounded);
        anim.SetFloat("YVelocity", rb.linearVelocity.y);
    }

    public void Die()
    {
        if (isDead) return;

        isDead = true;
        rb.linearVelocity = Vector2.zero;
        anim.SetTrigger("Die");
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }

        Gizmos.color = Color.cyan;
        float dir = (spriteRenderer != null && spriteRenderer.flipX) ? -1f : 1f;
        Vector3 origin = transform.position + new Vector3(dir * attackRange, 0.5f, 0f);
        Gizmos.DrawWireSphere(origin, attackRadius);
    }
}
