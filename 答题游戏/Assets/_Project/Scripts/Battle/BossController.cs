using UnityEngine;
using DG.Tweening;
using System;
using System.Collections;

namespace OriginXR.Battle
{
    /// <summary>
    /// BOSS 动画控制器
    /// 负责：
    /// 1. 管理 PVE 关卡中 BOSS 的 3D 模型显示与动画
    /// 2. 根据答题结果播放对应动画（攻击/受击/技能/出场/死亡）
    /// 3. 管理 BOSS 血条 UI 的更新与动画
    /// 4. BOSS 阶段切换（血量低于50%时进入第二阶段）
    ///
    /// BOSS 状态机：
    ///   Idle       -> 待机呼吸动画
    ///   Attacking  -> 释放攻击动画（玩家答错触发）
    ///   Hurt       -> 受击动画（玩家答对触发）
    ///   Skill      -> 释放技能动画（BOSS阶段切换）
    ///   Defeated   -> 死亡动画（HP归零）
    ///
    /// BOSS 配置来源：StageData.BossModelId / BossHP / BossAttack
    /// </summary>
    public enum BossState { Idle, Attacking, Hurt, Skill, Defeated }

    public class BossController : MonoBehaviour
    {
        // === 组件引用 ===
        [SerializeField] private Animator _bossAnimator;
        [SerializeField] private Transform _bossModelRoot;          // BOSS模型根节点
        [SerializeField] private ParticleSystem _attackVFX;        // 攻击特效
        [SerializeField] private ParticleSystem _hitVFX;           // 受击特效
        [SerializeField] private ParticleSystem _skillVFX;         // 技能特效
        [SerializeField] private ParticleSystem _defeatedVFX;      // 击败特效

        // === UI ===
        [SerializeField] private UnityEngine.UI.Slider _hpSlider;  // BOSS血条
        [SerializeField] private UnityEngine.UI.Image _hpFillImage;
        [SerializeField] private TMPro.TextMeshProUGUI _bossNameText;
        [SerializeField] private TMPro.TextMeshProUGUI _bossHpText;

        // === 属性 ===
        public BossState CurrentState { get; private set; }
        public float CurrentHP { get; private set; }
        public float MaxHP { get; private set; }
        public int DamagePerCorrect { get; set; } = 100;           // 每次答对的伤害值

        // === 阶段配置 ===
        [SerializeField] private float _phaseTwoThreshold = 0.5f;  // 第二阶段血量阈值
        private bool _isPhaseTwo;

        // === Animator 参数名 ===
        private static readonly int IsIdle = Animator.StringToHash("IsIdle");
        private static readonly int TriggerAttack = Animator.StringToHash("Attack");
        private static readonly int TriggerHurt = Animator.StringToHash("Hurt");
        private static readonly int TriggerSkill = Animator.StringToHash("Skill");
        private static readonly int TriggerDefeated = Animator.StringToHash("Defeated");

        // === Unity 生命周期 ===
        private void Start() { }

        // === 公共方法 ===

        /// <summary>初始化BOSS（加载模型 + 设置HP + 播放出场动画）</summary>
        /// <param name="modelId">BOSS 模型资源ID</param>
        /// <param name="bossName">BOSS 名称</param>
        /// <param name="maxHP">最大生命值</param>
        public void Initialize(string modelId, string bossName, float maxHP) { }

        /// <summary>播放 BOSS 攻击动画（玩家答错触发）</summary>
        public void PlayAttack() { }

        /// <summary>播放 BOSS 受击动画（玩家答对触发）</summary>
        /// <param name="damage">造成伤害值</param>
        /// <param name="isCombo">是否连击伤害</param>
        public void PlayHurt(int damage, bool isCombo = false) { }

        /// <summary>更新 BOSS 血条 UI</summary>
        public void UpdateHealthBar() { }

        /// <summary>获取 BOSS 剩余血量百分比</summary>
        public float GetHPPercent() { return CurrentHP / MaxHP; }

        /// <summary>是否已击败 BOSS</summary>
        public bool IsDefeated() { return CurrentHP <= 0; }

        /// <summary>设置 BOSS 模型可见性</summary>
        public void SetVisible(bool visible) { }

        // === 私有方法 ===
        private void LoadBossModel(string modelId) { }
        private IEnumerator PlayIntroAnimation() { yield return null; }
        private void ChangeState(BossState newState) { }
        private void CheckPhaseTransition() { }     // 检查是否进入第二阶段
        private void EnterPhaseTwo() { }
        private void PlayDefeatedSequence() { }

        // === 事件 ===
        /// <summary>BOSS 被击败事件</summary>
        public event Action OnBossDefeated;

        /// <summary>BOSS 进入第二阶段事件</summary>
        public event Action OnBossPhaseChange;
    }
}
