using NUnit.Framework;
using NAN2026.Core;

namespace NAN2026.Tests
{
    public class StatueLogicTests
    {
        [Test] public void Dormant_FarPlayer_Stays() => Assert.AreEqual(StatueLogic.Dormant, StatueLogic.Next(StatueLogic.Dormant, 10f, 5f, 2f, false));
        [Test] public void Dormant_NearPlayer_Awakens() => Assert.AreEqual(StatueLogic.Awakening, StatueLogic.Next(StatueLogic.Dormant, 4f, 5f, 2f, false));
        [Test] public void Awakening_TimerDone_Idle() => Assert.AreEqual(StatueLogic.Idle, StatueLogic.Next(StatueLogic.Awakening, 4f, 5f, 2f, true));
        [Test] public void Idle_TimerDone_Far_Chase() => Assert.AreEqual(StatueLogic.Chase, StatueLogic.Next(StatueLogic.Idle, 4f, 5f, 2f, true));
        [Test] public void Chase_InRange_Attack() => Assert.AreEqual(StatueLogic.Attack, StatueLogic.Next(StatueLogic.Chase, 1.5f, 5f, 2f, false));
        [Test] public void Attack_Done_Cooldown() => Assert.AreEqual(StatueLogic.Cooldown, StatueLogic.Next(StatueLogic.Attack, 1.5f, 5f, 2f, true));
        [Test] public void Cooldown_Done_Chase() => Assert.AreEqual(StatueLogic.Chase, StatueLogic.Next(StatueLogic.Cooldown, 3f, 5f, 2f, true));
        [Test] public void Hitbox_WindowOnly() { Assert.IsTrue(StatueLogic.HitboxOpen(0.5f, 0.4f, 0.6f)); Assert.IsFalse(StatueLogic.HitboxOpen(0.3f, 0.4f, 0.6f)); }
        [Test] public void FaceLeft_NegativeDx() => Assert.IsTrue(StatueLogic.FaceLeft(-1f));
    }
}
