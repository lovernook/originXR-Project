using UnityEngine;
using System;

namespace OriginXR.Core
{
    /// <summary>
    /// 游戏全局状态枚举
    /// </summary>
    public enum GameState
    {
        Boot,       // 启动初始化
        Login,      // 登录界面
        Lobby,      // 主城大厅
        Battle,     // 答题战斗
        Loading     // 场景加载中
    }

    /// <summary>
    /// 游戏全局管理器（单例）
    /// 负责：
    /// 1. 游戏整体状态机管理
    /// 2. 按顺序初始化各子系统
    /// 3. 暂停/恢复/退出控制
    /// 4. 跨场景持久化（DontDestroyOnLoad）
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("子系统引用")]
        [SerializeField] private AudioManager _audioManager;
        [SerializeField] private NetworkManager _networkManager;
        [SerializeField] private HttpManager _httpManager;
        [SerializeField] private SceneLoader _sceneLoader;

        // === 单例 ===
        public static GameManager Instance { get; private set; }

        // === 属性 ===
        public GameState CurrentState { get; private set; }
        public bool IsInitialized { get; private set; }
        public bool IsPaused { get; private set; }

        /// <summary>游戏启动时间戳（用于统计在线时长）</summary>
        public DateTime LaunchTimestamp { get; private set; }

        // === 事件 ===
        /// <summary>游戏状态变更事件，参数为新状态</summary>
        public event Action<GameState> OnGameStateChanged;
        /// <summary>游戏暂停事件</summary>
        public event Action OnGamePaused;
        /// <summary>游戏恢复事件</summary>
        public event Action OnGameResumed;

        // === Unity 生命周期 ===

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LaunchTimestamp = DateTime.UtcNow;
        }

        private void Start()
        {
            Initialize();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause)
                PauseGame();
            else
                ResumeGame();
        }

        private void OnApplicationQuit()
        {
            QuitGame();
        }

        // === 公共方法 ===

        /// <summary>
        /// 按依赖顺序初始化所有子系统
        /// 顺序：AudioManager(无依赖) -> HttpManager -> NetworkManager -> SceneLoader
        /// </summary>
        public void Initialize()
        {
            if (IsInitialized) return;
            Debug.Log("[GameManager] 开始初始化游戏系统...");

            // 1. 音频系统（无依赖，最先初始化）
            if (_audioManager != null)
                _audioManager.Initialize();
            else
                Debug.LogWarning("[GameManager] AudioManager 未配置");

            // 2. HTTP 管理器（不依赖网络连接）
            if (_httpManager != null)
                _httpManager.Initialize();
            else
                Debug.LogWarning("[GameManager] HttpManager 未配置");

            // 3. 网络管理器（依赖 HTTP Token）
            if (_networkManager != null)
                _networkManager.Initialize();
            else
                Debug.LogWarning("[GameManager] NetworkManager 未配置");

            // 4. 场景加载器（最后初始化）
            if (_sceneLoader != null)
                _sceneLoader.Initialize();
            else
                Debug.LogWarning("[GameManager] SceneLoader 未配置");

            IsInitialized = true;
            ChangeGameState(GameState.Boot);
            Debug.Log("[GameManager] 游戏系统初始化完成");
        }

        /// <summary>
        /// 切换游戏主状态，触发全局事件
        /// </summary>
        /// <param name="newState">目标状态</param>
        public void ChangeGameState(GameState newState)
        {
            if (CurrentState == newState) return;

            GameState oldState = CurrentState;
            CurrentState = newState;
            Debug.Log($"[GameManager] 游戏状态变更: {oldState} -> {newState}");
            OnGameStateChanged?.Invoke(newState);
        }

        /// <summary>
        /// 暂停游戏（Time.timeScale = 0，冻结物理和动画）
        /// </summary>
        public void PauseGame()
        {
            if (IsPaused) return;
            IsPaused = true;
            Time.timeScale = 0f;
            Debug.Log("[GameManager] 游戏已暂停");
            OnGamePaused?.Invoke();
        }

        /// <summary>
        /// 恢复游戏（Time.timeScale = 1）
        /// </summary>
        public void ResumeGame()
        {
            if (!IsPaused) return;
            IsPaused = false;
            Time.timeScale = 1f;
            Debug.Log("[GameManager] 游戏已恢复");
            OnGameResumed?.Invoke();
        }

        /// <summary>
        /// 安全退出游戏
        /// 处理平台差异：Editor停止播放，运行时可自定义退出逻辑
        /// </summary>
        public void QuitGame()
        {
            Debug.Log("[GameManager] 正在退出游戏...");

            // 断开网络连接
            if (_networkManager != null && _networkManager.IsConnected)
                _networkManager.Disconnect();

            // 保存音量设置
            if (_audioManager != null)
                _audioManager.SaveVolumeSettings();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        /// <summary>
        /// 获取在线时长（秒）
        /// </summary>
        public double GetOnlineSeconds()
        {
            return (DateTime.UtcNow - LaunchTimestamp).TotalSeconds;
        }
    }
}
