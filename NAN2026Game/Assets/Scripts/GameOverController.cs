using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// PlayerHealth.OnPlayerDied를 구독해 GameOverPanel을 띄우고, 아무 키(또는 마우스/게임패드
/// 시작 버튼) 입력 시 타이틀 씬으로 돌아간다. TitleScreen.cs / PauseMenu.cs와 동일한
/// 입력 감지 방식(Input System 우선, 레거시 Input 폴백)을 사용한다.
/// </summary>
public class GameOverController : MonoBehaviour
{
    [Header("연결")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private GameObject gameOverPanel;

    [Header("타이틀 씬")]
    [Tooltip("Build Settings에 등록된 씬 이름")]
    [SerializeField] private string titleSceneName = "TitleScene";

    [Header("오입력 방지")]
    [Tooltip("패널이 뜬 직후 이 시간(초) 동안은 키 입력을 무시한다. 죽는 순간 누르고 있던 키로 즉시 씬 전환되는 것을 막는다.")]
    [SerializeField] private float inputIgnoreDuration = 0.3f;

    [Header("사망 연출")]
    [Tooltip("사망 연출이 끝난 뒤 패널을 띄운다. PlayerHurtDeathFx 가 있으면 그 길이를 우선 사용한다.")]
    [SerializeField] private float minDeathSequenceDelay = 0f;

    private bool _waitingForInput;
    private float _acceptInputAt;

    private void Awake()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
    }

    private void OnEnable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnPlayerDied += HandlePlayerDied;
            playerHealth.SuppressRespawnOnDeath = true;   // 게임오버 노선: 체크포인트 부활과 경합 방지
        }
    }

    private void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnPlayerDied -= HandlePlayerDied;
            playerHealth.SuppressRespawnOnDeath = false;
        }
    }

    private void HandlePlayerDied()
    {
        StartCoroutine(ShowAfterDeathSequence());
    }

    /// 사망 연출을 끝까지 보여준 뒤에 패널을 띄우고 시간을 멈춘다.
    /// 즉시 timeScale=0 을 걸면 사망 애니메이션이 첫 프레임에서 정지한다.
    private System.Collections.IEnumerator ShowAfterDeathSequence()
    {
        float wait = minDeathSequenceDelay;
        if (playerHealth != null)
        {
            var fx = playerHealth.GetComponent<NAN2026.PlayerHurtDeathFx>();
            if (fx != null && fx.DeathDuration > wait) wait = fx.DeathDuration;
        }
        // 히트스톱으로 timeScale 이 0 일 수 있으므로 실시간 대기
        if (wait > 0f) yield return new WaitForSecondsRealtime(wait);

        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        Time.timeScale = 0;
        _waitingForInput = true;
        _acceptInputAt = Time.unscaledTime + inputIgnoreDuration;
    }

    private void Update()
    {
        if (!_waitingForInput) return;
        if (Time.unscaledTime < _acceptInputAt) return;
        if (!AnyKeyPressed()) return;

        _waitingForInput = false;
        Time.timeScale = 1f;   // 복구하지 않으면 타이틀에서 다시 시작한 게임이 정지 상태로 뜬다
        SceneManager.LoadScene(titleSceneName);
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
}
