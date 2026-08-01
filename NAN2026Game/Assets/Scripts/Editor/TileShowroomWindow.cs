using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor.Tilemaps;

namespace NAN2026.EditorTools
{
    // 에셋 쇼룸 v4: 타일·소품 격자 진열 + 씬 클릭 검사(클릭한 타일 즉시 미리보기·격자 자동 점프)
    public class TileShowroomWindow : EditorWindow
    {
        private const string SearchRoot = "Assets/Cainos";
        private static readonly Vector2 CellSize = new Vector2(84f, 104f);
        private static readonly string[] Tabs = { "타일", "소품" };

        private Dictionary<string, List<Object>>[] families;
        private string[][] familyNames;
        private int[] familyIndex;
        private Vector2 scroll;
        private float zoom = 1.0f;
        private int tab;

        private bool inspectMode;
        private readonly List<TileBase> hitTiles = new List<TileBase>();
        private readonly List<string> hitMeta = new List<string>();
        private Object highlight;
        private Object pendingScrollTo;

        [MenuItem("NAN2026/에셋 쇼룸")]
        public static void Open()
        {
            var w = GetWindow<TileShowroomWindow>("에셋 쇼룸");
            w.minSize = new Vector2(520f, 360f);
            w.RefreshAll();
        }

        public static string TileFamilyOf(string tileName)
        {
            int us = tileName.LastIndexOf('_');
            if (us <= 0) return tileName;
            int dummy;
            return int.TryParse(tileName.Substring(us + 1), out dummy) ? tileName.Substring(0, us) : tileName;
        }

        public static string PropFamilyOf(string prefabName, bool village)
        {
            string n = Regex.Replace(prefabName, @"^(PF|TX)\s+(Dungeon|Village)(\s+Props)?\s*-?\s*", "");
            n = Regex.Replace(n, @"[\s\-]*\d+[A-Z\s]*$", "").Trim();
            if (n.Length == 0) n = prefabName;
            return (village ? "[마을] " : "[던전] ") + n;
        }

        public static int NumberOf(string assetName)
        {
            var m = Regex.Match(assetName, @"(\d+)(?:\s*[A-Z])?\s*$");
            int n;
            return m.Success && int.TryParse(m.Groups[1].Value, out n) ? n : 0;
        }

        private void EnsureInit()
        {
            if (families == null || families.Length != 2 || families[0] == null || families[1] == null)
                families = new[] { new Dictionary<string, List<Object>>(), new Dictionary<string, List<Object>>() };
            if (familyNames == null || familyNames.Length != 2 || familyNames[0] == null || familyNames[1] == null)
                familyNames = new[] { new string[0], new string[0] };
            if (familyIndex == null || familyIndex.Length != 2)
                familyIndex = new[] { 0, 0 };
        }

