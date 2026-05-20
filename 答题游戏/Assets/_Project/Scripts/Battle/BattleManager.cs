using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using OriginXR.Data;
using OriginXR.Core;

namespace OriginXR.Battle
{
    /// <summary>
    /// 战斗状态枚举
    /// </summary>
    public enum BattleState
    {
        Intro,       // BOSS出场动画 + 准备
        Playing,     // 答题循环中
        Question,    // 展示题目 + 等待作答
        AnswerShow,  // 展示答案结果（正确/错误动画）
        Pause,       // 暂停（道具/退出确认）
        Settle,      // 结算面板
        GameOver     // 生命耗尽
    }

    public enum BattleMode { PVE, PVP }

    /// <summary>
    /// 战斗总控管理器（单例）
    /// 负责：
    /// 1. 统筹 BattleScene 完整答题战斗流程
    /// 2. 管理战斗状态机，协调各子系统
    /// 3. 与服务端通信：获取题目 -> 提交答案 -> 结算
    /// 4. PVE 专属逻辑（关卡/BOSS/生命数）
    ///
    /// 状态机流程：
    ///   Intro → Playing → Question → (ShowNextQuestion or) AnswerShow → Playing → ... → Settle/GameOver
    /// </summary>
    public class BattleManager : MonoBehaviour
    {
        [Header("战斗子系统")]
        [SerializeField] private QuestionDisplay _questionDisplay;
        [SerializeField] private TimerController _timerController;
        [SerializeField] private AnswerHandler _answerHandler;
        [SerializeField] private ComboEffectController _comboEffectController;
        [SerializeField] private BossController _bossController;
        [SerializeField] private PVEBattleController _pveController;
        [SerializeField] private PVPBattleController _pvpController;

        [Header("结算面板")]
        [SerializeField] private GameObject _settlePanelPrefab;
        [SerializeField] private Transform _settlePanelRoot;

        // === 单例 ===
        public static BattleManager Instance { get; private set; }

        // === 属性 ===
        public BattleMode CurrentMode { get; private set; }
        public BattleState CurrentState { get; private set; }
        public StageData CurrentStageData { get; private set; }
        public List<QuestionData> QuestionList { get; private set; }
        public int CurrentQuestionIndex { get; private set; } = -1;
        public int CurrentScore { get; private set; }
        public int CurrentCombo { get; private set; }
        public int MaxCombo { get; private set; }
        public int RemainingLives { get; private set; } = 3;
        public int CorrectCount { get; private set; }
        public int TotalQuestionCount => QuestionList?.Count ?? 0;
        public float TotalAnswerTime { get; private set; }
        public bool IsComboActive => CurrentCombo >= 3;

        // === 事件 ===
        public event Action<BattleState> OnBattleStateChanged;
        public event Action<QuestionData, int> OnQuestionChanged;
        public event Action<int> OnComboChanged;
        public event Action<StageResultData> OnBattleFinished;

        // === 内部状态 ===
        private float _questionStartTime;
        private bool _isWaitingForServerResult;

        // === Unity 生命周期 ===

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            InitializeSubsystems();
        }

        private void OnDestroy()
        {
            if (_timerController != null) _timerController.OnTimeUp -= OnTimeUp;
            if (_answerHandler != null) _answerHandler.OnResultReceived -= HandleAnswerResult;
            if (_pveController != null) _pveController.OnPVEBattleEnd -= OnPVEBattleEnd;
            if (Instance == this) Instance = null;
        }

        // === 初始化 ===

        private void InitializeSubsystems()
        {
            if (_questionDisplay != null)
                _questionDisplay.OnAnswerSubmitted += SubmitAnswer;

            if (_timerController != null)
                _timerController.OnTimeUp += OnTimeUp;

            if (_answerHandler != null)
                _answerHandler.OnResultReceived += HandleAnswerResult;

            if (_pveController != null)
                _pveController.OnPVEBattleEnd += OnPVEBattleEnd;
        }

        // === 公共方法 ===

        /// <summary>启动 PVE 对战（从关卡选择进入）</summary>
        public void StartPVEBattle(StageData stageData)
        {
            CurrentMode = BattleMode.PVE;
            CurrentStageData = stageData;
            RemainingLives = 3;
            CurrentScore = 0;
            CurrentCombo = 0;
            MaxCombo = 0;
            CorrectCount = 0;
            TotalAnswerTime = 0f;

            if (_pveController != null)
                _pveController.Initialize(stageData);

            ChangeState(BattleState.Intro);
            StartCoroutine(FetchQuestionsFromServer(stageData.id));
        }

