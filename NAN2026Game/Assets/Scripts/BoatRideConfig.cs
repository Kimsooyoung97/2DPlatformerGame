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
        [Tooltip("항해 중 점프를 막을지. 사용자 지시로 기본 꺼둔다 — 코드는 남기고 스위치만 끈 상태")]
        public bool lockJumpWhileSailing = false;
    }
}