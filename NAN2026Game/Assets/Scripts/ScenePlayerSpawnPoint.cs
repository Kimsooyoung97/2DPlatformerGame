using UnityEngine;

namespace NAN2026
{
    // 씬에 배치하는 빈 마커. 포탈을 타고 이 씬으로 들어왔을 때 플레이어가 시작할 위치.
    // 씬당 보통 하나만 두면 된다(여러 개 있으면 씬에서 먼저 찾히는 것 하나만 쓰임 —
    // 포탈마다 다른 도착 지점이 필요해지면 그때 id 필드를 추가해 확장한다).
    public sealed class ScenePlayerSpawnPoint : MonoBehaviour
    {
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 0.4f);
            Gizmos.DrawLine(transform.position + Vector3.left * 0.4f, transform.position + Vector3.right * 0.4f);
            Gizmos.DrawLine(transform.position + Vector3.down * 0.4f, transform.position + Vector3.up * 0.4f);
        }
    }
}
