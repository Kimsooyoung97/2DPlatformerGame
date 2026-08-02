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
    public partial class TileShowroomWindow : EditorWindow
    {
        private static readonly string[] SearchRoots = { "Assets/Cainos", "Assets/sanctum_pixel" };
        private static readonly Vector2 CellSize = new Vector2(84f, 104f);
        private static readonly string[] Tabs = { "타일", "소품" };

        private Dictionary<string, List<Object>>[] families;
        private string[][] familyNames;
        private int[] familyIndex;
        private Vector2 scroll;
        private float zoom = 1.0f;
        private int tab;

        private bool inspectMode;
        private static TileBase armedTile;
        private static GameObject armedProp;
        // 구간 복사 모드 상태
        private static bool regionMode;
        private static UnityEngine.Vector3? regionDragStart;
        private static bool hasClip;
        private static UnityEngine.Vector3Int clipSize;
        private static readonly List<Vector3Int> clipGroundOff = new List<Vector3Int>();
        private static readonly List<TileBase> clipGroundTile = new List<TileBase>();
        private static readonly List<Vector3Int> clipWallOff = new List<Vector3Int>();
        private static readonly List<TileBase> clipWallTile = new List<TileBase>();
        private static readonly List<GameObject> clipPropAsset = new List<GameObject>();
        private static readonly List<Vector3> clipPropOff = new List<Vector3>();
        private static readonly List<Vector3> clipPropScale = new List<Vector3>();
        private static readonly List<int> clipPropOrder = new List<int>();
        private static readonly List<bool> clipPropFlipX = new List<bool>();
        private readonly List<GameObject> hitProps = new List<GameObject>();
        private readonly List<string> hitPropMeta = new List<string>();
        private static string armedTarget = "Stage_Ground";
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
            foreach (string guid in AssetDatabase.FindAssets("t:TileBase", SearchRoots))
            {
                var tile = AssetDatabase.LoadAssetAtPath<TileBase>(AssetDatabase.GUIDToAssetPath(guid));
                if (tile == null) continue;
                Add(0, TileFamilyOf(tile.name), tile);
            }
            families[1].Clear();
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", SearchRoots))
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(p);
                if (go == null) continue;
                Add(1, PropFamilyOf(go.name, p.Contains("Village")), go);
            }
            // 현재 씬 사용중 분류 (바닥/벽 겹별 실사용 타일)
            AddUsageFamily("★ 바닥(Stage_Ground) 사용중", "Stage_Ground");
            AddUsageFamily("★ 벽(Stage_Wall) 사용중", "Stage_Wall");
            // forest 팩 역할 분할 (데모 실측: 지형/잔디 장식)
            if (families[0].ContainsKey("forest_tileset"))
            {
                var all = families[0]["forest_tileset"];
                var gI = new HashSet<int> { 9,10,11,12,13,14,15,17,18,19,20,21,22,24 };
                var wI = new HashSet<int> { 0,1,2,3,4,5,6 };
                var fg = new List<Object>(); var fw = new List<Object>();
                foreach (var o in all)
                {
                    var m = System.Text.RegularExpressions.Regex.Match(o.name, @"forest_tileset_(\d+)");
                    if (!m.Success) continue;
                    int n = int.Parse(m.Groups[1].Value);
                    if (gI.Contains(n)) fg.Add(o);
                    else if (wI.Contains(n)) fw.Add(o);
                }
                if (fg.Count > 0) families[0]["forest — Ground"] = fg;
                if (fw.Count > 0) families[0]["forest — Wall(잔디)"] = fw;
            }
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

        private static bool IsForestDeco(string n)
        {
            var m = System.Text.RegularExpressions.Regex.Match(n, @"forest_tileset_(\d+)$");
            return m.Success && int.Parse(m.Groups[1].Value) <= 6;
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
            if (set.Count == 0)
            {
                // 씬 겹이 비어도 메뉴 유지: 이름에 대응 키워드가 든 팩 패밀리로 대체
                string want = tilemapGoName == "Stage_Wall" ? "Wall" : "Ground";
                foreach (var kv in families[0])
                    if (!kv.Key.StartsWith("★") && kv.Key.Contains(want))
                    { families[0][familyName] = new List<Object>(kv.Value); return; }
                return;
            }
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

        private void JumpToProp(GameObject prefabAsset)
        {
            if (prefabAsset == null) return;
            tab = 1;
            string ap = AssetDatabase.GetAssetPath(prefabAsset);
            string fam = PropFamilyOf(prefabAsset.name, ap.Contains("Village"));
            int idx = System.Array.IndexOf(familyNames[1], fam);
            if (idx >= 0) familyIndex[1] = idx;
            highlight = prefabAsset;
            pendingScrollTo = prefabAsset;
            Repaint();
        }

        // 유니티 붓에 타일 장전 + 칠 대상 Stage_Ground + 페인트 도구 활성
        public static string PaintWith(TileBase tile, string targetName = "Stage_Ground")
        {
            // 자체 붓: 유니티 팔레트 상태와 무관하게 항상 동작
            armedTile = tile;
            armedProp = null;
            armedTarget = targetName;
            regionMode = false;
            layerMoveMode = false;
            SceneView.RepaintAll();
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
                EditorApplication.delayCall += () =>
                {
                    try { TilemapEditorTool.SetActiveEditorTool(typeof(PaintTool)); SceneView.RepaintAll(); } catch { }
                };
                SceneView.RepaintAll();
                return targetName + "에 칠할 준비 완료 — 씬에서 드래그 (Shift=지우기, Esc=해제)";
            }
            catch (System.Exception)
            {
                return targetName + "에 칠할 준비 완료(자체 붓) — 씬에서 드래그";
            }
        }

        public static string PlaceWith(GameObject prefab)
        {
            armedProp = prefab;
            armedTile = null;
            regionMode = false;
            layerMoveMode = false;
            SceneView.RepaintAll();
            return prefab.name + " 배치 모드 — 씬 클릭=놓기 (Ctrl=0.5스냅, Esc=해제)";
        }

        // 마우스 위치 → 월드 좌표 (z=0 평면)
        private static Vector3 MouseWorld(Event e)
        {
            Ray mray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            float mdz = Mathf.Approximately(mray.direction.z, 0f) ? 1f : mray.direction.z;
            return mray.origin + mray.direction * Mathf.Max(0f, -mray.origin.z / mdz);
        }

        // 구간 복사: 어느 씬에서든 드래그 캡처(에셋 참조 저장) → 우리 맵에서 붙여넣기(덮어쓰기)
        private void HandleRegionCopy(SceneView sv)
        {
            var e = Event.current;
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                if (hasClip) ClearClip();
                else regionMode = false;
                regionDragStart = null;
                sv.Repaint();
                Repaint();
                return;
            }
            int id = GUIUtility.GetControlID(FocusType.Passive);
            HandleUtility.AddDefaultControl(id);
            Vector3 world = MouseWorld(e);
            if (!hasClip)
            {
                if (e.type == EventType.MouseDown && e.button == 0)
                {
                    regionDragStart = world;
                    e.Use();
                }
                if (regionDragStart.HasValue)
                {
                    Vector3 a = regionDragStart.Value, b = world;
                    Vector3 mn = Vector3.Min(a, b), mx = Vector3.Max(a, b);
                    Handles.color = new Color(1f, 0.85f, 0.2f, 0.95f);
                    Handles.DrawSolidRectangleWithOutline(new[] {
                        new Vector3(mn.x, mn.y), new Vector3(mx.x, mn.y), new Vector3(mx.x, mx.y), new Vector3(mn.x, mx.y) },
                        new Color(1f, 0.9f, 0.3f, 0.08f), Handles.color);
                    if (e.type == EventType.MouseDrag && e.button == 0) { e.Use(); sv.Repaint(); }
                    if (e.type == EventType.MouseUp && e.button == 0)
                    {
                        CaptureRegion(mn, mx);
                        regionDragStart = null;
                        e.Use();
                        sv.Repaint();
                        Repaint();
                    }
                }
            }
            else
            {
                var anchorCell = new Vector3Int(Mathf.FloorToInt(world.x), Mathf.FloorToInt(world.y), 0);
                Vector3 aw = new Vector3(anchorCell.x, anchorCell.y, 0f);
                Vector3 sz = new Vector3(clipSize.x, clipSize.y, 0f);
                Handles.color = new Color(0.3f, 0.9f, 1f, 0.95f);
                Handles.DrawSolidRectangleWithOutline(new[] {
                    aw, aw + new Vector3(sz.x, 0f), aw + sz, aw + new Vector3(0f, sz.y) },
                    new Color(0.3f, 0.9f, 1f, 0.07f), Handles.color);
                Handles.Label(aw + new Vector3(0f, sz.y + 0.3f, 0f), "붙여넣기 위치 (클릭)");
                if (e.type == EventType.MouseMove) sv.Repaint();
                if (e.type == EventType.MouseDown && e.button == 0)
                {
                    PasteRegion(anchorCell);
                    e.Use();
                    sv.Repaint();
                }
            }
        }

        private static void ClearClip()
        {
            hasClip = false;
            clipGroundOff.Clear(); clipGroundTile.Clear();
            clipWallOff.Clear(); clipWallTile.Clear();
            clipPropAsset.Clear(); clipPropOff.Clear();
            clipPropScale.Clear(); clipPropOrder.Clear(); clipPropFlipX.Clear();
        }

        private static readonly string[] PropExclude = { "Player", "Princess", "Boss", "Portal", "Camera", "Background", "Global", "HitFlash", "EventSystem" };

        // 씬 무관 캡처: 모든 타일맵을 콜라이더 유무로 바닥/벽 분류, 소품은 프리팹 에셋 참조로 저장
        private void CaptureRegion(Vector3 mn, Vector3 mx)
        {
            ClearClip();
            var cellMin = new Vector3Int(Mathf.FloorToInt(mn.x), Mathf.FloorToInt(mn.y), 0);
            var cellMax = new Vector3Int(Mathf.FloorToInt(mx.x), Mathf.FloorToInt(mx.y), 0);
            foreach (var tm in Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
            {
                bool isWall = tm.GetComponent<TilemapCollider2D>() == null;
                // 이동·오프셋 층 대응: 맵별 좌표 환산 + 월드 위치로 범위 판정
                var a = tm.WorldToCell(mn);
                var b = tm.WorldToCell(mx);
                int x0 = Mathf.Min(a.x, b.x) - 1, x1 = Mathf.Max(a.x, b.x) + 1;
                int y0 = Mathf.Min(a.y, b.y) - 1, y1 = Mathf.Max(a.y, b.y) + 1;
                for (int x = x0; x <= x1; x++)
                    for (int y = y0; y <= y1; y++)
                    {
                        var p = new Vector3Int(x, y, 0);
                        var t = tm.GetTile(p);
                        if (t == null) continue;
                        Vector3 wc = tm.CellToWorld(p) + tm.cellSize * 0.5f;
                        if (wc.x < mn.x || wc.x > mx.x || wc.y < mn.y || wc.y > mx.y) continue;
                        var off = new Vector3Int(Mathf.FloorToInt(wc.x), Mathf.FloorToInt(wc.y), 0) - cellMin;
                        if (isWall) { clipWallOff.Add(off); clipWallTile.Add(t); }
                        else { clipGroundOff.Add(off); clipGroundTile.Add(t); }
                    }
            }
            Vector3 anchorW = new Vector3(cellMin.x, cellMin.y, 0f);
            var seenRoots = new HashSet<GameObject>();
            foreach (var sr in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
            {
                var root = PrefabUtility.GetNearestPrefabInstanceRoot(sr.gameObject);
                if (root == null || seenRoots.Contains(root)) continue;
                seenRoots.Add(root);
                var pos = root.transform.position;
                if (pos.x < mn.x || pos.x > mx.x || pos.y < mn.y || pos.y > mx.y) continue;
                bool skip = false;
                foreach (var ex in PropExclude) if (root.name.Contains(ex)) { skip = true; break; }
                if (skip) continue;
                var asset = PrefabUtility.GetCorrespondingObjectFromSource(root) as GameObject;
                if (asset == null) continue;
                var firstSr = root.GetComponentInChildren<SpriteRenderer>();
                clipPropAsset.Add(asset);
                clipPropOff.Add(pos - anchorW);
                clipPropScale.Add(root.transform.localScale);
                clipPropOrder.Add(firstSr != null ? firstSr.sortingOrder : 0);
                clipPropFlipX.Add(firstSr != null && firstSr.flipX);
            }
            clipSize = cellMax - cellMin + new Vector3Int(1, 1, 0);
            hasClip = clipGroundOff.Count + clipWallOff.Count + clipPropAsset.Count > 0;
            ShowNotification(new GUIContent(hasClip
                ? "복사됨: 바닥 " + clipGroundOff.Count + "·벽 " + clipWallOff.Count + "·소품 " + clipPropAsset.Count + " — [우리 맵]에서 클릭=붙여넣기"
                : "빈 범위"), 1.8d);
        }

        // 우리 씬 전용 붙여넣기 (덮어쓰기). 팩 원본 씬 보호
        private void PasteRegion(Vector3Int anchorCell)
        {
            try
            {
                var scenePath = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
                if (scenePath.StartsWith("Assets/Cainos"))
                {
                    ShowNotification(new GUIContent("팩 원본 씬에는 붙여넣기 금지 — [우리 맵]으로 전환하세요"), 2.5d);
                    return;
                }
                var gGo = GameObject.Find("Stage_Ground");
                if (gGo == null)
                {
                    ShowNotification(new GUIContent("Stage_Ground 없음 — 우리 맵에서 붙여넣으세요"), 2.5d);
                    return;
                }
                var gtm = gGo.GetComponent<Tilemap>();
                var wGo = GameObject.Find("Stage_Wall");
                var wtm = wGo != null ? wGo.GetComponent<Tilemap>() : null;
                Undo.RegisterCompleteObjectUndo(gtm, "구간 붙여넣기");
                if (wtm != null) Undo.RegisterCompleteObjectUndo(wtm, "구간 붙여넣기");
                for (int x = 0; x < clipSize.x; x++)
                    for (int y = 0; y < clipSize.y; y++)
                    {
                        var p = anchorCell + new Vector3Int(x, y, 0);
                        gtm.SetTile(p, null);
                        if (wtm != null) wtm.SetTile(p, null);
                    }
                Vector3 anchorW = new Vector3(anchorCell.x, anchorCell.y, 0f);
                Vector3 maxW = anchorW + new Vector3(clipSize.x, clipSize.y, 0f);
                var parentGo = GameObject.Find("Stage_Props");
                int removedProps = 0;
                if (parentGo != null)
                {
                    var doomed = new List<GameObject>();
                    foreach (Transform c in parentGo.transform)
                    {
                        var pp = c.position;
                        if (pp.x >= anchorW.x && pp.x <= maxW.x && pp.y >= anchorW.y && pp.y <= maxW.y)
                            doomed.Add(c.gameObject);
                    }
                    foreach (var d in doomed) { Undo.DestroyObjectImmediate(d); removedProps++; }
                }
                for (int i = 0; i < clipGroundOff.Count; i++)
                    gtm.SetTile(anchorCell + clipGroundOff[i], clipGroundTile[i]);
                if (wtm != null)
                    for (int i = 0; i < clipWallOff.Count; i++)
                        wtm.SetTile(anchorCell + clipWallOff[i], clipWallTile[i]);
                // 소품: 캡처 정렬 순서를 보존하며 우리 -300대역 고유값으로 재부여
                int next = -300;
                if (parentGo != null)
                    foreach (Transform c in parentGo.transform)
                    {
                        var sr0 = c.GetComponentInChildren<SpriteRenderer>();
                        if (sr0 != null && sr0.sortingOrder >= -300 && sr0.sortingOrder < 0 && sr0.sortingOrder >= next)
                            next = sr0.sortingOrder + 1;
                    }
                var order = new List<int>();
                for (int i = 0; i < clipPropAsset.Count; i++) order.Add(i);
                order.Sort((a, b) => clipPropOrder[a].CompareTo(clipPropOrder[b]));
                int placedProps = 0;
                foreach (int i in order)
                {
                    if (clipPropAsset[i] == null) continue;
                    var inst = (GameObject)PrefabUtility.InstantiatePrefab(clipPropAsset[i]);
                    inst.transform.position = anchorW + clipPropOff[i];
                    inst.transform.localScale = clipPropScale[i];
                    if (parentGo != null) inst.transform.SetParent(parentGo.transform);
                    foreach (var sr2 in inst.GetComponentsInChildren<SpriteRenderer>())
                    {
                        sr2.sortingOrder = next;
                        sr2.flipX = clipPropFlipX[i];
                    }
                    next++;
                    foreach (var col in inst.GetComponentsInChildren<Collider2D>()) Object.DestroyImmediate(col);
                    Undo.RegisterCreatedObjectUndo(inst, "구간 붙여넣기");
                    placedProps++;
                }
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gtm.gameObject.scene);
                ShowNotification(new GUIContent("붙여넣음: 바닥 " + clipGroundOff.Count + "·벽 " + clipWallOff.Count
                    + "·소품 " + placedProps + (removedProps > 0 ? " (기존 소품 " + removedProps + "개 덮어씀)" : "")), 1.8d);
                Debug.Log("[쇼룸] 붙여넣기 @셀(" + anchorCell.x + "," + anchorCell.y + ") G" + clipGroundOff.Count + " W" + clipWallOff.Count + " P" + placedProps + " 제거 " + removedProps);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[쇼룸] 붙여넣기 실패: " + ex);
                ShowNotification(new GUIContent("붙여넣기 오류: " + ex.Message), 3d);
            }
        }

        // 소품 배치 모드: 클릭 지점에 프리팹 생성
        private void HandlePropPlace(SceneView sv)
        {
            var e = Event.current;
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                armedProp = null;
                sv.Repaint();
                Repaint();
                return;
            }
            int id = GUIUtility.GetControlID(FocusType.Passive);
            HandleUtility.AddDefaultControl(id);
            Ray ray0 = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            float dz0 = Mathf.Approximately(ray0.direction.z, 0f) ? 1f : ray0.direction.z;
            Vector3 world = ray0.origin + ray0.direction * Mathf.Max(0f, -ray0.origin.z / dz0);
            if (e.control)
            {
                world.x = Mathf.Round(world.x * 2f) * 0.5f;
                world.y = Mathf.Round(world.y * 2f) * 0.5f;
            }
            var srcSr = armedProp.GetComponentInChildren<SpriteRenderer>();
            Vector3 psize = srcSr != null && srcSr.sprite != null ? (Vector3)(srcSr.sprite.bounds.size) : new Vector3(1f, 1f, 0f);
            Handles.color = new Color(0.4f, 0.8f, 1f, 0.9f);
            Handles.DrawWireCube(world + new Vector3(0f, psize.y * 0.5f, 0f), psize);
            Handles.Label(world + new Vector3(0f, psize.y + 0.3f, 0f), armedProp.name);
            if (e.type == EventType.MouseMove) sv.Repaint();
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(armedProp);
                inst.transform.position = new Vector3(world.x, world.y, 0f);
                var parentGo = GameObject.Find("Stage_Props");
                if (parentGo != null) inst.transform.SetParent(parentGo.transform);
                int next = -300;
                if (parentGo != null)
                    foreach (Transform c in parentGo.transform)
                    {
                        var sr2 = c.GetComponentInChildren<SpriteRenderer>();
                        if (sr2 != null && sr2.sortingOrder >= -300 && sr2.sortingOrder < 0 && sr2.sortingOrder >= next)
                            next = sr2.sortingOrder + 1;
                    }
                foreach (var sr2 in inst.GetComponentsInChildren<SpriteRenderer>()) sr2.sortingOrder = next;
                foreach (var col in inst.GetComponentsInChildren<Collider2D>()) Object.DestroyImmediate(col);
                Undo.RegisterCreatedObjectUndo(inst, "쇼룸 소품 배치");
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(inst.scene);
                e.Use();
                sv.Repaint();
            }
        }

        private void OnSceneGUI(SceneView sv)
        {
            if (layerMoveMode) { HandleLayerMove(sv); return; }
            if (regionMode) { HandleRegionCopy(sv); return; }
            if (!inspectMode && armedProp != null) { HandlePropPlace(sv); return; }
            if (!inspectMode && armedTile != null) { HandleBrush(sv); return; }
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
            // 소품 판독: 렌더러 경계 포함, 정렬 높은 순 3개
            hitProps.Clear(); hitPropMeta.Clear();
            var foundSr = new List<SpriteRenderer>();
            foreach (var sr in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
            {
                if (sr.sprite == null) continue;
                var bb = sr.bounds;
                if (world.x < bb.min.x || world.x > bb.max.x || world.y < bb.min.y || world.y > bb.max.y) continue;
                foundSr.Add(sr);
            }
            foundSr.Sort((a, b) => b.sortingOrder.CompareTo(a.sortingOrder));
            int taken = 0;
            foreach (var sr in foundSr)
            {
                if (taken >= 3) break;
                GameObject srcPf = null;
                var nearest = PrefabUtility.GetNearestPrefabInstanceRoot(sr.gameObject);
                if (nearest != null) srcPf = PrefabUtility.GetCorrespondingObjectFromSource(nearest) as GameObject;
                if (srcPf == null) continue;
                if (hitProps.Contains(srcPf)) continue;
                hitProps.Add(srcPf);
                hitPropMeta.Add("위치(" + sr.transform.position.x.ToString("F1") + "," + sr.transform.position.y.ToString("F1") + ") 정렬 " + sr.sortingOrder);
                taken++;
            }
            if (hitTiles.Count > 0) JumpTo(hitTiles[0]);
            else if (hitProps.Count > 0) JumpToProp(hitProps[0]);
            e.Use();
            Repaint();
        }

        // 자체 붓: 장전된 타일을 씬 클릭·드래그로 직접 찍는다 (Shift=지우기, Esc=해제)
        private void HandleBrush(SceneView sv)
        {
            var e = Event.current;
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                armedTile = null;
                sv.Repaint();
                Repaint();
                return;
            }
            var go = GameObject.Find(armedTarget);
            if (go == null) return;
            var tm = go.GetComponent<Tilemap>();
            if (tm == null) return;
            int id = GUIUtility.GetControlID(FocusType.Passive);
            HandleUtility.AddDefaultControl(id);
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            float dz = Mathf.Approximately(ray.direction.z, 0f) ? 1f : ray.direction.z;
            Vector3 world = ray.origin + ray.direction * Mathf.Max(0f, -ray.origin.z / dz);
            var cell = tm.WorldToCell(world);
            // 셀 미리보기 테두리
            Vector3 c0 = tm.CellToWorld(cell);
            var cs = tm.cellSize;
            Handles.color = e.shift ? new Color(1f, 0.35f, 0.3f, 0.9f) : new Color(0.3f, 1f, 0.5f, 0.9f);
            Handles.DrawSolidRectangleWithOutline(new[] {
                c0, c0 + new Vector3(cs.x, 0f), c0 + new Vector3(cs.x, cs.y), c0 + new Vector3(0f, cs.y) },
                new Color(1f, 1f, 1f, 0.06f), Handles.color);
            if (e.type == EventType.MouseMove) sv.Repaint();
            if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 0)
            {
                if (e.shift)
                {
                    // 층 무관 지우개: 그 지점에 타일이 있는 타일맵 중 가장 앞(정렬 최상위)부터 지운다
                    Tilemap best = null;
                    Vector3Int bestCell = default(Vector3Int);
                    int bestOrder = int.MinValue;
                    foreach (var m in FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
                    {
                        var mc = m.WorldToCell(world);
                        if (m.GetTile(mc) == null) continue;
                        var r = m.GetComponent<TilemapRenderer>();
                        int ord = r != null ? r.sortingOrder : 0;
                        if (ord > bestOrder) { bestOrder = ord; best = m; bestCell = mc; }
                    }
                    if (best != null)
                    {
                        Undo.RegisterCompleteObjectUndo(best, "쇼룸 지우개");
                        best.SetTile(bestCell, null);
                        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(best.gameObject.scene);
                    }
                }
                else
                {
                    Undo.RegisterCompleteObjectUndo(tm, "쇼룸 붓");
                    tm.SetTile(cell, armedTile);
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(tm.gameObject.scene);
                }
                e.Use();
                sv.Repaint();
            }
        }

        private void OnGUI()
        {
            DrawLayerTool();
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
                if (inspectMode != prev)
                {
                    if (inspectMode) { regionMode = false; }
                    SceneView.RepaintAll();
                }
                bool prevR = regionMode;
                GUILayout.Label("붓 대상:", EditorStyles.miniLabel, GUILayout.Width(50f));
                bool selA = string.IsNullOrEmpty(customBrushTarget);
                if (GUILayout.Toggle(selA, "자동", EditorStyles.toolbarButton, GUILayout.Width(40f)) && !selA) customBrushTarget = null;
                bool selG = customBrushTarget == "Stage_Ground";
                if (GUILayout.Toggle(selG, "Ground", EditorStyles.toolbarButton, GUILayout.Width(56f)) && !selG) customBrushTarget = "Stage_Ground";
                bool selW = customBrushTarget == "Stage_Wall";
                if (GUILayout.Toggle(selW, "Wall", EditorStyles.toolbarButton, GUILayout.Width(44f)) && !selW) customBrushTarget = "Stage_Wall";
                regionMode = GUILayout.Toggle(regionMode, "구간 복사", EditorStyles.toolbarButton, GUILayout.Width(66f));
                if (regionMode) layerMoveMode = false;
                if (regionMode != prevR)
                {
                    if (regionMode) { inspectMode = false; armedTile = null; armedProp = null; }
                    else { ClearClip(); regionDragStart = null; }
                    SceneView.RepaintAll();
                }
                using (new EditorGUI.DisabledScope(!(highlight is TileBase)))
                    if (GUILayout.Button("선택 타일로 칠하기", EditorStyles.toolbarButton, GUILayout.Width(110f)))
                    {
                        inspectMode = false;
                        ShowNotification(new GUIContent(PaintWith((TileBase)highlight)));
                    }
                GUILayout.FlexibleSpace();
                if (armedTile != null || armedProp != null)
                {
                    string armName = armedTile != null ? armedTile.name + " → " + armedTarget : armedProp.name + " → 배치";
                    GUILayout.Label("장전: " + armName, EditorStyles.miniLabel);
                    if (GUILayout.Button("해제", EditorStyles.toolbarButton, GUILayout.Width(40f)))
                    {
                        armedTile = null;
                        armedProp = null;
                        SceneView.RepaintAll();
                    }
                }
                if (familyNames[tab].Length > 0)
                    GUILayout.Label(families[tab][familyNames[tab][familyIndex[tab]]].Count + "개");
            }

            if (regionMode)
                EditorGUILayout.HelpBox(hasClip
                    ? "구간 복사: 클립 준비됨 (" + clipSize.x + "x" + clipSize.y + ") — 씬 클릭=붙여넣기(반복 가능), Esc=클립 비우기"
                    : "구간 복사: 씬에서 왼쪽 드래그로 범위를 지정하세요 (바닥+벽+소품 통째 복사)", MessageType.Info);
            if (inspectMode) DrawInspectPanel();
            if (tab == 1)
                EditorGUILayout.HelpBox("클릭=프로젝트에서 선택 / 셀을 잡고 씬 뷰로 드래그=바로 배치", MessageType.None);
            if (familyNames[tab].Length == 0) return;

            var items = families[tab][familyNames[tab][familyIndex[tab]]];
            float cw = CellSize.x * zoom, ch = CellSize.y * zoom;
            int cols = Mathf.Max(1, Mathf.FloorToInt((position.width - 24f) / cw));
            // 자동 스크롤 (검사 점프)
            if (pendingScrollTo != null)
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
                        highlight = o;
                        if (tab == 0 && o is TileBase)
                        {
                            // 타일 탭: 선택 변경 없이(도구 풀림 방지) 핑만 + 즉시 붓 장전
                            EditorGUIUtility.PingObject(o);
                            inspectMode = false;
                            try
                            {
                                // 타일 성격으로 대상 결정: 벽 타일은 항상 벽 겹으로 (사용자 팔레트 설정 존중)
                                string targetTm = "Stage_Ground";
                                if (o.name.Contains("Tileable") || o.name.Contains("Wall") || IsForestDeco(o.name))
                                    targetTm = "Stage_Wall";
                                if (familyNames[0].Length > 0)
                                {
                                    string curFam = familyNames[0][Mathf.Clamp(familyIndex[0], 0, familyNames[0].Length - 1)];
                                    if (curFam.Contains("Stage_Wall")) targetTm = "Stage_Wall";
                                    else if (curFam.Contains("Stage_Ground")) targetTm = "Stage_Ground";
                                }
                                if (!string.IsNullOrEmpty(customBrushTarget)) targetTm = customBrushTarget;
                                ShowNotification(new GUIContent(PaintWith((TileBase)o, targetTm)), 1.2d);
                            }
                            catch (System.Exception ex)
                            {
                                ShowNotification(new GUIContent("오류: " + ex.Message), 2d);
                            }
                        }
                        else if (tab == 1 && o is GameObject)
                        {
                            EditorGUIUtility.PingObject(o);
                            inspectMode = false;
                            ShowNotification(new GUIContent(PlaceWith((GameObject)o)), 1.2d);
                        }
                        else
                        {
                            Selection.activeObject = o;
                            EditorGUIUtility.PingObject(o);
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
                SceneButton("숲 데모", "Assets/sanctum_pixel/forest_side_pack/demo_scene.unity");
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
                            string tgt = (t.name.Contains("Tileable") || t.name.Contains("Wall") || IsForestDeco(t.name)) ? "Stage_Wall" : "Stage_Ground";
                            if (!string.IsNullOrEmpty(customBrushTarget)) tgt = customBrushTarget;
                            ShowNotification(new GUIContent(PaintWith(t, tgt)));
                        }
                    }
                }
                for (int i = 0; i < hitProps.Count; i++)
                {
                    var p = hitProps[i];
                    if (p == null) continue;
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        Rect r2 = GUILayoutUtility.GetRect(52f, 52f, GUILayout.Width(52f));
                        Texture2D pv2 = AssetPreview.GetAssetPreview(p);
                        if (pv2 != null) GUI.DrawTexture(r2, pv2, ScaleMode.ScaleToFit);
                        else EditorGUI.DrawRect(r2, new Color(0.2f, 0.2f, 0.2f));
                        using (new EditorGUILayout.VerticalScope())
                        {
                            GUILayout.Label(p.name, EditorStyles.boldLabel);
                            GUILayout.Label(hitPropMeta[i], EditorStyles.miniLabel);
                        }
                        if (GUILayout.Button("소품 탭에서 보기", GUILayout.Width(108f), GUILayout.Height(24f)))
                            JumpToProp(p);
                        if (GUILayout.Button("📦 이 소품 배치", GUILayout.Width(108f), GUILayout.Height(24f)))
                        {
                            inspectMode = false;
                            ShowNotification(new GUIContent(PlaceWith(p)));
                        }
                    }
                }
            }
        }
    }
}
