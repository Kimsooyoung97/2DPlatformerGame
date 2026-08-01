using UnityEngine;
using UnityEngine.InputSystem; // 신규 Input System 네임스페이스 추가

public class PlayerController : MonoBehaviour
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
    #endregion

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
    }
}