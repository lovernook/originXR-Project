using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using OriginXR.Data;

namespace OriginXR.Battle
{
    public enum BattleState { Intro, Playing, Question, AnswerShow, Pause, Win, Lose }
    public enum BattleMode { PVE, PVP }

    /// <summary>
    /// 战斗总控 — 新规则：
    /// 答对 → 计时重置 + 立即切下一题 + BOSS扣血
    /// 答错 → BOSS攻击 + 玩家扣血 + 展示答案 → 下一题
    /// 玩家HP归零 → 失败  |  全部答对 → BOSS死亡 → 胜利
    /// </summary>
    public class BattleManager : MonoBehaviour
    {
        [Header("子系统")]
        [SerializeField] private QuestionDisplay _questionDisplay;
        [SerializeField] private TimerController _timerController;
        [SerializeField] private AnswerHandler _answerHandler;
        [SerializeField] private ComboEffectController _comboEffectController;
        [SerializeField] private BossController _bossController;
        [SerializeField] private PVEBattleController _pveController;

        // === 单例 ===
        public static BattleManager Instance { get; private set; }

        // === 属性 ===
        public BattleState CurrentState { get; private set; }
        public StageData CurrentStageData { get; private set; }
        public List<QuestionData> QuestionList { get; private set; }
        public int CurrentQuestionIndex { get; private set; } = -1;
        public int CurrentScore { get; private set; }
        public int CurrentCombo { get; private set; }
        public int MaxCombo { get; private set; }
        public int CorrectCount { get; private set; }
        public int TotalQuestionCount => QuestionList?.Count ?? 0;
        public int PlayerHP { get; private set; } = 3;
        public int BossHP { get; private set; }
        public int BossMaxHP { get; private set; }
        public float TotalAnswerTime { get; private set; }

        // === 事件 ===
        public event Action<BattleState> OnBattleStateChanged;
        public event Action<QuestionData, int> OnQuestionChanged;
        public event Action<int> OnComboChanged;
        public event Action<StageResultData> OnBattleFinished;
        public event Action<int, int> OnPlayerHPChanged;      // currentHP, maxHP
        public event Action<int, int> OnBossHPChanged;        // currentHP, maxHP

        // === 内部 ===
        private float _questionStartTime;
        private bool _isWaitingResult;
        private int _playerMaxHP;
        private Coroutine _delayedCoroutine;        // 当前延迟协程

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start() => InitializeSubsystems();
        private void OnDestroy()
        {
            if (_timerController != null) _timerController.OnTimeUp -= OnTimeUp;
            if (_answerHandler != null) _answerHandler.OnResultReceived -= OnResult;
            if (Instance == this) Instance = null;
        }

        private void InitializeSubsystems()
        {
            if (_questionDisplay != null) _questionDisplay.OnAnswerSubmitted += SubmitAnswer;
            if (_timerController != null) _timerController.OnTimeUp += OnTimeUp;
            if (_answerHandler != null) _answerHandler.OnResultReceived += OnResult;
        }

        // === 开始 ===

        public void StartPVEBattle(StageData stageData)
        {
            // 清理上一次战斗的残留协程
            if (_delayedCoroutine != null) { StopCoroutine(_delayedCoroutine); _delayedCoroutine = null; }
            StopAllCoroutines();
            _isWaitingResult = false;

            CurrentStageData = stageData;
            CurrentScore = 0;
            CurrentCombo = 0;
            MaxCombo = 0;
            CorrectCount = 0;
            TotalAnswerTime = 0f;
            _playerMaxHP = PlayerPrefs.GetInt("Diff_PlayerHP", 3);
            PlayerHP = _playerMaxHP;
            BossHP = stageData.bossHP;
            BossMaxHP = BossHP;

            OnPlayerHPChanged?.Invoke(PlayerHP, _playerMaxHP);
            OnBossHPChanged?.Invoke(BossHP, BossMaxHP);

            ChangeState(BattleState.Intro);
            StartCoroutine(FetchQuestions(stageData));
        }

