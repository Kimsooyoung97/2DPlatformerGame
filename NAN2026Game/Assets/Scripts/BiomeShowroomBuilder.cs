#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using NAN2026.Showroom;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NAN2026.Showroom.Editor
{
    /// <summary>
    /// Builds a viewer scene for the 16 PPU biome packs.
    /// Every sprite is placed at scale 1 so pixels stay crisp; layout uses spacing instead of scaling.
    /// </summary>
    public static class BiomeShowroomBuilder
    {
        private const string ScenePath = "Assets/Map/Showroom/BiomeShowroom.unity";
        private const float CameraDepth = -10f;
        private const float SectionGap = 25f;

        private const int TileColumns = 10;
        private const float TileCell = 1.25f;
        private const float TileCategoryGap = 0.75f;
        private const float TilesWidth = 14f;
        private const float TilesCamera = 9f;

        private const int PropColumns = 6;
        private const float PropCellWidth = 6f;
        private const float PropCellHeight = 11f;
        private const float PropsWidth = 38f;
        private const float PropsCamera = 14f;

        private const float BackgroundSlotGap = 1f;
        private const float BackgroundsWidth = 105f;
        private const float BackgroundsCamera = 30f;

        private readonly struct BiomePack
        {
            public readonly string DisplayName;
            public readonly string AtlasPath;

            public BiomePack(string displayName, string atlasPath)
            {
                DisplayName = displayName;
                AtlasPath = atlasPath;
            }
        }

        private readonly struct SectionPlan
        {
            public readonly string Name;
            public readonly Vector3 RootPosition;
            public readonly float CameraSize;

            public SectionPlan(string name, Vector3 rootPosition, float cameraSize)
            {
                Name = name;
                RootPosition = rootPosition;
                CameraSize = cameraSize;
            }
        }

        private static readonly BiomePack[] Packs =
        {
            new BiomePack("Plains", "Assets/2D Pixel Art Platformer Biome - Plains/Sprites.png"),
            new BiomePack("Forest", "Assets/2D Pixel Art Platformer Biome - American Forest/Sprites.png")
        };

        private static readonly string[] TileCategoryOrder =
        {
            "TileGround", "TileBackGround", "TilePlant", "TileFence", "TileSpikes"
        };

        [MenuItem("Tools/Biome Showroom/Rebuild Biome Showroom")]
        public static void BuildShowroom()
        {
            if (!EnsureEditModeAndSave())
                return;

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SceneManager.SetActiveScene(scene);

            SectionPlan[] plan = BuildPlan();
            List<GameObject> sectionRoots = new List<GameObject>();
            int sectionIndex = 0;
            int spriteTotal = 0;
            int missingPacks = 0;

            foreach (BiomePack pack in Packs)
            {
                Sprite[] sprites = LoadAtlasSprites(pack.AtlasPath);
                if (sprites.Length == 0)
                {
                    Debug.LogError("Biome showroom: atlas has no sprites, pack skipped: " + pack.AtlasPath);
                    missingPacks++;
                    sectionIndex += 3;
                    sectionRoots.Add(null);
                    sectionRoots.Add(null);
                    sectionRoots.Add(null);
                    continue;
                }

                spriteTotal += sprites.Length;

                Sprite[] tiles = sprites.Where(sprite => IsTile(sprite.name)).ToArray();
                Sprite[] backgrounds = sprites.Where(sprite => IsBackground(sprite.name)).ToArray();
                Sprite[] props = sprites
                    .Where(sprite => !IsTile(sprite.name) && !IsBackground(sprite.name))
                    .ToArray();

                GameObject tileRoot = CreateSectionRoot(
                    $"{sectionIndex + 1:00}_{pack.DisplayName}_Tiles_{tiles.Length}", plan[sectionIndex++]);
                sectionRoots.Add(tileRoot);
                BuildTileSection(tileRoot.transform, tiles);

                GameObject propRoot = CreateSectionRoot(
                    $"{sectionIndex + 1:00}_{pack.DisplayName}_Props_{props.Length}", plan[sectionIndex++]);
                sectionRoots.Add(propRoot);
                BuildPropSection(propRoot.transform, props);

                GameObject backgroundRoot = CreateSectionRoot(
                    $"{sectionIndex + 1:00}_{pack.DisplayName}_Backgrounds_{backgrounds.Length}", plan[sectionIndex++]);
                sectionRoots.Add(backgroundRoot);
                BuildBackgroundSection(backgroundRoot.transform, backgrounds);
            }

            CreateCamera(plan, sectionRoots);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();

            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.pivot = plan[0].RootPosition;
                SceneView.lastActiveSceneView.size = plan[0].CameraSize + 2f;
                SceneView.lastActiveSceneView.Repaint();
            }

            Debug.Log($"Biome showroom rebuilt: {Packs.Length - missingPacks}/{Packs.Length} packs, " +
                      $"{plan.Length} sections, {spriteTotal} sprites. Scene: {ScenePath}");
        }

        [MenuItem("Tools/Biome Showroom/Open Biome Showroom")]
        public static void OpenShowroom()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                BuildShowroom();
                return;
            }

            if (!EnsureEditModeAndSave())
                return;

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        private static bool EnsureEditModeAndSave()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Biome showroom: exit play mode before building or opening the scene.");
                return false;
            }

            return EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        }

        private static SectionPlan[] BuildPlan()
        {
            List<SectionPlan> plan = new List<SectionPlan>();
            float cursor = 0f;

            foreach (BiomePack pack in Packs)
            {
                cursor = AddSection(plan, pack.DisplayName + " Tiles", cursor, TilesWidth, TilesCamera);
                cursor = AddSection(plan, pack.DisplayName + " Props", cursor, PropsWidth, PropsCamera);
                cursor = AddSection(plan, pack.DisplayName + " BG", cursor, BackgroundsWidth, BackgroundsCamera);
            }

            return plan.ToArray();
        }

        private static float AddSection(
            List<SectionPlan> plan, string name, float cursor, float width, float cameraSize)
        {
            float center = cursor + width * 0.5f;
            plan.Add(new SectionPlan(name, new Vector3(center, 0f, 0f), cameraSize));
            return center + width * 0.5f + SectionGap;
        }

        private static GameObject CreateSectionRoot(string objectName, SectionPlan section)
        {
            GameObject root = new GameObject(objectName);
            root.transform.position = section.RootPosition;
            return root;
        }

        private static void BuildTileSection(Transform parent, IReadOnlyList<Sprite> tiles)
        {
            List<KeyValuePair<string, List<Sprite>>> categories =
                new List<KeyValuePair<string, List<Sprite>>>();

            foreach (string category in TileCategoryOrder)
            {
                List<Sprite> members = tiles
                    .Where(sprite => IsInCategory(sprite.name, category))
                    .OrderBy(sprite => NumericSuffix(sprite.name))
                    .ToList();

                if (members.Count > 0)
                    categories.Add(new KeyValuePair<string, List<Sprite>>(category, members));
            }

            List<Sprite> leftovers = tiles
                .Where(sprite => !categories.Any(group => group.Value.Contains(sprite)))
                .OrderBy(sprite => sprite.name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (leftovers.Count > 0)
                categories.Add(new KeyValuePair<string, List<Sprite>>("TileOther", leftovers));

            float totalHeight = 0f;
            foreach (KeyValuePair<string, List<Sprite>> group in categories)
            {
                int rows = Mathf.CeilToInt(group.Value.Count / (float)TileColumns);
                totalHeight += rows * TileCell + TileCategoryGap;
            }

            float y = totalHeight * 0.5f;

            foreach (KeyValuePair<string, List<Sprite>> group in categories)
            {
                int rows = Mathf.CeilToInt(group.Value.Count / (float)TileColumns);

                GameObject categoryRoot = new GameObject($"{group.Key}_{group.Value.Count}");
                categoryRoot.transform.SetParent(parent, false);
                categoryRoot.transform.localPosition = new Vector3(0f, y, 0f);

                for (int i = 0; i < group.Value.Count; i++)
                {
                    int column = i % TileColumns;
                    int row = i / TileColumns;
                    float x = (column - (TileColumns - 1) * 0.5f) * TileCell;

                    CreateSprite(
                        group.Value[i].name,
                        group.Value[i],
                        categoryRoot.transform,
                        new Vector3(x, -row * TileCell, 0f),
                        10);
                }

                y -= rows * TileCell + TileCategoryGap;
            }
        }

        private static void BuildPropSection(Transform parent, IReadOnlyList<Sprite> props)
        {
            List<Sprite> ordered = props
                .OrderBy(sprite => sprite.name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            int rows = Mathf.Max(1, Mathf.CeilToInt(ordered.Count / (float)PropColumns));

            for (int i = 0; i < ordered.Count; i++)
            {
                int column = i % PropColumns;
                int row = i / PropColumns;

                float x = (column - (PropColumns - 1) * 0.5f) * PropCellWidth;
                float cellCenterY = ((rows - 1) * 0.5f - row) * PropCellHeight;
                float cellBottom = cellCenterY - PropCellHeight * 0.5f;
                float y = cellBottom + ordered[i].bounds.size.y * 0.5f;

                CreateSprite(ordered[i].name, ordered[i], parent, new Vector3(x, y, 0f), 10);
            }
        }

        private static void BuildBackgroundSection(Transform parent, IReadOnlyList<Sprite> backgrounds)
        {
            if (backgrounds.Count == 0)
                return;

            List<Sprite> ordered = backgrounds
                .OrderBy(sprite => NumericSuffix(sprite.name))
                .ToList();

            float slotWidth = ordered.Max(sprite => sprite.bounds.size.x) + BackgroundSlotGap;
            float startX = -(ordered.Count * slotWidth) * 0.5f;

            GameObject composite = new GameObject("Composite_Parallax_Preview");
            composite.transform.SetParent(parent, false);
            composite.transform.localPosition = new Vector3(startX, 0f, 0f);

            for (int i = 0; i < ordered.Count; i++)
            {
                CreateSprite(
                    "Composite | " + ordered[i].name,
                    ordered[i],
                    composite.transform,
                    Vector3.zero,
                    -100 + i,
                    false);
            }

            GameObject layers = new GameObject("Individual_Layers");
            layers.transform.SetParent(parent, false);

            for (int i = 0; i < ordered.Count; i++)
            {
                CreateSprite(
                    "Layer | " + ordered[i].name,
                    ordered[i],
                    layers.transform,
                    new Vector3(startX + slotWidth * (i + 1), 0f, 0f),
                    10 + i);
            }
        }

        private static void CreateCamera(SectionPlan[] plan, List<GameObject> sectionRoots)
        {
            const float assumedAspect = 16f / 9f;
            const float viewMargin = 1.12f;

            Vector3[] positions = new Vector3[plan.Length];
            float[] sizes = new float[plan.Length];

            for (int i = 0; i < plan.Length; i++)
            {
                GameObject root = i < sectionRoots.Count ? sectionRoots[i] : null;

                if (root == null || !TryGetContentBounds(root, out Bounds bounds))
                {
                    positions[i] = new Vector3(plan[i].RootPosition.x, plan[i].RootPosition.y, CameraDepth);
                    sizes[i] = plan[i].CameraSize;
                    continue;
                }

                positions[i] = new Vector3(bounds.center.x, bounds.center.y, CameraDepth);
                float byHeight = bounds.extents.y;
                float byWidth = bounds.extents.x / assumedAspect;
                sizes[i] = Mathf.Max(2f, Mathf.Max(byHeight, byWidth) * viewMargin);
            }

            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";

            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = sizes[0];
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.16f, 0.17f, 0.2f, 1f);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 200f;
            cameraObject.transform.position = positions[0];
            cameraObject.AddComponent<AudioListener>();

            PlatformerShowroomController controller =
                cameraObject.AddComponent<PlatformerShowroomController>();

            controller.Configure(
                positions,
                sizes,
                plan.Select(section => section.Name).ToArray());
        }

        private static bool TryGetContentBounds(GameObject root, out Bounds bounds)
        {
            SpriteRenderer[] renderers = root.GetComponentsInChildren<SpriteRenderer>(true);
            if (renderers.Length == 0)
            {
                bounds = default;
                return false;
            }

            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            return true;
        }


        private static GameObject CreateSprite(
            string objectName,
            Sprite sprite,
            Transform parent,
            Vector3 localPosition,
            int sortingOrder,
            bool hoverCollider = true)
        {
            GameObject gameObject = new GameObject(objectName);
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.localPosition = localPosition;
            gameObject.transform.localScale = Vector3.one;

            SpriteRenderer renderer = gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;

            if (hoverCollider)
            {
                BoxCollider2D box = gameObject.AddComponent<BoxCollider2D>();
                box.size = sprite.bounds.size;
                box.offset = sprite.bounds.center;
                box.isTrigger = true;
            }

            return gameObject;
        }

        private static Sprite[] LoadAtlasSprites(string path)
        {
            return AssetDatabase.LoadAllAssetRepresentationsAtPath(path)
                .OfType<Sprite>()
                .ToArray();
        }

        private static bool IsTile(string spriteName)
        {
            return spriteName.StartsWith("Tile", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsBackground(string spriteName)
        {
            return spriteName.StartsWith("Background", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsInCategory(string spriteName, string category)
        {
            if (!spriteName.StartsWith(category, StringComparison.OrdinalIgnoreCase))
                return false;

            string remainder = spriteName.Substring(category.Length);
            return remainder.Length == 0 || remainder.All(char.IsDigit);
        }

        private static int NumericSuffix(string spriteName)
        {
            int index = spriteName.Length;
            while (index > 0 && char.IsDigit(spriteName[index - 1]))
                index--;

            if (index == spriteName.Length)
                return 0;

            return int.TryParse(spriteName.Substring(index), out int value) ? value : 0;
        }
    }
}
#endif
