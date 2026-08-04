using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using NAN2026.Core;
using NAN2026.Showroom;

/// <summary>
/// Princess_Boss_Knight 전용 3패턴. 기존 BossOrbLauncher/BossOrb/BossBeam(리듬 빔)은
/// 사용하지 않고 새로 구현한다.
/// ① 구체 투척(Trans2, 속도 다른 5발, 패링 가능 — SpikeProjectile 재사용)
/// ② 중범위 공격(Trans3, 정면 넓은 범위, 패링 가능)
/// ③ 전범위 QTE(Trans1, 일시정지+리듬 QTE, 성공 시 그로기, 실패 시 패링 불가 피해)
/// EnemyAI가 IEnemyAttackOverride를 통해 위임한다.
/// </summary>
[DisallowMultipleComponent]
public sealed class PrincessBossAttackPatterns : MonoBehaviour, IEnemyAttackOverride
{
    [SerializeField] private PrincessBossAttackConfig config;
    [SerializeField] private Animator animator;

    private Assets.PixelFantasy.PixelMonsters.Common.Scripts.ExampleScripts.MonsterController2D controller;
    private NHNDemo.MonsterHealth health;

    private bool busy;
    private float nextAllowedPatternTime;
    private float groggyUntil;

    private bool qteActive;
    private int qteBeatsHit;
    private int qteCurrentBeat;
    private float qteElapsed;

    /// 패턴 실행 중이거나 그로기 상태면 EnemyAI가 완전히 개입하지 않는다(그로기 = 무행동).
    public bool IsBusy => busy || Time.time < groggyUntil;

    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        controller = GetComponent<Assets.PixelFantasy.PixelMonsters.Common.Scripts.ExampleScripts.MonsterController2D>();
        health = GetComponent<NHNDemo.MonsterHealth>();
    }

    public bool TryStartAttack(Transform player)
    {
        if (config == null || busy || player == null) return false;
        if (Time.time < nextAllowedPatternTime) return false;
        if (Time.time < groggyUntil) return false;

        int pick = Random.Range(0, 3);
        if (controller != null) controller.enabled = false;

        switch (pick)
        {
            case 0: StartCoroutine(DoOrbVolley(player)); break;
            case 1: StartCoroutine(DoFrontalAoE(player)); break;
            default: StartCoroutine(DoFullScreenQte(player)); break;
        }
        return true;
    }

    private IEnumerator DoOrbVolley(Transform player)
    {
        busy = true;
        if (animator != null) animator.Play("PTrans2");
        yield return new WaitForSeconds(config.orbWindup);

        for (int i = 0; i < config.orbSpeeds.Length; i++)
        {
            if (player != null)
            {
                Vector3 spawnPos = transform.position + Vector3.up * config.orbSpawnHeight;
                Vector2 dir = ((Vector2)player.position - (Vector2)spawnPos).normalized;
                GameObject go = new GameObject("PrincessOrb_" + i);
                go.transform.position = spawnPos;
                SpikeProjectile proj = go.AddComponent<SpikeProjectile>();
                proj.Init(dir, config.orbSpeeds[i], config.orbDamage, health);
            }
            yield return new WaitForSeconds(config.orbLaunchInterval);
        }

        if (animator != null) animator.Play("PIdle2");
        EndPattern();
    }

    private IEnumerator DoFrontalAoE(Transform player)
    {
        busy = true;
        if (animator != null) animator.Play("PTrans3");
        yield return new WaitForSeconds(config.aoeWindup);

        if (player != null)
        {
            float dir = player.position.x > transform.position.x ? 1f : -1f;
            Vector2 center = (Vector2)transform.position + new Vector2(dir * config.aoeForwardRange * 0.5f, config.aoeHeight * 0.5f);
            Vector2 size = new Vector2(config.aoeForwardRange, config.aoeHeight);
            int playerMask = LayerMask.GetMask("Player");
            Collider2D hit = Physics2D.OverlapBox(center, size, 0f, playerMask);
            if (hit != null)
            {
                DealDamageWithParryCheck(player, config.aoeDamage);
            }
        }

        if (animator != null) animator.Play("PIdle2");
        EndPattern();
    }

    private IEnumerator DoFullScreenQte(Transform player)
    {
        busy = true;
        if (animator != null) animator.Play("PTrans1");

        Time.timeScale = 0f;
        qteActive = true;
        qteBeatsHit = 0;
        qteCurrentBeat = 0;
        qteElapsed = 0f;

        while (qteCurrentBeat < config.qteBeatCount)
        {
            qteElapsed += Time.unscaledDeltaTime;
            float beatTarget = (qteCurrentBeat + 1) * config.qteBeatInterval;

            Keyboard kb = Keyboard.current;
            if (kb != null && kb.zKey.wasPressedThisFrame)
            {
                if (PrincessBossLogic.IsBeatHit(beatTarget, qteElapsed, config.qteHitWindow))
                    qteBeatsHit++;
                qteCurrentBeat++;
            }
            else if (qteElapsed > beatTarget + config.qteHitWindow)
            {
                qteCurrentBeat++;
            }
            yield return null;
        }

        bool success = PrincessBossLogic.QteSucceeded(qteBeatsHit, config.qteBeatCount);
        qteActive = false;
        Time.timeScale = 1f;

        if (success)
        {
            groggyUntil = Time.time + config.groggyDuration;
        }
        else if (player != null)
        {
            // 실패 시 패링 불가 피해
            PlayerHealth ph = player.GetComponentInParent<PlayerHealth>();
            if (ph != null) ph.TakeDamage(config.qteFailDamage);
        }

        if (animator != null) animator.Play("PIdle2");
        EndPattern();
    }

    private void DealDamageWithParryCheck(Transform player, float damage)
    {
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
        if (ph != null) ph.TakeDamage(damage);
    }

    private void EndPattern()
    {
        busy = false;
        if (controller != null) controller.enabled = true;
        nextAllowedPatternTime = Time.time + Random.Range(config.minPatternCooldown, config.maxPatternCooldown);
    }

    private void OnGUI()
    {
        if (!qteActive) return;

        GUIStyle style = new GUIStyle(GUI.skin.box) { fontSize = 22, alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } };
        float w = 360f, h = 90f;
        Rect rect = new Rect((Screen.width - w) * 0.5f, Screen.height * 0.2f, w, h);
        GUI.Box(rect, "QTE! Z\ub97c \ub9ac\ub4ec\uc5d0 \ub9de\ucdb0 \ub204\ub974\uc138\uc694\n" + qteBeatsHit + " / " + config.qteBeatCount, style);
    }
}
