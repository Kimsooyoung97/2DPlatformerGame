using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace NAN2026
{
    // ↑키(또는 W)로 활성화하는 포탈. 플레이어가 범위 안에서 위 입력 시 지정 씬 로드.
    // 팀 Portal(닿으면 즉시 이동)과 달리 의도적 입력을 요구한다.
    public class PortalUpKey : MonoBehaviour
    {
        [SerializeField] private string nextSceneName;
        private bool playerInside;
        private bool teleporting;

        // 순수 판정 (테스트 대상)
        public static bool ShouldTeleport(bool inside, bool upPressed, bool alreadyTeleporting)
        {
            return inside && upPressed && !alreadyTeleporting;
        }

        private static bool IsPlayer(Collider2D c)
        {
            return c != null && c.GetComponent<PlayerController2D>() != null;
        }

        private void OnTriggerEnter2D(Collider2D c)
        {
            if (IsPlayer(c)) playerInside = true;
        }

        private void OnTriggerExit2D(Collider2D c)
        {
            if (IsPlayer(c)) playerInside = false;
        }

        private void Update()
        {
            var kb = Keyboard.current;
            bool up = kb != null && (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame);
            if (!ShouldTeleport(playerInside, up, teleporting)) return;
            teleporting = true;
            SceneManager.LoadScene(nextSceneName);
        }
    }
}
