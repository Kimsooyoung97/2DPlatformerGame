using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace NAN2026.EditorTools
{
    // 겹층 도구: 타일맵 목록·편집 + 새 층 생성·붓 조준 (커스텀 층에 그리기)
    public partial class TileShowroomWindow
    {
        private static bool layerToolOpen;
        private static string customBrushTarget; // null=자동(Ground/Wall), 값 있으면 모든 칠하기가 이 층으로

        private static void StripTerrainColliders(GameObject go)
        {
            var cc = go.GetComponent<CompositeCollider2D>();
            if (cc != null) DestroyImmediate(cc);
            var tc = go.GetComponent<TilemapCollider2D>();
            if (tc != null) DestroyImmediate(tc);
            var rb = go.GetComponent<Rigidbody2D>();
            if (rb != null) DestroyImmediate(rb);
        }

        private void DrawLayerTool()
        {
            layerToolOpen = EditorGUILayout.Foldout(layerToolOpen, "겹층 도구 (지형 → 배경층 변환)", true);
            if (!layerToolOpen) return;

            // 붓 조준 상태 표시줄
            using (new EditorGUILayout.HorizontalScope())
            {
                string aim = string.IsNullOrEmpty(customBrushTarget) ? "자동 (Ground/Wall)" : customBrushTarget;
                EditorGUILayout.LabelField("붓 조준: " + aim, EditorStyles.miniBoldLabel);
                if (GUILayout.Button("＋ 새 층 생성+조준", GUILayout.Width(130f)))
                {
                    var grid = GameObject.Find("Stage_Grid");
                    int n = 1;
                    while (GameObject.Find("Stage_Layer_" + n) != null) n++;
                    var go = new GameObject("Stage_Layer_" + n);
                    if (grid != null) go.transform.SetParent(grid.transform, false);
                    go.AddComponent<Tilemap>();
                    var r = go.AddComponent<TilemapRenderer>();
                    r.sortingOrder = 0;
                    Undo.RegisterCreatedObjectUndo(go, "새 층 생성");
                    customBrushTarget = go.name;
                    Selection.activeGameObject = go;
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(go.scene);
                    ShowNotification(new GUIContent(go.name + " 생성 — 이제 타일을 칠하면 이 층에 그려진다"));
                }
                if (!string.IsNullOrEmpty(customBrushTarget) && GUILayout.Button("조준 해제", GUILayout.Width(70f)))
                    customBrushTarget = null;
            }

            foreach (var tm in FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    var tr0 = tm.GetComponent<TilemapRenderer>();
                    bool solid = tm.GetComponent<TilemapCollider2D>() != null;
                    string label = tm.gameObject.name + "  |  order " + (tr0 != null ? tr0.sortingOrder : 0)
                        + (solid ? "  |  충돌O" : "  |  충돌X(배경)");
                    if (GUILayout.Button(label, EditorStyles.miniButton))
                    {
                        Selection.activeGameObject = tm.gameObject;
                        EditorGUIUtility.PingObject(tm.gameObject);
                    }
                    if (GUILayout.Button("붓→", GUILayout.Width(40f)))
                        customBrushTarget = tm.gameObject.name;
                }
            }

            var selGo = Selection.activeGameObject;
            var sel = selGo != null ? selGo.GetComponent<Tilemap>() : null;
            if (sel == null)
            {
                EditorGUILayout.HelpBox("위 목록(또는 하이어라키)에서 타일맵을 선택하면 편집 필드가 열린다", MessageType.Info);
                EditorGUILayout.Space(4);
                return;
            }

            var tr = sel.GetComponent<TilemapRenderer>();
            EditorGUILayout.LabelField("선택: " + sel.gameObject.name, EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            int newOrder = EditorGUILayout.IntField("Order in Layer", tr.sortingOrder);
            Color newCol = EditorGUILayout.ColorField("틴트(어둡게=원경)", sel.color);
            Vector3 newPos = EditorGUILayout.Vector3Field("위치 오프셋", sel.transform.position);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObjects(new Object[] { tr, sel, sel.transform }, "겹층 편집");
                tr.sortingOrder = newOrder;
                sel.color = newCol;
                sel.transform.position = newPos;
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(sel.gameObject.scene);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("복제 → 뒤층 생성"))
            {
                var dup = Instantiate(sel.gameObject, sel.transform.parent);
                dup.name = sel.gameObject.name + "_Back";
                StripTerrainColliders(dup);
                var dtr = dup.GetComponent<TilemapRenderer>();
                if (dtr != null) dtr.sortingOrder = -110;
                dup.GetComponent<Tilemap>().color = new Color(0.45f, 0.52f, 0.55f, 1f);
                dup.transform.position = sel.transform.position + new Vector3(5f, 3f, 0f);
                Undo.RegisterCreatedObjectUndo(dup, "뒤층 생성");
                Selection.activeGameObject = dup;
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(dup.scene);
            }
            if (GUILayout.Button("충돌 제거(이 층을 배경화)"))
            {
                Undo.RegisterFullObjectHierarchyUndo(sel.gameObject, "충돌 제거");
                StripTerrainColliders(sel.gameObject);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(sel.gameObject.scene);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(4);
        }
    }
}