        private IEnumerator FetchQuestions(StageData stageData)
        {
            int qCount = stageData.questionCount;
            int minDiff = PlayerPrefs.GetInt("Diff_MinDifficulty", 1);
            int maxDiff = PlayerPrefs.GetInt("Diff_MaxDifficulty", 3);

            var qm = QuestionManager.Instance;
            if (qm != null && qm.GetTotalCount() > 0)
            {
                QuestionList = qm.GetQuestionsByDifficulty(qCount, minDiff, maxDiff);
                if (QuestionList.Count < qCount)
                {
                    var extra = qm.GetRandomQuestions(qCount - QuestionList.Count);
                    QuestionList.AddRange(extra);
                }
            }
            else
            {
                QuestionList = CreateFallback(qCount);
            }

            Debug.Log($"[Battle] 加载 {QuestionList.Count} 题, BossHP={BossHP}, PlayerHP={PlayerHP}");
            yield return new WaitForSeconds(0.3f);
            BeginBattle();
        }

        private void BeginBattle()
        {
            ChangeState(BattleState.Playing);
            ShowNextQuestion();
        }

        // === 出题 ===

        public void ShowNextQuestion()
        {
            // 已失败则不继续
            if (CurrentState == BattleState.Lose) return;

            CurrentQuestionIndex++;
            if (CurrentQuestionIndex >= TotalQuestionCount) { WinBattle(); return; }

            ChangeState(BattleState.Question);
            var q = QuestionList[CurrentQuestionIndex];
            _questionDisplay?.DisplayQuestion(q, CurrentQuestionIndex + 1, TotalQuestionCount);

            // 每次出新题重置计时
            float t = CurrentStageData?.timePerQuestion ?? 10f;
            _timerController?.StartCountdown(t);

            _questionStartTime = Time.time;
            OnQuestionChanged?.Invoke(q, CurrentQuestionIndex);
        }

        // === 提交答案 ===

        public void SubmitAnswer(string selectedOption)
        {
            if (_isWaitingResult) return;
            _timerController?.Pause();
            _isWaitingResult = true;

            float used = Time.time - _questionStartTime;
            TotalAnswerTime += used;

            var q = QuestionList[CurrentQuestionIndex];
            _answerHandler?.SubmitAnswer(q, selectedOption, used);
        }

        private void OnTimeUp()
        {
            if (!_isWaitingResult) SubmitAnswer("");  // 超时=空白=错
        }

        // === 判定结果 ===

        private void OnResult(bool isCorrect, string correctAnswer, string explanation, int score)
        {
            _isWaitingResult = false;
            var q = QuestionList[CurrentQuestionIndex];
            q.isCorrect = isCorrect;
            q.scoreGained = score;

            if (isCorrect)
            {
                CorrectCount++;
                CurrentScore += score;
                UpdateCombo(true);

                // BOSS 扣血
                BossHP = Mathf.Max(0, BossHP - 1);
                OnBossHPChanged?.Invoke(BossHP, BossMaxHP);

                ChangeState(BattleState.AnswerShow);
                _questionDisplay?.ShowResult(true, correctAnswer, explanation);

                Debug.Log($"[Battle] ✓正确! Combo={CurrentCombo}  BossHP={BossHP}/{BossMaxHP}");

                // 立刻切下一题
                _delayedCoroutine = StartCoroutine(DelayedNext(0.5f));
            }
            else
            {
                UpdateCombo(false);
                PlayerHP--;
                OnPlayerHPChanged?.Invoke(PlayerHP, _playerMaxHP);

                ChangeState(BattleState.AnswerShow);
                _questionDisplay?.ShowResult(false, correctAnswer, explanation);

                // BOSS 攻击动画
                _bossController?.PlayAttack();

                Debug.Log($"[Battle] ✗错误! PlayerHP={PlayerHP}/{_playerMaxHP}");

                if (PlayerHP <= 0)
                {
                    // 取消所有待执行的切题协程
                    if (_delayedCoroutine != null) StopCoroutine(_delayedCoroutine);
                    _delayedCoroutine = StartCoroutine(DelayedLose(1.5f));
                    return;
                }

                // 暂停一下再切题
                _delayedCoroutine = StartCoroutine(DelayedNext(1.5f));
            }
        }