        /// <summary>开始战斗（题目加载完成后调用）</summary>
        public void BeginBattle()
        {
            if (QuestionList == null || QuestionList.Count == 0)
            {
                Debug.LogError("[BattleManager] 题目列表为空，无法开始战斗");
                return;
            }

            ChangeState(BattleState.Playing);
            ShowNextQuestion();
        }

        /// <summary>显示下一题</summary>
        public void ShowNextQuestion()
        {
            CurrentQuestionIndex++;

            if (CurrentQuestionIndex >= TotalQuestionCount)
            {
                FinishBattle();
                return;
            }

            ChangeState(BattleState.Question);

            QuestionData question = QuestionList[CurrentQuestionIndex];
            _questionDisplay?.DisplayQuestion(question, CurrentQuestionIndex + 1, TotalQuestionCount);

            if (_timerController != null)
                _timerController.StartCountdown(question.timeLimit > 0 ? question.timeLimit : 10f);

            _questionStartTime = Time.time;
            OnQuestionChanged?.Invoke(question, CurrentQuestionIndex);
        }

        /// <summary>提交答案（由 QuestionDisplay 回调）</summary>
        public void SubmitAnswer(string selectedOption)
        {
            if (_isWaitingForServerResult) return;
            if (_timerController != null) _timerController.Pause();

            _isWaitingForServerResult = true;
            float usedTime = Time.time - _questionStartTime;
            TotalAnswerTime += usedTime;

            QuestionData question = QuestionList[CurrentQuestionIndex];
            _answerHandler?.SubmitAnswer(question.id, selectedOption, usedTime);
        }

        /// <summary>处理答题结果（服务端返回）</summary>
        private void HandleAnswerResult(bool isCorrect, string correctAnswer, string explanation, int scoreGained)
        {
            _isWaitingForServerResult = false;
            QuestionData question = QuestionList[CurrentQuestionIndex];
            question.isCorrect = isCorrect;
            question.scoreGained = scoreGained;

            ChangeState(BattleState.AnswerShow);

            if (isCorrect)
            {
                CorrectCount++;
                CurrentScore += scoreGained;
                UpdateCombo(true);

                if (_pveController != null)
                    _pveController.HandleCorrectAnswer(question.id, scoreGained);
            }
            else
            {
                UpdateCombo(false);

                if (_pveController != null)
                    _pveController.HandleWrongAnswer(question.id);
            }

            // 显示答题结果和解析
            _questionDisplay?.ShowResult(isCorrect, correctAnswer, explanation);

            // 1.5秒后自动显示下一题
            StartCoroutine(DelayedNextQuestion(1.5f));
        }

        private IEnumerator DelayedNextQuestion(float delay)
        {
            yield return new WaitForSeconds(delay);
            ChangeState(BattleState.Playing);
            ShowNextQuestion();
        }

        /// <summary>时间耗尽（自动提交空答案）</summary>
        private void OnTimeUp()
        {
            if (_isWaitingForServerResult) return;
            SubmitAnswer("");
        }

        /// <summary>更新连击</summary>
        private void UpdateCombo(bool isCorrect)
        {
            if (isCorrect)
            {
                CurrentCombo++;
                if (CurrentCombo > MaxCombo) MaxCombo = CurrentCombo;
                _comboEffectController?.IncrementCombo();
            }
            else
            {
                CurrentCombo = 0;
                _comboEffectController?.ResetCombo();
            }
            OnComboChanged?.Invoke(CurrentCombo);
        }

        /// <summary>PVE 战斗结束</summary>
        private void OnPVEBattleEnd(BattleEndReason reason)
        {
            if (reason == BattleEndReason.LivesExhausted)
            {
                ChangeState(BattleState.GameOver);
                ShowGameOverPanel();
            }
            else if (reason == BattleEndReason.BossDefeated || reason == BattleEndReason.QuestionsDone)
            {
                FinishBattle();
            }
        }

        /// <summary>完成战斗，进入结算</summary>
        public void FinishBattle()
        {
            ChangeState(BattleState.Settle);

            StartCoroutine(SubmitBattleResultToServer());
        }

        /// <summary>切换战斗状态</summary>
        public void ChangeState(BattleState newState)
        {
            CurrentState = newState;
            OnBattleStateChanged?.Invoke(newState);
        }

        // === 服务端通信 ===

        private IEnumerator FetchQuestionsFromServer(string stageId)
        {
            Debug.Log($"[BattleManager] 正在获取关卡题目: {stageId}");
            // 模拟：实际应通过 HttpManager 请求服务端
            // 此处使用模拟数据使演示可用
            QuestionList = CreateMockQuestions(stageId);
            yield return new WaitForSeconds(0.5f);

            if (_bossController != null && CurrentStageData != null)
            {
                _bossController.Initialize(CurrentStageData.bossModelId, CurrentStageData.bossName, CurrentStageData.bossHP);
            }

            BeginBattle();
        }

