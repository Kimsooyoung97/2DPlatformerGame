using UnityEngine;

namespace NAN2026
{
    [CreateAssetMenu(fileName = "CameraConfig", menuName = "Game/CameraConfig")]
    public class CameraConfig : ScriptableObject
    {
        [Header("수평 (마리오식 데드존)")]
        public float deadzoneWidth = 1.2f;
        public float horizontalSmoothTime = 0.12f;
        public float lookAheadX = 1.0f;
        public float lookAheadSmoothTime = 0.4f;

        [Header("수직 (착지 기준)")]
        public float verticalSmoothTime = 0.3f;
        public float fallCatchDistance = 2.5f;

        [Header("공통")]
        public Vector2 offset = new Vector2(0f, 1f);
    }
}