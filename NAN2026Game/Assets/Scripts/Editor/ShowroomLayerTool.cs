using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace NAN2026.EditorTools
{
    // 겹층 도구: 씬 타일맵 목록 → 클릭 선택 → Order/틴트/오프셋 편집, 복제→뒤층 원클릭
    public partial class TileShowroomWindow
    {
        private static bool layerToolOpen;

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

            foreach (var tm in FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
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
