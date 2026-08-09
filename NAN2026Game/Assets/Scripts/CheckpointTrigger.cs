using UnityEngine;
using UnityEngine.InputSystem;

namespace NAN2026
{
    /// <summary>
    /// 씬에 배치하는 체크포인트 트리거. BoxCollider2D 영역에 플레이어가 들어오면
    /// PlayerHealth.SetCheckpoint(Vector3)를 호출해 세이브포인트를 하나 누적 저장한다.
    /// 플레이어가 이 영역 안에 있는 동안 Enter키를 누르면, NPC 대화창처럼 지금까지 저장된
    /// 세이브포인트 목록을 보여주는 CheckpointTravelMenu를 연다(다른 씬 지점도 선택 가능).
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class CheckpointTrigger : MonoBehaviour
    {
        [SerializeField] private string playerTag = "Player";

        [Tooltip("비워두면 이 오브젝트(콜라이더) 자신의 위치를 체크포인트로 쓴다. " +
                 "콜라이더가 발판처럼 넓게 깔려있어서 실제 스폰 지점을 따로 지정하고 싶으면 여기에 연결.")]
        [SerializeField] private Transform checkpointPosition;

        [Tooltip("한 번 밟으면 다시 저장하지 않을지. 여러 번 지나가면 그때마다 새 항목을 " +
                 "누적 저장하고 싶으면 꺼둔다(기본값) — 같은 자리를 여러 번 밟아도 목록에 " +
                 "중복으로 계속 쌓이니, 반복 저장을 원치 않으면 켜라.")]
        [SerializeField] private bool triggerOnce = false;

        private bool consumed;
        private bool playerInside;
        private PlayerHealth insidePlayerHealth;

        private void Awake()
        {
            BoxCollider2D col = GetComponent<BoxCollider2D>();
            col.isTrigger = true; // 체크포인트는 플레이어를 밀지 않고 감지만 해야 하므로 강제로 트리거화
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag(playerTag)) return;

            PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
            if (health == null) return;

            playerInside = true;
            insidePlayerHealth = health;

            if (triggerOnce && consumed) return;

            Vector3 point = checkpointPosition != null ? checkpointPosition.position : transform.position;
            health.SetCheckpoint(point);
            consumed = true;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!other.CompareTag(playerTag)) return;
            playerInside = false;
            insidePlayerHealth = null;
        }

        private void Update()
        {
            if (!playerInside || insidePlayerHealth == null) return;

            Keyboard kb = Keyboard.current;
            if (kb == null) return;

            if ((kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
                && CheckpointTravelMenu.Instance != null)
            {
                CheckpointTravelMenu.Instance.Open(insidePlayerHealth);
            }
        }
    }
}
