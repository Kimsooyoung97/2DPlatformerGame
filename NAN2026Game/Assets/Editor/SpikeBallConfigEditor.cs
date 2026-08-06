using UnityEditor;
using UnityEngine;
using System.Reflection;
[CustomEditor(typeof(NAN2026.SpikeBallConfig))]
public class SpikeBallConfigEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var cfg = (NAN2026.SpikeBallConfig)target;
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("패링음 구간 미리듣기", EditorStyles.boldLabel);
        if (cfg.clashSound != null)
        {
            EditorGUILayout.LabelField("클립 길이: " + (cfg.clashSound.length*1000f).ToString("F0") + "ms");
            if (GUILayout.Button("▶ 지정 구간 재생"))
            {
                StopAll();
                float st = Mathf.Clamp(cfg.clashSoundStartMs/1000f, 0f, cfg.clashSound.length);
                PlayClipSegment(cfg.clashSound, st);
            }
            if (GUILayout.Button("■ 정지")) StopAll();
        }
        else EditorGUILayout.HelpBox("clashSound가 비어있음", MessageType.Info);
    }
    static void PlayClipSegment(AudioClip clip, float startSec)
    {
        var asm = typeof(AudioImporter).Assembly;
        var au = asm.GetType("UnityEditor.AudioUtil");
        var m = au.GetMethod("PlayPreviewClip", BindingFlags.Static|BindingFlags.Public, null, new[]{typeof(AudioClip),typeof(int),typeof(bool)}, null);
        int startSample = (int)(startSec * clip.frequency);
        if (m != null) m.Invoke(null, new object[]{clip, startSample, false});
    }
    static void StopAll()
    {
        var asm = typeof(AudioImporter).Assembly;
        var au = asm.GetType("UnityEditor.AudioUtil");
        var m = au.GetMethod("StopAllPreviewClips", BindingFlags.Static|BindingFlags.Public);
        if (m != null) m.Invoke(null, null);
    }
}
