using UnityEngine;

namespace NAN2026
{
    [CreateAssetMenu(fileName = "MidBossConfig", menuName = "NAN2026/MidBossConfig")]
    public class MidBossConfig : ScriptableObject
    {
        [Header("행동")]
        public float aggroRange = 8f;      // 감지 거리
        public float attackRange = 1.8f;   // 공격 개시 거리
        public float walkSpeed = 2.2f;
        [Header("공격 sp_atk")]
        public float attackDuration = 1.5f;
        [Range(0f,1f)] public float hitFrac = 0.55f; // 타격 판정 순간(진행률)
        public float hitReach = 2.2f;      // 타격 순간 이 거리 안이면 명중
        public int damage = 1;
        public float attackCooldown = 1.2f;
        [Header("패링 연동")]
        public SpikeBallConfig clashConfig; // 성공 시 격돌 FX·사운드 재사용
    }
}
