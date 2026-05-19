using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

namespace OriginXR.Battle
{
    /// <summary>
    /// 答题倒计时控制器
    /// 负责：
    /// 1. 管理每题答题倒计时（限时由 QuestionData.TimeLimit 指定）
    /// 2. 使用环形进度条 Shader 渲染倒计时圆环动画
    /// 3. 时间耗尽自动提交（视为答错）
    /// 4. 倒计时最后5秒变色（黄→红）+ 音效提醒
    /// 5. PVP 模式需要显示双方进度条
    ///
    /// 环形进度条原理：
    ///   使用 Shader 控制一个圆环 Mesh 的 FillAmount，
    ///   从 1 -> 0 平滑过渡，颜色从绿 -> 黄 -> 红
    /// </summary>
    public class TimerController : MonoBehaviour
    {
        // === UI 组件 ===
        [SerializeField] private Image _timerCircleImage;       // 环形进度条 Image（Material 使用环形 Shader）
        [SerializeField] private Text _timerText;                // 剩余秒数文本
        [SerializeField] private GameObject _timerWarningEffect; // 警告闪烁效果

        // === PVP 双方计时 ===
        [SerializeField] private Image _opponentTimerImage;     // 对手计时器（PVP模式）
        [SerializeField] private Text _opponentTimerText;

        // === 颜色配置 ===
        [SerializeField] private Color _normalColor = Color.green;      // 正常颜色
        [SerializeField] private Color _warningColor = Color.yellow;    // 警告颜色（最后5秒）
        [SerializeField] private Color _criticalColor = Color.red;      // 危险颜色（最后3秒）

        // === 音效提示 ===
        [SerializeField] private AudioSource _tickAudioSource;          // 滴答声
        [SerializeField] private AudioSource _timeUpAudioSource;        // 时间到音效

        // === 参数 ===
        private float _timeLimit;            // 当前题目限时（秒）
        private float _remainingTime;        // 剩余时间
        private bool _isRunning;             // 计时器是否运行
        private Coroutine _countdownCoroutine;

        // === Unity 生命周期 ===
        private void Awake() { }
        private void OnDisable() { }

        // === 公共方法 ===

        /// <summary>启动倒计时</summary>
        /// <param name="timeLimit">限时（秒）</param>
        /// <param name="onTimeUp">时间耗尽回调</param>
        public void StartCountdown(float timeLimit, Action onTimeUp) { }

        /// <summary>暂停计时</summary>
        public void Pause() { }

        /// <summary>恢复计时</summary>
        public void Resume() { }

        /// <summary>停止计时并重置</summary>
        public void StopAndReset() { }

        /// <summary>获取剩余时间</summary>
        public float GetRemainingTime() { return _remainingTime; }

        /// <summary>设置计时器可见性</summary>
        public void SetVisible(bool visible) { }

        // === 私有方法 ===
        private IEnumerator CountdownRoutine(Action onTimeUp) { yield return null; }
        private void UpdateTimerUI() { }       // 更新环 + 文本
        private void UpdateTimerColor() { }     // 根据剩余时间变色
        private void PlayTickSound() { }        // 最后5秒滴答声

        // === 事件 ===
        /// <summary>时间耗尽事件</summary>
        public event Action OnTimeUp;
    }
}
