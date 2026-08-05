using System.Collections;
using UnityEngine;

/// <summary>
/// Lich 전용 단일 패턴: 사거리(config.attackRange, 기본 5) 안에 플레이어가 있으면
/// 구체 1개를 발사한다. EnemyAI가 IEnemyAttackOverride를 통해 위임한다.
/// </summary>
[DisallowMultipleComponent]
public sealed class LichAttackPattern : MonoBehaviour, IEnemyAttackOverride
{
    [SerializeField] private LichAttackConfig config;
    private Assets.PixelFantasy.PixelMonsters.Common.Scripts.ExampleScripts.MonsterAnimation animation;
    [Tooltip("구체 발사에 사용할 스프라이트(비워두면 안 보이는 판정으로만 날아간다)")]
    [SerializeField] private Sprite orbSprite;
    [SerializeField] private int orbSortingOrder = 20;

    private NHNDemo.MonsterHealth health;
    private bool busy;
    private float nextAllowedTime;

    public bool IsBusy => busy;

    private void Awake()
    {
        animation = GetComponent<Assets.PixelFantasy.PixelMonsters.Common.Scripts.ExampleScripts.MonsterAnimation>();
        health = GetComponent<NHNDemo.MonsterHealth>();
    }

    public bool TryStartAttack(Transform player)
    {
        if (config == null || busy || player == null) return false;
        if (Time.time < nextAllowedTime) return false;

        float distance = Mathf.Abs(player.position.x - transform.position.x);
        if (distance > config.attackRange) return false;

        StartCoroutine(DoFireOrb(player));
        return true;
    }

    private IEnumerator DoFireOrb(Transform player)
    {
        busy = true;
        if (animation != null) animation.Attack();

        yield return new WaitForSeconds(config.windup);

        if (player != null)
        {
            Vector3 spawnPos = transform.position + Vector3.up * config.orbSpawnHeight;
            Vector2 dir = ((Vector2)player.position - (Vector2)spawnPos).normalized;
            GameObject go = new GameObject("LichOrb");
            go.transform.position = spawnPos;
            if (orbSprite != null)
            {
                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = orbSprite;
                sr.sortingOrder = orbSortingOrder;
            }
            SpikeProjectile proj = go.AddComponent<SpikeProjectile>();
            proj.Init(dir, config.orbSpeed, config.orbDamage, health);
        }

        busy = false;
        nextAllowedTime = Time.time + Random.Range(config.minCooldown, config.maxCooldown);
    }
}
