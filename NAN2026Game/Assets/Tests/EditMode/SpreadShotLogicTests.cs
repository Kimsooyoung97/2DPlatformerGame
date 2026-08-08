using NUnit.Framework;
using NAN2026.Core;

namespace NAN2026.Tests
{
    public class SpreadShotLogicTests
    {
        [Test] public void 단발이면_기준각_그대로()
        {
            Assert.AreEqual(-30f, SpreadShotLogic.AngleDeg(0, 1, -30f, 70f), 0.0001f);
        }

        [Test] public void 다섯발은_기준각_중심으로_대칭이다()
        {
            Assert.AreEqual(-65f, SpreadShotLogic.AngleDeg(0, 5, -30f, 70f), 0.0001f);
            Assert.AreEqual(-47.5f, SpreadShotLogic.AngleDeg(1, 5, -30f, 70f), 0.0001f);
            Assert.AreEqual(-30f, SpreadShotLogic.AngleDeg(2, 5, -30f, 70f), 0.0001f);
            Assert.AreEqual(-12.5f, SpreadShotLogic.AngleDeg(3, 5, -30f, 70f), 0.0001f);
            Assert.AreEqual(5f, SpreadShotLogic.AngleDeg(4, 5, -30f, 70f), 0.0001f);
        }

        [Test] public void 가운데_탄은_항상_기준각이다()
        {
            Assert.AreEqual(-30f, SpreadShotLogic.AngleDeg(1, 3, -30f, 40f), 0.0001f);
            Assert.AreEqual(0f, SpreadShotLogic.AngleDeg(3, 7, 0f, 90f), 0.0001f);
        }

        [Test] public void 양_끝은_MinMax와_일치한다()
        {
            Assert.AreEqual(SpreadShotLogic.MinAngleDeg(-30f, 70f), SpreadShotLogic.AngleDeg(0, 5, -30f, 70f), 0.0001f);
            Assert.AreEqual(SpreadShotLogic.MaxAngleDeg(-30f, 70f), SpreadShotLogic.AngleDeg(4, 5, -30f, 70f), 0.0001f);
        }

        [Test] public void 각도는_단조증가한다()
        {
            float prev = -999f;
            for (int i = 0; i < 5; i++)
            {
                float a = SpreadShotLogic.AngleDeg(i, 5, -30f, 70f);
                Assert.Greater(a, prev);
                prev = a;
            }
        }

        [Test] public void 범위밖_인덱스는_양끝으로_고정된다()
        {
            Assert.AreEqual(SpreadShotLogic.AngleDeg(0, 5, -30f, 70f), SpreadShotLogic.AngleDeg(-3, 5, -30f, 70f), 0.0001f);
            Assert.AreEqual(SpreadShotLogic.AngleDeg(4, 5, -30f, 70f), SpreadShotLogic.AngleDeg(99, 5, -30f, 70f), 0.0001f);
        }

        [Test] public void 확산0이면_전부_같은_각도()
        {
            for (int i = 0; i < 5; i++)
                Assert.AreEqual(-30f, SpreadShotLogic.AngleDeg(i, 5, -30f, 0f), 0.0001f);
        }

        [Test] public void 발사지연은_인덱스_비례()
        {
            Assert.AreEqual(0f, SpreadShotLogic.FireDelay(0, 0.05f), 0.0001f);
            Assert.AreEqual(0.2f, SpreadShotLogic.FireDelay(4, 0.05f), 0.0001f);
        }
    }
}
