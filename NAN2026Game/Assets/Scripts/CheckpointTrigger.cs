using UnityEngine;

namespace NAN2026
{
    /// <summary>
    /// 씬에 배치하는 체크포인트 트리거. BoxCollider2D 영역에 플레이어가 들어오면
    /// PlayerHealth.SetCheckpoint(Vector3)를 호출해 리스폰 지점을 갱신한다.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class CheckpointTrigger : MonoBehaviour
    {
        [SerializeField] private string playerTag = "Player";

        [Tooltip("비워두면 이 오브젝트(콜라이더) 자신의 위치를 체크포인트로 쓴다. " +
                 "콜라이더가 발판처럼 넓게 깔려있어서 실제 스폰 지점을 따로 지정하고 싶으면 여기에 연결.")]
        [SerializeField] private Transform checkpointPosition;

        [Tooltip("한 번 밟으면 다시 반응하지 않게 할지. 여러 번 지나가도 계속 최신 위치로 " +
                 "갱신하고 싶으면 꺼둔다(기본값).")]
        [SerializeField] private bool triggerOnce = false;

        private bool consumed;

        private void Awake()
        {
            BoxCollider2D col = GetComponent<BoxCollider2D>();
            col.isTrigger = true; // 체크포인트는 플레이어를 밀지 않고 감지만 해야 하므로 강제로 트리거화
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (triggerOnce && consumed) return;
            if (!other.CompareTag(playerTag)) return;

            PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
            if (health == null) return;

            Vector3 point = checkpointPosition != null ? checkpointPosition.position : transform.position;
            health.SetCheckpoint(point);
            consumed = true;
        }
    }
}