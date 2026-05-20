using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

namespace OriginXR.Battle
{
    /// <summary>
    /// 答题倒计时控制器
    /// 负责：
    /// 1. 每题倒计时（限定时间由 QuestionData.timeLimit 指定）
    /// 2. 环形进度条 FillAmount 平滑过渡
    /// 3. 剩余时间文字显示 + 颜色渐变（绿 → 黄 → 红）
    /// 4. 最后5秒加速滴答音效 + 脉冲动画
    /// 5. 时间耗尽触发 OnTimeUp 事件
    /// </summary>
    public class TimerController : MonoBehaviour
    {
        [Header("环形进度条")]
        [SerializeField] private Image _timerCircleImage;          // FillAmount 控制的环形 Image
        [SerializeField] private TextMeshProUGUI _timerText;       // 剩余秒数 "8.0s"

        [Header("警告特效")]
        [SerializeField] private RectTransform _timerRoot;
        [SerializeField] private float _pulseScale = 1.15f;        // 最后3秒脉冲大小
        [SerializeField] private float _pulseDuration = 0.3f;

        [Header("颜色配置")]
        [SerializeField] private Color _normalColor = new Color(0.2f, 0.8f, 0.3f);   // 绿色
        [SerializeField] private Color _warningColor = new Color(1f, 0.85f, 0.2f);    // 黄色
        [SerializeField] private Color _criticalColor = new Color(1f, 0.3f, 0.3f);    // 红色
        [SerializeField] private float _warningThreshold = 5f;
        [SerializeField] private float _criticalThreshold = 3f;

        [Header("音效")]
        [SerializeField] private AudioSource _tickAudioSource;
        [SerializeField] private AudioSource _timeUpAudioSource;
        [SerializeField] private float _tickStartTime = 3f;       // 倒数3秒开始滴答

        // === 状态 ===
        private float _timeLimit;
        private float _remainingTime;
        private bool _isRunning;
        private bool _isPaused;
        private Action _onTimeUpCallback;
        private Coroutine _timerRoutine;

        // === 事件 ===
        public event Action OnTimeUp;

        // === Unity 生命周期 ===

        private void Awake()
        {
            if (_timerCircleImage != null)
                _timerCircleImage.fillAmount = 1f;
        }

        private void OnDisable()
        {
            StopAndReset();
        }

        // === 公共方法 ===

        /// <summary>启动倒计时</summary>
        /// <param name="timeLimit">限时秒数</param>
        public void StartCountdown(float timeLimit)
        {
            _timeLimit = Mathf.Max(timeLimit, 3f);
            _remainingTime = _timeLimit;
            _isRunning = true;
            _isPaused = false;

            if (_timerCircleImage != null)
                _timerCircleImage.fillAmount = 1f;

            if (_timerText != null)
                _timerText.text = $"{_remainingTime:F1}s";

            UpdateTimerColor();

            if (_timerRoutine != null)
                StopCoroutine(_timerRoutine);

            _timerRoutine = StartCoroutine(CountdownRoutine());
        }

        /// <summary>暂停计时</summary>
        public void Pause()
        {
            _isPaused = true;
        }

        /// <summary>恢复计时</summary>
        public void Resume()
        {
            _isPaused = false;
        }

        /// <summary>停止计时</summary>
        public void StopAndReset()
        {
            _isRunning = false;
            _isPaused = false;

            if (_timerRoutine != null)
            {
                StopCoroutine(_timerRoutine);
                _timerRoutine = null;
            }

            if (_timerCircleImage != null)
                _timerCircleImage.fillAmount = 1f;

            if (_timerText != null)
                _timerText.text = "";

            if (_tickAudioSource != null)
                _tickAudioSource.Stop();
        }

        /// <summary>获取剩余时间</summary>
        public float GetRemainingTime() => _remainingTime;

        // === 私有 ===

        private IEnumerator CountdownRoutine()
        {
            while (_remainingTime > 0f && _isRunning)
            {
                if (!_isPaused)
                {
                    _remainingTime -= Time.deltaTime;
                    if (_remainingTime < 0f) _remainingTime = 0f;

                    UpdateTimerUI();
                    UpdateTimerColor();

                    // 最后滴答音效
                    if (_remainingTime <= _tickStartTime && _remainingTime > 0f && _tickAudioSource != null && !_tickAudioSource.isPlaying)
                    {
                        _tickAudioSource.Play();
                    }

                    // 脉冲动画（最后3秒）
                    if (_remainingTime <= _criticalThreshold && _timerRoot != null)
                    {
                        float t = Mathf.PingPong(Time.unscaledTime * 4f, 1f);
                        _timerRoot.localScale = Vector3.one * Mathf.Lerp(1f, _pulseScale, t);
                    }
                }

                yield return null;
            }

            // 时间耗尽
            _isRunning = false;

            if (_timerRoot != null)
                _timerRoot.localScale = Vector3.one;

            if (_timeUpAudioSource != null)
                _timeUpAudioSource.Play();

            if (_tickAudioSource != null)
                _tickAudioSource.Stop();

            OnTimeUp?.Invoke();
        }

        private void UpdateTimerUI()
        {
            float progress = _timeLimit > 0f ? _remainingTime / _timeLimit : 0f;

            if (_timerCircleImage != null)
                _timerCircleImage.fillAmount = progress;

            if (_timerText != null)
                _timerText.text = $"{_remainingTime:F1}s";
        }

        private void UpdateTimerColor()
        {
            Color targetColor;
            if (_remainingTime <= _criticalThreshold)
                targetColor = _criticalColor;
            else if (_remainingTime <= _warningThreshold)
                targetColor = _warningColor;
            else
                targetColor = _normalColor;

            if (_timerCircleImage != null)
                _timerCircleImage.color = targetColor;

            if (_timerText != null)
                _timerText.color = targetColor;
        }
    }
}
