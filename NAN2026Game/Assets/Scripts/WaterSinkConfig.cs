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
        public float respawnDelay = 2.0f;  // 잠긴 뒤 리스폰까지(임시 자리표시, 0 이하=리스폰 없음)
    }
}
