using UnityEngine;

namespace NAN2026
{
    [CreateAssetMenu(fileName = "MovementConfig", menuName = "Game/MovementConfig")]
    public class MovementConfig : ScriptableObject
    {
        [Header("이동")]
        public float walkSpeed = 2.2f;
        public float runSpeed = 4.2f;

        [Header("점프")]
        public float jumpVelocity = 8f;
        public float gravityScale = 2.5f;
        public float groundCheckDistance = 0.08f;

        [Header("공격")]
        public float slashDuration = 0.4f;
        public float combo2Duration = 0.4f;
        public float combo3Duration = 0.55f;
    }
}