using NUnit.Framework;
using NAN2026.Core;

namespace NAN2026.Tests
{
    public class AttackDamageLogicTests
    {
        [Test] public void Slash_UsesBasicDamage()
        {
            Assert.AreEqual(1, AttackDamageLogic.DamageForAttack("Slash", 1, 3));
        }

        [Test] public void Combo2_UsesPoweredDamage()
        {
            Assert.AreEqual(3, AttackDamageLogic.DamageForAttack("Combo2", 1, 3));
        }

        [Test] public void Combo3_UsesPoweredDamage()
        {
            Assert.AreEqual(3, AttackDamageLogic.DamageForAttack("Combo3", 1, 3));
        }

        [Test] public void Roll_DealsNoDamage()
        {
            Assert.AreEqual(0, AttackDamageLogic.DamageForAttack("Roll", 1, 3));
        }

        [Test] public void UnknownAttack_DealsNoDamage()
        {
            Assert.AreEqual(0, AttackDamageLogic.DamageForAttack("Unknown", 1, 3));
        }

        [Test] public void ComboV1_UsesComboVDamage()
        {
            Assert.AreEqual(3, AttackDamageLogic.DamageForComboV("ComboV1", 3));
        }

        [Test] public void ComboV2_UsesComboVDamage()
        {
            Assert.AreEqual(3, AttackDamageLogic.DamageForComboV("ComboV2", 3));
        }

        [Test] public void Slash_ComboVDamage_IsZero()
        {
            Assert.AreEqual(0, AttackDamageLogic.DamageForComboV("Slash", 3));
        }

        [Test] public void Combo2_IsPiercingSkill()
        {
            Assert.IsTrue(AttackDamageLogic.IsPiercingSkill("Combo2"));
        }

        [Test] public void Slash_IsNotPiercingSkill()
        {
            Assert.IsFalse(AttackDamageLogic.IsPiercingSkill("Slash"));
        }
    }
}
