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

    [Tooltip("세로(Y축)로도 카메라를 따라 패럴랙스 이동할지 여부. 기본은 꺼짐(가로만) — " +
        "기존에 이 스크립트를 쓰던 다른 씬(FirstScene 등)의 동작을 그대로 유지하기 위한 기본값이다. " +
        "세로로도 따라오게 하려면 켠다. Y축은 무한 반복(랩어라운드)을 적용하지 않는다.")]
    public bool applyVerticalParallax = false;

    private Transform cam;
    private float startPosX;
    private float startPosY;
    private float tileWidth;

    void Start()
    {
        if (Camera.main != null)
        {
            cam = Camera.main.transform;
        }
        startPosX = transform.position.x;
        startPosY = transform.position.y;
        tileWidth = GetComponent<SpriteRenderer>().bounds.size.x;
    }

    void LateUpdate()
    {
        if (cam == null) return;

        float distMoved = cam.position.x * (1f - parallaxEffect);
        float distToMoveX = cam.position.x * parallaxEffect;
        float newY = applyVerticalParallax ? startPosY + cam.position.y * parallaxEffect : transform.position.y;

        transform.position = new Vector3(startPosX + distToMoveX, newY, transform.position.z);

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
