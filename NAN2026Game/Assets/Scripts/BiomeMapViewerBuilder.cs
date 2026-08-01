#if UNITY_EDITOR
using System.Collections.Generic;
using NAN2026.Showroom;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NAN2026.Showroom.Editor
{
    /// <summary>
    /// Assembles a single, browsable map scene from the two biome demo scenes
    /// (American Forest + Plains) placed side by side, and wires up a hover
    /// inspector camera so every tile / prop / background reveals its asset name.
    /// </summary>
    public static class BiomeMapViewerBuilder
    {
        private const string ScenePath = "Assets/Map/Showroom/BiomeMap.unity";
        private const float CameraDepth = -10f;

        private readonly struct BiomeSource
        {
            public readonly string DisplayName;
            public readonly string DemoScenePath;
            public readonly float OffsetX;

            public BiomeSource(string displayName, string demoScenePath, float offsetX)
            {
                DisplayName = displayName;
                DemoScenePath = demoScenePath;
                OffsetX = offsetX;
            }
        }

        private static readonly BiomeSource[] Sources =
        {
            new BiomeSource("Forest", "Assets/2D Pixel Art Platformer Biome - American Forest/Demo.unity", 0f),
            new BiomeSource("Plains", "Assets/2D Pixel Art Platformer Biome - Plains/Demo.unity", 75f)
        };

        [MenuItem("Tools/Biome Showroom/Build Map Viewer")]
        public static void BuildMapViewer()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Biome map viewer: exit play mode before building.");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            Scene target = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SceneManager.SetActiveScene(target);

            List<Vector3> sectionPositions = new List<Vector3>();
            List<float> sectionSizes = new List<float>();
            List<string> sectionNames = new List<string>();

            foreach (BiomeSource source in Sources)
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(source.DemoScenePath) == null)
                {
                    Debug.LogError("Biome map viewer: demo scene not found: " + source.DemoScenePath);
                    continue;
                }

                Scene demo = EditorSceneManager.OpenScene(source.DemoScenePath, OpenSceneMode.Additive);

                GameObject container = new GameObject(source.DisplayName + "_Biome");
                SceneManager.MoveGameObjectToScene(container, target);
                container.transform.position = Vector3.zero;

                foreach (GameObject root in demo.GetRootGameObjects())
                {
                    // Drop each demo's own camera / audio listener; the viewer supplies its own.
                    if (root.GetComponent<Camera>() != null)
                        continue;

                    SceneManager.MoveGameObjectToScene(root, target);
                    root.transform.SetParent(container.transform, true);
                }

                container.transform.position = new Vector3(source.OffsetX, 0f, 0f);
                EditorSceneManager.CloseScene(demo, true);

                if (TryGetContentBounds(container, out Bounds bounds))
                {
                    const float aspect = 16f / 9f;
                    float byHeight = bounds.extents.y;
                    float byWidth = bounds.extents.x / aspect;
                    float size = Mathf.Clamp(Mathf.Max(byHeight, byWidth) * 1.06f, 4f, 60f);

                    sectionPositions.Add(new Vector3(bounds.center.x, bounds.center.y, CameraDepth));
                    sectionSizes.Add(size);
                }
                else
                {
                    sectionPositions.Add(new Vector3(source.OffsetX, 0f, CameraDepth));
                    sectionSizes.Add(12f);
                }
                sectionNames.Add(source.DisplayName);
            }

            CreateCamera(sectionPositions, sectionSizes, sectionNames);

            EditorSceneManager.SaveScene(target, ScenePath);
            AssetDatabase.SaveAssets();

            if (SceneView.lastActiveSceneView != null && sectionPositions.Count > 0)
            {
                SceneView.lastActiveSceneView.pivot = sectionPositions[0];
                SceneView.lastActiveSceneView.size = sectionSizes[0] + 2f;
                SceneView.lastActiveSceneView.Repaint();
            }

            Debug.Log("Biome map viewer built: " + sectionNames.Count + " biomes. Scene: " + ScenePath);
        }

        [MenuItem("Tools/Biome Showroom/Open Map Viewer")]
        public static void OpenMapViewer()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                BuildMapViewer();
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        private static void CreateCamera(
            List<Vector3> positions, List<float> sizes, List<string> names)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";

            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.53f, 0.68f, 0.75f, 1f);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 200f;
            camera.orthographicSize = sizes.Count > 0 ? sizes[0] : 12f;
            cameraObject.transform.position = positions.Count > 0
                ? positions[0]
                : new Vector3(0f, 0f, CameraDepth);
            cameraObject.AddComponent<AudioListener>();

            MapViewerHoverController controller =
                cameraObject.AddComponent<MapViewerHoverController>();
            controller.Configure(positions.ToArray(), sizes.ToArray(), names.ToArray());
        }

        private static bool TryGetContentBounds(GameObject root, out Bounds bounds)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            List<Renderer> relevant = new List<Renderer>();
            foreach (Renderer r in renderers)
            {
                // Skip parallax backgrounds so the framing focuses on the playable terrain.
                if (IsBackground(r.transform)) continue;
                relevant.Add(r);
            }

            if (relevant.Count == 0)
            {
                bounds = default;
                return false;
            }

            bounds = relevant[0].bounds;
            for (int i = 1; i < relevant.Count; i++)
                bounds.Encapsulate(relevant[i].bounds);
            return true;
        }

        private static bool IsBackground(Transform t)
        {
            while (t != null)
            {
                if (t.name.StartsWith("Background")) return true;
                t = t.parent;
            }
            return false;
        }
    }
}
#endif
