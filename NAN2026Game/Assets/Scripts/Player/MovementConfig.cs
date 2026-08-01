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
        public int maxJumps = 2;
        public float onewayRiseThreshold = 0.05f;
        public float apexSpeedThreshold = 1.2f;
        public float landDuration = 0.36f;

        [Header("공격")]
        public float slashDuration = 0.4f;
        public float combo2Duration = 0.4f;
        public float combo3Duration = 0.55f;

        [Header("패링")]
        public float parryWindow = 0.18f;
        public float parryEndDuration = 0.22f;
        public Vector2 parryBoxSize = new Vector2(1.0f, 1.4f);
        public float parryBoxOffsetX = 0.6f;
        public float parryPerfectDistance = 0.25f;

        [Header("공격 전진(런지) 속도")]
        public float slashLungeSpeed = 1.5f;
        public float combo2LungeSpeed = 3.5f;
        public float combo3LungeSpeed = 0f;
    }
}