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
    [Tooltip("구체 투척에 사용할 스프라이트(기존 BossOrb 비주얼 재사용, 스크립트는 재사용 안 함)")]
    [SerializeField] private Sprite orbSprite;
    [SerializeField] private int orbSortingOrder = 25;

    private Assets.PixelFantasy.PixelMonsters.Common.Scripts.ExampleScripts.MonsterController2D controller;
    private NHNDemo.MonsterHealth health;

    private bool busy;
    private float nextAllowedPatternTime;
    private float groggyUntil;

    private static readonly string[] QteKeyNames = { "Z", "X", "C" };

    private bool qteActive;
    private bool qteWaitingToStart;
    private float qteStartCountdown;
    private int qteBeatsHit;
    private int qteCurrentBeat;
    private float qteElapsed;
    private int qteCurrentKeyIndex;
    private string qteLastResult = string.Empty;
    private float qteLastResultTimer;
    private SpriteRenderer spriteRenderer;
    private Color spriteOriginalColor = Color.white;
    private bool wasGroggyLastFrame;

    /// 패턴 실행 중이거나 그로기 상태면 EnemyAI가 완전히 개입하지 않는다(그로기 = 무행동).
    public bool IsBusy => busy || Time.time < groggyUntil;

    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        controller = GetComponent<Assets.PixelFantasy.PixelMonsters.Common.Scripts.ExampleScripts.MonsterController2D>();
        health = GetComponent<NHNDemo.MonsterHealth>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) spriteOriginalColor = spriteRenderer.color;
    }

    // 그로기 상태를 눈에 보이게 스프라이트 색을 물들인다(그로기 시작/종료 시 한 번씩만 갱신).
    private void Update()
    {
        if (spriteRenderer == null) return;
        bool groggyNow = Time.time < groggyUntil;
        if (groggyNow == wasGroggyLastFrame) return;
        wasGroggyLastFrame = groggyNow;
        spriteRenderer.color = groggyNow ? new Color(1f, 0.85f, 0.2f, spriteOriginalColor.a) : spriteOriginalColor;
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
                // SpikeProjectile은 시각 요소가 전혀 없어(투명 판정만) 눈에 안 보였다 —
                // 여기서 스프라이트를 직접 붙여준다.
                if (orbSprite != null)
                {
                    SpriteRenderer orbSr = go.AddComponent<SpriteRenderer>();
                    orbSr.sprite = orbSprite;
                    orbSr.sortingOrder = orbSortingOrder;
                }
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

        // 일시정지 직후 비트가 바로 시작되면 반응할 시간이 없어 대기시간을 둔다.
        qteWaitingToStart = true;
        qteStartCountdown = config.qteStartDelay;
        while (qteStartCountdown > 0f)
        {
            qteStartCountdown -= Time.unscaledDeltaTime;
            yield return null;
        }
        qteWaitingToStart = false;

        qteCurrentKeyIndex = Random.Range(0, QteKeyNames.Length);

        while (qteCurrentBeat < config.qteBeatCount)
        {
            qteElapsed += Time.unscaledDeltaTime;
            float beatTarget = (qteCurrentBeat + 1) * config.qteBeatInterval;

            qteLastResultTimer -= Time.unscaledDeltaTime;

            if (WasQteKeyPressedThisFrame(qteCurrentKeyIndex))
            {
                bool hit = PrincessBossLogic.IsBeatHit(beatTarget, qteElapsed, config.qteHitWindow);
                if (hit) qteBeatsHit++;
                qteLastResult = hit ? "GOOD!" : "MISS";
                qteLastResultTimer = config.qteBeatInterval * 0.5f;
                qteCurrentBeat++;
                qteCurrentKeyIndex = Random.Range(0, QteKeyNames.Length);
            }
            else if (qteElapsed > beatTarget + config.qteHitWindow)
            {
                qteLastResult = "MISS";
                qteLastResultTimer = config.qteBeatInterval * 0.5f;
                qteCurrentBeat++;
                qteCurrentKeyIndex = Random.Range(0, QteKeyNames.Length);
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

    private bool WasQteKeyPressedThisFrame(int keyIndex)
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return false;
        switch (keyIndex)
        {
            case 0: return kb.zKey.wasPressedThisFrame;
            case 1: return kb.xKey.wasPressedThisFrame;
            default: return kb.cKey.wasPressedThisFrame;
        }
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

        float w = 500f, h = 150f;
        float left = (Screen.width - w) * 0.5f;
        float top = Screen.height * 0.2f;

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 24, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        titleStyle.normal.textColor = Color.white;

        if (qteWaitingToStart)
        {
            GUI.Label(new Rect(left, top, w, 36f), "\uace7 QTE\uac00 \uc2dc\uc791\ub429\ub2c8\ub2e4...", titleStyle);
            GUIStyle countStyle = new GUIStyle(GUI.skin.label) { fontSize = 40, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            countStyle.normal.textColor = Color.yellow;
            GUI.Label(new Rect(left, top + 44f, w, 60f), Mathf.CeilToInt(Mathf.Max(qteStartCountdown, 0f)).ToString(), countStyle);
            return;
        }

        string keyName = QteKeyNames[qteCurrentKeyIndex];
        GUI.Label(new Rect(left, top, w, 36f), keyName + " \ub97c \ub9ac\ub4ec\uc5d0 \ub9de\ucdb0 \ub204\ub974\uc138\uc694 (" + qteBeatsHit + " / " + config.qteBeatCount + ")", titleStyle);

        // 현재 비트 구간 안에서의 진행률(0~1)을 가로 바로 보여준다.
        // 오른쪽 끝(히트 구간)이 강조된 색으로 표시되고, 찾아오는 순간이 누르는 타이밍이다.
        float beatStart = qteCurrentBeat * config.qteBeatInterval;
        float tRaw = config.qteBeatInterval > 0f ? (qteElapsed - beatStart) / config.qteBeatInterval : 0f;
        float t = Mathf.Clamp01(tRaw);

        Rect barRect = new Rect(left, top + 50f, w, 34f);
        GUI.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);
        GUI.DrawTexture(barRect, Texture2D.whiteTexture);

        float hitZoneWidth = config.qteBeatInterval > 0f ? (config.qteHitWindow / config.qteBeatInterval) * w : 0f;
        Rect hitZoneRect = new Rect(left + w - hitZoneWidth, top + 50f, hitZoneWidth, 34f);
        GUI.color = new Color(0.3f, 0.9f, 0.4f, 0.9f);
        GUI.DrawTexture(hitZoneRect, Texture2D.whiteTexture);

        float markerX = left + t * w;
        Rect markerRect = new Rect(markerX - 3f, top + 44f, 6f, 46f);
        GUI.color = Color.white;
        GUI.DrawTexture(markerRect, Texture2D.whiteTexture);
        GUI.color = Color.white;

        if (qteLastResultTimer > 0f && !string.IsNullOrEmpty(qteLastResult))
        {
            GUIStyle resultStyle = new GUIStyle(GUI.skin.label) { fontSize = 28, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            resultStyle.normal.textColor = qteLastResult == "GOOD!" ? new Color(0.4f, 1f, 0.5f) : new Color(1f, 0.4f, 0.4f);
            GUI.Label(new Rect(left, top + 92f, w, 40f), qteLastResult, resultStyle);
        }
    }
}
