using NUnit.Framework;
using NAN2026.Core;

namespace NAN2026.Tests
{
    public class EnemyStateLogicTests
    {
        const float Aggro = 10f;
        const float Reach = 2f;

        [Test] public void 멀면_대기한다()
        {
            Assert.AreEqual(EnemyStateLogic.Idle, EnemyStateLogic.Decide(12f, Aggro, Reach, true));
        }

        [Test] public void 인지범위_안이면_접근한다()
        {
            Assert.AreEqual(EnemyStateLogic.Walk, EnemyStateLogic.Decide(6f, Aggro, Reach, true));
        }

        [Test] public void 사거리_안이고_준비되면_공격한다()
        {
            Assert.AreEqual(EnemyStateLogic.Attack, EnemyStateLogic.Decide(1.5f, Aggro, Reach, true));
        }

        [Test] public void 사거리_안이어도_쿨다운이면_접근유지()
        {
            Assert.AreEqual(EnemyStateLogic.Walk, EnemyStateLogic.Decide(1.5f, Aggro, Reach, false));
        }

        [Test] public void 경계값은_포함한다()
        {
            Assert.AreEqual(EnemyStateLogic.Attack, EnemyStateLogic.Decide(2f, Aggro, Reach, true));
            Assert.AreEqual(EnemyStateLogic.Walk, EnemyStateLogic.Decide(10f, Aggro, Reach, false));
        }

        [Test] public void 쿨다운_중에는_다가가지_않고_대기한다()
        {
            // 기존 Decide 는 Walk 를 반환해 관통했다
            Assert.AreEqual(EnemyStateLogic.Walk, EnemyStateLogic.Decide(1.5f, Aggro, Reach, false));
            Assert.AreEqual(EnemyStateLogic.Idle, EnemyStateLogic.DecideWithHold(1.5f, Aggro, Reach, false));
        }

        [Test] public void 사거리_안_준비되면_공격()
        {
            Assert.AreEqual(EnemyStateLogic.Attack, EnemyStateLogic.DecideWithHold(1.5f, Aggro, Reach, true));
        }

        [Test] public void 사거리_밖_인지_안이면_접근()
        {
            Assert.AreEqual(EnemyStateLogic.Walk, EnemyStateLogic.DecideWithHold(6f, Aggro, Reach, true));
        }

        [Test] public void 인지_밖이면_대기()
        {
            Assert.AreEqual(EnemyStateLogic.Idle, EnemyStateLogic.DecideWithHold(20f, Aggro, Reach, true));
        }

        [Test] public void 정지거리_안쪽으로는_파고들지_않는다()
        {
            Assert.AreEqual(0f, EnemyStateLogic.MoveStep(1.4f, 1.4f, 2f, 0.1f), 0.0001f);
            Assert.AreEqual(0f, EnemyStateLogic.MoveStep(1.0f, 1.4f, 2f, 0.1f), 0.0001f);
        }

        [Test] public void 남은_여유보다_크게_못_움직인다()
        {
            Assert.AreEqual(0.05f, EnemyStateLogic.MoveStep(1.45f, 1.4f, 2f, 0.1f), 0.0001f);
            Assert.AreEqual(0.2f, EnemyStateLogic.MoveStep(5f, 1.4f, 2f, 0.1f), 0.0001f);
        }

        [Test] public void 앞에_동료가_있으면_막힌다()
        {
            Assert.IsTrue(EnemyStateLogic.BlockedByNeighbor(10f, 10.5f, 1f, 1.0f));
            Assert.IsFalse(EnemyStateLogic.BlockedByNeighbor(10f, 11.5f, 1f, 1.0f));
        }

        [Test] public void 뒤에_있는_동료는_막지_않는다()
        {
            Assert.IsFalse(EnemyStateLogic.BlockedByNeighbor(10f, 9.5f, 1f, 1.0f));
            Assert.IsTrue(EnemyStateLogic.BlockedByNeighbor(10f, 9.5f, -1f, 1.0f));
        }

