using UnityEngine;
using System;
using System.Collections;

namespace OriginXR.Lobby
{
    /// <summary>
    /// 相机模式
    /// </summary>
    public enum CameraMode
    {
        Follow,     // 默认第三人称跟随
        Focus,      // 聚焦建筑入口 / NPC
        Free        // 自由观察
    }

    /// <summary>
    /// 主城相机控制器
    /// 负责：
    /// 1. 第三人称跟随相机（鼠标右键拖拽旋转、滚轮缩放、触摸双指缩放）
    /// 2. 障碍物碰撞检测与相机自动移动
    /// 3. 聚焦模式（平滑移动到目标位置）
    /// 4. 相机旋转上下限控制
    /// </summary>
    public class LobbyCameraController : MonoBehaviour
    {
        [Header("跟随目标")]
        [SerializeField] private Transform _followTarget;
        [SerializeField] private Vector3 _followOffset = new Vector3(0f, 2.5f, -5f);  // 默认偏移

        [Header("旋转")]
        [SerializeField] private float _mouseSensitivity = 2f;
        [SerializeField] private float _touchSensitivity = 0.5f;
        [SerializeField] private float _minPitch = -30f;        // 最低俯角
        [SerializeField] private float _maxPitch = 60f;         // 最高仰角
        [SerializeField] private float _rotationSmoothTime = 0.12f;

        [Header("缩放")]
        [SerializeField] private float _minZoom = 2f;
        [SerializeField] private float _maxZoom = 12f;
        [SerializeField] private float _zoomSpeed = 5f;
        [SerializeField] private float _zoomSmoothTime = 0.2f;
        [SerializeField] private float _mobileZoomSpeed = 0.01f;

        [Header("碰撞避让")]
        [SerializeField] private LayerMask _obstacleLayer;
        [SerializeField] private float _collisionRadius = 0.3f;
        [SerializeField] private float _collisionOffset = 0.3f;     // 碰撞后靠近墙壁的距离
        [SerializeField] private float _collisionSmoothTime = 0.15f;

        [Header("聚焦")]
        [SerializeField] private float _focusTransitionTime = 0.5f;  // 聚焦过渡时间

        // === 组件 ===
        private Camera _camera;
        private Transform _cameraTransform;

        // === 旋转状态 ===
        private float _yaw;              // 水平角（绕Y轴）
        private float _pitch;            // 俯仰角（绕X轴）
        private float _yawVelocity;
        private float _pitchVelocity;
        private float _targetYaw;
        private float _targetPitch;

        // === 缩放状态 ===
        private float _currentZoom;
        private float _targetZoom;
        private float _zoomVelocity;

        // === 碰撞 ===
        private float _collisionZoomAdjust;
        private float _collisionZoomVelocity;

        // === 模式状态 ===
        private CameraMode _currentMode = CameraMode.Follow;
        private Vector3 _focusPosition;
        private Quaternion _focusRotation;
        private Coroutine _focusCoroutine;
        private bool _touchEnabled = true;

        // === 属性 ===
        public CameraMode CurrentMode => _currentMode;

        // === Unity 生命周期 ===

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            if (_camera == null) _camera = GetComponentInChildren<Camera>();
            _cameraTransform = _camera != null ? _camera.transform : transform;
        }

        private void Start()
        {
            if (_followTarget != null)
            {
                Vector3 angles = transform.eulerAngles;
                _yaw = angles.y;
                _pitch = angles.x > 180f ? angles.x - 360f : angles.x;
            }
            else
            {
                _yaw = transform.eulerAngles.y;
                _pitch = 20f;
            }

            _targetYaw = _yaw;
            _targetPitch = _pitch;
            _currentZoom = _followOffset.magnitude;
            _targetZoom = _currentZoom;
        }

        private void LateUpdate()
        {
            if (_currentMode == CameraMode.Follow && _followTarget != null)
            {
                HandleRotationInput();
                HandleZoomInput();
            }

            HandleCollisionAvoidance();
            SmoothUpdateCamera();
        }

        // === 公共方法 ===

        /// <summary>设置跟随目标</summary>
        public void SetFollowTarget(Transform target)
        {
            _followTarget = target;
            if (_currentMode == CameraMode.Free)
                _currentMode = CameraMode.Follow;
        }

        /// <summary>切换相机模式</summary>
        public void SwitchMode(CameraMode mode)
        {
            if (_currentMode == mode) return;
            _currentMode = mode;
        }

        /// <summary>聚焦到指定世界坐标（平滑过渡）</summary>
        public void FocusOn(Vector3 worldPosition, float duration = -1f)
        {
            if (duration < 0f) duration = _focusTransitionTime;

            if (_focusCoroutine != null)
                StopCoroutine(_focusCoroutine);

            _currentMode = CameraMode.Focus;
            _focusCoroutine = StartCoroutine(FocusToPositionRoutine(worldPosition, duration));
        }

        /// <summary>重置为默认跟随视角</summary>
        public void ResetToDefault()
        {
            _currentMode = CameraMode.Follow;
            _targetYaw = _followTarget != null ? _followTarget.eulerAngles.y : 0f;
            _targetPitch = 20f;
            _targetZoom = _followOffset.magnitude;

            if (_focusCoroutine != null)
            {
                StopCoroutine(_focusCoroutine);
                _focusCoroutine = null;
            }
        }

