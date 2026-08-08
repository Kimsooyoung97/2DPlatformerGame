namespace NAN2026
{
    /// 플레이어 공격(Z 근접 · X 검기)이 대미지를 넣을 수 있는 대상.
    /// 디스패처(EffectProjectile / SlashProjectile)가 이 인터페이스 하나만 보면 되도록 통일한다.
    /// FAIL#24: 신규 적을 만들 때마다 디스패처에 분기를 추가하는 방식은 누락되면 침묵 무력화된다.
    public interface IPlayerDamageable
    {
        void TakeDamage(int amount);
    }
}
