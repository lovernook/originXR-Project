using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

namespace OriginXR.UI
{
    /// <summary>
    /// 弹窗管理器（单例）
    /// 负责：
    /// 1. 模态弹窗：通知(Alert)、确认(Confirm)、选择(Prompt)、自定义(Custom)
    /// 2. 弹窗进出动画（缩放弹入 + 遮罩淡入）
    /// 3. 点击遮罩关闭（可选）
    /// </summary>
    public class PopupManager : MonoBehaviour
    {
        [Header("组件")]
        [SerializeField] private Canvas _popupCanvas;
        [SerializeField] private Image _dimBackground;
        [SerializeField] private RectTransform _popupContainer;

        [Header("动画时间")]
        [SerializeField] private float _openDuration = 0.3f;
        [SerializeField] private float _closeDuration = 0.2f;

        // === 单例 ===
        public static PopupManager Instance { get; private set; }

        // === 状态 ===
        private GameObject _currentPopup;
        private bool _isShowing;
        private Action _onDimClickCallback;

        // === 事件 ===
        public event Action OnPopupOpened;
        public event Action OnPopupClosed;

        // === Unity 生命周期 ===

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (_popupCanvas != null)
                _popupCanvas.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // === 公共方法 ===

        /// <summary>显示通知弹窗（单按钮）</summary>
        public void ShowAlert(string title, string message, Action onConfirm = null, string confirmText = "确定")
        {
            ShowPopup(PopupType.Alert, title, message, new string[] { confirmText }, new Action[] { onConfirm });
        }

        /// <summary>显示确认弹窗（双按钮）</summary>
        public void ShowConfirm(string title, string message, Action onConfirm, Action onCancel = null,
            string confirmText = "确定", string cancelText = "取消")
        {
            ShowPopup(PopupType.Confirm, title, message,
                new string[] { confirmText, cancelText },
                new Action[] { onConfirm, onCancel });
        }

        /// <summary>显示选择弹窗（三按钮）</summary>
        public void ShowPrompt(string title, string message, string[] options, Action<int> onSelect)
        {
            Action[] actions = new Action[options.Length];
            for (int i = 0; i < options.Length; i++)
            {
                int index = i;
                actions[i] = () => onSelect?.Invoke(index);
            }
            ShowPopup(PopupType.Prompt, title, message, options, actions);
        }

        /// <summary>显示自定义内容弹窗</summary>
        public void ShowCustom(GameObject customContent)
        {
            CloseCurrentPopup();

            if (_popupCanvas != null)
                _popupCanvas.gameObject.SetActive(true);

            if (_popupContainer != null)
            {
                customContent.transform.SetParent(_popupContainer, false);
            }

            _currentPopup = customContent;
            _isShowing = true;

            if (_dimBackground != null)
                _dimBackground.gameObject.SetActive(true);

            StartCoroutine(PlayOpenAnimation(_popupContainer));
            OnPopupOpened?.Invoke();
        }

        /// <summary>关闭当前弹窗</summary>
        public void CloseCurrentPopup()
        {
            if (!_isShowing || _currentPopup == null) return;

            StartCoroutine(PlayCloseAnimation(_currentPopup, () =>
            {
                Destroy(_currentPopup);
                _currentPopup = null;
                _isShowing = false;

                if (_popupCanvas != null)
                    _popupCanvas.gameObject.SetActive(false);

                if (_dimBackground != null)
                    _dimBackground.gameObject.SetActive(false);

                OnPopupClosed?.Invoke();
            }));
        }

        /// <summary>关闭所有弹窗</summary>
        public void CloseAllPopups()
        {
            CloseCurrentPopup();
        }

        /// <summary>是否有弹窗显示</summary>
        public bool IsShowing() => _isShowing;

        // === 私有：构建弹窗 ===

