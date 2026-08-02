using UnityEngine;

namespace NAN2026
{
    // 전장의 안개 수치 단일 소유
    [CreateAssetMenu(fileName = "FogOfWarConfig", menuName = "NAN2026/FogOfWarConfig")]
    public class FogOfWarConfig : ScriptableObject
    {
        public float revealRadius;
        public float softEdge;
        [Range(0f, 1f)] public float fogAlpha;
        public Color fogColor;
        public float texelsPerUnit;
        public float moveThreshold;
        public int sortingOrder;
        public Vector2 boundsMin;
        public Vector2 boundsMax;

        [Header("시야 차폐 (A안)")]
        public LayerMask occlusionMask;   // 시야를 막는 지형 레이어
        public int rayCount;              // 각도 버킷 수 (레이 수)
        public float eyeHeight;           // 시점 높이 (발 기준 오프셋)
        public float occlusionTolerance;  // 차단면 자체가 보이는 관용 거리
    }
}
