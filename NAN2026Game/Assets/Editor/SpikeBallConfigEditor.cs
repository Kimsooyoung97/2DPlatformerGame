using UnityEditor;
using UnityEngine;
using System.Reflection;

[CustomEditor(typeof(NAN2026.SpikeBallConfig))]
public class SpikeBallConfigEditor : Editor
{
    Texture2D waveTex;
    AudioClip cachedClip;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var cfg = (NAN2026.SpikeBallConfig)target;
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("◆ 패링음 구간 트리머", EditorStyles.boldLabel);

        if (cfg.clashSound == null) { EditorGUILayout.HelpBox("clashSound 슬롯이 비어있다.", MessageType.Info); return; }
        var clip = cfg.clashSound;
        float lenMs = clip.length * 1000f;
        EditorGUILayout.LabelField($"클립: {clip.name}  |  길이 {lenMs:F0}ms  |  {clip.frequency}Hz {clip.channels}ch");

        // 파형 생성(캐시)
        if (waveTex == null || cachedClip != clip) { waveTex = BuildWaveform(clip, 600, 90); cachedClip = clip; }

        // 파형 + 구간 오버레이
        Rect r = GUILayoutUtility.GetRect(600, 90, GUILayout.ExpandWidth(true));
        if (waveTex != null) GUI.DrawTexture(r, waveTex, ScaleMode.StretchToFill);
        float s01 = Mathf.Clamp01(cfg.clashSoundStartMs / lenMs);
        float e01 = Mathf.Clamp01((cfg.clashSoundEndMs <= 0 ? lenMs : cfg.clashSoundEndMs) / lenMs);
        // 선택 구간 음영
        var selRect = new Rect(r.x + r.width * s01, r.y, r.width * (e01 - s01), r.height);
        EditorGUI.DrawRect(selRect, new Color(0.3f, 0.8f, 1f, 0.25f));
        // start/end 라인
        EditorGUI.DrawRect(new Rect(r.x + r.width * s01 - 1, r.y, 2, r.height), new Color(0.2f, 1f, 0.4f, 0.9f));
        EditorGUI.DrawRect(new Rect(r.x + r.width * e01 - 1, r.y, 2, r.height), new Color(1f, 0.4f, 0.3f, 0.9f));

        // 파형 클릭: 좌클릭=start, 우클릭/Shift=end
        Event ev = Event.current;
        if (ev.type == EventType.MouseDown && r.Contains(ev.mousePosition))
        {
            float f = Mathf.Clamp01((ev.mousePosition.x - r.x) / r.width) * lenMs;
            Undo.RecordObject(cfg, "트림 구간");
            if (ev.button == 1 || ev.shift) cfg.clashSoundEndMs = f; else cfg.clashSoundStartMs = f;
            EditorUtility.SetDirty(cfg); ev.Use();
        }
        EditorGUILayout.LabelField("좌클릭=시작(초록) · 우클릭/Shift=끝(빨강)", EditorStyles.miniLabel);

        // 정밀 슬라이더 + 숫자
        cfg.clashSoundStartMs = EditorGUILayout.Slider("시작(ms)", cfg.clashSoundStartMs, 0f, lenMs);
        cfg.clashSoundEndMs = EditorGUILayout.Slider("끝(ms)", cfg.clashSoundEndMs <= 0 ? lenMs : cfg.clashSoundEndMs, 0f, lenMs);
        // 미세조정 ±5ms
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("시작 -5")) cfg.clashSoundStartMs = Mathf.Max(0, cfg.clashSoundStartMs - 5);
        if (GUILayout.Button("시작 +5")) cfg.clashSoundStartMs += 5;
        if (GUILayout.Button("끝 -5")) cfg.clashSoundEndMs -= 5;
        if (GUILayout.Button("끝 +5")) cfg.clashSoundEndMs = Mathf.Min(lenMs, cfg.clashSoundEndMs + 5);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.LabelField($"선택 길이: {(cfg.clashSoundEndMs - cfg.clashSoundStartMs):F0}ms");

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("▶ 시작점부터")) { StopAll(); Play(clip, cfg.clashSoundStartMs / 1000f); }
        if (GUILayout.Button("▶ 끝점 확인")) { StopAll(); Play(clip, Mathf.Max(0, cfg.clashSoundEndMs / 1000f - 0.15f)); }
        if (GUILayout.Button("■ 정지")) StopAll();
        EditorGUILayout.EndHorizontal();

        if (GUI.changed) EditorUtility.SetDirty(cfg);
    }

    static Texture2D BuildWaveform(AudioClip clip, int w, int h)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        var bg = new Color(0.12f, 0.12f, 0.14f, 1f);
        for (int i = 0; i < w * h; i++) tex.SetPixel(i % w, i / w, bg);
        var data = new float[clip.samples * clip.channels];
        clip.GetData(data, 0);
        int step = Mathf.Max(1, data.Length / w);
        for (int x = 0; x < w; x++)
        {
            float mx = 0f;
            for (int j = 0; j < step; j++) { int idx = x * step + j; if (idx < data.Length) mx = Mathf.Max(mx, Mathf.Abs(data[idx])); }
            int half = h / 2, amp = (int)(mx * half);
            for (int y = half - amp; y <= half + amp; y++) if (y >= 0 && y < h) tex.SetPixel(x, y, new Color(0.4f, 0.9f, 1f, 1f));
        }
        tex.Apply();
        return tex;
    }

    static void Play(AudioClip clip, float startSec)
    {
        var au = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
        var m = au.GetMethod("PlayPreviewClip", BindingFlags.Static | BindingFlags.Public, null, new[] { typeof(AudioClip), typeof(int), typeof(bool) }, null);
        if (m != null) m.Invoke(null, new object[] { clip, (int)(startSec * clip.frequency), false });
    }
    static void StopAll()
    {
        var au = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
        var m = au.GetMethod("StopAllPreviewClips", BindingFlags.Static | BindingFlags.Public);
        if (m != null) m.Invoke(null, null);
    }
}
