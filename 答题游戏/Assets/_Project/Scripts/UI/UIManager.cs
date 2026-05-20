using UnityEngine;
using System;
using System.Collections.Generic;

namespace OriginXR.UI
{
    /// <summary>
    /// UI 总控管理器（单例，栈式面板管理）
    /// 负责：
    /// 1. 统一管理所有 UI 面板的生命周期（注册/打开/关闭）
    /// 2. 栈式管理面板层级，支持返回上一级
    /// 3. 处理 ESC/Android 返回键的返回逻辑
    /// 4. 阻止底层输入遮罩
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("UI 根节点")]
        [SerializeField] private Canvas _mainCanvas;
        [SerializeField] private Transform _panelRoot;
        [SerializeField] private Transform _popupRoot;
        [SerializeField] private Transform _toastRoot;
        [SerializeField] private GameObject _blockInputMask;      // 面板打开时阻止底层点击

        // === 单例 ===
        public static UIManager Instance { get; private set; }

        // === 内部状态 ===
        private Stack<UIPanelEntry> _panelStack = new Stack<UIPanelEntry>();
        private Dictionary<string, GameObject> _panelPrefabs = new Dictionary<string, GameObject>();
        private Dictionary<string, GameObject> _activePanels = new Dictionary<string, GameObject>();

        // === 事件 ===
        public event Action<string> OnPanelOpened;
        public event Action<string> OnPanelClosed;

        // === 内部类 ===
        private class UIPanelEntry
        {
            public string panelName;
            public object data;
        }

        // === Unity 生命周期 ===

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (_blockInputMask != null)
                _blockInputMask.SetActive(false);
        }

        private void Update()
        {
            HandleBackKey();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // === 公共方法 ===

        /// <summary>注册面板预制体</summary>
        public void RegisterPanel(string panelName, GameObject prefab)
        {
            if (string.IsNullOrEmpty(panelName) || prefab == null) return;
            _panelPrefabs[panelName] = prefab;
        }

        /// <summary>打开面板（推入栈顶），可传递数据</summary>
        public void ShowPanel(string panelName, object data = null)
        {
            if (string.IsNullOrEmpty(panelName)) return;
            if (_panelRoot == null) { Debug.LogError("[UIManager] PanelRoot 未配置"); return; }

            // 如果面板已在栈顶，不重复打开
            if (_panelStack.Count > 0 && _panelStack.Peek().panelName == panelName)
                return;

            // 实例化面板
            GameObject panelObj = GetOrCreatePanelInstance(panelName);
            if (panelObj == null) return;

            // 激活面板
            panelObj.SetActive(true);
            panelObj.transform.SetAsLastSibling();

            // 推送数据（如果面板实现了 IDataReceiver）
            if (data != null)
            {
                var receivers = panelObj.GetComponents<IDataReceiver>();
                foreach (var receiver in receivers)
                    receiver.ReceiveData(data);
            }

            // 推入栈
            _panelStack.Push(new UIPanelEntry { panelName = panelName, data = data });

            // 更新遮罩
            UpdateBlockMask();

            OnPanelOpened?.Invoke(panelName);
            Debug.Log($"[UIManager] 面板已打开: {panelName} (栈深度: {_panelStack.Count})");
        }

        /// <summary>关闭当前栈顶面板</summary>
        public void HidePanel()
        {
            if (_panelStack.Count == 0) return;

            UIPanelEntry entry = _panelStack.Pop();
            if (_activePanels.TryGetValue(entry.panelName, out var panelObj))
            {
                panelObj.SetActive(false);
            }

            UpdateBlockMask();
            OnPanelClosed?.Invoke(entry.panelName);
            Debug.Log($"[UIManager] 面板已关闭: {entry.panelName} (栈深度: {_panelStack.Count})");
        }

        /// <summary>关闭指定面板</summary>
        public void HidePanel(string panelName)
        {
            if (_activePanels.TryGetValue(panelName, out var panelObj))
            {
                panelObj.SetActive(false);
            }

            // 从栈中移除
            var tempStack = new Stack<UIPanelEntry>();
            while (_panelStack.Count > 0)
            {
                var entry = _panelStack.Pop();
                if (entry.panelName != panelName)
                    tempStack.Push(entry);
            }
            while (tempStack.Count > 0)
                _panelStack.Push(tempStack.Pop());

            UpdateBlockMask();
            OnPanelClosed?.Invoke(panelName);
        }

        /// <summary>关闭所有面板</summary>
        public void HideAllPanels()
        {
            while (_panelStack.Count > 0)
                HidePanel();
        }

        /// <summary>获取当前栈顶面板名称</summary>
        public string GetCurrentPanelName()
        {
            return _panelStack.Count > 0 ? _panelStack.Peek().panelName : null;
        }

        /// <summary>判断面板是否打开</summary>
        public bool IsPanelOpen(string panelName)
        {
            if (_activePanels.TryGetValue(panelName, out var panelObj))
                return panelObj.activeSelf;
            return false;
        }

        /// <summary>获取面板实例</summary>
        public GameObject GetPanel(string panelName)
        {
            _activePanels.TryGetValue(panelName, out var panel);
            return panel;
        }

        // === 私有方法 ===

        private GameObject GetOrCreatePanelInstance(string panelName)
        {
            if (_activePanels.TryGetValue(panelName, out var existing))
                return existing;

            if (!_panelPrefabs.TryGetValue(panelName, out var prefab))
            {
                Debug.LogWarning($"[UIManager] 面板预制体未注册: {panelName}");
                return null;
            }

            GameObject instance = Instantiate(prefab, _panelRoot);
            instance.name = panelName;
            _activePanels[panelName] = instance;
            return instance;
        }

        private void UpdateBlockMask()
        {
            if (_blockInputMask != null)
                _blockInputMask.SetActive(_panelStack.Count > 0);
        }

        private void HandleBackKey()
        {
            if (_panelStack.Count == 0) return;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                HidePanel();
            }
        }
    }

    /// <summary>
    /// 数据接收接口
    /// 面板实现此接口后可通过 ShowPanel(name, data) 接收数据
    /// </summary>
    public interface IDataReceiver
    {
        void ReceiveData(object data);
    }
}
