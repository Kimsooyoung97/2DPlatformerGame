namespace NAN2026.Core
{
    /// 순수 로직: UnityEngine 비의존. EditMode 테스트 대상.
    public static class AttackDamageLogic
    {
        /// 공격 이름에 따라 사용할 데미지 값을 고른다. 알 수 없는 이름이면 0(무피해).
        public static int DamageForAttack(string attackName, int basicDamage, int poweredDamage)
        {
            if (attackName == "Slash") return basicDamage;
            if (attackName == "Combo2" || attackName == "Combo3") return poweredDamage;
            return 0;
        }

        /// Z키 2단 콤보(ComboV1/ComboV2) 전용 데미지. 두 타 모두 같은 고정 데미지를 쓴다.
        public static int DamageForComboV(string attackName, int comboVDamage)
        {
            if (attackName == "ComboV1" || attackName == "ComboV2") return comboVDamage;
            return 0;
        }

        /// 스킬 투사체가 적을 맞힌 뒤에도 계속 날아가야 하는지(관통) 여부.
        /// Skill1(=Slash 애니메이션)은 단일 타격, Skill2(=Combo2 애니메이션)는 관통.
        public static bool IsPiercingSkill(string attackName)
        {
            return attackName == "Combo2";
        }
    }
}
