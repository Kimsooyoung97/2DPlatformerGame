using NUnit.Framework;
using NAN2026.Core;

namespace NAN2026.Tests
{
    public class HitFeedbackLogicTests
    {
        [Test] public void 왼쪽에서_맞으면_오른쪽으로_밀린다()
        {
            Assert.AreEqual(1f, HitFeedbackLogic.KnockbackSign(true, 10f, 8f, 1f));
        }

        [Test] public void 오른쪽에서_맞으면_왼쪽으로_밀린다()
        {
            Assert.AreEqual(-1f, HitFeedbackLogic.KnockbackSign(true, 10f, 13f, 1f));
        }

        [Test] public void 가해자를_모르면_바라보는_반대로()
        {
            Assert.AreEqual(-1f, HitFeedbackLogic.KnockbackSign(false, 10f, 0f, 1f));
            Assert.AreEqual(1f, HitFeedbackLogic.KnockbackSign(false, 10f, 0f, -1f));
        }

        [Test] public void 완전히_겹치면_바라보는_반대로()
        {
            Assert.AreEqual(-1f, HitFeedbackLogic.KnockbackSign(true, 10f, 10f, 1f));
        }

        [Test] public void 넉백_총이동량은_거리와_같다()
        {
            float dist = 0.25f, dur = 0.12f, dt = 0.002f, sum = 0f;
            for (float e = 0f; e < dur; e += dt) sum += HitFeedbackLogic.KnockbackStep(dist, e, dur, dt);
            Assert.AreEqual(dist, sum, 0.02f);
        }

        [Test] public void 넉백은_뒤로_갈수록_느려진다()
        {
            float a = HitFeedbackLogic.KnockbackStep(0.25f, 0f, 0.12f, 0.01f);
            float b = HitFeedbackLogic.KnockbackStep(0.25f, 0.06f, 0.12f, 0.01f);
            float c = HitFeedbackLogic.KnockbackStep(0.25f, 0.119f, 0.12f, 0.01f);
            Assert.Greater(a, b);
            Assert.Greater(b, c);
        }

        [Test] public void 넉백_종료후에는_0()
        {
            Assert.AreEqual(0f, HitFeedbackLogic.KnockbackStep(0.25f, 0.2f, 0.12f, 0.01f), 0.0001f);
            Assert.AreEqual(0f, HitFeedbackLogic.KnockbackStep(0.25f, 0f, 0f, 0.01f), 0.0001f);
        }

        [Test] public void 히트스톱_종료판정()
        {
            Assert.IsTrue(HitFeedbackLogic.HitStopFinished(10f, 0f));
            Assert.IsFalse(HitFeedbackLogic.HitStopFinished(9.9f, 10f));
            Assert.IsTrue(HitFeedbackLogic.HitStopFinished(10f, 10f));
        }

        [Test] public void 히트스톱은_무적의_25퍼센트를_넘지_않는다()
        {
            Assert.AreEqual(0.1125f, HitFeedbackLogic.ClampHitStop(0.5f, 0.45f), 0.0001f);
            Assert.AreEqual(0.06f, HitFeedbackLogic.ClampHitStop(0.06f, 0.45f), 0.0001f);
            Assert.AreEqual(0f, HitFeedbackLogic.ClampHitStop(-1f, 0.45f), 0.0001f);
        }
    }
}
