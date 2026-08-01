using UnityEngine;

namespace NHNDemo
{
    public sealed class SurfaceMonsterPatrol : MonoBehaviour
    {
        [SerializeField] private Vector2 surfaceUp = Vector2.up;
        [SerializeField] private float patrolDistance = 1.4f;
        [SerializeField] private float patrolSpeed = 0.8f;
        [SerializeField] private string movementState = "Run";

        private Vector3 origin;
        private SpriteRenderer spriteRenderer;
        private Animator animator;
        private float previousOffset;

        public void Configure(
            Vector2 fixedSurfaceUp,
            float distance,
            float speed,
            string animationState)
        {
            surfaceUp = fixedSurfaceUp.normalized;
            patrolDistance = distance;
            patrolSpeed = speed;
            movementState = animationState;
            ApplySurfaceRotation();
        }

        private void Awake()
        {
            origin = transform.position;
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            animator = GetComponentInChildren<Animator>();
            ApplySurfaceRotation();

            if (animator != null &&
                animator.HasState(0, Animator.StringToHash(movementState)))
            {
                animator.Play(movementState, 0, Random.value);
            }
        }

        private void Update()
        {
            Vector2 right = new Vector2(surfaceUp.y, -surfaceUp.x);
            float offset = Mathf.PingPong(Time.time * patrolSpeed, patrolDistance * 2f) - patrolDistance;
            transform.position = origin + (Vector3)(right * offset);

            if (spriteRenderer != null)
                spriteRenderer.flipX = offset < previousOffset;

            previousOffset = offset;
        }

        private void ApplySurfaceRotation()
        {
            float angle = Mathf.Atan2(surfaceUp.y, surfaceUp.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
}
