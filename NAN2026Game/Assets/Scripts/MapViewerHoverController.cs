using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem;

namespace NAN2026.Showroom
{
    /// <summary>
    /// Camera brain for the biome scenes.
    /// Free-look mode: pan with WASD / arrows, jump between sections with number keys.
    /// Follow mode (a target is assigned): the camera trails the player and the manual pan
    /// keys are released so they belong to the character.
    /// In both modes the mouse hover readout names the exact tile / prop / background asset.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class MapViewerHoverController : MonoBehaviour
    {
        [SerializeField] private Vector3[] sectionPositions;
        [SerializeField] private float[] sectionSizes;
        [SerializeField] private string[] sectionNames;
        [SerializeField] private float panSpeed = 10f;
        [SerializeField] private float minZoom = 2f;
        [SerializeField] private float maxZoom = 60f;

        [Header("Follow")]
        [SerializeField] private Transform followTarget;
        [SerializeField] private Vector2 followOffset = new Vector2(1.6f, 1.4f);
        [SerializeField] private float followSmooth = 0.15f;

        private Camera cam;
        private Tilemap[] tilemaps;
        private SpriteRenderer[] propRenderers;
        private SpriteRenderer[] backgroundRenderers;
        private Vector3 followVelocity;
        private int currentSection;
        private string hoverText = string.Empty;
        private bool helpVisible = true;

        public bool IsFollowing { get { return followTarget != null; } }

        public void Configure(Vector3[] positions, float[] sizes, string[] names)
        {
            sectionPositions = positions;
            sectionSizes = sizes;
            sectionNames = names;
        }

        public void SetFollowTarget(Transform target, float orthographicSize)
        {
            followTarget = target;
            if (cam == null)
                cam = GetComponent<Camera>();
            cam.orthographicSize = orthographicSize;

            if (target != null)
            {
                transform.position = new Vector3(
                    target.position.x + followOffset.x,
                    target.position.y + followOffset.y,
                    transform.position.z);
            }
        }

        private void Awake()
        {
            cam = GetComponent<Camera>();
            RefreshSceneObjects();

            if (followTarget == null)
                GoToSection(0);
        }

        private void RefreshSceneObjects()
        {
            tilemaps = FindObjectsByType<Tilemap>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            SpriteRenderer[] all = FindObjectsByType<SpriteRenderer>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            List<SpriteRenderer> props = new List<SpriteRenderer>();
            List<SpriteRenderer> backgrounds = new List<SpriteRenderer>();

            foreach (SpriteRenderer renderer in all)
            {
                if (IsBackground(renderer)) backgrounds.Add(renderer);
                else props.Add(renderer);
            }

            propRenderers = props.ToArray();
            backgroundRenderers = backgrounds.ToArray();
        }

        private static bool IsBackground(SpriteRenderer renderer)
        {
            if (renderer.sprite != null && renderer.sprite.name.StartsWith("Background"))
                return true;

            Transform t = renderer.transform;
            while (t != null)
            {
                if (t.name.StartsWith("Background")) return true;
                t = t.parent;
            }
            return false;
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;

            if (keyboard != null)
            {
                if (keyboard.f1Key.wasPressedThisFrame) helpVisible = !helpVisible;

                if (!IsFollowing)
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
                    float speed = panSpeed * Mathf.Max(0.5f, cam.orthographicSize / 6f);
                    transform.position += (Vector3)(direction * speed * Time.unscaledDeltaTime);
                }
            }

            if (mouse != null)
            {
                float scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    float zoomFactor = Mathf.Max(0.5f, cam.orthographicSize * 0.08f);
                    cam.orthographicSize = Mathf.Clamp(
                        cam.orthographicSize - Mathf.Sign(scroll) * zoomFactor,
                        minZoom,
                        maxZoom);
                }

                Vector2 screen = mouse.position.ReadValue();
                Vector3 world = cam.ScreenToWorldPoint(
                    new Vector3(screen.x, screen.y, -transform.position.z));
                world.z = 0f;
                hoverText = Probe(world);
            }
        }

        private void LateUpdate()
        {
            if (followTarget == null)
                return;

            Vector3 desired = new Vector3(
                followTarget.position.x + followOffset.x,
                followTarget.position.y + followOffset.y,
                transform.position.z);

            transform.position = Vector3.SmoothDamp(
                transform.position, desired, ref followVelocity, followSmooth);
        }

        private string Probe(Vector3 world)
        {
            SpriteRenderer bestProp = null;
            float bestArea = float.MaxValue;
            if (propRenderers != null)
            {
                foreach (SpriteRenderer renderer in propRenderers)
                {
                    if (renderer == null || renderer.sprite == null || !renderer.enabled) continue;
                    Bounds b = renderer.bounds;
                    if (world.x < b.min.x || world.x > b.max.x || world.y < b.min.y || world.y > b.max.y)
                        continue;
                    if (!PixelHit(renderer, world)) continue;

                    float area = b.size.x * b.size.y;
                    if (area < bestArea)
                    {
                        bestArea = area;
                        bestProp = renderer;
                    }
                }
            }
            if (bestProp != null)
                return "PROP   " + BiomeOf(bestProp.transform) + "   " + bestProp.gameObject.name +
                       "   (sprite: " + bestProp.sprite.name + ")";

            Tilemap bestMap = null;
            TileBase bestTile = null;
            int bestOrder = int.MinValue;
            if (tilemaps != null)
            {
                foreach (Tilemap map in tilemaps)
                {
                    if (map == null) continue;
                    Vector3Int cell = map.WorldToCell(world);
                    TileBase tile = map.GetTile(cell);
                    if (tile == null) continue;

                    TilemapRenderer renderer = map.GetComponent<TilemapRenderer>();
                    int order = renderer != null ? renderer.sortingOrder : 0;
                    if (order >= bestOrder)
                    {
                        bestOrder = order;
                        bestMap = map;
                        bestTile = tile;
                    }
                }
            }
            if (bestTile != null)
            {
                Vector3Int cell = bestMap.WorldToCell(world);
                return "TILE   " + BiomeOf(bestMap.transform) + "   " + bestMap.name +
                       "   -> " + bestTile.name + "   @cell(" + cell.x + "," + cell.y + ")";
            }

            SpriteRenderer bestBackground = null;
            int bestBackgroundOrder = int.MinValue;
            if (backgroundRenderers != null)
            {
                foreach (SpriteRenderer renderer in backgroundRenderers)
                {
                    if (renderer == null || renderer.sprite == null || !renderer.enabled) continue;
                    Bounds b = renderer.bounds;
                    if (world.x < b.min.x || world.x > b.max.x || world.y < b.min.y || world.y > b.max.y)
                        continue;
                    if (renderer.sortingOrder > bestBackgroundOrder)
                    {
                        bestBackgroundOrder = renderer.sortingOrder;
                        bestBackground = renderer;
                    }
                }
            }
            if (bestBackground != null)
                return "BG     " + BiomeOf(bestBackground.transform) + "   " +
                       bestBackground.sprite.name + "   (parallax layer)";

            return string.Empty;
        }

        private static bool PixelHit(SpriteRenderer renderer, Vector3 world)
        {
            try
            {
                Sprite sprite = renderer.sprite;
                Texture2D texture = sprite.texture;
                if (texture == null || !texture.isReadable) return true;

                Vector3 local = renderer.transform.InverseTransformPoint(world);
                Rect rect = sprite.textureRect;
                float ppu = sprite.pixelsPerUnit;
                Vector2 pivot = sprite.pivot;

                float px = rect.x + local.x * ppu + pivot.x;
                float py = rect.y + local.y * ppu + pivot.y;

                if (px < rect.x || px >= rect.xMax || py < rect.y || py >= rect.yMax)
                    return false;

                return texture.GetPixel((int)px, (int)py).a > 0.1f;
            }
            catch
            {
                return true;
            }
        }

        private static string BiomeOf(Transform t)
        {
            while (t != null)
            {
                if (t.name.EndsWith("_Biome"))
                    return t.name.Substring(0, t.name.Length - "_Biome".Length);
                t = t.parent;
            }
            return "?";
        }

        public void GoToSection(int index)
        {
            if (sectionPositions == null || sectionPositions.Length == 0)
                return;

            currentSection = Mathf.Clamp(index, 0, sectionPositions.Length - 1);
            transform.position = sectionPositions[currentSection];

            if (cam == null) cam = GetComponent<Camera>();
            if (sectionSizes != null && currentSection < sectionSizes.Length)
                cam.orthographicSize = sectionSizes[currentSection];
        }

        private void OnGUI()
        {
            const float margin = 12f;

            if (IsFollowing)
            {
                if (helpVisible)
                {
                    GUI.Box(new Rect(margin, Screen.height - 96f, Mathf.Min(940f, Screen.width - 24f), 40f),
                        GUIContent.none);
                    GUI.Label(new Rect(margin + 10f, Screen.height - 90f, 920f, 20f),
                        "Move A/D  ·  Jump Space (x2)  ·  Dash Q  ·  Combo J  ·  Sword K  ·  Guard hold G  ·  Zoom Wheel");
                    GUI.Label(new Rect(margin + 10f, Screen.height - 72f, 920f, 20f),
                        "Hover any tile with the mouse to see which asset it uses.  ·  Toggle help F1");
                }
            }
            else
            {
                const float buttonWidth = 150f;
                const float buttonHeight = 30f;

                int sectionCount = sectionNames != null && sectionNames.Length > 0
                    ? sectionNames.Length
                    : (sectionPositions != null ? sectionPositions.Length : 0);

                if (sectionCount > 0)
                {
                    float panelWidth = Mathf.Min(Screen.width - margin * 2f, sectionCount * buttonWidth + 16f);
                    GUI.Box(new Rect(margin, margin, panelWidth, helpVisible ? 112f : 52f), GUIContent.none);

                    for (int i = 0; i < sectionCount; i++)
                    {
                        string label = sectionNames != null && i < sectionNames.Length
                            ? (i + 1) + "  " + sectionNames[i]
                            : "Section " + (i + 1);

                        if (GUI.Button(new Rect(margin + 8f + i * buttonWidth, margin + 8f,
                            buttonWidth - 6f, buttonHeight), label))
                            GoToSection(i);
                    }

                    if (helpVisible)
                    {
                        GUI.Label(new Rect(margin + 10f, margin + 47f, panelWidth - 20f, 22f),
                            "Move: WASD / Arrows     Zoom: Mouse Wheel     Reset: R     Help: H");
                        GUI.Label(new Rect(margin + 10f, margin + 69f, panelWidth - 20f, 22f),
                            "Hover a tile / prop / background to see which asset it uses.");
                    }
                }
            }

            GUIStyle style = new GUIStyle(GUI.skin.box)
            {
                fontSize = 15,
                alignment = TextAnchor.MiddleLeft
            };
            string readout = string.IsNullOrEmpty(hoverText) ? "Hover the map..." : hoverText;
            GUI.Box(new Rect(margin, Screen.height - 50f, Mathf.Min(940f, Screen.width - 24f), 38f),
                readout, style);
        }
    }
}