        [Test] public void 쿨다운_편차는_기준을_중심으로_흔들린다()
        {
            Assert.AreEqual(1.7f, EnemyStateLogic.JitteredCooldown(2f, 0.6f, 0f), 0.0001f);
            Assert.AreEqual(2f, EnemyStateLogic.JitteredCooldown(2f, 0.6f, 0.5f), 0.0001f);
            Assert.AreEqual(2.3f, EnemyStateLogic.JitteredCooldown(2f, 0.6f, 1f), 0.0001f);
            Assert.AreEqual(2f, EnemyStateLogic.JitteredCooldown(2f, 0f, 0f), 0.0001f);
        }

        [Test] public void 최초_지연은_0에서_stagger_사이()
        {
            Assert.AreEqual(0f, EnemyStateLogic.InitialDelay(0.8f, 0f), 0.0001f);
            Assert.AreEqual(0.8f, EnemyStateLogic.InitialDelay(0.8f, 1f), 0.0001f);
            Assert.AreEqual(0f, EnemyStateLogic.InitialDelay(0f, 1f), 0.0001f);
        }

        [Test] public void 예열_종료판정()
        {
            Assert.IsFalse(EnemyStateLogic.WindupFinished(0.3f, 0.55f));
            Assert.IsTrue(EnemyStateLogic.WindupFinished(0.55f, 0.55f));
            Assert.IsTrue(EnemyStateLogic.WindupFinished(0f, 0f));
        }

        [Test] public void 점멸_삼각파는_0에서_1_사이를_왕복한다()
        {
            Assert.AreEqual(0f, EnemyStateLogic.FlashPulse01(0f, 12f), 0.0001f);
            Assert.AreEqual(1f, EnemyStateLogic.FlashPulse01(1f / 12f, 12f), 0.0001f);
            Assert.AreEqual(0f, EnemyStateLogic.FlashPulse01(2f / 12f, 12f), 0.0001f);
            for (float e = 0f; e < 1f; e += 0.017f)
            {
                float v = EnemyStateLogic.FlashPulse01(e, 12f);
                Assert.GreaterOrEqual(v, 0f);
                Assert.LessOrEqual(v, 1f);
            }
        }

        [Test] public void 점멸속도0이면_고정()
        {
            Assert.AreEqual(0f, EnemyStateLogic.FlashPulse01(5f, 0f), 0.0001f);
        }

        [Test] public void 다섯대_맞으면_죽는다()
        {
            Assert.IsFalse(EnemyStateLogic.IsDead(4, 5));
            Assert.IsTrue(EnemyStateLogic.IsDead(5, 5));
            Assert.IsTrue(EnemyStateLogic.IsDead(7, 5));
        }

        [Test] public void 루프_애니는_순환한다()
        {
            Assert.AreEqual(0, EnemyStateLogic.AnimIndex(0f, 10f, 5, true));
            Assert.AreEqual(4, EnemyStateLogic.AnimIndex(0.45f, 10f, 5, true));
            Assert.AreEqual(0, EnemyStateLogic.AnimIndex(0.5f, 10f, 5, true));
        }

        [Test] public void 비루프_애니는_마지막에서_멈춘다()
        {
            Assert.AreEqual(4, EnemyStateLogic.AnimIndex(0.5f, 10f, 5, false));
            Assert.AreEqual(4, EnemyStateLogic.AnimIndex(9f, 10f, 5, false));
        }

        [Test] public void 프레임0이면_안전하게_0()
        {
            Assert.AreEqual(0, EnemyStateLogic.AnimIndex(1f, 10f, 0, true));
            Assert.AreEqual(0, EnemyStateLogic.AnimIndex(1f, 0f, 5, true));
        }

        [Test] public void 비루프_종료판정()
        {
            Assert.IsFalse(EnemyStateLogic.AnimFinished(0.4f, 10f, 5));
            Assert.IsTrue(EnemyStateLogic.AnimFinished(0.5f, 10f, 5));
        }

        [Test] public void 발사는_한_번만()
        {
            Assert.IsFalse(EnemyStateLogic.ShouldFire(0.4f, 0.5f, false));
            Assert.IsTrue(EnemyStateLogic.ShouldFire(0.5f, 0.5f, false));
            Assert.IsFalse(EnemyStateLogic.ShouldFire(0.9f, 0.5f, true));
        }

        [Test] public void 바라보는_방향()
        {
            Assert.AreEqual(-1f, EnemyStateLogic.FaceSign(10f, 3f));
            Assert.AreEqual(1f, EnemyStateLogic.FaceSign(10f, 17f));
            Assert.AreEqual(1f, EnemyStateLogic.FaceSign(10f, 10f));
        }
    
