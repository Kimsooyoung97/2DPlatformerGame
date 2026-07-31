#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NAN2026.Showroom.Editor
{
    public sealed class NAN2026ProjectHubWindow : EditorWindow
    {
        private const string PlayerScenePath = "Assets/Scenes/PlayerTest.unity";
        private const string ShowroomScenePath = "Assets/Map/Showroom/PlatformerSet1Showroom.unity";
        private const string PlatformerAssetsPath = "Assets/PlatformerSet1";
        private const string MainTilesPath = PlatformerAssetsPath + "/main_lev_build.png";

[MenuItem("NAN2026/Project Hub", false, 1)]
        public static void ShowWindow()
        {
            NAN2026ProjectHubWindow window = GetWindow<NAN2026ProjectHubWindow>();
            window.titleContent = new GUIContent("NAN2026 Hub");
            window.minSize = new Vector2(420f, 500f);
            window.Show();
        }

        [MenuItem("NAN2026/Open Player Scene", false, 20)]
        public static void OpenPlayerScene()
        {
            OpenSceneSafely(PlayerScenePath, "Player scene");
        }

        [MenuItem("NAN2026/Open Map Asset Showroom", false, 21)]
        public static void OpenShowroomScene()
        {
            OpenSceneSafely(ShowroomScenePath, "Map asset showroom");
        }

        [MenuItem("NAN2026/Select PlatformerSet1 Assets", false, 40)]
        public static void SelectPlatformerAssets()
        {
            SelectAssetInProject(PlatformerAssetsPath);
        }

        [MenuItem("NAN2026/Select Main Tile Sheet", false, 41)]
        public static void SelectMainTileSheet()
        {
            SelectAssetInProject(MainTilesPath);
        }

[MenuItem("NAN2026/Add Selected Sprite To Sample Map", false, 60)]
        public static void AddSelectedSpriteToSampleMap()
        {
            GameObject sampleRoot = GameObject.Find("01_SampleMap");
            if (sampleRoot == null)
            {
                EditorUtility.DisplayDialog(
                    "Open Showroom First",
                    "먼저 NAN2026 > Open Map Asset Showroom을 열어 주세요.",
                    "확인");
                return;
            }

            SpriteRenderer sourceRenderer = Selection.activeGameObject != null
                ? Selection.activeGameObject.GetComponent<SpriteRenderer>()
                : null;
            Sprite sprite = Selection.activeObject as Sprite;
            if (sprite == null && sourceRenderer != null)
                sprite = sourceRenderer.sprite;

            if (sprite == null)
            {
                EditorUtility.DisplayDialog(
                    "Select A Sprite",
                    "Hierarchy의 타일 오브젝트 또는 Project 창의 개별 Sprite를 먼저 선택해 주세요.",
                    "확인");
                return;
            }

            Transform userRoot = sampleRoot.transform.Find("Layer_UserEdits__Move_Copy_Tiles_Here");
            if (userRoot == null)
            {
                GameObject userRootObject = new GameObject("Layer_UserEdits__Move_Copy_Tiles_Here");
                Undo.RegisterCreatedObjectUndo(userRootObject, "Create User Edit Layer");
                userRootObject.transform.SetParent(sampleRoot.transform, false);
                userRoot = userRootObject.transform;
            }

            int itemIndex = userRoot.childCount;
            GameObject placed = new GameObject("USER | " + sprite.name);
            Undo.RegisterCreatedObjectUndo(placed, "Add Sprite To Sample Map");
            placed.transform.SetParent(userRoot, false);
            placed.transform.localPosition = new Vector3(itemIndex * 0.45f, 0f, 0f);
            float largestPixelSide = Mathf.Max(sprite.rect.width, sprite.rect.height);
            float defaultScale = largestPixelSide > 256f ? 1f : 3.125f;
            placed.transform.localScale = Vector3.one * defaultScale;

            SpriteRenderer placedRenderer = placed.AddComponent<SpriteRenderer>();
            placedRenderer.sprite = sprite;
            placedRenderer.sortingOrder = 25;
            if (sourceRenderer != null)
            {
                placedRenderer.color = sourceRenderer.color;
                placedRenderer.flipX = sourceRenderer.flipX;
                placedRenderer.flipY = sourceRenderer.flipY;
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeGameObject = placed;
            Tools.current = Tool.Move;
            if (SceneView.lastActiveSceneView != null)
                SceneView.lastActiveSceneView.FrameSelected();
        }

[MenuItem("NAN2026/Focus Sample Map", false, 61)]
        public static void FocusSampleMap()
        {
            GameObject sampleRoot = GameObject.Find("01_SampleMap");
            if (sampleRoot == null)
            {
                EditorUtility.DisplayDialog(
                    "Open Showroom First",
                    "먼저 맵 에셋 쇼룸을 열어 주세요.",
                    "확인");
                return;
            }

            Selection.activeGameObject = sampleRoot;
            if (SceneView.lastActiveSceneView != null)
                SceneView.lastActiveSceneView.FrameSelected();
        }

[MenuItem("NAN2026/Save Current Scene", false, 62)]
        public static void SaveCurrentScene()
        {
            EditorSceneManager.SaveOpenScenes();
        }




private void OnGUI()
        {
            GUILayout.Space(12f);
            EditorGUILayout.LabelField("NAN2026 Project Hub", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Current Scene", SceneManager.GetActiveScene().name);
            EditorGUILayout.LabelField("Selected", Selection.activeObject != null ? Selection.activeObject.name : "None");
            GUILayout.Space(8f);

            EditorGUILayout.HelpBox(
                "쇼룸의 모든 타일과 샘플 맵은 실제 Scene GameObject입니다. " +
                "Hierarchy 또는 Scene 창에서 선택한 뒤 W(이동), E(회전), R(크기), Ctrl+D(복제)로 직접 편집할 수 있습니다.",
                MessageType.Info);

            GUILayout.Space(8f);
            if (GUILayout.Button("기존 플레이어 씬 열기", GUILayout.Height(38f)))
                OpenPlayerScene();

            if (GUILayout.Button("편집 가능한 맵 에셋 쇼룸 열기", GUILayout.Height(42f)))
                OpenShowroomScene();

            GUILayout.Space(8f);
            if (GUILayout.Button("PlatformerSet1 에셋 폴더 보기", GUILayout.Height(32f)))
                SelectPlatformerAssets();

            if (GUILayout.Button("메인 타일 시트 207개 보기", GUILayout.Height(32f)))
                SelectMainTileSheet();

            GUILayout.Space(10f);
            EditorGUILayout.LabelField("Map Editing", EditorStyles.boldLabel);
            if (GUILayout.Button("선택한 타일/스프라이트를 샘플 맵에 복사", GUILayout.Height(42f)))
                AddSelectedSpriteToSampleMap();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("샘플 맵 위치로 이동", GUILayout.Height(30f)))
                FocusSampleMap();
            if (GUILayout.Button("현재 씬 저장", GUILayout.Height(30f)))
                SaveCurrentScene();
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(8f);
            EditorGUILayout.HelpBox(
                "추가된 오브젝트는 01_SampleMap > Layer_UserEdits__Move_Copy_Tiles_Here 아래에 들어갑니다. " +
                "완성 예시 맵의 기존 오브젝트도 그대로 선택해 이동하거나 복제할 수 있습니다.",
                MessageType.None);

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("쇼룸 원본 다시 생성 (현재 쇼룸 편집 내용 초기화)", GUILayout.Height(28f)))
            {
                if (EditorUtility.DisplayDialog(
                        "Rebuild Showroom",
                        "쇼룸을 다시 생성하면 현재 쇼룸에서 직접 수정한 배치가 모두 초기화됩니다. 계속할까요?",
                        "초기화 후 다시 생성",
                        "취소"))
                {
                    PlatformerShowroomBuilder.BuildShowroom();
                }
            }
            GUILayout.Space(8f);
        }

        private static void OpenSceneSafely(string scenePath, string label)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog(
                    "Exit Play Mode",
                    "Play 모드를 종료한 다음 씬을 전환해 주세요.",
                    "확인");
                return;
            }

            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
            if (sceneAsset == null)
            {
                EditorUtility.DisplayDialog(
                    "Scene Not Found",
                    label + "을 찾을 수 없습니다.\n" + scenePath,
                    "확인");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        }

        private static void SelectAssetInProject(string assetPath)
        {
            Object asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
            if (asset == null)
            {
                EditorUtility.DisplayDialog(
                    "Asset Not Found",
                    "에셋을 찾을 수 없습니다.\n" + assetPath,
                    "확인");
                return;
            }

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }
    }
}
#endif
