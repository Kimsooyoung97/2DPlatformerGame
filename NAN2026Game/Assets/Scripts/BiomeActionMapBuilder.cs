#if UNITY_EDITOR
using System.Collections.Generic;
using NAN2026.Showroom;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace NAN2026.Showroom.Editor
{
    /// <summary>
    /// Procedurally builds a dynamic side-scrolling ACTION level that blends the
    /// American Forest and Plains tilesets, then drops the playable character in.
    ///
    /// Terrain is a height map with plateaus, steps, spike pits and floating platforms;
    /// spike traps are scattered throughout; props from both packs decorate the surface;
    /// parallax backgrounds follow each zone. Ground uses a CompositeCollider2D, spikes
    /// use trigger colliders, the player gets health + checkpoint respawn, and the camera
    /// follows the player while keeping the hover tile inspector.
    /// </summary>
    public static class BiomeActionMapBuilder
    {
        private const string ScenePath = "Assets/Map/Showroom/BiomeActionMap.unity";
        private const string ForestDir = "Assets/2D Pixel Art Platformer Biome - American Forest/";
        private const string PlainsDir = "Assets/2D Pixel Art Platformer Biome - Plains/";
        private const string PlayerPrefabPath = "Assets/Player/Prefabs/PixelPlayer.prefab";
        private const float CameraDepth = -10f;
        private const float PlayCameraSize = 9f;

        private const int LevelWidth = 176;
        private const int Bottom = -12;
        private const int MaxHeight = 8;
        private const int MinHeight = -4;
        private const int Seed = 8891;
        internal const string BuildVersion = "v5-traps";

        private sealed class Palette
        {
            public TileBase Top, TopLeft, TopRight, Fill, Spike;
            public Dictionary<string, Sprite> Sprites;
            public string[] Trees;
            public string[] Clutter;
        }

        private enum Biome { Forest, Plains }

        [MenuItem("Tools/Biome Showroom/Build Action Map")]
        public static void BuildActionMap()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Action map: exit play mode before building.");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            // Create the scene BEFORE loading assets: NewScene(Single) invalidates
            // asset references loaded beforehand, which would null the tiles.
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SceneManager.SetActiveScene(scene);

            Palette forest = LoadPalette(ForestDir,
                new[] { "Tree1", "Tree2", "Tree3", "Tree4", "Tree5", "Tree6", "Tree7", "Tree8" },
                new[] { "Plant1", "Plant2", "Plant3", "Stone1", "Stone2", "Stone3", "Stone4" });
            Palette plains = LoadPalette(PlainsDir,
                new[] { "Tree1", "Tree2", "Tree3", "Tree4", "Tree5" },
                new[] { "Stone1", "Stone2", "Stone3", "Stump1", "Stump2", "Stump3", "Barrel", "Box1", "Box2", "Scarecrow" });

            if (forest.Fill == null || plains.Fill == null)
            {
                Debug.LogError("Action map: could not load tile assets. Aborting.");
                return;
            }

            GameObject gridObject = new GameObject("Grid");
            Grid grid = gridObject.AddComponent<Grid>();
            grid.cellSize = new Vector3(1f, 1f, 0f);

            GameObject forestBiome = NewChild("Forest_Biome", gridObject.transform);
            GameObject plainsBiome = NewChild("Plains_Biome", gridObject.transform);

            Tilemap forestGround = NewTilemap("Forest_Ground", forestBiome.transform, 0);
            Tilemap plainsGround = NewTilemap("Plains_Ground", plainsBiome.transform, 0);
            Tilemap forestSpikes = NewTilemap("Forest_Spikes", forestBiome.transform, 3);
            Tilemap plainsSpikes = NewTilemap("Plains_Spikes", plainsBiome.transform, 3);

            System.Random rng = new System.Random(Seed);

            // --- 1. Height map + biome per column (alternating = balanced blend) ---
            int[] height = new int[LevelWidth];
            Biome[] columnBiome = new Biome[LevelWidth];
            bool[] spikeGround = new bool[LevelWidth];

            int h = 0;
            int x = 0;
            Biome biome = Biome.Plains;
            int biomeRun = 0;

            while (x < LevelWidth)
            {
                if (biomeRun <= 0)
                {
                    biome = biome == Biome.Forest ? Biome.Plains : Biome.Forest;
                    biomeRun = rng.Next(9, 16);
                }

                double roll = rng.NextDouble();

                if (roll < 0.20 && x > 10 && x < LevelWidth - 6)
                {
                    int pitLen = rng.Next(2, 5);
                    int pitFloor = Mathf.Max(Bottom + 2, h - rng.Next(2, 4));;
                    for (int i = 0; i < pitLen && x < LevelWidth; i++)
                    {
                        height[x] = pitFloor;
                        columnBiome[x] = biome;
                        spikeGround[x] = true;
                        x++;
                        biomeRun--;
                    }
                }
                else
                {
                    int segLen = rng.Next(4, 11);
                    for (int i = 0; i < segLen && x < LevelWidth; i++)
                    {
                        height[x] = h;
                        columnBiome[x] = biome;
                        x++;
                        biomeRun--;
                    }
                    int rise = rng.Next(-3, 4); if (rise > 2) rise = 2; h = Mathf.Clamp(h + rise, MinHeight, MaxHeight);
                }
            }

            // --- 2. Combined occupancy (ground body) ---------------------------
            HashSet<Vector3Int> solid = new HashSet<Vector3Int>();
            for (int c = 0; c < LevelWidth; c++)
                for (int y = Bottom; y <= height[c]; y++)
                    solid.Add(new Vector3Int(c, y, 0));

            // --- 3. Floating platforms -----------------------------------------
            List<Vector3Int> platformTops = new List<Vector3Int>();
            List<Biome> platformBiome = new List<Biome>();
            for (int c = 12; c < LevelWidth - 8; c += rng.Next(12, 20))
            {
                int pw = rng.Next(3, 7);
                int baseH = height[c];
                for (int k = 1; k < pw && c + k < LevelWidth; k++) if (height[c + k] > baseH) baseH = height[c + k];
                int py = baseH + 3;   // clearance 2 under, top reachable by double jump
                if (py > MaxHeight + 6) continue;
                // ledge sits 2 cells above the highest ground in its span
                Biome pb = columnBiome[Mathf.Clamp(c, 0, LevelWidth - 1)];
                for (int i = 0; i < pw && c + i < LevelWidth; i++)
                {
                    solid.Add(new Vector3Int(c + i, py, 0));
                    // single-cell ledge: keeps 2 cells of headroom underneath;
                    platformTops.Add(new Vector3Int(c + i, py, 0));
                    platformBiome.Add(pb);
                }
            }

            // --- 4. Autotile paint ---------------------------------------------
            foreach (Vector3Int cell in solid)
            {
                Biome b = CellBiome(cell, columnBiome, platformTops, platformBiome);
                Palette pal = b == Biome.Forest ? forest : plains;
                bool up = !solid.Contains(cell + Vector3Int.up);
                bool left = !solid.Contains(cell + Vector3Int.left);
                bool right = !solid.Contains(cell + Vector3Int.right);
                TileBase tile = PickGround(pal, up, left, right);
                Tilemap map = b == Biome.Forest ? forestGround : plainsGround;
                map.SetTile(cell, tile);
            }

            // --- 5. Spike traps ------------------------------------------------
            for (int c = 0; c < LevelWidth; c++)
            {
                if (!spikeGround[c]) continue;
                PlaceSpike(c, height[c] + 1, columnBiome[c], forest, plains, forestSpikes, plainsSpikes, solid);
            }
            int lastTrap = -10;
            for (int c = 10; c < LevelWidth - 3; c++)
            {
                if (spikeGround[c]) continue;
                bool flatTop = solid.Contains(new Vector3Int(c, height[c], 0)) &&
                               !solid.Contains(new Vector3Int(c, height[c] + 1, 0));
                bool neighboursFlat = height[c - 1] == height[c] && height[c + 1] == height[c];
                if (flatTop && neighboursFlat && c - lastTrap > rng.Next(9, 16) && rng.NextDouble() < 0.7)
                {
                    int cluster = rng.Next(1, 4);
                    for (int i = 0; i < cluster && c + i < LevelWidth - 2; i++)
                        PlaceSpike(c + i, height[c + i] + 1, columnBiome[c + i], forest, plains, forestSpikes, plainsSpikes, solid);
                    lastTrap = c + cluster;
                }
            }
            foreach (Vector3Int ptop in platformTops)
            {
                if (rng.NextDouble() < 0.25)
                {
                    Biome b = CellBiome(ptop, columnBiome, platformTops, platformBiome);
                    PlaceSpike(ptop.x, ptop.y + 1, b, forest, plains, forestSpikes, plainsSpikes, solid);
                }
            }

            // --- 6. Decorative props -------------------------------------------
            GameObject forestProps = NewChild("Props", forestBiome.transform);
            GameObject plainsProps = NewChild("Props", plainsBiome.transform);
            int lastProp = -5;
            for (int c = 2; c < LevelWidth - 2; c++)
            {
                bool surface = !solid.Contains(new Vector3Int(c, height[c] + 1, 0));
                bool hasSpike = HasSpike(c, height[c] + 1, forestSpikes, plainsSpikes);
                if (!surface || hasSpike || c - lastProp < 3) continue;
                if (rng.NextDouble() > 0.30) continue;

                Biome b = columnBiome[c];
                Palette pal = b == Biome.Forest ? forest : plains;
                bool bigTree = rng.NextDouble() < 0.55;
                string[] pool = bigTree ? pal.Trees : pal.Clutter;
                string spriteName = pool[rng.Next(pool.Length)];
                if (!pal.Sprites.TryGetValue(spriteName, out Sprite sprite) || sprite == null) continue;

                float topY = height[c] + 1f;
                Vector3 pos = new Vector3(c + 0.5f - sprite.bounds.center.x, topY - sprite.bounds.min.y, 0f);
                Transform parent = (b == Biome.Forest ? forestProps : plainsProps).transform;
                CreateSprite(spriteName, sprite, parent, pos, 5);
                lastProp = c;
            }

            // --- 7. Parallax backgrounds ---------------------------------------
            BuildBackgrounds(columnBiome, forest, plains, forestBiome.transform, plainsBiome.transform);

            // --- 8. Colliders --------------------------------------------------
            AddGroundCollider(forestGround);
            AddGroundCollider(plainsGround);
            AddSpikeCollider(forestSpikes);
            AddSpikeCollider(plainsSpikes);

            // --- 9. Player, checkpoints, camera --------------------------------
            int spawnColumn = FindSafeColumn(2, height, spikeGround, forestSpikes, plainsSpikes, solid);
            Vector3 spawnPosition = new Vector3(spawnColumn + 0.5f, height[spawnColumn] + 1.05f, 0f);

            GameObject player = SpawnPlayer(spawnPosition);
            int checkpointCount = BuildCheckpoints(height, spikeGround, forestSpikes, plainsSpikes, solid, spawnColumn);

            CreateCamera(forestGround, plainsGround, player);

            int trapCount = BiomeTrapPlacer.PlaceTraps();;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();

            Debug.Log("Action map built: width " + LevelWidth +
                      ", player spawn x=" + spawnColumn +
                      ", checkpoints=" + checkpointCount + ", traps=" + trapCount +
                      ", scene " + ScenePath);
        }

        [MenuItem("Tools/Biome Showroom/Open Action Map")]
        public static void OpenActionMap()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                BuildActionMap();
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        // ------------------------------------------------------------- player

        private static GameObject SpawnPlayer(Vector3 position)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (prefab == null)
            {
                Debug.LogError("Action map: player prefab not found at " + PlayerPrefabPath);
                return null;
            }

            GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            player.name = "Player";
            player.transform.position = position;
            player.transform.rotation = Quaternion.identity;

            if (player.GetComponent<PlayerHealth>() == null)
                player.AddComponent<PlayerHealth>();

            if (player.GetComponent<PlayerParry>() == null)
                player.AddComponent<PlayerParry>();

            SwordSlashSpawner slash = player.GetComponent<SwordSlashSpawner>();
            if (slash == null) slash = player.AddComponent<SwordSlashSpawner>();
            slash.Configure(
                LoadSlashFrames("Assets/Art/FX/Slash_BASIC.png"),
                LoadSlashFrames("Assets/Art/FX/Slash_POWERED.png"));

            return player;
        }

        /// <summary>Loads the sliced frames of an effect strip in frame order.</summary>
        private static Sprite[] LoadSlashFrames(string path)
        {
            List<Sprite> frames = new List<Sprite>();
            foreach (Object rep in AssetDatabase.LoadAllAssetRepresentationsAtPath(path))
            {
                Sprite sprite = rep as Sprite;
                if (sprite != null) frames.Add(sprite);
            }
            frames.Sort(delegate (Sprite a, Sprite b)
            {
                return FrameIndex(a.name).CompareTo(FrameIndex(b.name));
            });
            return frames.ToArray();
        }

        private static int FrameIndex(string name)
        {
            int underscore = name.LastIndexOf('_');
            int value;
            if (underscore >= 0 && int.TryParse(name.Substring(underscore + 1), out value)) return value;
            return 0;
        }

        private static int BuildCheckpoints(
            int[] height, bool[] spikeGround,
            Tilemap forestSpikes, Tilemap plainsSpikes,
            HashSet<Vector3Int> solid, int spawnColumn)
        {
            GameObject root = new GameObject("Checkpoints");
            int created = 0;
            int next = spawnColumn + 22;

            while (next < LevelWidth - 6)
            {
                int column = FindSafeColumn(next, height, spikeGround, forestSpikes, plainsSpikes, solid);
                if (column < 0) break;

                GameObject checkpoint = new GameObject("Checkpoint_" + (created + 1));
                checkpoint.transform.SetParent(root.transform, false);
                checkpoint.transform.position =
                    new Vector3(column + 0.5f, height[column] + 1.05f, 0f);

                BoxCollider2D box = checkpoint.AddComponent<BoxCollider2D>();
                box.isTrigger = true;
                box.size = new Vector2(1.4f, 3f);
                box.offset = new Vector2(0f, 1.4f);
                checkpoint.AddComponent<Checkpoint2D>();

                created++;
                next = column + 22;
            }

            return created;
        }

        /// <summary>Finds the next flat, spike-free surface column at or after <paramref name="from"/>.</summary>
        private static int FindSafeColumn(
            int from, int[] height, bool[] spikeGround,
            Tilemap forestSpikes, Tilemap plainsSpikes, HashSet<Vector3Int> solid)
        {
            for (int c = Mathf.Max(1, from); c < LevelWidth - 2; c++)
            {
                if (spikeGround[c]) continue;
                if (height[c - 1] != height[c] || height[c + 1] != height[c]) continue;

                int top = height[c] + 1;
                if (solid.Contains(new Vector3Int(c, top, 0))) continue;          // headroom
                if (solid.Contains(new Vector3Int(c, top + 1, 0))) continue;
                if (HasSpike(c, top, forestSpikes, plainsSpikes)) continue;
                if (HasSpike(c - 1, height[c - 1] + 1, forestSpikes, plainsSpikes)) continue;
                if (HasSpike(c + 1, height[c + 1] + 1, forestSpikes, plainsSpikes)) continue;

                return c;
            }
            return -1;
        }

        // ------------------------------------------------------------- helpers

        private static Biome CellBiome(
            Vector3Int cell, Biome[] columnBiome,
            List<Vector3Int> platformTops, List<Biome> platformBiome)
        {
            for (int i = 0; i < platformTops.Count; i++)
                if (platformTops[i].x == cell.x && Mathf.Abs(platformTops[i].y - cell.y) <= 1)
                    return platformBiome[i];

            int cx = Mathf.Clamp(cell.x, 0, columnBiome.Length - 1);
            return columnBiome[cx];
        }

        private static TileBase PickGround(Palette pal, bool up, bool left, bool right)
        {
            if (up && left && !right) return pal.TopLeft;
            if (up && right && !left) return pal.TopRight;
            if (up) return pal.Top;
            return pal.Fill;
        }

        private static void PlaceSpike(
            int cx, int cy, Biome biome, Palette forest, Palette plains,
            Tilemap forestSpikes, Tilemap plainsSpikes, HashSet<Vector3Int> solid)
        {
            if (solid.Contains(new Vector3Int(cx, cy, 0))) return;
            Palette pal = biome == Biome.Forest ? forest : plains;
            if (pal.Spike == null) return;
            Tilemap map = biome == Biome.Forest ? forestSpikes : plainsSpikes;
            map.SetTile(new Vector3Int(cx, cy, 0), pal.Spike);
        }

        private static bool HasSpike(int cx, int cy, Tilemap forestSpikes, Tilemap plainsSpikes)
        {
            Vector3Int p = new Vector3Int(cx, cy, 0);
            return forestSpikes.GetTile(p) != null || plainsSpikes.GetTile(p) != null;
        }

        private static void BuildBackgrounds(
            Biome[] columnBiome, Palette forest, Palette plains,
            Transform forestParent, Transform plainsParent)
        {
            GameObject forestBackground = NewChild("Background", forestParent);
            GameObject plainsBackground = NewChild("Background", plainsParent);

            for (int slotX = -8; slotX < LevelWidth + 8; slotX += 16)
            {
                int sampleColumn = Mathf.Clamp(slotX + 8, 0, LevelWidth - 1);
                Biome b = columnBiome[sampleColumn];
                Palette pal = b == Biome.Forest ? forest : plains;
                Transform parent = (b == Biome.Forest ? forestBackground : plainsBackground).transform;

                for (int layer = 1; layer <= 5; layer++)
                {
                    if (!pal.Sprites.TryGetValue("Background" + layer, out Sprite sprite) || sprite == null)
                        continue;
                    Vector3 pos = new Vector3(slotX, -4f + layer * 0.35f, 0f);
                    CreateSprite("Background" + layer, sprite, parent, pos, -20 + layer);
                }
            }
        }

        private static void AddGroundCollider(Tilemap map)
        {
            Rigidbody2D body = map.gameObject.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Static;
            TilemapCollider2D collider = map.gameObject.AddComponent<TilemapCollider2D>();
            collider.usedByComposite = true;
            CompositeCollider2D composite = map.gameObject.AddComponent<CompositeCollider2D>();
            composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
        }

        private static void AddSpikeCollider(Tilemap map)
        {
            TilemapCollider2D collider = map.gameObject.AddComponent<TilemapCollider2D>();
            collider.isTrigger = true;
        }

        private static void CreateCamera(Tilemap forestGround, Tilemap plainsGround, GameObject player)
        {
            forestGround.CompressBounds();
            plainsGround.CompressBounds();
            Bounds bounds = forestGround.localBounds;
            bounds.Encapsulate(plainsGround.localBounds);

            float aspect = 16f / 9f;
            float fullSize = Mathf.Clamp(
                Mathf.Max(bounds.extents.y, bounds.extents.x / aspect) * 1.05f, 6f, 70f);

            List<Vector3> positions = new List<Vector3>();
            List<float> sizes = new List<float>();
            List<string> names = new List<string>();

            positions.Add(new Vector3(bounds.center.x, Mathf.Max(bounds.center.y, 0f), CameraDepth));
            sizes.Add(fullSize);
            names.Add("Overview");

            float[] frac = { 0.12f, 0.38f, 0.63f, 0.88f };
            string[] labels = { "Start", "Zone 2", "Zone 3", "Finish" };
            for (int i = 0; i < frac.Length; i++)
            {
                positions.Add(new Vector3(bounds.min.x + bounds.size.x * frac[i], 2f, CameraDepth));
                sizes.Add(11f);
                names.Add(labels[i]);
            }

            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.53f, 0.68f, 0.75f, 1f);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 200f;
            camera.orthographicSize = sizes[0];
            cameraObject.transform.position = positions[0];
            cameraObject.AddComponent<AudioListener>();

            MapViewerHoverController controller = cameraObject.AddComponent<MapViewerHoverController>();
            controller.Configure(positions.ToArray(), sizes.ToArray(), names.ToArray());

            if (player != null)
                controller.SetFollowTarget(player.transform, PlayCameraSize);
        }

        private static Palette LoadPalette(string dir, string[] trees, string[] clutter)
        {
            Palette pal = new Palette
            {
                Top = AssetDatabase.LoadAssetAtPath<TileBase>(dir + "Tilemap/TileGround2.asset"),
                TopLeft = AssetDatabase.LoadAssetAtPath<TileBase>(dir + "Tilemap/TileGround1.asset"),
                TopRight = AssetDatabase.LoadAssetAtPath<TileBase>(dir + "Tilemap/TileGround3.asset"),
                Fill = AssetDatabase.LoadAssetAtPath<TileBase>(dir + "Tilemap/TileGround5.asset"),
                Spike = AssetDatabase.LoadAssetAtPath<TileBase>(dir + "Tilemap/TileSpikes.asset"),
                Sprites = new Dictionary<string, Sprite>(),
                Trees = trees,
                Clutter = clutter
            };

            foreach (Object rep in AssetDatabase.LoadAllAssetRepresentationsAtPath(dir + "Sprites.png"))
            {
                if (rep is Sprite sprite && !pal.Sprites.ContainsKey(sprite.name))
                    pal.Sprites.Add(sprite.name, sprite);
            }
            return pal;
        }

        private static GameObject NewChild(string name, Transform parent)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go;
        }

        private static Tilemap NewTilemap(string name, Transform parent, int sortingOrder)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Tilemap map = go.AddComponent<Tilemap>();
            TilemapRenderer renderer = go.AddComponent<TilemapRenderer>();
            renderer.sortingOrder = sortingOrder;
            return map;
        }

        private static void CreateSprite(
            string name, Sprite sprite, Transform parent, Vector3 position, int sortingOrder)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
        }
    }
}
#endif
