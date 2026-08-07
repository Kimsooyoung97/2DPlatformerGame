using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// PlayerHealth.OnHealthChanged를 구독해 체력을 하트(프리팹) 개수로 표시한다.
/// 현재 체력만큼 parentObject 아래에 prefab을 생성/삭제해 개수를 맞춘다.
/// 게임 로직은 전혀 갖지 않고 화면 표시만 담당한다.
/// </summary>
public sealed class PlayerHealthBarUI : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;
    [Tooltip("체력 1당 하나씩 생성될 프리팹(하트 아이콘 등)")]
    [SerializeField] private GameObject prefab;
    [Tooltip("프리팹 인스턴스들이 자식으로 들어갈 부모 오브젝트")]
    [SerializeField] private GameObject parentObject;

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

    /// <summary>parentObject 아래 자식 개수를 현재 체력(current)에 맞춘다.
    /// 부족하면 그만큼 prefab을 더 생성하고, 남으면 뒤에서부터 그만큼 삭제한다.</summary>
    private void HandleHealthChanged(int current, int max)
    {
        if (parentObject == null || prefab == null) return;

        // 비정상적으로 큰 값이 들어와도 하트를 무한정 생성하며 멈추지 않도록 방어.
        current = Mathf.Clamp(current, 0, 999);

        int existing = parentObject.transform.childCount;

        if (existing < current)
        {
            for (int i = existing; i < current; i++)
            {
                Instantiate(prefab, parentObject.transform);
            }
        }
        else if (existing > current)
        {
            for (int i = existing - 1; i >= current; i--)
            {
                Transform child = parentObject.transform.GetChild(i);
                Destroy(child.gameObject);
            }
        }
    }
}
