using System;
using UnityEngine;

namespace UsagiGame
{
    [CreateAssetMenu(menuName = "Usagi Game/Game Rule Config")]
    public sealed class GameRuleConfig : ScriptableObject
    {
        [field: SerializeField, Tooltip("단계 오름차순 과일 정의입니다.")]
        public FruitDefinition[] Fruits { get; private set; } = Array.Empty<FruitDefinition>();

        [field: SerializeField, Min(1f), Tooltip("상자 안쪽 너비입니다.")]
        public float BoardWidth { get; private set; } = 8f;

        [field: SerializeField, Tooltip("배경 장식 위에 배치할 상자 바닥 Y입니다.")]
        public float BoardBottomY { get; private set; } = 0.75f;

        [field: SerializeField, Min(1f), Tooltip("바닥부터 위험선까지 높이입니다.")]
        public float DangerHeight { get; private set; } = 10f;

        [field: SerializeField, Min(0f), Tooltip("위험선 위 조준 높이입니다.")]
        public float AimOffset { get; private set; } = 2f;

        [field: SerializeField, Min(0f), Tooltip("드롭 사이 최소 간격입니다.")]
        public float DropInterval { get; private set; } = 0.45f;

        [field: SerializeField, Min(0.1f), Tooltip("연속 위험선 초과 허용 시간입니다.")]
        public float OverflowGrace { get; private set; } = 1f;

        [field: SerializeField, Min(0.1f), Tooltip("합체 직후 초기 속도 상한입니다.")]
        public float MergeVelocityLimit { get; private set; } = 8f;

        public float DangerLineY => BoardBottomY + DangerHeight;
        public float AimY => DangerLineY + AimOffset;
        public float HalfBoardWidth => BoardWidth * 0.5f;

        public FruitDefinition GetByStage(int stage) =>
            stage > 0 && stage <= Fruits.Length ? Fruits[stage - 1] : null;

        public FruitDefinition PickDrop(System.Random random)
        {
            int total = 0;
            for (int i = 0; i < Fruits.Length; i++)
                if (Fruits[i] != null) total += Mathf.Max(0, Fruits[i].DropWeight);

            if (total <= 0) return null;
            int roll = random.Next(total);
            for (int i = 0; i < Fruits.Length; i++)
            {
                FruitDefinition fruit = Fruits[i];
                if (fruit == null) continue;
                roll -= Mathf.Max(0, fruit.DropWeight);
                if (roll < 0) return fruit;
            }
            return null;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            float previousRadius = 0f;
            for (int i = 0; i < Fruits.Length; i++)
            {
                FruitDefinition fruit = Fruits[i];
                if (fruit == null || fruit.Stage != i + 1)
                    Debug.LogWarning($"{name}: {i + 1}단계 정의가 없거나 단계 번호가 잘못되었습니다.", this);
                else if (fruit.Radius <= previousRadius)
                    Debug.LogWarning($"{name}: 과일 반지름은 단계에 따라 증가해야 합니다.", this);
                if (fruit != null) previousRadius = fruit.Radius;
            }
        }
#endif
    }
}
