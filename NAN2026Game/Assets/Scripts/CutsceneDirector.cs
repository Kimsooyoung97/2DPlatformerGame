using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

/// <summary>
/// 연출(컷신) 제어. Timeline 이 끝나면 다음 씬으로 넘기고,
/// C 키를 일정 시간 누르고 있으면 건너뜁니다.
/// </summary>
[RequireComponent(typeof(PlayableDirector))]
public class CutsceneDirector : MonoBehaviour
{
    [Header("다음 씬")]
    [SerializeField] private string nextSceneName = "ThirdScene";

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
    [Tooltip("Image Type = Filled 인 게이지. 누른 시간만큼 채워집니다.")]
    [SerializeField] private Image skipProgressFill;

    private PlayableDirector _director;
    private bool _finished;
    private float _elapsed;
    private float _hold;

    private void Awake()
    {
        _director = GetComponent<PlayableDirector>();
        _director.stopped += OnDirectorStopped;
        if (skipHintUI != null) skipHintUI.SetActive(false);
        if (skipProgressFill != null) skipProgressFill.fillAmount = 0f;
    }

    private void OnDestroy()
    {
        if (_director != null) _director.stopped -= OnDirectorStopped;
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

        if (_director != null && _director.playableAsset != null)
        {
            _director.time = _director.duration;
            _director.Evaluate();
            _director.Stop();
        }
        else Finish();
    }

    private void OnDirectorStopped(PlayableDirector d) { Finish(); }

    private void Finish()
    {
        if (_finished) return;
        _finished = true;

        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.LogWarning("[CutsceneDirector] nextSceneName 이 비어 있습니다.");
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
