using UnityEngine;
using UnityEngine.SceneManagement; // 씬 관리를 위해 필수

public class Portal : MonoBehaviour
{
    [Header("Scene Settings")]
    [SerializeField] private string nextSceneName; // 이동할 다음 씬 이름

    private bool isTeleporting = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 닿은 오브젝트가 플레이어이고 아직 씬 전환 중이 아니라면
        if (collision.CompareTag("Player") && !isTeleporting)
        {
            isTeleporting = true;
            LoadNextScene();
        }
    }

    private void LoadNextScene()
    {
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            Debug.LogWarning("다음 씬 이름(Next Scene Name)이 설정되지 않았습니다!");
        }
    }
}