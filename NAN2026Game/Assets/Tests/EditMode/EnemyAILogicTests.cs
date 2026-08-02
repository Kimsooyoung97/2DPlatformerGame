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

        [Test] public void HeightGapTimer_Accumulates_WhileAboveThreshold()
        {
            float t = EnemyAILogic.UpdateHeightGapTimer(true, 0f, 0.2f);
            t = EnemyAILogic.UpdateHeightGapTimer(true, t, 0.2f);
            Assert.AreEqual(0.4f, t, 0.0001f);
        }

        [Test] public void HeightGapTimer_ResetsImmediately_WhenBelowThreshold()
        {
            float t = EnemyAILogic.UpdateHeightGapTimer(true, 0.3f, 0.2f);
            t = EnemyAILogic.UpdateHeightGapTimer(false, t, 0.2f);
            Assert.AreEqual(0f, t);
        }

        [Test] public void ShouldJumpNow_False_BeforeSustainDuration()
        {
            Assert.IsFalse(EnemyAILogic.ShouldJumpNow(0.2f, 0.35f));
        }

        [Test] public void ShouldJumpNow_True_AfterSustainDuration()
        {
            Assert.IsTrue(EnemyAILogic.ShouldJumpNow(0.4f, 0.35f));
        }

        [Test] public void PlayerOwnJump_DoesNotTriggerFollow_WithinBriefSpike()
        {
            // 플레이어가 제자리 점프해서 0.15초만 문턱을 넘었다가 다시 내려온 상황을 시뮬레이션.
            float timer = 0f;
            timer = EnemyAILogic.UpdateHeightGapTimer(true, timer, 0.15f);
            bool jumpDuringSpike = EnemyAILogic.ShouldJumpNow(timer, 0.35f);
            timer = EnemyAILogic.UpdateHeightGapTimer(false, timer, 0.02f); // 다시 착지, 높이차 사라짐

            Assert.IsFalse(jumpDuringSpike);
            Assert.AreEqual(0f, timer);
        }
    }
}
