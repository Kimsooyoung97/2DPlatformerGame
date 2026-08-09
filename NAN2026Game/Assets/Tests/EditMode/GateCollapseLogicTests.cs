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

    [Test] public void 구간_판정은_1과2단만_포함한다()
    {
        // 1단 y0.04 / 2단 y2.16 는 포함, 보스 구역 y10.19 는 제외
        Assert.IsTrue(GateCollapseLogic.InClearBand(0.04f, -1f, 5f));
        Assert.IsTrue(GateCollapseLogic.InClearBand(2.16f, -1f, 5f));
        Assert.IsFalse(GateCollapseLogic.InClearBand(10.19f, -1f, 5f));
        Assert.IsFalse(GateCollapseLogic.InClearBand(-2f, -1f, 5f));
    }

    [Test] public void 전멸해야_열린다()
    {
        Assert.IsFalse(GateCollapseLogic.ShouldOpen(1, 9, false));
        Assert.IsTrue(GateCollapseLogic.ShouldOpen(0, 9, false));
    }

    [Test] public void 이미_열렸으면_다시_열지_않는다()
    {
        Assert.IsFalse(GateCollapseLogic.ShouldOpen(0, 9, true));
    }

    [Test] public void 셀_대상이_없으면_열지_않는다()
    {
        // 수집이 실패해 0마리가 잡히면 '전멸' 로 오인해 즉시 열리는 사고를 막는다
        Assert.IsFalse(GateCollapseLogic.ShouldOpen(0, 0, false));
    }

    [Test] public void 폴링_간격_판정()
    {
        Assert.IsFalse(GateCollapseLogic.TickDue(0.1f, 0.25f));
        Assert.IsTrue(GateCollapseLogic.TickDue(0.25f, 0.25f));
        Assert.IsTrue(GateCollapseLogic.TickDue(0f, 0f));   // 간격 0 이면 매 프레임
    }
}
