using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System;
using System.Collections;

namespace OriginXR.Guild
{
    /// <summary>
    /// 公会 BOSS 3D 展示控制器（预留 - 当前阶段暂不开发）
    /// 负责：
    /// 1. 在 GuildScene 中展示 3D 公会 BOSS 模型
    /// 2. 实时显示 BOSS 血条（通过 WebSocket guild:boss_hp_sync 同步）
    /// 3. 展示 BOSS 名称、等级、剩余时间
    /// 4. 伤害数字飘字效果（其他成员攻击时）
    /// 5. BOSS 击败特效与刷新倒计时
    ///
    /// 公会 BOSS 机制：
    ///   - 每周刷新一只巨型知识 BOSS
    ///   - 全员协作答题集火
    ///   - 每人每日可挑战3次
    ///   - 累积伤害计算贡献度排名
    ///
    /// 当前状态：暂不开发，仅保留接口定义。
    /// 待公会功能启动时实现具体逻辑。
    /// </summary>
    public class GuildBossDisplay : MonoBehaviour
    {
        // === 3D 组件 ===
        [SerializeField] private Transform _bossModelRoot;            // BOSS 模型挂载点
        [SerializeField] private Animator _bossAnimator;
        [SerializeField] private float _modelRotationSpeed = 30f;    // 模型展示旋转速度

        // === UI 组件 ===
        [SerializeField] private Slider _hpSlider;                    // BOSS 血条
        [SerializeField] private Image _hpFillImage;
        [SerializeField] private TextMeshProUGUI _bossNameText;
        [SerializeField] private TextMeshProUGUI _bossHpText;        // "75000 / 100000"
        [SerializeField] private TextMeshProUGUI _remainingTimeText;  // "剩余 3天12小时"
        [SerializeField] private TextMeshProUGUI _yourContributionText; // 个人贡献
        [SerializeField] private Button _challengeButton;             // 挑战按钮
        [SerializeField] private GameObject _defeatedOverlay;         // BOSS 被击败遮罩

        // === 伤害飘字 ===
        [SerializeField] private GameObject _damagePopupPrefab;       // 伤害数字预制体
        [SerializeField] private Transform _damagePopupRoot;          // 伤害数字父节点
        [SerializeField] private float _damagePopupDuration = 1.5f;

        // === 特效 ===
        [SerializeField] private ParticleSystem _bossAuraEffect;      // BOSS 光环
        [SerializeField] private ParticleSystem _hitEffect;           // 受击特效
        [SerializeField] private ParticleSystem _defeatedEffect;      // 击败特效

        // === 属性 ===
        public string BossId { get; private set; }
        public float CurrentHP { get; private set; }
        public float MaxHP { get; private set; }
        public DateTime RefreshTime { get; private set; }

        // === Unity 生命周期 ===
        private void Start() { }
        private void Update() { }

        // === 公共方法 ===

        /// <summary>初始化 BOSS 展示</summary>
        /// <param name="bossData">BOSS 数据 { id, name, modelId, maxHP, currentHP, refreshTime }</param>
        public void InitializeBoss(string bossDataJson) { }

        /// <summary>更新 BOSS 血量（由 WebSocket 推送触发）</summary>
        /// <param name="currentHP">当前血量</param>
        /// <param name="maxHP">最大血量</param>
        public void UpdateHP(float currentHP, float maxHP) { }

        /// <summary>显示伤害飘字</summary>
        /// <param name="playerName">造成伤害的玩家名称</param>
        /// <param name="damage">伤害值</param>
        public void ShowDamagePopup(string playerName, int damage) { }

        /// <summary>播放 BOSS 被击败动画</summary>
        public void PlayDefeatedSequence() { }

        /// <summary>点击挑战按钮回调</summary>
        public void OnChallengeClicked() { }

        /// <summary>更新剩余刷新时间显示</summary>
        public void UpdateRemainingTime() { }

        /// <summary>设置个人贡献显示</summary>
        public void SetPersonalContribution(int contribution) { }

        // === 私有方法 ===
        private void LoadBossModel(string modelId) { }
        private void RotateModel() { }                           // Update 中旋转模型展示
        private void AnimateHPBar(float targetHP) { }            // 血条缓动动画
        private IEnumerator PlayDefeatedVFXSequence() { yield return null; }
        private string FormatRemainingTime(DateTime refreshTime) { return ""; }

        // === 事件 ===
        /// <summary>点击挑战按钮事件</summary>
        public event Action OnChallengeRequested;

        /// <summary>BOSS 被击败事件</summary>
        public event Action OnBossDefeated;
    }
}
