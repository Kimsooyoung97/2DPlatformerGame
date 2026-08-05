using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// WarpPoint/WarpPortal에 부착. 플레이어가 닿으면 화면이 검게 페이드인 → 그 동안
/// WarpZone 위치로 이동 + 카메라 경계(BoundingShape2D) 교체 → 페이드아웃.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public sealed class WarpPortalController : MonoBehaviour
{
    [SerializeField] private Transform warpZone;
    [SerializeField] private CinemachineConfiner2D confiner;
    [SerializeField] private CinemachineCamera cmCamera;
    [SerializeField] private Collider2D newCameraBounds;
    [Tooltip("워프 시작 시 화면이 검게 덮이는 데 걸리는 시간(초)")]
    [SerializeField] private float fadeInDuration = 0.4f;
    [Tooltip("워프 완료 후 화면이 다시 밝아지는 데 걸리는 시간(초)")]
    [SerializeField] private float fadeOutDuration = 0.4f;

    private bool warping;

    private void Awake()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (warping) return;
        if (!other.CompareTag("Player")) return;
        if (warpZone == null) return;

        StartCoroutine(DoWarp(other));
    }

    private IEnumerator DoWarp(Collider2D player)
    {
        warping = true;

        yield return ScreenFader.Instance.FadeTo(1f, fadeInDuration);

        Rigidbody2D rb = player.attachedRigidbody;
        Vector3 oldPos = player.transform.position;
        player.transform.position = warpZone.position;
        if (rb != null) rb.linearVelocity = Vector2.zero;
        Physics2D.SyncTransforms();

        if (confiner != null && newCameraBounds != null)
        {
            confiner.BoundingShape2D = newCameraBounds;
            confiner.InvalidateBoundingShapeCache();
        }

        // 카메라가 댐핑 때문에 새 위치까지 서서히 따라오면, 화면이 밝아진 뒤에도
        // 카메라가 이동하는 게 보여 어색하다. Cinemachine에 '타깃이 순간이동했다'고
        // 알려서 카메라도 같은 프레임에 즉시 스냅하게 한다(검은 화면 상태에서 끝남).
        if (cmCamera != null) cmCamera.OnTargetObjectWarped(player.transform, warpZone.position - oldPos);

        yield return ScreenFader.Instance.FadeTo(0f, fadeOutDuration);

        warping = false;
    }
}
