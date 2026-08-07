using UnityEngine;

namespace NAN2026
{
    /// <summary>
    /// MidBoss 신규 4패턴(NormalAttack/FireAttack/FireBomb/WheelAttack) + 추격/점프/체력 설정.
    /// 기존 MidBossConfig(단일 SpAtk 패턴, MidBossAI용)와는 별개 — MidBossAI는 손대지 않는다.
    /// </summary>
    [CreateAssetMenu(fileName = "MidBossPatternConfig", menuName = "NAN2026/MidBoss Pattern Config")]
    public sealed class MidBossPatternConfig : ScriptableObject
    {
        [Header("공통")]
        public int maxHealth = 30;
        public float aggroRange = 8f;
        public float attackRange = 2.2f;
        public float chaseSpeed = 2.2f;
        [Tooltip("플레이어가 이 높이 이상 위에 있으면 점프로 따라간다")]
        public float jumpYThreshold = 1.2f;
        public float jumpVelocity = 8f;
        [Tooltip("패턴 사이 최소 대기시간(같은 패턴이 연속으로 안 나오게)")]
        public float minPatternGap = 0.6f;

        [Header("NormalAttack (쿨타임 없음, 근접)")]
        public float normalAttackWindup = 0.35f;
        public int normalAttackDamage = 1;
        public float normalAttackReach = 2.2f;

        [Header("FireAttack (쿨타임 4초, 원거리 구체 1개)")]
        public float fireAttackWindup = 0.5f;
        public int fireAttackDamage = 2;
        public float fireAttackCooldown = 4f;
        public float fireAttackOrbSpeed = 7f;
        public float fireAttackSpawnHeight = 1f;

        [Header("FireBomb (쿨타임 8초, 원거리 강력한 한 방)")]
        public float fireBombWindup = 0.7f;
        public int fireBombDamage = 4;
        public float fireBombCooldown = 8f;
        public float fireBombOrbSpeed = 5f;
        public float fireBombSpawnHeight = 1f;

        [Header("WheelAttack (쿨타임 6초, 근접 2틱)")]
        public float wheelAttackWindup = 0.4f;
        public int wheelAttackDamagePerTick = 3;
        public float wheelAttackTickInterval = 0.35f;
        public float wheelAttackCooldown = 6f;
        public float wheelAttackReach = 2.4f;
    }
}
