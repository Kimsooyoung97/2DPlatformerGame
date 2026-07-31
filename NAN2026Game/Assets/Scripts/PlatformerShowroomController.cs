using UnityEngine;
using UnityEngine.InputSystem;

namespace NAN2026.Showroom
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class PlatformerShowroomController : MonoBehaviour
    {
        [SerializeField] private Vector3[] sectionPositions;
        [SerializeField] private float[] sectionSizes;
        [SerializeField] private string[] sectionNames;
        [SerializeField] private float panSpeed = 8f;
        [SerializeField] private float minZoom = 2f;
        [SerializeField] private float maxZoom = 30f;

        private Camera showroomCamera;
        private int currentSection;
        private string hoveredAsset = string.Empty;
        private bool helpVisible = true;

        public void Configure(Vector3[] positions, float[] sizes, string[] names)
        {
            sectionPositions = positions;
            sectionSizes = sizes;
            sectionNames = names;
        }

        private void Awake()
        {
            showroomCamera = GetComponent<Camera>();
            GoToSection(0);
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;

            if (keyboard != null)
            {
                int sectionCount = sectionPositions != null ? sectionPositions.Length : 0;
                for (int i = 0; i < sectionCount && i < 9; i++)
                {
                    Key digit = (Key)((int)Key.Digit1 + i);
                    if (keyboard[digit].wasPressedThisFrame)
                        GoToSection(i);
                }

                if (keyboard.rKey.wasPressedThisFrame) GoToSection(currentSection);
                if (keyboard.hKey.wasPressedThisFrame) helpVisible = !helpVisible;

                Vector2 direction = Vector2.zero;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) direction.x -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) direction.x += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) direction.y -= 1f;
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) direction.y += 1f;

                if (direction.sqrMagnitude > 1f) direction.Normalize();
                float speed = panSpeed * Mathf.Max(0.5f, showroomCamera.orthographicSize / 6f);
                transform.position += (Vector3)(direction * speed * Time.unscaledDeltaTime);
            }

            if (mouse != null)
            {
                float scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    float zoomFactor = Mathf.Max(0.5f, showroomCamera.orthographicSize * 0.08f);
                    showroomCamera.orthographicSize = Mathf.Clamp(
                        showroomCamera.orthographicSize - Mathf.Sign(scroll) * zoomFactor,
                        minZoom,
                        maxZoom);
                }

                Vector2 screen = mouse.position.ReadValue();
                Vector3 world = showroomCamera.ScreenToWorldPoint(
                    new Vector3(screen.x, screen.y, -transform.position.z));
                Collider2D hit = Physics2D.OverlapPoint(world);
                hoveredAsset = hit != null ? hit.gameObject.name : string.Empty;
            }
        }

        public void GoToSection(int index)
        {
            if (sectionPositions == null || sectionPositions.Length == 0)
                return;

            currentSection = Mathf.Clamp(index, 0, sectionPositions.Length - 1);
            transform.position = sectionPositions[currentSection];

            if (showroomCamera == null)
                showroomCamera = GetComponent<Camera>();

            if (sectionSizes != null && currentSection < sectionSizes.Length)
                showroomCamera.orthographicSize = sectionSizes[currentSection];
        }

        private void OnGUI()
        {
            const float margin = 12f;
            const float buttonWidth = 142f;
            const float buttonHeight = 30f;

            int sectionCount = sectionNames != null && sectionNames.Length > 0
                ? sectionNames.Length
                : (sectionPositions != null ? sectionPositions.Length : 0);

            if (sectionCount == 0)
                return;

            float panelWidth = Mathf.Min(Screen.width - margin * 2f, sectionCount * buttonWidth + 16f);
            GUI.Box(new Rect(margin, margin, panelWidth, helpVisible ? 112f : 52f), GUIContent.none);

            for (int i = 0; i < sectionCount; i++)
            {
                string label = sectionNames != null && i < sectionNames.Length
                    ? (i + 1) + "  " + sectionNames[i]
                    : "Section " + (i + 1);

                if (GUI.Button(new Rect(margin + 8f + i * buttonWidth, margin + 8f, buttonWidth - 6f, buttonHeight), label))
                    GoToSection(i);
            }

            if (helpVisible)
            {
                GUI.Label(new Rect(margin + 10f, margin + 47f, panelWidth - 20f, 22f),
                    "Move: WASD / Arrow Keys     Zoom: Mouse Wheel     Reset: R     Help: H");
                GUI.Label(new Rect(margin + 10f, margin + 69f, panelWidth - 20f, 22f),
                    "Hover an item to see its Unity asset or sprite name.");
            }

            if (!string.IsNullOrEmpty(hoveredAsset))
            {
                GUI.Box(new Rect(margin, Screen.height - 48f, Mathf.Min(720f, Screen.width - 24f), 36f),
                    "Asset: " + hoveredAsset);
            }
        }
    }
}
