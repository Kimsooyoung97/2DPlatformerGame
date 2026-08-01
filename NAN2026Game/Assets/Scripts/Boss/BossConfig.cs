using UnityEngine;

namespace NAN2026
{
    [CreateAssetMenu(fileName = "BossConfig", menuName = "Game/BossConfig")]
    public class BossConfig : ScriptableObject
    {
        [Header("등장 시퀀스")]
        public int idle1Loops = 2;

        [Header("구체 공격")]
        public float orbInterval = 1.6f;
        public float orbSpeed = 6f;
        public float orbLifetime = 4f;
        public float orbSpawnHeight = 2.6f;

        [Header("리듬 빔")]
        public int orbsPerCycle = 3;
        public float beamNoteSpeed = 7f;
        public float beamThickness = 0.9f;
        public float beamHeightOffset = 0.9f;
        public float beamOverreach = 2f;
        public float beamLeadIn = 0.6f;
        public float beamTailTime = 1.2f;
        public float noteScale = 0.75f;
        public float missBehindDistance = 1.2f;
        public Color beamColor = new Color(1f, 0.5f, 0.8f, 0.35f);
        public float[] notePattern = new float[] { 0f, 0.5f, 1f, 1.25f, 1.5f, 2.5f, 3f, 3.25f, 3.5f, 3.75f };
    }
}