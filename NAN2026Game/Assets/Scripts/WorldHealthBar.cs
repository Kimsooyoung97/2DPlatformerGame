using UnityEngine;
using NAN2026.Core;
using NHNDemo;

/// <summary>
/// 몬스터 머리 위 체력바. UI Canvas를 쓰지 않고 SpriteRenderer 두 장(배경/채움)으로
/// 직접 그린다. MonsterHealth.OnHealthChanged 이벤트를 구독해 데미지를 입는 즉시
/// (다음 프레임을 기다리지 않고) 갱신된다.
/// </summary>
[DisallowMultipleComponent]
public sealed class WorldHealthBar : MonoBehaviour
{
    [SerializeField] private EnemyAIConfig config;
    [SerializeField] private MonsterHealth target;

    private Transform fillTransform;
    private SpriteRenderer fillRenderer;
    private static Sprite whitePixel;

    private void Awake()
    {
        if (target == null) target = GetComponent<MonsterHealth>();
        BuildBar();
    }

    private void OnEnable()
    {
        if (target != null) target.OnHealthChanged += HandleHealthChanged;
    }

    private void OnDisable()
    {
        if (target != null) target.OnHealthChanged -= HandleHealthChanged;
    }

    private static Sprite GetWhitePixel()
    {
        if (whitePixel != null) return whitePixel;

        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        // 필 스프라이트는 pivot을 왼쪽(0, 0.5)에 둬서, 스케일을 줄여도
        // 왼쪽 끝이 고정된 채로 오른쪽부터 줄어드는 체력바를 만들 수 있다.
        whitePixel = Sprite.Create(tex, new Rect(0f, 0f, 1f, 1f), new Vector2(0f, 0.5f), 1f);
        return whitePixel;
    }

    private static Sprite GetCenteredWhitePixel()
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
    }

    private void BuildBar()
    {
        Vector3 offset = config != null ? config.healthBarOffset : new Vector3(0f, 1.6f, 0f);
        Vector2 size = config != null ? config.healthBarSize : new Vector2(1.2f, 0.16f);
        Color bg = config != null ? config.healthBarBackground : new Color(0f, 0f, 0f, 0.75f);
        Color fill = config != null ? config.healthBarFill : new Color(0.85f, 0.15f, 0.15f, 1f);

        GameObject root = new GameObject("HealthBar");
        root.transform.SetParent(transform, false);
        root.transform.localPosition = offset;

        GameObject bgGO = new GameObject("Background");
        bgGO.transform.SetParent(root.transform, false);
        SpriteRenderer bgSr = bgGO.AddComponent<SpriteRenderer>();
        bgSr.sprite = GetCenteredWhitePixel();
        bgSr.color = bg;
        bgSr.sortingOrder = 60;
        bgGO.transform.localScale = new Vector3(size.x, size.y, 1f);

        GameObject fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(root.transform, false);
        fillRenderer = fillGO.AddComponent<SpriteRenderer>();
        fillRenderer.sprite = GetWhitePixel();
        fillRenderer.color = fill;
        fillRenderer.sortingOrder = 61;
        fillTransform = fillGO.transform;
        fillTransform.localPosition = new Vector3(-size.x * 0.5f, 0f, 0f);
        fillTransform.localScale = new Vector3(size.x, size.y * 0.7f, 1f);

        if (target != null)
            HandleHealthChanged(target.CurrentHealth, target.MaxHealth);
    }

    private void HandleHealthChanged(int current, int max)
    {
        if (fillTransform == null) return;

        float ratio = EnemyAILogic.HealthRatio(current, max);
        Vector2 size = config != null ? config.healthBarSize : new Vector2(1.2f, 0.16f);
        Vector3 scale = fillTransform.localScale;
        scale.x = size.x * ratio;
        fillTransform.localScale = scale;
    }
}
