using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// 엔딩 영상 후 재생되는 스크롤 크레딧.
/// 텍스트가 아래에서 위로 흘러가고, 끝나면 타이틀 씬으로 돌아갑니다.
/// 스킵 조작은 Opening / Ending 연출과 동일하게 C 키 길게 누르기입니다.
/// </summary>
public class CreditsRoller : MonoBehaviour
{
    [Header("다음 씬")]
    [SerializeField] private string nextSceneName = "TitleScene";

    [Header("스크롤 대상 (크레딧 텍스트의 RectTransform)")]
    [SerializeField] private RectTransform scrollTarget;
    [Tooltip("초당 이동 픽셀 (Canvas 기준 좌표)")]
    [SerializeField] private float scrollSpeed = 90f;
    [Tooltip("시작 전 대기 시간")]
    [SerializeField] private float startDelay = 1.0f;
    [Tooltip("크레딧이 다 올라간 뒤 여운")]
    [SerializeField] private float endHoldSeconds = 1.5f;

    [Header("BGM")]
    [SerializeField] private AudioSource bgmSource;
    [Range(0f, 1f)]
    [SerializeField] private float bgmVolume = 0.7f;
    [Tooltip("페이드 아웃과 함께 BGM 볼륨도 줄인다")]
    [SerializeField] private bool fadeBgmOut = true;

    [Header("스킵 — C 키 길게 누르기")]
    [SerializeField] private bool allowSkip = true;
    [SerializeField] private float holdSeconds = 3f;
    [Tooltip("시작 직후 오조작 방지")]
    [SerializeField] private float skipLockSeconds = 0.5f;
    [SerializeField] private KeyCode skipKey = KeyCode.C;

    [Header("스킵 효과음")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip skipSfx;
    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 1f;

    [Header("UI")]
    [SerializeField] private GameObject skipHintUI;
    [Tooltip("Image Type = Filled 인 게이지")]
    [SerializeField] private Image skipProgressFill;

    [Header("페이드 아웃")]
    [SerializeField] private CanvasGroup fadeGroup;
    [SerializeField] private float fadeSeconds = 1.2f;

    private bool _finished;
    private float _elapsed;
    private float _hold;
    private float _startY;
    private float _endY;
    private float _bgmStartVolume;

    private void Start()
    {
        if (skipHintUI != null) skipHintUI.SetActive(false);
        if (skipProgressFill != null) skipProgressFill.fillAmount = 0f;
        if (fadeGroup != null) fadeGroup.alpha = 0f;

        if (bgmSource != null)
        {
            bgmSource.loop = true;
            bgmSource.volume = bgmVolume;
            _bgmStartVolume = bgmVolume;
            if (!bgmSource.isPlaying) bgmSource.Play();
        }

        SetupScrollRange();
        StartCoroutine(Roll());
    }

    private void SetupScrollRange()
    {
        if (scrollTarget == null) return;

        // TMP 는 첫 프레임 전에 높이가 0 일 수 있어 강제로 갱신한다.
        var tmp = scrollTarget.GetComponent<TMP_Text>();
        if (tmp != null)
        {
            tmp.ForceMeshUpdate();
            var fitter = scrollTarget.GetComponent<ContentSizeFitter>();
            if (fitter != null) LayoutRebuilder.ForceRebuildLayoutImmediate(scrollTarget);
        }

        float contentHeight = scrollTarget.rect.height;
        float screenHeight = 1080f;
        var canvas = scrollTarget.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            var canvasRect = canvas.GetComponent<RectTransform>();
            if (canvasRect != null) screenHeight = canvasRect.rect.height;
        }

        // 앵커/피벗은 화면 하단 기준. 전체가 화면 아래에 있는 위치에서 시작해서
        // 전체가 화면 위로 사라지는 위치에서 끝난다.
        _startY = -contentHeight;
        _endY = screenHeight;

        var pos = scrollTarget.anchoredPosition;
        pos.y = _startY;
        scrollTarget.anchoredPosition = pos;
    }

    private System.Collections.IEnumerator Roll()
    {
        float t = 0f;
        while (t < startDelay)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        while (!_finished && scrollTarget != null && scrollTarget.anchoredPosition.y < _endY)
        {
            var pos = scrollTarget.anchoredPosition;
            pos.y += scrollSpeed * Time.unscaledDeltaTime;
            scrollTarget.anchoredPosition = pos;
            yield return null;
        }

        if (_finished) yield break;

        float hold = 0f;
        while (hold < endHoldSeconds)
        {
            hold += Time.unscaledDeltaTime;
            yield return null;
        }

        Finish();
    }

    private void Update()
    {
        _elapsed += Time.unscaledDeltaTime;

        if (skipHintUI != null && !skipHintUI.activeSelf && _elapsed >= skipLockSeconds)
            skipHintUI.SetActive(true);

        if (_finished || !allowSkip || _elapsed < skipLockSeconds) return;

        if (SkipKeyHeld()) _hold += Time.unscaledDeltaTime;
        else _hold = 0f;

        if (skipProgressFill != null)
            skipProgressFill.fillAmount = holdSeconds <= 0.001f ? 1f : Mathf.Clamp01(_hold / holdSeconds);

        if (_hold >= holdSeconds) Skip();
    }

    public void Skip()
    {
        if (_finished) return;
        if (skipSfx != null)
        {
            if (sfxSource != null) sfxSource.PlayOneShot(skipSfx, sfxVolume);
            else AudioSource.PlayClipAtPoint(skipSfx, Camera.main != null ? Camera.main.transform.position : Vector3.zero, sfxVolume);
        }
        Finish();
    }

    private void Finish()
    {
        if (_finished) return;
        _finished = true;

        if (fadeGroup != null && fadeSeconds > 0f) StartCoroutine(FadeAndLoad());
        else LoadNext();
    }

    private System.Collections.IEnumerator FadeAndLoad()
    {
        float t = 0f;
        while (t < fadeSeconds)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / fadeSeconds);
            fadeGroup.alpha = k;
            if (fadeBgmOut && bgmSource != null) bgmSource.volume = Mathf.Lerp(_bgmStartVolume, 0f, k);
            yield return null;
        }
        LoadNext();
    }

    private void LoadNext()
    {
        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.Log("[CreditsRoller] 크레딧 종료. nextSceneName 이 비어 있어 씬 전환은 하지 않습니다.");
            return;
        }
        Time.timeScale = 1f;
        SceneManager.LoadScene(nextSceneName);
    }

    private bool SkipKeyHeld()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        var kb = UnityEngine.InputSystem.Keyboard.current;
        return kb != null && kb.cKey.isPressed;
#else
        return Input.GetKey(skipKey);
#endif
    }
}
