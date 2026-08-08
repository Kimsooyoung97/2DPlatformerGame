using UnityEngine;

namespace NAN2026
{
    // 전투 이펙트용 무조명 재질 — 암흑 씬(전역광 저조도)에서도 스스로 빛난다
    public static class FxUnlit
    {
        private static Material mat;
        public static Material Mat
        {
            get
            {
                if (mat != null) return mat;
                var sh = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                if (sh == null) sh = Shader.Find("Sprites/Default");
                mat = new Material(sh);
                return mat;
            }
        }
    }
}
