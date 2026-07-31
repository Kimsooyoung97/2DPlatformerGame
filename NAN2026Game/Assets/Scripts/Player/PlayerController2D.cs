using UnityEngine;
using UnityEngine.InputSystem;
using NAN2026;
using NAN2026.Core;

public class PlayerController2D : MonoBehaviour
{
    [SerializeField] private MovementConfig config;

    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer sr;
    private Collider2D col;
    private readonly RaycastHit2D[] castHits = new RaycastHit2D[4];

    private float inputX;
    private bool runHeld;
    private bool jumpQueued;
    private string queuedAttack;
    private float queuedAttackDuration;
    private float queuedAttackLunge;
    private string activeAttack;
    private float activeAttackLunge;
    private float attackTimer;
    private bool grounded;
    private bool wasGrounded;
    private int jumpsUsed;
    private float landTimer;
    private string currentState;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        rb.gravityScale = config.gravityScale;
        rb.freezeRotation = true;
    }

    private void Update()
    {
        var kb = Keyboard.current;
        var mouse = Mouse.current;
        inputX = 0f;
        runHeld = false;
        if (kb != null)
        {
            if (kb.leftArrowKey.isPressed || kb.aKey.isPressed) inputX -= 1f;
            if (kb.rightArrowKey.isPressed || kb.dKey.isPressed) inputX += 1f;
            runHeld = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
            if (kb.spaceKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame) jumpQueued = true;
            if (kb.kKey.wasPressedThisFrame) QueueAttack("Combo2", config.combo2Duration, config.combo2LungeSpeed);
            if (kb.lKey.wasPressedThisFrame) QueueAttack("Combo3", config.combo3Duration, config.combo3LungeSpeed);
        }
        if (mouse != null && mouse.leftButton.wasPressedThisFrame) QueueAttack("Slash", config.slashDuration, config.slashLungeSpeed);

        sr.flipX = PlayerLocomotionLogic.ShouldFlipLeft(inputX, sr.flipX);
        string next = PlayerLocomotionLogic.SelectAnimState(
            activeAttack, grounded, landTimer > 0f, rb.linearVelocity.y, config.apexSpeedThreshold, inputX, runHeld);
        if (next != currentState)
        {
            currentState = next;
            anim.Play(currentState, 0, 0f);
        }
    }

    private void QueueAttack(string stateName, float duration, float lungeSpeed)
    {
        queuedAttack = stateName;
        queuedAttackDuration = duration;
        queuedAttackLunge = lungeSpeed;
    }

    private void FixedUpdate()
    {
        wasGrounded = grounded;
        grounded = col.Cast(Vector2.down, castHits, config.groundCheckDistance) > 0;
        if (grounded && !wasGrounded) landTimer = config.landDuration;
        if (landTimer > 0f) landTimer -= Time.fixedDeltaTime;
        if (grounded && rb.linearVelocity.y <= 0.01f) jumpsUsed = 0;

        bool attacking = attackTimer > 0f;
        if (attacking)
        {
            attackTimer -= Time.fixedDeltaTime;
            if (attackTimer <= 0f) activeAttack = null;
        }

        if (queuedAttack != null)
        {
            if (PlayerLocomotionLogic.CanAttack(grounded, attacking))
            {
                activeAttack = queuedAttack;
                activeAttackLunge = queuedAttackLunge;
                attackTimer = queuedAttackDuration;
                attacking = true;
            }
            queuedAttack = null;
        }

        float vx = attacking && grounded
            ? PlayerLocomotionLogic.AttackVelocity(sr.flipX, activeAttackLunge)
            : PlayerLocomotionLogic.HorizontalVelocity(inputX, runHeld, config.walkSpeed, config.runSpeed);
        float vy = rb.linearVelocity.y;

        if (jumpQueued)
        {
            jumpQueued = false;
            if (PlayerLocomotionLogic.CanJump(attacking, jumpsUsed, config.maxJumps))
            {
                vy = config.jumpVelocity;
                jumpsUsed++;
                landTimer = 0f;
            }
        }
        rb.linearVelocity = new Vector2(vx, vy);
    }
}