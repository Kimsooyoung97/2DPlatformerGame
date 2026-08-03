using NUnit.Framework;
using NAN2026.Core;

public class IntroSequenceLogicTests
{
    const float B = 0.5f, I = 0.9f, E = 1.2f;

    [Test] public void 페이즈_경계가_정확하다()
    {
        Assert.AreEqual(0, IntroSequenceLogic.GetPhase(0f, B, I, E));
        Assert.AreEqual(1, IntroSequenceLogic.GetPhase(0.5f, B, I, E));
        Assert.AreEqual(2, IntroSequenceLogic.GetPhase(1.4f, B, I, E));
        Assert.AreEqual(3, IntroSequenceLogic.GetPhase(2.6f, B, I, E));
    }

    [Test] public void 암전_중에는_모든_계수가_0이다()
    {
        Assert.AreEqual(0f, IntroSequenceLogic.CandleFactor(0.3f, B, I));
        Assert.AreEqual(0f, IntroSequenceLogic.GlobalFactor(0.3f, B, I, E));
    }

    [Test] public void 촛불_계수는_점화_구간에서_선형_상승한다()
    {
        Assert.AreEqual(0.5f, IntroSequenceLogic.CandleFactor(B + I * 0.5f, B, I), 0.001f);
        Assert.AreEqual(1f, IntroSequenceLogic.CandleFactor(B + I, B, I), 0.001f);
        Assert.AreEqual(1f, IntroSequenceLogic.CandleFactor(99f, B, I));
    }

    [Test] public void 전역_계수는_확장_구간에서만_상승한다()
    {
        Assert.AreEqual(0f, IntroSequenceLogic.GlobalFactor(B + I, B, I, E));
        Assert.AreEqual(0.5f, IntroSequenceLogic.GlobalFactor(B + I + E * 0.5f, B, I, E), 0.001f);
        Assert.AreEqual(1f, IntroSequenceLogic.GlobalFactor(B + I + E, B, I, E), 0.001f);
    }

    [Test] public void BGM은_확장_완료_시점부터_재생된다()
    {
        Assert.IsFalse(IntroSequenceLogic.BgmShouldPlay(B + I + E - 0.01f, B, I, E));
        Assert.IsTrue(IntroSequenceLogic.BgmShouldPlay(B + I + E, B, I, E));
    }

    [Test] public void 지속시간_0이어도_NaN_없이_동작한다()
    {
        float f = IntroSequenceLogic.CandleFactor(1f, 0f, 0f);
        Assert.IsFalse(float.IsNaN(f));
        Assert.AreEqual(1f, f);
        Assert.AreEqual(1f, IntroSequenceLogic.GlobalFactor(1f, 0f, 0f, 0f));
    }
}
