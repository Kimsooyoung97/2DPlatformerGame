using UnityEngine;

namespace NAN2026
{
    [CreateAssetMenu(fileName = "DemonBossConfig", menuName = "NAN2026/DemonBossConfig")]
    public class DemonBossConfig : ScriptableObject
    {
        [Header("기본")]
        public int maxHp = 10;            // 10대 사망
        public float aggroX = 14f;
        public float walkSpeed = 2.2f;
        public float attackCooldown = 1.6f;
        public float fps = 12f;
        [Header("클리브(근접)")]
        public float cleaveDur = 1.25f;
        public float cleaveWinS = 0.62f;  // 패링 시간창 (다른 보스와 동일 감각)
        public float cleaveWinE = 0.82f;
        public float cleaveReach = 6.0f;
        public int damage = 1;
        [Header("스매시(접근 공격)")]
        public float smashDur = 1.5f;
        public float smashWinS = 0.62f;
        public float smashWinE = 0.85f;
        public float smashApproachSpeed = 9f;
        public float smashStopX = 3.2f;   // 주인공 옆 정지 거리
        public float smashReach = 6.5f;
        [Header("캐스트(투사체)")]
        public float castDur = 0.9f;
        public float castFireFrac = 0.55f; // 이 진행률에 발사
        public Vector2 handOffset = new Vector2(3.2f, 5.6f); // 손 위치(로컬)
        public float projSpeed = 9f;
        public float projLife = 6f;
        public int projDamage = 1;
        [Header("패링·그로기")]
        public float parryBuffer = 0.2f;
        public int groggyNeed = 5;
        public float groggyTime = 3.0f;
        public float groggyFxOffsetY = 11.2f;
        [Header("연출")]
        public float hitFlash = 0.12f;
        public SpikeBallConfig clashConfig; // 클래시 공유
        [Header("패턴 가중치")]
        public float castChance = 0.35f;   // 원거리 시 캐스트 확률
        public float smashChance = 0.4f;   // 중거리 시 스매시 확률
    }
}
