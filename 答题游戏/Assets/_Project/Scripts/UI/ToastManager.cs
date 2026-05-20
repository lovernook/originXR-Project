using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

namespace OriginXR.UI
{
    /// <summary>
    /// Toast 提示类型
    /// </summary>
    public enum ToastType { Info, Success, Warning, Error }

    /// <summary>
    /// Toast 提示管理器（单例）
    /// 负责：
    /// 1. 顶部自动消失提示（支持 Info/Success/Warning/Error 四种类型）
    /// 2. 队列机制：多条 Toast 依次显示不重叠
    /// 3. 入场滑入 + 出场淡出动画
    /// 4. 支持带图标提示（如金币+1、经验+100）
    /// </summary>
    public class ToastManager : MonoBehaviour
    {
        [Header("Toast 预制体")]
        [SerializeField] private GameObject _toastPrefab;
        [SerializeField] private RectTransform _toastContainer;

        [Header("颜色")]
        [SerializeField] private Color _infoColor = new Color(0.2f, 0.5f, 1f);
        [SerializeField] private Color _successColor = new Color(0.2f, 0.8f, 0.3f);
        [SerializeField] private Color _warningColor = new Color(1f, 0.8f, 0.2f);
        [SerializeField] private Color _errorColor = new Color(1f, 0.3f, 0.3f);

        [Header("动画")]
        [SerializeField] private float _displayDuration = 2f;
        [SerializeField] private float _fadeInDuration = 0.3f;
        [SerializeField] private float _fadeOutDuration = 0.3f;
        [SerializeField] private float _slideDistance = 80f;

        // === 单例 ===
        public static ToastManager Instance { get; private set; }

        // === 内部状态 ===
        private Queue<ToastRequest> _queue = new Queue<ToastRequest>();
        private bool _isShowing;

        private class ToastRequest
        {
            public string message;
            public ToastType type;
            public Sprite icon;
            public float duration;
        }

        // === Unity 生命周期 ===

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // === 公共便捷方法 ===

        public void ShowInfo(string message) => Enqueue(message, ToastType.Info);
        public void ShowSuccess(string message) => Enqueue(message, ToastType.Success);
        public void ShowWarning(string message) => Enqueue(message, ToastType.Warning);
        public void ShowError(string message) => Enqueue(message, ToastType.Error);

        /// <summary>显示带图标提示</summary>
        public void ShowWithIcon(string message, Sprite icon, float duration = -1f)
        {
            Enqueue(message, ToastType.Info, icon, duration > 0f ? duration : _displayDuration);
        }

        /// <summary>清除队列</summary>
        public void ClearQueue() => _queue.Clear();

        // === 私有方法 ===

        private void Enqueue(string message, ToastType type, Sprite icon = null, float duration = -1f)
        {
            _queue.Enqueue(new ToastRequest
            {
                message = message,
                type = type,
                icon = icon,
                duration = duration > 0f ? duration : _displayDuration
            });

            if (!_isShowing)
                ProcessNext();
        }

        private void ProcessNext()
        {
            if (_queue.Count == 0) return;
            StartCoroutine(ShowToastRoutine(_queue.Dequeue()));
        }

        private IEnumerator ShowToastRoutine(ToastRequest request)
        {
            _isShowing = true;

            GameObject toastObj = _toastPrefab != null
                ? Instantiate(_toastPrefab, _toastContainer)
                : CreateToastObject(request);

            RectTransform rect = toastObj.GetComponent<RectTransform>();
            if (rect == null) rect = toastObj.AddComponent<RectTransform>();

            // 设置内容
            TextMeshProUGUI textComp = toastObj.GetComponentInChildren<TextMeshProUGUI>();
            if (textComp != null)
                textComp.text = request.message;

            Image bgImage = toastObj.GetComponent<Image>();
            if (bgImage != null)
                bgImage.color = GetColorForType(request.type);

            // 入场动画：从顶部滑入
            rect.anchoredPosition = new Vector2(0, _slideDistance);
            yield return StartCoroutine(SlideIn(rect));

            // 停留
            yield return new WaitForSeconds(request.duration);

            // 出场动画：淡出上滑
            yield return StartCoroutine(SlideOut(rect));

            Destroy(toastObj);
            _isShowing = false;

            // 处理下一条
            ProcessNext();
        }

        private GameObject CreateToastObject(ToastRequest request)
        {
            GameObject obj = new GameObject("Toast");
            obj.transform.SetParent(_toastContainer, false);

            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(500, 60);

            Image bg = obj.AddComponent<Image>();
            bg.color = GetColorForType(request.type);

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(obj.transform, false);
            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = request.message;
            tmp.fontSize = 20;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            return obj;
        }

        private Color GetColorForType(ToastType type)
        {
            return type switch
            {
                ToastType.Success => _successColor,
                ToastType.Warning => _warningColor,
                ToastType.Error => _errorColor,
                _ => _infoColor
            };
        }

        private IEnumerator SlideIn(RectTransform rect)
        {
            float elapsed = 0f;
            Vector2 startPos = new Vector2(0, _slideDistance);
            Vector2 targetPos = Vector2.zero;

            CanvasGroup cg = rect.GetComponent<CanvasGroup>();
            if (cg == null) cg = rect.gameObject.AddComponent<CanvasGroup>();

            while (elapsed < _fadeInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / _fadeInDuration);
                rect.anchoredPosition = Vector2.Lerp(startPos, targetPos, EaseOut(t));
                cg.alpha = t;
                yield return null;
            }

            rect.anchoredPosition = targetPos;
            cg.alpha = 1f;
        }

        private IEnumerator SlideOut(RectTransform rect)
        {
            float elapsed = 0f;
            Vector2 startPos = rect.anchoredPosition;
            Vector2 targetPos = new Vector2(0, _slideDistance);

            CanvasGroup cg = rect.GetComponent<CanvasGroup>();
            if (cg == null) cg = rect.gameObject.AddComponent<CanvasGroup>();

            while (elapsed < _fadeOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / _fadeOutDuration);
                rect.anchoredPosition = Vector2.Lerp(startPos, targetPos, EaseIn(t));
                cg.alpha = 1f - t;
                yield return null;
            }

            rect.anchoredPosition = targetPos;
            cg.alpha = 0f;
        }

        private float EaseOut(float t) => 1f - Mathf.Pow(1f - t, 3f);
        private float EaseIn(float t) => Mathf.Pow(t, 3f);
    }
}
