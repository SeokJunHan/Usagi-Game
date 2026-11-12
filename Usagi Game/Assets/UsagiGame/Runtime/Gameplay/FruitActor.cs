using System.Collections.Generic;
using UnityEngine;

namespace UsagiGame
{
    [DisallowMultipleComponent]
    public sealed class FruitActor : MonoBehaviour
    {
        [SerializeField, Tooltip("물리 강체입니다.")] private Rigidbody2D body;
        [SerializeField, Tooltip("스프라이트 외곽 충돌체입니다.")] private PolygonCollider2D spriteCollider;
        [SerializeField, Tooltip("과일 이미지를 표시할 렌더러입니다.")] private SpriteRenderer spriteRenderer;

        private MergeResolver mergeResolver;
        private readonly List<Vector2> shapePoints = new(64);
        private readonly Vector2[] fallbackShape = new Vector2[4];
        private int bornStep;

        public int Id { get; private set; }
        public FruitDefinition Definition { get; private set; }
        public Rigidbody2D Body => body;
        public Collider2D Collider => spriteCollider;
        public bool HasSettled { get; private set; }
        public bool IsConsumed { get; private set; }
        public int BornStep => bornStep;

        private void Awake()
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody2D>();
                if (body == null) body = gameObject.AddComponent<Rigidbody2D>();
            }

            body.gravityScale = 1f;
            body.collisionDetectionMode = CollisionDetectionMode2D.Discrete;

            if (spriteCollider == null)
            {
                spriteCollider = GetComponent<PolygonCollider2D>();
                if (spriteCollider == null) spriteCollider = gameObject.AddComponent<PolygonCollider2D>();
            }

            CircleCollider2D oldCircle = GetComponent<CircleCollider2D>();
            if (oldCircle != null) oldCircle.enabled = false;

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
                if (spriteRenderer == null)
                {
                    GameObject visual = new("Visual");
                    visual.transform.SetParent(transform, false);
                    spriteRenderer = visual.AddComponent<SpriteRenderer>();
                    spriteRenderer.sortingOrder = 5;
                }
            }
        }

        public void Initialize(int id, FruitDefinition definition, MergeResolver resolver, int physicsStep, bool merged)
        {
            Id = id;
            Definition = definition;
            mergeResolver = resolver;
            bornStep = physicsStep;
            HasSettled = merged;
            IsConsumed = false;
            body.simulated = true;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            spriteCollider.enabled = true;
            body.mass = definition.Mass;
            spriteRenderer.sprite = definition.Sprite;
            float diameter = definition.Radius * 2f;
            Vector2 spriteSize = definition.Sprite.bounds.size;
            float scale = diameter / Mathf.Max(spriteSize.x, spriteSize.y);
            spriteRenderer.transform.localScale = Vector3.one * scale;
            ApplySpriteShape(definition.Sprite, scale);
        }

        public void Consume()
        {
            IsConsumed = true;
            spriteCollider.enabled = false;
            body.simulated = false;
        }

        private void ApplySpriteShape(Sprite sprite, float scale)
        {
            int shapeCount = sprite.GetPhysicsShapeCount();
            spriteCollider.pathCount = Mathf.Max(1, shapeCount);
            if (shapeCount == 0)
            {
                float radius = Definition.Radius;
                fallbackShape[0] = new Vector2(-radius, -radius);
                fallbackShape[1] = new Vector2(-radius, radius);
                fallbackShape[2] = new Vector2(radius, radius);
                fallbackShape[3] = new Vector2(radius, -radius);
                spriteCollider.SetPath(0, fallbackShape);
                return;
            }

            for (int path = 0; path < shapeCount; path++)
            {
                shapePoints.Clear();
                sprite.GetPhysicsShape(path, shapePoints);
                for (int i = 0; i < shapePoints.Count; i++) shapePoints[i] *= scale;
                spriteCollider.SetPath(path, shapePoints);
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            HasSettled = true;
            Report(collision.collider);
        }

        private void OnCollisionStay2D(Collision2D collision) => Report(collision.collider);

        private void Report(Collider2D otherCollider)
        {
            if (IsConsumed || !otherCollider.TryGetComponent(out FruitActor other)) return;
            mergeResolver.ReportContact(this, other);
        }
    }
}
