using UnityEngine;

namespace OriginXR.Lobby
{
    /// <summary>
    /// 2D 相机控制器（正交跟随）
    /// 负责：
    /// 1. 平滑跟随玩家（Lerp 位置）
    /// 2. 鼠标滚轮 / 双指缩放（改变 orthographicSize）
    /// 3. 支持聚焦到指定位置（过渡动画）
    /// </summary>
    public class LobbyCameraController : MonoBehaviour
    {
        [Header("跟随目标")]
        [SerializeField] private Transform _followTarget;
        [SerializeField] private Vector3 _followOffset = new Vector3(0f, 0f, -10f);
        [SerializeField] private float _followSmoothTime = 0.15f;

        [Header("缩放")]
        [SerializeField] private float _minZoom = 2f;
        [SerializeField] private float _maxZoom = 12f;
        [SerializeField] private float _zoomSpeed = 5f;
        [SerializeField] private float _zoomSmoothTime = 0.2f;

        // === 组件 ===
        private Camera _camera;

        // === 状态 ===
        private Vector3 _velocity;
        private float _targetZoom;
        private float _zoomVelocity;
        private bool _isFocusing;
        private Vector3 _focusPosition;

        // === Unity 生命周期 ===

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            if (_camera == null)
                _camera = Camera.main;
        }

        private void Start()
        {
            if (_camera != null)
            {
                _camera.orthographic = true;
                _targetZoom = _camera.orthographicSize;
            }
        }

        private void LateUpdate()
        {
            HandleZoomInput();
            SmoothFollow();
        }

        // === 公共方法 ===

        public void SetFollowTarget(Transform target)
        {
            _followTarget = target;
        }

        public void FocusOn(Vector3 worldPosition)
        {
            _focusPosition = worldPosition;
            _isFocusing = true;
        }

        public void ResetToDefault()
        {
            _isFocusing = false;
        }

        // === 私有方法 ===

        private void SmoothFollow()
        {
            if (_followTarget == null) return;

            Vector3 targetPos = _isFocusing
                ? _focusPosition + _followOffset
                : _followTarget.position + _followOffset;

            transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref _velocity, _followSmoothTime);
        }

        private void HandleZoomInput()
        {
            if (_camera == null) return;

            // PC 鼠标滚轮
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.001f)
            {
                _targetZoom -= scroll * _zoomSpeed;
                _targetZoom = Mathf.Clamp(_targetZoom, _minZoom, _maxZoom);
            }

            // 移动端双指缩放
            if (Input.touchCount == 2)
            {
                Touch t0 = Input.GetTouch(0);
                Touch t1 = Input.GetTouch(1);
                Vector2 prevPos0 = t0.position - t0.deltaPosition;
                Vector2 prevPos1 = t1.position - t1.deltaPosition;
                float prevDist = (prevPos0 - prevPos1).magnitude;
                float currDist = (t0.position - t1.position).magnitude;

                _targetZoom -= (currDist - prevDist) * 0.01f * _zoomSpeed;
                _targetZoom = Mathf.Clamp(_targetZoom, _minZoom, _maxZoom);
            }

            // 平滑缩放
            _camera.orthographicSize = Mathf.SmoothDamp(
                _camera.orthographicSize,
                _targetZoom,
                ref _zoomVelocity,
                _zoomSmoothTime);
        }
    }
}
