using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// NPC 대화. 범위 안에서 Enter 로 시작하고, Enter 를 누를 때마다 다음 대사로 넘어갑니다.
/// 대사마다 화자(NPC/주인공)를 지정하면 이름과 초상화가 자동으로 바뀝니다.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class NPCDialogueTrigger : MonoBehaviour
{
    public enum Speaker { NPC, Player }

    [System.Serializable]
    public class Line
    {
        public Speaker speaker = Speaker.NPC;
        [TextArea(2, 4)] public string text = "";
    }

    [Header("대화 UI")]
    [SerializeField] private GameObject dialogueRoot;
    [SerializeField] private TMPro.TMP_Text speakerLabel;
    [SerializeField] private TMPro.TMP_Text bodyLabel;
    [Tooltip("화자에 따라 그림이 바뀌는 초상화 Image")]
    [SerializeField] private UnityEngine.UI.Image portraitImage;
    [Tooltip("대사가 끝났을 때만 깜빡이는 ▼ 표시")]
    [SerializeField] private GameObject moreHint;

    [Header("초상화")]
    [SerializeField] private Sprite npcPortrait;
    [SerializeField] private Sprite playerPortrait;

    [Header("이름")]
    [SerializeField] private string npcName = "제임스";
    [SerializeField] private string playerName = "주인공";

    [Header("대사 (위에서 아래 순서)")]
    [SerializeField] private List<Line> lines = new List<Line>();

    [Header("타이핑 연출")]
    [Tooltip("한 글자당 걸리는 시간(초). 0이면 즉시 표시")]
    [SerializeField] private float charInterval = 0.03f;

    [Header("대화가 끝나면 이동할 씬 (비우면 이동 안 함)")]
    [SerializeField] private string sceneOnFinish = "";
    [Tooltip("마지막 대사 후 씬 전환까지의 여유 시간")]
    [SerializeField] private float delayBeforeLoad = 0.4f;

    [Header("애니메이터")]
    [SerializeField] private Animator animator;
    [SerializeField] private string talkingBool = "Talking";

    [Header("상호작용 안내 (머리 위 표시)")]
    [SerializeField] private GameObject interactHint;

    private bool _inRange;
    private bool _isOpen;
    private int _index;
    private bool _typing;
    private Coroutine _typeRoutine;

    public bool InRange { get { return _inRange; } }
    public bool IsOpen { get { return _isOpen; } }

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
    }

    private void Start()
    {
        _isOpen = false;
        if (dialogueRoot != null) dialogueRoot.SetActive(false);
        if (interactHint != null) interactHint.SetActive(false);
    }

    private void Update()
    {
        bool enter = Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame;

        if (_isOpen)
        {
            if (enter) Advance();
            return;
        }

        if (_inRange)
        {
            if (interactHint != null && !interactHint.activeSelf) interactHint.SetActive(true);
            if (enter) Open();
        }
        else if (interactHint != null && interactHint.activeSelf)
        {
            interactHint.SetActive(false);
        }
    }

    // ---------------- 대화 흐름 ----------------

    private void Open()
    {
        if (lines == null || lines.Count == 0)
        {
            Debug.LogWarning("[NPCDialogueTrigger] 대사가 비어 있습니다.");
            return;
        }

        _isOpen = true;
        _index = 0;
        if (animator != null && !string.IsNullOrEmpty(talkingBool)) animator.SetBool(talkingBool, true);
        if (interactHint != null) interactHint.SetActive(false);
        if (dialogueRoot != null) dialogueRoot.SetActive(true);
        ShowLine(_index);
    }

    /// <summary>타이핑 중이면 즉시 완성, 아니면 다음 대사. 마지막이면 닫는다.</summary>
    private void Advance()
    {
        if (_typing)
        {
            CompleteTyping();
            return;
        }

        _index++;
        if (_index >= lines.Count) { Close(true); return; }
        ShowLine(_index);
    }

    private void ShowLine(int i)
    {
        var line = lines[i];
        bool isPlayer = line.speaker == Speaker.Player;

        if (speakerLabel != null) speakerLabel.text = isPlayer ? playerName : npcName;
        if (portraitImage != null)
        {
            var spr = isPlayer ? playerPortrait : npcPortrait;
            portraitImage.sprite = spr;
            portraitImage.enabled = (spr != null);
        }

        if (moreHint != null) moreHint.SetActive(false);

        if (_typeRoutine != null) StopCoroutine(_typeRoutine);
        if (charInterval <= 0f)
        {
            if (bodyLabel != null) bodyLabel.text = line.text;
            _typing = false;
            if (moreHint != null) moreHint.SetActive(true);
        }
        else
        {
            _typeRoutine = StartCoroutine(TypeText(line.text));
        }
    }

    private IEnumerator TypeText(string full)
    {
        _typing = true;
        if (bodyLabel != null) bodyLabel.text = "";

        for (int c = 0; c < full.Length; c++)
        {
            if (bodyLabel != null) bodyLabel.text = full.Substring(0, c + 1);
            float t = 0f;
            while (t < charInterval) { t += Time.deltaTime; yield return null; }
        }

        _typing = false;
        if (moreHint != null) moreHint.SetActive(true);
    }

    private void CompleteTyping()
    {
        if (_typeRoutine != null) StopCoroutine(_typeRoutine);
        _typing = false;
        if (bodyLabel != null && _index < lines.Count) bodyLabel.text = lines[_index].text;
        if (moreHint != null) moreHint.SetActive(true);
    }

    private void Close(bool completed = false)
    {
        _isOpen = false;
        _typing = false;
        if (_typeRoutine != null) StopCoroutine(_typeRoutine);
        if (animator != null && !string.IsNullOrEmpty(talkingBool)) animator.SetBool(talkingBool, false);
        if (dialogueRoot != null) dialogueRoot.SetActive(false);
        if (moreHint != null) moreHint.SetActive(false);

        if (completed && !string.IsNullOrEmpty(sceneOnFinish))
            StartCoroutine(LoadAfterDelay());
    }

    private IEnumerator LoadAfterDelay()
    {
        float t = 0f;
        while (t < delayBeforeLoad) { t += Time.unscaledDeltaTime; yield return null; }
        SceneManager.LoadScene(sceneOnFinish);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsPlayer(other)) return;
        _inRange = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsPlayer(other)) return;
        _inRange = false;
        if (_isOpen) Close(false);
        if (interactHint != null) interactHint.SetActive(false);
    }

    private bool IsPlayer(Collider2D c)
    {
        if (c == null) return false;
        if (c.CompareTag("Player")) return true;
        return c.GetComponentInParent<PlayerController2D>() != null;
    }
}
