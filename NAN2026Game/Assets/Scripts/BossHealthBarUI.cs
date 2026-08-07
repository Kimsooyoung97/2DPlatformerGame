using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 화면 우측 상단에 고정으로 떠 있는 보스 전용 체력바(UI Canvas, Image Type=Filled).
/// 몬스터 머리 위에 뜨는 WorldHealthBar(월드 스페이스, 모든 몬스터 공용)와는 별개로,
/// 보스 1마리만을 위한 화면 고정 UI다. NHNDemo.MonsterHealth의 OnHealthChanged/OnDied를
/// 그대로 구독하므로 보스 쪽 체력 로직은 전혀 건드리지 않는다.
/// </summary>
public sealed class BossHealthBarUI : MonoBehaviour
{
    [SerializeField] private NHNDemo.MonsterHealth bossHealth;
    [Tooltip("Image Type=Filled(가로/Horizontal)로 설정된 체력 채움 이미지")]
    [SerializeField] private Image fillImage;
    [SerializeField] private TMP_Text label;
    [SerializeField] private TMP_Text bossNameLabel;
    [Tooltip("체력바 전체를 감싸는 루트. 보스가 아직 없거나 죽으면 꺼진다. 비워두면 이 오브젝트 자신을 쓴다")]
    [SerializeField] private GameObject root;

    private void Awake()
    {
        if (root == null) root = gameObject;
    }

    private void OnEnable()
    {
        if (bossHealth != null)
        {
            bossHealth.OnHealthChanged += HandleHealthChanged;
            bossHealth.OnDied += HandleDied;
        }
    }

    private void OnDisable()
    {
        if (bossHealth != null)
        {
            bossHealth.OnHealthChanged -= HandleHealthChanged;
            bossHealth.OnDied -= HandleDied;
        }
    }

    private void Start()
    {
        if (bossHealth != null)
        {
            if (root != null) root.SetActive(true);
            HandleHealthChanged(bossHealth.CurrentHealth, bossHealth.MaxHealth);
        }
        else if (root != null)
        {
            root.SetActive(false);
        }
    }

    /// <summary>보스가 나중에 스폰되는 구조라면, 등장하는 순간 이 메서드로 대상을 연결한다.</summary>
    public void SetBoss(NHNDemo.MonsterHealth newBossHealth)
    {
        if (bossHealth != null)
        {
            bossHealth.OnHealthChanged -= HandleHealthChanged;
            bossHealth.OnDied -= HandleDied;
        }

        bossHealth = newBossHealth;

        if (bossHealth != null)
        {
            bossHealth.OnHealthChanged += HandleHealthChanged;
            bossHealth.OnDied += HandleDied;
            if (root != null) root.SetActive(true);
            HandleHealthChanged(bossHealth.CurrentHealth, bossHealth.MaxHealth);
        }
        else if (root != null)
        {
            root.SetActive(false);
        }
    }

    private void HandleHealthChanged(int current, int max)
    {
        if (fillImage != null) fillImage.fillAmount = max > 0 ? (float)current / max : 0f;
        if (label != null) label.text = current + " / " + max;
    }

    private void HandleDied()
    {
        if (root != null) root.SetActive(false);
    }
}
