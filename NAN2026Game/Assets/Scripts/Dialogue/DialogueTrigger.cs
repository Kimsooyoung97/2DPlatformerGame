using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 대사 데이터 + 발동 조건. 씬의 빈 오브젝트에 붙여서 쓰고, 실제 출력은 DialogueWindow 가 한다.
/// 한 씬에 여러 개를 두어 상황별 대화를 나눌 수 있다.
/// </summary>
public class DialogueTrigger : MonoBehaviour
{
    public enum TriggerMode
    {
        SceneEnter,  // 씬에 들어오면 자동 재생
        Proximity,   // 지정한 대상에 일정 거리 안으로 들어오면 재생
        TriggerZone, // 이 오브젝트의 Trigger 콜라이더에 플레이어가 닿으면 재생
        Manual       // 다른 스크립트/이벤트가 PlayNow() 를 부를 때만
    }

    public enum ReplayPolicy
    {
        Always,         // 조건이 맞을 때마다
        OncePerSession, // 게임을 켜 둔 동안 1회
        OnceForever     // 저장에 기록해서 영구히 1회
    }

    [Header("발동 방식")]
    [SerializeField] private TriggerMode mode = TriggerMode.SceneEnter;
    [Tooltip("발동 조건이 충족된 뒤 창이 열리기까지의 대기 시간")]
    [SerializeField] private float startDelay = 0.8f;

    [Header("Proximity 설정")]
    [Tooltip("이 대상에 가까워지면 발동. 비우면 이 오브젝트 자신")]
    [SerializeField] private Transform target;
    [SerializeField] private float radius = 7f;
    [Tooltip("대상이 실제로 보이는 상태(SpriteRenderer 켜짐)일 때만 발동. 등장 연출 전 보스에는 반응하지 않게 함")]
    [SerializeField] private bool requireTargetVisible = true;

    [Header("대사")]
    [SerializeField] private List<DialogueLine> lines = new List<DialogueLine>();

    [Header("재생 조건")]
    [SerializeField] private ReplayPolicy replayPolicy = ReplayPolicy.OncePerSession;
    [Tooltip("OncePerSession / OnceForever 에서 쓰이는 키. 대화마다 다르게 지정")]
    [SerializeField] private string saveKey = "Dialogue_Unnamed";

    [Header("대화가 끝난 뒤")]
    [SerializeField] private UnityEvent onComplete;

        private static readonly HashSet<string> _sessionPlayed = new HashSet<string>();

    // 이 프로젝트는 Enter Play Mode Options 에서 Domain Reload 를 꺼 두었다.
    // 그래서 static 값이 Play 를 멈춰도 에디터에 그대로 남는다. 초기화해 주지 않으면
    // 한 번 재생한 대사가 다음 Play 에서 영영 안 나온다. (Scene2Director 와 같은 처리)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticsOnPlay()
    {
        _sessionPlayed.Clear();
    }

    private Transform _player;
    private bool _fired;

    private void Start()
    {
        if (mode == TriggerMode.SceneEnter && CanPlay()) StartCoroutine(PlayAfterDelay());
    }

    private void Update()
    {
        if (mode != TriggerMode.Proximity) return;
        if (_fired || !CanPlay()) return;

        var t = target != null ? target : transform;
        if (requireTargetVisible && !IsVisible(t)) return;

        var p = FindPlayer();
        if (p == null) return;

        if (Vector2.Distance(p.position, t.position) <= radius)
        {
            _fired = true;
            StartCoroutine(PlayAfterDelay());
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (mode != TriggerMode.TriggerZone) return;
        if (_fired || !CanPlay()) return;
        if (!IsPlayer(other)) return;

        _fired = true;
        StartCoroutine(PlayAfterDelay());
    }

    /// <summary>Manual 모드용. UnityEvent 나 다른 스크립트에서 직접 호출.</summary>
    public void PlayNow()
    {
        if (!CanPlay()) return;
        _fired = true;
        StartCoroutine(PlayAfterDelay());
    }

    private IEnumerator PlayAfterDelay()
    {
        float t = 0f;
        while (t < startDelay) { t += Time.unscaledDeltaTime; yield return null; }

        var win = DialogueWindow.Instance;
        if (win == null)
        {
            Debug.LogWarning("[DialogueTrigger] 씬에 DialogueWindow 가 없습니다. DialogueCanvas 프리팹을 넣어주세요.", this);
            _fired = false;
            yield break;
        }

        if (!win.Play(lines, OnDialogueFinished))
        {
            // 다른 대화가 진행 중이면 이번 발동은 취소하고 다음 기회를 노린다
            _fired = false;
        }
    }

    private void OnDialogueFinished()
    {
        MarkPlayed();
        if (onComplete != null) onComplete.Invoke();
    }

    // ---------------- 조건 ----------------

    private bool CanPlay()
    {
        if (lines == null || lines.Count == 0) return false;

        switch (replayPolicy)
        {
            case ReplayPolicy.OncePerSession:
                return !_sessionPlayed.Contains(saveKey);
            case ReplayPolicy.OnceForever:
                return PlayerPrefs.GetInt(saveKey, 0) == 0;
            default:
                return true;
        }
    }

    private void MarkPlayed()
    {
        if (replayPolicy == ReplayPolicy.OncePerSession)
        {
            _sessionPlayed.Add(saveKey);
        }
        else if (replayPolicy == ReplayPolicy.OnceForever)
        {
            PlayerPrefs.SetInt(saveKey, 1);
            PlayerPrefs.Save();
        }
    }

    private Transform FindPlayer()
    {
        if (_player != null) return _player;

        // 프로젝트의 플레이어 탐색 단일 창구를 쓴다 (씬마다 이름이 Player / RealPlayer 로 다름)
        var go = NAN2026.PlayerLocator.Find();
        if (go != null) { _player = go.transform; return _player; }

        var pc = FindFirstObjectByType<PlayerController2D>();
        if (pc != null) { _player = pc.transform; return _player; }

        return null;
    }

    private bool IsPlayer(Collider2D c)
    {
        if (c == null) return false;
        if (c.CompareTag("Player")) return true;
        return c.GetComponentInParent<PlayerController2D>() != null;
    }

    private bool IsVisible(Transform t)
    {
        if (t == null) return false;
        if (!t.gameObject.activeInHierarchy) return false;
        var sr = t.GetComponentInChildren<SpriteRenderer>();
        return sr == null || sr.enabled;
    }

    private void OnDrawGizmosSelected()
    {
        if (mode != TriggerMode.Proximity) return;
        var t = target != null ? target : transform;
        Gizmos.color = new Color(1f, 0.85f, 0.3f, 0.8f);
        Gizmos.DrawWireSphere(t.position, radius);
    }
}
