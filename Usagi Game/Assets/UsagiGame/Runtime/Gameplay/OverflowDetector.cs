using System;
using System.Collections.Generic;
using UnityEngine;

namespace UsagiGame
{
    public sealed class OverflowDetector : MonoBehaviour
    {
        [SerializeField, Tooltip("게임 규칙 데이터입니다.")] private GameRuleConfig rules;
        [SerializeField, Tooltip("과일 생성기입니다.")] private FruitSpawner spawner;
        [SerializeField, Tooltip("위험선 표시입니다.")] private SpriteRenderer dangerLine;
        [SerializeField, Tooltip("평상시 위험선 색입니다.")] private Color normalColor = new(1f, 0.5f, 0.4f, 0.25f);
        [SerializeField, Tooltip("넘침 감지 중 위험선 색입니다.")] private Color warningColor = new(1f, 0.15f, 0.1f, 0.9f);
        [SerializeField, Tooltip("화면 비율에 맞춘 보드 경계입니다.")] private BoardLayout boardLayout;

        private readonly Dictionary<int, float> timers = new(64);
        private bool running;
        public event Action Overflowed;

        public void SetRunning(bool value)
        {
            if (rules == null && spawner != null) rules = spawner.Rules;
            if (boardLayout == null) boardLayout = GetComponent<BoardLayout>();
            running = value && rules != null && spawner != null && boardLayout != null;
        }
        public void ResetDetector()
        {
            timers.Clear();
            dangerLine.color = normalColor;
        }

        private void FixedUpdate()
        {
            if (!running) return;
            bool warning = false;
            IReadOnlyList<FruitActor> fruits = spawner.Active;
            for (int i = 0; i < fruits.Count; i++)
            {
                FruitActor fruit = fruits[i];
                if (fruit == null || fruit.IsConsumed || !fruit.HasSettled) continue;

                bool escaped = fruit.transform.position.x < boardLayout.Left - fruit.Definition.Radius ||
                               fruit.transform.position.x > boardLayout.Right + fruit.Definition.Radius ||
                               fruit.transform.position.y < boardLayout.Bottom - fruit.Definition.Radius * 2f;
                if (escaped) { Overflowed?.Invoke(); return; }

                if (fruit.Collider.bounds.max.y <= boardLayout.DangerY)
                {
                    timers.Remove(fruit.Id);
                    continue;
                }

                warning = true;
                timers.TryGetValue(fruit.Id, out float elapsed);
                elapsed = GameRuleLogic.UpdateOverflowTime(true, elapsed, Time.fixedDeltaTime);
                timers[fruit.Id] = elapsed;
                if (elapsed >= rules.OverflowGrace) { Overflowed?.Invoke(); return; }
            }
            dangerLine.color = warning ? warningColor : normalColor;
        }
    }
}
