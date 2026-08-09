using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace NAN2026
{
    /// <summary>
    /// 세이브포인트에서 Enter키로 여는 이동 메뉴 — NPC 대화창처럼 지금까지 저장된 세이브포인트
    /// 목록을 보여주고, 위/아래로 고르고 Enter로 확정하면 그 지점(다른 씬 포함)으로 이동한다.
    ///
    /// uGUI Button이 아니라 OnGUI로 직접 그린다 — 이 프로젝트에서 EventSystem이 씬에 없어서
    /// 버튼이 전혀 안 눌리던 사고가 있었다(FAIL.md #17과 동일 계열). 키보드 입력만으로
    /// 완결되는 이 메뉴는 그 리스크를 아예 피해간다.
    ///
    /// RealPlayer(PersistentSingleton)에 부착해서 DontDestroyOnLoad로 씬을 넘어가며 유지한다 —
    /// 다른 씬의 세이브포인트로 이동할 때 SceneManager.LoadScene 이후에도 "어디로 갈지"
    /// 기억하고 있어야 하기 때문이다.
    /// </summary>
    public sealed class CheckpointTravelMenu : MonoBehaviour
    {
        public static CheckpointTravelMenu Instance { get; private set; }

        private PlayerHealth playerHealth;
        private bool isOpen;
        private int selectedIndex;

        // 다른 씬으로 이동해야 할 때, 씬 로드가 끝난 뒤 어디로 옮길지 기억해두는 값.
        private CheckpointRecord pendingTravel;

        private void Awake()
        {
            // PersistentSingleton과 마찬가지로, 새로 로드된 씬에 이 컴포넌트를 가진 중복
            // 오브젝트가 있으면(있을 리 없지만 방어적으로) 기존 걸 유지하고 새 걸 버린다.
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        public void Open(PlayerHealth health)
        {
            if (isOpen || health == null) return;
            if (health.Checkpoints.Count == 0) return;

            playerHealth = health;
            selectedIndex = 0;
            isOpen = true;
        }

        public void Close()
        {
            isOpen = false;
            playerHealth = null;
        }

        private void Update()
        {
            if (!isOpen || playerHealth == null) return;

            var list = playerHealth.Checkpoints;
            if (list.Count == 0) { Close(); return; }

            Keyboard kb = Keyboard.current;
            if (kb == null) return;

            if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame)
                selectedIndex = (selectedIndex - 1 + list.Count) % list.Count;
            else if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame)
                selectedIndex = (selectedIndex + 1) % list.Count;
            else if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
                TravelTo(list[selectedIndex]);
            else if (kb.escapeKey.wasPressedThisFrame)
                Close();
        }

        private void TravelTo(CheckpointRecord record)
        {
            string currentScene = SceneManager.GetActiveScene().name;
            PlayerHealth targetHealth = playerHealth;
            Close();

            if (record.sceneName == currentScene)
            {
                MovePlayerTo(targetHealth, record.position);
                return;
            }

            pendingTravel = record;
            SceneManager.LoadScene(record.sceneName);
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (pendingTravel == null) return;
            if (scene.name != pendingTravel.sceneName) return;

            PlayerHealth health = FindFirstObjectByType<PlayerHealth>();
            MovePlayerTo(health, pendingTravel.position);
            pendingTravel = null;
        }

        private void MovePlayerTo(PlayerHealth health, Vector3 position)
        {
            if (health == null) return;

            Rigidbody2D body = health.GetComponent<Rigidbody2D>();
            if (body != null)
            {
                // transform.position만 바꾸면 Rigidbody2D가 다음 물리 스텝(FixedUpdate)에서
                // 자기가 내부적으로 추적하던 예전 위치로 되돌려놓는다(보간/인터폴레이션 때문).
                // body.position까지 같이 맞춰줘야 실제로 그 자리에 고정된다 — 실측으로 확인한 버그.
                body.position = position;
                body.linearVelocity = Vector2.zero;
                body.SetRotation(0f);
            }
            health.transform.position = position;
        }

        private void OnGUI()
        {
            if (!isOpen || playerHealth == null) return;

            var list = playerHealth.Checkpoints;
            GUIStyle boxStyle = new GUIStyle(GUI.skin.box) { fontSize = 16, alignment = TextAnchor.MiddleLeft };
            GUIStyle selectedStyle = new GUIStyle(boxStyle);
            selectedStyle.normal.textColor = Color.yellow;
            GUIStyle titleStyle = new GUIStyle(GUI.skin.box) { fontSize = 14, alignment = TextAnchor.MiddleCenter };

            float itemWidth = 340f;
            float itemHeight = 30f;
            float titleHeight = 34f;
            float totalHeight = titleHeight + list.Count * itemHeight;
            float startX = Screen.width * 0.5f - itemWidth * 0.5f;
            float startY = Screen.height * 0.5f - totalHeight * 0.5f;

            GUI.Box(new Rect(startX, startY, itemWidth, titleHeight),
                "세이브포인트 이동  (↑↓ 선택 · Enter 확정 · Esc 취소)", titleStyle);

            for (int i = 0; i < list.Count; i++)
            {
                Rect r = new Rect(startX, startY + titleHeight + i * itemHeight, itemWidth, itemHeight - 2f);
                string text = (i == selectedIndex ? "▶ " : "   ") + list[i].label;
                GUI.Box(r, text, i == selectedIndex ? selectedStyle : boxStyle);
            }
        }
    }
}
