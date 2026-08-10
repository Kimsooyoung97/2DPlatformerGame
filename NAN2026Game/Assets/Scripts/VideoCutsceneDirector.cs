using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

/// <summary>
/// 영상 기반 연출(엔딩) 제어.
/// 영상이 끝나면 다음 씬으로 넘기고, C 키를 일정 시간 누르면 건너뜁니다.
/// Opening 의 CutsceneDirector 와 같은 조작 규칙을 씁니다.
/// </summary>
[RequireComponent(typeof(VideoPlayer))]
public class VideoCutsceneDirector : MonoBehaviour
{
    [Header("다음 씬 (비우면 전환하지 않고 로그만)")]
    [SerializeField] private string nextSceneName = "";

    [Header("WebGL 전용 재생 (VideoClip 에셋은 WebGL에서 재생 불가)")]
    [Tooltip("Assets/StreamingAssets 안에 넣은 영상 파일명. WebGL 빌드에서만 이 파일을 URL로 재생한다. 비워두면 기존 VideoClip 그대로 시도(WebGL에선 안 나올 수 있음).")]
    [SerializeField] private string webglStreamingAssetsFileName = "";

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

    [Header("페이드 아웃 (영상 종료 시)")]
    [SerializeField] private CanvasGroup fadeGroup;
    [SerializeField] private float fadeSeconds = 0.8f;

    private VideoPlayer _player;
    private bool _finished;
    private float _elapsed;
    private float _hold;

private void Awake()
    {
        _player = GetComponent<VideoPlayer>();

        // WebGL은 VideoClip 에셋(내부 트랜스코딩 포맷)을 재생하지 못한다 — StreamingAssets에 둔
        // 원본 파일을 URL 소스로 재생해야만 브라우저 네이티브 디코더로 나온다. 다른 플랫폼은
        // 기존 VideoClip 방식 그대로 둔다(멀쩡히 작동하던 걸 안 건드림).
#if UNITY_WEBGL && !UNITY_EDITOR
        if (!string.IsNullOrEmpty(webglStreamingAssetsFileName))
        {
            _player.source = VideoSource.Url;
            _player.url = System.IO.Path.Combine(Application.streamingAssetsPath, webglStreamingAssetsFileName);
        }
#endif

        _player.loopPointReached += OnVideoEnd;
        if (skipHintUI != null) skipHintUI.SetActive(false);
        if (skipProgressFill != null) skipProgressFill.fillAmount = 0f;
        if (fadeGroup != null) fadeGroup.alpha = 0f;
    }

    private void OnDestroy()
    {
        if (_player != null) _player.loopPointReached -= OnVideoEnd;
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
        if (_finished) return;
        if (_player != null && _player.isPlaying) _player.Stop();
        Finish();
    }

    private void OnVideoEnd(VideoPlayer vp) { Finish(); }

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
            fadeGroup.alpha = Mathf.Clamp01(t / fadeSeconds);
            yield return null;
        }
        LoadNext();
    }

    private void LoadNext()
    {
        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.Log("[VideoCutsceneDirector] 영상 종료. nextSceneName 이 비어 있어 씬 전환은 하지 않습니다.");
            return;
        }
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
