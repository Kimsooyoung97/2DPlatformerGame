using NUnit.Framework;
using NAN2026.Core;

namespace NAN2026.Tests
{
    public class SkillLogicTests
    {
        [Test] public void OffsetX_First_IsStart() => Assert.AreEqual(1.2f, SkillLogic.OffsetX(0, 1.2f, 1.3f), 1e-4f);
        [Test] public void OffsetX_Third_Accumulates() => Assert.AreEqual(3.8f, SkillLogic.OffsetX(2, 1.2f, 1.3f), 1e-4f);
        [Test] public void FrameTime_Frame4_At10fps() => Assert.AreEqual(0.3f, SkillLogic.FrameTime(4, 10f), 1e-4f);
        [Test] public void FrameTime_Frame1_Zero() => Assert.AreEqual(0f, SkillLogic.FrameTime(1, 10f));
        [Test] public void FrameTime_ClampsBelow1() => Assert.AreEqual(0f, SkillLogic.FrameTime(0, 10f));
    }
}
