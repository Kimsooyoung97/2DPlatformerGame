using NAN2026.Showroom;
using UnityEngine;
using UnityEngine.InputSystem;

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
    [SerializeField] private float fallKillY = -5f;

    [Header("Hazards")]
    [SerializeField] private string hazardNameContains = "Spikes";

    [Header("Combat")]
    [Tooltip("체력·피격 수치의 단일 기준. MonoBehaviour에 숫자 리터럴을 두지 않는다")]
    [SerializeField] private PlayerCombatConfig combatConfig;

    private Rigidbody2D body;
    private MonoBehaviour movementController;
    private SpriteRenderer[] visuals;
    private Vector3 checkpoint;
    private float graceUntil;
    private bool dying;
    private int deaths;

    private int currentHealth;
    private float damageInvulnerableUntil;
    private float rollInvulnerableUntil;
    private int maxHealthBonus;

    public int Deaths { get { return deaths; } }
    public bool IsDying { get { return dying; } }
    public bool Invincible
    {
        get { return invincible; }
        set { invincible = value; }
    }

    public int CurrentHealth { get { return currentHealth; } }
    public int MaxHealth { get { return (combatConfig != null ? combatConfig.maxHealth : 0) + maxHealthBonus; } }
    public int ParryCounterDamage { get { return combatConfig != null ? combatConfig.parryCounterDamage : 0; } }

    /// <summary>체력이 바뀔 때마다 (현재, 최대)를 통지한다. 월드스페이스 HP바 등이 구독할 수 있다.</summary>
    public event System.Action<int, int> OnHealthChanged;

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

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        visuals = GetComponentsInChildren<SpriteRenderer>(true);

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

        // Falling out of the world still resets you, even while invincible.
        if (!dying && transform.position.y < fallKillY)
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


        OnHealthChanged?.Invoke(currentHealth, MaxHealth);

        if (currentHealth <= 0)
            Kill();
    }

    public void SetCheckpoint(Vector3 position)
    {
        checkpoint = position;
    }

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
        if (amount <= 0) return;
        maxHealthBonus += amount;
        currentHealth += amount;
        OnHealthChanged?.Invoke(currentHealth, MaxHealth);
    }

    public void Kill()
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
        transform.position = checkpoint;
        transform.rotation = Quaternion.identity;
        if (body != null)
        {
            body.SetRotation(0f);
            body.linearVelocity = Vector2.zero;
        }

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

        if (invincible)
        {
            Color previous = GUI.color;
            GUI.color = new Color(0.45f, 1f, 0.6f);
            GUI.Box(new Rect(Screen.width - width - 16f, 86f, width, 28f),
                "INVINCIBLE  (F2)", style);
            GUI.color = previous;
        }

        GUI.Label(new Rect(Screen.width - width - 16f, 118f, width, 22f),
            "   F2 invincible · F3 reset traps");
    }

}