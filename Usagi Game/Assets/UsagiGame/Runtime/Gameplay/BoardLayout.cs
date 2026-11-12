using UnityEngine;

namespace UsagiGame
{
    public sealed class BoardLayout : MonoBehaviour
    {
        [SerializeField, Tooltip("보드를 표시하는 월드 카메라입니다.")] private Camera worldCamera;
        [SerializeField, Tooltip("보드 외곽의 기준이 되는 카메라 배경입니다.")] private CameraBackground cameraBackground;
        [SerializeField, Tooltip("바닥입니다.")] private SpriteRenderer floor;
        [SerializeField, Tooltip("왼쪽 벽입니다.")] private SpriteRenderer leftWall;
        [SerializeField, Tooltip("오른쪽 벽입니다.")] private SpriteRenderer rightWall;
        [SerializeField, Tooltip("위험선입니다.")] private SpriteRenderer dangerLine;
        [SerializeField, Tooltip("조준선입니다.")] private SpriteRenderer aimLine;
        [SerializeField, Range(.6f, .98f), Tooltip("조준 위치의 화면 높이 비율입니다.")] private float aimViewport = .86f;
        [SerializeField, Min(.05f), Tooltip("벽과 바닥 두께입니다.")] private float thickness = .35f;

        private int lastWidth;
        private int lastHeight;
        private float lastOrthographicSize;
        private BoxCollider2D floorCollider;
        private BoxCollider2D leftWallCollider;
        private BoxCollider2D rightWallCollider;

        public float Left { get; private set; }
        public float Right { get; private set; }
        public float Bottom { get; private set; }
        public float DangerY { get; private set; }
        public float AimY { get; private set; }
        public float Width => Right - Left;

        private void Awake()
        {
            floorCollider = GetCollider(floor);
            leftWallCollider = GetCollider(leftWall);
            rightWallCollider = GetCollider(rightWall);
            Refresh(true);
        }
        private void LateUpdate() => Refresh(false);

        public void Refresh(bool force)
        {
            if (!Application.isPlaying && floorCollider == null)
            {
                floorCollider = GetCollider(floor);
                leftWallCollider = GetCollider(leftWall);
                rightWallCollider = GetCollider(rightWall);
            }
            if (!force && lastWidth == Screen.width && lastHeight == Screen.height && Mathf.Approximately(lastOrthographicSize, worldCamera.orthographicSize)) return;
            lastWidth = Screen.width;
            lastHeight = Screen.height;
            lastOrthographicSize = worldCamera.orthographicSize;

            Bounds bounds = cameraBackground != null
                ? cameraBackground.VisibleBounds
                : new Bounds(worldCamera.transform.position, new Vector3(worldCamera.orthographicSize * 2f * worldCamera.aspect, worldCamera.orthographicSize * 2f));
            float centerX = bounds.center.x;
            float backgroundWidth = bounds.size.x;
            float halfThickness = thickness * .5f;
            Left = bounds.min.x + halfThickness;
            Right = bounds.max.x - halfThickness;
            Bottom = bounds.min.y + halfThickness;
            DangerY = bounds.max.y;
            AimY = Mathf.Lerp(bounds.min.y, bounds.max.y, aimViewport);

            float width = Mathf.Max(1f, Right - Left);
            float boardHeight = Mathf.Max(1f, DangerY - Bottom);
            float centerY = Bottom + boardHeight * .5f;
            SetTransform(floor, floorCollider, new Vector2(centerX, bounds.min.y), new Vector2(backgroundWidth, thickness));
            SetTransform(leftWall, leftWallCollider, new Vector2(bounds.min.x, bounds.center.y), new Vector2(thickness, bounds.size.y));
            SetTransform(rightWall, rightWallCollider, new Vector2(bounds.max.x, bounds.center.y), new Vector2(thickness, bounds.size.y));
            SetTransform(dangerLine, null, new Vector2(centerX, bounds.max.y), new Vector2(backgroundWidth, .06f));
            SetTransform(aimLine, null, new Vector2(aimLine.transform.position.x, (DangerY + AimY) * .5f), new Vector2(.04f, DangerY - AimY));
        }

        private static void SetTransform(SpriteRenderer renderer, BoxCollider2D boxCollider, Vector2 position, Vector2 scale)
        {
            if (renderer == null) return;
            Transform target = renderer.transform;
            target.position = new Vector3(position.x, position.y, target.position.z);
            Vector2 spriteSize = renderer != null && renderer.sprite != null
                ? renderer.sprite.bounds.size
                : Vector2.one;
            spriteSize.x = Mathf.Max(.0001f, spriteSize.x);
            spriteSize.y = Mathf.Max(.0001f, spriteSize.y);
            target.localScale = new Vector3(scale.x / spriteSize.x, scale.y / spriteSize.y, 1f);

            if (boxCollider != null) boxCollider.size = spriteSize;
        }

        private static BoxCollider2D GetCollider(SpriteRenderer renderer) =>
            renderer != null ? renderer.GetComponent<BoxCollider2D>() : null;
    }
}
