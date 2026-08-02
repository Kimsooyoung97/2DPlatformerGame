using NUnit.Framework;
using NAN2026.Core;

namespace NAN2026.Tests
{
    public class LevelProgressionLogicTests
    {
        [Test] public void RequiredXp_GrowsWithLevel()
        {
            Assert.AreEqual(10, LevelProgressionLogic.RequiredXpForLevel(1, 10, 5));
            Assert.AreEqual(15, LevelProgressionLogic.RequiredXpForLevel(2, 10, 5));
            Assert.AreEqual(20, LevelProgressionLogic.RequiredXpForLevel(3, 10, 5));
        }

        [Test] public void TryLevelUp_NoLevelUp_WhenNotEnoughXp()
        {
            LevelProgressionLogic.TryLevelUp(5, 1, 10, 5, out int newLevel, out int remaining);
            Assert.AreEqual(1, newLevel);
            Assert.AreEqual(5, remaining);
        }

        [Test] public void TryLevelUp_SingleLevelUp()
        {
            LevelProgressionLogic.TryLevelUp(10, 1, 10, 5, out int newLevel, out int remaining);
            Assert.AreEqual(2, newLevel);
            Assert.AreEqual(0, remaining);
        }

        [Test] public void TryLevelUp_MultipleLevelsAtOnce()
        {
            // level1 필요 10, level2 필요 15, level3 필요 20 -> 총 25면 딱 레벨3 진입, 남는 것 0
            LevelProgressionLogic.TryLevelUp(25, 1, 10, 5, out int newLevel, out int remaining);
            Assert.AreEqual(3, newLevel);
            Assert.AreEqual(0, remaining);
        }

        [Test] public void TryLevelUp_LeavesOverflowXp()
        {
            LevelProgressionLogic.TryLevelUp(12, 1, 10, 5, out int newLevel, out int remaining);
            Assert.AreEqual(2, newLevel);
            Assert.AreEqual(2, remaining);
        }

        [Test] public void GoldChance_ClampsAtMax()
        {
            Assert.AreEqual(0.4f, LevelProgressionLogic.GoldChanceForLevel(100, 0.1f, 0.02f, 0.4f), 0.0001f);
        }

        [Test] public void GoldChance_IncreasesWithLevel()
        {
            float lvl1 = LevelProgressionLogic.GoldChanceForLevel(1, 0.1f, 0.02f, 0.4f);
            float lvl5 = LevelProgressionLogic.GoldChanceForLevel(5, 0.1f, 0.02f, 0.4f);
            Assert.Greater(lvl5, lvl1);
        }

        [Test] public void TierForRoll_LowRoll_IsGold()
        {
            Assert.AreEqual(2, LevelProgressionLogic.TierForRoll(0.05f, 0.1f, 0.3f));
        }

        [Test] public void TierForRoll_MidRoll_IsSilver()
        {
            Assert.AreEqual(1, LevelProgressionLogic.TierForRoll(0.25f, 0.1f, 0.3f));
        }

        [Test] public void TierForRoll_HighRoll_IsBronze()
        {
            Assert.AreEqual(0, LevelProgressionLogic.TierForRoll(0.9f, 0.1f, 0.3f));
        }
    }
}
