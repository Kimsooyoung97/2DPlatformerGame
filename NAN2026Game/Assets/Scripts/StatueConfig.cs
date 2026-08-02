using UnityEngine;

namespace NAN2026
{
    [CreateAssetMenu(fileName = "StatueConfig", menuName = "NAN2026/StatueConfig")]
    public class StatueConfig : ScriptableObject
    {
        public float awakenRange;
        public float attackRange;
        public float moveSpeed;
        public int maxHp;
        public int damage;
        public float awakenDuration;
        public float idlePauseAfterAwaken;
        public float slamDuration;
        public float hitboxStart;
        public float hitboxEnd;
        public float attackCooldown;
        public float hitBlinkInterval;
        public int hitBlinkCount;
        public float edgeProbeAhead;
        public float edgeProbeDepth;
    }
}
