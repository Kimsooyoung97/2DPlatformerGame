using UnityEditor;
using UnityEngine;

namespace NAN2026
{
    /// <summary>
    /// MidBoss를 선택했을 때 Scene 뷰에 사거리 원을 그리고, 그 가장자리를 마우스로
    /// 드래그해서 MidBossPatternConfig 값을 바로 조절할 수 있게 한다.
    /// 색상: 노랑=aggroRange, 빨강=attackRange, 마젠타=normalAttackReach, 시안=wheelAttackReach.
    /// FireAttack/FireBomb은 원거리 구체라 근접 reach 개념이 없어 핸들 대상에서 제외.
    /// </summary>
    [CustomEditor(typeof(MidBossController))]
    public class MidBossControllerEditor : Editor
    {
        private static readonly Color AggroColor = new Color(1f, 0.92f, 0.2f, 0.9f);
        private static readonly Color AttackColor = new Color(1f, 0.25f, 0.25f, 0.9f);
        private static readonly Color NormalReachColor = new Color(1f, 0.3f, 1f, 0.9f);
        private static readonly Color WheelReachColor = new Color(0.3f, 0.95f, 1f, 0.9f);
        private static readonly Color FireAttackReachColor = new Color(1f, 0.55f, 0.1f, 0.9f);
        private static readonly Color FireBombReachColor = new Color(0.55f, 0.25f, 0.85f, 0.9f);

        private void OnSceneGUI()
        {
            MidBossController controller = (MidBossController)target;
            SerializedObject so = new SerializedObject(controller);
            SerializedProperty configProp = so.FindProperty("config");
            MidBossPatternConfig config = configProp.objectReferenceValue as MidBossPatternConfig;
            if (config == null) return;

            Vector3 pos = controller.transform.position;

            DrawRangeHandle(config, pos, AggroColor, "감지(aggroRange)", config.aggroRange,
                v => config.aggroRange = v);
            DrawRangeHandle(config, pos, AttackColor, "공격개시(attackRange)", config.attackRange,
                v => config.attackRange = v);
            DrawRangeHandle(config, pos, NormalReachColor, "NormalAttack reach", config.normalAttackReach,
                v => config.normalAttackReach = v);
            DrawRangeHandle(config, pos, WheelReachColor, "WheelAttack reach", config.wheelAttackReach,
                v => config.wheelAttackReach = v);
            DrawRangeHandle(config, pos, FireAttackReachColor, "FireAttack reach", config.fireAttackReach,
                v => config.fireAttackReach = v);
            DrawRangeHandle(config, pos, FireBombReachColor, "FireBomb reach", config.fireBombReach,
                v => config.fireBombReach = v);
        }

        private void DrawRangeHandle(MidBossPatternConfig config, Vector3 center, Color color,
            string label, float currentValue, System.Action<float> apply)
        {
            Handles.color = color;
            EditorGUI.BeginChangeCheck();
            float newValue = Handles.RadiusHandle(Quaternion.identity, center, currentValue);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(config, "MidBoss 사거리 조절: " + label);
                apply(Mathf.Max(0f, newValue));
                EditorUtility.SetDirty(config);
            }

            Handles.Label(center + new Vector3(0f, currentValue + 0.15f, 0f), label, EditorStyles.whiteBoldLabel);
        }
    }
}
