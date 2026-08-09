using UnityEngine;
using NAN2026.Core;
using NAN2026.Showroom;
using NAN2026;

/// <summary>
/// 플레이어 경험치/레벨/증강 효과를 관리하는 순수 데이터·로직 계층.
/// UI는 전혀 모르고, 증강 선택이 필요해지면 이벤트만 발행한다.
/// 실제 화면 표시는 LevelUpSkillManager가 이 이벤트를 구독해서 담당한다.
/// 순수 판정(XP 곡선, 등급 확률)은 NAN2026.Core.LevelProgressionLogic이 갖고 있다.
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerProgression : MonoBehaviour
{
    [SerializeField] private LevelProgressionConfig levelConfig;
    [SerializeField] private AugmentConfig augmentConfig;
    [SerializeField] private GameObject canvas;
    [SerializeField] private PlayerMana playerMana;
    private PlayerHealth health;
    private PlayerController2D controller;

    private int level = 1;
    private int xp;
    private int pendingAugmentChoices;
    private bool choosing;
    private AugmentType[] offeredTypes;
    private int[] offeredTiers;

    // 다른 스크립트(PlayerController2D 등)가 읽는 누적 증강 효과
    private float damageBonus;
    private float attackRangeMultiplier = 1f;
    private float parryDurationBonus;
    private float parryCooldownReduction;

    public int Level => level;
    public int Xp => xp;
    public bool IsChoosingAugment => choosing;
    public AugmentConfig AugmentConfig => augmentConfig;
    public float DamageBonus => damageBonus;
    public float AttackRangeMultiplier => attackRangeMultiplier;
    public float ParryDurationBonus => parryDurationBonus;
    public float ParryCooldownReduction => parryCooldownReduction;

    /// <summary>증강 선택창을 띄워야 할 때 발행(선택지 종류·등급·현재 레벨). 여러 레벨을 한번에\n    /// 오르면 선택이 끝날 때마다 다음 선택을 위해 다시 발행된다.</summary>
    public event System.Action<AugmentType[], int[], int> OnAugmentChoiceReady;
    /// <summary>대기 중이던 증강 선택이 전부 끝났을 때(더 이상 띄울 선택지가 없을 때) 발행.</summary>
    public event System.Action OnAllAugmentChoicesComplete;

    private void Awake()
    {
        health = GetComponent<PlayerHealth>();
        controller = GetComponent<PlayerController2D>();
    }

    /// <summary>몬스터를 처치했을 때 등 경험치를 지급한다. 레벨업이 일어나면 증강 선택 이벤트를 발행한다.</summary>
    public void AddXp(int amount)
    {
        if (levelConfig == null || amount <= 0) return;

        xp += amount;
        int levelBefore = level;
        LevelProgressionLogic.TryLevelUp(xp, level, levelConfig.baseXpToLevel2, levelConfig.xpIncrementPerLevel,
            out int newLevel, out int remaining);
        xp = remaining;

        if (newLevel > levelBefore)
        {
            pendingAugmentChoices += newLevel - levelBefore;
            level = newLevel;
            if (!choosing)
            {
                canvas.SetActive(true);
                BeginAugmentChoice();
            }
        }
    }

    private void BeginAugmentChoice()
    {
        if (augmentConfig == null || levelConfig == null) return;

        var allTypes = (AugmentType[])System.Enum.GetValues(typeof(AugmentType));
        int count = Mathf.Min(levelConfig.choicesPerLevelUp, allTypes.Length);
        offeredTypes = new AugmentType[count];
        offeredTiers = new int[count];

        var pool = new System.Collections.Generic.List<AugmentType>(allTypes);
        float goldChance = LevelProgressionLogic.GoldChanceForLevel(level, levelConfig.goldBaseChance, levelConfig.goldChancePerLevel, levelConfig.goldMaxChance);
        float silverChance = LevelProgressionLogic.SilverChanceForLevel(level, levelConfig.silverBaseChance, levelConfig.silverChancePerLevel, levelConfig.silverMaxChance);

        for (int i = 0; i < count; i++)
        {
            int idx = Random.Range(0, pool.Count);
            offeredTypes[i] = pool[idx];
            pool.RemoveAt(idx);
            offeredTiers[i] = LevelProgressionLogic.TierForRoll(Random.value, goldChance, silverChance);
        }

        choosing = true;
        OnAugmentChoiceReady?.Invoke(offeredTypes, offeredTiers, level);

    }

    /// <summary>LevelUpSkillManager 등 UI 쪽에서 사용자가 고른 선택지 인덱스를 전달한다.</summary>
    public void ChooseAugment(int index)
    {
        if (!choosing || offeredTypes == null || index < 0 || index >= offeredTypes.Length) return;

        ApplyAugment(offeredTypes[index], offeredTiers[index]);

        choosing = false;
        pendingAugmentChoices = Mathf.Max(0, pendingAugmentChoices - 1);
        if (pendingAugmentChoices > 0)
            BeginAugmentChoice();
        else
            OnAllAugmentChoicesComplete?.Invoke();
    }

    private void ApplyAugment(AugmentType type, int tier)
    {
        float magnitude = augmentConfig.GetMagnitude(type, tier);
        switch (type)
        {
            case AugmentType.DamageUp:
                damageBonus += magnitude;
                break;
            case AugmentType.Heal:
                if (health != null) health.Heal(Mathf.RoundToInt(magnitude));
                break;
            case AugmentType.ManaHeal:
                if (playerMana != null) playerMana.MpHeal(Mathf.RoundToInt(magnitude));
                break;
            case AugmentType.ManaUp:
                if (playerMana != null) playerMana.config.parryGain += Mathf.RoundToInt(magnitude);
                break;
        }
    }
}
