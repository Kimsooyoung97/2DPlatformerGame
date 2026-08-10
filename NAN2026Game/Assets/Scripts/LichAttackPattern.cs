using System.Collections;
using UnityEngine;

/// <summary>
/// Lich 전용 단일 패턴: 사거리(config.attackRange, 기본 5) 안에 플레이어가 있으면
/// 구체 1개를 발사한다. EnemyAI가 IEnemyAttackOverride를 통해 위임한다.
/// 사운드: windup이 끝나 구체가 실제로 생성되는 순간(=발사가 확정된 순간)에만 1회 재생한다.
/// </summary>
[DisallowMultipleComponent]
public sealed class LichAttackPattern : MonoBehaviour, IEnemyAttackOverride
{
    [SerializeField] private LichAttackConfig config;
    private Assets.PixelFantasy.PixelMonsters.Common.Scripts.ExampleScripts.MonsterAnimation animation;
    [Tooltip("구체 발사에 사용할 스프라이트(비워두면 안 보이는 판정으로만 날아간다)")]
    [SerializeField] private Sprite orbSprite;
    [SerializeField] private int orbSortingOrder = 20;
    [SerializeField] private Transform orbSpawnPos;
    private NHNDemo.MonsterHealth health;
    private AudioSource audioSource;
    private bool busy;
    private float nextAllowedTime;

    public bool IsBusy => busy;

    private void Awake()
    {
        animation = GetComponent<Assets.PixelFantasy.PixelMonsters.Common.Scripts.ExampleScripts.MonsterAnimation>();
        health = GetComponent<NHNDemo.MonsterHealth>();
        audioSource = GetComponent<AudioSource>();
    }

    // clip이 null이거나 AudioSource가 없으면 조용히 무시 — 사운드 미배치 상태에서도 안전.
    private void PlayClip(AudioClip clip, float volume)
    {
        if (audioSource == null || clip == null) return;
        audioSource.PlayOneShot(clip, volume);
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
            // windup을 통과해 구체가 실제로 발사되는 순간에만 1회 — 애니메이션 트리거(위)가
            // 아니라 여기가 "확정" 지점이라, 사거리 이탈로 공격이 무산돼도 소리가 안 난다.
            PlayClip(config.attackClip, config.attackVolume);
            Vector2 dir = ((Vector2)player.position - (Vector2)orbSpawnPos.position).normalized;
            GameObject go = new GameObject("LichOrb");
            go.transform.position = orbSpawnPos.position;
            if (orbSprite != null)
            {
                SpriteRenderer sr = go.AddComponent<SpriteRenderer>(); sr.sharedMaterial = NAN2026.FxUnlit.Mat;
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