using UnityEngine;

namespace UsagiGame
{
    [DefaultExecutionOrder(-200)]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class CameraBackground : MonoBehaviour
    {
        [SerializeField, Tooltip("배경을 맞출 카메라입니다.")] private Camera targetCamera;
        private SpriteRenderer spriteRenderer;

        public Bounds VisibleBounds { get; private set; }

        public void Configure(Camera camera) => targetCamera = camera;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            Refresh();
        }

        private void LateUpdate() => Refresh();

        public void Refresh()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (targetCamera == null || spriteRenderer == null || spriteRenderer.sprite == null) return;
            transform.position = new Vector3(targetCamera.transform.position.x, targetCamera.transform.position.y, 5f);
            VisibleBounds = spriteRenderer.bounds;
        }
    }
}
