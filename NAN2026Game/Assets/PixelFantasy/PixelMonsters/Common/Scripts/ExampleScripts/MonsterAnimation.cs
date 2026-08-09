using System;
using Assets.PixelFantasy.Common.Scripts;
using UnityEngine;
namespace Assets.PixelFantasy.PixelMonsters.Common.Scripts.ExampleScripts
{
    [RequireComponent(typeof(Monster))]
    public class MonsterAnimation : MonoBehaviour
    {
        private Monster _monster;
        public void Start()
        {
            _monster = GetComponent<Monster>();
        }
        public void SetState(MonsterState state)
        {
            foreach (var variable in new[] { "Idle", "Ready", "Walk", "Run", "Jump", "Die" })
            {
                _monster.Animator.SetBool(variable, false);
            }
            switch (state)
            {
                case MonsterState.Idle: _monster.Animator.SetBool("Idle", true); break;
                case MonsterState.Ready: _monster.Animator.SetBool("Ready", true); break;
                case MonsterState.Walk: _monster.Animator.SetBool("Walk", true); break;
                case MonsterState.Run: _monster.Animator.SetBool("Run", true); break;
                case MonsterState.Jump: _monster.Animator.SetBool("Jump", true); break;
                case MonsterState.Die: _monster.Animator.SetBool("Die", true); break;
                default: throw new NotSupportedException();
            }
            //Debug.Log("SetState: " + state);
        }
        public MonsterState GetState()
        {
            if (_monster.Animator.GetBool("Idle")) return MonsterState.Idle;
            if (_monster.Animator.GetBool("Ready")) return MonsterState.Ready;
            if (_monster.Animator.GetBool("Walk")) return MonsterState.Walk;
            if (_monster.Animator.GetBool("Run")) return MonsterState.Run;
            if (_monster.Animator.GetBool("Jump")) return MonsterState.Jump;
            if (_monster.Animator.GetBool("Die")) return MonsterState.Die;
            return MonsterState.Ready;
        }
        public void Idle()
        {
            SetState(MonsterState.Idle);
        }
        public void Ready()
        {
            if (GetState() == MonsterState.Walk)
            {
                EffectManager.Instance.CreateSpriteEffect(_monster, "Brake");
            }
            else if (GetState() == MonsterState.Idle)
            {
                return;
            }
            SetState(MonsterState.Ready);
        }
        public void Run()
        {
            if (GetState() != MonsterState.Walk)
            {
                EffectManager.Instance.CreateSpriteEffect(_monster, "Run");
            }
            SetState(MonsterState.Walk);
        }
        public void Jump()
        {
            EffectManager.Instance.CreateSpriteEffect(_monster, "Jump");
            SetState(MonsterState.Run);
        }
        public void Fall()
        {
            SetState(MonsterState.Run);
        }
        public void Land()
        {
            EffectManager.Instance.CreateSpriteEffect(_monster, "Fall");
        }

        public void Die()
        {
            SetState(MonsterState.Die);
        }

        public void Attack()
        {
            _monster.Animator.SetTrigger("Attack");
        }
        public void Attack2()
        {
            _monster.Animator.SetTrigger("Attack2");
        }
        public void Attack3()
        {
            _monster.Animator.SetTrigger("Attack3");
        }

        // 피격 시 MonsterHealth가 잠깐 애니메이터 평가를 멈추고 싶을 때 쓴다.
        // enabled=false로 두면 Animator가 파라미터/트랜지션 평가를 멈추고 현재 프레임에서
        // '정지'한다 — Hit()으로 Play()해서 히트 포즈로 점프시킨 직후 꺼두면 그 포즈에서
        // 얼어붙은 채로 플래시가 확실히 보인다. 다시 true로 켜면 멈췄던 지점부터 이어서 평가된다.
        public void SetAnimatorEnabled(bool value)
        {
            if (_monster != null && _monster.Animator != null)
                _monster.Animator.enabled = value;
        }

        [SerializeField] private float hitFreezeDuration = 0.08f;
        private System.Collections.IEnumerator freezeRoutine;

        // 예전엔 SetTrigger("Hit")만 불렀는데, Attack 계열 State에서 Hit로 넘어가는
        // 전이(transition)가 그래프에 없거나 Exit Time이 걸려있으면 트리거가 그냥 씹혔다
        // (공격 중엔 맞아도 Hit 애니메이션이 안 나오는 증상). Animator.Play()는 전이 그래프를
        // 완전히 무시하고 지정한 State로 즉시 강제 점프시키므로, 어떤 State에서 맞든 무조건
        // Hit이 재생된다. 대신 Attack 계열 트리거가 남아있으면 Hit 끝나고 다시 공격 애니메이션이
        // 즉시 튀어나올 수 있어 같이 리셋해준다.
        //
        // 여기에 더해 Animator.speed를 잠깐 0으로 만들어 그 프레임에서 딱 멈추는 히트스톱을
        // 추가했다 — "Hit"이라는 State 이름이 실제 Animator Controller와 정확히 안 맞아도
        // (Play가 조용히 실패해도) 최소한 지금 재생 중이던 프레임이 멈추는 반응은 확실히 나온다.
        public void Hit()
        {
            _monster.Animator.ResetTrigger("Attack");
            _monster.Animator.ResetTrigger("Attack2");
            _monster.Animator.ResetTrigger("Attack3");
            _monster.Animator.Play("Hit", 0, 0f);

            StopAllCoroutines();
            StartCoroutine(FreezeBriefly());
        }

        private System.Collections.IEnumerator FreezeBriefly()
        {
            _monster.Animator.speed = 0f;
            yield return new WaitForSeconds(hitFreezeDuration);
            _monster.Animator.speed = 1f;
        }
    }
}