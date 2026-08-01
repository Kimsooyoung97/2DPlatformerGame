using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace NHNDemo
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
    public sealed class PixelPlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float sprintSpeed = 8f;
        [SerializeField] private float jumpForce = 10f;
        [SerializeField] private int maxJumpCount = 2;
        [SerializeField] private float dashSpeed = 13f;
        [SerializeField] private float dashDuration = 0.18f;

        [Header("Gravity Shift")]
        [SerializeField] private float gravityAcceleration = 32f;
        [SerializeField] private float gravityShiftCooldown = 0.35f;

        [Header("Grounding")]
        [SerializeField] private float groundProbeDistance = 0.12f;

        [Header("Combat")]
        [SerializeField] private int attackDamage = 1;
        [SerializeField] private float meleeReach = 0.9f;
        [SerializeField] private float meleeRadius = 0.68f;

        private Rigidbody2D body;
        private Animator animator;
        private SpriteRenderer spriteRenderer;
        private CapsuleCollider2D capsule;
        private string currentState;
        private float dashTimer;
        private float actionLockTimer;
        private float lastGroundedTime = float.NegativeInfinity;
        private float lastComboTime = float.NegativeInfinity;
        private float lastGravityShiftTime = float.NegativeInfinity;
        private float lastJumpTime = float.NegativeInfinity;
        private int comboStep = -1;
        private int jumpCount;
        private int facing = 1;
        private bool bowDrawing;
        private Vector2 currentUp = Vector2.up;

        public string CurrentAnimation => currentState;
        public Vector2 CurrentUp => currentUp;
        private Vector2 CurrentRight => new Vector2(currentUp.y, -currentUp.x);

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            animator = GetComponentInChildren<Animator>();
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            capsule = GetComponent<CapsuleCollider2D>();
            body.gravityScale = 0f;

            if (animator == null || spriteRenderer == null)
            {
                Debug.LogError(
                    "PixelPlayerController requires an Animator and SpriteRenderer on a visual child.",
                    this);
                enabled = false;
            }
        }

        private IEnumerator Start()
        {
#if UNITY_EDITOR
            yield return new WaitForSeconds(1f);
            string previewPath = Path.Combine(Application.dataPath, "NHNDemo", "ShowcasePreview.png");
            ScreenCapture.CaptureScreenshot(previewPath);
#else
            yield break;
#endif
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;
            if (keyboard == null)
                return;

            float dt = Time.deltaTime;
            dashTimer = Mathf.Max(0f, dashTimer - dt);
            actionLockTimer = Mathf.Max(0f, actionLockTimer - dt);

            float horizontal = 0f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) horizontal -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) horizontal += 1f;

            if (horizontal != 0f)
            {
                facing = horizontal > 0f ? 1 : -1;
                spriteRenderer.flipX = facing < 0;
            }

            bool grounded = IsGrounded();
            if (grounded &&
                Time.time - lastJumpTime > 0.16f &&
                Vector2.Dot(body.linearVelocity, currentUp) <= 0.1f)
            {
                jumpCount = 0;
            }

            if (keyboard.spaceKey.wasPressedThisFrame &&
                (grounded || jumpCount < maxJumpCount) &&
                actionLockTimer <= 0f)
            {
                float lateralSpeed = Vector2.Dot(body.linearVelocity, CurrentRight);
                body.linearVelocity = CurrentRight * lateralSpeed + currentUp * jumpForce;
                jumpCount++;
                lastJumpTime = Time.time;
                PlayState(jumpCount >= 2 ? "JumpRise" : "Jump", true);
            }

            if (keyboard.qKey.wasPressedThisFrame && dashTimer <= 0f)
            {
                dashTimer = dashDuration;
                actionLockTimer = dashDuration;
                PlayState("Dash", true);
            }

            bool combatAnimationActive = HandleCombatInput(keyboard, mouse);
            if (combatAnimationActive)
            {
                // Aim, draw, guard and attacks must own the animation for this frame.
                // Continuing below would immediately replace them with Idle/Run/Jump.
                if (dashTimer <= 0f)
                    StopLateralMovement();
                return;
            }

            if (dashTimer > 0f)
            {
                float verticalSpeed = Vector2.Dot(body.linearVelocity, currentUp);
                body.linearVelocity = CurrentRight * (facing * dashSpeed) + currentUp * verticalSpeed;
                return;
            }

            float speed = keyboard.leftShiftKey.isPressed ? sprintSpeed : moveSpeed;
            float normalSpeed = Vector2.Dot(body.linearVelocity, currentUp);
            body.linearVelocity = CurrentRight * (horizontal * speed) + currentUp * normalSpeed;

            if (actionLockTimer > 0f)
                return;

            if (!grounded)
            {
                PlayState(Vector2.Dot(body.linearVelocity, currentUp) > 0.15f ? "JumpRise" : "JumpFall");
            }
            else if (Mathf.Abs(horizontal) > 0.01f)
            {
                PlayState(keyboard.leftShiftKey.isPressed ? "Sprint" : "Run");
            }
            else
            {
                PlayState("Idle");
            }
        }

        private bool HandleCombatInput(Keyboard keyboard, Mouse mouse)
        {
            if (HandleBowInput(mouse))
                return true;

            if (dashTimer > 0f && keyboard.jKey.wasPressedThisFrame)
            {
                dashTimer = 0f;
                return PlayAction("SwordSprintSlash", 0.55f, true);
            }

            if (keyboard.hKey.wasPressedThisFrame)
                return PlayAction("SwordSprintSlash", 0.55f);

            if (keyboard.jKey.wasPressedThisFrame && actionLockTimer <= 0f)
            {
                comboStep = Time.time - lastComboTime <= 0.95f
                    ? (comboStep + 1) % 4
                    : 0;
                lastComboTime = Time.time;
                string[] comboStates =
                {
                    "ComboAttackA",
                    "ComboAttackB",
                    "ComboAttackC",
                    "ComboAttackD"
                };
                return PlayAction(comboStates[comboStep], 0.42f, true);
            }

            if (TryPlayAction(keyboard.kKey, "SwordAttack", 0.48f) ||
                TryPlayAction(keyboard.rKey, "Roll", 0.52f) ||
                TryPlayAction(keyboard.zKey, "PunchA", 0.36f) ||
                TryPlayAction(keyboard.xKey, "PunchB", 0.38f) ||
                TryPlayAction(keyboard.cKey, "PunchC", 0.42f) ||
                TryPlayAction(keyboard.vKey, "KickA", 0.42f) ||
                TryPlayAction(keyboard.bKey, "KickB", 0.44f) ||
                TryPlayAction(keyboard.nKey, "KickC", 0.48f) ||
                TryPlayAction(keyboard.digit1Key, "AirSlash", 0.48f) ||
                TryPlayAction(keyboard.digit2Key, "AirSlashUp", 0.5f) ||
                TryPlayAction(keyboard.digit3Key, "AirSlashDown", 0.5f) ||
                TryPlayAction(keyboard.digit4Key, "GroundSlam", 0.62f) ||
                TryPlayAction(keyboard.digit5Key, "Spin", 0.58f) ||
                TryPlayAction(keyboard.digit6Key, "ThrowOverarm", 0.5f) ||
                TryPlayAction(keyboard.digit7Key, "ThrowUnderarm", 0.5f) ||
                TryPlayAction(keyboard.eKey, "FishingCast", 0.7f))
            {
                return true;
            }

            if (keyboard.gKey.isPressed && actionLockTimer <= 0f && dashTimer <= 0f)
            {
                StopLateralMovement();
                PlayState("SwordGuard");
                return true;
            }

            return false;
        }

        private bool HandleBowInput(Mouse mouse)
        {
            if (mouse == null)
                return false;

            bool aimHeld = mouse.rightButton.isPressed;
            bool drawHeld = mouse.leftButton.isPressed;

            if (bowDrawing && mouse.leftButton.wasReleasedThisFrame)
            {
                bowDrawing = false;
                return PlayAction("BowFire", 0.45f, true);
            }

            if (!aimHeld)
            {
                bowDrawing = false;
                return false;
            }

            if (actionLockTimer > 0f || dashTimer > 0f)
                return true;

            StopLateralMovement();
            if (drawHeld)
            {
                bowDrawing = true;
                PlayState("BowDraw");
            }
            else
            {
                bowDrawing = false;
                PlayState("BowAim");
            }

            return true;
        }

        private bool TryPlayAction(KeyControl key, string stateName, float lockDuration)
        {
            if (!key.wasPressedThisFrame || actionLockTimer > 0f || dashTimer > 0f)
                return false;

            return PlayAction(stateName, lockDuration, true);
        }

        private bool PlayAction(string stateName, float lockDuration, bool force = false)
        {
            if (!force && (actionLockTimer > 0f || dashTimer > 0f))
                return false;

            actionLockTimer = lockDuration;
            StopLateralMovement();
            PlayState(stateName, true);
            if (IsDamagingAction(stateName))
                StartCoroutine(ApplyAttackHit(stateName, lockDuration));
            return true;
        }

        private IEnumerator ApplyAttackHit(string stateName, float actionDuration)
        {
            float hitDelay = actionDuration * GetHitTiming(stateName);
            yield return new WaitForSeconds(hitDelay);

            bool ranged = stateName == "BowFire" ||
                          stateName == "AirSlash" ||
                          stateName == "AirSlashUp" ||
                          stateName == "AirSlashDown" ||
                          stateName == "ThrowOverarm" ||
                          stateName == "ThrowUnderarm";
            bool areaAttack = stateName == "GroundSlam" || stateName == "Spin";

            Vector2 center = body.position + currentUp * 0.72f;
            float radius = meleeRadius;
            if (ranged)
            {
                center += CurrentRight * facing * 2.1f;
                radius = 2.1f;
            }
            else if (areaAttack)
            {
                radius = 1.45f;
            }
            else
            {
                center += CurrentRight * facing * meleeReach;
            }

            Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius);
            HashSet<MonsterHealth> damaged = new HashSet<MonsterHealth>();
            foreach (Collider2D hit in hits)
            {
                MonsterHealth monster = hit.GetComponentInParent<MonsterHealth>();
                if (monster != null && damaged.Add(monster))
                    monster.TakeDamage(attackDamage, CurrentRight * facing);
            }
        }

        private static bool IsDamagingAction(string stateName)
        {
            return stateName.StartsWith("ComboAttack") ||
                   stateName.StartsWith("Punch") ||
                   stateName.StartsWith("Kick") ||
                   stateName.StartsWith("AirSlash") ||
                   stateName == "SwordAttack" ||
                   stateName == "SwordSprintSlash" ||
                   stateName == "GroundSlam" ||
                   stateName == "Spin" ||
                   stateName == "ThrowOverarm" ||
                   stateName == "ThrowUnderarm" ||
                   stateName == "BowFire";
        }

        private static float GetHitTiming(string stateName)
        {
            if (stateName == "BowFire")
                return 0.18f;
            if (stateName == "GroundSlam")
                return 0.68f;
            if (stateName.StartsWith("ComboAttack"))
                return 0.42f;
            return 0.48f;
        }

        private void FixedUpdate()
        {
            body.AddForce(-currentUp * (gravityAcceleration * body.mass), ForceMode2D.Force);
        }

        private void StopLateralMovement()
        {
            float normalSpeed = Vector2.Dot(body.linearVelocity, currentUp);
            body.linearVelocity = currentUp * normalSpeed;
        }

        private bool IsGrounded()
        {
            if (Time.time - lastGroundedTime <= 0.12f)
                return true;

            ContactFilter2D filter = new ContactFilter2D
            {
                useTriggers = false,
                useLayerMask = false
            };
            RaycastHit2D[] hits = new RaycastHit2D[8];
            int hitCount = capsule.Cast(-currentUp, filter, hits, groundProbeDistance);

            for (int index = 0; index < hitCount; index++)
            {
                RaycastHit2D hit = hits[index];
                if (hit.collider != null && hit.collider != capsule)
                {
                    lastGroundedTime = Time.time;
                    return true;
                }
            }

            return false;
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            EvaluateGravitySurface(collision);

            foreach (ContactPoint2D contact in collision.contacts)
            {
                if (Vector2.Dot(contact.normal, currentUp) > 0.35f)
                {
                    lastGroundedTime = Time.time;
                    return;
                }
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            EvaluateGravitySurface(collision);
        }

        private void EvaluateGravitySurface(Collision2D collision)
        {
            GravitySurface2D surface = collision.collider.GetComponentInParent<GravitySurface2D>();
            if (surface == null ||
                !surface.AllowsGravityShift ||
                Time.time - lastGravityShiftTime < gravityShiftCooldown)
            {
                return;
            }

            Vector2 targetUp = SnapToCardinal(surface.SurfaceUp);
            if (Vector2.Dot(targetUp, currentUp) > 0.55f)
                return;

            SetGravitySurface(targetUp);
        }

        private void SetGravitySurface(Vector2 surfaceNormal)
        {
            currentUp = SnapToCardinal(surfaceNormal.normalized);
            lastGravityShiftTime = Time.time;
            lastGroundedTime = Time.time;

            float targetAngle = Mathf.Atan2(currentUp.y, currentUp.x) * Mathf.Rad2Deg - 90f;
            body.SetRotation(targetAngle);
            body.linearVelocity = Vector2.zero;
            body.position += currentUp * 0.04f;
        }

        private static Vector2 SnapToCardinal(Vector2 direction)
        {
            if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
                return direction.x >= 0f ? Vector2.right : Vector2.left;

            return direction.y >= 0f ? Vector2.up : Vector2.down;
        }

        private void PlayState(string stateName, bool restart = false)
        {
            if (!restart && currentState == stateName)
                return;

            currentState = stateName;
            animator.Play(stateName, 0, 0f);
        }

        private void OnGUI()
        {
            GUIStyle title = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            GUIStyle text = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                normal = { textColor = new Color(0.92f, 0.95f, 1f) }
            };

            GUI.Box(new Rect(18, 18, 690, 212), GUIContent.none);
            GUI.Label(new Rect(34, 28, 460, 28), "FTTGR × Pixel Prototype Player", title);
            GUI.Label(new Rect(34, 60, 650, 22), "Move A/D · Sprint Shift · Double Jump Space ×2 · Dash Q · Roll R", text);
            GUI.Label(new Rect(34, 82, 650, 22), "Combo J ×4 · Sword K · Dash Attack: Q then J (or H) · Guard: hold G", text);
            GUI.Label(new Rect(34, 104, 650, 22), "Bow: hold Right Mouse to Aim · hold Left to Draw · release Left to Fire", text);
            GUI.Label(new Rect(34, 126, 650, 22), "Punch Z/X/C · Kick V/B/N", text);
            GUI.Label(new Rect(34, 148, 650, 22), "Special 1 Air · 2 Up · 3 Down · 4 Slam · 5 Spin · 6/7 Throw", text);
            GUI.Label(new Rect(34, 174, 650, 22), $"Animation: {currentState}", text);
            GUI.Label(new Rect(34, 196, 650, 22), $"Gravity Up: {currentUp}", text);
        }
    }
}
