using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System;
using System.Collections;

namespace OriginXR.UI
{
    /// <summary>
    /// 弹窗管理器
    /// 负责：
    /// 1. 管理所有模态弹窗（对话框/确认框/选择框/通知框/物品详情弹窗）
    /// 2. 统一的弹窗样式（标题 + 内容 + 按钮组）
    /// 3. 弹窗进出动画（缩放弹入 + 遮罩淡入）
    /// 4. 支持单按钮/双按钮/三按钮弹窗
    /// 5. 支持自定义内容的弹窗（传入自定义 Prefab）
    ///
    /// 弹窗类型：
    ///   Alert     - 单按钮通知弹窗（"确定"）
    ///   Confirm   - 双按钮确认弹窗（"确定" / "取消"）
    ///   Prompt    - 三按钮选择弹窗（"选项A" / "选项B" / "取消"）
    ///   Custom    - 自定义内容弹窗
    ///   Toast     - 自动消失提示（见 ToastManager）
    ///
    /// 依赖：DOTween（动画）
    /// </summary>
    public class PopupManager : MonoBehaviour
    {
        // === 单例 ===
        public static PopupManager Instance { get; private set; }

        // === UI 组件 ===
        [SerializeField] private Canvas _popupCanvas;
        [SerializeField] private Image _dimBackground;             // 暗色遮罩
        [SerializeField] private RectTransform _popupContainer;    // 弹窗容器

        // === 弹窗预制体 ===
        [SerializeField] private GameObject _alertPopupPrefab;     // 通知弹窗
        [SerializeField] private GameObject _confirmPopupPrefab;   // 确认弹窗
        [SerializeField] private GameObject _promptPopupPrefab;    // 选择弹窗

        // === 动画参数 ===
        [SerializeField] private float _openDuration = 0.3f;
        [SerializeField] private float _closeDuration = 0.2f;
        [SerializeField] private Ease _openEase = Ease.OutBack;
        [SerializeField] private Ease _closeEase = Ease.InBack;

        // === 状态 ===
        private bool _isPopupOpen;
        private GameObject _currentPopup;

        // === 公共方法 ===

        /// <summary>显示通知弹窗（单按钮）</summary>
        /// <param name="title">标题</param>
        /// <param name="message">内容</param>
        /// <param name="onConfirm">确认回调</param>
        /// <param name="confirmText">确认按钮文字，默认"确定"</param>
        public void ShowAlert(string title, string message, Action onConfirm = null, string confirmText = "确定") { }

        /// <summary>显示确认弹窗（双按钮）</summary>
        /// <param name="title">标题</param>
        /// <param name="message">内容</param>
        /// <param name="onConfirm">确认回调</param>
        /// <param name="onCancel">取消回调</param>
        /// <param name="confirmText">确认按钮文字</param>
        /// <param name="cancelText">取消按钮文字</param>
        public void ShowConfirm(string title, string message, Action onConfirm, Action onCancel = null, string confirmText = "确定", string cancelText = "取消") { }

        /// <summary>显示三按钮选择弹窗</summary>
        public void ShowPrompt(string title, string message, string[] options, Action<int> onSelect) { }

        /// <summary>显示自定义弹窗（传入预制体实例）</summary>
        /// <param name="customContent">自定义内容的 GameObject</param>
        public void ShowCustom(GameObject customContent) { }

        /// <summary>关闭当前弹窗</summary>
        public void CloseCurrentPopup() { }

        /// <summary>关闭所有弹窗</summary>
        public void CloseAllPopups() { }

        /// <summary>是否有弹窗正在显示</summary>
        public bool IsShowing() { return _isPopupOpen; }

        // === 私有方法 ===
        private IEnumerator PlayOpenAnimation(GameObject popup) { yield return null; }
        private IEnumerator PlayCloseAnimation(GameObject popup, Action onComplete) { yield return null; }
        private void SetupPopupButtons(GameObject popup, PopupType type, Action[] callbacks, string[] buttonTexts) { }
        private void OnDimBackgroundClicked() { }  // 点击遮罩关闭弹窗（可选）

        private enum PopupType { Alert, Confirm, Prompt, Custom }

        // === 事件 ===
        /// <summary>弹窗打开事件</summary>
        public event Action OnPopupOpened;

        /// <summary>弹窗关闭事件</summary>
        public event Action OnPopupClosed;
    }
}
