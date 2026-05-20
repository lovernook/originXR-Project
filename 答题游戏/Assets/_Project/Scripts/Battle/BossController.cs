using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

namespace OriginXR.Battle
{
    /// <summary>
    /// BOSS 状态
    /// </summary>
    public enum BossState { Idle, Attacking, Hurt, Skill, Defeated, Intro }

    /// <summary>
    /// BOSS 动画与展示控制器
    /// 负责：
    /// 1. PVE 战斗中 BOSS 3D 模型展示与状态动画
    /// 2. BOSS 血条 UI 更新与平滑扣血动画
    /// 3. 答题结果触发 BOSS 受击/攻击动画
    /// 4. BOSS 阶段切换（HP < 50% 进入狂暴第二阶段）
    /// 5. BOSS 击败特效与动画序列
    /// </summary>
    public class BossController : MonoBehaviour
    {
        [Header("3D 模型")]
        [SerializeField] private Transform _bossModelRoot;
        [SerializeField] private Animator _bossAnimator;
        [SerializeField] private float _modelRotationSpeed = 20f;   // 待机时缓慢旋转

        [Header("特效")]
        [SerializeField] private ParticleSystem _attackVFX;
        [SerializeField] private ParticleSystem _hitVFX;
        [SerializeField] private ParticleSystem _skillVFX;
        [SerializeField] private ParticleSystem _defeatedVFX;
        [SerializeField] private ParticleSystem _introVFX;
        [SerializeField] private ParticleSystem _phaseTwoVFX;       // 第二阶段激活特效

        [Header("UI 血条")]
        [SerializeField] private Slider _hpSlider;
        [SerializeField] private Image _hpFillImage;
        [SerializeField] private TextMeshProUGUI _bossNameText;
        [SerializeField] private TextMeshProUGUI _bossHpText;        // "750/1000"
        [SerializeField] private float _hpBarAnimSpeed = 2f;        // 血条平滑速度

        [Header("关卡数据")]
        [SerializeField] private int _damagePerCorrect = 100;       // 答对一次伤害值
        [SerializeField] private int _damagePerAttack = 25;         // 答错一次扣血量

        [Header("阶段切换")]
        [SerializeField] private float _phaseTwoThreshold = 0.5f;   // HP 低于 50% 进入第二阶段
        [SerializeField] private float _phaseTwoDamageMultiplier = 1.5f;  // 第二阶段伤害倍率

        // === Animator 参数哈希 ===
        private static readonly int ParamState = Animator.StringToHash("State");
        private static readonly int TriggerHit = Animator.StringToHash("Hit");
        private static readonly int TriggerAttack = Animator.StringToHash("Attack");
        private static readonly int TriggerSkill = Animator.StringToHash("Skill");
        private static readonly int TriggerDefeated = Animator.StringToHash("Defeated");
        private static readonly int TriggerIntro = Animator.StringToHash("Intro");

        // === 属性 ===
        public BossState CurrentState { get; private set; } = BossState.Idle;
        public float CurrentHP { get; private set; }
        public float MaxHP { get; private set; }
        public string BossName { get; private set; }
        public bool IsPhaseTwo { get; private set; }

        // === 内部状态 ===
        private float _displayHP;            // 用于平滑显示
        private float _idleRotationTimer;

        // === 事件 ===
        public event Action OnBossDefeated;
        public event Action OnBossPhaseChange;

        // === Unity 生命周期 ===

        private void Start()
        {
            if (_hpSlider != null)
            {
                _hpSlider.minValue = 0f;
                _hpSlider.maxValue = 1f;
            }
        }

        private void Update()
        {
            // 待机时缓慢旋转模型
            if (CurrentState == BossState.Idle && _bossModelRoot != null)
            {
                _bossModelRoot.Rotate(Vector3.up, _modelRotationSpeed * Time.deltaTime);
            }

            // 平滑血条显示
            if (_hpSlider != null && Mathf.Abs(_displayHP - Mathf.Clamp01(CurrentHP / MaxHP)) > 0.001f)
            {
                _displayHP = Mathf.Lerp(_displayHP, Mathf.Clamp01(CurrentHP / MaxHP), Time.deltaTime * _hpBarAnimSpeed);
                _hpSlider.value = _displayHP;
                UpdateHPText();
            }
        }

        // === 公共方法 ===

        /// <summary>初始化 BOSS</summary>
        public void Initialize(string modelId, string bossName, float maxHP)
        {
            BossName = bossName;
            MaxHP = maxHP > 0 ? maxHP : 1000f;
            CurrentHP = MaxHP;
            _displayHP = 1f;
            IsPhaseTwo = false;

            if (_bossNameText != null)
                _bossNameText.text = bossName;

            if (_hpSlider != null)
            {
                _hpSlider.value = 1f;
                _hpSlider.maxValue = 1f;
            }

            UpdateHPText();

            // 播放出场动画
            ChangeState(BossState.Intro);
            StartCoroutine(PlayIntroSequence());
        }

