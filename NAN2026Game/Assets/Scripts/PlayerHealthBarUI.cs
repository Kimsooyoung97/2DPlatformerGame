using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// PlayerHealth.OnHealthChanged를 구독해 Canvas의 Image(Filled)로 체력을 표시한다.
/// 게임 로직은 전혀 갖지 않고 화면 표시만 담당한다.
/// </summary>
public sealed class PlayerHealthBarUI : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;
    [Tooltip("Image Type=Filled(가로/Horizontal)로 설정된 체력 채움 이미지")]
    [SerializeField] private Image fillImage;
    [SerializeField] private TMP_Text label;

    private void OnEnable()
    {
        if (playerHealth != null) playerHealth.OnHealthChanged += HandleHealthChanged;
    }

    private void OnDisable()
    {
        if (playerHealth != null) playerHealth.OnHealthChanged -= HandleHealthChanged;
    }

    private void Start()
    {
        if (playerHealth != null) HandleHealthChanged(playerHealth.CurrentHealth, playerHealth.MaxHealth);
    }

    private void HandleHealthChanged(int current, int max)
    {
        if (fillImage != null) fillImage.fillAmount = max > 0 ? (float)current / max : 0f;
        if (label != null) label.text = current + " / " + max;
    }
}
