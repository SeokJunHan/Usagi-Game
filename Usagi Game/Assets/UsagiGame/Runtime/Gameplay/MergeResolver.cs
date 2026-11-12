using System;
using System.Collections.Generic;
using UnityEngine;

namespace UsagiGame
{
    [DefaultExecutionOrder(-100)]
    public sealed class MergeResolver : MonoBehaviour
    {
        private readonly struct Pair : IComparable<Pair>
        {
            public readonly FruitActor A;
            public readonly FruitActor B;
            public readonly long Key;

            public Pair(FruitActor first, FruitActor second)
            {
                if (first.Id < second.Id) { A = first; B = second; }
                else { A = second; B = first; }
                Key = GameRuleLogic.PairKey(A.Id, B.Id);
            }

            public int CompareTo(Pair other) => Key.CompareTo(other.Key);
        }

        [SerializeField, Tooltip("게임 규칙 데이터입니다.")] private GameRuleConfig rules;
        [SerializeField, Tooltip("과일 생성기입니다.")] private FruitSpawner spawner;

        private readonly List<Pair> candidates = new(64);
        private readonly HashSet<long> candidateKeys = new();
        private readonly HashSet<int> consumedIds = new();

        public GameRuleConfig Rules => rules;
        public int PhysicsStep { get; private set; }
        public event Action<int> ScoreAwarded;

        public void ReportContact(FruitActor a, FruitActor b)
        {
            if (a == null || b == null || a.IsConsumed || b.IsConsumed) return;
            if (!GameRuleLogic.CanMerge(a.Definition.Stage, b.Definition.Stage, a.BornStep, b.BornStep, PhysicsStep)) return;
            Pair pair = new(a, b);
            if (candidateKeys.Add(pair.Key)) candidates.Add(pair);
        }

        public void ResetResolver()
        {
            candidates.Clear();
            candidateKeys.Clear();
            consumedIds.Clear();
            PhysicsStep = 0;
        }

        private void FixedUpdate()
        {
            PhysicsStep++;
            if (candidates.Count == 0) return;

            candidates.Sort();
            consumedIds.Clear();
            for (int i = 0; i < candidates.Count; i++) Resolve(candidates[i]);
            candidates.Clear();
            candidateKeys.Clear();
            spawner.FlushPoolReturns();
        }

        private void Resolve(Pair pair)
        {
            FruitActor a = pair.A;
            FruitActor b = pair.B;
            if (a == null || b == null || a.IsConsumed || b.IsConsumed) return;
            if (consumedIds.Contains(a.Id) || consumedIds.Contains(b.Id)) return;
            if (a.Definition.Stage != b.Definition.Stage) return;

            consumedIds.Add(a.Id);
            consumedIds.Add(b.Id);
            a.Consume();
            b.Consume();

            Vector2 position = (a.transform.position + b.transform.position) * 0.5f;
            Vector2 velocity = (a.Body.linearVelocity + b.Body.linearVelocity) * 0.5f;
            int stage = a.Definition.Stage;
            ScoreAwarded?.Invoke(a.Definition.MergeScore);

            spawner.Remove(a);
            spawner.Remove(b);
            FruitDefinition next = rules.GetByStage(stage + 1);
            if (next != null)
                spawner.SpawnMerged(next, position, velocity, PhysicsStep);
            else if (LocalSave.VibrationEnabled)
                WebVibration.Vibrate(50);
        }
    }
}
