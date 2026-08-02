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
    }
}
