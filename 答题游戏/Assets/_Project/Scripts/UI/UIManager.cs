using UnityEngine;
using System;
using System.Collections.Generic;

namespace OriginXR.UI
{
    /// <summary>
    /// UI 总控管理器（栈式管理）
    /// 负责：
    /// 1. 管理所有 UI 面板的生命周期（打开/关闭/切换）
    /// 2. 使用栈（Stack）管理面板层级，支持返回上一级面板
    /// 3. 提供统一的 ShowPanel / HidePanel / GoBack 接口
    /// 4. 管理 UI 面板之间的数据传递
    /// 5. 处理 Android 返回键 / ESC 键的返回逻辑
    ///
    /// 面板层级（从下到上）：
    ///   LobbyHUD (常驻) -> 功能面板 -> 二级面板 -> 弹窗 -> Toast
    ///
    /// 面板名称常量：
    ///   RankPanel, ShopPanel, BagPanel, AchievementPanel, SettingsPanel,
    ///   StageSelectPanel, DailyChallengePanel, KnowledgeDetailPanel,
    ///   MailPanel, ActivityPanel
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        // === 单例 ===
        public static UIManager Instance { get; private set; }

        // === UI 组件 ===
        [SerializeField] private Canvas _mainCanvas;           // 主 UI Canvas
        [SerializeField] private Transform _panelRoot;         // 面板根节点
        [SerializeField] private Transform _popupRoot;         // 弹窗根节点
        [SerializeField] private Transform _toastRoot;         // Toast 根节点
        [SerializeField] private GameObject _blockInputMask;   // 阻止点击的遮罩

        // === 运行时数据 ===
        private Stack<string> _panelStack;                     // 面板栈（面板名称）
        private Dictionary<string, GameObject> _panelInstances; // 面板名称 -> 实例
        private Dictionary<string, GameObject> _panelPrefabs;   // 面板名称 -> 预制体

        // === Unity 生命周期 ===
        private void Awake() { }
        private void Start() { }
        private void Update() { }                              // 处理 ESC/返回键

        // === 公共方法 ===

        /// <summary>注册面板预制体</summary>
        /// <param name="panelName">面板名称</param>
        /// <param name="prefab">面板预制体</param>
        public void RegisterPanel(string panelName, GameObject prefab) { }

        /// <summary>显示面板（推入栈顶）</summary>
        /// <param name="panelName">面板名称</param>
        /// <param name="data">传递的数据（可为 null）</param>
        public void ShowPanel(string panelName, object data = null) { }

        /// <summary>关闭当前面板（弹出栈顶），返回上一级</summary>
        public void HidePanel() { }

        /// <summary>关闭指定面板</summary>
        public void HidePanel(string panelName) { }

        /// <summary>返回上一级面板（不做任何操作则不处理）</summary>
        public void GoBack() { }

        /// <summary>关闭所有面板（回到主城 HUD）</summary>
        public void HideAllPanels() { }

        /// <summary>获取当前打开的面板名称</summary>
        public string GetCurrentPanelName() { return _panelStack.Count > 0 ? _panelStack.Peek() : null; }

        /// <summary>是否打开了指定面板</summary>
        public bool IsPanelOpen(string panelName) { return _panelStack.Contains(panelName); }

        /// <summary>显示/隐藏阻止输入遮罩</summary>
        public void SetBlockInput(bool block) { }

        // === 私有方法 ===
        private GameObject GetOrCreatePanelInstance(string panelName) { return null; }
        private void PushPanel(string panelName) { }
        private void PopPanel() { }
        private void HandleBackKey() { }

        // === 事件 ===
        /// <summary>面板打开事件</summary>
        public event Action<string> OnPanelOpened;

        /// <summary>面板关闭事件</summary>
        public event Action<string> OnPanelClosed;
    }
}
