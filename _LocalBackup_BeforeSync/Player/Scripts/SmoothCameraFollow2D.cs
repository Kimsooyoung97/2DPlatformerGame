using UnityEngine;

namespace NHNDemo
{
    public sealed class SmoothCameraFollow2D : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float smoothTime = 0.18f;
        [SerializeField] private Vector2 offset = new Vector2(1.5f, 1f);

        private Vector3 velocity;
        private float rotationVelocity;

        public void SetTarget(Transform value)
        {
            target = value;
        }

        private void LateUpdate()
        {
            if (target == null)
                return;

            Vector3 rotatedOffset = target.TransformVector(offset);
            Vector3 desired = new Vector3(
                target.position.x + rotatedOffset.x,
                target.position.y + rotatedOffset.y,
                transform.position.z);
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);

            float angle = Mathf.SmoothDampAngle(
                transform.eulerAngles.z,
                target.eulerAngles.z,
                ref rotationVelocity,
                0.38f);
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
}