        /// <summary>设置触摸输入开关</summary>
        public void SetTouchEnabled(bool enabled) => _touchEnabled = enabled;

        // === 私有：输入处理 ===

        private void HandleRotationInput()
        {
            // PC 鼠标右键旋转
            if (Input.GetMouseButton(1))
            {
                _targetYaw += Input.GetAxis("Mouse X") * _mouseSensitivity;
                _targetPitch -= Input.GetAxis("Mouse Y") * _mouseSensitivity;
                _targetPitch = Mathf.Clamp(_targetPitch, _minPitch, _maxPitch);
            }

            // 移动端触摸滑动旋转（单指滑动）
            if (_touchEnabled && Input.touchCount == 1)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Moved)
                {
                    _targetYaw += touch.deltaPosition.x * _touchSensitivity;
                    _targetPitch -= touch.deltaPosition.y * _touchSensitivity;
                    _targetPitch = Mathf.Clamp(_targetPitch, _minPitch, _maxPitch);
                }
            }
        }

        private void HandleZoomInput()
        {
            // PC 鼠标滚轮缩放
            float scrollInput = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scrollInput) > 0.001f)
            {
                _targetZoom -= scrollInput * _zoomSpeed;
                _targetZoom = Mathf.Clamp(_targetZoom, _minZoom, _maxZoom);
            }

            // 移动端双指缩放
            if (_touchEnabled && Input.touchCount == 2)
            {
                Touch t0 = Input.GetTouch(0);
                Touch t1 = Input.GetTouch(1);

                Vector2 prevPos0 = t0.position - t0.deltaPosition;
                Vector2 prevPos1 = t1.position - t1.deltaPosition;

                float prevDist = (prevPos0 - prevPos1).magnitude;
                float currDist = (t0.position - t1.position).magnitude;
                float delta = (currDist - prevDist) * _mobileZoomSpeed;

                _targetZoom -= delta;
                _targetZoom = Mathf.Clamp(_targetZoom, _minZoom, _maxZoom);
            }
        }

        private void HandleCollisionAvoidance()
        {
            if (_followTarget == null) return;

            // 从目标位置向相机期望位置射线检测
            Vector3 desiredPosition = CalculateDesiredPosition();
            Vector3 direction = (desiredPosition - _followTarget.position).normalized;
            float distance = Vector3.Distance(_followTarget.position, desiredPosition) + _collisionOffset;

            RaycastHit hit;
            if (Physics.SphereCast(_followTarget.position + Vector3.up * 0.5f, _collisionRadius, direction, out hit, distance, _obstacleLayer))
            {
                // 有障碍物，缩短相机距离
                _collisionZoomAdjust = Mathf.SmoothDamp(_collisionZoomAdjust, hit.distance - _collisionOffset, ref _collisionZoomVelocity, _collisionSmoothTime);
            }
            else
            {
                _collisionZoomAdjust = Mathf.SmoothDamp(_collisionZoomAdjust, 0f, ref _collisionZoomVelocity, _collisionSmoothTime);
            }
        }

        /// <summary>计算期望的相机世界位置</summary>
        private Vector3 CalculateDesiredPosition()
        {
            Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            return _followTarget.position + Vector3.up * _followOffset.y + rotation * Vector3.back * _targetZoom;
        }

        private void SmoothUpdateCamera()
        {
            // 平滑角度
            _yaw = Mathf.SmoothDampAngle(_yaw, _targetYaw, ref _yawVelocity, _rotationSmoothTime);
            _pitch = Mathf.SmoothDampAngle(_pitch, _targetPitch, ref _pitchVelocity, _rotationSmoothTime);
            _currentZoom = Mathf.SmoothDamp(_currentZoom, _targetZoom - _collisionZoomAdjust, ref _zoomVelocity, _zoomSmoothTime);

            if (_currentMode == CameraMode.Follow && _followTarget != null)
            {
                Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
                Vector3 targetPos = _followTarget.position + Vector3.up * _followOffset.y + rotation * Vector3.back * _currentZoom;
                _cameraTransform.position = targetPos;
                _cameraTransform.LookAt(_followTarget.position + Vector3.up * 1.5f);
            }
            else if (_currentMode == CameraMode.Focus)
            {
                _cameraTransform.LookAt(_focusPosition);
            }
        }

        /// <summary>聚焦到目标位置的过渡协程</summary>
        private IEnumerator FocusToPositionRoutine(Vector3 targetPosition, float duration)
        {
            Vector3 startPos = _cameraTransform.position;
            Quaternion startRot = _cameraTransform.rotation;
            Vector3 endPos = targetPosition + Vector3.back * 5f + Vector3.up * 3f;
            _focusPosition = targetPosition;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                _cameraTransform.position = Vector3.Lerp(startPos, endPos, t);
                _cameraTransform.LookAt(targetPosition);
                yield return null;
            }

            _focusCoroutine = null;
        }
    }
}