        private void OnEnable()
        {
            // 미리보기 캐시 확대: 수백 개 썸네일 상호 축출(깜빡임) 방지
            AssetPreview.SetPreviewTextureCacheSize(2048);
            EnsureInit();
            RefreshAll();
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        private void RefreshAll()
        {
            EnsureInit();
            families[0].Clear();
            foreach (string guid in AssetDatabase.FindAssets("t:TileBase", new[] { SearchRoot }))
            {
                var tile = AssetDatabase.LoadAssetAtPath<TileBase>(AssetDatabase.GUIDToAssetPath(guid));
                if (tile == null) continue;
                Add(0, TileFamilyOf(tile.name), tile);
            }
            families[1].Clear();
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { SearchRoot }))
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(p);
                if (go == null) continue;
                Add(1, PropFamilyOf(go.name, p.Contains("Village")), go);
            }
            // 현재 씬 사용중 분류 (바닥/벽 겹별 실사용 타일)
            AddUsageFamily("★ 바닥(Stage_Ground) 사용중", "Stage_Ground");
            AddUsageFamily("★ 벽(Stage_Wall) 사용중", "Stage_Wall");
            for (int t = 0; t < 2; t++)
            {
                foreach (var list in families[t].Values)
                    list.Sort((a, b) => NumberOf(a.name) != NumberOf(b.name)
                        ? NumberOf(a.name).CompareTo(NumberOf(b.name))
                        : string.CompareOrdinal(a.name, b.name));
                familyNames[t] = families[t].Keys.OrderBy(k => k.StartsWith("★") ? "0" + k : "1" + k).ToArray();
                familyIndex[t] = Mathf.Clamp(familyIndex[t], 0, Mathf.Max(0, familyNames[t].Length - 1));
            }
        }

        private void AddUsageFamily(string familyName, string tilemapGoName)
        {
            var go = GameObject.Find(tilemapGoName);
            if (go == null) return;
            var tm = go.GetComponent<Tilemap>();
            if (tm == null) return;
            var set = new HashSet<TileBase>();
            foreach (var pos in tm.cellBounds.allPositionsWithin)
            {
                var t = tm.GetTile(pos);
                if (t != null) set.Add(t);
            }
            if (set.Count == 0) return;
            families[0][familyName] = set.Cast<Object>().ToList();
        }

        private void Add(int t, string key, Object o)
        {
            if (!families[t].ContainsKey(key)) families[t][key] = new List<Object>();
            families[t][key].Add(o);
        }

        // 격자에서 해당 타일이 보이도록 탭·계열·스크롤·하이라이트 세팅
        private void JumpTo(Object o)
        {
            if (o == null) return;
            string fam = o is TileBase ? TileFamilyOf(o.name) : null;
            if (fam == null) return;
            tab = 0;
            int idx = System.Array.IndexOf(familyNames[0], fam);
            if (idx >= 0) familyIndex[0] = idx;
            highlight = o;
            pendingScrollTo = o;
            Repaint();
        }

        // 유니티 붓에 타일 장전 + 칠 대상 Stage_Ground + 페인트 도구 활성
        public static string PaintWith(TileBase tile, string targetName = "Stage_Ground")
        {
            try
            {
                var brush = GridPaintingState.gridBrush as GridBrush;
                if (brush == null)
                {
                    brush = ScriptableObject.CreateInstance<GridBrush>();
                    GridPaintingState.gridBrush = brush;
                }
                brush.Init(Vector3Int.one, Vector3Int.zero);
                brush.cells[0].tile = tile;
                brush.cells[0].matrix = Matrix4x4.identity;
                brush.cells[0].color = Color.white;
                var target = GameObject.Find(targetName);
                if (target != null) GridPaintingState.scenePaintTarget = target;
                TilemapEditorTool.SetActiveEditorTool(typeof(PaintTool));
                SceneView.RepaintAll();
                return target != null ? targetName + "에 칠할 준비 완료" : "붓 장전(대상 타일맵은 팔레트에서 지정)";
            }
            catch (System.Exception ex)
            {
                return "실패: " + ex.Message;
            }
        }

        private void OnSceneGUI(SceneView sv)
        {
            if (!inspectMode) return;
            var e = Event.current;
            int id = GUIUtility.GetControlID(FocusType.Passive);
            HandleUtility.AddDefaultControl(id);
            if (e.type != EventType.MouseDown || e.button != 0) return;
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            float t2 = -ray.origin.z / (Mathf.Approximately(ray.direction.z, 0f) ? 1f : ray.direction.z);
            Vector3 world = ray.origin + ray.direction * Mathf.Max(0f, t2);
            hitTiles.Clear();
            hitMeta.Clear();
            foreach (var tm in Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
            {
                var cell = tm.WorldToCell(world);
                var tile = tm.GetTile(cell);
                if (tile == null) continue;
                hitTiles.Add(tile);
                hitMeta.Add("[" + tm.gameObject.name + "] 셀(" + cell.x + "," + cell.y + ")");
            }
            if (hitTiles.Count > 0) JumpTo(hitTiles[0]);
            e.Use();
            Repaint();
        }

        private void OnGUI()
        {
            EnsureInit();
            if (familyNames[0].Length == 0 && familyNames[1].Length == 0) RefreshAll();
            DrawSceneSwitcher();
            tab = GUILayout.Toolbar(tab, Tabs);
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (familyNames[tab].Length > 0)
                    familyIndex[tab] = EditorGUILayout.Popup(Mathf.Clamp(familyIndex[tab], 0, familyNames[tab].Length - 1), familyNames[tab], EditorStyles.toolbarPopup, GUILayout.Width(260f));
                else
                    GUILayout.Label("(비어 있음)", EditorStyles.toolbarButton, GUILayout.Width(260f));
                zoom = GUILayout.HorizontalSlider(zoom, 0.6f, 1.8f, GUILayout.Width(100f));
                if (GUILayout.Button("새로고침", EditorStyles.toolbarButton, GUILayout.Width(64f))) RefreshAll();
                bool prev = inspectMode;
                inspectMode = GUILayout.Toggle(inspectMode, "씬 클릭 검사", EditorStyles.toolbarButton, GUILayout.Width(88f));
                if (inspectMode != prev) SceneView.RepaintAll();
                using (new EditorGUI.DisabledScope(!(highlight is TileBase)))
                    if (GUILayout.Button("선택 타일로 칠하기", EditorStyles.toolbarButton, GUILayout.Width(110f)))
                    {
                        inspectMode = false;
                        ShowNotification(new GUIContent(PaintWith((TileBase)highlight)));
                    }
                GUILayout.FlexibleSpace();
                if (familyNames[tab].Length > 0)
                    GUILayout.Label(families[tab][familyNames[tab][familyIndex[tab]]].Count + "개");
            }

            if (inspectMode) DrawInspectPanel();
            if (tab == 1)
                EditorGUILayout.HelpBox("클릭=프로젝트에서 선택 / 셀을 잡고 씬 뷰로 드래그=바로 배치", MessageType.None);
            if (familyNames[tab].Length == 0) return;

            var items = families[tab][familyNames[tab][familyIndex[tab]]];
            float cw = CellSize.x * zoom, ch = CellSize.y * zoom;
            int cols = Mathf.Max(1, Mathf.FloorToInt((position.width - 24f) / cw));
            // 자동 스크롤 (검사 점프)
            if (pendingScrollTo != null && tab == 0)
            {
                int idx = items.IndexOf(pendingScrollTo);
                if (idx >= 0) scroll.y = (idx / cols) * ch;
                pendingScrollTo = null;
            }
            int rows = Mathf.CeilToInt(items.Count / (float)cols);
            scroll = EditorGUILayout.BeginScrollView(scroll);
            Rect area = GUILayoutUtility.GetRect(cols * cw, rows * ch);
            // 가시 행 범위 계산: 보이는 칸만 미리보기 요청 (캐시 폭주 방지)
            float viewH = position.height;
            int firstRow = Mathf.Max(0, Mathf.FloorToInt(scroll.y / ch) - 1);
            int lastRow = Mathf.Min(rows - 1, Mathf.CeilToInt((scroll.y + viewH) / ch) + 1);
            for (int i = 0; i < items.Count; i++)
            {
                var o = items[i];
                int rowIdx = i / cols;
                Rect cell = new Rect(area.x + (i % cols) * cw, area.y + rowIdx * ch, cw, ch);
                if (o == highlight)
                    EditorGUI.DrawRect(cell, new Color(1f, 0.9f, 0.2f, 0.28f));
                Rect img = new Rect(cell.x + 4f, cell.y + 4f, cw - 8f, cw - 26f);
                bool visible = rowIdx >= firstRow && rowIdx <= lastRow;
                Texture2D preview = visible ? AssetPreview.GetAssetPreview(o) : null;
                if (preview != null) GUI.DrawTexture(img, preview, ScaleMode.ScaleToFit);
                else EditorGUI.DrawRect(img, new Color(0.2f, 0.2f, 0.2f));
                string label = tab == 0 ? NumberOf(o.name).ToString()
                    : Regex.Replace(o.name, @"^(PF|TX)\s+(Dungeon|Village)(\s+Props)?\s*-?\s*", "");
                GUI.Label(new Rect(cell.x, img.yMax, cw, 20f), label, EditorStyles.centeredGreyMiniLabel);

                var e = Event.current;
                if (cell.Contains(e.mousePosition))
                {
                    if (e.type == EventType.MouseDown)
                    {
                        Selection.activeObject = o;
                        EditorGUIUtility.PingObject(o);
                        highlight = o;
                        // 타일 탭: 클릭 즉시 붓 장전 → 씬에서 바로 칠하기 가능
                        if (tab == 0 && o is TileBase)
                        {
                            inspectMode = false;
                            string curFam = familyNames[0][Mathf.Clamp(familyIndex[0], 0, familyNames[0].Length - 1)];
                            string targetTm = curFam.Contains("Stage_Wall") ? "Stage_Wall" : "Stage_Ground";
                            ShowNotification(new GUIContent(PaintWith((TileBase)o, targetTm)), 1.2d);
                        }
                        e.Use();
                    }
                    else if (e.type == EventType.MouseDrag && tab == 1)
                    {
                        DragAndDrop.PrepareStartDrag();
                        DragAndDrop.objectReferences = new[] { o };
                        DragAndDrop.StartDrag(o.name);
                        e.Use();
                    }
                }
            }
            EditorGUILayout.EndScrollView();
            if (AssetPreview.IsLoadingAssetPreviews()) Repaint();
        }

        // 원클릭 씬 전환 바 (수정 씬은 저장 확인 후 전환, 팩 원본은 저장하지 않음)
        private void DrawSceneSwitcher()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("씬:", GUILayout.Width(24f));
                SceneButton("우리 맵", "Assets/Scenes/SecondScene.unity");
                SceneButton("데모(정답지)", "Assets/Cainos/Pixel Art Platformer - Dungeon/Scene/SC Demo Scene.unity");
                SceneButton("소품 카탈로그", "Assets/Cainos/Pixel Art Platformer - Dungeon/Scene/SC All Props.unity");
                GUILayout.FlexibleSpace();
                GUILayout.Label(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, EditorStyles.miniLabel);
            }
        }

        private void SceneButton(string label, string scenePath)
        {
            var active = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            bool isCurrent = active.path == scenePath;
            using (new EditorGUI.DisabledScope(isCurrent))
            {
                if (!GUILayout.Button(label, EditorStyles.toolbarButton)) return;
                // 우리 씬만 저장 대상 — 팩 원본 수정본은 저장 확인 창에 맡김
                if (active.isDirty && active.path.StartsWith("Assets/Scenes/"))
                    UnityEditor.SceneManagement.EditorSceneManager.SaveScene(active);
                else if (active.isDirty)
                {
                    if (!UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                }
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);
            }
        }

        // 검사 결과 패널: 클릭한 타일들의 이미지·이름 즉시 표시
        private void DrawInspectPanel()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (hitTiles.Count == 0)
                {
                    GUILayout.Label("검사 모드 ON — 씬 뷰에서 타일을 클릭하세요.", EditorStyles.miniLabel);
                    return;
                }
                for (int i = 0; i < hitTiles.Count; i++)
                {
                    var t = hitTiles[i];
                    if (t == null) continue;
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        Rect r = GUILayoutUtility.GetRect(52f, 52f, GUILayout.Width(52f));
                        Texture2D preview = AssetPreview.GetAssetPreview(t);
                        if (preview != null) GUI.DrawTexture(r, preview, ScaleMode.ScaleToFit);
                        else EditorGUI.DrawRect(r, new Color(0.2f, 0.2f, 0.2f));
                        using (new EditorGUILayout.VerticalScope())
                        {
                            GUILayout.Label(t.name, EditorStyles.boldLabel);
                            GUILayout.Label(hitMeta[i], EditorStyles.miniLabel);
                        }
                        if (GUILayout.Button("격자에서 보기", GUILayout.Width(96f), GUILayout.Height(24f)))
                            JumpTo(t);
                        if (GUILayout.Button("🖌 이 타일로 칠하기", GUILayout.Width(124f), GUILayout.Height(24f)))
                        {
                            inspectMode = false;
                            ShowNotification(new GUIContent(PaintWith(t)));
                        }
                    }
                }
            }
        }
    }
}
