using UnityEngine;
using UnityEngine.Audio;

namespace NAN2026
{
    // 설정 화면의 볼륨 슬라이더(0~1)와 AudioMixer의 노출 파라미터(dB)를 잇는다.
    // MainMixer 에셋에 Master/BGM/SFX 볼륨을 각각 "MasterVolume"/"BGMVolume"/"SFXVolume"
    // 이름으로 Expose 해둬야 한다 (Mixer 창 Groups에서 Volume 슬라이더 우클릭 → Expose).
    public class AudioMixerSettings : MonoBehaviour
    {
        public AudioMixer mixer;

        private const string MasterParam = "MasterVolume";
        private const string BgmParam = "BGMVolume";
        private const string SfxParam = "SFXVolume";

        private const string MasterPrefKey = "vol_master";
        private const string BgmPrefKey = "vol_bgm";
        private const string SfxPrefKey = "vol_sfx";

        private void Start()
        {
            // 저장된 값이 없으면 100%(슬라이더 1.0)로 시작
            SetMasterVolume(PlayerPrefs.GetFloat(MasterPrefKey, 1f));
            SetBgmVolume(PlayerPrefs.GetFloat(BgmPrefKey, 1f));
            SetSfxVolume(PlayerPrefs.GetFloat(SfxPrefKey, 1f));
        }

        /// <summary>슬라이더(0~1)에 그대로 연결. 0이면 완전 무음(-80dB), 1이면 0dB(원본 볼륨).</summary>
        public void SetMasterVolume(float linear01) => Apply(MasterParam, MasterPrefKey, linear01);
        public void SetBgmVolume(float linear01) => Apply(BgmParam, BgmPrefKey, linear01);
        public void SetSfxVolume(float linear01) => Apply(SfxParam, SfxPrefKey, linear01);

        private void Apply(string param, string prefKey, float linear01)
        {
            if (mixer == null) return;
            float clamped = Mathf.Clamp01(linear01);
            // 0을 그대로 Log10에 넣으면 -Infinity라 슬라이더 맨 끝에서 특별 취급
            float db = clamped <= 0.0001f ? -80f : Mathf.Log10(clamped) * 20f;
            mixer.SetFloat(param, db);
            PlayerPrefs.SetFloat(prefKey, clamped);
        }
    }
}