        [Test]
        public void 공격fps가_0이면_공용fps를_쓴다()
        {
            Assert.AreEqual(12f, EnemyStateLogic.AttackFps(0f, 12f));
            Assert.AreEqual(12f, EnemyStateLogic.AttackFps(-3f, 12f));
        }

        [Test]
        public void 공격fps가_양수면_그값을_쓴다()
        {
            Assert.AreEqual(8f, EnemyStateLogic.AttackFps(8f, 12f));
        }

        [Test]
        public void 프레임수와_fps로_모션_지속시간을_구한다()
        {
            // 기사 ATTACK3 6프레임을 8fps 로 재생하면 0.75초. attackDur 는 이 값과 같아야 잘리지 않는다
            Assert.AreEqual(0.75f, EnemyStateLogic.DurationForFrames(6, 8f), 0.0001f);
            Assert.AreEqual(0.5f, EnemyStateLogic.DurationForFrames(6, 12f), 0.0001f);
            Assert.AreEqual(0f, EnemyStateLogic.DurationForFrames(0, 12f));
            Assert.AreEqual(0f, EnemyStateLogic.DurationForFrames(6, 0f));
        }
    
        // 기사 타격창 0.40~0.80 기준. 0=대기, 1=패링 접수, 2=데미지 확정
        [Test]
        public void 창_전에는_아무것도_하지_않는다()
        {
            Assert.AreEqual(0, EnemyStateLogic.SwingResolve(0.00f, 0.4f, 0.8f, false));
            Assert.AreEqual(0, EnemyStateLogic.SwingResolve(0.39f, 0.4f, 0.8f, false));
        }

        [Test]
        public void 창_안에서는_매_프레임_패링을_접수한다()
        {
            Assert.AreEqual(1, EnemyStateLogic.SwingResolve(0.40f, 0.4f, 0.8f, false));
            Assert.AreEqual(1, EnemyStateLogic.SwingResolve(0.60f, 0.4f, 0.8f, false));
            Assert.AreEqual(1, EnemyStateLogic.SwingResolve(0.79f, 0.4f, 0.8f, false));
        }

        [Test]
        public void 데미지는_창의_끝에서_확정된다()
        {
            Assert.AreEqual(2, EnemyStateLogic.SwingResolve(0.80f, 0.4f, 0.8f, false));
            Assert.AreEqual(2, EnemyStateLogic.SwingResolve(0.95f, 0.4f, 0.8f, false));
        }

        [Test]
        public void 이미_확정된_휘두름은_다시_판정하지_않는다()
        {
            Assert.AreEqual(0, EnemyStateLogic.SwingResolve(0.60f, 0.4f, 0.8f, true));
            Assert.AreEqual(0, EnemyStateLogic.SwingResolve(0.99f, 0.4f, 0.8f, true));
        }
    
        [Test]
        public void 그로기_상수는_다른_상태와_겹치지_않는다()
        {
            var all = new[] { EnemyStateLogic.Idle, EnemyStateLogic.Walk, EnemyStateLogic.Attack,
                              EnemyStateLogic.Hurt, EnemyStateLogic.Death, EnemyStateLogic.Windup, EnemyStateLogic.Groggy };
            CollectionAssert.AllItemsAreUnique(all);
            Assert.AreEqual(6, EnemyStateLogic.Groggy);
        }

        [Test]
        public void 그로기_종료판정()
        {
            Assert.IsFalse(EnemyStateLogic.GroggyFinished(1.59f, 1.6f));
            Assert.IsTrue(EnemyStateLogic.GroggyFinished(1.6f, 1.6f));
        }

        [Test]
        public void 반사화살은_최소수명을_보장받는다()
        {
            // 수명이 0.2초 남은 화살을 패링하면 되돌아가는 도중 사라진다 → 최소 1.5초 보장
            Assert.AreEqual(1.5f, EnemyStateLogic.ReflectLife(0.2f, 1.5f), 0.0001f);
            Assert.AreEqual(3.0f, EnemyStateLogic.ReflectLife(3.0f, 1.5f), 0.0001f);
        }

