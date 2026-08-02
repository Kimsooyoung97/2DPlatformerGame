using UnityEngine;
using UnityEngine.InputSystem;
using NAN2026;
using NAN2026.Core;

public class PlayerController2D : MonoBehaviour
{
    [SerializeField] private MovementConfig config;
    [SerializeField] private AttackEffectConfig effectConfig;
    [SerializeField] private GameObject basicEffectPrefab;
    [SerializeField] private GameObject poweredEffectPrefab;
    [SerializeField] private Sprite[] basicEffectFrames;
    [SerializeField] private Sprite[] poweredEffectFrames;

    [Header("Roll")]
    [SerializeField] private float rollDuration = 0.75f;
    [SerializeField] private float rollSpeed = 4f;

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
    private bool parryHeld;
    private float parryEndTimer;
    private float parryPressTime = -999f;
    private readonly Collider2D[] parryHits = new Collider2D[6];
    private bool grounded;
    private bool wasGrounded;
    private int jumpsUsed;
    private float landTimer;
    private string currentState;
    public bool IsGrounded { get { return grounded; } }
    private UnityEngine.Collider2D[] groundColliders;
    private bool ignoringGround;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        rb.gravityScale = config.gravityScale;
        rb.freezeRotation = true;
        // 상승 시 충돌 무시는 원웨이 발판(Platform_ 접두)에만 적용한다.
        // 벽·바닥·천장(솔리드 지형)은 항상 충돌 유지 — 전체 무시는 벽 관통·중간 착지 사고의 원인이었다.
        // Stage_Platform(타일맵 원웨이)은 PlatformEffector2D가 전담하므로 여기서도 제외한다.
        var found = new System.Collections.Generic.List<Collider2D>();
        foreach (var tc in FindObjectsByType<UnityEngine.Tilemaps.TilemapCollider2D>(FindObjectsSortMode.None))
        {
            if (tc.gameObject.name.StartsWith("Platform_")) found.Add(tc);
        }
        foreach (var cc in FindObjectsByType<CompositeCollider2D>(FindObjectsSortMode.None))
        {
            if (cc.gameObject.name.StartsWith("Platform_")) found.Add(cc);
        }
        foreach (var bc in FindObjectsByType<BoxCollider2D>(FindObjectsSortMode.None))
        {
            if (bc.gameObject.name.StartsWith("Platform_")) found.Add(bc);
        }
        groundColliders = found.ToArray();
    }

    private void SetGroundIgnored(bool ignore)
    {
        if (ignoringGround == ignore) return;
        foreach (var g in groundColliders)
        {
            if (g != null && g.enabled && !g.isTrigger) Physics2D.IgnoreCollision(col, g, ignore);
        }
        ignoringGround = ignore;
    }

    private bool OverlappingGround()
    {
        foreach (var g in groundColliders)
        {
            if (g == null || !g.enabled || g.isTrigger) continue;
            if (col.Distance(g).isOverlapped) return true;
        }
        return false;
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
            if (kb.gKey.wasPressedThisFrame) QueueAttack("Roll", rollDuration, rollSpeed);
        }
        if (mouse != null && mouse.leftButton.wasPressedThisFrame) QueueAttack("Slash", config.slashDuration, config.slashLungeSpeed);
        if (mouse != null)
        {
            if (mouse.middleButton.wasPressedThisFrame && attackTimer <= 0f)
            {
                parryHeld = true;
                parryPressTime = Time.time;
            }
            if (mouse.middleButton.wasReleasedThisFrame && parryHeld)
            {
                parryHeld = false;
                parryEndTimer = config.parryEndDuration;
            }
        }

        sr.flipX = PlayerLocomotionLogic.ShouldFlipLeft(inputX, sr.flipX);
        int parryPhase = PlayerLocomotionLogic.ParryPhase(parryHeld, parryEndTimer > 0f);
        string next = parryPhase == 1 ? "ParryStart"
            : parryPhase == 2 ? "ParryEnd"
            : PlayerLocomotionLogic.SelectAnimState(
            activeAttack, grounded, landTimer > 0f, rb.linearVelocity.y, config.apexSpeedThreshold, inputX, runHeld);
        if (next != currentState)
        {
            currentState = next;
            anim.Play(currentState, 0, 0f);
        }
    }

    private void SpawnAttackEffect(string attackName)
    {
        GameObject prefab = null;
        Sprite[] frames = null;
        float speed = 0f;
        float scale = 1f;
        if (attackName == "Slash") { prefab = basicEffectPrefab; frames = basicEffectFrames; speed = effectConfig.basicSpeed; scale = effectConfig.basicScale; }
        else if (attackName == "Combo2") { prefab = poweredEffectPrefab; frames = poweredEffectFrames; speed = effectConfig.poweredSpeed; scale = effectConfig.poweredScale; }
        if (prefab == null || effectConfig == null) return;
        float dir = PlayerLocomotionLogic.EffectDirection(sr.flipX);
        Vector3 pos = transform.position + new Vector3(effectConfig.spawnOffset.x * dir, effectConfig.spawnOffset.y, 0f);
        var go = Instantiate(prefab, pos, Quaternion.identity);
        go.transform.localScale = new Vector3(scale, scale, 1f);
        var ep = go.GetComponent<EffectProjectile>();
        if (ep != null) ep.Launch(dir, speed, effectConfig.lifetime, frames, effectConfig.frameRate);
    }

    private void QueueAttack(string stateName, float duration, float lungeSpeed)
    {
        queuedAttack = stateName;
        queuedAttackDuration = duration;
        queuedAttackLunge = lungeSpeed;
    }

    private void FixedUpdate()
    {
        bool wantIgnore = PlayerLocomotionLogic.ShouldIgnoreGround(rb.linearVelocity.y, config.onewayRiseThreshold);
        if (wantIgnore) SetGroundIgnored(true);
        else if (ignoringGround && !OverlappingGround()) SetGroundIgnored(false);

        wasGrounded = grounded;
        grounded = !ignoringGround && col.Cast(Vector2.down, castHits, config.groundCheckDistance) > 0;
        if (grounded && !wasGrounded) landTimer = config.landDuration;
        if (landTimer > 0f) landTimer -= Time.fixedDeltaTime;
        if (grounded && rb.linearVelocity.y <= 0.01f) jumpsUsed = 0;

        if (parryEndTimer > 0f) parryEndTimer -= Time.fixedDeltaTime;
        // 패링 판정: 홀드 중 + 판정 창 이내 + 전방 박스에 BossOrb
        if (parryHeld && PlayerLocomotionLogic.ParrySuccessWindow(Time.time - parryPressTime, config.parryWindow))
        {
            float pdir = PlayerLocomotionLogic.EffectDirection(sr.flipX);
            Vector2 center = (Vector2)transform.position + new Vector2(config.parryBoxOffsetX * pdir, config.parryBoxSize.y * 0.5f);
            int n = Physics2D.OverlapBoxNonAlloc(center, config.parryBoxSize, 0f, parryHits);
            for (int i = 0; i < n; i++)
            {
                if (parryHits[i] == null) continue;
                var orb = parryHits[i].GetComponent<BossOrb>();
                if (orb != null)
                {
                    int judge = PlayerLocomotionLogic.NoteJudgment(orb.transform.position.x - center.x, config.parryPerfectDistance);
                    FloatingText.Spawn(transform.position + Vector3.up * 1.1f,
                        judge == 0 ? "PERFECT" : "GOOD",
                        judge == 0 ? Color.yellow : Color.white);
                    Destroy(orb.gameObject);
                }
            }
        }

        bool attacking = attackTimer > 0f;
        if (attacking)
        {
            attackTimer -= Time.fixedDeltaTime;
            if (attackTimer <= 0f) activeAttack = null;
        }

        if (queuedAttack != null)
        {
            if (PlayerLocomotionLogic.CanAttack(attacking))
            {
                activeAttack = queuedAttack;
                activeAttackLunge = queuedAttackLunge;
                attackTimer = queuedAttackDuration;
                attacking = true;
                SpawnAttackEffect(queuedAttack);
            }
            queuedAttack = null;
        }

        bool parrying = parryHeld || parryEndTimer > 0f;
        float vx = parrying && grounded ? 0f
            : attacking
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