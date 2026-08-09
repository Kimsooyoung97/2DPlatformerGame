using System.Linq;
using UnityEngine;

namespace Assets.PixelFantasy.PixelMonsters.Common.Scripts.ExampleScripts
{
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(MonsterAnimation))]
    public class MonsterController2D : MonoBehaviour
    {
        public Vector2 Input;
        public bool IsGrounded;

        public float Acceleration = 40;
        public float MaxSpeed = 8;
        public float JumpForce = 1000;
        public float Gravity = 70;

        private Collider2D _collider;
        private Rigidbody2D _rigidbody;
        private MonsterAnimation _animation;

        private bool _jump;

        // 넉백: 이 시각까지는 FixedUpdate가 Input 기반 속도 제어(가속/감속)를 건너뛴다.
        // Input.x==0일 때 매 스텝 velocity.x를 0으로 되돌리는 로직이 넉백 속도를 그대로
        // 씹어버리기 때문에(MiddleBossAttackPatterns.cs에서 겪은 것과 동일한 원인), 이
        // 기간 동안만 그 로직을 완전히 우회한다. 중력은 계속 적용해서 자연스럽게 떨어진다.
        private float _knockbackUntil;

        public void Start()
        {
            _collider = GetComponent<Collider2D>();
            _rigidbody = GetComponent<Rigidbody2D>();
            _animation = GetComponent<MonsterAnimation>();
        }

        /// <summary>외부(MonsterHealth 등)에서 피격 넉백을 걸 때 호출한다.
        /// velocity: 즉시 부여할 속도(방향+크기). duration: 이 속도를 유지하며
        /// Input 기반 제어를 무시할 시간(초).</summary>
        public void ApplyKnockback(Vector2 velocity, float duration)
        {
            _rigidbody.linearVelocity = velocity;
            _knockbackUntil = Time.time + duration;
        }

        public void FixedUpdate()
        {
            var state = _animation.GetState();

            if (state == MonsterState.Die) return;

            var velocity = _rigidbody.linearVelocity;

            if (Time.time < _knockbackUntil)
            {
                // 넉백 중: Input 기반 가속/감속을 건너뛰고, 중력만 그대로 적용한다.
                if (!IsGrounded)
                    velocity.y -= Gravity * Time.fixedDeltaTime;

                _rigidbody.linearVelocity = velocity;
                return;
            }

            if (Input.x == 0)
            {
                if (IsGrounded)
                {
                    velocity.x = Mathf.MoveTowards(velocity.x, 0, Acceleration * 3 * Time.fixedDeltaTime);
                }
            }
            else
            {
                var maxSpeed = MaxSpeed;
                var acceleration = Acceleration;

                if (_jump)
                {
                    acceleration /= 2;
                }

                velocity.x = Mathf.MoveTowards(velocity.x, Input.x * maxSpeed, acceleration * Time.fixedDeltaTime);
                Turn(velocity.x);
            }

            if (IsGrounded)
            {
                if (!_jump)
                {
                    if (Input.x == 0)
                    {
                        _animation.Ready();
                    }
                    else
                    {
                        _animation.Run();
                    }
                }

                if (Input.y > 0 && !_jump)
                {
                    _jump = true;
                    _rigidbody.AddForce(Vector2.up * JumpForce);
                    _animation.Jump();
                }
            }
            else
            {
                velocity.y -= Gravity * Time.fixedDeltaTime;

                if (velocity.y < 0)
                {
                    _jump = true;
                    _animation.Fall();
                }
            }

            _rigidbody.linearVelocity = velocity;
        }

        private void Turn(float direction)
        {
            var scale = transform.localScale;

            scale.x = Mathf.Sign(direction) * Mathf.Abs(scale.x);

            transform.localScale = scale;
        }

        private Collider2D _ground;

        public void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.contacts.All(i => i.point.y <= _collider.bounds.min.y + 0.1f))
            {
                IsGrounded = true;
                _ground = collision.collider;

                if (_jump)
                {
                    _jump = false;
                    _animation.Land();
                }
            }
        }

        public void OnCollisionExit2D(Collision2D collision)
        {
            if (IsGrounded && collision.collider == _ground)
            {
                IsGrounded = false;
            }
        }
    }
}