using UnityEngine;
using System.Reflection;

namespace NAN2026
{
    // 이 씬에서만 패링 범위를 바꾼다: 컨트롤러의 MovementConfig를 런타임 사본으로 갈아끼움 (원본 에셋 무손상)
    public class SceneParryOverride : MonoBehaviour
    {
        public ParryRangeOverrideConfig config;
        private float baseOffsetX = 0.6f;

        private void Start()
        {
            if (config == null) return;
            var pc = GetComponent("PlayerController2D");
            if (pc == null) return;
            FieldInfo cfgField = null;
            foreach (var f in pc.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                if (f.FieldType.Name == "MovementConfig") { cfgField = f; break; }
            if (cfgField == null) return;
            var mc = cfgField.GetValue(pc) as ScriptableObject;
            if (mc == null) return;
            var clone = Instantiate(mc);
            var reach = clone.GetType().GetField("parryReachX");
            if (reach != null) reach.SetValue(clone, config.reachX);
            var off = clone.GetType().GetField("parryBoxOffsetX");
            if (off != null) baseOffsetX = (float)off.GetValue(clone);
            cfgField.SetValue(pc, clone);
        }

        private void OnDrawGizmos()
        {
            float reach = config != null ? config.reachX : 1.5f;
            Gizmos.color = new Color(1f, 0.85f, 0.1f, 0.9f);
            Vector3 c = transform.position + new Vector3(0f, 0.6f, 0f);
            Gizmos.DrawWireCube(c, new Vector3(reach * 2f + baseOffsetX * 2f, 1.6f, 0f));
            Gizmos.color = new Color(1f, 0.85f, 0.1f, 0.18f);
            Gizmos.DrawCube(c, new Vector3(reach * 2f + baseOffsetX * 2f, 1.6f, 0f));
        }
    }
}
