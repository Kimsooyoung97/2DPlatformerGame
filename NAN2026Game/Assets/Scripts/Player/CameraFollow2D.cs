using UnityEngine;

namespace NAN2026
{
    public class CameraFollow2D : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private CameraConfig config;

        private Vector3 velocity;

        private void LateUpdate()
        {
            if (target == null || config == null) return;
            Vector3 goal = new Vector3(target.position.x + config.offset.x, target.position.y + config.offset.y, transform.position.z);
            transform.position = Vector3.SmoothDamp(transform.position, goal, ref velocity, config.smoothTime);
        }
    }
}