using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

namespace OriginXR.Guild
{
    /// <summary>
    /// 公会 BOSS 3D 展示控制器（预留模块）
    /// 负责：
    /// 1. GuildScene 中 3D BOSS 模型展示与自动旋转
    /// 2. BOSS 血条实时展示与平滑动画
    /// 3. 伤害数字飘字效果
    /// 4. 剩余刷新时间倒计时
    ///
    /// 当前状态：接口已实现，具体数据对接待公会功能启动
    /// </summary>
    public class GuildBossDisplay : MonoBehaviour
    {
        [Header("3D 模型")]
        [SerializeField] private Transform _bossModelRoot;
        [SerializeField] private Animator _bossAnimator;
        [SerializeField] private float _modelRotationSpeed = 20f;

        [Header("UI 血条")]
        [SerializeField] private Slider _hpSlider;
        [SerializeField] private Image _hpFillImage;
        [SerializeField] private TextMeshProUGUI _bossNameText;
        [SerializeField] private TextMeshProUGUI _bossHpText;
        [SerializeField] private TextMeshProUGUI _remainingTimeText;
        [SerializeField] private TextMeshProUGUI _yourContributionText;

        [Header("按钮")]
        [SerializeField] private Button _challengeButton;

        [Header("伤害飘字")]
        [SerializeField] private GameObject _damagePopupPrefab;
        [SerializeField] private Transform _damagePopupRoot;
        [SerializeField] private float _damagePopupDuration = 1.5f;
        [SerializeField] private float _damagePopupFloatSpeed = 50f;

        [Header("特效")]
        [SerializeField] private ParticleSystem _bossAuraEffect;
        [SerializeField] private ParticleSystem _hitEffect;
        [SerializeField] private ParticleSystem _defeatedEffect;

        [Header("状态")]
        [SerializeField] private GameObject _defeatedOverlay;

        // === 属性 ===
        public string BossId { get; private set; }
        public float CurrentHP { get; private set; }
        public float MaxHP { get; private set; }
        public DateTime BossRefreshTime { get; private set; }

        // === 内部状态 ===
        private float _displayHP;

        // === 事件 ===
        public event Action OnChallengeRequested;
        public event Action OnBossDefeated;

        // === Unity 生命周期 ===

        private void Update()
        {
            RotateBossModel();
            SmoothUpdateHPBar();
        }

        // === 公共方法 ===

        /// <summary>初始化 BOSS 展示</summary>
        public void InitializeBoss(string bossId, string bossName, float maxHP, float currentHP, DateTime refreshTime)
        {
            BossId = bossId;
            MaxHP = maxHP;
            CurrentHP = currentHP;
            _displayHP = currentHP;
            BossRefreshTime = refreshTime;

            if (_bossNameText != null) _bossNameText.text = bossName;
            if (_hpSlider != null) { _hpSlider.maxValue = 1f; _hpSlider.value = CurrentHP / MaxHP; }

            UpdateHPText();
            SetDefeatedState(CurrentHP <= 0f);
            _bossAuraEffect?.Play();
        }

        /// <summary>更新 BOSS 血量</summary>
        public void UpdateHP(float currentHP, float maxHP)
        {
            MaxHP = maxHP;
            CurrentHP = currentHP;
            UpdateHPText();
        }

        /// <summary>显示伤害飘字</summary>
        public void ShowDamagePopup(string playerName, int damage)
        {
            if (_damagePopupPrefab == null || _damagePopupRoot == null) return;

            GameObject popup = Instantiate(_damagePopupPrefab, _damagePopupRoot);
            TextMeshProUGUI text = popup.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null) text.text = $"-{damage}";

            Vector3 randomOffset = new Vector3(UnityEngine.Random.Range(-50f, 50f), 0f, 0f);
            popup.transform.localPosition = randomOffset;

            StartCoroutine(AnimateDamagePopup(popup));
        }

        /// <summary>播放 BOSS 击败动画</summary>
        public void PlayDefeatedSequence()
        {
            SetDefeatedState(true);
            _bossAnimator?.SetTrigger("Defeated");
            _defeatedEffect?.Play();
            _bossAuraEffect?.Stop();
            OnBossDefeated?.Invoke();
        }

        /// <summary>更新剩余时间显示</summary>
        public void UpdateRemainingTime(DateTime refreshTime)
        {
            BossRefreshTime = refreshTime;
            TimeSpan remaining = BossRefreshTime - DateTime.UtcNow;
            if (remaining.TotalSeconds > 0)
            {
                string timeStr = remaining.Days > 0
                    ? $"{remaining.Days}天{remaining.Hours}小时"
                    : $"{remaining.Hours}小时{remaining.Minutes}分";
                if (_remainingTimeText != null)
                    _remainingTimeText.text = $"剩余: {timeStr}";
            }
        }

        /// <summary>设置个人贡献</summary>
        public void SetPersonalContribution(int contribution)
        {
            if (_yourContributionText != null)
                _yourContributionText.text = $"我的贡献: {contribution:N0}";
        }

        /// <summary>挑战按钮回调</summary>
        public void OnChallengeClicked()
        {
            OnChallengeRequested?.Invoke();
        }

        // === 私有方法 ===

        private void RotateBossModel()
        {
            if (_bossModelRoot != null)
                _bossModelRoot.Rotate(Vector3.up, _modelRotationSpeed * Time.deltaTime);
        }

        private void SmoothUpdateHPBar()
        {
            if (_hpSlider == null) return;
            float target = MaxHP > 0f ? CurrentHP / MaxHP : 0f;
            _displayHP = Mathf.Lerp(_displayHP, target, Time.deltaTime * 3f);
            _hpSlider.value = _displayHP;
        }

        private void UpdateHPText()
        {
            if (_bossHpText != null)
                _bossHpText.text = $"{CurrentHP:N0} / {MaxHP:N0}";

            // 血量颜色渐变
            if (_hpFillImage != null)
            {
                float percent = MaxHP > 0f ? CurrentHP / MaxHP : 0f;
                if (percent > 0.5f) _hpFillImage.color = Color.green;
                else if (percent > 0.25f) _hpFillImage.color = Color.yellow;
                else _hpFillImage.color = Color.red;
            }
        }

        private void SetDefeatedState(bool defeated)
        {
            if (_defeatedOverlay != null) _defeatedOverlay.SetActive(defeated);
            if (_challengeButton != null) _challengeButton.interactable = !defeated;

            if (_bossAnimator != null)
            {
                _bossAnimator.SetBool("IsDefeated", defeated);
            }
        }

        private IEnumerator AnimateDamagePopup(GameObject popup)
        {
            float elapsed = 0f;
            Vector3 startPos = popup.transform.localPosition;
            CanvasGroup cg = popup.GetComponent<CanvasGroup>();
            if (cg == null) cg = popup.AddComponent<CanvasGroup>();

            while (elapsed < _damagePopupDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / _damagePopupDuration;
                popup.transform.localPosition = startPos + Vector3.up * _damagePopupFloatSpeed * t;
                cg.alpha = 1f - t;
                yield return null;
            }

            Destroy(popup);
        }
    }
}
