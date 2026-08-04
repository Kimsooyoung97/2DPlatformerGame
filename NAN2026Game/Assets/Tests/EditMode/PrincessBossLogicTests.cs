using NUnit.Framework;
using NAN2026.Core;

namespace NAN2026.Tests
{
    public class PrincessBossLogicTests
    {
        [Test] public void IsBeatHit_ExactMatch()
        {
            Assert.IsTrue(PrincessBossLogic.IsBeatHit(1.0f, 1.0f, 0.2f));
        }

        [Test] public void IsBeatHit_WithinWindow_Early()
        {
            Assert.IsTrue(PrincessBossLogic.IsBeatHit(1.0f, 0.85f, 0.2f));
        }

        [Test] public void IsBeatHit_WithinWindow_Late()
        {
            Assert.IsTrue(PrincessBossLogic.IsBeatHit(1.0f, 1.15f, 0.2f));
        }

        [Test] public void IsBeatHit_OutsideWindow_TooEarly()
        {
            Assert.IsFalse(PrincessBossLogic.IsBeatHit(1.0f, 0.5f, 0.2f));
        }

        [Test] public void IsBeatHit_OutsideWindow_TooLate()
        {
            Assert.IsFalse(PrincessBossLogic.IsBeatHit(1.0f, 1.5f, 0.2f));
        }

        [Test] public void QteSucceeded_AllHits()
        {
            Assert.IsTrue(PrincessBossLogic.QteSucceeded(4, 4));
        }

        [Test] public void QteSucceeded_MissingOne_Fails()
        {
            Assert.IsFalse(PrincessBossLogic.QteSucceeded(3, 4));
        }
    }
}