        private IEnumerator SubmitBattleResultToServer()
        {
            var result = new StageResultData
            {
                stageId = CurrentStageData?.id ?? "",
                stageName = CurrentStageData?.name ?? "",
                score = CurrentScore,
                correctCount = CorrectCount,
                totalCount = TotalQuestionCount,
                maxCombo = MaxCombo,
                totalTime = TotalAnswerTime,
                starsEarned = CalculateStars(),
                expGained = CurrentStageData?.rewardExp ?? 100,
                goldGained = CurrentStageData?.rewardGold ?? 50,
                isBossDefeated = _bossController?.IsDefeated() ?? false,
                weakKnowledgePoints = new List<string>(),
                completedAt = DateTime.UtcNow
            };

            // 发送结算数据到服务端（通过 HttpManager）
            // HttpManager.Instance.Post("game/stages/" + result.stageId + "/finish", result, ...);

            Debug.Log($"[BattleManager] 战斗结算: 得分{result.score} 正确{result.correctCount}/{result.totalCount} 连击{result.maxCombo} ★{result.starsEarned}");
            yield return new WaitForSeconds(0.3f);

            // 显示结算面板
            ShowSettlementPanel(result);
            OnBattleFinished?.Invoke(result);
        }

        // === UI 面板 ===

        private void ShowSettlementPanel(StageResultData result)
        {
            if (_settlePanelPrefab == null || _settlePanelRoot == null) return;
            GameObject panel = Instantiate(_settlePanelPrefab, _settlePanelRoot);
            // TODO: 绑定结算数据到 UI 控件
        }

        private void ShowGameOverPanel()
        {
            Debug.Log("[BattleManager] 生命耗尽，游戏结束");
            UI.PopupManager.Instance?.ShowConfirm("挑战失败", "生命已耗尽！是否重新挑战？",
                () => { StartPVEBattle(CurrentStageData); },
                () => { Core.SceneLoader.Instance?.LoadScene("LobbyScene"); },
                "重新挑战", "返回主城");
        }

        /// <summary>计算获得星数（0~3）</summary>
        private int CalculateStars()
        {
            if (CurrentStageData?.starConditions == null) return 3;
            var result = new StageResultData
            {
                correctCount = CorrectCount,
                totalCount = TotalQuestionCount,
                maxCombo = MaxCombo,
                totalTime = TotalAnswerTime
            };

            int stars = 0;
            foreach (var cond in CurrentStageData.starConditions)
            {
                if (cond.IsMet(result)) stars++;
            }
            return Mathf.Min(stars, 3);
        }

        // === 模拟题目数据（开发阶段使用） ===

        private List<QuestionData> CreateMockQuestions(string stageId)
        {
            return new List<QuestionData>
            {
                new QuestionData {
                    id = Guid.NewGuid().ToString(), type = QuestionType.SingleChoice, content = "Unity中，以下哪个组件用于控制3D角色的移动？",
                    difficulty = 1, timeLimit = 10, explanation = "CharacterController 是Unity中用于控制角色移动的专用组件。",
                    options = new List<OptionData> {
                        new OptionData { key = "A", content = "CharacterController" },
                        new OptionData { key = "B", content = "BoxCollider" },
                        new OptionData { key = "C", content = "Rigidbody" },
                        new OptionData { key = "D", content = "MeshRenderer" }
                    }
                },
                new QuestionData {
                    id = Guid.NewGuid().ToString(), type = QuestionType.SingleChoice, content = "C# 中，以下哪个关键字用于定义接口？",
                    difficulty = 2, timeLimit = 10, explanation = "interface 关键字用于定义接口。",
                    options = new List<OptionData> {
                        new OptionData { key = "A", content = "class" },
                        new OptionData { key = "B", content = "interface" },
                        new OptionData { key = "C", content = "struct" },
                        new OptionData { key = "D", content = "abstract" }
                    }
                },
                new QuestionData {
                    id = Guid.NewGuid().ToString(), type = QuestionType.TrueFalse, content = "Unity 的 Time.deltaTime 表示上一帧到当前帧的时间间隔。",
                    difficulty = 1, timeLimit = 8, explanation = "Time.deltaTime 确实表示每帧的时间间隔，常用于平滑运动计算。",
                    options = new List<OptionData> {
                        new OptionData { key = "T", content = "正确" },
                        new OptionData { key = "F", content = "错误" }
                    }
                }
            };
        }
    }
}
