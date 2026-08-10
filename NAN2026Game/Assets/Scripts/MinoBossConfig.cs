using UnityEngine;

namespace NAN2026
{
    [CreateAssetMenu(fileName = "MinoBossConfig", menuName = "NAN2026/MinoBossConfig")]
    public class MinoBossConfig : ScriptableObject
    {
        [Header("전투")]
        public int maxHp = 30;
        public float aggroX = 9f;
        public float attackRange = 2.6f;
        public float hitReach = 3.4f;      // 공격 명중 인정 거리
        public float walkSpeed = 2.2f;
        public int damage = 1;
        public float attackDuration = 1.1f;
        public float hitFracStart = 0.42f;  // atk_1 창 (홀드 풀린 직후)
        public float hitFracEnd = 0.62f;
        [Header("atk_1 프레임 판정창 (이단 베기)")]
        public int atk1Win1Start = 5;
        public int atk1Win1End = 8;
        public int atk1Win2Start = 11;
        public int atk1Win2End = 14;
        public float hit2FracStart = 0.62f; // atk_2 창 (MidBoss 감각)
        public float hit2FracEnd = 0.82f;
        public float attackCooldown = 1.6f;
        [Header("atk_1 예고 홀드")]
        public int atk1HoldFrame = 3;      // 이 프레임에서 멈춤 (치켜든 자세)
        public float atk1HoldTime = 0.55f; // 멈추는 시간
        [Header("프레임 속도")]
        public float fpsIdle = 10f;
        public float fpsWalk = 12f;
        public float fpsAtk = 14f;
        public float fpsHit = 14f;
        public float fpsDeath = 12f;
        public float parryBuffer = 0.2f; // 선입력 버퍼(일찍 눌러도 이 시간 내 유효)
        [Header("디버그")]
        public bool showParryDebug = true; // 패링 판정 텔레메트리 (제출 전 OFF)
        [Header("그로기")]
        public int groggyNeed = 5;        // 패링 몇 회에 그로기
        public float groggyTime = 3.0f;   // 무방비 시간
        public float groggyFxOffsetY = 3.4f;
        [Header("그로기 버스트")]
        public float burstAtkSpeedMul = 2f;   // 공속 배율
        public float burstDashSpeed = 20f;    // 자동 대시 속도
        public float burstDashStopX = 1.7f;   // 보스 앞 정지 거리
        public float sparkleInterval = 0.22f; // 반짝 주기
        [Header("공격 예열(Windup) — 플레이어 반응 시간 확보용 텔레그래프")]
        public float atk1Windup = 0.25f;
        public float atk2Windup = 0.3f;
        public float dashWindup = 0.35f;
        public Color windupFlashColor = new Color(1f, 0.35f, 0.35f); // 예열 중 깜빡이는 경고 색
        public float windupFlashSpeed = 12f; // 값이 클수록 플래시가 빠르게 깜빡임
        [Header("돌진 공격(Dash)")]
        public float dashSpeed = 9f;       // walkSpeed보다 확실히 빠르게
        public float dashOvershoot = 2.5f; // 플레이어를 지나쳐서 멈추는 거리
        public float dashHitReach = 2.5f;  // 돌진 중 명중 판정 거리
        [Header("체력바")]
        public float barScale = 1.8f;      // 견본(sample_100) 크기감
        public float barOffsetY = 3.1f;
        public Sprite barUnder;
        public Sprite barProgress;
        public Sprite barOver;
        [Header("보상")]
        [Tooltip("이 보스를 처치했을 때 플레이어에게 주는 경험치")]
        public int xpReward = 55;

        [Header("클래시")]
        public SpikeBallConfig clashConfig;
        [Header("사운드")]
        public AudioClip atk1Clip;
        public AudioClip atk2Clip;
        public AudioClip dashClip;
        public AudioClip[] hitClips = new AudioClip[2];
        public AudioClip deathClip;
        [Range(0f, 1f)] public float attackVolume = 0.85f;
        [Range(0f, 1f)] public float hitVolume = 0.8f;
        [Range(0f, 1f)] public float deathVolume = 0.9f;

        /// <summary>배열에서 null 아닌 클립 중 하나를 균등 랜덤 선택. 비어있으면 null.</summary>
        public AudioClip RandomClip(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0) return null;
            int start = Random.Range(0, clips.Length);
            for (int i = 0; i < clips.Length; i++)
            {
                var c = clips[(start + i) % clips.Length];
                if (c != null) return c;
            }
            return null;
        }

    }
}