using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace OriginXR.Battle
{
    /// <summary>
    /// 星星弹出动画控制器
    /// 挂载到 SettlePanel 上，ShowStars 时依次弹出
    /// </summary>
    public class StarAnimator : MonoBehaviour
    {
        [Header("3颗星星 Image")]
        public Image[] stars;               // 按顺序 Star1/Star2/Star3

        [Header("动画参数")]
        [SerializeField] private float _bigScale = 2.5f;        // 初始放大倍数
        [SerializeField] private float _finalScale = 1f;        // 最终大小
        [SerializeField] private float _staggerDelay = 0.3f;    // 每颗星间隔
        [SerializeField] private float _growDuration = 0.3f;     // 放大时长
        [SerializeField] private float _shrinkDuration = 0.4f;   // 缩小时长

        /// <summary>播放星星弹出动画</summary>
        /// <param name="starCount">亮几颗星（1~3）</param>
        public void Play(int starCount)
        {
            StartCoroutine(PlayRoutine(starCount));
        }

        private IEnumerator PlayRoutine(int starCount)
        {
            for (int i = 0; i < stars.Length; i++)
            {
                if (stars[i] == null) continue;

                // 初始化：放大 + 透明 + 激活
                stars[i].gameObject.SetActive(true);
                stars[i].transform.localScale = Vector3.one * _bigScale;
                SetAlpha(stars[i], 0f);

                // 只在星星数范围内亮起
                bool isActive = i < starCount;
                stars[i].color = isActive ? Color.white : new Color(0.3f, 0.3f, 0.3f, 1f);

                if (isActive)
                {
                    // 动画：放大出现 → 缩小到目标大小
                    yield return StartCoroutine(AnimateStar(stars[i]));
                }

                // 间隔
                yield return new WaitForSeconds(_staggerDelay);
            }
        }

        private IEnumerator AnimateStar(Image star)
        {
            // 阶段1：放大+淡入
            float elapsed = 0f;
            while (elapsed < _growDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / _growDuration;
                SetAlpha(star, t);
                yield return null;
            }
            SetAlpha(star, 1f);

            // 阶段2：缩小回弹到最终大小
            elapsed = 0f;
            Vector3 startScale = star.transform.localScale;
            while (elapsed < _shrinkDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / _shrinkDuration;

                // 弹性缓出效果（超过目标再回弹）
                float ease = 1f + (1f - t) * (1f - t) * Mathf.Sin(t * Mathf.PI * 3f) * 0.5f;
                star.transform.localScale = Vector3.Lerp(startScale, Vector3.one * _finalScale, t) * ease;
                yield return null;
            }

            star.transform.localScale = Vector3.one * _finalScale;
        }

        private void SetAlpha(Image img, float alpha)
        {
            Color c = img.color;
            c.a = alpha;
            img.color = c;
        }
    }
}
