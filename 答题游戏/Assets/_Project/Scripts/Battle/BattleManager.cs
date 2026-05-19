using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using OriginXR.Data;

namespace OriginXR.Battle
{
    /// <summary>
    /// 战斗总控管理器
    /// 负责：
    /// 1. 统筹管理 BattleScene 的整个答题战斗流程（PVE / PVP）
    /// 2. 管理战斗状态机：Intro -> Playing -> Question -> Result -> Settle
    /// 3. 协调各子系统：QuestionDisplay / TimerController / AnswerHandler / ComboEffect / BossController
    /// 4. 与服务端通信：获取题目 -> 提交答案 -> 结算奖励
    ///
    /// 战斗状态机：
    ///   Intro      - BOSS出场动画 + 准备工作
    ///   Playing    - 答题循环中
    ///   Question   - 展示题目 + 等待作答
    ///   AnswerShow - 展示答案结果（正确/错误动画）
    ///   Pause      - 暂停（使用道具/退出确认）
    ///   Settle     - 结算面板（得分/经验/奖励）
    ///   GameOver   - 生命耗尽
    ///
    /// API 对接：
    ///   POST /api/v1/game/stages/:id/start  -> 开始关卡
    ///   POST /api/v1/game/stages/:id/answer -> 提交答案
    ///   POST /api/v1/game/stages/:id/finish -> 结束关卡结算
    /// </summary>
    public enum BattleState
    {
        Intro,
        Playing,
        Question,
        AnswerShow,
        Pause,
        Settle,
        GameOver
    }

    public class BattleManager : MonoBehaviour
    {
        // === 单例 ===
        public static BattleManager Instance { get; private set; }

        // === 属性 ===
        /// <summary>当前对战模式</summary>
        public BattleMode CurrentMode { get; private set; }

        /// <summary>当前战斗状态</summary>
        public BattleState CurrentState { get; private set; }

        /// <summary>当前关卡数据（PVE模式）</summary>
        public StageData CurrentStageData { get; private set; }

        /// <summary>当前题目列表（已从服务端获取）</summary>
        public List<QuestionData> QuestionList { get; private set; }

        /// <summary>当前题目索引</summary>
        public int CurrentQuestionIndex { get; private set; }

        /// <summary>当前得分</summary>
        public int CurrentScore { get; private set; }

        /// <summary>当前连击数</summary>
        public int CurrentCombo { get; private set; }

        /// <summary>最大连击数</summary>
        public int MaxCombo { get; private set; }

        /// <summary>剩余生命数</summary>
        public int RemainingLives { get; private set; }

        /// <summary>正确答题数</summary>
        public int CorrectCount { get; private set; }

        /// <summary>总答题耗时</summary>
        public float TotalAnswerTime { get; private set; }

        /// <summary>是否连击激活（连续答对3题）</summary>
        public bool IsComboActive { get; private set; }

        // === Battle 子系统引用 ===
        [SerializeField] private QuestionDisplay _questionDisplay;
        [SerializeField] private TimerController _timerController;
        [SerializeField] private AnswerHandler _answerHandler;
        [SerializeField] private ComboEffectController _comboEffectController;
        [SerializeField] private BossController _bossController;

        public enum BattleMode { PVE, PVP }

        // === Unity 生命周期 ===
        private void Awake() { }
        private void Start() { }
        private void Update() { }
        private void OnDestroy() { }

        // === 公共方法 ===

        /// <summary>初始化PVE对战（从关卡选择进入）</summary>
        /// <param name="stageId">关卡ID</param>
        public void StartPVEBattle(string stageId) { }

        /// <summary>开始战斗（初始化完成后的流程入口）</summary>
        public void BeginBattle() { }

        /// <summary>显示下一题</summary>
        public void ShowNextQuestion() { }

        /// <summary>提交答案</summary>
        /// <param name="selectedOption">选择的选项 key（A/B/C/D）</param>
        public void SubmitAnswer(string selectedOption) { }

        /// <summary>处理答题结果（服务端返回）</summary>
        public void HandleAnswerResult(bool isCorrect, string correctAnswer, string explanation, int scoreGained) { }

        /// <summary>暂停战斗</summary>
        public void PauseBattle() { }

        /// <summary>恢复战斗</summary>
        public void ResumeBattle() { }

        /// <summary>退出战斗（返回主城）</summary>
        public void QuitBattle() { }

        /// <summary>生命耗尽，游戏结束</summary>
        public void GameOver() { }

        /// <summary>完成所有题目，进入结算</summary>
        public void FinishBattle() { }

        /// <summary>切换战斗状态</summary>
        public void ChangeState(BattleState newState) { }

        // === 私有方法 ===
        private IEnumerator FetchQuestionsFromServer(string stageId) { yield return null; }
        private IEnumerator SubmitAnswerToServer(string questionId, string answer) { yield return null; }
        private IEnumerator SubmitBattleResultToServer() { yield return null; }
        private void UpdateCombo(bool isCorrect) { }
        private void HandleLifeLost() { }
        private void ShowSettlementPanel() { }

        // === 事件 ===
        /// <summary>战斗状态变更事件</summary>
        public event Action<BattleState> OnBattleStateChanged;

        /// <summary>题目切换事件</summary>
        public event Action<QuestionData, int> OnQuestionChanged;

        /// <summary>连击数变更事件</summary>
        public event Action<int> OnComboChanged;

        /// <summary>战斗结束事件（含结算数据）</summary>
        public event Action<StageResultData> OnBattleFinished;
    }
}
