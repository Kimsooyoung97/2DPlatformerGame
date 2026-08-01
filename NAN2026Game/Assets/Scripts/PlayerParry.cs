using UnityEngine;
using UnityEngine.InputSystem;

namespace NAN2026.Showroom
{
    /// <summary>
    /// Guard and parry on the same key the character controller already uses for its
    /// guard animation (G).
    ///
    ///  - Holding G is a BLOCK: the projectile is stopped, nothing happens to you.
    ///  - Tapping G just as the projectile arrives is a PARRY: it is fired straight back,
    ///    faster than it came in. That is the window a boss fight would be built around.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerParry : MonoBehaviour
    {
        [Tooltip("How long after pressing guard the parry counts. Bigger = more forgiving.")]
        [SerializeField] private float parryWindow = 0.22f;
        [Tooltip("Delay after a parry before another one can be timed.")]
        [SerializeField] private float parryCooldown = 0.25f;

        private float guardPressedAt = -99f;
        private float parriedAt = -99f;
        private float flashUntil;
        private string flashText = string.Empty;

        private int parries;
        private int blocks;

        public bool IsGuarding
        {
            get
            {
                Keyboard keyboard = Keyboard.current;
                return keyboard != null && keyboard.gKey.isPressed;
            }
        }

        public bool ParryReady
        {
            get
            {
                return Time.time - guardPressedAt <= parryWindow &&
                       Time.time - parriedAt >= parryCooldown;
            }
        }

        public int Parries { get { return parries; } }
        public int Blocks { get { return blocks; } }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.gKey.wasPressedThisFrame)
                guardPressedAt = Time.time;
        }

        public void NotifyParry()
        {
            parries++;
            parriedAt = Time.time;
            Flash("PARRY!");
        }

        public void NotifyBlock()
        {
            blocks++;
            Flash("BLOCK");
        }

        private void Flash(string text)
        {
            flashText = text;
            flashUntil = Time.time + 0.6f;
        }

        private void OnGUI()
        {
            GUIStyle small = new GUIStyle(GUI.skin.box)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter
            };

            const float width = 170f;
            GUI.Box(new Rect(Screen.width - width - 16f, 110f, width, 28f),
                "PARRY " + parries + "   BLOCK " + blocks, small);

            if (Time.time < flashUntil)
            {
                GUIStyle big = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 44,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = flashText == "PARRY!"
                        ? new Color(1f, 0.9f, 0.3f)
                        : new Color(0.7f, 0.85f, 1f) }
                };
                GUI.Label(new Rect(0f, Screen.height * 0.28f, Screen.width, 60f), flashText, big);
            }
        }
    }
}
