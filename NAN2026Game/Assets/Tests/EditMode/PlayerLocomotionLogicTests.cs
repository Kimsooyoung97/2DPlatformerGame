using NUnit.Framework;
using NAN2026.Core;

namespace NAN2026.Tests
{
    public class PlayerLocomotionLogicTests
    {
        [Test] public void Horizontal_Walk_Right() { Assert.AreEqual(2f, PlayerLocomotionLogic.HorizontalVelocity(1f, false, 2f, 4f)); }
        [Test] public void Horizontal_Run_Left() { Assert.AreEqual(-4f, PlayerLocomotionLogic.HorizontalVelocity(-0.7f, true, 2f, 4f)); }
        [Test] public void Horizontal_NoInput_Zero() { Assert.AreEqual(0f, PlayerLocomotionLogic.HorizontalVelocity(0f, true, 2f, 4f)); }
        [Test] public void Jump_OnlyWhenGroundedAndNotAttacking()
        {
            Assert.IsTrue(PlayerLocomotionLogic.CanJump(true, false));
            Assert.IsFalse(PlayerLocomotionLogic.CanJump(false, false));
            Assert.IsFalse(PlayerLocomotionLogic.CanJump(true, true));
        }
        [Test] public void Attack_OnlyWhenGroundedAndNotAttacking()
        {
            Assert.IsTrue(PlayerLocomotionLogic.CanAttack(true, false));
            Assert.IsFalse(PlayerLocomotionLogic.CanAttack(false, false));
            Assert.IsFalse(PlayerLocomotionLogic.CanAttack(true, true));
        }
        [Test] public void AnimState_Priority()
        {
            Assert.AreEqual("Slash", PlayerLocomotionLogic.SelectAnimState("Slash", true, 1f, true));
            Assert.AreEqual("Combo3", PlayerLocomotionLogic.SelectAnimState("Combo3", false, 1f, true));
            Assert.AreEqual("Idle", PlayerLocomotionLogic.SelectAnimState(null, false, 1f, true));
            Assert.AreEqual("Run", PlayerLocomotionLogic.SelectAnimState("", true, 1f, true));
            Assert.AreEqual("Walk", PlayerLocomotionLogic.SelectAnimState(null, true, -1f, false));
            Assert.AreEqual("Idle", PlayerLocomotionLogic.SelectAnimState(null, true, 0f, false));
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