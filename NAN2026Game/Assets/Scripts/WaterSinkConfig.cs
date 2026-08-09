using UnityEngine;

namespace NAN2026
{
    [CreateAssetMenu(fileName = "WaterSinkConfig", menuName = "NAN2026/WaterSinkConfig")]
    public class WaterSinkConfig : ScriptableObject
    {
        public float peekHeight = 0.35f;   // 수면 위로 내미는 높이
        public float peekTime = 0.35f;     // 내민 채 버티는 시간
        public float sinkDepth = 2.0f;     // 수면 아래로 가라앉는 깊이
        public float sinkTime = 1.4f;      // 가라앉는 데 걸리는 시간
        public float respawnDelay = 0.4f;  // 완전히 잠긴 뒤 사망 판정까지의 사이
        public bool useDeathFlow = true;   // 켜면 PlayerHealth.Kill() 로 기존 사망·체크포인트 흐름에 합류
    }
}