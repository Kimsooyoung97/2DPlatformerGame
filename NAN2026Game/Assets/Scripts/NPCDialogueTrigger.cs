using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
/// <summary>
/// 플레이어가 범위 안에 들어오면 대화 UI를 띄우고, 벗어나면 닫습니다.
/// 지금은 형태 확인용이라 대사 진행(다음 줄 넘기기) 기능은 없습니다.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class NPCDialogueTrigger : MonoBehaviour
{
    [Header("대화 UI")]
    [SerializeField] private GameObject dialogueRoot;
    [SerializeField] private TMPro.TMP_Text speakerLabel;
    [SerializeField] private TMPro.TMP_Text bodyLabel;

    [Header("내용")]
    [SerializeField] private string speakerName = "마을 주민";
    [TextArea(2, 4)]
    [SerializeField] private string message = "여어, 모험가 양반!\n공주님이 잡혀갔다는 소문 들었나?";

    [Header("애니메이터")]
    [SerializeField] private Animator animator;
    [SerializeField] private string talkingBool = "Talking";

    [Header("상호작용 안내 (머리 위 표시)")]
    [SerializeField] private GameObject interactHint;

    private bool _inRange;
    private bool isOpen;
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
        isOpen = false;
        if (dialogueRoot != null) dialogueRoot.SetActive(false);
        if (interactHint != null) interactHint.SetActive(false);
    }
    private void Update()
    {
        if (dialogueRoot.activeSelf && isOpen)
        {
            if (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
            {
                Hide();
                isOpen = false;
                return;
            }
            
        }
        if (_inRange)
        {
            if (interactHint != null) interactHint.SetActive(true);

            if (!isOpen && Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
            {
                Show();
                isOpen = true;
                return;
            }
        }
        else 
        {
            interactHint.SetActive(false);
        }
        
        
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
        Hide();
    }

    private bool IsPlayer(Collider2D c)
    {
        if (c == null) return false;
        if (c.CompareTag("Player")) return true;
        return c.GetComponentInParent<PlayerController2D>() != null;
    }

    private void Show()
    {
        if (animator != null && !string.IsNullOrEmpty(talkingBool)) animator.SetBool(talkingBool, true);
        if (dialogueRoot == null) return;
        if (speakerLabel != null) speakerLabel.text = speakerName;
        if (bodyLabel != null) bodyLabel.text = message;
        dialogueRoot.SetActive(true);
    }

    private void Hide()
    {
        if (animator != null && !string.IsNullOrEmpty(talkingBool)) animator.SetBool(talkingBool, false);
        if (interactHint != null) interactHint.SetActive(false);
        if (dialogueRoot != null) dialogueRoot.SetActive(false);
    }

    public bool InRange { get { return _inRange; } }
}
