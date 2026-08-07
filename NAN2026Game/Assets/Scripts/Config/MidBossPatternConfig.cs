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
        [Header("애니메이션 클립 실제 길이(초) — 이 시간이 끝날 때까지는 방향을\n" +
            "바꾸지 않는다(busy 유지). MidBoss.controller의 각 클립 길이와 맞춰둘 것")]
        public float normalAttackAnimLength = 0.92f;
        public float fireAttackAnimLength = 1.5f;
        public float fireBombAnimLength = 0.75f;
        public float wheelAttackAnimLength = 1f;
        public float jumpAnimLength = 1.67f;
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

        [Header("FireAttack (쿨타임 4초, 검에 불 붙여 앞을 내려찍는 근접기)")]
        public float fireAttackWindup = 0.5f;
        public int fireAttackDamage = 2;
        public float fireAttackCooldown = 4f;
        public float fireAttackReach = 2.4f;

        [Header("FireBomb (쿨타임 8초, 검을 쳐올리며 앞에 폭발 이펙트가 나는 근접기)")]
        public float fireBombWindup = 0.7f;
        public int fireBombDamage = 4;
        public float fireBombCooldown = 8f;
        public float fireBombReach = 2.6f;

        [Header("WheelAttack (쿨타임 6초, 근접 2틱)")]
        public float wheelAttackWindup = 0.4f;
        public int wheelAttackDamagePerTick = 3;
        public float wheelAttackTickInterval = 0.35f;
        public float wheelAttackCooldown = 6f;
        public float wheelAttackReach = 2.4f;

        [Header("근접 히트박스 공통")]
        [Tooltip("판정용 콜라이더 오브젝트가 생성되어 있는 시간(초) — 그 동안 겹치면 맞는다")]
        public float meleeHitboxLifetime = 0.15f;
        [Tooltip("중간보스 공격 콜라이더의 가로, 세로")]
        public float midBossNormalAttackHitboxWidth = 2.5f;
        public float midBossNormalAttackHitboxHeight = 2.3f;
        public float midBossFireAttackHitboxWidth = 2.5f;
        public float midBossFireAttackHitboxHeight = 2.3f;
        public float midBossFireBombHitboxWidth = 5f;
        public float midBossFireBombHitboxHeight = 5f;
        public float midBossWheelAttackHitboxWidth = 3.3f;
        public float midBossWheelAttackHitboxHeight = 3.3f;

        //[Tooltip("Tmp")]
        //public float midBossNormalAttackHitboxHeight = 3f;

    }
}
