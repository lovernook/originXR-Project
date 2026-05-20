using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System;
using System.Collections;
using TMPro;

namespace OriginXR.Core
{
    /// <summary>
    /// 场景加载管理器（单例）
    /// 负责：
    /// 1. 基于 Unity SceneManager 异步加载/卸载场景
    /// 2. 显示加载过渡界面（遮罩 + 进度条 + 旋转Logo + 提示文字）
    /// 3. 加载进度分阶段：场景加载(0~0.7) -> 资源预热(0.7~0.9) -> 过渡动画(0.9~1.0)
    /// 4. 加载完成后触发场景初始化事件
    ///
    /// 场景名称常量（与 Build Settings 中保持一致）：
    ///   SplashScene / LoginScene / LobbyScene / BattleScene
    ///   KnowledgeVisualizationScene / GuildScene / AchievementScene
    /// </summary>
    public class SceneLoader : MonoBehaviour
    {
        [Header("加载界面")]
        [SerializeField] private Canvas _loadingCanvas;
        [SerializeField] private Image _loadingBackground;
        [SerializeField] private Slider _progressSlider;
        [SerializeField] private TextMeshProUGUI _progressText;
        [SerializeField] private TextMeshProUGUI _tipText;               // 随机提示文字
        [SerializeField] private RectTransform _spinnerTransform;        // 旋转Logo

        [Header("提示文字库")]
        [SerializeField] private string[] _loadingTips = new string[]
        {
            "知识就是力量！",
            "温故而知新，可以为师矣。",
            "每天进步一点点~",
            "学而不思则罔，思而不学则殆。",
            "正在准备题目...",
            "正在召唤BOSS..."
        };

        [Header("参数")]
        [SerializeField] private float _minLoadTime = 1.5f;              // 最小加载时间（避免闪烁）
        [SerializeField] private float _spinnerSpeed = 360f;             // Logo 旋转速度

        // === 单例 ===
        public static SceneLoader Instance { get; private set; }

        // === 属性 ===
        public float LoadingProgress { get; private set; }
        public bool IsLoading { get; private set; }
        public string CurrentSceneName { get; private set; }

        // === 事件 ===
        public event Action<float> OnLoadingProgressChanged;
        public event Action<string> OnSceneLoaded;
        public event Action<string> OnSceneUnloaded;

        // === 内部状态 ===
        private AsyncOperation _currentAsyncOperation;

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

            // 初始隐藏加载界面
            if (_loadingCanvas != null)
                _loadingCanvas.gameObject.SetActive(false);
        }

