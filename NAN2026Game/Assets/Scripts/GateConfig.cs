using UnityEngine;

namespace NAN2026
{
    [CreateAssetMenu(fileName = "GateConfig", menuName = "NAN2026/GateConfig")]
    public class GateConfig : ScriptableObject
    {
        [Header("페이즈(초)")]
        [Header("전멸 개방 조건")]
        [Tooltip("이 y 범위에 배치된 몬스터를 전부 처치하면 방어막이 무너진다. 1·2단만 포함하고 보스 구역(y10)은 제외")]
        public float clearMinY = -1f;
        public float clearMaxY = 5f;
        [Tooltip("생존 수를 다시 세는 간격(초)")]
        public float clearCheckInterval = 0.25f;
        [Tooltip("남은 적 라벨 표시 여부")]
        public bool showRemainingLabel = true;
        public float labelFadeSeconds = 1.5f;

        public float delaySeconds = 0.4f;
        public float collapseSeconds = 0.8f;
        public float holdSeconds = 0.6f;

        [Header("개방부 조명")]
        public float lightIntensity = 1.2f;
        public float lightRadius = 2.2f;
        public Color lightColor = new Color(1f, 0.72f, 0.35f, 1f);

        [Header("파편·먼지")]
        public int debrisCount = 14;
        public float shakeAmplitude = 1.7f;
        public float zoomFactor = 0.7f;
        public float shakeFrequency = 0.5f;

        [Header("결계")]
        public Color barrierColor = new Color(0.55f, 0.9f, 1f, 0.75f);
        public float barrierLightIntensity = 0.7f;

        [Header("사운드")]
        public float sfxVolume = 1f;
        public float sfxPitch = 0.85f;
        public float rumbleVolume = 1f;
        public float debrisImpulse = 2.5f;
        public float debrisLifetime = 2.5f;
        public float dustLifetime = 3f;
    }
}
