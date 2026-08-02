using NUnit.Framework;
using NAN2026.Core;

namespace NAN2026.Tests
{
    public class EnemyAILogicTests
    {
        [Test] public void State_Patrol_WhenFar()
        {
            Assert.AreEqual(EnemyAIState.Patrol, EnemyAILogic.DetermineState(20f, false, 8f, 1.5f, 12f));
        }

        [Test] public void State_Chase_WhenInAggroButOutsideAttack()
        {
            Assert.AreEqual(EnemyAIState.Chase, EnemyAILogic.DetermineState(5f, false, 8f, 1.5f, 12f));
        }

        [Test] public void State_Attack_WhenInAttackRange()
        {
            Assert.AreEqual(EnemyAIState.Attack, EnemyAILogic.DetermineState(1f, false, 8f, 1.5f, 12f));
        }

        [Test] public void State_StaysEngaged_UntilChaseStopDistance()
        {
            Assert.AreEqual(EnemyAIState.Chase, EnemyAILogic.DetermineState(10f, true, 8f, 1.5f, 12f));
        }

        [Test] public void State_GivesUp_PastChaseStopDistance()
        {
            Assert.AreEqual(EnemyAIState.Patrol, EnemyAILogic.DetermineState(13f, true, 8f, 1.5f, 12f));
        }

        [Test] public void State_EngagedAttack_WhenBackInRange()
        {
            Assert.AreEqual(EnemyAIState.Attack, EnemyAILogic.DetermineState(1f, true, 8f, 1.5f, 12f));
        }

        [Test] public void Jump_Needed_WhenTargetHigherAndGrounded()
        {
            Assert.IsTrue(EnemyAILogic.NeedsJumpToFollow(0f, 2f, 1.5f, true));
        }

        [Test] public void Jump_NotNeeded_WhenNotGrounded()
        {
            Assert.IsFalse(EnemyAILogic.NeedsJumpToFollow(0f, 2f, 1.5f, false));
        }

        [Test] public void Jump_NotNeeded_WhenBelowThreshold()
        {
            Assert.IsFalse(EnemyAILogic.NeedsJumpToFollow(0f, 1f, 1.5f, true));
        }

        [Test] public void Patrol_FlipsAtRightBound()
        {
            Assert.AreEqual(-1f, EnemyAILogic.PatrolDirection(5f, 0f, 5f, 1f));
        }

        [Test] public void Patrol_FlipsAtLeftBound()
        {
            Assert.AreEqual(1f, EnemyAILogic.PatrolDirection(0f, 0f, 5f, -1f));
        }

        [Test] public void Patrol_KeepsDirection_MidRange()
        {
            Assert.AreEqual(1f, EnemyAILogic.PatrolDirection(2.5f, 0f, 5f, 1f));
        }

        [Test] public void HealthRatio_ClampedHigh()
        {
            Assert.AreEqual(1f, EnemyAILogic.HealthRatio(10f, 5f));
        }

        [Test] public void HealthRatio_ClampedLow()
        {
            Assert.AreEqual(0f, EnemyAILogic.HealthRatio(-1f, 5f));
        }

        [Test] public void HealthRatio_Normal()
        {
            Assert.AreEqual(0.5f, EnemyAILogic.HealthRatio(5f, 10f));
        }

        [Test] public void HealthRatio_ZeroMax_ReturnsZero()
        {
            Assert.AreEqual(0f, EnemyAILogic.HealthRatio(3f, 0f));
        }
    }
}
