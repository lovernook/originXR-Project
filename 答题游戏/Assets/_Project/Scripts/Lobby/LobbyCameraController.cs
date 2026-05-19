using UnityEngine;
using Cinemachine;

namespace OriginXR.Lobby
{
    /// <summary>
    /// 主城相机控制器
    /// 负责：
    /// 1. 控制 LobbyScene 中的第三人称跟随相机
    /// 2. 基于 Cinemachine FreeLook 虚拟相机实现
    /// 3. 支持鼠标滚轮缩放、触摸双指缩放
    /// 4. 靠近建筑时自动调整相机距离（避免穿墙）
    /// 5. 对话/UI 交互时切换到固定视角
    ///
    /// 相机模式：
    ///   Follow  - 默认第三人称跟随
    ///   Focus   - 聚焦建筑入口/NPC
    ///   Free    - 自由观察模式
    /// </summary>
    public class LobbyCameraController : MonoBehaviour
    {
        // === 相机组件 ===
        [SerializeField] private CinemachineFreeLook _freeLookCamera;
        [SerializeField] private Camera _mainCamera;

        // === 跟随目标 ===
        [SerializeField] private Transform _followTarget;

        // === 缩放参数 ===
        [SerializeField] private float _minZoomDistance = 3f;   // 最小相机距离
        [SerializeField] private float _maxZoomDistance = 10f;  // 最大相机距离
        [SerializeField] private float _zoomSensitivity = 2f;   // 缩放灵敏度
        [SerializeField] private float _zoomSmoothTime = 0.2f;  // 缩放平滑时间

        // === 碰撞检测 ===
        [SerializeField] private LayerMask _obstacleLayers;      // 障碍物层
        [SerializeField] private float _cameraCollisionRadius = 0.3f; // 碰撞检测半径

        // === 状态 ===
        private float _currentZoom;           // 当前缩放值
        private float _targetZoom;            // 目标缩放值
        private float _zoomVelocity;          // 缩放平滑速度
        private CameraMode _currentMode;      // 当前相机模式
        private Vector3 _focusPosition;       // 聚焦位置

        public enum CameraMode { Follow, Focus, Free }

        // === Unity 生命周期 ===
        private void Awake() { }
        private void Start() { }
        private void LateUpdate() { }

        // === 公共方法 ===

        /// <summary>设置相机跟随目标</summary>
        public void SetFollowTarget(Transform target) { _followTarget = target; }

        /// <summary>切换相机模式</summary>
        public void SwitchMode(CameraMode mode) { _currentMode = mode; }

        /// <summary>聚焦到指定世界坐标位置</summary>
        public void FocusOn(Vector3 worldPosition, float duration = 0.5f) { }

        /// <summary>重置为默认跟随视角</summary>
        public void ResetToDefault() { }

        /// <summary>设置触摸输入是否启用</summary>
        public void SetTouchEnabled(bool enabled) { }

        // === 私有方法 ===
        private void HandleZoomInput() { }
        private void HandleCollisionAvoidance() { }
        private void UpdateFreeLookSettings() { }
        private void UpdateFocusMode() { }
    }
}
