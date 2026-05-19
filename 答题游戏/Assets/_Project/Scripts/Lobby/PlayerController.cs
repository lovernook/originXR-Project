using UnityEngine;

namespace OriginXR.Lobby
{
    /// <summary>
    /// 玩家移动控制器（主城大厅场景）
    /// 负责：
    /// 1. 处理玩家在 LobbyScene 中的第三人称移动
    /// 2. 支持摇杆（移动端）和 WASD（PC端）输入
    /// 3. 触摸拖动旋转视角
    /// 4. 播放对应的行走/待机/跑步动画
    /// 5. 将移动位置同步至服务端（WebSocket lobby:player_move 事件）
    ///
    /// 输入映射：
    ///   W/A/S/D 或 左摇杆 -> 移动方向
    ///   鼠标右键拖动 或 触摸滑动 -> 视角旋转
    ///   左Shift 或 长按摇杆外圈 -> 加速跑
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        // === 移动参数 ===
        [SerializeField] private float _walkSpeed = 5f;       // 行走速度
        [SerializeField] private float _runSpeed = 10f;       // 跑步速度
        [SerializeField] private float _rotationSpeed = 120f; // 旋转速度
        [SerializeField] private float _jumpHeight = 2f;      // 跳跃高度（预留）
        [SerializeField] private float _gravity = -9.81f;     // 重力

        // === 组件引用 ===
        private CharacterController _characterController;
        private Animator _animator;
        private Transform _cameraTransform;

        // === 状态 ===
        private Vector3 _moveDirection;
        private Vector3 _velocity;
        private bool _isRunning;
        private float _currentSpeed;

        // === Unity 生命周期 ===
        private void Awake() { }
        private void Start() { }
        private void Update() { }
        private void FixedUpdate() { }
        private void LateUpdate() { }

        // === 公共方法 ===

        /// <summary>获取当前移动速度（用于动画混合）</summary>
        public float GetCurrentSpeed() { return _currentSpeed; }

        /// <summary>获取是否在移动</summary>
        public bool IsMoving() { return _moveDirection.magnitude > 0.1f; }

        /// <summary>启用/禁用玩家输入控制</summary>
        public void SetInputEnabled(bool enabled) { }

        /// <summary>传送到指定位置（用于快速移动到建筑入口）</summary>
        public void Teleport(Vector3 position) { }

        /// <summary>播放表情动作</summary>
        /// <param name="emoteId">表情动作ID（wave/dance/clap/sit）</param>
        public void PlayEmote(string emoteId) { }

        // === 私有方法 ===
        private void HandleMovementInput() { }
        private void HandleRotationInput() { }
        private void ApplyGravity() { }
        private void UpdateAnimation() { }
        private void SyncPositionToServer() { }
    }
}
