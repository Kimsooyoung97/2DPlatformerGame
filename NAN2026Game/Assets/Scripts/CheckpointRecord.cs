using UnityEngine;

namespace NAN2026
{
    /// <summary>
    /// 저장된 세이브포인트 하나. 씬 이름 + 좌표 쌍으로 저장해서, 다른 씬에 있던
    /// 세이브포인트로도 정확히 되돌아갈 수 있게 한다(같은 씬 안 좌표만으로는 불가능).
    /// </summary>
    [System.Serializable]
    public sealed class CheckpointRecord
    {
        public string sceneName;
        public Vector3 position;
        public string label; // 이동 메뉴에 표시할 이름

        public CheckpointRecord(string sceneName, Vector3 position, string label)
        {
            this.sceneName = sceneName;
            this.position = position;
            this.label = label;
        }
    }
}