        private IEnumerator DelayedNext(float d)
        {
            yield return new WaitForSeconds(d);
            if (CurrentState != BattleState.Lose)
            {
                ChangeState(BattleState.Playing);
                ShowNextQuestion();
            }
        }

        private IEnumerator DelayedLose(float d)
        {
            yield return new WaitForSeconds(d);
            LoseBattle();
        }

        // === 胜负 ===

        private void WinBattle()
        {
            ChangeState(BattleState.Win);
            BossHP = 0;
            OnBossHPChanged?.Invoke(0, BossMaxHP);

            var result = new StageResultData
            {
                stageName = CurrentStageData?.name ?? "",
                score = CurrentScore,
                correctCount = CorrectCount,
                totalCount = TotalQuestionCount,
                maxCombo = MaxCombo,
                totalTime = TotalAnswerTime,
                starsEarned = CalculateStars(),
                expGained = CurrentStageData?.rewardExp ?? 100,
                goldGained = CurrentStageData?.rewardGold ?? 50,
                isBossDefeated = true
            };

            Debug.Log($"[Battle] 🏆 胜利! 得分={result.score}");
            OnBattleFinished?.Invoke(result);
        }

        private void LoseBattle()
        {
            ChangeState(BattleState.Lose);

            // 失败也生成结算数据
            var result = new StageResultData
            {
                stageName = CurrentStageData?.name ?? "",
                score = CurrentScore,
                correctCount = CorrectCount,
                totalCount = TotalQuestionCount,
                maxCombo = MaxCombo,
                totalTime = TotalAnswerTime,
                starsEarned = CalculateStars(),
                expGained = Mathf.RoundToInt((CurrentStageData?.rewardExp ?? 50) * 0.3f),
                goldGained = Mathf.RoundToInt((CurrentStageData?.rewardGold ?? 30) * 0.3f),
                isBossDefeated = false
            };

            Debug.Log($"[Battle] 💀 失败! 结算: ★{result.starsEarned}");
            OnBattleFinished?.Invoke(result);
        }

        private void UpdateCombo(bool correct)
        {
            if (correct) { CurrentCombo++; if (CurrentCombo > MaxCombo) MaxCombo = CurrentCombo; _comboEffectController?.IncrementCombo(); }
            else { CurrentCombo = 0; _comboEffectController?.ResetCombo(); }
            OnComboChanged?.Invoke(CurrentCombo);
        }

        private int CalculateStars()
        {
            int wrong = TotalQuestionCount - CorrectCount;
            if (wrong == 0) return 3;
            if (wrong == 1) return 2;
            if (wrong == 2) return 1;
            return 0;
        }

        private void ChangeState(BattleState s) { CurrentState = s; OnBattleStateChanged?.Invoke(s); }

        private List<QuestionData> CreateFallback(int count)
        {
            var list = new List<QuestionData>();
            for (int i = 0; i < count; i++)
            {
                string correct = ((char)('A' + (i % 4))).ToString();
                list.Add(new QuestionData {
                    id=$"mock_{i}", type=QuestionType.SingleChoice,
                    content=$"模拟题目 {i+1}", timeLimit=10,
                    devCorrectAnswer=correct, difficulty=1,
                    options = new List<OptionData>{
                        new OptionData{key="A",content="选项A"},
                        new OptionData{key="B",content="选项B"},
                        new OptionData{key="C",content="选项C"},
                        new OptionData{key="D",content="选项D"}
                    },
                    explanation="这是题目解析。" });
            }
            return list;
        }
    }
}
