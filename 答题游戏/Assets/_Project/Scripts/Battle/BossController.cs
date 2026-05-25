using UnityEngine;
using UnityEngine.UI;
using System;

namespace OriginXR.Battle
{
    /// <summary>
    /// BOSS 2D 表现控制器
    /// 负责 SpriteRenderer/Image 的动画触发和血条显示
    /// </summary>
    public class BossController : MonoBehaviour
    {
        [Header("BOSS 展示")]
        public Image bossImage;                    // BOSS 图片（Animator 挂这个上）
        public Animator bossAnimator;              // BOSS 动画控制器

        [Header("血条")]
        public Slider hpSlider;
        public Image hpFillImage;

        [Header("动画参数")]
        [SerializeField] private string _attackTrigger = "Attack";
        [SerializeField] private string _hurtTrigger = "Hurt";

        // === 属性 ===
        public float CurrentHP { get; private set; }
        public float MaxHP { get; private set; }

        // === 事件 ===
        public event Action OnBossDefeated;

        /// <summary>初始化</summary>
        public void Initialize(string modelId, string bossName, float maxHP)
        {
            MaxHP = maxHP;
            CurrentHP = maxHP;

            if (hpSlider != null) { hpSlider.maxValue = 1f; hpSlider.value = 1f; }
        }

        /// <summary>BOSS 攻击动画（玩家答错）</summary>
        public void PlayAttack()
        {
            bossAnimator?.SetTrigger(_attackTrigger);
        }

        /// <summary>BOSS 受击（玩家答对）</summary>
        public void TakeDamage(int damage = 1)
        {
            CurrentHP = Mathf.Max(0, CurrentHP - damage);
            bossAnimator?.SetTrigger(_hurtTrigger);
            UpdateHPBar();
        }

        /// <summary>获取剩余百分比</summary>
        public float GetHPPercent() => MaxHP > 0f ? CurrentHP / MaxHP : 0f;

        /// <summary>是否击败</summary>
        public bool IsDefeated() => CurrentHP <= 0f;

        private void UpdateHPBar()
        {
            if (hpSlider != null)
                hpSlider.value = Mathf.Clamp01(CurrentHP / MaxHP);
        }
    }
}
