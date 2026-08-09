using UnityEngine;
using UnityEngine.SceneManagement;

namespace NAN2026
{
    // RealPlayer(PersistentSingleton으로 DontDestroyOnLoad돼있는)에 부착.
    // 포탈로 새 씬이 로드되면, 그 씬에 배치된 ScenePlayerSpawnPoint 위치로 플레이어를 옮긴다.
    // 씬에 스폰 포인트가 없으면(예: 게임을 처음 시작하는 씬) 아무것도 안 하고 원래
    // 배치된 위치를 그대로 쓴다 — 그러니 스폰 포인트가 없는 씬에서도 안전하다.
    [RequireComponent(typeof(PersistentSingleton))]
    public sealed class PlayerScenePositioner : MonoBehaviour
    {
        private Rigidbody2D body;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single) return;

            ScenePlayerSpawnPoint spawn = FindFirstObjectByType<ScenePlayerSpawnPoint>();
            if (spawn != null)
            {
                transform.position = spawn.transform.position;
                if (body != null)
                {
                    body.linearVelocity = Vector2.zero;
                    body.SetRotation(0f);
                }
            }
            // 스폰 포인트가 없는 씬(예: 시작 씬)이어도 카메라 연결은 아래에서 계속 진행한다.

            // 새로 로드된 씬의 시네마틱 카메라(CinemachineCamera)들이 자기 씬에 원래 있던
            // 로컬 플레이어를 추적 대상으로 물고 있을 수 있는데, 이제 플레이어는 DontDestroyOnLoad
            // 싱글톤 하나뿐이라 그 오브젝트를 못 찾아 추적이 끊긴다. 이 씬의 모든 카메라를
            // 찾아서 Tracking Target(Follow)을 이 플레이어로 강제 재설정한다.
            Unity.Cinemachine.CinemachineCamera[] cams = FindObjectsByType<Unity.Cinemachine.CinemachineCamera>(FindObjectsSortMode.None);
            foreach (Unity.Cinemachine.CinemachineCamera cam in cams)
            {
                if (cam == null) continue;
                cam.Follow = transform;
            }
        }
    }
}
