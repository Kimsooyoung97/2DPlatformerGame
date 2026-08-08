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
    }
}
