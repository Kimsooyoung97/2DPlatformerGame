using UnityEditor;
using UnityEngine;

namespace NAN2026.EditorTools
{
    /// <summary>
    /// 선택된 GameObject(프리팹 애셋이든 씬 인스턴스든)와 그 모든 자식을 재귀적으로 훑어서
    /// Missing Script(컴포넌트는 있는데 원본 .cs가 없어서 깨진 것)를 전부 제거한다.
    /// Hierarchy에서 눈으로 못 찾을 때, 또는 콘솔 에러가 어느 오브젝트인지 핑이 안 될 때 쓴다.
    /// </summary>
    public static class RemoveMissingScriptsTool
    {
        [MenuItem("Tools/NAN2026/선택 오브젝트에서 Missing Script 제거 (재귀)")]
        private static void RemoveMissingScriptsRecursively()
        {
            GameObject[] selected = Selection.gameObjects;
            if (selected.Length == 0)
            {
                Debug.LogWarning("[MissingScript] Hierarchy나 Project 창에서 오브젝트(또는 프리팹)를 먼저 선택하세요.");
                return;
            }

            int totalRemoved = 0;
            int totalObjects = 0;

            foreach (GameObject go in selected)
            {
                totalRemoved += RemoveRecursively(go.transform, ref totalObjects);
            }

            Debug.Log($"[MissingScript] 검사한 오브젝트 {totalObjects}개 중 Missing Script {totalRemoved}개 제거 완료. " +
                      $"프리팹이면 지금 Ctrl+S로 저장하세요.");

            if (totalRemoved > 0)
            {
                EditorUtility.SetDirty(selected[0]);
                // 프리팹 애셋을 직접 선택한 상태였다면 애셋에 바로 반영되도록 저장까지 시도한다.
                string path = AssetDatabase.GetAssetPath(selected[0]);
                if (!string.IsNullOrEmpty(path) && path.EndsWith(".prefab"))
                {
                    PrefabUtility.SavePrefabAsset(selected[0]);
                    Debug.Log($"[MissingScript] 프리팹 애셋에 즉시 저장했습니다: {path}");
                }
            }
        }

        private static int RemoveRecursively(Transform t, ref int totalObjects)
        {
            totalObjects++;
            int removedHere = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
            if (removedHere > 0)
                Debug.Log($"[MissingScript] '{GetPath(t)}' 에서 {removedHere}개 제거", t.gameObject);

            int removedTotal = removedHere;
            for (int i = 0; i < t.childCount; i++)
                removedTotal += RemoveRecursively(t.GetChild(i), ref totalObjects);

            return removedTotal;
        }

        private static string GetPath(Transform t)
        {
            string path = t.name;
            while (t.parent != null)
            {
                t = t.parent;
                path = t.name + "/" + path;
            }
            return path;
        }
    }
}