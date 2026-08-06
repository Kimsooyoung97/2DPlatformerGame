using UnityEngine;
using UnityEngine.InputSystem;
using NAN2026;
using NAN2026.Core;

public class PlayerController2D : MonoBehaviour, IParryReflector
{
    [SerializeField] private MovementConfig config;
    [SerializeField] private AttackEffectConfig effectConfig;
    [SerializeField] private GameObject basicEffectPrefab;
    [SerializeField] private GameObject poweredEffectPrefab;
    [SerializeField] private Sprite[] basicEffectFrames;
    [SerializeField] private Sprite[] poweredEffectFrames;

    [Header("Roll")]
    private float backstepStartTime = -999f;
    private float backstepReadyTime = 0f;
    private bool backstepHopped = false;
    public bool IsBackstepInvincible
    {
        get
        {
            float e = Time.time - backstepStartTime;
            return e >= config.backstepDuration * config.backstepIFrameStartFrac
                && e <  config.backstepDuration * config.backstepIFrameEndFrac;
        }
    }
    [SerializeField] private UnityEngine.Sprite[] comboV1Fx; // V 1타 슬래시(1~5)
    [SerializeField] private UnityEngine.Sprite[] comboV2Fx; // V 2타 슬래시(6~9)
    private int comboVStage = 0;
    private float comboVWindowEnd = 0f;
    private bool comboVBuffered = false;
    [SerializeField] private float rollDuration = 0.75f;
    [SerializeField] private float rollSpeed = 4f;

    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer sr;
    private Collider2D col;
    private PlayerHealth health;
    private PlayerProgression progression;
    private float parryReadyTime = -999f;
    private readonly RaycastHit2D[] castHits = new RaycastHit2D[8];
    private ContactFilter2D groundCastFilter;

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
    private bool dashing;
    private Vector3 dashStartPos;
    private float dashDir;
    private int airDashesUsed;
    private float landTimer;
    private bool launching;
    private float launchEndTime;
    private string currentState;
    public bool IsGrounded { get { return grounded; } }

    /// <summary>지금 이 순간 패링 판정 창 안인지. EnemyAI 등 몬스터 공격 판정이 참조한다.</summary>
    private float EffectiveParryWindow()
    {
        return config.parryWindow + (progression != null ? progression.ParryDurationBonus : 0f);
    }

    /// <summary>JumpZoneLauncher가 호출한다. 정확히 duration초 뒤 target에 도착하도록
    /// 포물선 궤적의 초기 속도를 계산해 물리에 맡긴다(슈퍼점프).</summary>
    public void LaunchTo(Vector3 target, float duration)
    {
        if (duration <= 0f) return;
        float gravity = -Physics2D.gravity.y * rb.gravityScale;
        Vector3 delta = target - transform.position;
        var v = PlayerLocomotionLogic.LaunchVelocityForTarget(delta.x, delta.y, duration, gravity);
        rb.linearVelocity = new Vector2(v.vx, v.vy);
        launching = true;
        launchEndTime = Time.time + duration;
        dashing = false;
        attackTimer = 0f;
        activeAttack = null;
        queuedAttack = null;
    }

    private float EffectiveParryCooldown()
    {
        float reduced = config.parryCooldown - (progression != null ? progression.ParryCooldownReduction : 0f);
        return Mathf.Max(config.parryCooldownMinimum, reduced);
    }

    public bool IsParryWindowActive()
    {
        return parryHeld && PlayerLocomotionLogic.ParrySuccessWindow(Time.time - parryPressTime, EffectiveParryWindow());
    }

    /// <summary>IParryReflector 구현 — SpikeProjectile 등 투사체가 자동으로 패링 여부를 물어본다.</summary>
    public bool TryParry(GameObject attacker)
    {
        if (!IsParryWindowActive()) return false;
        // 방향 정보가 없는 공격(attacker==null)은 안전하게 그냥 허용한다.
        if (attacker == null) return true;
        return PlayerLocomotionLogic.IsAttackerInFront(transform.position.x, attacker.transform.position.x, sr.flipX);
    }
    private UnityEngine.Collider2D[] groundColliders;
    private bool ignoringGround;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        health = GetComponent<PlayerHealth>();
        progression = GetComponent<PlayerProgression>();
        rb.gravityScale = config.gravityScale;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        // 지면 판정용 캐스트에서 트리거(카메라 경계, 볼륨 등)는 제외한다.
        // 트리거가 섞여 들어오면 결과 배열이 오염되어(자리 차지) 정작 진짜 지면 히트가
        // 배열에서 밀려날 수 있고, 트리거의 접촉 법선이 옆방향이라 오판의 원인도 됐다.
        groundCastFilter = new ContactFilter2D();
        groundCastFilter = ContactFilter2D.noFilter;
        groundCastFilter.useTriggers = false;
        groundCastFilter.SetLayerMask(LayerMask.GetMask("Ground", "Wall", "Default"));
        groundCastFilter.useLayerMask = true;
        // 상승 시 충돌 무시는 원웨이 발판(Platform_ 접두)에만 적용한다.
        // 벽·바닥·천장(솔리드 지형)은 항상 충돌 유지 — 전체 무시는 벽 관통·중간 착지 사고의 원인이었다.
        // Stage_Platform(타일맵 원웨이)은 PlatformEffector2D가 전담하므로 여기서도 제외한다.
        var found = new System.Collections.Generic.List<Collider2D>();
        foreach (var tc in FindObjectsByType<UnityEngine.Tilemaps.TilemapCollider2D>())
        {
            if (tc.gameObject.name.StartsWith("Platform_")) found.Add(tc);
        }
        foreach (var cc in FindObjectsByType<CompositeCollider2D>())
        {
            if (cc.gameObject.name.StartsWith("Platform_")) found.Add(cc);
        }
        foreach (var bc in FindObjectsByType<BoxCollider2D>())
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

