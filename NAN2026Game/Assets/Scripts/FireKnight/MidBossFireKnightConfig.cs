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
        public float frontDeadZone = 1.0f; // FrontOnly 판정 시, 이 거리 안이면 등 뒤라도 정면 처리(DemonBoss와 동일 개념)

        [Header("Normal Attack — 판정 창은 프레임 구간(포함)")]
        public float normalWindup = 0.25f;
        public int normalWinStart = 4;   // normalF 프레임 인덱스 기준
        public int normalWinEnd = 6;
        public int normalDamage = 1;
        public float normalCooldown = 1.4f;
        public float normalHitReach = 2.2f;  // DemonBoss 방식: 거리 기반 판정
        public bool normalFrontOnly = false; // true면 보스가 바라보는 방향만 타격 판정

        [Header("Fire Attack (근접 — 검에 불 붙여 내려찍기) — 판정 창은 프레임 구간")]
        public float fireWindup = 0.3f;
        public int fireWinStart = 5;   // fireF 프레임 인덱스 기준
        public int fireWinEnd = 8;
        public int fireDamage = 1;
        public float fireCooldown = 2.0f;
        public float fireHitReach = 2.5f;
        public bool fireFrontOnly = false;

        [Header("Fire Bomb (근접 — 아래에서 위로 쳐올리며 폭발 이펙트) — 판정 창은 프레임 구간")]
        public float bombWindup = 0.35f;
        public int bombWinStart = 4;   // bombF 프레임 인덱스 기준
        public int bombWinEnd = 6;
        public int bombDamage = 1;
        public float bombCooldown = 2.2f;
        public float bombHitReach = 2.0f;
        public bool bombFrontOnly = false;

        [Header("Wheel Attack (2연속 판정) — 판정 창 2개, 각각 프레임 구간")]
        public float wheelWindup = 0.3f;
        public int wheelWin1Start = 3;   // wheelF 프레임 인덱스 기준
        public int wheelWin1End = 4;
        public int wheelWin2Start = 7;
        public int wheelWin2End = 8;
        public int wheelDamagePerTick = 1;
        public float wheelCooldown = 2.5f;
        public float wheelHitReach = 2.8f;
        public bool wheelFrontOnly = false;

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
        public float groggyPipsOffsetY = -0.4f; // 패링 핍(◆◇) 표시 Y 위치
        public float groggyExitCooldown = 1.6f; // 그로기 끝나거나 그로기 진입 시 4개 공격 쿨타임 일괄 리셋에 쓰는 값

        [Header("그로기 버스트")]
        public float burstAtkSpeedMul = 2f;
        public float burstDashSpeed = 20f;
        public float burstDashStopX = 1.7f;
        public float sparkleInterval = 0.22f;

        [Header("클래시")]
        public SpikeBallConfig clashConfig;

        [Header("디버그 표시 (제출 전 OFF)")]
        public bool showRangesInGame = true;  // 게임 뷰에 공격 범위 띠 표시
        public bool showRangeLabels = true;   // 거리·상태 텍스트 라벨
        public float rangeBandHeight = 3f;    // 표시용 띠 높이 (판정과 무관, 보기용)
        public float rangeBandYOffset = -2.5f; // 캔버스 pivot이 캐릭터 몸통보다 위에 있어서 보정용(보기용, 판정과 무관)

    }
}