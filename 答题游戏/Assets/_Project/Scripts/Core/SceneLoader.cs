using UnityEngine;
using System;
using System.Collections;

namespace OriginXR.Core
{
    /// <summary>
    /// 场景加载管理器
    /// 负责：
    /// 1. 基于 Addressables（或 SceneManager）异步加载/卸载场景
    /// 2. 显示加载进度条与加载过渡动画
    /// 3. 场景切换时的资源预加载与旧资源释放
    /// 4. 覆盖7个场景：Splash / Login / Lobby / Battle / KnowledgeVisualization / Guild / Achievement
    ///
    /// 场景名称常量：
    ///   SplashScene, LoginScene, LobbyScene, BattleScene,
    ///   KnowledgeVisualizationScene, GuildScene, AchievementScene
    /// </summary>
    public class SceneLoader : MonoBehaviour
    {
        // === 单例 ===
        public static SceneLoader Instance { get; private set; }

        // === 属性 ===
        /// <summary>当前加载进度（0~1）</summary>
        public float LoadingProgress { get; private set; }

        /// <summary>是否正在加载场景</summary>
        public bool IsLoading { get; private set; }

        /// <summary>当前场景名称</summary>
        public string CurrentSceneName { get; private set; }

        // === Unity 生命周期 ===
        private void Awake() { }

        // === 公共方法 ===

        /// <summary>加载指定场景（会先显示加载界面）</summary>
        /// <param name="sceneName">场景名称</param>
        /// <param name="onComplete">加载完成回调</param>
        public void LoadScene(string sceneName, Action onComplete = null) { }

        /// <summary>异步加载场景协程，分阶段更新进度</summary>
        /// <param name="sceneName">场景名称</param>
        /// <param name="onComplete">加载完成回调</param>
        public IEnumerator LoadSceneAsync(string sceneName, Action onComplete = null) { yield return null; }

        /// <summary>卸载指定场景（非当前场景）</summary>
        public void UnloadScene(string sceneName) { }

        /// <summary>显示加载过渡界面（渐变遮罩 + 旋转Logo + 进度条）</summary>
        public void ShowLoadingScreen() { }

        /// <summary>隐藏加载过渡界面</summary>
        public void HideLoadingScreen() { }

        /// <summary>更新加载进度（由 LoadSceneAsync 内部调用）</summary>
        public void UpdateProgress(float progress) { LoadingProgress = progress; }

        // === 事件 ===
        /// <summary>加载进度变化事件</summary>
        public event Action<float> OnLoadingProgressChanged;

        /// <summary>场景加载完成事件</summary>
        public event Action<string> OnSceneLoaded;

        /// <summary>场景卸载完成事件</summary>
        public event Action<string> OnSceneUnloaded;
    }
}
