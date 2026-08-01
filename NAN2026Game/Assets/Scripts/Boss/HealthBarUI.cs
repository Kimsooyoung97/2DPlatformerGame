using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 캐릭터 머리 위에 뜨는 심플한 월드스페이스 체력바.
/// 별도 프리팹/에셋 없이 코드에서 Canvas + Image 두 장으로 즉석 생성합니다.
/// 나중에 예쁜 UI 에셋으로 교체하고 싶으면 이 컴포넌트만 바꿔치기하면 됩니다.
/// </summary>
public class HealthBarUI : MonoBehaviour
{
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 3.6f, 0f);
    [SerializeField] private Vector2 barSize = new Vector2(2.2f, 0.28f);
    [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0.65f);

    private Image fillImage;
    private Func<float> getCurrent;
    private Func<float> getMax;

    /// <param name="currentGetter">현재 체력을 반환하는 함수</param>
    /// <param name="maxGetter">최대 체력을 반환하는 함수</param>
    /// <param name="fillColor">체력바 채워지는 색</param>
    public void Init(Func<float> currentGetter, Func<float> maxGetter, Color fillColor)
    {
        getCurrent = currentGetter;
        getMax = maxGetter;
        BuildUI(fillColor);
    }

    private void BuildUI(Color fillColor)
    {
        GameObject canvasGO = new GameObject("HealthBarCanvas");
        canvasGO.transform.SetParent(transform, false);
        canvasGO.transform.localPosition = worldOffset;
        canvasGO.transform.localRotation = Quaternion.identity;
        canvasGO.transform.localScale = Vector3.one * 0.01f; // 월드 유닛 크기로 축소

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 200; // 스프라이트보다 위에 그려지도록

        var rt = canvas.GetComponent<RectTransform>();
        Vector2 pixelSize = barSize * 100f;
        rt.sizeDelta = pixelSize;

        GameObject bgGO = new GameObject("Background");
        bgGO.transform.SetParent(canvasGO.transform, false);
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color = backgroundColor;
        var bgRect = bgGO.GetComponent<RectTransform>();
        bgRect.sizeDelta = pixelSize;
        bgRect.anchoredPosition = Vector2.zero;

        GameObject fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(canvasGO.transform, false);
        fillImage = fillGO.AddComponent<Image>();
        fillImage.color = fillColor;
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImage.fillAmount = 1f;
        var fillRect = fillGO.GetComponent<RectTransform>();
        // 배경보다 살짝 안쪽으로 (테두리처럼 보이게)
        fillRect.sizeDelta = pixelSize - new Vector2(6f, 6f);
        fillRect.anchoredPosition = Vector2.zero;
    }

    void LateUpdate()
    {
        if (fillImage == null || getCurrent == null || getMax == null) return;

        float max = getMax();
        float ratio = max > 0f ? Mathf.Clamp01(getCurrent() / max) : 0f;
        fillImage.fillAmount = ratio;
    }
}
