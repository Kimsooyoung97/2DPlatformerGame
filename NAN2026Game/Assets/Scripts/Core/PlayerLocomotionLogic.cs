namespace NAN2026.Core
{
    /// 순수 로직: UnityEngine 비의존. EditMode 테스트 대상.
    public static class PlayerLocomotionLogic
    {
        public static float HorizontalVelocity(float inputX, bool runHeld, float walkSpeed, float runSpeed)
        {
            if (inputX > 0f) inputX = 1f;
            else if (inputX < 0f) inputX = -1f;
            return inputX * (runHeld ? runSpeed : walkSpeed);
        }

        public static bool CanJump(bool attacking, int jumpsUsed, int maxJumps)
        {
            return !attacking && jumpsUsed < maxJumps;
        }

        public static bool CanAttack(bool attacking)
        {
            return !attacking;
        }

        public static string SelectAnimState(string activeAttack, bool grounded, bool landing, float verticalVelocity, float apexThreshold, float inputX, bool runHeld)
        {
            if (!string.IsNullOrEmpty(activeAttack)) return activeAttack;
            if (!grounded)
            {
                if (verticalVelocity > apexThreshold) return "JumpRise";
                if (verticalVelocity < -apexThreshold) return "JumpFall";
                return "JumpApex";
            }
            if (landing && inputX == 0f) return "Land";
            if (inputX != 0f) return runHeld ? "Run" : "Walk";
            return "Idle";
        }

        public static float CameraDeadzoneTargetX(float camX, float playerX, float deadzoneWidth)
        {
            float half = deadzoneWidth * 0.5f;
            if (playerX > camX + half) return playerX - half;
            if (playerX < camX - half) return playerX + half;
            return camX;
        }

        public static float EffectDirection(bool facingLeft)
        {
            return facingLeft ? -1f : 1f;
        }

        public static bool ShouldIgnoreGround(float verticalVelocity, float riseThreshold)
        {
            return verticalVelocity > riseThreshold;
        }

        public static float AttackVelocity(bool facingLeft, float lungeSpeed)
        {
            return facingLeft ? -lungeSpeed : lungeSpeed;
        }

        public static bool ShouldFlipLeft(float inputX, bool currentFlip)
        {
            if (inputX < 0f) return true;
            if (inputX > 0f) return false;
            return currentFlip;
        }
    }
}