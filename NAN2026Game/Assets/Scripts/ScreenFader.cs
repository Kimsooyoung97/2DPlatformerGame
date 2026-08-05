using System.Collections;
using UnityEngine;

/// <summary>
/// 화면 전체를 검게 덮는 페이드 유틸리티(OnGUI 기반, Canvas 불필요).
/// 씬에 하나만 있으면 되므로 필요할 때 자동 생성되는 싱글턴으로 둔다.
/// </summary>
public sealed class ScreenFader : MonoBehaviour
{
    private static ScreenFader instance;
    private float alpha;

    public static ScreenFader Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("ScreenFader");
                instance = go.AddComponent<ScreenFader>();
                Object.DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    private void OnGUI()
    {
        if (alpha <= 0f) return;
        Color prev = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, alpha);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = prev;
    }

    /// <summary>alpha를 targetAlpha까지 duration초 동안 선형으로 바꾼다(코루틴, yield return으로 대기 가능).</summary>
    public IEnumerator FadeTo(float targetAlpha, float duration)
    {
        float start = alpha;
        if (duration <= 0f)
        {
            alpha = targetAlpha;
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            alpha = Mathf.Lerp(start, targetAlpha, Mathf.Clamp01(t / duration));
            yield return null;
        }
        alpha = targetAlpha;
    }
}
