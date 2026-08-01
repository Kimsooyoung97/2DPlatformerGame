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
    }
}