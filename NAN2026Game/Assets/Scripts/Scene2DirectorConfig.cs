using UnityEngine;

namespace NAN2026
{
    [CreateAssetMenu(fileName = "Scene2DirectorConfig", menuName = "NAN2026/Scene2DirectorConfig")]
    public class Scene2DirectorConfig : ScriptableObject
    {
        public int parryGoal = 5;          // 스파이크 패링 목표 횟수
        public float camHold = 0.9f;       // 보스 컷 유지 시간
        public float brightenTarget = 0.55f; // 목표 전역광
        public float brightenTime = 1.8f;  // 밝아지는 시간
        public float pipOffsetY = 4.0f;    // 핍 UI 높이(보스 위)
        [Header("테스트")]
        public bool debugSkipToBoss = false; // 켜면: 시작 즉시 밝음+스파이크 정지+보스 앞 텔레포트
        public float debugSpawnOffsetX = 8f;
    }
}
