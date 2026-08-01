using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace NAN2026.EditorTools
{
    // 타일 쇼룸: 팩의 타일을 계열별로 격자 진열하는 에디터 창.
    // 타일 클릭 시 프로젝트 창에서 해당 에셋을 핑+선택한다.
    public class TileShowroomWindow : EditorWindow
    {
        private const string SearchRoot = "Assets/Cainos";
        private static readonly Vector2 CellSize = new Vector2(72f, 92f);

        private Dictionary<string, List<TileBase>> families = new Dictionary<string, List<TileBase>>();
        private string[] familyNames = new string[0];
        private int familyIndex;
        private Vector2 scroll;
        private float zoom = 1.0f;

        [MenuItem("NAN2026/타일 쇼룸")]
        public static void Open()
        {
            var w = GetWindow<TileShowroomWindow>("타일 쇼룸");
            w.minSize = new Vector2(480f, 320f);
            w.RefreshFamilies();
        }

        // 파일명에서 계열 접두 추출 (끝의 _숫자 제거). 순수 로직.
        public static string FamilyKeyOf(string tileName)
        {
            int us = tileName.LastIndexOf('_');
            if (us <= 0) return tileName;
            int dummy;
            return int.TryParse(tileName.Substring(us + 1), out dummy) ? tileName.Substring(0, us) : tileName;
        }

        private void RefreshFamilies()
        {
            families.Clear();
            foreach (string guid in AssetDatabase.FindAssets("t:TileBase", new[] { SearchRoot }))
            {
                var tile = AssetDatabase.LoadAssetAtPath<TileBase>(AssetDatabase.GUIDToAssetPath(guid));
                if (tile == null) continue;
                string key = FamilyKeyOf(tile.name);
                if (!families.ContainsKey(key)) families[key] = new List<TileBase>();
                families[key].Add(tile);
            }
            foreach (var list in families.Values)
                list.Sort((a, b) => NumberOf(a.name).CompareTo(NumberOf(b.name)));
            familyNames = families.Keys.OrderBy(k => k).ToArray();
            familyIndex = Mathf.Clamp(familyIndex, 0, Mathf.Max(0, familyNames.Length - 1));
        }

        public static int NumberOf(string tileName)
        {
            int us = tileName.LastIndexOf('_');
            int n;
            return (us > 0 && int.TryParse(tileName.Substring(us + 1), out n)) ? n : 0;
        }

        private void OnGUI()
        {
            if (familyNames.Length == 0) RefreshFamilies();
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                familyIndex = EditorGUILayout.Popup(familyIndex, familyNames, EditorStyles.toolbarPopup, GUILayout.Width(260f));
                zoom = GUILayout.HorizontalSlider(zoom, 0.6f, 1.6f, GUILayout.Width(110f));
                if (GUILayout.Button("새로고침", EditorStyles.toolbarButton, GUILayout.Width(70f))) RefreshFamilies();
                GUILayout.FlexibleSpace();
                if (familyNames.Length > 0)
                    GUILayout.Label(families[familyNames[familyIndex]].Count + "개");
            }
            if (familyNames.Length == 0) { EditorGUILayout.HelpBox("타일을 찾지 못했습니다: " + SearchRoot, MessageType.Info); return; }

            var tiles = families[familyNames[familyIndex]];
            float cw = CellSize.x * zoom, ch = CellSize.y * zoom;
            int cols = Mathf.Max(1, Mathf.FloorToInt((position.width - 20f) / cw));
            int rows = Mathf.CeilToInt(tiles.Count / (float)cols);
            scroll = EditorGUILayout.BeginScrollView(scroll);
            Rect area = GUILayoutUtility.GetRect(cols * cw, rows * ch);
            for (int i = 0; i < tiles.Count; i++)
            {
                var t = tiles[i];
                Rect cell = new Rect(area.x + (i % cols) * cw, area.y + (i / cols) * ch, cw, ch);
                Rect img = new Rect(cell.x + 4f, cell.y + 4f, cw - 8f, cw - 8f);
                Texture2D preview = AssetPreview.GetAssetPreview(t);
                if (preview != null) GUI.DrawTexture(img, preview, ScaleMode.ScaleToFit);
                else EditorGUI.DrawRect(img, new Color(0.2f, 0.2f, 0.2f));
                GUI.Label(new Rect(cell.x, img.yMax, cw, 18f), NumberOf(t.name).ToString(), EditorStyles.centeredGreyMiniLabel);
                if (GUI.Button(cell, GUIContent.none, GUIStyle.none))
                {
                    Selection.activeObject = t;
                    EditorGUIUtility.PingObject(t);
                }
            }
            EditorGUILayout.EndScrollView();
            if (AssetPreview.IsLoadingAssetPreviews()) Repaint();
        }
    }
}
