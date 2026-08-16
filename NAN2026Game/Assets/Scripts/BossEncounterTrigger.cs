using UnityEngine;

namespace NAN2026
{
    /// <summary>
    /// 씬에 배치하는 보스 등장 트리거. BoxCollider2D 영역에 플레이어가 들어오면
    /// 비활성 상태로 대기 중이던 보스 오브젝트를 SetActive(true)로 깨운다.
    ///
    /// 보스(DemonBoss 등)는 Start()에서 곧바로 등장 인트로를 재생하므로,
    /// 씬 입장과 동시에 연출이 나가지 않게 하려면 보스를 꺼둔 채 두고
    /// 이 트리거가 실제 조우 시점에 켜주는 방식이 가장 간단하다.
    ///
    /// resetBossOnRespawn이 켜져 있으면 PlayerHealth.OnPlayerRespawned를 구독해,
    /// 플레이어가 죽고 체크포인트에서 부활할 때마다 보스를 끄고 DemonBoss.ResetBoss()로
    /// 초기 상태(체력·위치·그로기·쿨타임)로 되돌린 뒤 트리거를 재무장한다.
    ///
    /// AdventureScene4 배치 예: 상단 발판(y≈14)에서 출발한 플레이어가 우측으로 이동해
    /// 아래 아레나 바닥(y≈-3.95)으로 떨어지면 그 순간 데몬 보스가 등장한다.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class BossEncounterTrigger : MonoBehaviour
    {
        [SerializeField] private string playerTag = "Player";

        [Tooltip("깨울 보스 오브젝트. 씬에서 비활성(체크 해제) 상태로 두면 된다.")]
        [SerializeField] private GameObject boss;

        [Tooltip("켜두면 Awake에서 보스를 강제로 꺼둔다. 씬에서 실수로 활성화해둬도 안전하게 동작.")]
        [SerializeField] private bool forceDeactivateOnAwake = true;

        [Tooltip("한 번만 발동. 끄면 영역에 들어올 때마다 보스를 다시 켠다.")]
        [SerializeField] private bool triggerOnce = true;

        [Tooltip("트리거 진입 후 보스가 등장하기까지의 지연(초). 낙하 착지 모션을 보여주고 싶으면 0.2~0.5 정도.")]
        [SerializeField] private float activateDelay = 0.25f;

        [Header("리트라이")]
        [Tooltip("플레이어가 죽고 체크포인트에서 부활하면 보스를 초기 상태로 되돌리고 트리거를 다시 무장한다. " +
                 "끄면 깎인 체력 그대로 남아 있는다.")]
        [SerializeField] private bool resetBossOnRespawn = true;

        [Header("기즈모")]
        [SerializeField] private Color gizmoColor = new Color(1f, 0.3f, 0.2f, 0.25f);

        private bool consumed;
        private PlayerHealth boundHealth;

        private void Awake()
        {
            GetComponent<BoxCollider2D>().isTrigger = true; // 플레이어를 밀지 않고 감지만
            if (forceDeactivateOnAwake && boss != null) boss.SetActive(false);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryActivate(other);
        }

        // 부활 지점이 이미 이 영역 안에 있으면(아레나 안에 세이브포인트를 찍은 뒤 사망 등)
        // Enter가 다시 발생하지 않으므로 Stay로도 재무장 여부를 본다.
        private void OnTriggerStay2D(Collider2D other)
        {
            if (!consumed) TryActivate(other);
        }

        private void TryActivate(Collider2D other)
        {
            if (triggerOnce && consumed) return;
            if (!IsPlayer(other)) return;

            BindRespawnHook(other);

            if (boss == null)
            {
                Debug.LogWarning("[BossEncounterTrigger] boss 미배선 — 인스펙터에서 보스 오브젝트를 연결하세요.", this);
                return;
            }

            consumed = true;
            if (activateDelay > 0f) StartCoroutine(ActivateAfterDelay());
            else boss.SetActive(true);
        }

        // 플레이어는 이전 씬에서 넘어오므로 Awake 시점에 잡지 않고, 처음 조우할 때 붙인다.
        private void BindRespawnHook(Collider2D other)
        {
            if (!resetBossOnRespawn) return;
            PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
            if (health == null || health == boundHealth) return;
            if (boundHealth != null) boundHealth.OnPlayerRespawned -= HandleRespawn;
            boundHealth = health;
            boundHealth.OnPlayerRespawned += HandleRespawn;
        }

        // 리트라이: 보스를 끄고 초기 상태로 되돌린 뒤 트리거를 다시 무장시킨다.
        // 아레나로 다시 내려오면 등장 인트로부터 새로 시작된다.
        private void HandleRespawn()
        {
            if (!resetBossOnRespawn || boss == null) return;

            boss.SetActive(false); // OnDisable에서 연출·조작잠금 부작용까지 정리된다
            DemonBoss demon = boss.GetComponent<DemonBoss>();
            if (demon != null) demon.ResetBoss();
            consumed = false;
        }

        private void OnDestroy()
        {
            if (boundHealth != null) boundHealth.OnPlayerRespawned -= HandleRespawn;
        }

        // 태그가 비어있거나 다르게 설정된 씬에서도 동작하도록 PlayerHealth 보유 여부를 함께 본다.
        private bool IsPlayer(Collider2D other)
        {
            if (!string.IsNullOrEmpty(playerTag) && other.CompareTag(playerTag)) return true;
            return other.GetComponentInParent<PlayerHealth>() != null;
        }

        private System.Collections.IEnumerator ActivateAfterDelay()
        {
            yield return new WaitForSeconds(activateDelay);
            if (boss != null) boss.SetActive(true);
        }

        private void OnDrawGizmos()
        {
            BoxCollider2D col = GetComponent<BoxCollider2D>();
            if (col == null) return;
            Gizmos.color = gizmoColor;
            Vector3 center = transform.TransformPoint(col.offset);
            Vector3 size = new Vector3(col.size.x * transform.lossyScale.x, col.size.y * transform.lossyScale.y, 0.1f);
            Gizmos.DrawCube(center, size);
            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 1f);
            Gizmos.DrawWireCube(center, size);
        }
    }
}
