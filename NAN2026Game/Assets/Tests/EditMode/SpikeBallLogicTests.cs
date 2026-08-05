using NUnit.Framework;
using NAN2026.Core;

namespace NAN2026.Tests
{
    public class SpikeBallLogicTests
    {
        const float EPS = 0.0001f;

        [Test] public void Phase_Far_Idle()
        { Assert.AreEqual(0, SpikeBallLogic.Phase(10f, 4.5f, 2f, 1.1f)); }

        [Test] public void Phase_WarnBand_Blink()
        { Assert.AreEqual(1, SpikeBallLogic.Phase(8f, 4.5f, 2f, 1.1f)); }

        [Test] public void Phase_Close_Launch()
        { Assert.AreEqual(2, SpikeBallLogic.Phase(4.0f, 4.5f, 2f, 1.1f)); }

        [Test] public void BlinkAlpha_InRange()
        {
            for (float t = 0f; t < 1f; t += 0.05f)
            {
                float a = SpikeBallLogic.BlinkAlpha(t, 5f);
                Assert.IsTrue(a >= 0.35f - EPS && a <= 1f + EPS);
            }
        }

        [Test] public void LaunchDir_Normalized_TowardTarget()
        {
            float dx, dy;
            SpikeBallLogic.LaunchDir(0f, 10f, 3f, 6f, out dx, out dy);
            Assert.IsTrue(System.Math.Abs(dx * dx + dy * dy - 1f) < 0.001f);
            Assert.IsTrue(dx > 0f && dy < 0f);
        }
    }
}
