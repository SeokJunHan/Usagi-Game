using System;
using System.Collections.Generic;
using UnityEngine;

namespace UsagiGame
{
    public sealed class FruitSpawner : MonoBehaviour
    {
        private const int InitialPoolSize = 50;

        [SerializeField, Tooltip("게임 규칙 데이터입니다.")] private GameRuleConfig rules;
        [SerializeField, Tooltip("물리 과일 프리팹입니다.")] private FruitActor fruitPrefab;
        [SerializeField, Tooltip("생성된 과일의 부모입니다.")] private Transform fruitRoot;
        [SerializeField, Tooltip("생성 공간 확인용 레이어입니다.")] private LayerMask fruitLayer;
        [SerializeField, Tooltip("화면 비율에 맞춘 보드 경계입니다.")] private BoardLayout boardLayout;

        private readonly List<FruitActor> active = new(64);
        private readonly List<FruitActor> pendingReturns = new(16);
        private readonly Stack<FruitActor> pool = new(InitialPoolSize);
        private readonly Collider2D[] overlapBuffer = new Collider2D[8];
        private readonly System.Random random = new();
        private ContactFilter2D spawnFilter;
        private MergeResolver mergeResolver;
        private int nextId = 1;

        public IReadOnlyList<FruitActor> Active => active;
        public GameRuleConfig Rules => rules;
        public FruitDefinition Current { get; private set; }
        public FruitDefinition Next { get; private set; }
        public event Action<FruitDefinition, FruitDefinition> QueueChanged;

        public bool Initialize(MergeResolver resolver)
        {
            mergeResolver = resolver;
            if (rules == null && resolver != null) rules = resolver.Rules;
            if (boardLayout == null) boardLayout = GetComponent<BoardLayout>();
            if (fruitRoot == null)
            {
                GameObject root = new("Fruits");
                fruitRoot = root.transform;
            }
            if (fruitPrefab == null) fruitPrefab = CreateRuntimePrefab();

            if (rules == null || boardLayout == null)
            {
                Debug.LogError("FruitSpawner에 GameRuleConfig 또는 BoardLayout이 필요합니다.", this);
                enabled = false;
                return false;
            }

            spawnFilter = new ContactFilter2D { useTriggers = false };
            spawnFilter.SetLayerMask(fruitLayer);
            PrewarmPool();
            ResetQueue();
            return true;
        }

        public void ResetQueue()
        {
            Current = rules.PickDrop(random);
            Next = rules.PickDrop(random);
            QueueChanged?.Invoke(Current, Next);
        }

        public bool CanSpawnAt(float x)
        {
            if (Current == null) return false;
            int count = Physics2D.OverlapCircle(new Vector2(x, boardLayout.AimY), Current.Radius, spawnFilter, overlapBuffer);
            for (int i = 0; i < count; i++)
            {
                Rigidbody2D body = overlapBuffer[i].attachedRigidbody;
                if (body != null && body.bodyType == RigidbodyType2D.Dynamic) return false;
            }
            return true;
        }

        public FruitActor Drop(float x, int physicsStep)
        {
            if (!CanSpawnAt(x)) return null;
            FruitActor actor = Spawn(Current, new Vector2(x, boardLayout.AimY), physicsStep, false);
            Current = Next;
            Next = rules.PickDrop(random);
            QueueChanged?.Invoke(Current, Next);
            return actor;
        }

        public FruitActor SpawnMerged(FruitDefinition definition, Vector2 position, Vector2 velocity, int physicsStep)
        {
            FruitActor actor = Spawn(definition, ClampInsideBoard(position, definition.Radius), physicsStep, true);
            actor.Body.linearVelocity = Vector2.ClampMagnitude(velocity, rules.MergeVelocityLimit);
            return actor;
        }

        public void Remove(FruitActor actor)
        {
            if (actor == null || !active.Remove(actor)) return;
            actor.gameObject.SetActive(false);
            pendingReturns.Add(actor);
        }

        public void Clear()
        {
            FlushPoolReturns();
            for (int i = active.Count - 1; i >= 0; i--)
                if (active[i] != null) ReturnToPool(active[i]);
            active.Clear();
            nextId = 1;
            ResetQueue();
        }

        public bool Restore(GameSaveData data, int physicsStep)
        {
            Clear();
            Current = rules.GetByStage(data.CurrentStage);
            Next = rules.GetByStage(data.NextStage);
            if (Current == null || Next == null) return false;

            for (int i = 0; i < data.Fruits.Length; i++)
            {
                FruitSaveData saved = data.Fruits[i];
                FruitDefinition definition = rules.GetByStage(saved.Stage);
                if (definition == null) continue;

                FruitActor actor = Spawn(definition, saved.Position, physicsStep, true);
                actor.transform.rotation = Quaternion.Euler(0f, 0f, saved.Rotation);
                actor.Body.linearVelocity = saved.Velocity;
                actor.Body.angularVelocity = saved.AngularVelocity;
            }

            QueueChanged?.Invoke(Current, Next);
            return true;
        }

        internal void FlushPoolReturns()
        {
            for (int i = 0; i < pendingReturns.Count; i++)
                ReturnToPool(pendingReturns[i]);
            pendingReturns.Clear();
        }

        private FruitActor Spawn(FruitDefinition definition, Vector2 position, int physicsStep, bool merged)
        {
            FruitActor actor = pool.Count > 0 ? pool.Pop() : CreatePooledActor();
            actor.transform.SetPositionAndRotation(position, Quaternion.identity);
            actor.Initialize(nextId++, definition, mergeResolver, physicsStep, merged);
            actor.gameObject.SetActive(true);
            active.Add(actor);
            return actor;
        }

        private void PrewarmPool()
        {
            while (pool.Count < InitialPoolSize)
                pool.Push(CreatePooledActor());
        }

        private FruitActor CreatePooledActor()
        {
            FruitActor actor = Instantiate(fruitPrefab, fruitRoot);
            actor.gameObject.SetActive(false);
            return actor;
        }

        private void ReturnToPool(FruitActor actor)
        {
            actor.gameObject.SetActive(false);
            actor.transform.SetParent(fruitRoot, false);
            pool.Push(actor);
        }

        private FruitActor CreateRuntimePrefab()
        {
            GameObject template = new("RuntimeFruitTemplate");
            template.transform.SetParent(fruitRoot, false);
            FruitActor actor = template.AddComponent<FruitActor>();
            template.SetActive(false);
            return actor;
        }

        private Vector2 ClampInsideBoard(Vector2 position, float radius)
        {
            return new Vector2(Mathf.Clamp(position.x, boardLayout.Left + radius, boardLayout.Right - radius), Mathf.Max(boardLayout.Bottom + radius, position.y));
        }
    }
}
