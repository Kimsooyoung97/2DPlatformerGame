using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement; // 씬 재시작을 위해 필요

public class Mine : MonoBehaviour
{
    [Header("Restart Delay")]
    [SerializeField] private float restartDelay = 1.5f; // 사망 애니메이션을 보여준 뒤 씬을 다시 열 때까지의 시간

    private bool isTriggered = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 닿은 대상이 플레이어이고, 아직 작동하지 않았다면
        if (collision.CompareTag("Player") && !isTriggered)
        {
            isTriggered = true;

            // 1. 플레이어 스크립트의 Die() 호출
            PlayerController player = collision.GetComponent<PlayerController>();
            if (player != null)
            {
                player.Die();
            }

            // 2. 일정 시간 지연 후 씬 재시작 코루틴 실행
            StartCoroutine(RestartSceneAfterDelay());
        }
    }

    private IEnumerator RestartSceneAfterDelay()
    {
        // 지정한 시간(예: 1.5초) 동안 지뢰 사망 모션을 보여줌
        yield return new WaitForSeconds(restartDelay);

        // 현재 활성화된 씬을 다시 로드하여 처음으로 돌아감
        Scene activeScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(activeScene.name);
    }
}