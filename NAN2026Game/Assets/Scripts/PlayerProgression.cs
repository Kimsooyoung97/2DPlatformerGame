using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NAN2026.Core;
using NAN2026.Showroom;

/// <summary>
/// 플레이어 경험치/레벨 추적 + 레벨업 시 브론즈/실버/골드 증강 3택 UI.
/// 씬에 배치된 LevelUpCanvas(LevelUpPanel + 버튼 3개)를 그대로 사용한다(OnGUI 아님).
/// 순수 판정(XP 곡선, 등급 확률)은 NAN2026.Core.LevelProgressionLogic이 갖고 있고,
/// 이 클래스는 그 결과를 받아 UI에 반영하고 실제 효과를 적용하는 역할만 한다.
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerProgression : MonoBehaviour
{
    [SerializeField] private LevelProgressionConfig levelConfig;
    [SerializeField] private AugmentConfig augmentConfig;

    [Header("LevelUpCanvas 연결 (씬의 LevelUpPanel 및 하위 버튼 3개)")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Image[] cardBackgrounds;
    [SerializeField] private Button[] choiceButtons;
    [SerializeField] private TMP_Text[] choiceTexts;

    private PlayerHealth health;

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
    public float DamageBonus => damageBonus;
    public float AttackRangeMultiplier => attackRangeMultiplier;
    public float ParryDurationBonus => parryDurationBonus;
    public float ParryCooldownReduction => parryCooldownReduction;

    private void Awake()
    {
        health = GetComponent<PlayerHealth>();
        if (panel != null) panel.SetActive(false);
    }

    /// <summary>몬스터를 처치했을 때 등 경험치를 지급한다. 레벨업이 일어나면 증강 선택 UI를 띄운다.</summary>
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
            if (!choosing) BeginAugmentChoice();
        }
    }

    private void BeginAugmentChoice()
    {
        if (augmentConfig == null || levelConfig == null || panel == null) return;

        var allTypes = (AugmentType[])System.Enum.GetValues(typeof(AugmentType));
        int count = Mathf.Min(levelConfig.choicesPerLevelUp, allTypes.Length);
        count = Mathf.Min(count, choiceButtons != null ? choiceButtons.Length : count);
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
        Time.timeScale = 0f;
        RefreshUI(count);
        panel.SetActive(true);
    }

    private void RefreshUI(int count)
    {
        if (titleText != null) titleText.text = "Level UP! Lv." + level;

        for (int i = 0; i < count; i++)
        {
            int captured = i; // 클로저 캡처용 지역 변수
            string tierName = offeredTiers[i] == 2 ? "GOLD" : offeredTiers[i] == 1 ? "SILVER" : "BRONZE";
            string desc = DescribeAugment(offeredTypes[i], offeredTiers[i]);

            if (choiceTexts != null && i < choiceTexts.Length && choiceTexts[i] != null)
                choiceTexts[i].text = "[" + tierName + "]\n" + desc;

            if (cardBackgrounds != null && i < cardBackgrounds.Length && cardBackgrounds[i] != null)
                cardBackgrounds[i].color = offeredTiers[i] == 2 ? new Color(1f, 0.85f, 0.3f)
                    : offeredTiers[i] == 1 ? new Color(0.8f, 0.85f, 0.92f)
                    : new Color(0.82f, 0.55f, 0.35f);

            if (choiceButtons != null && i < choiceButtons.Length && choiceButtons[i] != null)
            {
                choiceButtons[i].onClick.RemoveAllListeners();
                choiceButtons[i].onClick.AddListener(() => ChooseAugment(captured));
                choiceButtons[i].gameObject.SetActive(true);
            }
        }

        // 이번에 제시하는 개수보다 버튼이 더 많으면 나머지는 숨긴다.
        if (choiceButtons != null)
            for (int i = count; i < choiceButtons.Length; i++)
                if (choiceButtons[i] != null) choiceButtons[i].gameObject.SetActive(false);
    }

    public void ChooseAugment(int index)
    {
        if (!choosing || offeredTypes == null || index < 0 || index >= offeredTypes.Length) return;

        ApplyAugment(offeredTypes[index], offeredTiers[index]);

        choosing = false;
        pendingAugmentChoices = Mathf.Max(0, pendingAugmentChoices - 1);
        if (pendingAugmentChoices > 0)
        {
            BeginAugmentChoice();
        }
        else
        {
            Time.timeScale = 1f;
            if (panel != null) panel.SetActive(false);
        }
    }

    private void ApplyAugment(AugmentType type, int tier)
    {
        float magnitude = augmentConfig.GetMagnitude(type, tier);
        switch (type)
        {
            case AugmentType.ParryCooldownDown:
                parryCooldownReduction += magnitude;
                break;
            case AugmentType.ParryDurationUp:
                parryDurationBonus += magnitude;
                break;
            case AugmentType.DamageUp:
                damageBonus += magnitude;
                break;
            case AugmentType.Heal:
                if (health != null) health.Heal(Mathf.RoundToInt(magnitude));
                break;
            case AugmentType.MaxHealthUp:
                if (health != null) health.AddMaxHealthBonus(Mathf.RoundToInt(magnitude));
                break;
            case AugmentType.AttackRangeUp:
                attackRangeMultiplier += magnitude;
                break;
        }
    }

    private string DescribeAugment(AugmentType type, int tier)
    {
        float m = augmentConfig.GetMagnitude(type, tier);
        switch (type)
        {
            case AugmentType.ParryCooldownDown: return "패링 쿨타임\n-" + m + "초";
            case AugmentType.ParryDurationUp: return "패링 지속시간\n+" + m + "초";
            case AugmentType.DamageUp: return "공격 데미지\n+" + m;
            case AugmentType.Heal: return "체력 회복\n+" + m;
            case AugmentType.MaxHealthUp: return "최대 체력\n+" + m;
            case AugmentType.AttackRangeUp: return "공격 사거리\n+" + Mathf.RoundToInt(m * 100f) + "%";
            default: return string.Empty;
        }
    }
}
