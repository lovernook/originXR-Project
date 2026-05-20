using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using OriginXR.Data;

namespace OriginXR.Battle
{
    /// <summary>
    /// PVE 战斗结束原因
    /// </summary>
    public enum BattleEndReason
    {
        None,
        BossDefeated,
        LivesExhausted,
        QuestionsDone,
        PlayerQuit
    }

    /// <summary>
    /// PVE 对战逻辑控制器
    /// 负责：
    /// 1. PVE 专属逻辑：3条命管理、连击伤害倍率
    /// 2. BOSS 交互协调（答题对错 → BOSS 受伤/攻击）
    /// 3. 对话气泡展示（"强化学者"答题提示）
    /// 4. 生命耗尽判定与战斗结束
    /// </summary>
    public class PVEBattleController : MonoBehaviour
    {
        [Header("生命系统")]
        [SerializeField] private int _baseLives = 3;
        [SerializeField] private Image[] _lifeIcons;                // 生命图标（3个）
        [SerializeField] private Sprite _lifeIconFull;
        [SerializeField] private Sprite _lifeIconEmpty;

        [Header("对话气泡")]
        [SerializeField] private GameObject _dialogueBubble;
        [SerializeField] private TMPro.TextMeshProUGUI _dialogueText;
        [SerializeField] private float _dialogueShowTime = 2f;      // 对话显示时长

        [Header("连击倍率")]
        [SerializeField] private float _comboMultiplier3 = 1.5f;
        [SerializeField] private float _comboMultiplier5 = 2f;
        [SerializeField] private float _comboMultiplier10 = 3f;

        [Header("组件引用")]
        [SerializeField] private BossController _bossController;
        [SerializeField] private ComboEffectController _comboEffect;

        // === 属性 ===
        public int RemainingLives { get; private set; }
        public bool IsBossDefeated => _bossController != null && _bossController.IsDefeated();

        // === 事件 ===
        public event Action<BattleEndReason> OnPVEBattleEnd;
        public event Action<int> OnLivesChanged;

        // === Unity 生命周期 ===

        private void Start()
        {
            if (_dialogueBubble != null)
                _dialogueBubble.SetActive(false);
        }

        // === 公共方法 ===

        /// <summary>初始化 PVE 战斗</summary>
        public void Initialize(StageData stageData)
        {
            RemainingLives = _baseLives;
            UpdateLifeIcons();

            // 初始化 BOSS
            if (_bossController != null && stageData != null)
            {
                _bossController.Initialize(stageData.bossModelId, stageData.bossName, stageData.bossHP);
            }

            Debug.Log($"[PVEBattleController] 初始化完成，生命数: {RemainingLives}");
        }

        /// <summary>处理正确答题</summary>
        public void HandleCorrectAnswer(string questionId, int baseScore)
        {
            // 计算连击加成
            float multiplier = GetComboDamageMultiplier();
            int damage = Mathf.RoundToInt(100 * multiplier);
            bool isCombo = _comboEffect != null && _comboEffect.IsComboActive();

            // BOSS 受伤
            _bossController?.TakeDamage(damage, isCombo);

            // 对话气泡（随机鼓励语）
            string[] encouragements = { "漂亮！", "太棒了！", "答对了！", "继续保持！", "完美一击！" };
            string msg = encouragements[UnityEngine.Random.Range(0, encouragements.Length)];
            if (isCombo) msg = $"连击 ×{_comboEffect.GetComboCount()}！{msg}";
            ShowDialogue(msg);

            // 检查 BOSS 是否被击败
            if (_bossController != null && _bossController.IsDefeated())
            {
                OnPVEBattleEnd?.Invoke(BattleEndReason.BossDefeated);
            }
        }

        /// <summary>处理错误答题</summary>
        public void HandleWrongAnswer(string questionId)
        {
            RemainingLives--;
            UpdateLifeIcons();
            OnLivesChanged?.Invoke(RemainingLives);

            // BOSS 攻击
            _bossController?.PlayAttack();

            // 对话气泡
            string[] discouragements = { "小心！", "再想想...", "没关系的！", "加油！", "别气馁！" };
            ShowDialogue(discouragements[UnityEngine.Random.Range(0, discouragements.Length)]);

            // 生命耗尽
            if (RemainingLives <= 0)
            {
                OnPVEBattleEnd?.Invoke(BattleEndReason.LivesExhausted);
            }
        }

        /// <summary>检查战斗是否结束</summary>
        public BattleEndReason CheckBattleEnd()
        {
            if (RemainingLives <= 0) return BattleEndReason.LivesExhausted;
            if (IsBossDefeated) return BattleEndReason.BossDefeated;
            return BattleEndReason.None;
        }

        /// <summary>获取连击伤害倍率</summary>
        public float GetComboDamageMultiplier()
        {
            if (_comboEffect == null) return 1f;
            int combo = _comboEffect.GetComboCount();
            if (combo >= 10) return _comboMultiplier10;
            if (combo >= 5) return _comboMultiplier5;
            if (combo >= 3) return _comboMultiplier3;
            return 1f;
        }

        // === 私有方法 ===

        private void UpdateLifeIcons()
        {
            if (_lifeIcons == null) return;

            for (int i = 0; i < _lifeIcons.Length; i++)
            {
                if (_lifeIcons[i] != null)
                {
                    _lifeIcons[i].sprite = i < RemainingLives ? _lifeIconFull : _lifeIconEmpty;
                }
            }
        }

        private void ShowDialogue(string message)
        {
            if (_dialogueBubble == null || _dialogueText == null) return;

            StopAllCoroutines();
            _dialogueBubble.SetActive(true);
            _dialogueText.text = message;
            StartCoroutine(HideDialogueAfterDelay(_dialogueShowTime));
        }

        private IEnumerator HideDialogueAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (_dialogueBubble != null)
                _dialogueBubble.SetActive(false);
        }
    }
}
