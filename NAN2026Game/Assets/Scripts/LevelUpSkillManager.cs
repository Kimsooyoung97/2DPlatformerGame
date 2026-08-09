using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// PlayerProgression의 증강 선택 이벤트를 구독해 LevelUpCanvas(패널+버튼 3개)를
/// 실제로 표시·제어하는 역할만 담당한다. 게임 로직(효과 적용 등)은 전혀 갖지 않고
/// PlayerProgression.ChooseAugment로 사용자의 선택만 전달한다.
/// </summary>
public sealed class LevelUpSkillManager : MonoBehaviour
{
    [SerializeField] private PlayerProgression playerProgression;

    [Header("LevelUpCanvas 연결 (씬의 LevelUpPanel 및 하위 버튼 3개)")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Image[] cardBackgrounds;
    [SerializeField] private Button[] choiceButtons;
    [SerializeField] private TMP_Text[] choiceTexts;

    private void Awake()
    {
        if (panel != null) panel.SetActive(false);
    }

    private void OnEnable()
    {
        if (playerProgression == null) return;
        playerProgression.OnAugmentChoiceReady += HandleChoiceReady;
        playerProgression.OnAllAugmentChoicesComplete += HandleAllComplete;
    }

    private void OnDisable()
    {
        if (playerProgression == null) return;
        playerProgression.OnAugmentChoiceReady -= HandleChoiceReady;
        playerProgression.OnAllAugmentChoicesComplete -= HandleAllComplete;
    }

    private void HandleChoiceReady(AugmentType[] types, int[] tiers, int level)
    {
        if (panel == null) return;

        Time.timeScale = 0f;
        RefreshUI(types, tiers, level);
        panel.SetActive(true);
    }

    private void HandleAllComplete()
    {
        Time.timeScale = 1f;
        if (panel != null) panel.SetActive(false);

    }

    private void RefreshUI(AugmentType[] types, int[] tiers, int level)
    {
        if (titleText != null) titleText.text = "Level UP! Lv." + level;

        int count = types.Length;
        for (int i = 0; i < count; i++)
        {
            int captured = i; // 클로저 캡처용 지역 변수
            string tierName = tiers[i] == 2 ? "GOLD" : tiers[i] == 1 ? "SILVER" : "BRONZE";
            string desc = DescribeAugment(types[i], tiers[i], i);

            if (choiceTexts != null && i < choiceTexts.Length && choiceTexts[i] != null)
                choiceTexts[i].text = "[" + tierName + "]\n" + desc;

            if (cardBackgrounds != null && i < cardBackgrounds.Length && cardBackgrounds[i] != null)
                cardBackgrounds[i].color = tiers[i] == 2 ? new Color(1f, 0.85f, 0.3f)
                    : tiers[i] == 1 ? new Color(0.8f, 0.85f, 0.92f)
                    : new Color(0.82f, 0.55f, 0.35f);

            if (choiceButtons != null && i < choiceButtons.Length && choiceButtons[i] != null)
            {
                choiceButtons[i].onClick.RemoveAllListeners();
                choiceButtons[i].onClick.AddListener(() => playerProgression.ChooseAugment(captured));
                choiceButtons[i].gameObject.SetActive(true);
            }
        }

        if (choiceButtons != null)
            for (int i = count; i < choiceButtons.Length; i++)
                if (choiceButtons[i] != null) choiceButtons[i].gameObject.SetActive(false);
    }

    private string DescribeAugment(AugmentType type, int tier, int idx)
    {
        if (playerProgression == null || playerProgression.AugmentConfig == null) return string.Empty;
        float m = playerProgression.AugmentConfig.GetMagnitude(type, tier);

        // 스킬 획득 타입만 카드 아이콘을 스킬 스프라이트로 바꾼다(그 외 6종은 원래 아이콘 유지).
        
        switch (type)
        {
            case AugmentType.DamageUp:
                skillIcon[idx].sprite = Resources.Load<Sprite>("DamageUp");
                return "공격 데미지\n+" + m;
            case AugmentType.Heal:
                skillIcon[idx].sprite = Resources.Load<Sprite>("Heal");
                return "체력 회복\n+" + m;
            case AugmentType.ManaUp:
                skillIcon[idx].sprite = Resources.Load<Sprite>("ManaUp");
                return "마나 수급량\n+" + m;
            case AugmentType.ManaHeal:
                skillIcon[idx].sprite = Resources.Load<Sprite>("ManaHeal");
                return "마나 회복\n+" + m;
            default: return string.Empty;
        }
    }
}
