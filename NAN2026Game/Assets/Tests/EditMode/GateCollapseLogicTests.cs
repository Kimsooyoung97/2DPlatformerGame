using NUnit.Framework;
using NAN2026.Core;

public class GateCollapseLogicTests
{
    const float D = 0.4f, C = 0.8f, H = 0.6f;

    [Test] public void 페이즈_경계()
    {
        Assert.AreEqual(0, GateCollapseLogic.GetPhase(0.2f, D, C, H));
        Assert.AreEqual(1, GateCollapseLogic.GetPhase(0.5f, D, C, H));
        Assert.AreEqual(2, GateCollapseLogic.GetPhase(1.4f, D, C, H));
        Assert.AreEqual(3, GateCollapseLogic.GetPhase(D + C + H, D, C, H));
    }

    [Test] public void 틴트는_붕괴_구간에서_1에서_0으로()
    {
        Assert.AreEqual(1f, GateCollapseLogic.TintAlpha(D, D, C));
        Assert.AreEqual(0.5f, GateCollapseLogic.TintAlpha(D + C * 0.5f, D, C), 0.001f);
        Assert.AreEqual(0f, GateCollapseLogic.TintAlpha(D + C, D, C), 0.001f);
    }

    [Test] public void 조명은_유지_구간에서만_상승()
    {
        Assert.AreEqual(0f, GateCollapseLogic.LightFactor(D + C, D, C, H));
        Assert.AreEqual(1f, GateCollapseLogic.LightFactor(D + C + H, D, C, H), 0.001f);
    }

    [Test] public void 팬은_복귀_시점에_꺼진다()
    {
        Assert.IsTrue(GateCollapseLogic.PanActive(0f, D, C, H));
        Assert.IsFalse(GateCollapseLogic.PanActive(D + C + H, D, C, H));
    }

    [Test] public void 지속시간_0_안전()
    {
        Assert.IsFalse(float.IsNaN(GateCollapseLogic.TintAlpha(1f, 0f, 0f)));
        Assert.AreEqual(1f, GateCollapseLogic.LightFactor(1f, 0f, 0f, 0f));
    }
}
