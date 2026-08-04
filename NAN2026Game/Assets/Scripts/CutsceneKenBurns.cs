using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// 정지 이미지에 생동감을 주는 켄 번즈(Ken Burns) 효과.
/// 이 카메라가 화면에 잡히는 순간부터 지정한 시간 동안 천천히 줌·팬 합니다.
/// Timeline 이 샷을 전환하면 자동으로 시작되므로 별도 연결이 필요 없습니다.
/// </summary>
[RequireComponent(typeof(CinemachineCamera))]
public class CutsceneKenBurns : MonoBehaviour
{
    [Header("이동 (샷 기준 오프셋, 유닛)")]
    [SerializeField] private Vector2 startOffset = Vector2.zero;
    [SerializeField] private Vector2 endOffset = new Vector2(0.6f, 0f);

    [Header("줌 (Orthographic Size)")]
    [SerializeField] private float startSize = 5.0f;
    [SerializeField] private float endSize = 4.55f;

    [Header("진행 시간(초)")]
    [SerializeField] private float duration = 5f;

    [Tooltip("켜면 시작·끝이 부드럽게 감속합니다.")]
    [SerializeField] private bool easeInOut = true;

    private CinemachineCamera _vc;
    private Vector3 _home;
    private float _elapsed;
    private bool _running;

    private void Awake()
    {
        _vc = GetComponent<CinemachineCamera>();
        _home = transform.position;
        Apply(0f);
    }

    private void OnEnable()
    {
        _elapsed = 0f;
        _running = false;
        Apply(0f);
    }

    private void Update()
    {
        if (!_running)
        {
            // 이 카메라가 화면에 잡히기 시작하면 (블렌드 시작 포함) 진행
            if (!CinemachineCore.IsLive(_vc)) return;
            _running = true;
            _elapsed = 0f;
        }

        _elapsed += Time.deltaTime;
        float t = duration <= 0.001f ? 1f : Mathf.Clamp01(_elapsed / duration);
        if (easeInOut) t = t * t * (3f - 2f * t);
        Apply(t);
    }

    private void Apply(float t)
    {
        Vector2 off = Vector2.LerpUnclamped(startOffset, endOffset, t);
        transform.position = new Vector3(_home.x + off.x, _home.y + off.y, _home.z);

        var lens = _vc.Lens;
        lens.OrthographicSize = Mathf.LerpUnclamped(startSize, endSize, t);
        _vc.Lens = lens;
    }
}
