using NUnit.Framework;
using NAN2026.Core;

namespace NAN2026.Tests
{
    public class PlayerLocomotionLogicTests
    {
        [Test] public void Horizontal_Walk_Right() { Assert.AreEqual(2f, PlayerLocomotionLogic.HorizontalVelocity(1f, false, 2f, 4f)); }
        [Test] public void Horizontal_Run_Left() { Assert.AreEqual(-4f, PlayerLocomotionLogic.HorizontalVelocity(-0.7f, true, 2f, 4f)); }
        [Test] public void Horizontal_NoInput_Zero() { Assert.AreEqual(0f, PlayerLocomotionLogic.HorizontalVelocity(0f, true, 2f, 4f)); }
        [Test] public void DoubleJump_Rules()
        {
            Assert.IsTrue(PlayerLocomotionLogic.CanJump(false, 0, 2));
            Assert.IsTrue(PlayerLocomotionLogic.CanJump(false, 1, 2));
            Assert.IsFalse(PlayerLocomotionLogic.CanJump(false, 2, 2));
            Assert.IsFalse(PlayerLocomotionLogic.CanJump(true, 0, 2));
        }
        [Test] public void Attack_AllowedInAir_BlockedWhileAttacking()
        {
            Assert.IsTrue(PlayerLocomotionLogic.CanAttack(false));
            Assert.IsFalse(PlayerLocomotionLogic.CanAttack(true));
        }
        [Test] public void AnimState_AirStates()
        {
            Assert.AreEqual("JumpRise", PlayerLocomotionLogic.SelectAnimState(null, false, false, 5f, 1.2f, 0f, false));
            Assert.AreEqual("JumpApex", PlayerLocomotionLogic.SelectAnimState(null, false, false, 0.5f, 1.2f, 0f, false));
            Assert.AreEqual("JumpFall", PlayerLocomotionLogic.SelectAnimState(null, false, false, -3f, 1.2f, 0f, false));
        }
        [Test] public void AnimState_GroundPriority()
        {
            Assert.AreEqual("Slash", PlayerLocomotionLogic.SelectAnimState("Slash", true, false, 0f, 1.2f, 1f, true));
            Assert.AreEqual("Combo3", PlayerLocomotionLogic.SelectAnimState("Combo3", false, false, 5f, 1.2f, 1f, true));
            Assert.AreEqual("Land", PlayerLocomotionLogic.SelectAnimState(null, true, true, 0f, 1.2f, 0f, false));
            Assert.AreEqual("Walk", PlayerLocomotionLogic.SelectAnimState(null, true, true, 0f, 1.2f, 1f, false));
            Assert.AreEqual("Run", PlayerLocomotionLogic.SelectAnimState(null, true, false, 0f, 1.2f, 1f, true));
            Assert.AreEqual("Idle", PlayerLocomotionLogic.SelectAnimState(null, true, false, 0f, 1.2f, 0f, false));
        }
        [Test] public void NoteJudgment_PerfectInsideThreshold()
        {
            Assert.AreEqual(0, PlayerLocomotionLogic.NoteJudgment(0.1f, 0.25f));
            Assert.AreEqual(0, PlayerLocomotionLogic.NoteJudgment(-0.2f, 0.25f));
            Assert.AreEqual(1, PlayerLocomotionLogic.NoteJudgment(0.4f, 0.25f));
        }

        [Test] public void ParryPhase_HoldAndRelease()
        {
            Assert.AreEqual(1, PlayerLocomotionLogic.ParryPhase(true, false));
            Assert.AreEqual(2, PlayerLocomotionLogic.ParryPhase(false, true));
            Assert.AreEqual(0, PlayerLocomotionLogic.ParryPhase(false, false));
        }

        [Test] public void ParryWindow_OnlyEarly()
        {
            Assert.IsTrue(PlayerLocomotionLogic.ParrySuccessWindow(0.1f, 0.18f));
            Assert.IsFalse(PlayerLocomotionLogic.ParrySuccessWindow(0.3f, 0.18f));
            Assert.IsFalse(PlayerLocomotionLogic.ParrySuccessWindow(-1f, 0.18f));
        }

