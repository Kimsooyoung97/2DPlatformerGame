using UnityEngine;

namespace NAN2026
{
    [CreateAssetMenu(fileName = "RopeClimbConfig", menuName = "NAN2026/RopeClimbConfig")]
    public class RopeClimbConfig : ScriptableObject
    {
        public float climbSpeed = 3.5f;
        public float exitJumpVelocity = 7f;
        public float snapLerp = 0.5f;
    }
}