        private void ShowPopup(PopupType type, string title, string message, string[] buttonTexts, Action[] callbacks)
        {
            CloseCurrentPopup();

            if (_popupCanvas != null)
                _popupCanvas.gameObject.SetActive(true);

            // 构建弹窗 GameObject
            GameObject popupObj = BuildPopupObject(type, title, message, buttonTexts, callbacks);
            if (popupObj == null) return;

            if (_popupContainer != null)
            {
                foreach (Transform child in _popupContainer)
                    Destroy(child.gameObject);

                popupObj.transform.SetParent(_popupContainer, false);
                popupObj.transform.localPosition = Vector3.zero;
            }

            _currentPopup = popupObj;
            _isShowing = true;

            if (_dimBackground != null)
            {
                _dimBackground.gameObject.SetActive(true);
                _dimBackground.raycastTarget = true;
            }

            _onDimClickCallback = null; // Alert/Confirm 不允许点击遮罩关闭
            StartCoroutine(PlayOpenAnimation(_popupContainer));
            OnPopupOpened?.Invoke();
        }

        private GameObject BuildPopupObject(PopupType type, string title, string message, string[] buttonTexts, Action[] callbacks)
        {
            GameObject obj = new GameObject($"Popup_{type}");
            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(500, 300);

            Image bg = obj.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.15f, 0.2f, 0.95f);

            // 标题
            GameObject titleObj = CreateText(obj.transform, "Title", title, 28, TextAlignmentOptions.Center);
            titleObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 100);

            // 内容
            GameObject msgObj = CreateText(obj.transform, "Message", message, 20, TextAlignmentOptions.Center);
            msgObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 20);

            // 按钮
            float buttonWidth = 180f;
            float spacing = 20f;
            float totalWidth = buttonTexts.Length * buttonWidth + (buttonTexts.Length - 1) * spacing;
            float startX = -totalWidth / 2f + buttonWidth / 2f;

            for (int i = 0; i < buttonTexts.Length; i++)
            {
                int index = i;
                CreateButton(obj.transform, $"Btn_{i}", buttonTexts[i],
                    new Vector2(startX + i * (buttonWidth + spacing), -80),
                    new Vector2(buttonWidth, 60),
                    () =>
                    {
                        callbacks[index]?.Invoke();
                        CloseCurrentPopup();
                    });
            }

            return obj;
        }

        private GameObject CreateText(Transform parent, string name, string text, int fontSize, TextAlignmentOptions alignment)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(400, 50);

            TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = Color.white;

            return obj;
        }

        private void CreateButton(Transform parent, string name, string text, Vector2 pos, Vector2 size, Action onClick)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = pos;

            Image img = obj.AddComponent<Image>();
            img.color = new Color(0.27f, 0.53f, 1f);

            Button btn = obj.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());

            GameObject textObj = CreateText(obj.transform, "Text", text, 22, TextAlignmentOptions.Center);
            textObj.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        }

        // === 动画 ===

        private IEnumerator PlayOpenAnimation(RectTransform target)
        {
            if (target == null) yield break;

            target.localScale = Vector3.zero;
            float elapsed = 0f;

            while (elapsed < _openDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / _openDuration);
                float eased = 1f - Mathf.Pow(1f - t, 3f); // EaseOut
                target.localScale = Vector3.one * eased;
                yield return null;
            }

            target.localScale = Vector3.one;
        }

        private IEnumerator PlayCloseAnimation(GameObject target, Action onComplete)
        {
            if (target == null) { onComplete?.Invoke(); yield break; }

            RectTransform rect = target.GetComponent<RectTransform>();
            if (rect == null) { onComplete?.Invoke(); yield break; }

            float elapsed = 0f;

            while (elapsed < _closeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / _closeDuration);
                float eased = Mathf.Pow(t, 3f); // EaseIn
                rect.localScale = Vector3.one * (1f - eased);
                yield return null;
            }

            onComplete?.Invoke();
        }

        /// <summary>遮罩点击事件</summary>
        public void OnDimBackgroundClicked()
        {
            _onDimClickCallback?.Invoke();
        }

        private enum PopupType { Alert, Confirm, Prompt }
    }
}
