using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Assets/Configs 아래 모든 ScriptableObject의 단순 필드(float/int/bool/string)를
/// 엑셀에서 바로 열고 편집할 수 있는 CSV 한 장으로 내보내고, 편집한 CSV를 다시
/// 해당 Config 에셋에 적용하는 도구.
///
/// 런타임 코드는 전혀 바뀌지 않는다 — 여전히 ScriptableObject Config가 수치를
/// 소유하고(SPEC.md 규칙), 이 도구는 그 값을 에디터에서 CSV로 왕복시키는 역할만 한다.
/// Vector/Color/LayerMask/배열 등 복합 타입은 CSV로 다루지 않고 인스펙터에서 직접 편집한다.
/// </summary>
public static class GameDataTableTool
{
    private const string CsvPath = "Assets/_Data/GameDataTable.csv";
    private const string ConfigsFolder = "Assets/Configs";

    [MenuItem("NAN2026/데이터 테이블/CSV로 내보내기 (Config → CSV)")]
    public static void Export()
    {
        var rows = new List<string> { "AssetPath,FieldName,Value,Note" };
        int count = 0;

        foreach (var so in FindAllConfigs())
        {
            string path = AssetDatabase.GetAssetPath(so);
            foreach (var field in GetSupportedFields(so.GetType()))
            {
                object value = field.GetValue(so);
                rows.Add(string.Join(",", new[] {
                    path,
                    field.Name,
                    FormatValue(value),
                    DescribeField(field)
                }));
                count++;
            }
        }

        string dir = Path.GetDirectoryName(CsvPath);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        File.WriteAllLines(CsvPath, rows, System.Text.Encoding.UTF8);
        AssetDatabase.Refresh();
        Debug.Log("[GameDataTable] " + count + "개 필드를 " + CsvPath + "로 내보냈습니다. 엑셀로 열어 편집한 뒤 저장(CSV 형식 유지)하고 '적용하기'를 실행하세요.");
    }

    [MenuItem("NAN2026/데이터 테이블/CSV 적용하기 (CSV → Config)")]
    public static void Import()
    {
        if (!File.Exists(CsvPath))
        {
            Debug.LogError("[GameDataTable] CSV가 없습니다: " + CsvPath + " — 먼저 내보내기를 실행하세요.");
            return;
        }

        string[] lines = File.ReadAllLines(CsvPath, System.Text.Encoding.UTF8);
        int applied = 0, skipped = 0;
        var touched = new HashSet<ScriptableObject>();

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;

            string[] parts = SplitCsvLine(line);
            if (parts.Length < 3) { skipped++; continue; }

            string assetPath = parts[0].Trim();
            string fieldName = parts[1].Trim();
            string valueStr = parts[2].Trim();

            var so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
            if (so == null) { Debug.LogWarning("[GameDataTable] 에셋을 찾을 수 없음: " + assetPath); skipped++; continue; }

            FieldInfo field = so.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
            if (field == null || !IsSupportedType(field.FieldType)) { skipped++; continue; }

            if (!TryApplyValue(so, field, valueStr)) { skipped++; continue; }

            touched.Add(so);
            applied++;
        }

        foreach (var so in touched) EditorUtility.SetDirty(so);
        AssetDatabase.SaveAssets();
        Debug.Log("[GameDataTable] " + applied + "개 필드 적용, " + skipped + "개 건너뜀 (" + touched.Count + "개 에셋 갱신)");
    }

    private static IEnumerable<ScriptableObject> FindAllConfigs()
    {
        string[] guids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { ConfigsFolder });
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            if (so != null) yield return so;
        }
    }

    private static IEnumerable<FieldInfo> GetSupportedFields(System.Type type)
    {
        foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            if (IsSupportedType(field.FieldType)) yield return field;
        }
    }

    private static bool IsSupportedType(System.Type t)
    {
        return t == typeof(float) || t == typeof(int) || t == typeof(bool) || t == typeof(string);
    }

    private static string FormatValue(object value)
    {
        if (value is float f) return f.ToString(CultureInfo.InvariantCulture);
        if (value == null) return string.Empty;
        return value.ToString();
    }

    private static bool TryApplyValue(object target, FieldInfo field, string valueStr)
    {
        System.Type t = field.FieldType;
        try
        {
            if (t == typeof(float))
            {
                field.SetValue(target, float.Parse(valueStr, CultureInfo.InvariantCulture));
                return true;
            }
            if (t == typeof(int))
            {
                field.SetValue(target, int.Parse(valueStr, CultureInfo.InvariantCulture));
                return true;
            }
            if (t == typeof(bool))
            {
                string v = valueStr.Trim().ToLowerInvariant();
                field.SetValue(target, v == "true" || v == "1");
                return true;
            }
            if (t == typeof(string))
            {
                field.SetValue(target, valueStr);
                return true;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[GameDataTable] 값 파싱 실패: " + field.Name + "=" + valueStr + " ("+ e.Message + ")");
        }
        return false;
    }

    private static string DescribeField(FieldInfo field)
    {
        var tooltip = field.GetCustomAttribute<TooltipAttribute>();
        return tooltip != null ? tooltip.tooltip.Replace(",", " ") : string.Empty;
    }

    // 단순 콤마 구분. 우리 데이터는 값에 콤마가 들어가지 않는 숫자/불리언/짧은 문자열이라
    // RFC4180 수준의 완전한 CSV 파서는 필요하지 않다. Note 칸에 콤마가 들어가면 잘릴 수 있음.
    private static string[] SplitCsvLine(string line)
    {
        return line.Split(',');
    }
}
