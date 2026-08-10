using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ESC 로 여닫는 일시정지 메뉴.
/// 게임 재개 / 옵션 / 게임 종료 3개 항목을 마우스와 키보드(위아래+엔터) 양쪽으로 조작합니다.
/// 옵션은 화면 전환만 하고 기능은 비어 있습니다.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    [Header("패널")]
    [SerializeField] private GameObject pauseRoot;
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject optionsPanel;

    [Header("메뉴 버튼 — 위에서 아래 순서")]
    [SerializeField] private List<Button> menuButtons = new List<Button>();

    [Header("버튼 이미지")]
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite selectedSprite;
    [SerializeField] private Color normalTextColor = new Color(0.24f, 0.16f, 0.10f);
    [SerializeField] private Color selectedTextColor = new Color(0.12f, 0.30f, 0.08f);

    [Header("타이틀 씬 이름")]
    [SerializeField] private string titleSceneName = "FirstTitle";

    private int _index;
    private bool _paused;

    public bool IsPaused { get { return _paused; } }

    private void Start()
    {
        if (pauseRoot != null) pauseRoot.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(false);
        _paused = false;
        Time.timeScale = 1f;
    }

    private void Update()
    {
        if (TogglePressed())
        {
            if (_paused && optionsPanel != null && optionsPanel.activeSelf) { CloseOptions(); return; }
            if (_paused) Resume(); else Pause();
            return;
        }

        if (!_paused) return;
        if (optionsPanel != null && optionsPanel.activeSelf) return;

        if (DownPressed()) Move(1);
        else if (UpPressed()) Move(-1);
        else if (SubmitPressed() && _index >= 0 && _index < menuButtons.Count)
            menuButtons[_index].onClick.Invoke();
    }

    public void Pause()
    {
        _paused = true;
        Time.timeScale = 0f;
        if (pauseRoot != null) pauseRoot.SetActive(true);
        if (mainPanel != null) mainPanel.SetActive(true);
        if (optionsPanel != null) optionsPanel.SetActive(false);
        _index = 0;
        Refresh();
    }

    public void Resume()
    {
        _paused = false;
        Time.timeScale = 1f;
        if (pauseRoot != null) pauseRoot.SetActive(false);
    }

    public void OpenOptions()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(true);
    }

    public void CloseOptions()
    {
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (mainPanel != null) mainPanel.SetActive(true);
        Refresh();
    }

    public void QuitToTitle()
    {
#if UNITY_EDITOR
        // 유니티 에디터에서 실행 중인 경우 Play 모드 종료
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // 빌드된 실제 게임에서 실행 중인 경우 애플리케이션 종료
        Application.Quit();
#endif
    }

    public void SetIndex(int i)
    {
        if (i < 0 || i >= menuButtons.Count) return;
        _index = i;
        Refresh();
    }

    private void Move(int delta)
    {
        if (menuButtons.Count == 0) return;
        _index = (_index + delta + menuButtons.Count) % menuButtons.Count;
        Refresh();
    }

    private void Refresh()
    {
        for (int i = 0; i < menuButtons.Count; i++)
        {
            var btn = menuButtons[i];
            if (btn == null) continue;
            bool on = (i == _index);

            var img = btn.GetComponent<Image>();
            if (img != null && normalSprite != null && selectedSprite != null)
                img.sprite = on ? selectedSprite : normalSprite;

            var label = btn.GetComponentInChildren<TMPro.TMP_Text>();
            if (label != null)
            {
                label.color = on ? selectedTextColor : normalTextColor;
                label.fontStyle = on ? TMPro.FontStyles.Bold : TMPro.FontStyles.Normal;
            }

            btn.transform.localScale = Vector3.one * (on ? 1.04f : 1f);
        }
    }

    private bool TogglePressed()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        var kb = UnityEngine.InputSystem.Keyboard.current;
        return kb != null && kb.escapeKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }

    private bool UpPressed()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        var kb = UnityEngine.InputSystem.Keyboard.current;
        return kb != null && (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W);
#endif
    }

    private bool DownPressed()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        var kb = UnityEngine.InputSystem.Keyboard.current;
        return kb != null && (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S);
#endif
    }

    private bool SubmitPressed()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        var kb = UnityEngine.InputSystem.Keyboard.current;
        return kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
#endif
    }
}
