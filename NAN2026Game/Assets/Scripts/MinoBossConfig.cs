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
        public float hitFracStart = 0.5f;  // 애니 중 타격 유효창
        public float hitFracEnd = 0.75f;
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
        [Header("디버그")]
        public bool showParryDebug = true; // 패링 판정 텔레메트리 (제출 전 OFF)
        [Header("그로기")]
        public int groggyNeed = 5;        // 패링 몇 회에 그로기
        public float groggyTime = 3.0f;   // 무방비 시간
        public float groggyFxOffsetY = 3.4f;
        [Header("체력바")]
        public float barScale = 1.8f;      // 견본(sample_100) 크기감
        public float barOffsetY = 3.1f;
        public Sprite barUnder;
        public Sprite barProgress;
        public Sprite barOver;
        [Header("클래시")]
        public SpikeBallConfig clashConfig;
    }
}
