using UnityEngine;

namespace NAN2026
{
    // 스킬 해금·쿨타임 단일 창구.
    // 슬롯 0=1번(번개) / 1=2번(가로베기) / 2=3번(나선환) — 좌하단 아이콘 순서와 일치.
    public static class SkillGate
    {
        private const int Slots = 3;
        private static float[] endAt = new float[Slots];
        private static float[] dur = new float[Slots];

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() // DisableDomainReload 대응
        {
            endAt = new float[Slots];
            dur = new float[Slots];
        }

        /// 상자에서 아이콘을 몇 개 먹었는지로 해금 판정
        public static bool IsUnlocked(int slot)
        {
            if (slot < 0 || slot >= Slots) return false;
            var f = ChestRewardEvents.Unlocked;
            return f != null && slot < f.Length && f[slot]; // 슬롯별 해금 플래그
        }

        public static bool IsReady(int slot)
        {
            if (slot < 0 || slot >= Slots) return false;
            return Time.time >= endAt[slot];
        }

        /// 발동 보고 — 쿨타임 시작
        public static void Report(int slot, float cooldown)
        {
            if (slot < 0 || slot >= Slots) return;
            dur[slot] = Mathf.Max(0.01f, cooldown);
            endAt[slot] = Time.time + dur[slot];
        }

        /// 0(막 사용)~1(사용 가능) — 아이콘 색 채우기에 쓴다
        public static float Progress(int slot)
        {
            if (slot < 0 || slot >= Slots) return 1f;
            if (dur[slot] <= 0f) return 1f;
            float remain = endAt[slot] - Time.time;
            if (remain <= 0f) return 1f;
            return Mathf.Clamp01(1f - remain / dur[slot]);
        }
    }
}
