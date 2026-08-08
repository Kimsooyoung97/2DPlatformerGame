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

        [Header("군집 제어")]
        public float stopDistance = 1.4f;    // 이 안쪽으로는 파고들지 않는다
        public float separation = 1.0f;      // 진행 방향 앞 동료와의 최소 간격
        public float fireStagger = 0.8f;     // 최초 공격 준비까지 랜덤 지연 상한
        public float cooldownJitter = 0.6f;  // 쿨다운 ± 편차(동기화 방지)
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

        [Header("범위 표시 (제출 전 OFF)")]
        public bool showRangesInGame = true;   // 게임 뷰에 사거리 띠 표시
        public bool showRangeLabels = true;    // 머리 위 숫자 라벨
        public float rangeBandHeight = 1.8f;   // 표시용 띠 높이(판정과 무관, 보기용)
        [Header("피격 연출")]
        public float hitFlash = 0.1f;
    }
}
