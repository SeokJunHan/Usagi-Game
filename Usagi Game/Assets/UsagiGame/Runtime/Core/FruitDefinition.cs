using UnityEngine;

namespace UsagiGame
{
    [CreateAssetMenu(menuName = "Usagi Game/Fruit Definition")]
    public sealed class FruitDefinition : ScriptableObject
    {
        [field: SerializeField, Tooltip("1부터 시작하는 합체 단계입니다.")]
        public int Stage { get; private set; }

        [field: SerializeField, Tooltip("화면에 표시할 이름입니다.")]
        public string DisplayName { get; private set; } = string.Empty;

        [field: SerializeField, Tooltip("과일 이미지입니다.")]
        public Sprite Sprite { get; private set; }

        [field: SerializeField, Min(0.01f), Tooltip("월드 단위 충돌 반지름입니다.")]
        public float Radius { get; private set; } = 0.3f;

        [field: SerializeField, Min(0.01f), Tooltip("Rigidbody2D 질량입니다.")]
        public float Mass { get; private set; } = 0.1f;

        [field: SerializeField, Min(0), Tooltip("이 단계 두 개가 합쳐질 때 획득하는 점수입니다.")]
        public int MergeScore { get; private set; }

        [field: SerializeField, Min(0), Tooltip("직접 드롭 추첨 가중치입니다. 0이면 드롭되지 않습니다.")]
        public int DropWeight { get; private set; }
    }
}
