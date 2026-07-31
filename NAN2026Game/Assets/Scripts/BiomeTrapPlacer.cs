#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using NAN2026.Showroom;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace NAN2026.Showroom.Editor
{
    /// <summary>
    /// Places Cat-Mario style traps into the current action map scene.
    /// Every trap sits at a FIXED position derived from the terrain - never random -
    /// so each replay is identical and the level is learnable. Traps are put on
    /// ground that looks completely safe.
    /// </summary>
    public static class BiomeTrapPlacer
    {
        private const string ForestDir = "Assets/2D Pixel Art Platformer Biome - American Forest/";
        private const string PlainsDir = "Assets/2D Pixel Art Platformer Biome - Plains/";

        private const int PopUpSpikeStart = 14;
        private const int PopUpSpikeStep = 17;
        private const int FallingBlockStart = 24;
        private const int FallingBlockStep = 23;
        private const float FallingBlockHeight = 4.5f;

        [MenuItem("Tools/Biome Showroom/Add Cat-Mario Traps")]
        public static void AddTrapsMenu()
        {
            int count = PlaceTraps();
            Debug.Log("Cat-Mario traps placed: " + count);
        }

        public static int PlaceTraps()
        {
            Sprite forestSpikeSprite = SpriteOfTile(ForestDir + "Tilemap/TileSpikes.asset");
            Sprite plainsSpikeSprite = SpriteOfTile(PlainsDir + "Tilemap/TileSpikes.asset");
            Sprite blockSprite = SpriteOfTile(ForestDir + "Tilemap/TileGround5.asset");

            if (forestSpikeSprite == null || blockSprite == null)
            {
                Debug.LogError("Trap placer: could not load tile sprites.");
                return 0;
            }

            List<Tilemap> groundMaps = new List<Tilemap>();
            List<Tilemap> spikeMaps = new List<Tilemap>();
            foreach (Tilemap map in Object.FindObjectsByType<Tilemap>(
                FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (map.name.EndsWith("_Ground")) groundMaps.Add(map);
                else if (map.name.EndsWith("_Spikes")) spikeMaps.Add(map);
            }

            if (groundMaps.Count == 0)
            {
                Debug.LogError("Trap placer: no *_Ground tilemaps in the scene.");
                return 0;
            }

            HashSet<Vector2Int> solid = new HashSet<Vector2Int>();
            int maxX = int.MinValue;
            foreach (Tilemap map in groundMaps)
            {
                map.CompressBounds();
                foreach (Vector3Int cell in map.cellBounds.allPositionsWithin)
                {
                    if (map.GetTile(cell) == null) continue;
                    solid.Add(new Vector2Int(cell.x, cell.y));
                    if (cell.x > maxX) maxX = cell.x;
                }
            }

            HashSet<Vector2Int> spikes = new HashSet<Vector2Int>();
            foreach (Tilemap map in spikeMaps)
            {
                map.CompressBounds();
                foreach (Vector3Int cell in map.cellBounds.allPositionsWithin)
                    if (map.GetTile(cell) != null)
                        spikes.Add(new Vector2Int(cell.x, cell.y));
            }

            int width = maxX + 1;
            int[] surface = new int[width + 2];
            for (int x = 0; x <= width + 1; x++)
            {
                int y = -12;
                if (!solid.Contains(new Vector2Int(x, y))) { surface[x] = int.MinValue; continue; }
                while (solid.Contains(new Vector2Int(x, y + 1))) y++;
                surface[x] = y + 1;
            }

            GameObject old = GameObject.Find("Traps");
            if (old != null) Object.DestroyImmediate(old);
            GameObject root = new GameObject("Traps");

            int placed = 0;
            placed += PlacePopUpSpikes(root.transform, surface, solid, spikes, width,
                forestSpikeSprite, plainsSpikeSprite);
            placed += PlaceFallingBlocks(root.transform, surface, solid, spikes, width, blockSprite);
            placed += PlaceHiddenPitfalls(root.transform, surface, spikes, width, groundMaps, spikeMaps);
            placed += PlaceFallingTrees();
            placed += PlaceOrbEmitters(surface, solid, spikes, width);

            foreach (Tilemap map in groundMaps)
            {
                CompositeCollider2D composite = map.GetComponent<CompositeCollider2D>();
                if (composite != null) composite.GenerateGeometry();
            }

            return placed;
        }

        // ---------------------------------------------------------------- traps

        private static int PlacePopUpSpikes(
            Transform parent, int[] surface, HashSet<Vector2Int> solid,
            HashSet<Vector2Int> spikes, int width, Sprite forestSpike, Sprite plainsSpike)
        {
            int placed = 0;
            for (int seed = PopUpSpikeStart; seed < width - 6; seed += PopUpSpikeStep)
            {
                int column = FindInnocentColumn(seed, surface, solid, spikes, width);
                if (column < 0) continue;

                float surfaceY = surface[column];
                Sprite sprite = (placed % 2 == 0 && plainsSpike != null) ? plainsSpike : forestSpike;

                GameObject trap = new GameObject("PopUpSpikeTrap_" + column);
                trap.transform.SetParent(parent, false);
                trap.transform.position = new Vector3(column + 0.5f, surfaceY, 0f);

                BoxCollider2D zone = trap.AddComponent<BoxCollider2D>();
                zone.isTrigger = true;
                zone.size = new Vector2(1.3f, 2.2f);
                zone.offset = new Vector2(0f, 1.1f);

                GameObject spike = new GameObject("PopUpSpikes");
                spike.transform.SetParent(trap.transform, false);
                spike.transform.localPosition = new Vector3(0f, -0.5f, 0f);

                SpriteRenderer renderer = spike.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sortingOrder = -2;           // hidden behind the ground tiles

                BoxCollider2D hitbox = spike.AddComponent<BoxCollider2D>();
                hitbox.isTrigger = true;
                hitbox.size = new Vector2(0.85f, 0.8f);
                hitbox.enabled = false;
                spike.AddComponent<Hazard2D>();

                PopUpSpikeTrap logic = trap.AddComponent<PopUpSpikeTrap>();
                logic.Configure(spike.transform, hitbox, renderer, -0.5f, 0.5f);

                placed++;
            }
            return placed;
        }

        private static int PlaceFallingBlocks(
            Transform parent, int[] surface, HashSet<Vector2Int> solid,
            HashSet<Vector2Int> spikes, int width, Sprite blockSprite)
        {
            int placed = 0;
            for (int seed = FallingBlockStart; seed < width - 6; seed += FallingBlockStep)
            {
                int column = FindInnocentColumn(seed, surface, solid, spikes, width);
                if (column < 0) continue;

                float surfaceY = surface[column];

                bool clear = true;
                for (int y = 1; y <= 6; y++)
                    if (solid.Contains(new Vector2Int(column, surface[column] + y))) { clear = false; break; }
                if (!clear) continue;

                GameObject trap = new GameObject("FallingBlockTrap_" + column);
                trap.transform.SetParent(parent, false);
                trap.transform.position = new Vector3(column + 0.5f, surfaceY, 0f);

                BoxCollider2D zone = trap.AddComponent<BoxCollider2D>();
                zone.isTrigger = true;
                zone.size = new Vector2(1.5f, 2.4f);
                zone.offset = new Vector2(0f, 1.2f);

                GameObject block = new GameObject("FallingBlock");
                block.transform.SetParent(trap.transform, false);
                block.transform.localPosition = new Vector3(0f, FallingBlockHeight, 0f);

                SpriteRenderer renderer = block.AddComponent<SpriteRenderer>();
                renderer.sprite = blockSprite;
                renderer.sortingOrder = 7;

                BoxCollider2D collider = block.AddComponent<BoxCollider2D>();
                collider.isTrigger = true;
                collider.size = new Vector2(0.95f, 0.95f);
                Hazard2D hazard = block.AddComponent<Hazard2D>();

                FallingBlockTrap logic = trap.AddComponent<FallingBlockTrap>();
                logic.Configure(block.transform, collider, hazard, surfaceY + 0.5f);

                placed++;
            }
            return placed;
        }

        /// <summary>
        /// Fills a spike pit with fake ground painted into the real terrain tilemap,
        /// using the neighbouring biome's own tiles so there is no visible seam.
        /// </summary>
        private static int PlaceHiddenPitfalls(
            Transform parent, int[] surface, HashSet<Vector2Int> spikes, int width,
            List<Tilemap> groundMaps, List<Tilemap> spikeMaps)
        {
            int placed = 0;
            int x = 2;

            while (x < width - 3)
            {
                bool isPit = surface[x] != int.MinValue &&
                             spikes.Contains(new Vector2Int(x, surface[x]));
                if (!isPit) { x++; continue; }

                int start = x;
                while (x < width - 3 &&
                       surface[x] != int.MinValue &&
                       spikes.Contains(new Vector2Int(x, surface[x])))
                    x++;
                int end = x - 1;

                int leftSurface = surface[Mathf.Max(0, start - 1)];
                int rightSurface = surface[Mathf.Min(width + 1, end + 1)];
                if (leftSurface == int.MinValue || rightSurface == int.MinValue) continue;

                // Only disguise a pit whose rim is level on both sides - otherwise the
                // patched floor would form a visible step and give itself away.
                if (leftSurface != rightSurface) continue;

                int bridgeY = leftSurface;
                if (bridgeY - surface[start] < 2) continue;

                Tilemap rimMap = MapOwningCell(groundMaps, new Vector3Int(start - 1, bridgeY - 1, 0));
                if (rimMap == null) continue;

                TileBase rimTop = TopTileFor(rimMap);
                TileBase rimFill = FillTileFor(rimMap);
                if (rimTop == null || rimFill == null) continue;

                List<Tilemap> maps = new List<Tilemap>();
                List<Vector3Int> cells = new List<Vector3Int>();
                List<TileBase> intact = new List<TileBase>();
                List<TileBase> collapsed = new List<TileBase>();

                // 1. Bury the pit floor's grass row, otherwise a green line shows in the dirt.
                for (int cx = start; cx <= end; cx++)
                {
                    Vector3Int floorCell = new Vector3Int(cx, surface[cx] - 1, 0);
                    Tilemap owner = MapOwningCell(groundMaps, floorCell);
                    if (owner == null) continue;

                    TileBase original = owner.GetTile(floorCell);
                    TileBase fill = FillTileFor(owner);
                    if (original == null || fill == null || original == fill) continue;

                    maps.Add(owner);
                    cells.Add(floorCell);
                    intact.Add(fill);
                    collapsed.Add(original);
                }

                // 2. Fill the pit itself with the rim's own tiles.
                for (int cx = start; cx <= end; cx++)
                {
                    for (int cy = surface[cx]; cy <= bridgeY - 1; cy++)
                    {
                        maps.Add(rimMap);
                        cells.Add(new Vector3Int(cx, cy, 0));
                        intact.Add(cy == bridgeY - 1 ? rimTop : rimFill);
                        collapsed.Add(null);
                    }
                }

                // 3. Flatten the rim corner tiles so no seam is drawn down either side.
                int[] rimColumns = { start - 1, end + 1 };
                foreach (int rimX in rimColumns)
                {
                    Vector3Int rimCell = new Vector3Int(rimX, bridgeY - 1, 0);
                    Tilemap owner = MapOwningCell(groundMaps, rimCell);
                    if (owner == null) continue;

                    TileBase original = owner.GetTile(rimCell);
                    TileBase top = TopTileFor(owner);
                    if (original == null || top == null || original == top) continue;

                    maps.Add(owner);
                    cells.Add(rimCell);
                    intact.Add(top);
                    collapsed.Add(original);
                }

                if (cells.Count == 0) continue;

                // Lift the spikes out while the floor covers them; they come back on collapse.
                List<Vector3Int> spikeCells = new List<Vector3Int>();
                List<TileBase> spikeTiles = new List<TileBase>();
                Tilemap spikeMap = null;
                for (int cx = start; cx <= end; cx++)
                {
                    Vector3Int cell = new Vector3Int(cx, surface[cx], 0);
                    foreach (Tilemap candidate in spikeMaps)
                    {
                        TileBase tile = candidate.GetTile(cell);
                        if (tile == null) continue;
                        spikeMap = candidate;
                        spikeCells.Add(cell);
                        spikeTiles.Add(tile);
                        break;
                    }
                }

                int span = end - start + 1;
                GameObject trap = new GameObject("HiddenPitfall_" + start);
                trap.transform.SetParent(parent, false);
                trap.transform.position = new Vector3(start + span * 0.5f, bridgeY + 0.6f, 0f);

                BoxCollider2D zone = trap.AddComponent<BoxCollider2D>();
                zone.isTrigger = true;
                zone.size = new Vector2(span, 1.2f);

                VanishingPlatformTrap logic = trap.AddComponent<VanishingPlatformTrap>();
                logic.Configure(maps.ToArray(), cells.ToArray(), intact.ToArray(), collapsed.ToArray(),
                    spikeMap, spikeCells.ToArray(), spikeTiles.ToArray());

                placed++;
            }

            return placed;
        }

        private static TileBase TopTileFor(Tilemap map)
        {
            string dir = map.name.StartsWith("Forest") ? ForestDir : PlainsDir;
            return AssetDatabase.LoadAssetAtPath<TileBase>(dir + "Tilemap/TileGround2.asset");
        }

        private static TileBase FillTileFor(Tilemap map)
        {
            string dir = map.name.StartsWith("Forest") ? ForestDir : PlainsDir;
            return AssetDatabase.LoadAssetAtPath<TileBase>(dir + "Tilemap/TileGround5.asset");
        }

        /// <summary>
        /// Turns some of the existing scenery trees into ambushes: a pivot is inserted at
        /// the trunk base so the tree can hinge over onto the player who walks past it.
        /// </summary>
        private static int PlaceFallingTrees()
        {
            List<SpriteRenderer> trees = new List<SpriteRenderer>();
            foreach (SpriteRenderer renderer in Object.FindObjectsByType<SpriteRenderer>(
                FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (renderer.sprite == null) continue;
                if (!renderer.gameObject.name.StartsWith("Tree")) continue;
                if (renderer.sprite.bounds.size.y < 4f) continue;          // skip bushes
                if (renderer.GetComponentInParent<FallingTreeTrap>() != null) continue;
                trees.Add(renderer);
            }

            trees.Sort(delegate (SpriteRenderer a, SpriteRenderer b)
            {
                return a.transform.position.x.CompareTo(b.transform.position.x);
            });

            int placed = 0;
            for (int i = 1; i < trees.Count && placed < 6; i += 3)
            {
                SpriteRenderer tree = trees[i];
                Transform originalParent = tree.transform.parent;

                Bounds world = tree.bounds;
                Vector3 basePoint = new Vector3(world.center.x, world.min.y, 0f);

                GameObject root = new GameObject("FallingTreeTrap_" + Mathf.RoundToInt(basePoint.x));
                root.transform.SetParent(originalParent, true);
                root.transform.position = basePoint;

                BoxCollider2D zone = root.AddComponent<BoxCollider2D>();
                zone.isTrigger = true;
                zone.size = new Vector2(6.5f, 3f);
                zone.offset = new Vector2(0f, 1.5f);

                GameObject pivot = new GameObject("TreePivot");
                pivot.transform.SetParent(root.transform, false);
                pivot.transform.localPosition = Vector3.zero;

                tree.transform.SetParent(pivot.transform, true);

                Bounds local = tree.sprite.bounds;
                BoxCollider2D hitbox = tree.gameObject.AddComponent<BoxCollider2D>();
                hitbox.isTrigger = true;
                hitbox.size = new Vector2(local.size.x * 0.45f, local.size.y * 0.62f);
                hitbox.offset = new Vector2(local.center.x, local.center.y + local.size.y * 0.17f);
                hitbox.enabled = false;
                Hazard2D hazard = tree.gameObject.AddComponent<Hazard2D>();

                FallingTreeTrap logic = root.AddComponent<FallingTreeTrap>();
                logic.Configure(pivot.transform, hitbox, hazard);

                placed++;
            }

            return placed;
        }


        /// <summary>Boss stand-ins that shoot orbs, for practising the parry timing.</summary>
        private static int PlaceOrbEmitters(
            int[] surface, HashSet<Vector2Int> solid, HashSet<Vector2Int> spikes, int width)
        {
            Sprite orb = LoadOrCreateOrbSprite();
            if (orb == null) return 0;

            int placed = 0;
            int[] seeds = { 34, 96, 150 };
            foreach (int seed in seeds)
            {
                int column = FindInnocentColumn(seed, surface, solid, spikes, width);
                if (column < 0) continue;

                GameObject emitter = new GameObject("OrbEmitter_" + column);
                emitter.transform.position = new Vector3(column + 0.5f, surface[column] + 3.4f, 0f);

                SpriteRenderer renderer = emitter.AddComponent<SpriteRenderer>();
                renderer.sprite = orb;
                renderer.color = new Color(0.75f, 0.45f, 1f);
                renderer.sortingOrder = 11;
                emitter.transform.localScale = Vector3.one * 1.8f;

                OrbEmitter logic = emitter.AddComponent<OrbEmitter>();
                logic.Configure(orb, 1.5f, 6f, 13f);

                placed++;
            }
            return placed;
        }

        /// <summary>Draws a small glowing ball as a sprite asset, so no art import is needed.</summary>
        private static Sprite LoadOrCreateOrbSprite()
        {
            const string folder = "Assets/Art/Generated";
            const string path = folder + "/Orb.png";

            Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null) return existing;

            if (!AssetDatabase.IsValidFolder("Assets/Art"))
                AssetDatabase.CreateFolder("Assets", "Art");
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets/Art", "Generated");

            const int size = 16;
            const float radius = 6.4f;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 centre = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), centre);
                    if (distance > radius)
                    {
                        texture.SetPixel(x, y, new Color(0f, 0f, 0f, 0f));
                        continue;
                    }

                    float t = Mathf.Clamp01(distance / radius);
                    Color colour = Color.Lerp(
                        new Color(1f, 0.96f, 0.62f),
                        new Color(0.95f, 0.33f, 0.14f),
                        t * t);

                    if (distance > radius - 1.3f)
                        colour = Color.Lerp(colour, new Color(0.35f, 0.09f, 0.05f), 0.65f);

                    texture.SetPixel(x, y, colour);
                }
            }
            texture.Apply();

            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.filterMode = FilterMode.Point;
                importer.spritePixelsPerUnit = 16f;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // -------------------------------------------------------------- helpers

        private static Tilemap MapOwningCell(List<Tilemap> maps, Vector3Int cell)
        {
            foreach (Tilemap map in maps)
                if (map.GetTile(cell) != null)
                    return map;
            return null;
        }

        /// <summary>Flat, spike-free, open ground - the kind a player stops worrying about.</summary>
        private static int FindInnocentColumn(
            int from, int[] surface, HashSet<Vector2Int> solid,
            HashSet<Vector2Int> spikes, int width)
        {
            for (int c = Mathf.Max(2, from); c < width - 3 && c < from + 12; c++)
            {
                if (surface[c] == int.MinValue) continue;
                if (surface[c - 1] != surface[c] || surface[c + 1] != surface[c]) continue;
                if (spikes.Contains(new Vector2Int(c, surface[c]))) continue;
                if (spikes.Contains(new Vector2Int(c - 1, surface[c - 1]))) continue;
                if (spikes.Contains(new Vector2Int(c + 1, surface[c + 1]))) continue;
                if (solid.Contains(new Vector2Int(c, surface[c]))) continue;
                if (solid.Contains(new Vector2Int(c, surface[c] + 1))) continue;
                return c;
            }
            return -1;
        }

        private static Sprite SpriteOfTile(string path)
        {
            Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            return tile != null ? tile.sprite : null;
        }
    }
}
#endif
