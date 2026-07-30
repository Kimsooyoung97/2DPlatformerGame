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
