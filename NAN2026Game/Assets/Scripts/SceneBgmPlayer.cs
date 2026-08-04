using UnityEngine;

namespace NAN2026
{
    public class SceneBgmPlayer : MonoBehaviour
    {
        public SoundConfig config;
        public AudioSource source;
        float t;

        void Start()
        {
            if (source == null) return;
            source.loop = true;
            source.volume = 0f;
            source.Play();
        }

        void Update()
        {
            if (source == null || config == null) return;
            t += Time.deltaTime;
            float dur = config.bgmFadeSeconds < 0.0001f ? 0.0001f : config.bgmFadeSeconds;
            source.volume = config.bgmVolume * Mathf.Clamp01(t / dur);
            if (t >= dur) enabled = false;
        }
    }
}
