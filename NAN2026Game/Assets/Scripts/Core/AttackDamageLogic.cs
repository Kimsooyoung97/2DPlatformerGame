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
    }
}
