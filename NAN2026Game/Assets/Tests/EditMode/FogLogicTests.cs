using NUnit.Framework;
using NAN2026.Core;

namespace NAN2026.Tests
{
    public class FogLogicTests
    {
        [Test] public void RevealFactor_InsideRadius_Full() => Assert.AreEqual(1f, FogLogic.RevealFactor(3f, 5f, 2f));
        [Test] public void RevealFactor_BeyondSoftEdge_Zero() => Assert.AreEqual(0f, FogLogic.RevealFactor(8f, 5f, 2f));
        [Test] public void RevealFactor_MidSoftEdge_Half() => Assert.AreEqual(0.5f, FogLogic.RevealFactor(6f, 5f, 2f), 1e-4f);
        [Test] public void RevealFactor_ZeroSoft_NoBleed() => Assert.AreEqual(0f, FogLogic.RevealFactor(5.01f, 5f, 0f));
        [Test] public void ShouldRestamp_UnderThreshold_False() => Assert.IsFalse(FogLogic.ShouldRestamp(0.1f, 0.1f, 0.25f));
        [Test] public void ShouldRestamp_OverThreshold_True() => Assert.IsTrue(FogLogic.ShouldRestamp(0.3f, 0f, 0.25f));

        [Test] public void AngleBucket_Right_MidBucket() => Assert.AreEqual(180, FogLogic.AngleBucket(1f, 0f, 360));
        [Test] public void AngleBucket_Up_ThreeQuarter() => Assert.AreEqual(270, FogLogic.AngleBucket(0f, 1f, 360));
        [Test] public void AngleBucket_Left_EdgeWrap_InRange()
        {
            int b = FogLogic.AngleBucket(-1f, 0f, 360);
            Assert.IsTrue(b == 0 || b == 359);
        }
        [Test] public void VisibleAt_WithinBlocked_True() => Assert.IsTrue(FogLogic.VisibleAt(4f, 5f, 0.5f));
        [Test] public void VisibleAt_JustPastBlocked_ToleranceCovers() => Assert.IsTrue(FogLogic.VisibleAt(5.4f, 5f, 0.5f));
        [Test] public void VisibleAt_BeyondTolerance_False() => Assert.IsFalse(FogLogic.VisibleAt(5.6f, 5f, 0.5f));
    }
}