        [Test] public void SequenceStage_Progression()
        {
            float[] d = new float[] { 2f, 1f, 1f };
            Assert.AreEqual(0, PlayerLocomotionLogic.SequenceStage(1.9f, d));
            Assert.AreEqual(1, PlayerLocomotionLogic.SequenceStage(2.5f, d));
            Assert.AreEqual(2, PlayerLocomotionLogic.SequenceStage(3.5f, d));
            Assert.AreEqual(3, PlayerLocomotionLogic.SequenceStage(99f, d));
        }

        [Test] public void CrumblePhase_Progression()
        {
            Assert.AreEqual(0, PlayerLocomotionLogic.CrumblePhase(-1f, 0.8f, 2.5f));
            Assert.AreEqual(1, PlayerLocomotionLogic.CrumblePhase(0.3f, 0.8f, 2.5f));
            Assert.AreEqual(2, PlayerLocomotionLogic.CrumblePhase(1.5f, 0.8f, 2.5f));
            Assert.AreEqual(3, PlayerLocomotionLogic.CrumblePhase(3.4f, 0.8f, 2.5f));
        }

        [Test] public void CameraDeadzone_HoldsInsideMovesOutside()
        {
            Assert.AreEqual(5f, PlayerLocomotionLogic.CameraDeadzoneTargetX(5f, 5.4f, 1.2f));
            Assert.AreEqual(6.4f, PlayerLocomotionLogic.CameraDeadzoneTargetX(5f, 7f, 1.2f), 0.0001f);
            Assert.AreEqual(3.6f, PlayerLocomotionLogic.CameraDeadzoneTargetX(5f, 3f, 1.2f), 0.0001f);
        }

        [Test] public void EffectDirection_FollowsFacing()
        {
            Assert.AreEqual(1f, PlayerLocomotionLogic.EffectDirection(false));
            Assert.AreEqual(-1f, PlayerLocomotionLogic.EffectDirection(true));
        }

        [Test] public void OnewayIgnore_OnlyWhileRising()
        {
            Assert.IsTrue(PlayerLocomotionLogic.ShouldIgnoreGround(3f, 0.05f));
            Assert.IsFalse(PlayerLocomotionLogic.ShouldIgnoreGround(0f, 0.05f));
            Assert.IsFalse(PlayerLocomotionLogic.ShouldIgnoreGround(-2f, 0.05f));
        }

        [Test] public void AttackVelocity_FollowsFacing()
        {
            Assert.AreEqual(3.5f, PlayerLocomotionLogic.AttackVelocity(false, 3.5f));
            Assert.AreEqual(-3.5f, PlayerLocomotionLogic.AttackVelocity(true, 3.5f));
            Assert.AreEqual(0f, PlayerLocomotionLogic.AttackVelocity(true, 0f));
        }
        [Test] public void Flip_KeepsFacingWhenIdle()
        {
            Assert.IsTrue(PlayerLocomotionLogic.ShouldFlipLeft(-1f, false));
            Assert.IsFalse(PlayerLocomotionLogic.ShouldFlipLeft(1f, true));
            Assert.IsTrue(PlayerLocomotionLogic.ShouldFlipLeft(0f, true));
        }

        [Test] public void GroundNormal_FloorCountsAsGround()
        {
            Assert.IsTrue(PlayerLocomotionLogic.IsGroundNormal(1f, 0.5f));
        }

        [Test] public void GroundNormal_VerticalWallIsNotGround()
        {
            Assert.IsFalse(PlayerLocomotionLogic.IsGroundNormal(0f, 0.5f));
        }

        [Test] public void GroundNormal_SteepSlopeStillCountsAsGround()
        {
            Assert.IsTrue(PlayerLocomotionLogic.IsGroundNormal(0.6f, 0.5f));
        }

        [Test] public void GroundNormal_ShallowAngleWallDoesNotCount()
        {
            Assert.IsFalse(PlayerLocomotionLogic.IsGroundNormal(0.4f, 0.5f));
        }

        [Test] public void WallClamp_BlocksRightwardIntoRightWall()
        {
            Assert.AreEqual(0f, PlayerLocomotionLogic.ClampHorizontalVelocityAgainstWalls(3f, false, true));
        }

        [Test] public void WallClamp_BlocksLeftwardIntoLeftWall()
        {
            Assert.AreEqual(0f, PlayerLocomotionLogic.ClampHorizontalVelocityAgainstWalls(-3f, true, false));
        }

