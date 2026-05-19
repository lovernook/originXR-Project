using UnityEngine;
using System;

namespace OriginXR.Core
{
    /// <summary>
    /// 游戏全局管理器
    /// 负责：
    /// 1. 游戏整体状态管理（启动/运行/暂停/退出）
    /// 2. 协调各子系统（音频/网络/UI/场景）的初始化与生命周期
    /// 3. 全局单例，提供跨场景的全局访问入口
    /// </summary>
    public enum GameState
    {
        Boot,    // 启动初始化
        Login,   // 登录界面
        Lobby,   // 主城大厅
        Battle,  // 答题战斗
        Loading   // 场景加载中
    }

    public class GameManager : MonoBehaviour
    {
        // === 单例 ===
        public static GameManager Instance { get; private set; }

        // === 属性 ===
        /// <summary>当前游戏状态</summary>
        public GameState CurrentState { get; private set; }

        /// <summary>是否已初始化完成</summary>
        public bool IsInitialized { get; private set; }

        // === Unity 生命周期 ===
        private void Awake() { }
        private void Start() { }
        private void OnDestroy() { }
        private void OnApplicationPause(bool pause) { }
        private void OnApplicationQuit() { }

        // === 公共方法 ===

        /// <summary>
        /// 初始化所有子系统
        /// 调用顺序：AudioManager -> NetworkManager -> HttpManager -> UIManager
        /// </summary>
        public void Initialize() { }

        /// <summary>切换游戏状态，触发 OnGameStateChanged 事件</summary>
        /// <param name="newState">目标状态</param>
        public void ChangeGameState(GameState newState) { }

        /// <summary>暂停游戏（Time.timeScale = 0）</summary>
        public void PauseGame() { }

        /// <summary>恢复游戏（Time.timeScale = 1）</summary>
        public void ResumeGame() { }

        /// <summary>退出游戏</summary>
        public void QuitGame() { }

        // === 私有方法 ===
        private void InitializeSubsystems() { }
        private void HandleGameStateChanged(GameState state) { }

        // === 事件 ===
        /// <summary>游戏状态变更事件</summary>
        public event Action<GameState> OnGameStateChanged;
    }
}
