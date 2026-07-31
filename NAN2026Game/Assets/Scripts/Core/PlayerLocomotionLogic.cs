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

        public static bool CanJump(bool grounded, bool attacking)
        {
            return grounded && !attacking;
        }

        public static bool CanAttack(bool grounded, bool attacking)
        {
            return grounded && !attacking;
        }

        public static string SelectAnimState(bool attacking, bool grounded, float inputX, bool runHeld)
        {
            if (attacking) return "Slash";
            if (!grounded) return "Idle";
            if (inputX != 0f) return runHeld ? "Run" : "Walk";
            return "Idle";
        }

        public static bool ShouldFlipLeft(float inputX, bool currentFlip)
        {
            if (inputX < 0f) return true;
            if (inputX > 0f) return false;
            return currentFlip;
        }
    }
}