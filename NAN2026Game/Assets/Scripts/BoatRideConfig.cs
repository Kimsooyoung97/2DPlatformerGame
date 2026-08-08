using UnityEngine;

namespace NAN2026
{
    [CreateAssetMenu(fileName = "BoatRideConfig", menuName = "NAN2026/BoatRideConfig")]
    public class BoatRideConfig : ScriptableObject
    {
        public float sailSpeed = 3.5f;      // 항해 속도
        public float deckHalfWidth = 1.2f;  // 갑판 절반 폭
        public float deckTopOffset = 0.95f; // 바닥 피벗 기준 갑판 높이
        public float riderGrace = 1.6f;     // 갑판 위 몇 유닛까지 탑승자로 인정
        public float edgeMargin = 1.0f;     // 물 끝에서 멈출 여유
    }
}
