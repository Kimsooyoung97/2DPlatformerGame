using System.Collections.Generic;
using NAN2026;
using NAN2026.Showroom;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// 플레이어 HP. 전역 네임스페이스 — 팀 스크립트(OrkanBoss·Spike·Checkpoint2D·OrbProjectile) 계약 준수.
// 사망: 체크포인트 있으면 그 지점 부활, 없으면 씬 재시작 (SPEC: 죽으면 처음부터)
public class PlayerHealth : MonoBehaviour
{
    [Header("Testing")]
    [Tooltip("While on, hazards cannot kill. Toggle in play mode with F2.")]
    [SerializeField] private bool invincible = false;

    [Header("Death")]
    [SerializeField] private float respawnDelay = 0.2f;
    [SerializeField] private float spawnGrace = 0.5f;
    [SerializeField] private float fallKillY = -18f;
    [Tooltip("이미 저장된 세이브포인트와 이 거리 안이면(같은 씬 기준) 새로 추가하지 않고 중복으로 친다.")]
    [SerializeField] private float duplicateCheckpointRadius = 0.5f;

    [Header("Hazards")]
    [SerializeField] private string hazardNameContains = "Spikes";

    [Header("Combat")]
    [Tooltip("체력·피격 수치의 단일 기준. MonoBehaviour에 숫자 리터럴을 두지 않는다")]
    [SerializeField] private PlayerCombatConfig combatConfig;

    private Rigidbody2D body;
    private MonoBehaviour movementController;
    private SpriteRenderer[] visuals;
    private Vector3 checkpoint;
    // 세이브포인트 누적 목록 — 씬+좌표 쌍이라 다른 씬의 지점도 정확히 되돌아갈 수 있다.
    // SetCheckpoint()가 호출될 때마다 여기 새 항목이 추가된다(덮어쓰지 않음).
    private readonly List<CheckpointRecord> checkpoints = new List<CheckpointRecord>();
    private float graceUntil;
    private bool dying;
    private int deaths;

    private int currentHealth;
    private float damageInvulnerableUntil;
    private float rollInvulnerableUntil;
    private int maxHealthBonus;

    // OnGUI 디버그 표시용 — 증강으로 늘어난 수치를 보여주기 위해서만 참조한다(전투 로직엔 관여 안 함).
    private PlayerProgression progression;
    private PlayerMana mana;

    public int Deaths { get { return deaths; } }
    public bool IsDying { get { return dying; } }
    public bool Invincible
    {
        get { return invincible; }
        set { invincible = value; }
    }

    public int CurrentHealth { get { return currentHealth; } }
    public int MaxHealth
    {
        get
        {
            if (combatConfig == null) return maxHealthBonus;
            return NAN2026.Core.HealthProgressionLogic.ClampedMaxHealth(combatConfig.maxHealth, maxHealthBonus, combatConfig.maxHealthCap);
        }
    }
    public int ParryCounterDamage { get { return combatConfig != null ? combatConfig.parryCounterDamage : 0; } }

    /// <summary>체력이 바뀔 때마다 (현재, 최대)를 통지한다. 월드스페이스 HP바 등이 구독할 수 있다.</summary>
    public event System.Action<int, int> OnHealthChanged;

    /// <summary>모든 무적/그레이스 판정을 통과해 실제로 체력이 깎인 순간에만 통지한다(회복은 제외).
    /// PlayerSoundPlayer가 피격 사운드(랜덤) 재생에 구독한다.</summary>
    public event System.Action OnDamaged;

    /// <summary>플레이어가 죽는 순간(Kill 진입 시) 딱 한 번 통지한다. GameOverPanel 등이 구독해
    /// 화면 전환을 시작할 수 있다. 체크포인트 재시작 로직(Respawn)과는 무관하게 별도로 발생한다.</summary>
    public event System.Action OnPlayerDied;

    /// <summary>부활이 끝난 순간 통지한다. 사망 연출(PlayerHurtDeathFx)이 원상복구 시점으로 쓴다.</summary>
    public event System.Action OnPlayerRespawned;

    /// <summary>true 면 Kill() 이 스프라이트를 즉시 끄지 않는다. 사망 연출을 보여줄 때 켠다.</summary>
    public bool SuppressDeathHide { get; set; }

    /// <summary>true 면 Kill() 이 체크포인트 부활을 예약하지 않는다.
    /// 게임오버→타이틀 노선에서 부활과 게임오버가 같은 시점에 경합하는 것을 막는다.
    /// GameOverController 가 구독 시점에 켠다.</summary>
    public bool SuppressRespawnOnDeath { get; set; }

    /// <summary>수몰 연출 진행 중에는 낙사 판정이 시퀀스를 가로채지 않는다.</summary>
    public bool SinkingInWater { get; set; }

