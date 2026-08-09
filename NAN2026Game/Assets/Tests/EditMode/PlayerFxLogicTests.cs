using NUnit.Framework;
using NAN2026.Core;

namespace NAN2026.Tests
{
    public class PlayerFxLogicTests
    {
        [Test] public void 체력이_줄면_피격연출()
        {
            Assert.IsTrue(PlayerFxLogic.ShouldPlayHurt(10, 9));
            Assert.IsTrue(PlayerFxLogic.ShouldPlayHurt(3, 1));
        }

        [Test] public void 회복이나_동일하면_재생하지_않는다()
        {
            Assert.IsFalse(PlayerFxLogic.ShouldPlayHurt(5, 5));
            Assert.IsFalse(PlayerFxLogic.ShouldPlayHurt(5, 8));
        }

        [Test] public void 죽는_타격은_피격연출이_아니다()
        {
            Assert.IsFalse(PlayerFxLogic.ShouldPlayHurt(1, 0));
            Assert.IsFalse(PlayerFxLogic.ShouldPlayHurt(1, -2));
        }

        [Test] public void 사망_판정()
        {
            Assert.IsTrue(PlayerFxLogic.ShouldPlayDeath(0));
            Assert.IsTrue(PlayerFxLogic.ShouldPlayDeath(-3));
            Assert.IsFalse(PlayerFxLogic.ShouldPlayDeath(1));
        }

        [Test] public void 재생_길이()
        {
            Assert.AreEqual(0.75f, PlayerFxLogic.Duration(6, 8f, 0f), 0.0001f);
            Assert.AreEqual(1.15f, PlayerFxLogic.Duration(6, 8f, 0.4f), 0.0001f);
        }

        [Test] public void 프레임0이면_유지시간만()
        {
            Assert.AreEqual(0.3f, PlayerFxLogic.Duration(0, 8f, 0.3f), 0.0001f);
            Assert.AreEqual(0f, PlayerFxLogic.Duration(6, 0f, -1f), 0.0001f);
        }

        [Test] public void 부활지연은_연출보다_짧아지지_않는다()
        {
            Assert.AreEqual(1.15f, PlayerFxLogic.RespawnDelay(0.2f, 1.15f), 0.0001f);
            Assert.AreEqual(2f, PlayerFxLogic.RespawnDelay(2f, 1.15f), 0.0001f);
        }
    }
}
