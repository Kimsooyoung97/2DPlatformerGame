using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// WarpPoint/WarpPortal에 부착. 플레이어가 닿으면 WarpZone 위치로 이동시키고,
/// CM_PlayerCamera의 카메라 경계(BoundingShape2D)를 새 구역용으로 교체한다.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public sealed class WarpPortalController : MonoBehaviour
{
    [SerializeField] private Transform warpZone;
    [SerializeField] private CinemachineConfiner2D confiner;
    [SerializeField] private Collider2D newCameraBounds;

    private void Awake()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (warpZone == null) return;

        Rigidbody2D rb = other.attachedRigidbody;
        other.transform.position = warpZone.position;
        if (rb != null) rb.linearVelocity = Vector2.zero;
        Physics2D.SyncTransforms();

        if (confiner != null && newCameraBounds != null)
        {
            confiner.BoundingShape2D = newCameraBounds;
            confiner.InvalidateBoundingShapeCache();
        }
    }
}
