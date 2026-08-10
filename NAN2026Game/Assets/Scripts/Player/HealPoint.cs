using UnityEngine;

namespace NAN2026
{
    // 세이브포인트 오브젝트(트리거 콜라이더 필요)에 붙인다. 플레이어가 닿으면
    // 체크포인트를 등록하고 체력·마나를 최대치로 채운다.
    // SetCheckpoint()는 내부에서 같은 씬+근접 좌표 중복을 걸러내므로, 다른 체크포인트
    // 등록 스크립트(Checkpoint2D 등)와 같이 붙어있어도 중복 등록 걱정 없이 안전하다.
    public class HealPoint : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D other)
        {
            TryActivate(other);
        }

        private void TryActivate(Collider2D other)
        {
            if (other == null) return;

            PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
            if (health == null) return;

            health.SetCheckpoint(transform.position);
            health.Heal(health.MaxHealth); // 현재 체력과 무관하게 항상 풀피로

            PlayerMana mana = other.GetComponentInParent<PlayerMana>();
            if (mana != null) mana.RefillToMax();
        }
    }
}