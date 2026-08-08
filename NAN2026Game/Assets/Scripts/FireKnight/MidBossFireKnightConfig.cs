using UnityEngine;

namespace NAN2026
{
    [CreateAssetMenu(fileName = "MidBossFireKnightConfig", menuName = "NAN2026/MidBossFireKnightConfig")]
    public class MidBossFireKnightConfig : ScriptableObject
    {
        [Header("행동")]
        public int maxHp = 20;
        public float aggroRange = 8f;
        public float attackRange = 1.8f;
        public float walkSpeed = 2.2f;

        [Header("Normal Attack")]
        public float normalWindup = 0.25f;
        public float normalHitDelay = 0.3f;   // windup 종료 후, 실제 히트박스가 켜지기까지 대기(애니메이션 진행 시간)
        public int normalDamage = 1;
        public float normalCooldown = 1.4f;
        public float normalHitboxLifetime = 0.15f;

        [Header("Fire Attack (근접 — 검에 불 붙여 내려찍기)")]
        public float fireWindup = 0.3f;
        public float fireHitDelay = 0.35f;
        public int fireDamage = 1;
        public float fireCooldown = 2.0f;
        public float fireHitboxLifetime = 0.15f;

        [Header("Fire Bomb (근접 — 아래에서 위로 쳐올리며 폭발 이펙트)")]
        public float bombWindup = 0.35f;
        public float bombHitDelay = 0.4f;
        public int bombDamage = 1;
        public float bombCooldown = 2.2f;
        public float bombHitboxLifetime = 0.15f;

        [Header("Wheel Attack (2연속 판정)")]
        public float wheelWindup = 0.3f;
        public float wheelHitDelay = 0.3f;
        public float wheelTickInterval = 0.25f;
        public int wheelDamagePerTick = 1;
        public float wheelCooldown = 2.5f;
        public float wheelHitboxLifetime = 0.15f;

        [Header("프레임 속도")]
        public float fpsIdle = 10f;
        public float fpsWalk = 12f;
        public float fpsNormal = 12f;
        public float fpsFire = 12f;
        public float fpsBomb = 12f;
        public float fpsWheel = 12f;
        public float fpsHit = 14f;
        public float fpsDeath = 12f;

        [Header("공격 예열(Windup) — 플레이어 반응 시간 확보용 텔레그래프")]
        public Color windupFlashColor = new Color(1f, 0.35f, 0.35f);
        public float windupFlashSpeed = 12f;

        [Header("패링")]
        public float parryBuffer = 0.2f;

        [Header("그로기")]
        public int groggyNeed = 5;
        public float groggyTime = 3.0f;
        public float groggyFxOffsetY = 3.4f;
        public float groggyExitCooldown = 1.6f; // 그로기 끝나거나 그로기 진입 시 4개 공격 쿨타임 일괄 리셋에 쓰는 값

        [Header("그로기 버스트")]
        public float burstAtkSpeedMul = 2f;
        public float burstDashSpeed = 20f;
        public float burstDashStopX = 1.7f;
        public float sparkleInterval = 0.22f;

        [Header("클래시")]
        public SpikeBallConfig clashConfig;
    }
}