using NUnit.Framework;
using NAN2026.Core;

namespace NAN2026.Tests
{
    public class BossFacingLogicTests
    {
        // 데몬 시트는 비반전 상태에서 왼쪽을 향한다(cleave 타격창이 좌측으로 쓸림).
        const bool DemonFacesLeft = true;

        [Test] public void 플레이어가_왼쪽이면_반전하지_않는다()
        {
            Assert.IsFalse(BossFacingLogic.ShouldFlipX(10f, 3f, DemonFacesLeft));
        }

        [Test] public void 플레이어가_오른쪽이면_반전한다()
        {
            Assert.IsTrue(BossFacingLogic.ShouldFlipX(10f, 17f, DemonFacesLeft));
        }

        [Test] public void 오른쪽바라보기_시트는_반대로_반전한다()
        {
            Assert.IsTrue(BossFacingLogic.ShouldFlipX(10f, 3f, false));
            Assert.IsFalse(BossFacingLogic.ShouldFlipX(10f, 17f, false));
        }

        [Test] public void 좌향시트_비반전이면_왼쪽을_향한다()
        {
            Assert.AreEqual(-1f, BossFacingLogic.FacingSign(false, DemonFacesLeft));
            Assert.AreEqual(1f, BossFacingLogic.FacingSign(true, DemonFacesLeft));
        }

        [Test] public void 바라보기_결과는_항상_플레이어쪽을_가리킨다()
        {
            float[] px = { -50f, -1f, 0f, 9.9f, 10.1f, 40f };
            foreach (var p in px)
            {
                bool flip = BossFacingLogic.ShouldFlipX(10f, p, DemonFacesLeft);
                float sign = BossFacingLogic.FacingSign(flip, DemonFacesLeft);
                if (p < 10f) Assert.AreEqual(-1f, sign, "player=" + p);
                else Assert.AreEqual(1f, sign, "player=" + p);
            }
        }

        [Test] public void 등뒤의_대상은_정면이_아니다()
        {
            Assert.IsFalse(BossFacingLogic.TargetInFront(10f, 3f, 1f, 0.5f));
            Assert.IsFalse(BossFacingLogic.TargetInFront(10f, 17f, -1f, 0.5f));
        }

        [Test] public void 정면의_대상은_통과한다()
        {
            Assert.IsTrue(BossFacingLogic.TargetInFront(10f, 17f, 1f, 0.5f));
            Assert.IsTrue(BossFacingLogic.TargetInFront(10f, 3f, -1f, 0.5f));
        }

        [Test] public void 겹친_거리는_방향과_무관하게_정면처리()
        {
            Assert.IsTrue(BossFacingLogic.TargetInFront(10f, 10.3f, -1f, 0.5f));
            Assert.IsTrue(BossFacingLogic.TargetInFront(10f, 9.7f, 1f, 0.5f));
        }

        [Test] public void 접지_피벗Y는_지면에_발끝을_올린다()
        {
            // 데몬: 지면 -3.95, 피벗→발 7.9804
            Assert.AreEqual(4.0304f, BossFacingLogic.GroundedPivotY(-3.95f, 7.9804f), 0.0001f);
        }

        [Test] public void 손위치는_바라보는_쪽으로_나온다()
        {
            Assert.AreEqual(3.15f, BossFacingLogic.HandWorldX(10f, 6.85f, -1f), 0.0001f);
            Assert.AreEqual(16.85f, BossFacingLogic.HandWorldX(10f, 6.85f, 1f), 0.0001f);
        }

        [Test] public void 손Y는_피벗_상대값을_더한다()
        {
            Assert.AreEqual(1.99f, BossFacingLogic.HandWorldY(4.03f, -2.04f), 0.0001f);
        }
    }
}
