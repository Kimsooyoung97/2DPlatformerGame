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
        [Tooltip("이 높이차를 넘어가면(점프) 맞지 않는다. 0이면 세로 제한 없음")]
        public float attackHeight = 1.2f;

        [Tooltip("공격 모션 전용 fps. 0 이면 공용 fps 사용. 낮추면 휘두름만 느려진다")]
        public float attackFps = 0f;
        [Header("공격 예열 (= 플레이어 반응 시간)")]
        [Tooltip("이 시간 동안 제자리에서 색상 점멸로 경고한 뒤 공격에 들어간다")]
        public float attackWindup = 0.55f;
        public float windupFlashSpeed = 12f;
        public Color windupFlashColor = new Color(1f, 0.55f, 0.2f);

        [Header("순찰 범위")]
        [Tooltip("배치 지점에서 좌우로 이 거리까지만 움직인다. 0 이면 제한 없음(옛 동작)")]
        public float patrolRange = 6f;
        [Tooltip("단차를 찾을 때 훑는 간격")]
        public float patrolProbeStep = 0.5f;
        [Tooltip("이 높이차를 넘는 지면은 다른 단으로 보고 거기서 순찰을 끊는다")]
        public float patrolLevelTolerance = 0.6f;

        [Tooltip("화살이 이 거리 안까지 접근하면 닿기 전에도 매 프레임 패링을 접수한다")]
        public float arrowParryZone = 1.5f;
        [Tooltip("화살이 플레이어 발끝 기준 이 높이 안을 지날 때만 패링 대상. 점프로 넘긴 화살 제외")]
        public float arrowParryHeight = 2.0f;

        [Header("패링 보상")]
        [Tooltip("패링 성공 시 무방비로 굳는 시간. 플레이어 3연타(0.40+0.40+0.55=1.35초)가 다 들어가야 보상이 된다")]
        public float groggyDuration = 1.6f;
        public float groggyFlashSpeed = 6f;
        public Color groggyFlashColor = new Color(1f, 0.9f, 0.3f);
        [Tooltip("패링한 화살을 되돌려 쏜 적에게 맞춘다")]
        public bool reflectOnParry = true;
        public float reflectSpeedMul = 1.4f;
        [Tooltip("반사 후 최소 수명. 짧으면 되돌아가는 도중 사라진다")]
        public float reflectMinLife = 1.5f;

        [Header("패링")]
        [Tooltip("패링 성공 시 격돌 연출. 보스·함정과 같은 자산을 쓴다")]
        public SpikeBallConfig clashConfig;

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

        [Header("보상")]
        [Tooltip("이 적을 처치했을 때 플레이어에게 주는 경험치")]
        public int xpReward = 8;
    }
}