        [Test]
        public void 반사속도는_배율이_0이하면_원속도를_쓴다()
        {
            Assert.AreEqual(14f, EnemyStateLogic.ReflectSpeed(10f, 1.4f), 0.0001f);
            Assert.AreEqual(10f, EnemyStateLogic.ReflectSpeed(10f, 0f), 0.0001f);
            Assert.AreEqual(10f, EnemyStateLogic.ReflectSpeed(10f, -2f), 0.0001f);
        }
    
        [Test]
        public void 방향은_고정시점_전까지_추적한다()
        {
            // 아처: fireFrac 0.75 까지는 계속 겨누고, 활을 놓는 순간 고정
            Assert.IsFalse(EnemyStateLogic.FaceLocked(0.00f, 0.75f));
            Assert.IsFalse(EnemyStateLogic.FaceLocked(0.74f, 0.75f));
            Assert.IsTrue(EnemyStateLogic.FaceLocked(0.75f, 0.75f));
            Assert.IsTrue(EnemyStateLogic.FaceLocked(1.00f, 0.75f));
        }

        [Test]
        public void 기사는_타격창이_열릴_때_방향이_굳는다()
        {
            Assert.IsFalse(EnemyStateLogic.FaceLocked(0.39f, 0.40f));
            Assert.IsTrue(EnemyStateLogic.FaceLocked(0.40f, 0.40f));
        }

        [Test]
        public void 화살은_플레이어_몸높이_안에서만_패링된다()
        {
            // 발끝 0, 몸높이 2.0 기준. 총구 높이 0.8 은 통과
            Assert.IsTrue(EnemyStateLogic.WithinBodyHeight(0.8f, 0f, 2.0f));
            Assert.IsTrue(EnemyStateLogic.WithinBodyHeight(0f, 0f, 2.0f));
            Assert.IsTrue(EnemyStateLogic.WithinBodyHeight(2.0f, 0f, 2.0f));
            // 1.5u 점프로 넘긴 화살(상대 -0.7)은 제외
            Assert.IsFalse(EnemyStateLogic.WithinBodyHeight(0.8f, 1.5f, 2.0f));
            Assert.IsFalse(EnemyStateLogic.WithinBodyHeight(2.1f, 0f, 2.0f));
        }
    
        [Test]
        public void 순찰_범위_안에서는_걸음이_그대로다()
        {
            Assert.AreEqual(0.5f, EnemyStateLogic.PatrolStep(45f, 0.5f, 1f, 40f, 48.5f), 0.0001f);
            Assert.AreEqual(0.5f, EnemyStateLogic.PatrolStep(45f, 0.5f, -1f, 40f, 48.5f), 0.0001f);
        }

        [Test]
        public void 순찰_경계에서는_걸음이_잘린다()
        {
            // x=48.2 에서 오른쪽 0.5 를 가면 48.7 이지만 경계가 48.5 → 0.3 만 허용
            Assert.AreEqual(0.3f, EnemyStateLogic.PatrolStep(48.2f, 0.5f, 1f, 40f, 48.5f), 0.0001f);
            Assert.AreEqual(0f, EnemyStateLogic.PatrolStep(48.5f, 0.5f, 1f, 40f, 48.5f), 0.0001f);
        }

        [Test]
        public void 경계밖이어도_안쪽으로는_돌아올_수_있다()
        {
            // 경계 밖에 있어도 되돌아오는 방향은 막지 않는다
            Assert.AreEqual(0.5f, EnemyStateLogic.PatrolStep(49f, 0.5f, -1f, 40f, 48.5f), 0.0001f);
            Assert.AreEqual(0f, EnemyStateLogic.PatrolStep(49f, 0.5f, 1f, 40f, 48.5f), 0.0001f);
        }

        [Test]
        public void 같은_단_판정은_허용오차를_따른다()
        {
            // 낮은 단 y0.04 에서 높은 단 y2.0 은 1.96 차이 → 다른 단
            Assert.IsFalse(EnemyStateLogic.SameLevel(2.0f, 0.04f, 0.6f));
            Assert.IsTrue(EnemyStateLogic.SameLevel(0.3f, 0.04f, 0.6f));
            Assert.IsTrue(EnemyStateLogic.SameLevel(-0.5f, 0.04f, 0.6f));
        }
    }
}
