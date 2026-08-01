using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NAN2026.Showroom
{
    /// <summary>
    /// Cat-Mario style life handling: one touch of any hazard kills, respawn is
    /// almost instant, and every trap in the level snaps back to its untriggered
    /// state so each attempt is identical.
    ///
    /// Testing aids: F2 toggles invincibility, F3 resets every trap by hand.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerHealth : MonoBehaviour
    {
        [Header("Testing")]
        [Tooltip("While on, hazards cannot kill. Toggle in play mode with F2.")]
        [SerializeField] private bool invincible = true;

        [Header("Death")]
        [SerializeField] private float respawnDelay = 0.2f;
        [SerializeField] private float spawnGrace = 0.5f;
        [SerializeField] private float fallKillY = -18f;

        [Header("Hazards")]
        [SerializeField] private string hazardNameContains = "Spikes";

        private Rigidbody2D body;
        private MonoBehaviour movementController;
        private SpriteRenderer[] visuals;
        private Vector3 checkpoint;
        private float graceUntil;
        private bool dying;
        private int deaths;

        public int Deaths { get { return deaths; } }
        public bool IsDying { get { return dying; } }
        public bool Invincible
        {
            get { return invincible; }
            set { invincible = value; }
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            visuals = GetComponentsInChildren<SpriteRenderer>(true);

            foreach (MonoBehaviour behaviour in GetComponents<MonoBehaviour>())
            {
                if (behaviour != this && behaviour.GetType().Name == "PixelPlayerController")
                {
                    movementController = behaviour;
                    break;
                }
            }

            checkpoint = transform.position;
            graceUntil = Time.time + spawnGrace;
        }
        public void TakeDamage(float damage, Vector3 pos)
        {

        }
        public void SetCheckpoint(Vector3 position)
        {
            checkpoint = position;
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.f2Key.wasPressedThisFrame)
                    invincible = !invincible;
                if (keyboard.f3Key.wasPressedThisFrame)
                    ResetAllTraps();
            }

            // Falling out of the world still resets you, even while invincible.
            if (!dying && transform.position.y < fallKillY)
                Respawn();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryHazard(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            TryHazard(other);
        }

        private void TryHazard(Collider2D other)
        {
            if (other == null || dying || invincible || Time.time < graceUntil)
                return;

            Hazard2D hazard = other.GetComponentInParent<Hazard2D>();
            bool lethal = (hazard != null && hazard.enabled) ||
                          other.gameObject.name.Contains(hazardNameContains);

            if (lethal)
                Kill();
        }

        public void Kill()
        {
            if (dying || invincible)
                return;

            deaths++;
            Respawn();
        }

        /// <summary>Sends the player back to the checkpoint and rearms every trap.</summary>
        public void Respawn()
        {
            if (dying)
                return;

            dying = true;
            StartCoroutine(RespawnRoutine());
        }

        private IEnumerator RespawnRoutine()
        {
            SetControllerEnabled(false);
            body.linearVelocity = Vector2.zero;
            body.gravityScale = 0f;
            SetVisible(false);

            yield return new WaitForSeconds(respawnDelay);

            ResetAllTraps();

            transform.position = checkpoint;
            transform.rotation = Quaternion.identity;
            body.SetRotation(0f);
            body.linearVelocity = Vector2.zero;

            SetVisible(true);
            SetControllerEnabled(true);

            graceUntil = Time.time + spawnGrace;
            dying = false;
        }

        /// <summary>Returns every trap in the scene to its untriggered state.</summary>
        public static int ResetAllTraps()
        {
            int count = 0;
            MonoBehaviour[] all = FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (MonoBehaviour behaviour in all)
            {
                ITrapResettable trap = behaviour as ITrapResettable;
                if (trap == null)
                    continue;

                trap.ResetTrap();
                count++;
            }
            return count;
        }

        private void SetControllerEnabled(bool value)
        {
            if (movementController != null)
                movementController.enabled = value;
        }

        private void SetVisible(bool value)
        {
            foreach (SpriteRenderer renderer in visuals)
            {
                if (renderer != null)
                    renderer.enabled = value;
            }
        }

        private void OnGUI()
        {
            GUIStyle style = new GUIStyle(GUI.skin.box)
            {
                fontSize = 17,
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };

            const float width = 170f;
            GUI.Box(new Rect(Screen.width - width - 16f, 14f, width, 32f),
                "DEATHS   " + deaths, style);

            if (invincible)
            {
                Color previous = GUI.color;
                GUI.color = new Color(0.45f, 1f, 0.6f);
                GUI.Box(new Rect(Screen.width - width - 16f, 50f, width, 28f),
                    "INVINCIBLE  (F2)", style);
                GUI.color = previous;
            }

            GUI.Label(new Rect(Screen.width - width - 16f, 82f, width, 22f),
                "   F2 invincible · F3 reset traps");
        }
    }
}
