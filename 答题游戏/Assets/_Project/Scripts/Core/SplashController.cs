using UnityEngine;
using TMPro;
using System.Collections;

namespace OriginXR.Core
{
    /// <summary>
    /// SplashScene 启动画面控制器
    /// Logo 淡入 → 停留 → 淡出 → 跳转 HomeScene
    /// </summary>
    public class SplashController : MonoBehaviour
    {
        [Header("UI")]
        public CanvasGroup logoGroup;          // Logo 文字的 CanvasGroup
        public TextMeshProUGUI loadingText;

        [Header("动画时间")]
        [SerializeField] private float _fadeInTime = 0.8f;
        [SerializeField] private float _stayTime = 1.5f;
        [SerializeField] private float _fadeOutTime = 0.5f;
        [SerializeField] private string _nextSceneName = "HomeScene";

        private IEnumerator Start()
        {
            // 隐藏
            if (logoGroup != null) logoGroup.alpha = 0f;

            // 阶段1：淡入
            yield return StartCoroutine(Fade(0f, 1f, _fadeInTime));

            // 阶段2：停留
            if (loadingText != null)
            {
                loadingText.text = "正在加载...";
                loadingText.gameObject.SetActive(true);
            }
            yield return new WaitForSeconds(_stayTime);

            // 阶段3：淡出
            yield return StartCoroutine(Fade(1f, 0f, _fadeOutTime));

            // 跳转
            SceneLoader.Instance?.LoadScene(_nextSceneName);
        }

        private IEnumerator Fade(float from, float to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                if (logoGroup != null) logoGroup.alpha = Mathf.Lerp(from, to, t);
                yield return null;
            }
            if (logoGroup != null) logoGroup.alpha = to;
        }
    }
}
