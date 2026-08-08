using UnityEngine;

namespace NAN2026
{
    /// 플레이어 탐색 단일 창구.
    /// 팀이 프리팹을 교체하면서 씬마다 오브젝트 이름이 달라졌다(Player / RealPlayer).
    /// 이름은 언제든 또 바뀔 수 있으므로 태그('Player')를 1순위로 쓰고, 이름은 폴백으로만 둔다.
    public static class PlayerLocator
    {
        public static GameObject Find()
        {
            GameObject go = null;
            try { go = GameObject.FindGameObjectWithTag("Player"); } catch { }
            if (go != null) return go;
            go = GameObject.Find("Player");
            if (go != null) return go;
            return GameObject.Find("RealPlayer");
        }

        public static Transform FindTransform()
        {
            var go = Find();
            return go != null ? go.transform : null;
        }
    }
}