    /// <summary>외부(GameOverController 등)에서 명시적으로 체크포인트 부활을 트리거할 때 쓴다.
    /// SuppressRespawnOnDeath=true 노선(게임오버 패널 등)에서는 Kill()이 자동으로 Respawn을
    /// 예약하지 않으므로, 호출자가 원하는 타이밍(예: 엔터키 입력)에 직접 이걸 부른다.</summary>
    public void RespawnNow()
    {
        Respawn();
    }

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        visuals = GetComponentsInChildren<SpriteRenderer>(true);
        progression = GetComponent<PlayerProgression>();
        mana = GetComponent<PlayerMana>();

        foreach (MonoBehaviour behaviour in GetComponents<MonoBehaviour>())
        {
            // FAIL#24 계열: 이름 하나만 보면 프리팹 교체 시 조용히 무력화된다. 실제 사용 중인 두 컨트롤러를 모두 인정.
            if (behaviour != this && (behaviour.GetType().Name == "PixelPlayerController" || behaviour.GetType().Name == "PlayerController2D"))
            {
                movementController = behaviour;
                break;
            }
        }

        checkpoint = transform.position;
        // 시작 위치도 첫 세이브포인트로 등록해둔다 — 그래야 아무것도 안 밟은 상태에서
        // 이동 메뉴를 열어도 최소 한 곳(시작 지점)은 나온다.
        graceUntil = Time.time + spawnGrace;

