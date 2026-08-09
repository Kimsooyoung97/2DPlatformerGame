using NUnit.Framework;
using NAN2026.Core;

public class ChestRewardLogicTests
{
    [Test]
    public void Phase_상승_흡수_완료_순서()
    {
        Assert.AreEqual(ChestRewardLogic.PhaseRise,   ChestRewardLogic.Phase(0.0f, 0.5f, 0.6f));
        Assert.AreEqual(ChestRewardLogic.PhaseRise,   ChestRewardLogic.Phase(0.49f, 0.5f, 0.6f));
        Assert.AreEqual(ChestRewardLogic.PhaseAbsorb, ChestRewardLogic.Phase(0.5f, 0.5f, 0.6f));
        Assert.AreEqual(ChestRewardLogic.PhaseAbsorb, ChestRewardLogic.Phase(1.09f, 0.5f, 0.6f));
        Assert.AreEqual(ChestRewardLogic.PhaseDone,   ChestRewardLogic.Phase(1.10f, 0.5f, 0.6f));
    }

    [Test]
    public void RiseOffset_시작0_끝은_전체거리()
    {
        Assert.AreEqual(0f,   ChestRewardLogic.RiseOffset(0f,   0.5f, 1.6f), 1e-4f);
        Assert.AreEqual(1.6f, ChestRewardLogic.RiseOffset(0.5f, 0.5f, 1.6f), 1e-4f);
        Assert.AreEqual(1.6f, ChestRewardLogic.RiseOffset(9f,   0.5f, 1.6f), 1e-4f);
        // riseTime 0 이면 즉시 최대
        Assert.AreEqual(1.6f, ChestRewardLogic.RiseOffset(0f, 0f, 1.6f), 1e-4f);
    }

    [Test]
    public void AbsorbT_상승중에는_0이고_끝나면_1로_고정()
    {
        Assert.AreEqual(0f, ChestRewardLogic.AbsorbT(0.2f, 0.5f, 0.6f), 1e-4f);
        Assert.AreEqual(0.5f, ChestRewardLogic.AbsorbT(0.8f, 0.5f, 0.6f), 1e-3f);
        Assert.AreEqual(1f, ChestRewardLogic.AbsorbT(5f, 0.5f, 0.6f), 1e-4f);
    }

    [Test]
    public void Alpha_페이드시작_전에는_불투명_도착시_0()
    {
        Assert.AreEqual(1f, ChestRewardLogic.Alpha(0f,    0.35f), 1e-4f);
        Assert.AreEqual(1f, ChestRewardLogic.Alpha(0.35f, 0.35f), 1e-4f);
        Assert.AreEqual(0f, ChestRewardLogic.Alpha(1f,    0.35f), 1e-4f);
        Assert.Less(ChestRewardLogic.Alpha(0.8f, 0.35f), 1f);
    }

    [Test]
    public void ScaleAt_양끝값()
    {
        Assert.AreEqual(1f,   ChestRewardLogic.ScaleAt(0f, 1f, 0.3f), 1e-4f);
        Assert.AreEqual(0.3f, ChestRewardLogic.ScaleAt(1f, 1f, 0.3f), 1e-4f);
    }

    [Test]
    public void NextSlot_용량초과면_음수()
    {
        Assert.AreEqual(0,  ChestRewardLogic.NextSlot(0, 3));
        Assert.AreEqual(2,  ChestRewardLogic.NextSlot(2, 3));
        Assert.AreEqual(-1, ChestRewardLogic.NextSlot(3, 3));
        Assert.AreEqual(-1, ChestRewardLogic.NextSlot(0, 0));
    }

    [Test]
    public void PopScale_0에서_시작해_1로_끝난다()
    {
        Assert.AreEqual(0f, ChestRewardLogic.PopScale(0f,   0.3f, 1.5f), 1e-4f);
        Assert.AreEqual(1.5f, ChestRewardLogic.PopScale(0.15f, 0.3f, 1.5f), 1e-3f);
        Assert.AreEqual(1f, ChestRewardLogic.PopScale(0.3f, 0.3f, 1.5f), 1e-4f);
        Assert.AreEqual(1f, ChestRewardLogic.PopScale(9f,   0.3f, 1.5f), 1e-4f);
    }
}
