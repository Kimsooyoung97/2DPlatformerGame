using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// 타이틀 화면 제어: BGM 무한 반복, "아무 키 입력 시 게임 시작" 텍스트 점멸, 아무 키 입력 시 다음 씬 로드.
/// </summary>
public class TitleScreen : MonoBehaviour
{
    [Header("Press Any Key 텍스트")]
    [SerializeField] private TMP_Text pressAnyKeyText;
    [SerializeField] private float blinkCycle = 1.4f;
    [SerializeField] private float minAlpha = 0.2f;
    [SerializeField] private float maxAlpha = 1.0f;

    [Header("BGM (무한 반복)")]
    [SerializeField] private AudioSource bgmSource;

    [Header("다음 씬")]
    [Tooltip("비워두면 씬 전환 없이 로그만 남깁니다. Build Settings에 등록된 씬 이름을 입력하세요.")]
    [SerializeField] private string nextSceneName = "";
    [SerializeField] private float fadeOutBeforeLoad = 0.0f;

    private bool _started;

    private void Awake()
    {
        if (bgmSource != null)
        {
            bgmSource.loop = true;
            bgmSource.playOnAwake = true;
            if (!bgmSource.isPlaying) bgmSource.Play();
        }
    }

    private void Update()
    {
        Blink();

        if (_started) return;
        if (!AnyKeyPressed()) return;

        _started = true;
        StartGame();
    }

    private void Blink()
    {
        if (pressAnyKeyText == null) return;

        float t = (Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f / Mathf.Max(0.01f, blinkCycle)) + 1f) * 0.5f;
        Color c = pressAnyKeyText.color;
        c.a = Mathf.Lerp(minAlpha, maxAlpha, t);
        pressAnyKeyText.color = c;
    }

    private bool AnyKeyPressed()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        if (keyboard != null && keyboard.anyKey.wasPressedThisFrame) return true;

        var mouse = UnityEngine.InputSystem.Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame) return true;

        var gamepad = UnityEngine.InputSystem.Gamepad.current;
        if (gamepad != null && gamepad.startButton.wasPressedThisFrame) return true;

        return false;
#else
        return Input.anyKeyDown;
#endif
    }

    private void StartGame()
    {
        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.LogWarning("[TitleScreen] nextSceneName이 비어 있습니다. 인스펙터에서 다음 씬 이름을 지정하세요.");
            _started = false;
            return;
        }

        if (fadeOutBeforeLoad > 0f && bgmSource != null)
        {
            StartCoroutine(FadeAndLoad());
            return;
        }

        SceneManager.LoadScene(nextSceneName);
    }

    private System.Collections.IEnumerator FadeAndLoad()
    {
        float startVolume = bgmSource.volume;
        float elapsed = 0f;

        while (elapsed < fadeOutBeforeLoad)
        {
            elapsed += Time.unscaledDeltaTime;
            bgmSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / fadeOutBeforeLoad);
            yield return null;
        }

        SceneManager.LoadScene(nextSceneName);
    }
}
