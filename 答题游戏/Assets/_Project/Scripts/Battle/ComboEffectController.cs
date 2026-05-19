using UnityEngine;
using TMPro;
using DG.Tweening;
using System;
using System.Collections;

namespace OriginXR.Battle
{
    /// <summary>
    /// 连击特效控制器
    /// 负责：
    /// 1. 管理连击计数器的显示与动画
    /// 2. 连续答对3题触发"连击"效果（双倍伤害 + 特效爆发）
    /// 3. 答错时清零连击 + 播放连击断裂动画
    /// 4. 不同连击段位的视觉差异化（3连击 / 5连击 / 10连击）
    ///
    /// 连击段位：
    ///   3 combo  -> "连击！"  蓝色特效 + 1.5x 得分
    ///   5 combo  -> "超连击！" 紫色特效 + 2x 得分
    ///   10 combo -> "完美连击！" 金色特效 + 3x 得分
    ///
    /// 依赖：
    ///   DOTween（动画序列），Particle System（粒子特效），TextMeshPro（文本）
    /// </summary>
    public class ComboEffectController : MonoBehaviour
    {
        // === UI 组件 ===
        [SerializeField] private TextMeshProUGUI _comboCountText;     // 连击数字
        [SerializeField] private TextMeshProUGUI _comboLabelText;      // "连击！"标签
        [SerializeField] private RectTransform _comboContainer;        // 连击UI容器
        [SerializeField] private CanvasGroup _canvasGroup;             // 透明度控制

        // === 特效组件 ===
        [SerializeField] private ParticleSystem _comboActivateEffect;  // 连击激活粒子
        [SerializeField] private ParticleSystem _comboBreakEffect;     // 连击断裂粒子
        [SerializeField] private ParticleSystem _comboUpgradeEffect;   // 连击升级粒子

        // === 连击参数 ===
        [SerializeField] private int _minComboForBonus = 3;           // 最低触发连击的答题数
        [SerializeField] private int _superComboThreshold = 5;        // 超连击阈值
        [SerializeField] private int _perfectComboThreshold = 10;     // 完美连击阈值
        [SerializeField] private float _comboTimeout = 5f;            // 连击超时（秒，未答题自动断连）

        // === 颜色配置 ===
        [SerializeField] private Color _normalComboColor = new Color(0.2f, 0.6f, 1f);     // 蓝色
        [SerializeField] private Color _superComboColor = new Color(0.6f, 0.2f, 1f);       // 紫色
        [SerializeField] private Color _perfectComboColor = new Color(1f, 0.8f, 0.2f);     // 金色

        // === 状态 ===
        private int _currentCombo;

        // === Unity 生命周期 ===
        private void Start() { }

        // === 公共方法 ===

        /// <summary>增加连击数（答对时调用）</summary>
        public void IncrementCombo() { }

        /// <summary>重置连击（答错/超时时调用）</summary>
        public void ResetCombo() { }

        /// <summary>获取当前连击得分倍率</summary>
        /// <returns>1.0 / 1.5 / 2.0 / 3.0</returns>
        public float GetComboMultiplier() { return 1f; }

        /// <summary>是否处于连击状态</summary>
        public bool IsComboActive() { return _currentCombo >= _minComboForBonus; }

        /// <summary>获取当前连击段位文字</summary>
        public string GetComboTierText()
        {
            if (_currentCombo >= _perfectComboThreshold) return "完美连击！";
            if (_currentCombo >= _superComboThreshold) return "超连击！";
            if (_currentCombo >= _minComboForBonus) return "连击！";
            return "";
        }

        // === 私有方法 ===
        private void UpdateComboUI() { }
        private void PlayComboActivateAnimation() { }
        private void PlayComboBreakAnimation() { }
        private void PlayComboUpgradeAnimation() { }
        private void UpdateComboTextColor() { }
        private IEnumerator ShakeComboText() { yield return null; }         // 数字震动动画
        private IEnumerator ScalePunchAnimation(float scale) { yield return null; }  // 弹性缩放动画
    }
}
