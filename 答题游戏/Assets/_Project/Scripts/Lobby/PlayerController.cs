using UnityEngine;

namespace OriginXR.Lobby
{
    /// <summary>
    /// 玩家移动控制器（2D 俯视角 / 横版）
    /// 负责：
    /// 1. 键盘 WASD / 摇杆移动输入
    /// 2. Rigidbody2D 物理移动
    /// 3. 面向方向自动翻转 Sprite
    /// 4. Animator 动画控制（Idle / Walk）
    /// 5. 位置同步至服务端（降频200ms）
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("移动参数")]
        [SerializeField] private float _walkSpeed = 5f;
        [SerializeField] private float _runSpeed = 8f;

        [Header("输入")]
        [SerializeField] private bool _useMobileInput;
        [SerializeField] private MobileJoystick _moveJoystick;        // 移动摇杆（移动端）

        [Header("同步")]
        [SerializeField] private float _syncInterval = 0.2f;

        // === 组件引用 ===
        private Rigidbody2D _rigidbody2D;
        private Animator _animator;
        private SpriteRenderer _spriteRenderer;

        // === 状态 ===
        private Vector2 _moveDirection;
        private float _currentSpeed;
        private float _targetSpeed;
        private bool _inputEnabled = true;
        private float _lastSyncTime;

        // === Animator 参数哈希 ===
        private static readonly int ParamSpeed = Animator.StringToHash("Speed");
        private static readonly int ParamIsMoving = Animator.StringToHash("IsMoving");
        private static readonly int ParamHorizontal = Animator.StringToHash("Horizontal");
        private static readonly int ParamVertical = Animator.StringToHash("Vertical");

        // === 属性 ===
        public Vector2 MoveDirection => _moveDirection;
        public float CurrentSpeed => _currentSpeed;

        // === Unity 生命周期 ===

        private void Awake()
        {
            _rigidbody2D = GetComponent<Rigidbody2D>();
            _animator = GetComponentInChildren<Animator>();
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            // Rigidbody2D 配置
            _rigidbody2D.gravityScale = 0f;          // 俯视角无重力
            _rigidbody2D.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        private void Update()
        {
            if (!_inputEnabled) return;
            HandleMovementInput();
            UpdateAnimation();
        }

        private void FixedUpdate()
        {
            MoveInFixedUpdate();
        }

        // === 公共方法 ===

        public bool IsMoving() => _moveDirection.magnitude > 0.1f;

        public void SetInputEnabled(bool enabled)
        {
            _inputEnabled = enabled;
            if (!enabled)
            {
                _moveDirection = Vector2.zero;
                _targetSpeed = 0f;
            }
        }

        public void Teleport(Vector2 position)
        {
            _rigidbody2D.position = position;
            _rigidbody2D.velocity = Vector2.zero;
        }

        public void PlayEmote(string emoteId)
        {
            _animator?.SetTrigger(emoteId);
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

            _moveDirection = new Vector2(horizontal, vertical).normalized;
            float inputMagnitude = Mathf.Clamp01(new Vector2(horizontal, vertical).magnitude);
            _moveDirection *= inputMagnitude;

            _targetSpeed = _moveDirection.magnitude > 0.1f
                ? (isRunning ? _runSpeed : _walkSpeed)
                : 0f;

            // 面向方向翻转
            if (_spriteRenderer != null && Mathf.Abs(horizontal) > 0.1f)
            {
                _spriteRenderer.flipX = horizontal < 0;
            }

            // 位置同步
            SyncPositionToServer();
        }

        private void MoveInFixedUpdate()
        {
            _currentSpeed = _targetSpeed;
            _rigidbody2D.velocity = _moveDirection * _currentSpeed;
        }

        private void UpdateAnimation()
        {
            if (_animator == null) return;

            float animSpeed = Mathf.Lerp(
                _animator.GetFloat(ParamSpeed),
                _currentSpeed / _runSpeed,
                Time.deltaTime * 10f);

            _animator.SetFloat(ParamSpeed, animSpeed);
            _animator.SetBool(ParamIsMoving, IsMoving());
            _animator.SetFloat(ParamHorizontal, _moveDirection.x);
            _animator.SetFloat(ParamVertical, _moveDirection.y);
        }

        private void SyncPositionToServer()
        {
            if (Time.time - _lastSyncTime < _syncInterval) return;
            if (!IsMoving()) return;
            _lastSyncTime = Time.time;

            Core.NetworkManager networkManager = Core.NetworkManager.Instance;
            if (networkManager == null || !networkManager.IsConnected) return;

            Vector2 pos = _rigidbody2D.position;
            string jsonData = $"{{\"position\":{{\"x\":{pos.x:F2},\"y\":{pos.y:F2}}},\"rotation\":{{\"y\":0}}}}";
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

        public float GetHorizontal() => _inputVector.x;
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
