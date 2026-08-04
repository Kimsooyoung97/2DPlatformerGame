using UnityEngine;

/// <summary>
/// Attach to each background tile. Moves the tile horizontally relative to the
/// camera at a fraction of the camera's speed (parallaxEffect), and silently
/// wraps the tile back into the loop once it scrolls out of view so a small
/// set of tiles can cover an arbitrarily long side-scrolling level.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class ParallaxLayer : MonoBehaviour
{
    [Range(0f, 1f)]
    [Tooltip("0 = moves with camera (infinitely far away), 1 = moves 1:1 with the world (foreground).")]
    public float parallaxEffect = 0.5f;

    [Tooltip("타일이 화면 밖으로 나가면 반대쪽으로 순간이동해 무한 반복하는 기능. " +
        "고르게 반복 배치된 진짜 타일에는 켜두고, 한 곳에 뭉쳐 배치된(반복용이 아닌) " +
        "배경 조각에는 꺼서(false) 그냥 패럴랙스만 적용되게 한다 — 켠 채로 두면 " +
        "카메라가 타일 범위를 벗어나는 순간 눈에 띄게 순간이동해 보인다.")]
    public bool infiniteWrap = true;

    private Transform cam;
    private float startPosX;
    private float tileWidth;

    void Start()
    {
        if (Camera.main != null)
        {
            cam = Camera.main.transform;
        }
        startPosX = transform.position.x;
        tileWidth = GetComponent<SpriteRenderer>().bounds.size.x;
    }

    void LateUpdate()
    {
        if (cam == null) return;

        float distMoved = cam.position.x * (1f - parallaxEffect);
        float distToMove = cam.position.x * parallaxEffect;

        transform.position = new Vector3(startPosX + distToMove, transform.position.y, transform.position.z);

        if (!infiniteWrap) return;

        if (distMoved > startPosX + tileWidth)
        {
            startPosX += tileWidth;
        }
        else if (distMoved < startPosX - tileWidth)
        {
            startPosX -= tileWidth;
        }
    }
}
