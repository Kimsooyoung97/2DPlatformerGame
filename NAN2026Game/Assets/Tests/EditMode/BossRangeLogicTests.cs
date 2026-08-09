using NUnit.Framework;
using NAN2026.Core;

namespace NAN2026.Tests
{
    public class BossRangeLogicTests
    {
        const float Reach = 6f;
        const float Dead = 1f;

        [Test] public void 왼쪽을_보면_띠가_왼쪽으로_뻗는다()
        {
            Assert.AreEqual(4f, BossRangeLogic.BandMinX(10f, Reach, -1f, Dead), 0.0001f);
            Assert.AreEqual(11f, BossRangeLogic.BandMaxX(10f, Reach, -1f, Dead), 0.0001f);
        }

        [Test] public void 오른쪽을_보면_띠가_오른쪽으로_뻗는다()
        {
            Assert.AreEqual(9f, BossRangeLogic.BandMinX(10f, Reach, 1f, Dead), 0.0001f);
            Assert.AreEqual(16f, BossRangeLogic.BandMaxX(10f, Reach, 1f, Dead), 0.0001f);
        }

        [Test] public void 정면_사거리_안이면_맞는다()
        {
            Assert.IsTrue(BossRangeLogic.InHitBand(10f, 15f, Reach, 1f, Dead));
            Assert.IsTrue(BossRangeLogic.InHitBand(10f, 5f, Reach, -1f, Dead));
        }

        [Test] public void 정면이어도_사거리_밖이면_빗나간다()
        {
            Assert.IsFalse(BossRangeLogic.InHitBand(10f, 16.5f, Reach, 1f, Dead));
            Assert.IsFalse(BossRangeLogic.InHitBand(10f, 3.5f, Reach, -1f, Dead));
        }

        [Test] public void 사거리_안이어도_등뒤면_빗나간다()
        {
            Assert.IsFalse(BossRangeLogic.InHitBand(10f, 5f, Reach, 1f, Dead));
            Assert.IsFalse(BossRangeLogic.InHitBand(10f, 15f, Reach, -1f, Dead));
        }

        [Test] public void 사거리_경계는_포함한다()
        {
            Assert.IsTrue(BossRangeLogic.InHitBand(10f, 16f, Reach, 1f, Dead));
        }

        [Test] public void 데드존_안은_등뒤라도_맞는다()
        {
            Assert.IsTrue(BossRangeLogic.InHitBand(10f, 9.5f, Reach, 1f, Dead));
        }

        [Test] public void 띠_경계와_판정이_일치한다()
        {
            // 표시(BandMin/Max)와 실판정(InHitBand)이 어긋나면 디버그 표시가 거짓말을 한다
            for (int i = 0; i <= 200; i++)
            {
                float target = 0f + i * 0.15f;
                bool inBand = target >= BossRangeLogic.BandMinX(10f, Reach, 1f, Dead)
                           && target <= BossRangeLogic.BandMaxX(10f, Reach, 1f, Dead);
                Assert.AreEqual(inBand, BossRangeLogic.InHitBand(10f, target, Reach, 1f, Dead), "target=" + target);
            }
        }

        [Test] public void 양방향_판정은_등뒤도_맞힌다()
        {
            Assert.IsTrue(BossRangeLogic.InHitBandBothSides(10f, 15f, Reach));
            Assert.IsTrue(BossRangeLogic.InHitBandBothSides(10f, 5f, Reach));
            Assert.IsFalse(BossRangeLogic.InHitBandBothSides(10f, 16.5f, Reach));
            Assert.IsFalse(BossRangeLogic.InHitBandBothSides(10f, 3.5f, Reach));
        }

        [Test] public void 세로제한_같은_높이면_맞는다()
        {
            Assert.IsTrue(BossRangeLogic.InHitBand(10f, 11f, Reach, 1f, Dead, 0f, 0f, 1.2f));
        }

        [Test] public void 세로제한_뛰어넘으면_빗나간다()
        {
            Assert.IsFalse(BossRangeLogic.InHitBand(10f, 11f, Reach, 1f, Dead, 0f, 1.5f, 1.2f));
            Assert.IsFalse(BossRangeLogic.InHitBand(10f, 11f, Reach, 1f, Dead, 0f, 2.25f, 1.2f));
        }

        [Test] public void 세로제한_경계는_포함한다()
        {
            Assert.IsTrue(BossRangeLogic.InHitBand(10f, 11f, Reach, 1f, Dead, 0f, 1.2f, 1.2f));
        }

        [Test] public void 세로제한_낮은_점프는_여전히_맞는다()
        {
            Assert.IsTrue(BossRangeLogic.InHitBand(10f, 11f, Reach, 1f, Dead, 0f, 0.5f, 1.2f));
        }

        [Test] public void 세로제한_아래층도_빗나간다()
        {
            Assert.IsFalse(BossRangeLogic.InHitBand(10f, 11f, Reach, 1f, Dead, 0f, -2f, 1.2f));
        }

        [Test] public void 세로제한_0이면_기존동작()
        {
            Assert.IsTrue(BossRangeLogic.InHitBand(10f, 11f, Reach, 1f, Dead, 0f, 99f, 0f));
        }

        [Test] public void 세로제한이어도_수평_밖이면_빗나간다()
        {
            Assert.IsFalse(BossRangeLogic.InHitBand(10f, 20f, Reach, 1f, Dead, 0f, 0f, 1.2f));
        }

        [Test] public void 시간창_판정()
        {
            Assert.IsFalse(BossRangeLogic.WindowOpen(0.5f, 0.62f, 0.82f));
            Assert.IsTrue(BossRangeLogic.WindowOpen(0.62f, 0.62f, 0.82f));
            Assert.IsTrue(BossRangeLogic.WindowOpen(0.7f, 0.62f, 0.82f));
            Assert.IsTrue(BossRangeLogic.WindowOpen(0.82f, 0.62f, 0.82f));
            Assert.IsFalse(BossRangeLogic.WindowOpen(0.9f, 0.62f, 0.82f));
        }

        [Test] public void 시간창까지_남은_진행률()
        {
            Assert.AreEqual(0.12f, BossRangeLogic.FracUntilWindow(0.5f, 0.62f), 0.0001f);
            Assert.Less(BossRangeLogic.FracUntilWindow(0.7f, 0.62f), 0f);
        }
    }
}
