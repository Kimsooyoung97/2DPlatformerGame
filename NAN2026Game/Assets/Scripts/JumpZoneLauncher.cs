using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// JumpZoneN 오브젝트에 부착. 플레이어가 안에 있을 때 점프(방향키 위)를 누르면
/// 이름이 매칭되는 ArriveZoneN으로 포물선 궤적의 슈퍼점프를 시킨다.
/// 이름의 숫자로 자동 매칭하므로 JumpZone/ArriveZone 쌍을 몇 개를 두든 그대로 동작한다.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public sealed class JumpZoneLauncher : MonoBehaviour
{
    [SerializeField] private JumpZoneConfig config;
    [Tooltip("비워두면 오브젝트 이름의 'JumpZone'을 'ArriveZone'으로 바꿔 자동으로 찾는다 (예: JumpZone1 → ArriveZone1)")]
    [SerializeField] private Transform arriveZone;

    private bool playerInside;
    private GameObject playerObject;

    private void Awake()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;

        if (arriveZone == null)
        {
            string targetName = gameObject.name.Replace("JumpZone", "ArriveZone");
            GameObject found = GameObject.Find(targetName);
            if (found != null) arriveZone = found.transform;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerInside = true;
        playerObject = other.gameObject;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerInside = false;
        playerObject = null;
    }

    private void Update()
    {
        if (!playerInside || arriveZone == null || playerObject == null || config == null) return;

        Keyboard kb = Keyboard.current;
        if (kb != null && kb.upArrowKey.wasPressedThisFrame)
        {
            PlayerController2D pc = playerObject.GetComponent<PlayerController2D>();
            if (pc != null) pc.LaunchTo(arriveZone.position, config.flightDuration);
        }
    }
}
