using UnityEngine;

/// <summary>
/// Simple side-scroll camera follow: tracks the target's X position (with optional
/// smoothing) while keeping the camera's own Y and Z fixed. Drives ParallaxLayer
/// scrolling indirectly since ParallaxLayer reads Camera.main's position.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float smoothTime = 0.15f;
    public float fixedY;
    public float minX = float.NegativeInfinity;
    public float maxX = float.PositiveInfinity;

    private Vector3 velocity = Vector3.zero;

    void LateUpdate()
    {
        if (target == null) return;

        float targetX = Mathf.Clamp(target.position.x, minX, maxX);
        Vector3 desired = new Vector3(targetX, fixedY, transform.position.z);
        transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
    }
}
