using System.Collections;
using UnityEngine;
using NAN2026.Showroom;

/// <summary>
/// MiddleBoss 전용 추가 공격 패턴(돌진/투사체 던지기). OrkanBoss.cs의 해당 두 패턴을
/// 이식한 것으로, 셸/그로기 데미지 배율 시스템은 가져오지 않았다(이번 요청 범위 밖).
/// EnemyAI가 IEnemyAttackOverride를 통해 이 컴포넌트에 공격을 위임한다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public sealed class MiddleBossAttackPatterns : MonoBehaviour, IEnemyAttackOverride
{
    [SerializeField] private MiddleBossAttackConfig config;
    [Tooltip("비워두면 임시 오브젝트로 자동 대체")]
    [SerializeField] private GameObject rockSpikePrefab;

    private Rigidbody2D body;
    private NHNDemo.MonsterHealth health;
    private Assets.PixelFantasy.PixelMonsters.Common.Scripts.ExampleScripts.MonsterController2D controller;
    private Assets.PixelFantasy.PixelMonsters.Common.Scripts.ExampleScripts.MonsterAnimation animation;
    private bool busy;
    private float nextAllowedPatternTime;

    public bool IsBusy => busy;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        health = GetComponent<NHNDemo.MonsterHealth>();
        controller = GetComponent<Assets.PixelFantasy.PixelMonsters.Common.Scripts.ExampleScripts.MonsterController2D>();
        animation = GetComponent<Assets.PixelFantasy.PixelMonsters.Common.Scripts.ExampleScripts.MonsterAnimation>();
    }

    public bool TryStartAttack(Transform player)
    {
        if (config == null || busy || player == null)
            return false;

        if (Time.time < nextAllowedPatternTime)
            return false;

        float distance = Mathf.Abs(player.position.x - transform.position.x);
        if (distance < config.rangedMinDistance)
            return false; // 근접이면 EnemyAI 기본 공격에 맡긴다

        bool useCharge = Random.value < 0.9f;

        // MonsterController2D.FixedUpdate가 Input.x==0일 때 매 물리 스텝마다
        // rb.linearVelocity를 자체적으로 감속시켜, 이 컴포넌트가 코루틴에서
        // 직접 넣는 돌진 속도와 매 프레임 충돌했다(돌진이 제대로 안 나가던 원인).
        // 패턴이 몸을 직접 제어하는 동안은 그 컨트롤러를 꺼서 충돌을 없앤다.
        if (controller != null) controller.enabled = false;

        StartCoroutine(useCharge ? DoCharge(player) : DoProjectileThrow(player));
        return true;
    }

    private IEnumerator DoCharge(Transform player)
    {
        Debug.Log("씀");
        busy = true;
        yield return new WaitForSeconds(config.chargeWindup);

        if (player == null) { EndPattern(); yield break; }

        float dir = Mathf.Sign(player.position.x - transform.position.x);
        Vector3 startPos = transform.position;
        bool hitPlayerOnce = false;

        // MonsterController2D를 꺼두는 동안은 그쪽이 담당하던 Run 애니메이션 전환·
        // 좌우 방향 전환(Turn)도 멈추므로 여기서 직접 처리한다.
        if (animation != null) animation.Run();
        Vector3 facingScale = transform.localScale;
        facingScale.x = dir * Mathf.Abs(facingScale.x);
        transform.localScale = facingScale;

        while (Vector3.Distance(startPos, transform.position) < config.chargeMaxDistance)
        {
            Vector2 rayOrigin = (Vector2)transform.position + new Vector2(dir * config.wallCheckOriginOffset, 0f);
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, new Vector2(dir, 0f), config.wallCheckDistance, config.wallLayerMask);
            if (hit.collider != null)
                break;

            body.linearVelocity = new Vector2(dir * config.chargeSpeed, body.linearVelocity.y);

            if (!hitPlayerOnce && player != null && Vector3.Distance(player.position, transform.position) < config.chargeHitDistance)
            {
                hitPlayerOnce = true;
                DealContactDamage(player, config.chargeDamage);
            }

            yield return new WaitForFixedUpdate();
        }

        body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
        EndPattern();
    }

    private IEnumerator DoProjectileThrow(Transform player)
    {
        busy = true;
        yield return new WaitForSeconds(config.throwWindup);

        for (int i = 0; i < config.throwCount; i++)
        {
            ThrowSpike(player);
            yield return new WaitForSeconds(config.throwInterval);
        }

        EndPattern();
    }

    private void ThrowSpike(Transform player)
    {
        if (player == null) return;

        Vector3 spawnPos = transform.position + Vector3.up * config.throwSpawnHeight;
        Vector2 dir = ((Vector2)player.position - (Vector2)spawnPos).normalized;

        GameObject go = rockSpikePrefab != null
            ? Instantiate(rockSpikePrefab, spawnPos, Quaternion.identity)
            : new GameObject("SpikeProjectile");
        if (rockSpikePrefab == null)
            go.transform.position = spawnPos;

        SpikeProjectile proj = go.GetComponent<SpikeProjectile>();
        if (proj == null) proj = go.AddComponent<SpikeProjectile>();
        proj.Init(dir, config.rockSpikeSpeed, config.rockSpikeDamage, health);
    }

    private void DealContactDamage(Transform player, float damage)
    {
        // 패링 성공 시: 플레이어는 무피해, 이 보스가 대신 반격 데미지를 받는다.
        IParryReflector reflector = player.GetComponentInParent<IParryReflector>();
        if (reflector != null && reflector.TryParry(gameObject))
        {
            PlayerHealth parriedPh = player.GetComponentInParent<PlayerHealth>();
            int counterDamage = parriedPh != null ? parriedPh.ParryCounterDamage : 0;
            if (health != null && counterDamage > 0)
                health.TakeDamage(counterDamage, (Vector2)(transform.position - player.position));
            return;
        }

        PlayerHealth ph = player.GetComponentInParent<PlayerHealth>();
        if (ph != null)
            ph.TakeDamage(damage);
    }

    private void EndPattern()
    {
        busy = false;
        if (controller != null) controller.enabled = true;
        if (animation != null) animation.Ready();
        nextAllowedPatternTime = Time.time + Random.Range(config.minPatternCooldown, config.maxPatternCooldown);
    }
}
