using UnityEngine;

/// <summary>
/// 플레이어가 아직 체력 시스템을 갖고 있지 않아서, 보스가 데미지를 줄 대상으로 사용할
/// 최소한의 체력 컴포넌트입니다. 이미 자체 체력 시스템이 있다면 이 스크립트 대신
/// 그 시스템에 맞춰 OrkanBoss.cs / SpikeProjectile.cs / SpikeShockwave.cs 안의
/// TakeDamage 호출부만 교체해주면 됩니다.
/// </summary>
[DisallowMultipleComponent]
public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;

    [Header("Hit Feedback")]
    [SerializeField] private float invincibleDuration = 0.5f; // 피격 후 잠깐 무적
    [SerializeField] private float knockbackForce = 6f;

    private Rigidbody2D rb;
    private PlayerController playerController;
    private float invincibleTimer = 0f;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDead { get; private set; } = false;

    void Awake()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        playerController = GetComponent<PlayerController>();

        var bar = gameObject.AddComponent<HealthBarUI>();
        bar.Init(() => currentHealth, () => maxHealth, Color.green);
    }

    void Update()
    {
        if (invincibleTimer > 0f)
            invincibleTimer -= Time.deltaTime;
    }

    public bool IsInvincible => invincibleTimer > 0f;

    /// <param name="damage">최종 데미지(이미 보스 쪽 계산이 끝난 값)</param>
    /// <param name="attackerPosition">넉백 방향 계산용 공격자 위치</param>
    public void TakeDamage(float damage, Vector3 attackerPosition)
    {
        if (IsDead || IsInvincible) return;

        currentHealth -= damage;
        invincibleTimer = invincibleDuration;

        if (rb != null)
        {
            float dir = Mathf.Sign(transform.position.x - attackerPosition.x);
            rb.linearVelocity = new Vector2(dir * knockbackForce, rb.linearVelocity.y);
        }

        if (currentHealth <= 0f)
        {
            currentHealth = 0f;
            IsDead = true;
            if (playerController != null)
                playerController.Die();
        }
    }

    public void TakeDamage(float damage)
    {
        TakeDamage(damage, transform.position + Vector3.left);
    }
}
