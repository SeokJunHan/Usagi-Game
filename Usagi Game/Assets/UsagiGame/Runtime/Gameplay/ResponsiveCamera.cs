using UnityEngine;

namespace UsagiGame
{
    [RequireComponent(typeof(Camera))]
    public sealed class ResponsiveCamera : MonoBehaviour
    {
        [SerializeField, Min(1f), Tooltip("세로로 반드시 보여야 하는 월드 절반 높이입니다.")] private float minimumHalfHeight = 7.2f;
        [SerializeField, Min(1f), Tooltip("가로로 반드시 보여야 하는 월드 절반 너비입니다.")] private float minimumHalfWidth = 4.6f;

        private Camera cachedCamera;
        private int lastWidth;
        private int lastHeight;

        private void Awake()
        {
            cachedCamera = GetComponent<Camera>();
            Resize();
        }

        private void Update()
        {
            if (lastWidth != Screen.width || lastHeight != Screen.height) Resize();
        }

        private void Resize()
        {
            lastWidth = Screen.width;
            lastHeight = Screen.height;
            float aspect = Screen.height > 0 ? (float)Screen.width / Screen.height : 9f / 16f;
            cachedCamera.orthographicSize = Mathf.Max(minimumHalfHeight, minimumHalfWidth / aspect);
        }
    }
}
