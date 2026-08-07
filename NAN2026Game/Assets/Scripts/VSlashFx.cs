using UnityEngine;

namespace NAN2026
{
    // V콤보 제자리 슬래시 오버레이: 프레임 재생 후 자멸
    public static class VSlashFx
    {
        public static void Play(Vector3 pos, Sprite[] frames, float fps, bool flipX, float scale, float alpha = 1f, Transform follow = null, Color? tint = null)
        {
            if (frames == null || frames.Length == 0) return;
            var go = new GameObject("VSlashFx");
            go.transform.position = pos;
            if (follow != null) go.transform.SetParent(follow, true); // 추종 모드: 시전자를 따라다님
            go.transform.localScale = new Vector3(scale, scale, 1f);
            var a = go.AddComponent<VSlashFxAnim>();
            a.frames = frames; a.fps = fps <= 0f ? 18f : fps;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 900; sr.flipX = flipX; sr.sprite = frames[0];
            var tc = tint ?? Color.white;
            sr.color = new Color(tc.r, tc.g, tc.b, Mathf.Clamp01(alpha));
            a.sr = sr;
        }
    }
    public class VSlashFxAnim : MonoBehaviour
    {
        public Sprite[] frames; public float fps = 18f; public SpriteRenderer sr;
        float t;
        void Update()
        {
            t += Time.deltaTime;
            int idx = (int)(t * fps);
            if (idx >= frames.Length) { Destroy(gameObject); return; }
            sr.sprite = frames[idx];
        }
    }
}
