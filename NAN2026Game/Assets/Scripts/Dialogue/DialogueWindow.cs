// 대사창 UI 본체 (DialogueTrigger 와 쌍)
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>초상화가 붙는 쪽. 보스 대화처럼 화자가 둘일 때 좌/우로 나눠서 쓴다.</summary>
public enum PortraitSide
{
    Left,
    Right
}

/// <summary>
/// 대사창의 크기·여백·글자 크기를 한곳에서 조절하기 위한 묶음.
/// 여기 값만 바꾸면 자식 오브젝트(창/초상화/본문)의 RectTransform 이 자동으로 맞춰진다.
/// 수치는 CanvasScaler 기준 해상도(1920x1080) 기준 px.
/// </summary>
[Serializable]
public class DialogueLayout
{
    [Header("창 크기")]
    [Tooltip("대사창 전체 가로/세로 크기")]
    public Vector2 windowSize = new Vector2(1120f, 330f);
    [Tooltip("화면 아래에서 창을 띄울 높이")]
    public float windowBottomMargin = 46f;

    [Header("초상화")]
    [Tooltip("초상화 표시 크기 (가로, 세로)")]
    public Vector2 portraitSize = new Vector2(245f, 305f);
    [Tooltip("창 모서리에서 초상화까지의 여백 (가로, 세로)")]
    public Vector2 portraitMargin = new Vector2(40f, 30f);

    [Header("본문 여백")]
    [Tooltip("초상화가 있는 쪽 여백. 보통 (초상화 가로 + 여백)보다 조금 크게")]
    public float bodyMarginPortraitSide = 300f;
    [Tooltip("초상화 반대쪽 여백")]
    public float bodyMarginOppositeSide = 88f;
    public float bodyMarginTop = 112f;
    public float bodyMarginBottom = 58f;

    [Header("글자 크기")]
    public float speakerFontSize = 34f;
    public float bodyFontSize = 32f;
    public float hintFontSize = 30f;
}

/// <summary>대사 한 줄. 화자 이름과 초상화를 줄 단위로 지정한다.</summary>
[Serializable]
public class DialogueLine
{
    public string speakerName = "주인공";
    [Tooltip("이 줄에서 보여줄 초상화. 비우면 DialogueWindow 의 '기본 초상화' 가 대신 쓰인다")]
    public Sprite portrait;
    [Tooltip("초상화를 창의 어느 쪽에 붙일지. 보스 대사 줄만 Right 로 두면 좌우로 주고받는 그림이 된다")]
    public PortraitSide portraitSide = PortraitSide.Left;    [Tooltip("이 줄에서만 쓸 초상화 박스 크기(px). (0,0)이면 DialogueWindow 의 기본 크기(layout.portraitSize)를 쓴다. " +
             "원본 그림 비율이 달라서 한쪽만 작아 보일 때 여기서 줄 단위로 키운다")]
    public Vector2 portraitSizeOverride = Vector2.zero;

    [TextArea(2, 4)] public string text = "";
    [Tooltip("다 출력한 뒤 자동으로 넘어가는 시간(초). 0이면 입력을 기다림")]
    public float autoAdvance = 0f;
}

/// <summary>
/// 대사창 UI 본체. 씬마다 프리팹으로 하나만 두고, DialogueTrigger 들이 이걸 호출해서 쓴다.
/// 대화 중에는 게임을 일시 정지(Time.timeScale = 0)하고 플레이어 입력을 잠근다.
/// 정지 중에도 연출이 돌아가야 하므로 내부 타이머는 모두 unscaledDeltaTime 을 쓴다.
/// </summary>
public class DialogueWindow : MonoBehaviour
{
    public static DialogueWindow Instance { get; private set; }

    /// <summary>다른 시스템(일시정지 메뉴 등)이 대화 중인지 확인할 수 있게 공개</summary>
        public static bool DialogueActive { get; private set; }

    // Domain Reload 를 꺼 둔 프로젝트라 Play 를 멈춰도 static 이 남는다.
    // 이전 세션의 파괴된 Instance / 켜진 채로 남은 DialogueActive 를 정리한다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticsOnPlay()
    {
        Instance = null;
        DialogueActive = false;
    }

    [Header("UI 참조")]
    [SerializeField] private GameObject dialogueRoot;
    [SerializeField] private TMP_Text speakerLabel;
    [SerializeField] private TMP_Text bodyLabel;
    [SerializeField] private UnityEngine.UI.Image portraitImage;
    [Tooltip("대사가 다 출력됐을 때만 깜빡이는 표시")]
    [SerializeField] private GameObject moreHint;
    [SerializeField] private CanvasGroup fadeGroup;

