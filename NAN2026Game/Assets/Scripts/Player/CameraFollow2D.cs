using UnityEngine;
using NAN2026.Core;

namespace NAN2026
{
    public class CameraFollow2D : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private CameraConfig config;

        private PlayerController2D targetController;
        private float vx;
        private float vy;
        private float lookAhead;
        private float lookAheadVel;
        private float lastTargetX;
        private float moveDir;
        private float anchorY;

        private void Start()
        {
            if (target != null)
            {
                targetController = target.GetComponent<PlayerController2D>();
                lastTargetX = target.position.x;
                anchorY = target.position.y;
                transform.position = new Vector3(target.position.x + config.offset.x, anchorY + config.offset.y, transform.position.z);
            }
        }

        private void LateUpdate()
        {
            if (target == null || config == null) return;

            float dx = target.position.x - lastTargetX;
            if (dx > 0.001f) moveDir = 1f;
            else if (dx < -0.001f) moveDir = -1f;
            lastTargetX = target.position.x;

            lookAhead = Mathf.SmoothDamp(lookAhead, moveDir * config.lookAheadX, ref lookAheadVel, config.lookAheadSmoothTime);

            float baseX = PlayerLocomotionLogic.CameraDeadzoneTargetX(transform.position.x - config.offset.x - lookAhead, target.position.x, config.deadzoneWidth);
            float goalX = baseX + config.offset.x + lookAhead;

            bool grounded = targetController == null || targetController.IsGrounded;
            if (grounded) anchorY = target.position.y;
            else if (target.position.y < anchorY - config.fallCatchDistance) anchorY = target.position.y + config.fallCatchDistance;
            float goalY = anchorY + config.offset.y;

            float nx = Mathf.SmoothDamp(transform.position.x, goalX, ref vx, config.horizontalSmoothTime);
            float ny = Mathf.SmoothDamp(transform.position.y, goalY, ref vy, config.verticalSmoothTime);
            transform.position = new Vector3(nx, ny, transform.position.z);
        }
    }
}