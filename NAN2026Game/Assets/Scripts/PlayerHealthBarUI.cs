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
    [Tooltip("현재체력만큼 채워서 보여줄 스프라이트 (예: 16x16 Heart Health Red_0)")]
    [SerializeField] private Sprite filledSprite;
    [Tooltip("최대체력 중 비어있는 칸에 보여줄 스프라이트 (예: 16x16 Heart Health Red_2). 미지정 시 기존 채움 스프라이트를 그대로 유지한다.")]
    [SerializeField] private Sprite emptySprite;

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
        max = Mathf.Clamp(max, 0, 999);

        int existing = parentObject.transform.childCount;

        // GridLayoutGroup 등 레이아웃 컴포넌트가 같은 오브젝트에 있으면, 반복문 안에서
        // Instantiate/Destroy를 연달아 호출할 때마다 레이아웃을 즉시 재계산하려다 걸리는
        // 경우가 있다. 바뀌는 동안은 레이아웃 컴포넌트를 꺼롘다가, 다 끝난 뒤 한 번만 다시 켜다.
        LayoutGroup layoutGroup = parentObject.GetComponent<LayoutGroup>();
        if (layoutGroup != null) layoutGroup.enabled = false;

        // 하트 개수는 현재체력이 아니라 최대체력 기준으로 맞춘다 — 그래야 증강으로 늘어난
        // 최대체력을 빈 하트로 함께 보여줄 수 있다.
        if (existing < max)
        {
            for (int i = existing; i < max; i++)
            {
                Instantiate(prefab, parentObject.transform);
            }
        }
        else if (existing > max)
        {
            for (int i = existing - 1; i >= max; i--)
            {
                Transform child = parentObject.transform.GetChild(i);
                child.gameObject.SetActive(false); // 비활성화하면 즉시 화면·레이아웃 계산에서 빠진다
                Destroy(child.gameObject);
            }
        }

        // 칸별로 현재체력 이내면 채움, 그 이상(최대체력까지)이면 빈 상태로 표시해
        // 현재 최대체력이 몇인지를 보여준다.
        for (int i = 0; i < max; i++)
        {
            Transform child = parentObject.transform.GetChild(i);
            Image image = child.GetComponent<Image>();
            if (image == null) continue;

            Sprite target = i < current ? filledSprite : emptySprite;
            if (target != null) image.sprite = target;
        }

        if (layoutGroup != null) layoutGroup.enabled = true;
    }
}