    [Header("크기 · 글자 조절")]
    [Tooltip("여기 값을 바꾸면 인스펙터에서 바로 창/초상화/글자 크기가 반영된다")]
    [SerializeField] private DialogueLayout layout = new DialogueLayout();
    [Tooltip("대사 줄에 초상화를 비워 뒀을 때 대신 쓸 기본 초상화 (주인공_0)")]
    [SerializeField] private Sprite defaultPortrait;

    [Header("연출")]
    [Tooltip("한 글자당 걸리는 시간(초). 0이면 즉시 표시")]
    [SerializeField] private float charInterval = 0.045f;
    [SerializeField] private float fadeDuration = 0.25f;
    [SerializeField] private float hintBlinkInterval = 0.5f;
    [Tooltip("대화 중 플레이어 조작을 막을지")]
    [SerializeField] private bool lockPlayerInput = true;
    [Tooltip("대화 중 게임을 일시 정지할지 (Time.timeScale = 0)")]
    [SerializeField] private bool pauseGame = true;

    [Header("사운드 (선택)")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip typeSfx;
    [SerializeField] private int typeSfxStride = 3;

    private readonly List<DialogueLine> _lines = new List<DialogueLine>();
    private Action _onComplete;

    private bool _isOpen;
    private bool _typing;
    private int _index;
    private Coroutine _typeRoutine;
    private Coroutine _blinkRoutine;
    private Coroutine _fadeRoutine;
    private float _prevTimeScale = 1f;

    private PortraitSide _currentSide = PortraitSide.Left;
    private bool _currentHasPortrait = true;    private Vector2 _currentSizeOverride = Vector2.zero;


    public bool IsOpen { get { return _isOpen; } }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[DialogueWindow] 씬에 대사창이 2개 이상입니다. 나중 것을 비활성화합니다.", this);
            gameObject.SetActive(false);
            return;
        }
        Instance = this;

        ApplyLayout();

        if (dialogueRoot != null) dialogueRoot.SetActive(false);
        if (moreHint != null) moreHint.SetActive(false);
    }

    private void OnDestroy()
    {
        if (_isOpen) ReleaseGameState();
        if (Instance == this) Instance = null;
    }

    // ---------------- 레이아웃 (크기 · 글자 조절) ----------------

    /// <summary>현재 상태 기준으로 크기/글자 설정을 다시 적용</summary>
    [ContextMenu("레이아웃 다시 적용")]
    public void ApplyLayout()
    {
        ApplyLayout(_currentSide, _currentHasPortrait);
    }

    /// <summary>줄별 초상화 크기 오버라이드까지 함께 적용한다. (0,0)이면 기본 크기 사용.</summary>
    public void ApplyLayout(PortraitSide side, bool hasPortrait, Vector2 sizeOverride)
    {
        _currentSizeOverride = sizeOverride;
        ApplyLayout(side, hasPortrait);
    }
    /// <summary>초상화 위치(좌/우)와 유무에 맞춰 창·초상화·본문 크기를 갱신</summary>
    public void ApplyLayout(PortraitSide side, bool hasPortrait)
    {
        if (layout == null) return;

        _currentSide = side;
        _currentHasPortrait = hasPortrait;
        // 줄별 오버라이드가 있으면 그 크기를, 없으면 공용 기본값을 쓴다.
        Vector2 usedPortraitSize = (_currentSizeOverride.x > 0f && _currentSizeOverride.y > 0f)
            ? _currentSizeOverride
            : layout.portraitSize;
        // 오버라이드로 초상화가 넓어진 만큼만 본문을 더 밀어낸다(기본 크기일 땐 기존 여백 그대로).
        float portraitExtraWidth = Mathf.Max(0f, usedPortraitSize.x - layout.portraitSize.x);


        // 창 본체
        var root = dialogueRoot != null ? dialogueRoot.transform as RectTransform : null;
        if (root != null)
        {
            root.anchorMin = new Vector2(0.5f, 0f);
            root.anchorMax = new Vector2(0.5f, 0f);
            root.pivot = new Vector2(0.5f, 0f);
            root.sizeDelta = layout.windowSize;
            root.anchoredPosition = new Vector2(0f, layout.windowBottomMargin);
        }

        // 초상화
        if (portraitImage != null)
        {
            var pr = portraitImage.rectTransform;
            pr.sizeDelta = usedPortraitSize;

            if (side == PortraitSide.Left)
            {
                pr.anchorMin = pr.anchorMax = pr.pivot = new Vector2(0f, 0f);
                pr.anchoredPosition = new Vector2(layout.portraitMargin.x, layout.portraitMargin.y);
            }
            else
            {
                pr.anchorMin = pr.anchorMax = pr.pivot = new Vector2(1f, 0f);
                pr.anchoredPosition = new Vector2(-layout.portraitMargin.x, layout.portraitMargin.y);
            }
        }

        // 본문
        if (bodyLabel != null)
        {
            float near = hasPortrait ? layout.bodyMarginPortraitSide + portraitExtraWidth : layout.bodyMarginOppositeSide;
            float far = layout.bodyMarginOppositeSide;
            float left = (side == PortraitSide.Left) ? near : far;
            float right = (side == PortraitSide.Left) ? far : near;

            var br = bodyLabel.rectTransform;
            br.anchorMin = new Vector2(0f, 0f);
            br.anchorMax = new Vector2(1f, 1f);
            br.offsetMin = new Vector2(left, layout.bodyMarginBottom);
            br.offsetMax = new Vector2(-right, -layout.bodyMarginTop);

            bodyLabel.fontSize = layout.bodyFontSize;
        }

        if (speakerLabel != null) speakerLabel.fontSize = layout.speakerFontSize;

        if (moreHint != null)
        {
            var hintText = moreHint.GetComponent<TMP_Text>();
            if (hintText != null) hintText.fontSize = layout.hintFontSize;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying) return;
        ApplyLayout(_currentSide, _currentHasPortrait);
    }
