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
    }
}