        private void Update()
        {
            // 加载Logo旋转动画
            if (IsLoading && _spinnerTransform != null)
            {
                _spinnerTransform.Rotate(0f, 0f, -_spinnerSpeed * Time.unscaledDeltaTime);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // === 公共方法 ===

        public void Initialize()
        {
            Debug.Log("[SceneLoader] 场景加载器已初始化");
        }

        /// <summary>
        /// 异步加载指定场景，显示加载界面
        /// </summary>
        /// <param name="sceneName">目标场景名称</param>
        /// <param name="onComplete">加载完成回调</param>
        public void LoadScene(string sceneName, Action onComplete = null)
        {
            if (IsLoading)
            {
                Debug.LogWarning($"[SceneLoader] 已在加载中，忽略重复请求: {sceneName}");
                return;
            }

            StartCoroutine(LoadSceneRoutine(sceneName, onComplete));
        }

        /// <summary>
        /// 卸载指定场景（Additive模式加载的场景）
        /// </summary>
        public void UnloadScene(string sceneName)
        {
            if (SceneManager.GetSceneByName(sceneName).isLoaded)
            {
                StartCoroutine(UnloadSceneRoutine(sceneName));
            }
        }

        /// <summary>
        /// 显示加载界面（外部调用，如手动控制加载流程）
        /// </summary>
        public void ShowLoadingScreen()
        {
            if (_loadingCanvas != null)
            {
                _loadingCanvas.gameObject.SetActive(true);

                // 随机选择提示文字
                if (_tipText != null && _loadingTips.Length > 0)
                {
                    _tipText.text = _loadingTips[UnityEngine.Random.Range(0, _loadingTips.Length)];
                }
            }

            UpdateProgress(0f);
        }

        /// <summary>
        /// 隐藏加载界面
        /// </summary>
        public void HideLoadingScreen()
        {
            if (_loadingCanvas != null)
            {
                _loadingCanvas.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 更新加载进度（0~1），刷新进度条和文字
        /// </summary>
        /// <param name="progress">进度值 0~1</param>
        public void UpdateProgress(float progress)
        {
            LoadingProgress = Mathf.Clamp01(progress);

            if (_progressSlider != null)
                _progressSlider.value = LoadingProgress;

            if (_progressText != null)
                _progressText.text = $"{(LoadingProgress * 100f):F0}%";

            OnLoadingProgressChanged?.Invoke(LoadingProgress);
        }

        // === 私有：场景加载协程 ===

        private IEnumerator LoadSceneRoutine(string sceneName, Action onComplete)
        {
            IsLoading = true;
            Debug.Log($"[SceneLoader] 开始加载场景: {sceneName}");

            // 记录开始时间
            float startTime = Time.realtimeSinceStartup;

            // 显示加载界面
            ShowLoadingScreen();
            UpdateProgress(0.05f);

            // 阶段1：异步加载场景 (0.05 ~ 0.7)
            _currentAsyncOperation = SceneManager.LoadSceneAsync(sceneName);
            if (_currentAsyncOperation != null)
            {
                _currentAsyncOperation.allowSceneActivation = false;

                while (_currentAsyncOperation.progress < 0.9f)
                {
                    float p = 0.05f + _currentAsyncOperation.progress * 0.65f;
                    UpdateProgress(p);
                    yield return null;
                }

                // 场景加载完成 (0.7)
                UpdateProgress(0.7f);
            }

            // 阶段2：资源预热 (0.7 ~ 0.9)
            yield return StartCoroutine(PreloadResources());

            UpdateProgress(0.9f);

            // 阶段3：最小加载时间保证
            float elapsed = Time.realtimeSinceStartup - startTime;
            if (elapsed < _minLoadTime)
            {
                yield return new WaitForSecondsRealtime(_minLoadTime - elapsed);
            }

            // 阶段4：激活场景
            if (_currentAsyncOperation != null)
            {
                _currentAsyncOperation.allowSceneActivation = true;
            }

            // 等待场景完全激活
            yield return new WaitUntil(() =>
                _currentAsyncOperation == null || _currentAsyncOperation.isDone);

            UpdateProgress(1f);
            CurrentSceneName = sceneName;

            // 过渡动画
            yield return new WaitForSecondsRealtime(0.3f);

            Debug.Log($"[SceneLoader] 场景加载完成: {sceneName}");

            // 隐藏加载界面
            HideLoadingScreen();
            IsLoading = false;
            _currentAsyncOperation = null;

            OnSceneLoaded?.Invoke(sceneName);
            onComplete?.Invoke();
        }

        /// <summary>
        /// 预加载资源（模拟，实际可替换为 Addressables 预加载）
        /// </summary>
        private IEnumerator PreloadResources()
        {
            // 模拟资源预加载阶段的进度
            float preloadDuration = 0.5f;
            float elapsed = 0f;

            while (elapsed < preloadDuration)
            {
                elapsed += Time.deltaTime;
                float p = 0.7f + (elapsed / preloadDuration) * 0.2f;
                UpdateProgress(p);
                yield return null;
            }
        }

        /// <summary>
        /// 卸载场景协程
        /// </summary>
        private IEnumerator UnloadSceneRoutine(string sceneName)
        {
            Debug.Log($"[SceneLoader] 正在卸载场景: {sceneName}");
            AsyncOperation asyncOp = SceneManager.UnloadSceneAsync(sceneName);

            if (asyncOp != null)
            {
                while (!asyncOp.isDone)
                    yield return null;
            }

            Debug.Log($"[SceneLoader] 场景卸载完成: {sceneName}");
            OnSceneUnloaded?.Invoke(sceneName);
        }
    }
}