#endif

    // ---------------- 외부에서 부르는 진입점 ----------------

    /// <summary>대사 목록을 재생한다. 이미 열려 있으면 무시한다.</summary>
    public bool Play(IList<DialogueLine> lines, Action onComplete = null)
    {
        if (_isOpen) return false;

        if (_fadeRoutine != null)
        {
            StopCoroutine(_fadeRoutine);
            _fadeRoutine = null;
            if (dialogueRoot != null) dialogueRoot.SetActive(false);
            Finish();
        }

        if (lines == null || lines.Count == 0)
        {
            Debug.LogWarning("[DialogueWindow] 재생할 대사가 없습니다.", this);
            return false;
        }

        _lines.Clear();
        for (int i = 0; i < lines.Count; i++) _lines.Add(lines[i]);
        _onComplete = onComplete;

        _isOpen = true;
        _index = 0;
        HoldGameState();

        if (dialogueRoot != null) dialogueRoot.SetActive(true);
        if (fadeGroup != null) StartCoroutine(Fade(0f, 1f));

        ShowLine(_index);
        return true;
    }

    // ---------------- 게임 상태 ----------------

    private void HoldGameState()
    {
        DialogueActive = true;
        if (lockPlayerInput) PlayerController2D.InputLocked = true;
        if (pauseGame)
        {
            _prevTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;
        }
    }

    private void ReleaseGameState()
    {
        DialogueActive = false;
        if (lockPlayerInput) PlayerController2D.InputLocked = false;
        if (pauseGame) Time.timeScale = _prevTimeScale > 0f ? _prevTimeScale : 1f;
    }

    // ---------------- 대화 흐름 ----------------

    private void Update()
    {
        if (!_isOpen) return;
        if (WasAdvancePressed()) Advance();
    }

    private bool WasAdvancePressed()
    {
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.enterKey.wasPressedThisFrame) return true;
            if (kb.numpadEnterKey.wasPressedThisFrame) return true;
            if (kb.spaceKey.wasPressedThisFrame) return true;
        }
        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame) return true;
        return false;
    }

    /// <summary>타이핑 중이면 즉시 완성, 아니면 다음 대사. 마지막이면 닫는다.</summary>
    public void Advance()
    {
        if (!_isOpen) return;

        if (_typing)
        {
            CompleteTyping();
            return;
        }

        _index++;
        if (_index >= _lines.Count)
        {
            Close();
            return;
        }
        ShowLine(_index);
    }

    private void ShowLine(int i)
    {
        var line = _lines[i];

        if (speakerLabel != null) speakerLabel.text = line.speakerName;

        var sprite = line.portrait != null ? line.portrait : defaultPortrait;
        if (portraitImage != null)
        {
            portraitImage.sprite = sprite;
            portraitImage.enabled = (sprite != null);
        }

        // 화자가 바뀌면 초상화 위치와 본문 여백이 따라 움직인다 (보스 대화용)
        ApplyLayout(line.portraitSide, sprite != null, line.portraitSizeOverride);

        SetHintVisible(false);

        if (_typeRoutine != null) StopCoroutine(_typeRoutine);
        if (bodyLabel == null) return;

        bodyLabel.text = line.text;

        if (charInterval <= 0f)
        {
            bodyLabel.maxVisibleCharacters = int.MaxValue;
            _typing = false;
            SetHintVisible(true);
            if (line.autoAdvance > 0f) StartCoroutine(AutoAdvanceAfter(line.autoAdvance, i));
        }
        else
        {
            _typeRoutine = StartCoroutine(TypeText(line, i));
        }
    }

    private IEnumerator TypeText(DialogueLine line, int lineIndex)
    {
        _typing = true;
        bodyLabel.maxVisibleCharacters = 0;
        bodyLabel.ForceMeshUpdate();

        int total = bodyLabel.textInfo.characterCount;
        int shown = 0;

        while (shown < total)
        {
            shown++;
            bodyLabel.maxVisibleCharacters = shown;

            if (sfxSource != null && typeSfx != null && typeSfxStride > 0 && shown % typeSfxStride == 0)
                sfxSource.PlayOneShot(typeSfx);

            yield return WaitUnscaled(charInterval);
        }

        _typing = false;
        _typeRoutine = null;
        SetHintVisible(true);

        if (line.autoAdvance > 0f) yield return AutoAdvanceAfter(line.autoAdvance, lineIndex);
    }

    private IEnumerator AutoAdvanceAfter(float delay, int lineIndex)
    {
        float t = 0f;
        while (t < delay)
        {
            if (!_isOpen || _index != lineIndex) yield break;
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        if (_isOpen && _index == lineIndex && !_typing) Advance();
    }

    private void CompleteTyping()
    {
        if (_typeRoutine != null) StopCoroutine(_typeRoutine);
        _typeRoutine = null;
        _typing = false;
        if (bodyLabel != null) bodyLabel.maxVisibleCharacters = int.MaxValue;
        SetHintVisible(true);
    }

    // ---------------- ▼ 깜빡임 ----------------

    private void SetHintVisible(bool on)
    {
        if (moreHint == null) return;

        if (_blinkRoutine != null)
        {
            StopCoroutine(_blinkRoutine);
            _blinkRoutine = null;
        }

        moreHint.SetActive(on);
        if (on && hintBlinkInterval > 0f) _blinkRoutine = StartCoroutine(BlinkHint());
    }

    private IEnumerator BlinkHint()
    {
        bool on = true;
        while (moreHint != null)
        {
            yield return WaitUnscaled(hintBlinkInterval);
            on = !on;
            if (moreHint != null) moreHint.SetActive(on);
        }
    }

    // ---------------- 닫기 ----------------

    public void Close()
    {
        if (!_isOpen) return;

        _isOpen = false;
        _typing = false;
        if (_typeRoutine != null) StopCoroutine(_typeRoutine);
        _typeRoutine = null;
        SetHintVisible(false);

        if (fadeGroup != null && gameObject.activeInHierarchy)
        {
            _fadeRoutine = StartCoroutine(FadeOutAndFinish());
        }
        else
        {
            if (dialogueRoot != null) dialogueRoot.SetActive(false);
            Finish();
        }
    }

    private IEnumerator FadeOutAndFinish()
    {
        yield return Fade(1f, 0f);
        _fadeRoutine = null;
        if (_isOpen) yield break;
        if (dialogueRoot != null) dialogueRoot.SetActive(false);
        Finish();
    }

    private void Finish()
    {
        ReleaseGameState();
        var cb = _onComplete;
        _onComplete = null;
        if (cb != null) cb();
    }

    private IEnumerator Fade(float from, float to)
    {
        if (fadeGroup == null) yield break;

        fadeGroup.alpha = from;
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            fadeGroup.alpha = Mathf.Lerp(from, to, fadeDuration <= 0f ? 1f : t / fadeDuration);
            yield return null;
        }
        fadeGroup.alpha = to;
    }

    private IEnumerator WaitUnscaled(float seconds)
    {
        float t = 0f;
        while (t < seconds) { t += Time.unscaledDeltaTime; yield return null; }
    }
}
