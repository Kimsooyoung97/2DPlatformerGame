namespace NAN2026.Core
{
    // 기사석상 상태 전이 순수 로직. 상태: 0잠듦 1각성중 2대기 3추적 4공격 5쿨다운 6사망
    public static class StatueLogic
    {
        public const int Dormant = 0, Awakening = 1, Idle = 2, Chase = 3, Attack = 4, Cooldown = 5, Dead = 6;

        public static int Next(int state, float dist, float awakenRange, float attackRange, bool timerDone)
        {
            switch (state)
            {
                case Dormant: return dist <= awakenRange ? Awakening : Dormant;
                case Awakening: return timerDone ? Idle : Awakening;
                case Idle: return timerDone ? (dist <= attackRange ? Attack : Chase) : Idle;
                case Chase: return dist <= attackRange ? Attack : Chase;
                case Attack: return timerDone ? Cooldown : Attack;
                case Cooldown: return timerDone ? Chase : Cooldown;
                default: return Dead;
            }
        }

        public static bool FaceLeft(float dxToPlayer) { return dxToPlayer < 0f; }

        public static bool HitboxOpen(float elapsed, float start, float end)
        {
            return elapsed >= start && elapsed <= end;
        }
    }
}
