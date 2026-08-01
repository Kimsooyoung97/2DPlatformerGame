using UnityEngine;

/// <summary>
/// 플레이어에게 패링/반사 스킬이 생기면 이 인터페이스를 구현해서
/// SpikeProjectile이 자동으로 반사 판정을 물어보도록 합니다.
/// 아직 플레이어 쪽에 패링 스킬이 없다면 구현체가 없어도 정상 동작합니다
/// (그 경우 투사체는 그냥 플레이어에게 데미지를 줍니다).
/// </summary>
public interface IParryReflector
{
    /// <summary>
    /// 지금 이 순간 패링(반사)이 가능한 타이밍인지 여부.
    /// true를 반환하면 투사체가 반사되어 보스에게 되돌아갑니다.
    /// </summary>
    bool TryParry(GameObject attacker);
}
