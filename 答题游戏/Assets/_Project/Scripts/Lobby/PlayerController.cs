using UnityEngine;
using System;

namespace OriginXR.Lobby
{
    /// <summary>
    /// 玩家移动控制器（第三人称）
    /// 负责：
    /// 1. 键盘 WASD / 摇杆移动输入
    /// 2. 鼠标右键 / 触摸滑动旋转视角
    /// 3. CharacterController 物理移动 + 重力
    /// 4. Animator 动画控制（Idle / Walk / Run）
    /// 5. 位置同步至服务端（降频200ms）
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("移动参数")]
        [SerializeField] private float _walkSpeed = 5f;
        [SerializeField] private float _runSpeed = 10f;
        [SerializeField] private float _rotationSmoothTime = 0.1f;
        [SerializeField] private float _gravity = -9.81f;
        [SerializeField] private float _groundCheckRadius = 0.2f;
        [SerializeField] private LayerMask _groundLayer = -1;

        [Header("输入")]
        [SerializeField] private bool _useMobileInput;               // 是否使用移动端输入
        [SerializeField] private MobileJoystick _moveJoystick;        // 移动摇杆（移动端，挂载 MobileJoystick 组件）

        [Header("同步")]
        [SerializeField] private float _syncInterval = 0.2f;        // 位置同步间隔（秒）

        // === 组件引用 ===
        private CharacterController _characterController;
        private Animator _animator;
        private Camera _mainCamera;

        // === 状态 ===
        private Vector3 _moveDirection;
        private Vector3 _velocity;
        private float _currentSpeed;
        private float _targetSpeed;
        private float _rotationVelocity;
        private bool _isGrounded;
        private float _lastSyncTime;
        private bool _inputEnabled = true;

        // === Animator 参数哈希 ===
        private static readonly int ParamSpeed = Animator.StringToHash("Speed");
        private static readonly int ParamIsMoving = Animator.StringToHash("IsMoving");
        private static readonly int ParamJump = Animator.StringToHash("Jump");

        // === 属性 ===
        public Vector3 MoveDirection => _moveDirection;
        public float CurrentSpeed => _currentSpeed;

        // === Unity 生命周期 ===

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            _animator = GetComponentInChildren<Animator>();
            _mainCamera = Camera.main;
        }

        private void Start()
        {
            if (_mainCamera == null)
                _mainCamera = Camera.main;
        }

        private void Update()
        {
            if (!_inputEnabled) return;

            HandleMovementInput();
            ApplyGravity();
            MoveCharacter();
            UpdateAnimation();
            SyncPositionToServer();
        }

        private void OnDrawGizmosSelected()
        {
            // 绘制地面检测范围
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.1f, _groundCheckRadius);
        }

        // === 公共方法 ===

        /// <summary>获取是否在移动</summary>
        public bool IsMoving() => _moveDirection.magnitude > 0.1f;

        /// <summary>启用/禁用输入控制</summary>
        public void SetInputEnabled(bool enabled)
        {
            _inputEnabled = enabled;
            if (!enabled)
            {
                _moveDirection = Vector3.zero;
                _targetSpeed = 0f;
            }
        }

        /// <summary>传送到指定位置</summary>
        public void Teleport(Vector3 position)
        {
            if (_characterController != null)
                _characterController.enabled = false;

            transform.position = position;

            if (_characterController != null)
                _characterController.enabled = true;

            _velocity = Vector3.zero;
        }

        /// <summary>播放表情动作</summary>
        /// <param name="emoteId">动作标识: "wave"/"dance"/"clap"/"sit"</param>
        public void PlayEmote(string emoteId)
        {
            if (_animator != null)
            {
                _animator.SetTrigger(emoteId);
            }
        }

        /// <summary>设置 Animator 控制器（切换皮肤/装备时）</summary>
        public void SetAnimatorController(RuntimeAnimatorController controller)
        {
            if (_animator != null)
                _animator.runtimeAnimatorController = controller;
        }

        // === 私有方法 ===

        private void HandleMovementInput()
        {
            float horizontal = 0f;
            float vertical = 0f;
            bool isRunning = false;

            if (_useMobileInput && _moveJoystick != null)
            {
                horizontal = _moveJoystick.GetHorizontal();
                vertical = _moveJoystick.GetVertical();
                float magnitude = Mathf.Sqrt(horizontal * horizontal + vertical * vertical);
                isRunning = magnitude > 0.8f;
            }
            else
            {
                horizontal = Input.GetAxisRaw("Horizontal");
                vertical = Input.GetAxisRaw("Vertical");
                isRunning = Input.GetKey(KeyCode.LeftShift);
            }

            // 计算移动方向（基于相机朝向）
            Vector3 forward = _mainCamera != null ? _mainCamera.transform.forward : Vector3.forward;
            Vector3 right = _mainCamera != null ? _mainCamera.transform.right : Vector3.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            _moveDirection = (forward * vertical + right * horizontal).normalized;

            // 限制对角线速度
            float inputMagnitude = Mathf.Clamp01(new Vector2(horizontal, vertical).magnitude);
            _moveDirection *= inputMagnitude;

            _targetSpeed = _moveDirection.magnitude > 0.1f
                ? (isRunning ? _runSpeed : _walkSpeed)
                : 0f;

            // 平滑旋转面向移动方向
            if (_moveDirection.magnitude > 0.1f)
            {
                float targetAngle = Mathf.Atan2(_moveDirection.x, _moveDirection.z) * Mathf.Rad2Deg;
                float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref _rotationVelocity, _rotationSmoothTime);
                transform.rotation = Quaternion.Euler(0f, angle, 0f);
            }
        }

        private void ApplyGravity()
        {
            // 地面检测
            _isGrounded = Physics.CheckSphere(
                transform.position + Vector3.up * 0.1f,
                _groundCheckRadius,
                _groundLayer);

            if (_isGrounded && _velocity.y < 0f)
            {
                _velocity.y = -2f; // 保持贴地
            }

            _velocity.y += _gravity * Time.deltaTime;
        }

        private void MoveCharacter()
        {
            _currentSpeed = _isGrounded ? _targetSpeed : _currentSpeed;

            Vector3 motion = _moveDirection * _currentSpeed + Vector3.up * _velocity.y;
            _characterController.Move(motion * Time.deltaTime);
        }

        private void UpdateAnimation()
        {
            if (_animator == null) return;

            // 平滑速度过渡
            float animSpeed = Mathf.Lerp(_animator.GetFloat(ParamSpeed), _currentSpeed / _runSpeed, Time.deltaTime * 10f);
            _animator.SetFloat(ParamSpeed, animSpeed);
            _animator.SetBool(ParamIsMoving, IsMoving());
        }

        private void SyncPositionToServer()
        {
            if (Time.time - _lastSyncTime < _syncInterval) return;
            _lastSyncTime = Time.time;

            // 仅当移动时同步
            if (!IsMoving()) return;

            Core.NetworkManager networkManager = Core.NetworkManager.Instance;
            if (networkManager == null || !networkManager.IsConnected) return;

            Vector3 pos = transform.position;
            Vector3 rot = transform.eulerAngles;
            string jsonData = $"{{\"position\":{{\"x\":{pos.x:F2},\"y\":{pos.y:F2},\"z\":{pos.z:F2}}},\"rotation\":{{\"y\":{rot.y:F2}}}}}";
            networkManager.Send("lobby:player_move", jsonData);
        }
    }

    /// <summary>
    /// 移动端虚拟摇杆组件
    /// 挂载到 UI 摇杆 GameObject 上，通过拖拽计算水平/垂直输入值
    /// </summary>
    public class MobileJoystick : MonoBehaviour
    {
        [Header("摇杆范围")]
        [SerializeField] private RectTransform _joystickBackground;
        [SerializeField] private RectTransform _joystickHandle;
        [SerializeField] private float _maxRadius = 100f;

        private Vector2 _inputVector;
        private Vector2 _startPosition;

        private void Start()
        {
            if (_joystickBackground != null)
                _startPosition = _joystickBackground.position;
        }

        /// <summary>获取水平输入值 (-1 ~ 1)</summary>
        public float GetHorizontal() => _inputVector.x;

        /// <summary>获取垂直输入值 (-1 ~ 1)</summary>
        public float GetVertical() => _inputVector.y;

        private void Update()
        {
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                    _startPosition = touch.position;
                    if (_joystickBackground != null)
                        _joystickBackground.position = _startPosition;
                }
                else if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                {
                    Vector2 direction = touch.position - _startPosition;
                    _inputVector = Vector2.ClampMagnitude(direction / _maxRadius, 1f);

                    if (_joystickHandle != null)
                        _joystickHandle.localPosition = _inputVector * _maxRadius;
                }
                else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    _inputVector = Vector2.zero;
                    if (_joystickHandle != null)
                        _joystickHandle.localPosition = Vector3.zero;
                }
            }
        }
    }
}
