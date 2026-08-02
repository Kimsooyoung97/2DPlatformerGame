namespace NAN2026.Core
{
    /// 순수 로직: UnityEngine 비의존. EditMode 테스트 대상.
    public static class LevelProgressionLogic
    {
        /// level(예: 1→2로 가기 위해 필요한 경험치)을 요구한다. level이 클수록 더 많이 필요.
        public static int RequiredXpForLevel(int level, int baseXp, int xpIncrementPerLevel)
        {
            int required = baseXp + (level - 1) * xpIncrementPerLevel;
            return required < 1 ? 1 : required;
        }

        /// 누적 경험치를 다 써서 갈 수 있는 최대 레벨과 남은 경험치를 계산한다.
        /// 한 번에 여러 레벨을 오르는 경우(큰 경험치 획득)도 처리한다. 최대 999레벨까지만 계산해
        /// 잘못된 설정값(예: xpIncrementPerLevel이 음수)으로 인한 무한 루프를 방지한다.
        public static void TryLevelUp(int currentXp, int currentLevel, int baseXp, int xpIncrementPerLevel,
            out int newLevel, out int remainingXp)
        {
            newLevel = currentLevel;
            remainingXp = currentXp;

            for (int guard = 0; guard < 999; guard++)
            {
                int required = RequiredXpForLevel(newLevel, baseXp, xpIncrementPerLevel);
                if (remainingXp < required) break;
                remainingXp -= required;
                newLevel++;
            }
        }

        /// 레벨이 오를수록 골드 등급이 나올 확률이 조금씩 올라가되 상한선을 넘지 않는다.
        public static float GoldChanceForLevel(int level, float baseChance, float perLevel, float maxChance)
        {
            float chance = baseChance + (level - 1) * perLevel;
            if (chance > maxChance) return maxChance;
            if (chance < 0f) return 0f;
            return chance;
        }

        public static float SilverChanceForLevel(int level, float baseChance, float perLevel, float maxChance)
        {
            float chance = baseChance + (level - 1) * perLevel;
            if (chance > maxChance) return maxChance;
            if (chance < 0f) return 0f;
            return chance;
        }

        /// 0=Bronze, 1=Silver, 2=Gold. roll01은 [0,1) 난수.
        public static int TierForRoll(float roll01, float goldChance, float silverChance)
        {
            if (roll01 < goldChance) return 2;
            if (roll01 < goldChance + silverChance) return 1;
            return 0;
        }
    }
}
