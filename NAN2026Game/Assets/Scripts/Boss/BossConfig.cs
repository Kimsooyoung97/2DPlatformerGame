using UnityEngine;

namespace NAN2026
{
    [CreateAssetMenu(fileName = "BossConfig", menuName = "Game/BossConfig")]
    public class BossConfig : ScriptableObject
    {
        [Header("등장 시퀀스")]
        public int idle1Loops = 2;
    }
}