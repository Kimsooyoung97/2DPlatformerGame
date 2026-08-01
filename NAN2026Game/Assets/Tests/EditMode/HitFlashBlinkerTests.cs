using NUnit.Framework;
using NAN2026.Core;

public class HitFlashBlinkerTests
{
    [Test]
    public void IsVisible_시작직후에는_보인다()
    {
        Assert.IsTrue(HitFlashBlinker.IsVisible(0f, 0.1f));
    }

    [Test]
    public void IsVisible_한_구간_지나면_숨는다()
    {
        Assert.IsFalse(HitFlashBlinker.IsVisible(0.1f, 0.1f));
    }

    [Test]
    public void IsVisible_두_구간_지나면_다시_보인다()
    {
        Assert.IsTrue(HitFlashBlinker.IsVisible(0.2f, 0.1f));
    }

    [Test]
    public void IsVisible_구간마다_번갈아_뒤집힌다()
    {
        const float interval = 0.05f;
        for (int step = 0; step < 8; step++)
        {
            float mid = interval * step + interval * 0.5f;
            bool expected = step % 2 == 0;
            Assert.AreEqual(expected, HitFlashBlinker.IsVisible(mid, interval),
                "step " + step + " 에서 기대와 다름");
        }
    }

    [Test]
    public void IsVisible_간격이_0이하면_항상_보인다()
    {
        Assert.IsTrue(HitFlashBlinker.IsVisible(0.37f, 0f));
        Assert.IsTrue(HitFlashBlinker.IsVisible(0.37f, -1f));
    }

    [Test]
    public void IsFinished_지속시간_전에는_끝나지_않는다()
    {
        Assert.IsFalse(HitFlashBlinker.IsFinished(0.19f, 0.2f));
    }

    [Test]
    public void IsFinished_지속시간에_도달하면_끝난다()
    {
        Assert.IsTrue(HitFlashBlinker.IsFinished(0.2f, 0.2f));
        Assert.IsTrue(HitFlashBlinker.IsFinished(0.5f, 0.2f));
    }

    [Test]
    public void IsFinished_지속시간이_0이하면_즉시_끝난다()
    {
        Assert.IsTrue(HitFlashBlinker.IsFinished(0f, 0f));
        Assert.IsTrue(HitFlashBlinker.IsFinished(0f, -1f));
    }
}
