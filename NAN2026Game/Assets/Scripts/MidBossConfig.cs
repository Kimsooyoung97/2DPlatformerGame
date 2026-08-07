using UnityEngine;

namespace NAN2026
{
    [CreateAssetMenu(fileName = "MidBossConfig", menuName = "NAN2026/MidBossConfig")]
    public class MidBossConfig : ScriptableObject
    {
        [Header("행동")]
        public float aggroRange = 8f;      // 감지 거리
        public float attackRange = 1.8f;   // 공격 개시 거리
        public float walkSpeed = 2.2f;
        [Header("공격 sp_atk")]
        public float attackDuration = 1.5f;
        [Range(0f,1f)] public float hitFrac = 0.5f;    // 타격 구간 시작(진행률)
        [Range(0f,1f)] public float hitFracEnd = 0.72f; // 타격 구간 끝 — 이 사이 접촉이면 패링 가능
        public float hitReach = 2.2f;      // 타격 순간 이 거리 안이면 명중
        public int damage = 1;
        public float attackCooldown = 1.2f;
        [Header("디버그")]
        public bool showRangesInGame = false; // 게임 뷰에 범위 링 표시 (제출 전 끄기)
        [Header("패링 연동")]
        public SpikeBallConfig clashConfig; // 성공 시 격돌 FX·사운드 재사용
    }
}
