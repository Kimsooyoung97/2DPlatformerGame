/// <summary>
/// 특정 적(예: 보스)이 기본 근접 공격 대신 자기만의 공격 패턴(돌진, 투사체 등)을
/// 갖고 있을 때 EnemyAI가 대신 위임하기 위한 훅. 같은 GameObject에 이 인터페이스를
/// 구현한 컴포넌트가 있으면 EnemyAI는 그 컴포넌트가 바쁜 동안 이동/공격을 스스로 하지 않는다.
/// </summary>
public interface IEnemyAttackOverride
{
    /// 지금 이 컴포넌트가 공격 패턴(코루틴 등)을 실행 중인지. true인 동안 EnemyAI는 개입하지 않는다.
    bool IsBusy { get; }

    /// EnemyAI가 공격을 시도하려는 시점(추적 중 또는 공격 사거리 진입 시)에 호출된다.
    /// true를 반환하면 이 컴포넌트가 패턴을 시작했다는 뜻이며, EnemyAI는 자기 기본 공격을 하지 않는다.
    /// false를 반환하면 EnemyAI가 평소처럼(근접 공격 등) 행동한다.
    bool TryStartAttack(UnityEngine.Transform player);
}