        /// <summary>BOSS 攻击玩家（玩家答错）</summary>
        public void PlayAttack()
        {
            ChangeState(BossState.Attacking);

            if (_bossAnimator != null)
                _bossAnimator.SetTrigger(TriggerAttack);

            if (_attackVFX != null)
                _attackVFX.Play();

            // 短暂延迟后回到待机
            StartCoroutine(ReturnToIdleAfter(1.2f));

            Core.AudioManager.Instance?.PlaySFX("boss_attack");
        }

        /// <summary>BOSS 受击（玩家答对）</summary>
        /// <param name="damage">伤害值</param>
        /// <param name="isCombo">是否连击伤害</param>
        public void TakeDamage(int damage, bool isCombo = false)
        {
            if (CurrentHP <= 0f) return;

            float actualDamage = damage;
            if (isCombo) actualDamage *= 1.5f;
            if (IsPhaseTwo) actualDamage *= _phaseTwoDamageMultiplier;

            CurrentHP = Mathf.Max(0f, CurrentHP - actualDamage);

            // 受击动画
            ChangeState(BossState.Hurt);

            if (_bossAnimator != null)
                _bossAnimator.SetTrigger(TriggerHit);

            if (_hitVFX != null)
                _hitVFX.Play();

            // 血量低亮闪烁
            FlashHPBar();

            StartCoroutine(ReturnToIdleAfter(0.8f));

            Core.AudioManager.Instance?.PlaySFX("boss_hit");

            // 检查阶段切换
            if (!IsPhaseTwo && CurrentHP / MaxHP <= _phaseTwoThreshold)
            {
                EnterPhaseTwo();
            }

            // 检查是否击败
            if (CurrentHP <= 0f)
            {
                StartCoroutine(PlayDefeatedSequence());
            }
        }

        /// <summary>获取 BOSS 剩余血量百分比</summary>
        public float GetHPPercent() => MaxHP > 0f ? CurrentHP / MaxHP : 0f;

        /// <summary>是否已击败</summary>
        public bool IsDefeated() => CurrentHP <= 0f;

        /// <summary>设置可见性</summary>
        public void SetVisible(bool visible)
        {
            if (_bossModelRoot != null)
                _bossModelRoot.gameObject.SetActive(visible);
        }

        // === 私有方法 ===

        private void ChangeState(BossState newState)
        {
            CurrentState = newState;
        }

        private IEnumerator PlayIntroSequence()
        {
            if (_bossAnimator != null)
                _bossAnimator.SetTrigger(TriggerIntro);

            if (_introVFX != null)
                _introVFX.Play();

            yield return new WaitForSeconds(2f);
            ChangeState(BossState.Idle);
        }

        private IEnumerator PlayDefeatedSequence()
        {
            ChangeState(BossState.Defeated);

            if (_bossAnimator != null)
                _bossAnimator.SetTrigger(TriggerDefeated);

            if (_defeatedVFX != null)
                _defeatedVFX.Play();

            Core.AudioManager.Instance?.PlaySFX("boss_defeated");

            yield return new WaitForSeconds(2f);

            OnBossDefeated?.Invoke();
        }

        private void EnterPhaseTwo()
        {
            IsPhaseTwo = true;
            Debug.Log($"[BossController] BOSS 进入第二阶段！伤害 ×{_phaseTwoDamageMultiplier}");

            if (_bossAnimator != null)
                _bossAnimator.SetTrigger(TriggerSkill);

            if (_phaseTwoVFX != null)
                _phaseTwoVFX.Play();

            if (_skillVFX != null)
                _skillVFX.Play();

            ChangeState(BossState.Skill);
            StartCoroutine(ReturnToIdleAfter(2f));

            OnBossPhaseChange?.Invoke();
            Core.AudioManager.Instance?.PlaySFX("boss_phase2");
        }

        private IEnumerator ReturnToIdleAfter(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (CurrentState != BossState.Defeated && CurrentState != BossState.Intro)
                ChangeState(BossState.Idle);
        }

        private void FlashHPBar()
        {
            if (_hpFillImage == null) return;
            StartCoroutine(FlashHPRoutine());
        }

        private IEnumerator FlashHPRoutine()
        {
            Color originalColor = _hpFillImage.color;
            _hpFillImage.color = Color.white;
            yield return new WaitForSeconds(0.1f);
            _hpFillImage.color = originalColor;
        }

        private void UpdateHPText()
        {
            if (_bossHpText != null)
            {
                float displayCurrent = Mathf.RoundToInt(_displayHP * MaxHP);
                _bossHpText.text = $"{displayCurrent} / {MaxHP}";
            }
        }
    }
}
