using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace NAN2026.EditorTools
{
    // 에셋 쇼룸: 팩의 타일·소품 프리팹을 계열별로 격자 진열하는 에디터 창.
    // 클릭=프로젝트 핑+선택, 소품은 창에서 씬으로 드래그해 바로 배치 가능.
    public class TileShowroomWindow : EditorWindow
    {
        private const string SearchRoot = "Assets/Cainos";
        private static readonly Vector2 CellSize = new Vector2(84f, 104f);
        private static readonly string[] Tabs = { "타일", "소품" };

        private Dictionary<string, List<Object>>[] families =
            { new Dictionary<string, List<Object>>(), new Dictionary<string, List<Object>>() };
        private string[][] familyNames = { new string[0], new string[0] };
        private int[] familyIndex = { 0, 0 };
        private Vector2 scroll;
        private float zoom = 1.0f;
        private int tab;

        private void EnsureInit()
        {
            if (families[0] == null || families[1] == null)
            {
                families[0] = new Dictionary<string, List<Object>>();
                families[1] = new Dictionary<string, List<Object>>();
            }
            if (familyNames == null || familyNames.Length != 2 || familyNames[0] == null || familyNames[1] == null)
                familyNames = new[] { new string[0], new string[0] };
            if (familyIndex == null || familyIndex.Length != 2)
                familyIndex = new[] { 0, 0 };
        }

        private void OnEnable()
        {
            // 도메인 리로드·구버전 직렬화 잔재 복구
            EnsureInit();
            RefreshAll();
        }

        [MenuItem("NAN2026/에셋 쇼룸")]
        public static void Open()
        {
            var w = GetWindow<TileShowroomWindow>("에셋 쇼룸");
            w.minSize = new Vector2(520f, 340f);
            w.RefreshAll();
        }

        // 타일 계열: 끝의 _숫자 제거. 순수 로직.
        public static string TileFamilyOf(string tileName)
        {
            int us = tileName.LastIndexOf('_');
            if (us <= 0) return tileName;
            int dummy;
            return int.TryParse(tileName.Substring(us + 1), out dummy) ? tileName.Substring(0, us) : tileName;
        }

        // 소품 계열: 팩 접두 제거 후 끝의 번호·변형 문자 제거. 순수 로직.
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
            for (int t = 0; t < 2; t++)
            {
                foreach (var list in families[t].Values)
                    list.Sort((a, b) => NumberOf(a.name) != NumberOf(b.name)
                        ? NumberOf(a.name).CompareTo(NumberOf(b.name))
                        : string.CompareOrdinal(a.name, b.name));
                familyNames[t] = families[t].Keys.OrderBy(k => k).ToArray();
                familyIndex[t] = Mathf.Clamp(familyIndex[t], 0, Mathf.Max(0, familyNames[t].Length - 1));
            }
        }

        private void Add(int t, string key, Object o)
        {
            if (!families[t].ContainsKey(key)) families[t][key] = new List<Object>();
            families[t][key].Add(o);
        }

        private void OnGUI()
        {
            EnsureInit();
            if (familyNames[0].Length == 0 && familyNames[1].Length == 0) RefreshAll();
            tab = GUILayout.Toolbar(tab, Tabs);
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (familyNames[tab].Length > 0)
                    familyIndex[tab] = EditorGUILayout.Popup(Mathf.Clamp(familyIndex[tab], 0, familyNames[tab].Length - 1), familyNames[tab], EditorStyles.toolbarPopup, GUILayout.Width(280f));
                else
                    GUILayout.Label("(비어 있음)", EditorStyles.toolbarButton, GUILayout.Width(280f));
                zoom = GUILayout.HorizontalSlider(zoom, 0.6f, 1.8f, GUILayout.Width(110f));
                if (GUILayout.Button("새로고침", EditorStyles.toolbarButton, GUILayout.Width(70f))) RefreshAll();
                GUILayout.FlexibleSpace();
                if (familyNames[tab].Length > 0)
                    GUILayout.Label(families[tab][familyNames[tab][familyIndex[tab]]].Count + "개");
            }
            if (familyNames[tab].Length == 0) { EditorGUILayout.HelpBox("자산 없음: " + SearchRoot, MessageType.Info); return; }
            if (tab == 1)
                EditorGUILayout.HelpBox("클릭=프로젝트에서 선택 / 셀을 잡고 씬 뷰로 드래그=바로 배치", MessageType.None);

            var items = families[tab][familyNames[tab][familyIndex[tab]]];
            float cw = CellSize.x * zoom, ch = CellSize.y * zoom;
            int cols = Mathf.Max(1, Mathf.FloorToInt((position.width - 24f) / cw));
            int rows = Mathf.CeilToInt(items.Count / (float)cols);
            scroll = EditorGUILayout.BeginScrollView(scroll);
            Rect area = GUILayoutUtility.GetRect(cols * cw, rows * ch);
            for (int i = 0; i < items.Count; i++)
            {
                var o = items[i];
                Rect cell = new Rect(area.x + (i % cols) * cw, area.y + (i / cols) * ch, cw, ch);
                Rect img = new Rect(cell.x + 4f, cell.y + 4f, cw - 8f, cw - 26f);
                Texture2D preview = AssetPreview.GetAssetPreview(o);
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
    }
}