    // 이동 방향(오른쪽/왼쪽)에 물리적으로 막힌(트리거 아닌) 콜라이더가 있는지 검사한다.
    private bool WallInDirection(Vector2 direction)
    {
        int hitCount = col.Cast(direction, groundCastFilter, castHits, config.wallCheckDistance);
        for (int i = 0; i < hitCount; i++)
        {
            if (castHits[i].collider == null) continue;
            // Physics2D.IgnoreCollision은 물리 밀림(시뮬레이션)만 막을 뿐 이런 캐스트
            // 쿼리에는 영향이 없다. 몬스터는 태그/레이어가 일관되지 않을 수 있어
            // MonsterHealth 보유 여부로 판별해 벽 판정에서 제외한다(몬스터를 밀지도,
            // 몬스터한테 막히지도 않게).
            if (castHits[i].collider.GetComponentInParent<NHNDemo.MonsterHealth>() != null) continue;
            // 위/아래 방향에 가까운 법선(바닥·발판 경사면 등)은 벽으로 취급하지 않는다.
            float absNormalX = Mathf.Abs(castHits[i].normal.x);
            if (absNormalX >= config.wallNormalMinX) return true;
        }
        return false;
    }

    private void Update()
    {
        var kb = Keyboard.current;
        inputX = 0f;
        runHeld = false;
        if (kb != null)
        {
            // 이동은 방향키만 사용한다 (WASD 제거)
            if (kb.leftArrowKey.isPressed) inputX -= 1f;
            if (kb.rightArrowKey.isPressed) inputX += 1f;
            // 기존에 Shift로 홀드해야 하던 달리기를 기본 동작으로 변경 — 방향키만 눌러도 항상 달린다.
            runHeld = true;
            // 점프는 방향키 위쪽만 (Space 제거)
            if (kb.upArrowKey.wasPressedThisFrame) jumpQueued = true;
            // 대쉬(이동기, 공격 아님): Left Shift. 땅에서는 사용할 수 없고 공중에서만
            // 가능하다. 이미 대쉬 중이면 재시작하지 않고, 착지 전까지 maxAirDashes(기본 1회)까지만 허용한다.
            if (kb.leftShiftKey.wasPressedThisFrame && !dashing && !grounded
                && PlayerLocomotionLogic.CanDash(grounded, airDashesUsed, config.maxAirDashes))
            {
                dashing = true;
                dashStartPos = transform.position;
                dashDir = PlayerLocomotionLogic.EffectDirection(sr.flipX);
                airDashesUsed++;
            }
            // 기본 공격: 좌클릭 → Z
            if (kb.zKey.wasPressedThisFrame) QueueAttack("Slash", config.slashDuration, config.slashLungeSpeed);
            // 스킬 공격(구 K) → X
            if (kb.xKey.wasPressedThisFrame) QueueAttack("Combo2", config.combo2Duration, config.combo2LungeSpeed);
            // V 2단 콤보 (이펙트 없음): 1타 Slash모션 → 창 내 재입력 시 2타 Combo2모션
            if (kb.vKey.wasPressedThisFrame)
            {
                if (comboVStage == 1)
                {
                    // 1타 진행/직후 어느 시점이든 2타를 예약 → 프레임 경합 제거
                    comboVBuffered = true;
                }
                else if (attackTimer <= 0f)
                {
                    QueueAttack("ComboV1", config.slashDuration, config.slashLungeSpeed);
                    comboVStage = 1; comboVWindowEnd = 0f; comboVBuffered = false;
                }
            }
            if (kb.lKey.wasPressedThisFrame) QueueAttack("Combo3", config.combo3Duration, config.combo3LungeSpeed);
            // 구르기: G키 제거, Ctrl(좌/우)만 사용. 공중에서는 사용할 수 없다(접지 중에만).
            if (grounded && (kb.leftCtrlKey.wasPressedThisFrame || kb.rightCtrlKey.wasPressedThisFrame))
            {
                bool dirHeld = kb.leftArrowKey.isPressed || kb.rightArrowKey.isPressed;
                if (dirHeld)
                {
                    QueueAttack("Roll", rollDuration, rollSpeed);
                }
                else if (Time.time >= backstepReadyTime)
                {
                    // 방향키 없는 Ctrl = 백스텝 (뒤로 회피, 음수 런지)
                    QueueAttack("Backstep", config.backstepDuration, 0f); // 이동은 자체 창에서
                    backstepStartTime = Time.time;
                    backstepHopped = false;
                    backstepReadyTime = Time.time + config.backstepDuration + config.backstepCooldown;
                }
            }
            // 패링: 마우스 휠클릭 → C
            if (kb.cKey.wasPressedThisFrame && attackTimer <= 0f && Time.time >= parryReadyTime)
            {
                parryHeld = true;
                parryPressTime = Time.time;
                parryReadyTime = Time.time + EffectiveParryCooldown();
            }
            if (kb.cKey.wasReleasedThisFrame && parryHeld)
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
        if (attackName == "ComboV1" || attackName == "ComboV2")
        {
            var fxFrames = attackName == "ComboV1" ? comboV1Fx : comboV2Fx;
            float fxDir = PlayerLocomotionLogic.EffectDirection(sr.flipX);
            Vector3 fxPos = transform.position + new Vector3(config.comboVFxOffsetX * fxDir, config.comboVFxOffsetY, 0f);
            VSlashFx.Play(fxPos, fxFrames, config.comboVFxFps, fxDir < 0f, config.comboVFxScale, config.comboVFxAlpha);
            return;
        }
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
        if (ep != null)
        {
            int baseDamage = AttackDamageLogic.DamageForAttack(attackName, effectConfig.basicDamage, effectConfig.poweredDamage);
            int damageBonus = progression != null ? Mathf.RoundToInt(progression.DamageBonus) : 0;
            float rangeMultiplier = progression != null ? progression.AttackRangeMultiplier : 1f;
            ep.Launch(dir, speed, effectConfig.lifetime * rangeMultiplier, frames, effectConfig.frameRate,
                baseDamage + damageBonus, effectConfig.hitboxSize);
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


        // 점프존 슈퍼점프 비행 중에는 물리 궤적(포물선)에 전부 맡기고 평소 이동/공격
        // 로직을 건너뛴다. 벽 클램프·중력 오버라이드 등과 충돌하면 목표 지점에
        // 정확히 도착하지 못할 수 있기 때문이다.
        if (launching)
        {
            if (Time.time >= launchEndTime)
            {
                rb.linearVelocity = Vector2.zero;
                launching = false;
            }
            return;
        }

        bool wantIgnore = PlayerLocomotionLogic.ShouldIgnoreGround(rb.linearVelocity.y, config.onewayRiseThreshold);
        if (wantIgnore) SetGroundIgnored(true);
        else if (ignoringGround && !OverlappingGround()) SetGroundIgnored(false);

        wasGrounded = grounded;
        grounded = false;
        if (!ignoringGround)
        {
            // 옆 벽에 붙어있을 때(콜라이더가 겹친 상태)도 아래로 스윕한 Cast에 그 벽이
            // 잡힐 수 있다. 접촉면 법선이 충분히 위쪽을 향하는 경우만 '지면'으로 인정해
            // 벽을 지면으로 오판하지 않게 한다 (무한 점프·공중 정지 버그의 원인이었음).
            int hitCount = col.Cast(Vector2.down, groundCastFilter, castHits, config.groundCheckDistance);
            for (int i = 0; i < hitCount; i++)
            {
                if (PlayerLocomotionLogic.IsGroundNormal(castHits[i].normal.y, config.groundNormalMinY))
                {
                    grounded = true;
                    break;
                }
            }
        }
        // 접지 캐스트는 트리거(카메라 경계 등) 무시 — 실지형만 인정
        var groundFilter = new ContactFilter2D();
        groundFilter.useTriggers = false;
        grounded = !ignoringGround && CastGroundNoTriggers() > 0; // 트리거(카메라 경계) 제외
        if (grounded && !wasGrounded) landTimer = config.landDuration;
        if (landTimer > 0f) landTimer -= Time.fixedDeltaTime;
        if (grounded && rb.linearVelocity.y <= 0.01f) { jumpsUsed = 0; airDashesUsed = 0; }

        if (parryEndTimer > 0f) parryEndTimer -= Time.fixedDeltaTime;
        // 패링 판정: 홀드 중 + 판정 창 이내 + 전방 박스에 BossOrb
        if (parryHeld && PlayerLocomotionLogic.ParrySuccessWindow(Time.time - parryPressTime, EffectiveParryWindow()))
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
            // 1타 캔슬 구간 진입 + 2타 예약됨 → 즉시 2타 발동(반응성)
            if (comboVStage == 1 && comboVBuffered && activeAttack == "ComboV1"
                && attackTimer <= config.slashDuration * (1f - config.comboVCancelFrac))
            {
                activeAttack = null; attackTimer = 0f; attacking = false; // attacking도 내려 같은 프레임 2타 소비 허용
                QueueAttack("ComboV2", config.combo2Duration, config.combo2LungeSpeed);
                comboVStage = 0; comboVBuffered = false;
            }
            if (attackTimer <= 0f)
            {
                activeAttack = null;
                if (comboVStage == 1)
                {
                    comboVWindowEnd = Time.time + config.comboVWindow; // 1타 종료 순간부터 0.6초 창 개시
                    if (comboVBuffered)
                    { QueueAttack("ComboV2", config.combo2Duration, config.combo2LungeSpeed); comboVStage = 0; }
                }
                comboVBuffered = false;
            }
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
                if (queuedAttack == "Roll" && health != null)
                    health.BeginRollInvincibility();
            }
            queuedAttack = null;
        }

        if (comboVStage == 1 && comboVWindowEnd > 0f && Time.time > comboVWindowEnd) comboVStage = 0; // comboVStage 만료
        bool parrying = parryHeld || parryEndTimer > 0f;
        float vx = parrying && grounded ? 0f
            : attacking
            ? PlayerLocomotionLogic.AttackVelocity(sr.flipX, activeAttackLunge)
            : PlayerLocomotionLogic.HorizontalVelocity(inputX, runHeld, config.walkSpeed, config.runSpeed);

        // 대쉬(이동기, 공격 시스템과 별개): 최대거리(dashMaxDistance)를 채우거나 벽에
        // 막히면 종료한다. 활성 중에는 평소 이동/공격 속도를 덮어쓴다.
        if (dashing)
        {
            float traveled = Vector3.Distance(dashStartPos, transform.position);
            bool dashWallBlocked = (dashDir > 0f && WallInDirection(Vector2.right)) || (dashDir < 0f && WallInDirection(Vector2.left));
            if (dashWallBlocked || !PlayerLocomotionLogic.DashActive(traveled, config.dashMaxDistance))
            {
                dashing = false;
            }
            else
            {
                vx = dashDir * config.dashSpeed;
            }
        }

        // 벽 쪽으로 velocity를 계속 밀어넣으면(매 프레임 덮어쓰기 방식) 물리 반응이
        // 코너에서 수직 이동까지 간섭하는 경우가 있었다(공중에서 벽을 밀면 안 떨어지는 버그).
        // 그래서 물리 반응에 맡기지 않고, 이동 방향에 벽이 있는지 미리 확인해 그쪽 속도를 0으로 자른다.
        bool blockedRight = !parrying && vx > 0f && WallInDirection(Vector2.right);
        bool blockedLeft = !parrying && vx < 0f && WallInDirection(Vector2.left);
        vx = PlayerLocomotionLogic.ClampHorizontalVelocityAgainstWalls(vx, blockedLeft, blockedRight);

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
    
    
        // 백스텝 이동창 (말미 배치 — 모든 속도 기록 이후 최종 적용)
        // 백스텝 이동창: 3~4프레임 후진, 창 밖 순간정지 (미끄러짐 종결)
        float __bsE = Time.time - backstepStartTime;
        if (__bsE >= 0f && __bsE < config.backstepDuration)
        {
            bool __win = __bsE >= config.backstepDuration * config.backstepMoveStartFrac
                      && __bsE <  config.backstepDuration * config.backstepMoveEndFrac;
            float __vx = __win ? (sr.flipX ? 1f : -1f) * config.backstepSpeed : 0f; // 바라보는 반대로
            float __vy = rb.linearVelocity.y;
            if (__win && !backstepHopped) { backstepHopped = true; __vy = config.backstepHopSpeed; } // 소도약 1회
            rb.linearVelocity = new Vector2(__vx, __vy);
        }
    }

    // 접지 캐스트: 트리거 무시 — 실지형만 인정
    private int CastGroundNoTriggers()
    {
        var f = new ContactFilter2D();
        f.useTriggers = false;
        return col.Cast(Vector2.down, f, castHits, config.groundCheckDistance);
    }

}