using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace NAN2026.EditorTools
{
    // 겹층 도구: 타일맵 목록·편집 + 새 층·붓 조준 + 구간→층 이동(드래그)
    public partial class TileShowroomWindow
    {
        private static bool layerToolOpen;
        private static string customBrushTarget;
        private static bool layerMoveMode;
        private static int layerMoveTarget = 1;
        private static bool layerDragging;
        private static Vector2 layerDragStart;

        // 규칙: Stage_Layer_N = 정렬 -10*N (1이 제일 앞, 클수록 뒤)
        private static Tilemap EnsureLayer(int n)
        {
            var go = GameObject.Find("Stage_Layer_" + n);
            if (go == null)
            {
                go = new GameObject("Stage_Layer_" + n);
                var grid = GameObject.Find("Stage_Grid");
                if (grid != null) go.transform.SetParent(grid.transform, false);
                go.AddComponent<Tilemap>();
                go.AddComponent<TilemapRenderer>();
                Undo.RegisterCreatedObjectUndo(go, "층 생성");
            }
            var tr = go.GetComponent<TilemapRenderer>();
            if (tr != null) tr.sortingOrder = -10 * n;
            return go.GetComponent<Tilemap>();
        }

        private static void StripTerrainColliders(GameObject go)
        {
            var cc = go.GetComponent<CompositeCollider2D>();
            if (cc != null) DestroyImmediate(cc);
            var tc = go.GetComponent<TilemapCollider2D>();
            if (tc != null) DestroyImmediate(tc);
            var rb = go.GetComponent<Rigidbody2D>();
            if (rb != null) DestroyImmediate(rb);
        }

        private void HandleLayerMove(SceneView sv)
        {
            var e = Event.current;
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                layerMoveMode = false; layerDragging = false;
                sv.Repaint(); Repaint();
                return;
            }
            int id = GUIUtility.GetControlID(FocusType.Passive);
            HandleUtility.AddDefaultControl(id);
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            float dz = Mathf.Approximately(ray.direction.z, 0f) ? 1f : ray.direction.z;
            Vector3 world = ray.origin + ray.direction * Mathf.Max(0f, -ray.origin.z / dz);
            Handles.BeginGUI();
            GUI.Label(new Rect(10, 10, 460, 22), "구간→층 이동: 드래그로 범위 지정 → Layer" + layerMoveTarget + "로 이동 (Esc 취소)", EditorStyles.helpBox);
            Handles.EndGUI();
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                layerDragging = true; layerDragStart = world; e.Use();
            }
            if (layerDragging)
            {
                Vector2 a = layerDragStart, b = world;
                var r = Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
                Handles.color = new Color(0.4f, 0.7f, 1f, 0.9f);
                Handles.DrawSolidRectangleWithOutline(new Vector3[] {
                    new Vector3(r.xMin, r.yMin), new Vector3(r.xMax, r.yMin),
                    new Vector3(r.xMax, r.yMax), new Vector3(r.xMin, r.yMax) },
                    new Color(0.4f, 0.7f, 1f, 0.08f), Handles.color);
                sv.Repaint();
                if (e.type == EventType.MouseUp && e.button == 0)
                {
                    layerDragging = false;
                    MoveRegionToLayer(r, layerMoveTarget);
                    layerMoveMode = false; // 원샷
                    e.Use(); Repaint();
                }
            }
        }

        private void MoveRegionToLayer(Rect r, int n)
        {
            var target = EnsureLayer(n);
            var moved = 0;
            var sources = FindObjectsByType<Tilemap>(FindObjectsSortMode.None);
            Undo.RegisterCompleteObjectUndo(target, "구간 층 이동");
            foreach (var m in sources)
            {
                if (m == target) continue;
                var b = m.cellBounds;
                var pending = new System.Collections.Generic.List<Vector3Int>();
                var tiles = new System.Collections.Generic.List<TileBase>();
                foreach (var pos in b.allPositionsWithin)
                {
                    var t = m.GetTile(pos);
                    if (t == null) continue;
                    Vector3 wc = m.CellToWorld(pos) + m.cellSize * 0.5f;
                    if (!r.Contains(new Vector2(wc.x, wc.y))) continue;
                    pending.Add(pos); tiles.Add(t);
                }
                if (pending.Count == 0) continue;
                Undo.RegisterCompleteObjectUndo(m, "구간 층 이동");
                for (int i = 0; i < pending.Count; i++)
                {
                    Vector3 wc = m.CellToWorld(pending[i]) + m.cellSize * 0.5f;
                    target.SetTile(target.WorldToCell(wc), tiles[i]);
                    m.SetTile(pending[i], null);
                    moved++;
                }
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(m.gameObject.scene);
            }
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(target.gameObject.scene);
            ShowNotification(new GUIContent("Layer" + n + "(정렬 " + (-10 * n) + ")로 " + moved + "셀 이동"));
        }

        private void DrawLayerTool()
        {
            layerToolOpen = EditorGUILayout.Foldout(layerToolOpen, "겹층 도구 (지형 → 배경층 변환)", true);
            if (!layerToolOpen) return;

            using (new EditorGUILayout.HorizontalScope())
            {
                string aim = string.IsNullOrEmpty(customBrushTarget) ? "자동 (Ground/Wall)" : customBrushTarget;
                EditorGUILayout.LabelField("붓 조준: " + aim, EditorStyles.miniBoldLabel);
                if (GUILayout.Button("＋ 새 층 생성+조준", GUILayout.Width(130f)))
                {
                    int n = 1;
                    while (GameObject.Find("Stage_Layer_" + n) != null) n++;
                    var tm = EnsureLayer(n);
                    customBrushTarget = tm.gameObject.name;
                    Selection.activeGameObject = tm.gameObject;
                    ShowNotification(new GUIContent(tm.gameObject.name + " 생성 (정렬 " + (-10 * n) + ") — 칠하기가 이 층으로 간다"));
                }
                if (!string.IsNullOrEmpty(customBrushTarget) && GUILayout.Button("조준 해제", GUILayout.Width(70f)))
                    customBrushTarget = null;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                layerMoveTarget = Mathf.Max(1, EditorGUILayout.IntField("이동 대상 Layer 번호", layerMoveTarget));
                bool on = GUILayout.Toggle(layerMoveMode, "구간→층 이동 (드래그)", "Button", GUILayout.Width(160f));
                if (on != layerMoveMode)
                {
                    layerMoveMode = on;
                    if (on) { regionMode = false; armedTile = null; armedProp = null; inspectMode = false; }
                    SceneView.RepaintAll();
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("투명 발판 (솔리드)"))
                    MakeInvisibleBox(false);
                if (GUILayout.Button("투명 발판 (원웨이 Platform_)"))
                    MakeInvisibleBox(true);
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

        private static void MakeInvisibleBox(bool oneway)
        {
            int n = 1;
            string prefix = oneway ? "Platform_Invisible_" : "Solid_Invisible_";
            while (GameObject.Find(prefix + n) != null) n++;
            var go = new GameObject(prefix + n);
            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(3f, 0.5f);
            if (oneway)
            {
                box.usedByEffector = true;
                var eff = go.AddComponent<PlatformEffector2D>();
                eff.useOneWay = true;
                eff.surfaceArc = 130f;
            }
            var t = System.Type.GetType("NAN2026.InvisiblePlatform, Assembly-CSharp");
            if (t != null) go.AddComponent(t);
            var sv = SceneView.lastActiveSceneView;
            go.transform.position = sv != null ? (Vector3)(Vector2)sv.pivot : Vector3.zero;
            Undo.RegisterCreatedObjectUndo(go, "투명 발판");
            Selection.activeGameObject = go;
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(go.scene);
        }
    }
}
