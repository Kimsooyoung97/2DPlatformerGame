using UnityEngine;

/// <summary>
/// 특정 몬스터(예: KeyMonster)가 죽으면 지정한 게이트 오브젝트를 비활성화해
/// 잠긴 길을 연다. MonsterHealth.OnDied 이벤트에 연결만 하는 단순 배선 컴포넌트라
/// 튜닝할 수치가 없다 (Config 불필요).
/// </summary>
[DisallowMultipleComponent]
public sealed class KeyMonsterGate : MonoBehaviour
{
    [SerializeField] private NHNDemo.MonsterHealth health;
    [Tooltip("이 몬스터가 죽으면 SetActive(false)로 비활성화할 오브젝트 (예: 잠긴 문/타일맵)")]
    [SerializeField] private GameObject gateObject;

    private void Awake()
    {
        if (health == null) health = GetComponent<NHNDemo.MonsterHealth>();
        if (health != null) health.OnDied += HandleDied;
    }

    private void OnDestroy()
    {
        if (health != null) health.OnDied -= HandleDied;
    }

    private void HandleDied()
    {
        if (gateObject != null) gateObject.SetActive(false);
    }
}
