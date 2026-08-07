using UnityEngine;
using UnityEngine.InputSystem;
using NAN2026.Core;

namespace NAN2026
{
    // 밧줄 존 안에서 ↑로 등반 시작, 상/하 이동, 점프(스페이스)나 존 이탈로 종료
    public class RopeClimber : MonoBehaviour
    {
        public RopeClimbConfig config;
        private Rigidbody2D rb;
        private Behaviour controller;
        private RopeZone zone;
        private bool climbing;
        private float savedGravity;
        private Collider2D[] wallCols;
        private Collider2D myCol;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            controller = GetComponent("PlayerController2D") as Behaviour;
            myCol = GetComponent<Collider2D>();
            if (myCol == null) myCol = GetComponentInChildren<Collider2D>();
            wallCols = CollectStageCols();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var z = other.GetComponent<RopeZone>();
            if (z != null) zone = z;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            var z = other.GetComponent<RopeZone>();
            if (z != null && z == zone) { zone = null; if (climbing) StopClimb(false); }
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null || config == null) return;
            if (!climbing)
            {
                if (zone != null && kb.upArrowKey.isPressed) StartClimb();
                return;
            }
            if (kb.spaceKey.wasPressedThisFrame) { StopClimb(true); return; }
            float vy = ClimbMath.ClimbVelocity(kb.upArrowKey.isPressed, kb.downArrowKey.isPressed, config.climbSpeed);
            rb.linearVelocity = new Vector2(0f, vy);
            float tx = zone != null ? zone.transform.position.x + ((BoxCollider2D)zone.GetComponent<BoxCollider2D>()).offset.x : transform.position.x;
            transform.position = new Vector3(Mathf.Lerp(transform.position.x, tx, config.snapLerp * Time.deltaTime * 10f), transform.position.y, transform.position.z);
        }

        private Collider2D[] CollectStageCols()
        {
            var list = new System.Collections.Generic.List<Collider2D>();
            foreach (var nm in new[] { "Stage_Wall", "Stage_Ground" })
            {
                var go = GameObject.Find(nm);
                if (go != null) list.AddRange(go.GetComponentsInChildren<Collider2D>());
            }
            return list.ToArray();
        }

        private void StartClimb()
        {
            climbing = true;
            savedGravity = rb.gravityScale;
            rb.gravityScale = 0f;
            rb.linearVelocity = Vector2.zero;
            if (controller != null) controller.enabled = false;
            wallCols = CollectStageCols();
            if (myCol != null) foreach (var w in wallCols) if (w != null) Physics2D.IgnoreCollision(myCol, w, true);
        }

        private void StopClimb(bool jump)
        {
            climbing = false;
            rb.gravityScale = savedGravity;
            if (jump) rb.linearVelocity = new Vector2(rb.linearVelocity.x, config.exitJumpVelocity);
            if (controller != null) controller.enabled = true;
            if (myCol != null && wallCols != null) foreach (var w in wallCols) if (w != null) Physics2D.IgnoreCollision(myCol, w, false);
        }
    }
}