        currentHealth = MaxHealth;
        OnHealthChanged?.Invoke(currentHealth, MaxHealth);
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.f2Key.wasPressedThisFrame)
                invincible = !invincible;
            if (keyboard.f3Key.wasPressedThisFrame)
                ResetAllTraps();
        }

        // 월드 밖으로 떨어지면 무적이어도 사망 — 단 Respawn()을 직접 부르지 않고
        // 정식 사망 경로(Kill)를 태워 death 모션·게임오버 패널이 나오게 한다.
        if (!dying && !SinkingInWater && transform.position.y < fallKillY)
            Kill();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryHazard(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryHazard(other);
    }

    private void TryHazard(Collider2D other)
    {
        if (other == null || dying || invincible || Time.time < graceUntil)
            return;

        Hazard2D hazard = other.GetComponentInParent<Hazard2D>();
        bool lethal = (hazard != null && hazard.enabled) ||
                      other.gameObject.name.Contains(hazardNameContains);

        if (lethal)
            Kill();
    }

    /// <summary>몬스터의 공격 등으로 데미지를 받는다. 무적/스폰 그레이스/피격 직후 무적 중에는 무시된다.
    /// 체력이 0 이하가 되면 기존 Kill()/Respawn() 경로를 그대로 탄다 (죽으면 체크포인트에서 재시작).</summary>
    public void TakeDamage(float damage)
    {
        var __bs = GetComponent<PlayerController2D>();
        if (__bs != null && __bs.IsBackstepInvincible) return; // 백스텝 무적

        if (dying || invincible || Time.time < graceUntil || Time.time < damageInvulnerableUntil || Time.time < rollInvulnerableUntil)
            return;

        if (combatConfig == null)
            return;

        currentHealth -= Mathf.Max(1, Mathf.RoundToInt(damage));
        damageInvulnerableUntil = Time.time + combatConfig.hitInvulnerabilityDuration;

        OnDamaged?.Invoke(); // 실제로 데미지가 적용된 순간에만 1회 — 피격 사운드용
        OnHealthChanged?.Invoke(currentHealth, MaxHealth);

        if (currentHealth <= 0)
            Kill();
    }

    /// <summary>새 세이브포인트를 목록에 추가한다(기존 것을 덮어쓰지 않고 누적). 낙사 시
    /// 자동 부활 지점(checkpoint)도 항상 가장 최근 것으로 갱신된다.</summary>
    public void SetCheckpoint(Vector3 position)
    {
        checkpoint = position;
        string scene = SceneManager.GetActiveScene().name;

        // 같은 세이브포인트를 다시 밟아도 목록에 중복으로 안 쌓이게 — 같은 씬 + 근접 좌표면
        // 새 항목을 추가하지 않는다(이미 있는 걸로 충분).
        for (int i = 0; i < checkpoints.Count; i++)
        {
            CheckpointRecord existing = checkpoints[i];
            if (existing.sceneName == scene && Vector3.Distance(existing.position, position) <= duplicateCheckpointRadius)
                return;
        }

        checkpoints.Add(new CheckpointRecord(scene, position, scene + " #" + checkpoints.Count));
    }

    /// <summary>지금까지 저장된 모든 세이브포인트(씬+좌표). CheckpointTravelMenu가 이걸 읽어서
    /// 이동 목록을 그린다.</summary>
    public IReadOnlyList<CheckpointRecord> Checkpoints => checkpoints;

    /// <summary>구르기가 시작되는 순간 PlayerController2D가 호출한다. combatConfig.rollInvincibilityDuration 동안 무적.</summary>
    public void BeginRollInvincibility()
    {
        if (combatConfig == null) return;
        rollInvulnerableUntil = Time.time + combatConfig.rollInvincibilityDuration;
    }

    /// <summary>즉시 체력을 회복한다(레벨업 증강 등). 최대 체력을 넘지 않는다.</summary>
    public void Heal(int amount)
    {
        if (amount <= 0) return;
        currentHealth = Mathf.Min(MaxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke(currentHealth, MaxHealth);
    }

    /// <summary>최대 체력을 영구적으로 늘리고, 늘어난 만큼 즉시 회복한다(레벨업 증강 등).</summary>
    public void AddMaxHealthBonus(int amount)
    {
        if (amount <= 0 || combatConfig == null) return;

        int actualGain = NAN2026.Core.HealthProgressionLogic.ActualMaxHealthGain(combatConfig.maxHealth, maxHealthBonus, amount, combatConfig.maxHealthCap);
        maxHealthBonus += amount;
        if (actualGain > 0)
            currentHealth += actualGain;
        OnHealthChanged?.Invoke(currentHealth, MaxHealth);
    }


    /// <summary>낙사·수몰처럼 구제 불가한 상황은 ignoreInvincible=true로 강제 사망시킨다.
    /// (무적 중이라도 월드 밖으로 떨어지면 되돌릴 방법이 없다)</summary>
    public void Kill( )
    {
        if (dying || invincible)
            return;

        dying = true;
        deaths++;
        SetControllerEnabled(false);
        if (!SuppressDeathHide)
            SetVisible(false);

        OnPlayerDied?.Invoke();

        // 게임오버 노선(타이틀 복귀)에서는 부활을 예약하지 않는다.
        if (SuppressRespawnOnDeath)
            return;

        // 사망 연출이 있으면 그 길이만큼 부활을 미룬다(연출이 잘리지 않도록).
        float delay = respawnDelay;
        var fx = GetComponent<NAN2026.PlayerHurtDeathFx>();
        if (fx != null)
            delay = NAN2026.Core.PlayerFxLogic.RespawnDelay(respawnDelay, fx.DeathDuration);
        Invoke(nameof(Respawn), delay);
    }

    private void Respawn()
    {
        // FAIL: transform.position만 바꾸면 Rigidbody2D가 다음 물리 스텝에서 자기가 내부적으로
        // 추적하던 예전 위치로 되돌려놓는다(보간 때문) — CheckpointTravelMenu에서 실측으로 확인된
        // 버그와 동일 패턴. body.position도 같이 맞춰야 확실히 고정된다.
        if (body != null)
        {
            body.position = checkpoint;
            body.SetRotation(0f);
            body.linearVelocity = Vector2.zero;
        }
        transform.position = checkpoint;
        transform.rotation = Quaternion.identity;

        SetVisible(true);
        SetControllerEnabled(true);

        currentHealth = MaxHealth;
        OnHealthChanged?.Invoke(currentHealth, MaxHealth);

        graceUntil = Time.time + spawnGrace;
        dying = false;

        OnPlayerRespawned?.Invoke();
    }

    /// <summary>Returns every trap in the scene to its untriggered state.</summary>
    public static int ResetAllTraps()
    {
        int count = 0;
        MonoBehaviour[] all = FindObjectsByType<MonoBehaviour>(
            FindObjectsInactive.Include);

        foreach (MonoBehaviour behaviour in all)
        {
            ITrapResettable trap = behaviour as ITrapResettable;
            if (trap == null)
                continue;

            trap.ResetTrap();
            count++;
        }
        return count;
    }

    private void SetControllerEnabled(bool value)
    {
        if (movementController != null)
            movementController.enabled = value;
    }

    private void SetVisible(bool value)
    {
        if (visuals == null) return;
        foreach (SpriteRenderer renderer in visuals)
        {
            if (renderer != null)
                renderer.enabled = value;
        }
    }

    private void OnGUI()
    {
        GUIStyle style = new GUIStyle(GUI.skin.box)
        {
            fontSize = 17,
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold
        };

        const float width = 170f;
        //GUI.Box(new Rect(Screen.width - width - 16f, 14f, width, 32f),
        //    "HP   " + currentHealth + "/" + MaxHealth, style);
        GUI.Box(new Rect(Screen.width - width - 16f, 50f, width, 28f),
            "DEATHS   " + deaths, style);

        // 증강으로 늘어난 수치 표시 — 전투 로직엔 관여 안 하고 보여주기만 함.
        if (progression != null)
        {
            GUI.Box(new Rect(Screen.width - width - 16f, 154f, width, 28f),
                "ATK +" + progression.DamageBonus.ToString("F1"), style);
        }
        if (mana != null && mana.config != null)
        {
            GUI.Box(new Rect(Screen.width - width - 16f, 186f, width, 28f),
                "MP GAIN " + mana.config.parryGain, style);
        }

        if (invincible)
        {
            Color previous = GUI.color;
            GUI.color = new Color(0.45f, 1f, 0.6f);
            GUI.Box(new Rect(Screen.width - width - 16f, 86f, width, 28f),
                "INVINCIBLE  (F2)", style);
            GUI.color = previous;
        }

    }

}