using UnityEngine;

namespace NAN2026
{
    // 상자 보상 연출 수치 단일 소유 (MonoBehaviour 숫자 리터럴 금지 규약)
    [CreateAssetMenu(fileName = "ChestRewardConfig", menuName = "NAN2026/ChestRewardConfig")]
    public class ChestRewardConfig : ScriptableObject
    {
        [Header("상자 내구도")]
        [Tooltip("몇 대 때리면 부서지는가")]
        public int chestHits = 3;
        [Tooltip("부술 수 있는 판정 상자 크기(월드 유닛)")]
        public Vector2 hitBoxSize = new Vector2(1.09f, 0.91f);
        public Vector2 hitBoxOffset = new Vector2(0f, 0.45f);

        [Header("피격 흔들림")]
        public float shakeAmount = 0.06f;
        public float shakeSeconds = 0.12f;

        [Header("보상 아이콘 - 떠오름")]
        [Tooltip("상자 위 몇 유닛에서 생겨나는가")]
        public float spawnHeight = 0.6f;
        [Tooltip("떠오르는 높이(유닛)")]
        public float riseDistance = 1.5f;
        public float riseTime = 0.5f;

        [Header("보상 아이콘 - 흡수")]
        [Tooltip("플레이어까지 빨려 들어가는 시간")]
        public float absorbTime = 0.6f;
        [Tooltip("흡수 진행도 몇 부터 투명해지기 시작하는가")]
        [Range(0f, 1f)] public float fadeStart = 0.35f;
        public float scaleFrom = 1f;
        public float scaleTo = 0.3f;
        [Tooltip("플레이어 발밑에서 몇 유닛 위를 목표로 하는가")]
        public float targetHeight = 0.9f;

        [Header("보상 아이콘 - 표시")]
        [Tooltip("슬롯별 개별 아이콘이 없거나 인덱스 범위를 벗어났을 때 쓰는 기본값(폴백)")]
        public Sprite icon;
        [Tooltip("슬롯별 아이콘. index 0=1번(번개) / 1=2번(가로베기) / 2=3번(나선환)")]
        public Sprite[] icons = new Sprite[3];

        /// <summary>슬롯 인덱스에 맞는 아이콘을 돌려준다. 그 슬롯에 개별 아이콘이 없으면
        /// 공용 icon으로 폴백한다(연출이 아예 안 뜨는 것보단 낫다).</summary>
        public Sprite GetIcon(int slot)
        {
            if (icons != null && slot >= 0 && slot < icons.Length && icons[slot] != null)
                return icons[slot];
            return icon;
        }
        [Tooltip("아이콘 월드 한 변 길이(유닛)")]
        public float worldSize = 1.1f;
        public int sortingOrder = 940;
        public Color tint = Color.white;

        [Header("좌하단 슬롯 UI")]
        public int slotCapacity = 3;
        public float slotSize = 84f;
        public float slotSpacing = 14f;
        public float marginX = 40f;
        public float marginY = 40f;
        public float popTime = 0.3f;
        public float popPeak = 1.5f;
        [Tooltip("아직 못 얻은 칸도 흐리게 보여줄지")]
        public bool showEmptySlots = true;
        public Color slotEmptyTint = new Color(1f, 1f, 1f, 0.18f);
    }
}
