using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using OriginXR.Data;
using OriginXR.Core;

namespace OriginXR.Battle
{
    /// <summary>
    /// PVE 对战逻辑控制器
    /// 负责：
    /// 1. 管理 PVE 模式（闯关推图）的专属逻辑
    /// 2. 处理"强化学者"对话形式弹出题目
    /// 3. 玩家 Avatar + BOSS Avatar 的伤害/受击动画协调
    /// 4. 连击伤害倍率计算（连续3题1.5x, 5题2x, 10题3x）
    /// 5. 3条命机制管理（答错扣命，耗尽重新挑战）
    /// 6. 通关结算：经验/金币/星数/章节徽章
    ///
    /// 战斗流程：
    ///   1. 玩家选择关卡 -> POST /api/v1/game/stages/:id/start
    ///   2. 服务端下发题目列表（已根据关卡题目池抽取）
    ///   3. 逐题展示 -> 玩家作答 -> 判定结果 -> BOSS动画
    ///   4. 全部答完或BOSS击败 -> POST /api/v1/game/stages/:id/finish
    ///   5. 服务端返回结算数据
    ///
    /// 与 BattleManager 的关系：
    ///   BattleManager 负责任务分发和状态管理
    ///   PVEBattleController 负责 PVE 特有逻辑（生命数、BOSS交互、关卡进度）
    /// </summary>
    public class PVEBattleController : MonoBehaviour
    {
        // === 配置 ===
        /// <summary>每关基础生命数</summary>
        [SerializeField] private int _baseLives = 3;

        /// <summary>连续答对触发连击的阈值</summary>
        [SerializeField] private int _comboThreshold = 3;

        /// <summary>双倍伤害连击阈值（5连击）</summary>
        [SerializeField] private int _superComboThreshold = 5;

        // === 组件引用 ===
        [SerializeField] private BossController _bossController;
        [SerializeField] private ComboEffectController _comboEffectController;
        [SerializeField] private UnityEngine.UI.Image[] _lifeIcons;     // 生命图标
        [SerializeField] private GameObject _dialogueBubble;            // 学者对话气泡

        // === 属性 ===
        /// <summary>当前剩余生命数</summary>
        public int RemainingLives { get; private set; }

        /// <summary>当前连击数</summary>
        public int CurrentCombo { get; private set; }

        /// <summary>是否已击败BOSS</summary>
        public bool IsBossDefeated { get; private set; }

        /// <summary>当前关卡数据</summary>
        public StageData CurrentStage { get; private set; }

        // === Unity 生命周期 ===
        private void Start() { }

        // === 公共方法 ===

        /// <summary>初始化 PVE 战斗</summary>
        /// <param name="stageData">关卡数据</param>
        public void Initialize(StageData stageData) { }

        /// <summary>处理玩家正确答题</summary>
        /// <param name="questionId">题目ID</param>
        /// <param name="scoreGained">获得分数</param>
        public void HandleCorrectAnswer(string questionId, int scoreGained) { }

        /// <summary>处理玩家错误答题</summary>
        /// <param name="questionId">题目ID</param>
        public void HandleWrongAnswer(string questionId) { }

        /// <summary>检查是否战斗结束（生命耗尽 或 BOSS击败 或 题目用尽）</summary>
        public BattleEndReason CheckBattleEnd() { return BattleEndReason.None; }

        /// <summary>执行结算流程</summary>
        public void ExecuteSettlement() { }

        /// <summary>获取连击伤害倍率</summary>
        public float GetComboDamageMultiplier()
        {
            if (CurrentCombo >= _superComboThreshold) return 2f;
            if (CurrentCombo >= _comboThreshold) return 1.5f;
            return 1f;
        }

        // === 私有方法 ===
        private void UpdateLifeIcons() { }
        private void ShowDialogueBubble(string message) { }
        private void HideDialogueBubble() { }
        private int CalculateDamage(bool isComboActive) { return 0; }
        private void HandleLifeLost() { }
        private IEnumerator PlayRetryDialog() { yield return null; }

        // === 事件 ===
        /// <summary>战斗结束事件</summary>
        public event Action<BattleEndReason> OnPVEBattleEnd;

        /// <summary>生命数变化事件</summary>
        public event Action<int> OnLivesChanged;
    }

    /// <summary>PVE 战斗结束原因</summary>
    public enum BattleEndReason
    {
        None,           // 未结束
        BossDefeated,   // BOSS 被击败
        LivesExhausted, // 生命耗尽
        QuestionsDone,  // 题目答完（最终结算）
        PlayerQuit      // 玩家主动退出
    }
}
