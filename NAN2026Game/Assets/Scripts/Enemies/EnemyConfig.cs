using UnityEngine;

namespace NAN2026
{
    [CreateAssetMenu(fileName = "EnemyConfig", menuName = "NAN2026/EnemyConfig")]
    public class EnemyConfig : ScriptableObject
    {
        [Header("생존")]
        public int hitsToDie = 5;          // 플레이어 평타 5번
        public float hurtLock = 0.35f;     // 피격 경직
        public float deathLinger = 1.2f;   // 사망 애니 후 잔류 시간

        [Header("탐지·이동")]
        public float aggroRange = 12f;
        public float attackRange = 1.8f;
        public float walkSpeed = 1.6f;
        public float attackCooldown = 1.6f;

        [Header("공격")]
        public float attackDur = 0.75f;
        public float hitWinS = 0.45f;      // 타격 시간창 시작(진행률)
        public float hitWinE = 0.65f;
        public int damage = 1;
        public float frontDeadZone = 0.5f;

        [Header("애니메이션")]
        public float fps = 12f;

        [Header("접지")]
        public bool snapToGround = true;
        public float groundY = 0f;         // 피벗이 발끝이므로 이 값이 곧 발 위치

        [Header("원거리 전용")]
        public float fireFrac = 0.6f;      // 이 진행률에 화살 발사
        public Vector2 muzzleOffset = new Vector2(0.5f, 0.9f); // 바라보는 쪽 +x
        public float arrowSpeed = 9f;
        public float arrowLife = 4f;
        public int arrowDamage = 1;

        [Header("피격 연출")]
        public float hitFlash = 0.1f;
    }
}