        [Test] public void WallClamp_AllowsMovementAwayFromWall()
        {
            Assert.AreEqual(-3f, PlayerLocomotionLogic.ClampHorizontalVelocityAgainstWalls(-3f, false, true));
        }

        [Test] public void WallClamp_NoWalls_Unaffected()
        {
            Assert.AreEqual(3f, PlayerLocomotionLogic.ClampHorizontalVelocityAgainstWalls(3f, false, false));
        }

        [Test] public void WallClamp_ZeroVelocity_Unaffected()
        {
            Assert.AreEqual(0f, PlayerLocomotionLogic.ClampHorizontalVelocityAgainstWalls(0f, true, true));
        }

        [Test] public void DashActive_TrueBeforeMaxDistance()
        {
            Assert.IsTrue(PlayerLocomotionLogic.DashActive(5f, 8f));
        }

        [Test] public void DashActive_FalseAtMaxDistance()
        {
            Assert.IsFalse(PlayerLocomotionLogic.DashActive(8f, 8f));
        }

        [Test] public void DashActive_FalseBeyondMaxDistance()
        {
            Assert.IsFalse(PlayerLocomotionLogic.DashActive(9f, 8f));
        }

        [Test] public void CanDash_AlwaysTrueWhenGrounded()
        {
            Assert.IsTrue(PlayerLocomotionLogic.CanDash(true, 5, 1));
        }

        [Test] public void CanDash_TrueInAir_WhenUnderLimit()
        {
            Assert.IsTrue(PlayerLocomotionLogic.CanDash(false, 0, 1));
        }

        [Test] public void CanDash_FalseInAir_WhenLimitReached()
        {
            Assert.IsFalse(PlayerLocomotionLogic.CanDash(false, 1, 1));
        }

        [Test] public void ParryDirection_FacingRight_AttackerToRight_IsFront()
        {
            Assert.IsTrue(PlayerLocomotionLogic.IsAttackerInFront(0f, 5f, false));
        }

        [Test] public void ParryDirection_FacingRight_AttackerToLeft_IsBehind()
        {
            Assert.IsFalse(PlayerLocomotionLogic.IsAttackerInFront(0f, -5f, false));
        }

        [Test] public void ParryDirection_FacingLeft_AttackerToLeft_IsFront()
        {
            Assert.IsTrue(PlayerLocomotionLogic.IsAttackerInFront(0f, -5f, true));
        }

        [Test] public void ParryDirection_FacingLeft_AttackerToRight_IsBehind()
        {
            Assert.IsFalse(PlayerLocomotionLogic.IsAttackerInFront(0f, 5f, true));
        }

        [Test] public void ParryDirection_SamePosition_CountsAsFront()
        {
            Assert.IsTrue(PlayerLocomotionLogic.IsAttackerInFront(3f, 3f, false));
            Assert.IsTrue(PlayerLocomotionLogic.IsAttackerInFront(3f, 3f, true));
        }

        [Test] public void LaunchVelocity_FlatDistance_NoGravity()
        {
            var (vx, vy) = PlayerLocomotionLogic.LaunchVelocityForTarget(10f, 0f, 2f, 0f);
            Assert.AreEqual(5f, vx, 0.001f);
            Assert.AreEqual(0f, vy, 0.001f);
        }

        [Test] public void LaunchVelocity_UpwardTarget_CompensatesGravity()
        {
            // dy=0, duration=2, gravity=10 -> vy = (0 + 0.5*10*4)/2 = 10
            var (vx, vy) = PlayerLocomotionLogic.LaunchVelocityForTarget(0f, 0f, 2f, 10f);
            Assert.AreEqual(10f, vy, 0.001f);
        }

        [Test] public void LaunchVelocity_ReachesTargetUnderGravity()
        {
            float dx = 8f, dy = 5f, duration = 1.5f, gravity = 9.8f;
            var (vx, vy) = PlayerLocomotionLogic.LaunchVelocityForTarget(dx, dy, duration, gravity);
            // 위치 공식으로 역검증: x(T)=vx*T, y(T)=vy*T-0.5*g*T^2
            float finalX = vx * duration;
            float finalY = vy * duration - 0.5f * gravity * duration * duration;
            Assert.AreEqual(dx, finalX, 0.01f);
            Assert.AreEqual(dy, finalY, 0.01f);
        }
    }
}