using UnityEngine;

namespace NAN2026
{
    [CreateAssetMenu(fileName = "MovementConfig", menuName = "Game/MovementConfig")]
    public class MovementConfig : ScriptableObject
    {
        [Header("이동")]
        public float walkSpeed = 2.2f;
        public float runSpeed = 4.2f;

        [Header("점프")]
        public float jumpVelocity = 8f;
        public float gravityScale = 2.5f;
        public float groundCheckDistance = 0.08f;
        public float groundNormalMinY = 0.5f;
        public float wallCheckDistance = 0.05f;
        public float wallNormalMinX = 0.5f;
        public float dashSpeed = 20f;
        public float dashMaxDistance = 8f;
        public int maxAirDashes = 1;
        public int maxJumps = 2;
        public float onewayRiseThreshold = 0.05f;
        public float apexSpeedThreshold = 1.2f;
        public float landDuration = 0.36f;

        [Header("공격")]
        public float slashDuration = 0.4f;
        public float combo2Duration = 0.4f;
        public float combo3Duration = 0.55f;

        [Header("패링")]
        public float parryWindow = 0.18f;
        public float parryEndDuration = 0.22f;
        public Vector2 parryBoxSize = new Vector2(1.0f, 1.4f);
        public float parryBoxOffsetX = 0.6f;
        public float parryPerfectDistance = 0.25f;
        public float parryCooldown = 1.5f;
        public float parryCooldownMinimum = 0.3f;

        [Header("공격 전진(런지) 속도")]
        public float slashLungeSpeed = 1.5f;
        public float combo2LungeSpeed = 3.5f;
        public float combo3LungeSpeed = 0f;
    
    [Header("백스텝")]
    public float backstepDuration = 0.35f;
    public float backstepSpeed = 12f;
    public float backstepCooldown = 0.15f;
    public float backstepHopSpeed = 1.6f; // 소도약 상향 속도
    [Range(0f,1f)] public float backstepMoveStartFrac = 0.30f; // 3프레임부터 이동
    [Range(0f,1f)] public float backstepMoveEndFrac = 0.85f;   // 4프레임까지 이동, 이후 정지
    [Range(0f,1f)] public float backstepIFrameStartFrac = 0.333f; // 3프레임 시작
    [Range(0f,1f)] public float backstepIFrameEndFrac = 0.833f;   // 5프레임 끝

    [Header("V 2단 콤보")]
    public float comboVWindow = 0.6f;
    [Range(0f,1f)] public float comboVCancelFrac = 0.6f;
    public float comboB1Duration = 0.6f;
    public float comboB1FxFps = 24f; // 2키 동작 길이(느긋한 묵직함)
    public float comboVFxFps = 18f;
    public float comboVFxScale = 1.7f;
    public float comboVFxOffsetX = 1.2f; // 캐릭터 궤적 너머 전방 이격
    public float comboVFxOffsetY = 0.35f;
    [Range(0f,1f)] public float comboVFxAlpha = 0.85f;
    [Header("패링 이펙트")]
    public float parryFxFps = 22f;
    public float parryFxScale = 4f;
    public float parryFxOffsetX = 0.8f;
    public float parryFxOffsetY = 0.5f;
    public float parryReachX = 1.5f; // 전방 이 거리 안 위협은 접촉 전 조기 패링
    [Range(0f,1f)] public float parryFxAlpha = 0.85f; // 1타 이 비율 경과 후 2타 캔슬 허용(3/5프레임) // 2단 입력 유효창
}
}