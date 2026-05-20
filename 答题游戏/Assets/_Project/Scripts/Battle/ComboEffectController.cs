using UnityEngine;
using TMPro;
using System.Collections;

namespace OriginXR.Battle
{
    /// <summary>
    /// 连击特效控制器
    /// 负责：
    /// 1. 管理连击计数器 UI 显示（数字 + 文字标签）
    /// 2. 连击触发/断裂动画（弹性缩放 + 震动 + 粒子特效）
    /// 3. 连击得分倍率计算（3连=1.5x, 5连=2x, 10连=3x）
    /// 4. 连击段位颜色差异化
    /// </summary>
    public class ComboEffectController : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private RectTransform _comboContainer;
        [SerializeField] private TextMeshProUGUI _comboCountText;
        [SerializeField] private TextMeshProUGUI _comboLabelText;
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("特效")]
        [SerializeField] private ParticleSystem _comboActivateEffect;
        [SerializeField] private ParticleSystem _comboBreakEffect;
        [SerializeField] private ParticleSystem _comboUpgradeEffect;

        [Header("连击阈值")]
        [SerializeField] private int _minComboForBonus = 3;
        [SerializeField] private int _superComboThreshold = 5;
        [SerializeField] private int _perfectComboThreshold = 10;

        [Header("颜色")]
        [SerializeField] private Color _normalComboColor = new Color(0.2f, 0.6f, 1f);      // 蓝色
        [SerializeField] private Color _superComboColor = new Color(0.7f, 0.3f, 1f);       // 紫色
        [SerializeField] private Color _perfectComboColor = new Color(1f, 0.8f, 0.1f);     // 金色

        [Header("动画参数")]
        [SerializeField] private float _scalePunchAmount = 0.3f;
        [SerializeField] private float _scalePunchDuration = 0.3f;
        [SerializeField] private float _shakeAmount = 10f;
        [SerializeField] private float _shakeDuration = 0.3f;
        [SerializeField] private float _breakDuration = 0.5f;

        // === 状态 ===
        private int _currentCombo;
        private Coroutine _breakCoroutine;
        private Vector3 _originalScale;
        private Vector3 _originalPosition;

        // === Unity 生命周期 ===

        private void Awake()
        {
            if (_comboContainer != null)
            {
                _originalScale = _comboContainer.localScale;
                _originalPosition = _comboContainer.localPosition;
                _comboContainer.gameObject.SetActive(false);
            }
        }

        // === 公共方法 ===

        /// <summary>增加连击数（答对时调用）</summary>
        public void IncrementCombo()
        {
            _currentCombo++;

            if (_comboContainer != null)
                _comboContainer.gameObject.SetActive(true);

            if (_comboCountText != null)
                _comboCountText.text = _currentCombo.ToString();

            // 连击标签文字
            if (_comboLabelText != null)
            {
                _comboLabelText.text = GetComboTierText();
                _comboLabelText.color = GetComboTierColor();
            }

            // 更新颜色
            if (_comboCountText != null)
                _comboCountText.color = GetComboTierColor();

            // 连击激活特效
            if (_currentCombo == _minComboForBonus)
            {
                PlayComboActivateEffect();
            }
            // 连击升级特效
            else if (_currentCombo == _superComboThreshold || _currentCombo == _perfectComboThreshold)
            {
                PlayComboUpgradeEffect();
            }
            // 普通增加动画
            else if (_currentCombo > _minComboForBonus)
            {
                PlayScalePunch(_scalePunchAmount * 0.5f);
            }
        }

        /// <summary>重置连击（答错/超时）</summary>
        public void ResetCombo()
        {
            if (_currentCombo >= _minComboForBonus)
            {
                PlayComboBreakEffect();
            }

            _currentCombo = 0;

            if (_comboContainer != null)
            {
                if (_breakCoroutine != null)
                    StopCoroutine(_breakCoroutine);
                _breakCoroutine = StartCoroutine(HideComboAfterDelay(_breakDuration));
            }
        }

        /// <summary>获取当前连击得分倍率</summary>
        public float GetComboMultiplier()
        {
            if (_currentCombo >= _perfectComboThreshold) return 3f;
            if (_currentCombo >= _superComboThreshold) return 2f;
            if (_currentCombo >= _minComboForBonus) return 1.5f;
            return 1f;
        }

        /// <summary>是否连击激活中</summary>
        public bool IsComboActive() => _currentCombo >= _minComboForBonus;

        /// <summary>获取当前连击数</summary>
        public int GetComboCount() => _currentCombo;

        /// <summary>获取连击段位文字</summary>
        public string GetComboTierText()
        {
            if (_currentCombo >= _perfectComboThreshold) return "完美连击！";
            if (_currentCombo >= _superComboThreshold) return "超连击！";
            if (_currentCombo >= _minComboForBonus) return "连击！";
            return "";
        }

        // === 私有方法 ===

        private Color GetComboTierColor()
        {
            if (_currentCombo >= _perfectComboThreshold) return _perfectComboColor;
            if (_currentCombo >= _superComboThreshold) return _superComboColor;
            return _normalComboColor;
        }

        private void PlayComboActivateEffect()
        {
            if (_comboActivateEffect != null)
                _comboActivateEffect.Play();

            PlayScalePunch(_scalePunchAmount);
            PlayShake(_shakeAmount, _shakeDuration);

            Core.AudioManager.Instance?.PlayUISFX("combo_activate");
        }

        private void PlayComboBreakEffect()
        {
            if (_comboBreakEffect != null)
                _comboBreakEffect.Play();

            Core.AudioManager.Instance?.PlayUISFX("combo_break");
        }

        private void PlayComboUpgradeEffect()
        {
            if (_comboUpgradeEffect != null)
                _comboUpgradeEffect.Play();

            PlayScalePunch(_scalePunchAmount * 1.5f);

            Core.AudioManager.Instance?.PlayUISFX("combo_upgrade");
        }

        private void PlayScalePunch(float amount)
        {
            if (_comboContainer == null) return;
            StartCoroutine(ScalePunchRoutine(amount));
        }

        private IEnumerator ScalePunchRoutine(float amount)
        {
            float elapsed = 0f;
            float duration = _scalePunchDuration;
            Vector3 start = _originalScale;
            Vector3 peak = start * (1f + amount);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float ease = 1f - Mathf.Pow(1f - t, 3f); // easeOutCubic
                float scale = 1f + Mathf.Sin(t * Mathf.PI * 2f) * amount * (1f - t);
                _comboContainer.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }

            _comboContainer.localScale = _originalScale;
        }

        private void PlayShake(float amount, float duration)
        {
            if (_comboContainer == null) return;
            StartCoroutine(ShakeRoutine(amount, duration));
        }

        private IEnumerator ShakeRoutine(float amount, float duration)
        {
            float elapsed = 0f;
            Vector3 originalPos = _comboContainer.localPosition;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float decay = 1f - (elapsed / duration);
                float x = Random.Range(-1f, 1f) * amount * decay;
                float y = Random.Range(-1f, 1f) * amount * decay;
                _comboContainer.localPosition = originalPos + new Vector3(x, y, 0f);
                yield return null;
            }

            _comboContainer.localPosition = originalPos;
        }

        private IEnumerator HideComboAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (_comboContainer != null && _currentCombo == 0)
            {
                _comboContainer.gameObject.SetActive(false);
            }
        }
    }
}
