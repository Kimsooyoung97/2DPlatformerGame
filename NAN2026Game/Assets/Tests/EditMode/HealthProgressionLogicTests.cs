using NUnit.Framework;
using NAN2026.Core;

namespace NAN2026.Tests
{
    public class HealthProgressionLogicTests
    {
        [Test] public void ClampedMaxHealth_AddsBonusToBase()
        {
            Assert.AreEqual(6, HealthProgressionLogic.ClampedMaxHealth(5, 1, 20));
        }

        [Test] public void ClampedMaxHealth_ClampsAtCap()
        {
            Assert.AreEqual(20, HealthProgressionLogic.ClampedMaxHealth(5, 30, 20));
        }

        [Test] public void ClampedMaxHealth_NeverBelowBase()
        {
            Assert.AreEqual(5, HealthProgressionLogic.ClampedMaxHealth(5, -3, 20));
        }

        [Test] public void ActualMaxHealthGain_FullAmount_WhenBelowCap()
        {
            Assert.AreEqual(1, HealthProgressionLogic.ActualMaxHealthGain(5, 0, 1, 20));
            Assert.AreEqual(3, HealthProgressionLogic.ActualMaxHealthGain(5, 0, 3, 20));
        }

        [Test] public void ActualMaxHealthGain_PartialAmount_WhenCrossingCap()
        {
            // base 5, bonus 14 -> max 19. amount 3 -> would be 22, capped 20. actual gain = 1
            Assert.AreEqual(1, HealthProgressionLogic.ActualMaxHealthGain(5, 14, 3, 20));
        }

        [Test] public void ActualMaxHealthGain_Zero_WhenAlreadyAtCap()
        {
            Assert.AreEqual(0, HealthProgressionLogic.ActualMaxHealthGain(5, 15, 1, 20));
        }

        [Test] public void ActualMaxHealthGain_Zero_WhenAmountNotPositive()
        {
            Assert.AreEqual(0, HealthProgressionLogic.ActualMaxHealthGain(5, 0, 0, 20));
            Assert.AreEqual(0, HealthProgressionLogic.ActualMaxHealthGain(5, 0, -1, 20));
        }
    }
}
