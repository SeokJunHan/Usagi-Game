using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace UsagiGame
{
    public sealed class DropController : MonoBehaviour
    {
        [SerializeField, Tooltip("게임 규칙 데이터입니다.")] private GameRuleConfig rules;
        [SerializeField, Tooltip("월드 카메라입니다.")] private Camera worldCamera;
        [SerializeField, Tooltip("과일 생성기입니다.")] private FruitSpawner spawner;
        [SerializeField, Tooltip("합체 처리기입니다.")] private MergeResolver mergeResolver;
        [SerializeField, Tooltip("대기 과일 표시입니다.")] private SpriteRenderer preview;
        [SerializeField, Tooltip("조준선 표시입니다.")] private SpriteRenderer aimLine;
        [SerializeField, Tooltip("정상 조준선 색입니다.")] private Color readyColor = new(1f, 1f, 1f, 0.5f);
        [SerializeField, Tooltip("생성 불가 조준선 색입니다.")] private Color blockedColor = new(1f, 0.2f, 0.2f, 0.9f);
        [SerializeField, Tooltip("화면 비율에 맞춘 보드 경계입니다.")] private BoardLayout boardLayout;

        private bool running;
        private bool tracking;
        private int pointerId;
        private float aimX;
        private float nextDropTime;

        public void SetRunning(bool value)
        {
            running = value;
            if (!value) tracking = false;
            preview.enabled = value;
            aimLine.enabled = value;
        }

        public void ResetController()
        {
            tracking = false;
            nextDropTime = 0f;
            aimX = 0f;
            RefreshPreview();
        }

        private void OnEnable() => spawner.QueueChanged += OnQueueChanged;
        private void OnDisable() => spawner.QueueChanged -= OnQueueChanged;
        private void OnQueueChanged(FruitDefinition current, FruitDefinition next) => RefreshPreview();

        private void Update()
        {
            if (!running || spawner.Current == null) return;
            Pointer pointer = Pointer.current;
            if (pointer == null) return;

            if (!tracking && pointer.press.wasPressedThisFrame)
            {
                if (Time.unscaledTime < nextDropTime || IsOverUi(pointer)) return;
                tracking = true;
                pointerId = pointer.deviceId;
                SetAim(pointer.position.ReadValue());
            }
            else if (tracking && pointer.deviceId == pointerId)
            {
                SetAim(pointer.position.ReadValue());
                if (pointer.press.wasReleasedThisFrame)
                {
                    tracking = false;
                    if (spawner.Drop(aimX, mergeResolver.PhysicsStep) != null)
                        nextDropTime = Time.unscaledTime + rules.DropInterval;
                }
            }
        }

        private bool IsOverUi(Pointer pointer)
        {
            if (EventSystem.current == null) return false;
            return EventSystem.current.IsPointerOverGameObject(pointer is Touchscreen ? ((Touchscreen)pointer).primaryTouch.touchId.ReadValue() : -1);
        }

        private void SetAim(Vector2 screenPosition)
        {
            Vector3 world = worldCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, -worldCamera.transform.position.z));
            float radius = spawner.Current.Radius;
            aimX = Mathf.Clamp(world.x, boardLayout.Left + radius, boardLayout.Right - radius);
            preview.transform.position = new Vector3(aimX, boardLayout.AimY, 0f);
            aimLine.transform.position = new Vector3(aimX, (boardLayout.DangerY + boardLayout.AimY) * .5f, 0.1f);
            aimLine.color = spawner.CanSpawnAt(aimX) ? readyColor : blockedColor;
        }

        private void RefreshPreview()
        {
            FruitDefinition fruit = spawner.Current;
            if (fruit == null) return;
            preview.sprite = fruit.Sprite;
            float scale = fruit.Radius * 2f / Mathf.Max(fruit.Sprite.bounds.size.x, fruit.Sprite.bounds.size.y);
            preview.transform.localScale = Vector3.one * scale;
            SetAim(worldCamera.WorldToScreenPoint(new Vector3(aimX, boardLayout.AimY)));
        }
    }
}
