using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

namespace OriginXR.Battle
{
    /// <summary>
    /// 答题倒计时控制器（纯文字版）
    /// 负责：
    /// 1. 每题倒计时显示 "8s"
    /// 2. 最后5秒变黄、3秒变红
    /// 3. 时间耗尽触发 OnTimeUp
    /// </summary>
    public class TimerController : MonoBehaviour
    {
        [Header("UI 引用（通过 BattleSceneSetup 自动注入）")]
        public Image timerCircleImage;        // 可选，环形进度条
        public TextMeshProUGUI timerText;      // 必填，倒计时文字

        [Header("颜色")]
        [SerializeField] private Color _normalColor = new Color(0.2f, 0.8f, 0.3f);
        [SerializeField] private Color _warningColor = new Color(1f, 0.85f, 0.2f);
        [SerializeField] private Color _criticalColor = new Color(1f, 0.3f, 0.3f);
        [SerializeField] private float _warningThreshold = 5f;
        [SerializeField] private float _criticalThreshold = 3f;

        // === 状态 ===
        private float _timeLimit;
        private float _remainingTime;
        private bool _isRunning;
        private bool _isPaused;
        private Coroutine _timerRoutine;

        // === 事件 ===
        public event Action OnTimeUp;

        // === 公共方法 ===

        public void StartCountdown(float timeLimit)
        {
            Debug.Log("=== StartCountdown 被调用！时间限制: " + timeLimit);
            _timeLimit = Mathf.Max(timeLimit, 3f);
            _remainingTime = _timeLimit;
            _isRunning = true;
            _isPaused = false;

            UpdateTimerUI();

            if (_timerRoutine != null)
                StopCoroutine(_timerRoutine);

            _timerRoutine = StartCoroutine(CountdownRoutine());
        }

        public void Pause() => _isPaused = true;
        public void Resume() => _isPaused = false;

        public void StopAndReset()
        {
            _isRunning = false;
            if (_timerRoutine != null) StopCoroutine(_timerRoutine);
            if (timerText != null) timerText.text = "";
        }

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
                }
                yield return null;
            }

            _isRunning = false;
            OnTimeUp?.Invoke();
        }

        private void UpdateTimerUI()
        {
            // 环形进度条（如果有）
            if (timerCircleImage != null && _timeLimit > 0f)
                timerCircleImage.fillAmount = _remainingTime / _timeLimit;

            // 文字
            if (timerText != null)
                timerText.text = $"⏱ {_remainingTime:F1}s";
        }

        private void UpdateTimerColor()
        {
            Color c;
            if (_remainingTime <= _criticalThreshold)
                c = _criticalColor;
            else if (_remainingTime <= _warningThreshold)
                c = _warningColor;
            else
                c = _normalColor;

            if (timerText != null) timerText.color = c;
            if (timerCircleImage != null) timerCircleImage.color = c;
        }
    }
}
