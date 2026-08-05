using UnityEngine;

/// <summary>
/// JumpZone → 대응하는 ArriveZone으로의 슈퍼점프(포물선 발사) 설정.
/// </summary>
[CreateAssetMenu(fileName = "JumpZoneConfig", menuName = "NAN2026/Jump Zone Config")]
public sealed class JumpZoneConfig : ScriptableObject
{
    [Tooltip("발사 시작부터 ArriveZone 도착까지 걸리는 시간(초)")]
    public float flightDuration = 1f;
}
