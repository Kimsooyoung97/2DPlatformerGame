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
        public Font customFont;
        private PlayerHealth playerHealth;
        private bool isOpen;
        private int selectedIndex;

        // 다른 씬으로 이동해야 할 때, 씬 로드가 끝난 뒤 어디로 옮길지 기억해두는 값.
        private CheckpointRecord pendingTravel;

        // Open()을 부른 그 프레임엔 입력 처리를 건너뛴다. CheckpointTrigger가 같은 Enter
        // 입력으로 Open()을 호출하는데, 그 직후 같은 프레임에 이 Update()도 돌면서 같은
        // wasPressedThisFrame을 또 읽어 selectedIndex=0(시작 지점)으로 즉시 확정해버리는
        // 버그가 실측으로 확인됐다 — 한 번의 엔터가 열기+확정을 동시에 처리해버림.
        private int openedFrame = -1;
        // 확정(TravelTo)으로 Close()가 호출된 그 프레임엔 Open()을 막는다. CheckpointTrigger가
        // 같은 Enter 입력을 같은 프레임에 또 읽어서, 방금 닫힌 메뉴를 그 자리에서 다시 열어버리는
        // 반대 방향 레이스 컨디션이 실측으로 확인됐다(이동은 되는데 UI가 안 닫히는 증상).
        private int closedFrame = -1;

        // FAIL.md #27: PlayerController2D.InputLocked는 참조 카운트 없는 전역 static이라
        // 여러 시스템이 동시에 쓰면 나중에 false로 푸는 쪽이 이긴다. 최소 안전장치로,
        // "내가 잠갔을 때만 내가 푼다" — 이미 다른 시스템이 잠가둔 상태였다면 손대지 않는다.
        private bool weLockedInput;

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
            if (isOpen) Time.timeScale = 1f; // 열린 채로 파괴되는 경우(거의 없지만) timeScale 0에 갇히는 것 방지
            if (weLockedInput) { PlayerController2D.InputLocked = false; weLockedInput = false; }
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
            if (Time.frameCount == closedFrame) return; // 방금 닫힌 그 프레임엔 다시 안 연다
            if (health.Checkpoints.Count == 0) return;

            playerHealth = health;
            selectedIndex = 0;
            isOpen = true;
            openedFrame = Time.frameCount;
            Time.timeScale = 0f; // 메뉴 여는 동안 게임 정지 — 고르는 사이에 몬스터한테 맞거나 하지 않게
            if (!PlayerController2D.InputLocked)
            {
                PlayerController2D.InputLocked = true; // 좌우 방향키 등으로 캐릭터가 방향 바뀌는 것 방지
                weLockedInput = true;
            }
        }

        public void Close()
        {
            isOpen = false;
            playerHealth = null;
            closedFrame = Time.frameCount;
            Time.timeScale = 1f; // 복구하지 않으면 메뉴 닫은 뒤 게임이 계속 정지 상태로 남는다
            if (weLockedInput)
            {
                PlayerController2D.InputLocked = false;
                weLockedInput = false;
            }
        }

        private void Update()
        {
            if (!isOpen || playerHealth == null) return;
            if (Time.frameCount == openedFrame) return; // 방금 연 그 프레임의 입력은 무시(열기+확정 동시발동 방지)

            var list = playerHealth.Checkpoints;
            if (list.Count == 0) { Close(); return; }

            Keyboard kb = Keyboard.current;
            if (kb == null) return;

            // timeScale=0 이어도 Keyboard 폴링 자체는 스케일과 무관하게 계속 갱신되므로 입력 감지엔 문제 없다.
            if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame)
                selectedIndex = (selectedIndex - 1 + list.Count) % list.Count;
            else if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame)
                selectedIndex = (selectedIndex + 1) % list.Count;
            else if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
                TravelTo(list[selectedIndex]);
            else if (kb.xKey.wasPressedThisFrame)
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

            PlayerHealth health = FindAnyObjectByType<PlayerHealth>();
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
            GUIStyle boxStyle = new GUIStyle(GUI.skin.box) { fontSize = 32, alignment = TextAnchor.MiddleLeft, font = customFont };
            GUIStyle selectedStyle = new GUIStyle(boxStyle);
            selectedStyle.normal.textColor = Color.yellow;
            GUIStyle titleStyle = new GUIStyle(GUI.skin.box) { fontSize = 28, alignment = TextAnchor.MiddleCenter, font = customFont };

            float itemWidth = 680f;
            float itemHeight = 60f;
            float titleHeight = 68f;
            float totalHeight = titleHeight + list.Count * itemHeight;
            float startX = Screen.width * 0.5f - itemWidth * 0.5f;
            float startY = Screen.height * 0.5f - totalHeight * 0.5f;

            GUI.Box(new Rect(startX, startY, itemWidth, titleHeight),
                "세이브포인트 이동  (↑↓ 선택 · Enter 확정 · X 취소)", titleStyle);

            for (int i = 0; i < list.Count; i++)
            {
                Rect r = new Rect(startX, startY + titleHeight + i * itemHeight, itemWidth, itemHeight - 2f);
                string text = (i == selectedIndex ? "▶ " : "   ") + list[i].label;
                GUI.Box(r, text, i == selectedIndex ? selectedStyle : boxStyle);
            }
        }
    }
}
