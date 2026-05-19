using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;

namespace OriginXR.UI
{
    /// <summary>
    /// Toast 提示管理器
    /// 负责：
    /// 1. 管理屏幕顶部的自动消失提示信息
    /// 2. 支持多种类型：Info（信息）、Success（成功）、Warning（警告）、Error（错误）
    /// 3. 支持队列机制：多条 Toast 依次显示，不重叠
    /// 4. 支持带图标的 Toast（金币/经验/体力获得提示）
    ///
    /// 动画：
    ///   入场：从顶部滑入 + 淡入
    ///   出场：向上滑出 + 淡出
    ///   显示时长：默认 2 秒
    ///
    /// 类型颜色：
    ///   Info    -> 蓝色背景
    ///   Success -> 绿色背景
    ///   Warning -> 黄色背景
    ///   Error   -> 红色背景
    /// </summary>
    public class ToastManager : MonoBehaviour
    {
        // === 单例 ===
        public static ToastManager Instance { get; private set; }

        // === UI 组件 ===
        [SerializeField] private RectTransform _toastContainer;       // Toast 容器
        [SerializeField] private GameObject _toastPrefab;              // Toast 预制体（TextMeshProUGUI + Image）
        [SerializeField] private Canvas _toastCanvas;

        // === 颜色配置 ===
        [SerializeField] private Color _infoColor = new Color(0.2f, 0.5f, 1f);
        [SerializeField] private Color _successColor = new Color(0.2f, 0.8f, 0.3f);
        [SerializeField] private Color _warningColor = new Color(1f, 0.8f, 0.2f);
        [SerializeField] private Color _errorColor = new Color(1f, 0.3f, 0.3f);

        // === 参数 ===
        [SerializeField] private float _displayDuration = 2f;         // 默认显示时长
        [SerializeField] private float _fadeInDuration = 0.3f;
        [SerializeField] private float _fadeOutDuration = 0.3f;
        [SerializeField] private int _maxQueueSize = 10;              // 最大队列长度

        // === 运行时数据 ===
        private Queue<ToastRequest> _toastQueue;                      // Toast 请求队列
        private bool _isShowing;                                      // 是否正在显示

        private class ToastRequest
        {
            public string Message;
            public ToastType Type;
            public Sprite Icon;
            public float Duration;
        }

        // === Unity 生命周期 ===
        private void Awake() { }

        // === 公共方法 ===

        /// <summary>显示信息提示</summary>
        public void ShowInfo(string message) { EnqueueToast(message, ToastType.Info); }

        /// <summary>显示成功提示</summary>
        public void ShowSuccess(string message) { EnqueueToast(message, ToastType.Success); }

        /// <summary>显示警告提示</summary>
        public void ShowWarning(string message) { EnqueueToast(message, ToastType.Warning); }

        /// <summary>显示错误提示</summary>
        public void ShowError(string message) { EnqueueToast(message, ToastType.Error); }

        /// <summary>显示带图标的提示（金币/钻石/经验获得等）</summary>
        /// <param name="message">提示文字</param>
        /// <param name="icon">图标 Sprite</param>
        /// <param name="duration">显示时长（秒），默认使用全局配置</param>
        public void ShowWithIcon(string message, Sprite icon, float duration = -1f) { }

        /// <summary>清除所有待显示的 Toast</summary>
        public void ClearQueue() { _toastQueue.Clear(); }

        // === 私有方法 ===
        private void EnqueueToast(string message, ToastType type, Sprite icon = null, float duration = -1f) { }
        private void ProcessNextToast() { }
        private IEnumerator ShowToastRoutine(ToastRequest request) { yield return null; }
        private Color GetColorForType(ToastType type) { return Color.white; }
        private IEnumerator PlayEnterAnimation(GameObject toast) { yield return null; }
        private IEnumerator PlayExitAnimation(GameObject toast) { yield return null; }
    }

    /// <summary>Toast 类型</summary>
    public enum ToastType
    {
        Info,
        Success,
        Warning,
        Error
    }
}
