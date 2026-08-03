using UnityEngine;

namespace NAN2026
{
    [CreateAssetMenu(fileName = "GateConfig", menuName = "NAN2026/GateConfig")]
    public class GateConfig : ScriptableObject
    {
        [Header("페이즈(초)")]
        public float delaySeconds = 0.4f;
        public float collapseSeconds = 0.8f;
        public float holdSeconds = 0.6f;

        [Header("개방부 조명")]
        public float lightIntensity = 1.2f;
        public float lightRadius = 2.2f;
        public Color lightColor = new Color(1f, 0.72f, 0.35f, 1f);

        [Header("파편·먼지")]
        public int debrisCount = 4;
        public float debrisImpulse = 2.5f;
        public float debrisLifetime = 2.5f;
        public float dustLifetime = 3f;
    }
}
