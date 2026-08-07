using NUnit.Framework;
using NAN2026.Core;

namespace NAN2026.Tests
{
    public class MidBossLogicTests
    {
        [Test] public void 공격거리면_2() { Assert.AreEqual(2, MidBossLogic.Phase(1.0f, 8f, 1.6f)); }
        [Test] public void 감지거리면_1() { Assert.AreEqual(1, MidBossLogic.Phase(5f, 8f, 1.6f)); }
        [Test] public void 멀면_0() { Assert.AreEqual(0, MidBossLogic.Phase(20f, 8f, 1.6f)); }
        [Test] public void 반원_왼쪽응시() { Assert.IsTrue(MidBossLogic.InFacingHalf(10f, 8f, true)); Assert.IsFalse(MidBossLogic.InFacingHalf(10f, 12f, true)); }
        [Test] public void 반원_오른쪽응시() { Assert.IsTrue(MidBossLogic.InFacingHalf(10f, 12f, false)); Assert.IsFalse(MidBossLogic.InFacingHalf(10f, 8f, false)); }
        [Test] public void 타격구간_판정() { Assert.IsFalse(MidBossLogic.InStrikeInterval(0.6f, 1.5f, 0.5f, 0.72f)); Assert.IsTrue(MidBossLogic.InStrikeInterval(0.9f, 1.5f, 0.5f, 0.72f)); Assert.IsFalse(MidBossLogic.InStrikeInterval(1.2f, 1.5f, 0.5f, 0.72f)); }
        [Test] public void 타격순간_경계() { Assert.IsFalse(MidBossLogic.HitMomentPassed(0.5f, 1.5f, 0.55f)); Assert.IsTrue(MidBossLogic.HitMomentPassed(0.9f, 1.5f, 0.55f)); }
    }
}
