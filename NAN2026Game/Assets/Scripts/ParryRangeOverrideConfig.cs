using UnityEngine;

namespace NAN2026
{
    [CreateAssetMenu(fileName = "ParryRangeOverrideConfig", menuName = "NAN2026/ParryRangeOverrideConfig")]
    public class ParryRangeOverrideConfig : ScriptableObject
    {
        public float reachX = 3.0f; // 이 씬에서의 패링 인정 거리 (기본 1.5의 2배)
    }
}
