namespace NAN2026.Core
{
    /// 순수 로직: UnityEngine 비의존. EditMode 테스트 대상.
    /// 보스 좌우 바라보기 · 접지 높이 · 손(발사구) 위치 계산.
    /// spriteFacesLeft = 원본(비반전) 스프라이트가 왼쪽을 향하는가.
    public static class BossFacingLogic
    {
        /// 플레이어를 바라보기 위해 SpriteRenderer.flipX 를 켜야 하는가.
        public static bool ShouldFlipX(float bossX, float playerX, bool spriteFacesLeft)
        {
            bool playerIsRight = playerX >= bossX;
            return spriteFacesLeft ? playerIsRight : !playerIsRight;
        }

        /// 현재 flipX 상태에서 보스가 실제로 향하는 월드 방향 (+1 오른쪽 / -1 왼쪽).
        public static float FacingSign(bool flipX, bool spriteFacesLeft)
        {
            float baseSign = spriteFacesLeft ? -1f : 1f;
            return flipX ? -baseSign : baseSign;
        }

        /// 대상이 보스가 바라보는 쪽에 있는가 (등 뒤 타격 방지).
        /// deadZone 안쪽(거의 겹친 상태)은 항상 정면으로 본다.
        public static bool TargetInFront(float bossX, float targetX, float facingSign, float deadZone)
        {
            float d = targetX - bossX;
            float ad = d < 0f ? -d : d;
            if (ad <= deadZone) return true;
            return d * facingSign > 0f;
        }

        /// 발이 지면에 닿도록 하는 피벗(중앙) 월드 Y. feetOffset = 피벗→발끝 거리(양수).
        public static float GroundedPivotY(float groundSurfaceY, float feetOffset)
        {
            return groundSurfaceY + feetOffset;
        }

        /// 손(투사체 발사구)의 월드 X. handOffsetX 는 '바라보는 쪽'으로의 거리(양수).
        public static float HandWorldX(float bossX, float handOffsetX, float facingSign)
        {
            return bossX + handOffsetX * facingSign;
        }

        /// 손의 월드 Y. handOffsetY 는 피벗 기준 상대값(음수 가능).
        public static float HandWorldY(float bossPivotY, float handOffsetY)
        {
            return bossPivotY + handOffsetY;
        }
    }
}
