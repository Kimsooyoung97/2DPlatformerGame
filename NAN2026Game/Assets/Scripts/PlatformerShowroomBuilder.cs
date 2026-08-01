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
    public static class PlatformerShowroomBuilder
    {
        private const string ScenePath = "Assets/Map/Showroom/PlatformerSet1Showroom.unity";
        private const string SetRoot = "Assets/PlatformerSet1";
        private const string AtlasPath = SetRoot + "/main_lev_build.png";
        private const float CameraDepth = -10f;

        private readonly struct ShowroomSection
        {
            public readonly string Name;
            public readonly Vector3 RootPosition;
            public readonly float CameraSize;

            public ShowroomSection(string name, Vector3 rootPosition, float cameraSize)
            {
                Name = name;
                RootPosition = rootPosition;
                CameraSize = cameraSize;
            }
        }

        private const float PackSectionStride = 70f;
        private const float TileGalleryTargetSize = 1.5f;
        private const int BaseSectionCount = 4;

        private readonly struct AtlasPack
        {
            public readonly string DisplayName;
            public readonly string AtlasPath;
            public readonly float CameraSize;

            public AtlasPack(string displayName, string atlasPath, float cameraSize)
            {
                DisplayName = displayName;
                AtlasPath = atlasPath;
                CameraSize = cameraSize;
            }
        }

        private static readonly AtlasPack[] ExtraAtlasPacks =
        {
            new AtlasPack(
                "Plains",
                "Assets/2D Pixel Art Platformer Biome - Plains/Sprites.png",
                10f),
            new AtlasPack(
                "Forest",
                "Assets/2D Pixel Art Platformer Biome - American Forest/Sprites.png",
                10f)
        };

        private static ShowroomSection[] Sections
        {
            get
            {
                List<ShowroomSection> plan = new List<ShowroomSection>
                {
                    new ShowroomSection("Sample Map", new Vector3(0f, 0f, 0f), 6f),
                    new ShowroomSection("All Tiles", new Vector3(80f, 0f, 0f), 16.5f),
                    new ShowroomSection("Backgrounds", new Vector3(145f, 0f, 0f), 10f),
                    new ShowroomSection("Props & Frames", new Vector3(205f, 4f, 0f), 12.5f)
                };

                float nextX = plan[plan.Count - 1].RootPosition.x;
                foreach (AtlasPack pack in ExtraAtlasPacks)
                {
                    nextX += PackSectionStride;
                    plan.Add(new ShowroomSection(
                        pack.DisplayName,
                        new Vector3(nextX, 0f, 0f),
                        pack.CameraSize));
                }

                return plan.ToArray();
            }
        }

        private static Sprite[] LoadAtlasSprites(string path)
        {
            return AssetDatabase.LoadAllAssetRepresentationsAtPath(path)
                .OfType<Sprite>()
                .OrderByDescending(sprite => sprite.rect.y)
                .ThenBy(sprite => sprite.rect.x)
                .ToArray();
        }

        private static Vector3 SectionCameraPosition(int index)
        {
            ShowroomSection section = Sections[index];
            return new Vector3(section.RootPosition.x, section.RootPosition.y, CameraDepth);
        }


        private static readonly string[] BackgroundPaths =
        {
            SetRoot + "/01 background.png",
            SetRoot + "/02 background.png",
            SetRoot + "/03 background A.png",
            SetRoot + "/03 background B.png",
            SetRoot + "/04 background.png",
            SetRoot + "/05 background.png"
        };

        [MenuItem("Tools/Platformer Set 1/Rebuild Showroom")]
        public static void BuildShowroom()
        {
            if (!EnsureEditModeAndSave())
                return;

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SceneManager.SetActiveScene(scene);

            ShowroomSection[] plan = Sections;
            Sprite[] atlasSprites = LoadAtlasSprites(AtlasPath);

            Dictionary<string, Sprite> atlasByName = atlasSprites
                .GroupBy(sprite => sprite.name)
                .ToDictionary(group => group.Key, group => group.First());

            GameObject sampleRoot = new GameObject("01_SampleMap");
            GameObject tileRoot = new GameObject($"02_AllTiles_{atlasSprites.Length}");
            GameObject backgroundRoot = new GameObject($"03_AllBackgrounds_{BackgroundPaths.Length}");
            GameObject frameRoot = new GameObject("04_Props_And_AnimationFrames");

            sampleRoot.transform.position = plan[0].RootPosition;
            tileRoot.transform.position = plan[1].RootPosition;
            backgroundRoot.transform.position = plan[2].RootPosition;
            frameRoot.transform.position = plan[3].RootPosition;

            BuildSampleMap(sampleRoot.transform, atlasByName);
            BuildTileGallery(tileRoot.transform, atlasSprites);
            BuildBackgroundGallery(backgroundRoot.transform);
            BuildFrameGallery(frameRoot.transform);

            int packSpriteTotal = 0;
            int packSectionsBuilt = 0;
            for (int i = 0; i < ExtraAtlasPacks.Length; i++)
            {
                AtlasPack pack = ExtraAtlasPacks[i];
                Sprite[] packSprites = LoadAtlasSprites(pack.AtlasPath);

                if (packSprites.Length == 0)
                {
                    Debug.LogError("Showroom pack atlas has no sprites, section left empty: " + pack.AtlasPath);
                    continue;
                }

                GameObject packRoot = new GameObject($"{i + BaseSectionCount + 1:00}_{pack.DisplayName}_{packSprites.Length}");
                packRoot.transform.position = plan[BaseSectionCount + i].RootPosition;
                BuildTileGallery(packRoot.transform, packSprites);

                packSpriteTotal += packSprites.Length;
                packSectionsBuilt++;
            }

            CreateCameraAndLight();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();

            Selection.activeGameObject = sampleRoot;
            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.pivot = plan[0].RootPosition;
                SceneView.lastActiveSceneView.size = plan[0].CameraSize + 2f;
                SceneView.lastActiveSceneView.Repaint();
            }

            Debug.Log($"Showroom rebuilt: {plan.Length} sections | base atlas {atlasSprites.Length} tiles, " +
                      $"{BackgroundPaths.Length} backgrounds | extra packs {packSectionsBuilt}/{ExtraAtlasPacks.Length} " +
                      $"({packSpriteTotal} sprites). Scene: {ScenePath}");
        }

        [MenuItem("Tools/Platformer Set 1/Open Showroom")]
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

        private static void CreateCameraAndLight()
        {
            ShowroomSection[] plan = Sections;

            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = plan[0].CameraSize;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.31f, 0.31f, 0.35f, 1f);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 200f;
            cameraObject.transform.position = SectionCameraPosition(0);
            cameraObject.AddComponent<AudioListener>();

            PlatformerShowroomController controller = cameraObject.AddComponent<PlatformerShowroomController>();
            controller.Configure(
                plan.Select(section => new Vector3(section.RootPosition.x, section.RootPosition.y, CameraDepth)).ToArray(),
                plan.Select(section => section.CameraSize).ToArray(),
                plan.Select(section => section.Name).ToArray());

            GameObject lightObject = new GameObject("Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 0.6f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        private static void BuildTileGallery(Transform parent, IReadOnlyList<Sprite> sprites)
        {
            const int columns = 15;
            const float spacingX = 2.15f;
            const float spacingY = 2.15f;
            int rows = Mathf.CeilToInt(sprites.Count / (float)columns);

            for (int i = 0; i < sprites.Count; i++)
            {
                int column = i % columns;
                int row = i / columns;
                float x = (column - (columns - 1) * 0.5f) * spacingX;
                float y = ((rows - 1) * 0.5f - row) * spacingY;

                Sprite sprite = sprites[i];
                float largestSide = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
                float scale = largestSide > 0f ? Mathf.Clamp(TileGalleryTargetSize / largestSide, 0.01f, 6f) : 1f;

                CreateSpriteObject(
                    $"Tile {i + 1:000} | {sprite.name}",
                    sprite,
                    parent,
                    new Vector3(x, y, 0f),
                    scale,
                    i,
                    true,
                    false);
            }
        }

        private static void BuildBackgroundGallery(Transform parent)
        {
            for (int i = 0; i < BackgroundPaths.Length; i++)
            {
                Sprite sprite = LoadPrimarySprite(BackgroundPaths[i]);
                if (sprite == null)
                    continue;

                int column = i % 3;
                int row = i / 3;
                float x = (column - 1) * 8.7f;
                float y = row == 0 ? 4f : -4f;

                CreateSpriteObject(
                    "Background | " + sprite.name,
                    sprite,
                    parent,
                    new Vector3(x, y, 0f),
                    1.75f,
                    i,
                    true,
                    false);
            }
        }

        private static void BuildFrameGallery(Transform parent)
        {
            Sprite decorationSheet = LoadPrimarySprite(SetRoot + "/other_and_decorative.png");
            if (decorationSheet != null)
            {
                CreateSpriteObject(
                    "Raw Decoration Sheet | other_and_decorative",
                    decorationSheet,
                    parent,
                    new Vector3(0f, 4.8f, 0f),
                    1.35f,
                    0,
                    true,
                    false);
            }

            string[] frameGuids = AssetDatabase.FindAssets(
                "t:Texture2D",
                new[] { SetRoot + "/Animated", SetRoot + "/AnimCharacter" });

            string[] framePaths = frameGuids
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            const int columns = 17;
            const float spacingX = 1.38f;
            const float spacingY = 1.55f;

            for (int i = 0; i < framePaths.Length; i++)
            {
                Sprite sprite = LoadPrimarySprite(framePaths[i]);
                if (sprite == null)
                    continue;

                int column = i % columns;
                int row = i / columns;
                float x = (column - (columns - 1) * 0.5f) * spacingX;
                float y = -1.4f - row * spacingY;
                string shortPath = framePaths[i].Replace(SetRoot + "/", string.Empty);

                CreateSpriteObject(
                    $"Frame {i + 1:00} | {shortPath}",
                    sprite,
                    parent,
                    new Vector3(x, y, 0f),
                    3.5f,
                    i + 10,
                    true,
                    false);
            }
        }

        private static void BuildSampleMap(Transform parent, IReadOnlyDictionary<string, Sprite> atlas)
        {
            GameObject backgroundGroup = new GameObject("Layer_Background_Composite");
            backgroundGroup.transform.SetParent(parent, false);

            string[] compositeBackgrounds =
            {
                BackgroundPaths[0], BackgroundPaths[1], BackgroundPaths[2],
                BackgroundPaths[4], BackgroundPaths[5]
            };

            for (int i = 0; i < compositeBackgrounds.Length; i++)
            {
                Sprite sprite = LoadPrimarySprite(compositeBackgrounds[i]);
                if (sprite == null)
                    continue;

                CreateSpriteObject(
                    "Background Layer | " + sprite.name,
                    sprite,
                    backgroundGroup.transform,
                    Vector3.zero,
                    5f,
                    -100 + i,
                    false,
                    false);
            }

            GameObject architecture = new GameObject("Layer_Architecture");
            architecture.transform.SetParent(parent, false);

            AddMapPiece(atlas, architecture.transform, "tile_1088_912_144x128", new Vector3(0f, -2.35f, 0f), 3.125f, -8, false, false);
            AddMapPiece(atlas, architecture.transform, "tile_144_976_32x128", new Vector3(-8.4f, -3.1f, 0f), 3.125f, 0, false, true);
            AddMapPiece(atlas, architecture.transform, "tile_144_976_32x128", new Vector3(8.4f, -3.1f, 0f), 3.125f, 0, true, true);
            AddMapPiece(atlas, architecture.transform, "tile_832_208_32x176", new Vector3(0f, -3.1f, 0f), 3.125f, -2, false, false);

            AddMapPiece(atlas, architecture.transform, "tile_240_128_224x32", new Vector3(-6.8f, -3.1f, 0f), 3.125f, 5, false, true);
            AddMapPiece(atlas, architecture.transform, "tile_240_128_224x32", new Vector3(0f, -3.1f, 0f), 3.125f, 5, false, true);
            AddMapPiece(atlas, architecture.transform, "tile_240_128_224x32", new Vector3(6.8f, -3.1f, 0f), 3.125f, 5, false, true);
            AddMapPiece(atlas, architecture.transform, "tile_208_176_176x32", new Vector3(-4.3f, -0.2f, 0f), 3.125f, 5, false, true);
            AddMapPiece(atlas, architecture.transform, "tile_208_176_176x32", new Vector3(4.3f, 1.2f, 0f), 3.125f, 5, false, true);
            AddMapPiece(atlas, architecture.transform, "tile_560_368_96x64", new Vector3(-8.3f, -3.1f, 0f), 3.125f, 6, false, true);
            AddMapPiece(atlas, architecture.transform, "tile_560_368_96x64", new Vector3(8.3f, -3.1f, 0f), 3.125f, 6, true, true);

            GameObject animated = new GameObject("Layer_AnimatedProps");
            animated.transform.SetParent(parent, false);
            CreateAnimatedProp(animated.transform, "Diamond", "diamond", 5, new Vector3(4.3f, 2.65f, 0f), 4f, 8f, 20);
            CreateAnimatedProp(animated.transform, "Torch A Left", "torch-A", 4, new Vector3(-5.9f, -1.75f, 0f), 3.5f, 9f, 20);
            CreateAnimatedProp(animated.transform, "Torch B Center", "torch-B", 4, new Vector3(0f, -1.75f, 0f), 3.5f, 9f, 20);
            CreateAnimatedProp(animated.transform, "Torch C Right", "torch-C", 4, new Vector3(5.9f, -1.75f, 0f), 3.5f, 9f, 20);
            CreateAnimatedProp(animated.transform, "Light Beam", "light", 4, new Vector3(0f, 1.4f, 0f), 3.5f, 7f, 15);
        }

        private static void AddMapPiece(
            IReadOnlyDictionary<string, Sprite> atlas,
            Transform parent,
            string spriteName,
            Vector3 localPosition,
            float scale,
            int sortingOrder,
            bool flipX,
            bool collider)
        {
            if (!atlas.TryGetValue(spriteName, out Sprite sprite))
            {
                Debug.LogWarning("Showroom sample tile not found: " + spriteName);
                return;
            }

            GameObject piece = CreateSpriteObject(
                "Map Tile | " + spriteName,
                sprite,
                parent,
                localPosition,
                scale,
                sortingOrder,
                false,
                collider);

            piece.GetComponent<SpriteRenderer>().flipX = flipX;
        }

        private static void CreateAnimatedProp(
            Transform parent,
            string objectName,
            string filePrefix,
            int frameCount,
            Vector3 localPosition,
            float scale,
            float framesPerSecond,
            int sortingOrder)
        {
            List<Sprite> frames = new List<Sprite>();
            for (int i = 1; i <= frameCount; i++)
            {
                string path = $"{SetRoot}/Animated/{filePrefix}-{i:00}.png";
                Sprite frame = LoadPrimarySprite(path);
                if (frame != null)
                    frames.Add(frame);
            }

            if (frames.Count == 0)
                return;

            GameObject prop = CreateSpriteObject(
                objectName,
                frames[0],
                parent,
                localPosition,
                scale,
                sortingOrder,
                true,
                false);

            PlatformerShowroomSpriteAnimator animator = prop.AddComponent<PlatformerShowroomSpriteAnimator>();
            animator.Configure(frames.ToArray(), framesPerSecond);
        }

        private static GameObject CreateSpriteObject(
            string objectName,
            Sprite sprite,
            Transform parent,
            Vector3 localPosition,
            float uniformScale,
            int sortingOrder,
            bool hoverCollider,
            bool solidCollider)
        {
            GameObject gameObject = new GameObject(objectName);
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.localPosition = localPosition;
            gameObject.transform.localScale = Vector3.one * uniformScale;

            SpriteRenderer renderer = gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;

            if (hoverCollider || solidCollider)
            {
                BoxCollider2D box = gameObject.AddComponent<BoxCollider2D>();
                box.size = sprite.bounds.size;
                box.offset = sprite.bounds.center;
                box.isTrigger = hoverCollider && !solidCollider;
            }

            return gameObject;
        }

        private static Sprite LoadPrimarySprite(string path)
        {
            Sprite direct = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (direct != null)
                return direct;

            return AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<Sprite>()
                .OrderByDescending(sprite => sprite.rect.width * sprite.rect.height)
                .FirstOrDefault();
        }
    

        private static bool EnsureEditModeAndSave()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Showroom: exit play mode before building or opening the showroom.");
                return false;
            }

            return EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        }
}
}
#endif
