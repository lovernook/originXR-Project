using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using OriginXR.Data;

namespace OriginXR.Battle
{
    public enum BattleState { Intro, Playing, Question, AnswerShow, Win, Lose }

    public class BattleManager : MonoBehaviour
    {
        public static BattleManager Instance { get; private set; }

        [Header("引用")]
        public QuestionDisplay questionDisplay;
        public TimerController timerController;
        public AnswerHandler answerHandler;
        public ComboEffectController comboEffect;
        public BossController bossController;

        public BattleState CurrentState { get; private set; }
        public List<QuestionData> QuestionList { get; private set; } = new List<QuestionData>();
        public int QuestionIndex { get; private set; } = -1;
        public int Score { get; private set; }
        public int Combo { get; private set; }
        public int MaxCombo { get; private set; }
        public int Correct { get; private set; }
        public int Total => QuestionList.Count;
        public int PlayerHP { get; private set; }
        public int PlayerMaxHP { get; private set; }
        public int BossHP { get; private set; }
        public int BossMaxHP { get; private set; }

        public event Action<int> OnComboChanged;
        public event Action<StageResultData> OnBattleFinished;
        public event Action<int, int> OnPlayerHPChanged;
        public event Action<int, int> OnBossHPChanged;
        public event Action OnScoreUpdated;

        private bool _busy;
        private Coroutine _delayRoutine;
        private StageData _stageData;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            if (questionDisplay != null) questionDisplay.OnAnswerSubmitted += OnAnswer;
            if (timerController != null) timerController.OnTimeUp += OnTimeUp;
            if (answerHandler != null) answerHandler.OnResult += OnResult;
            Invoke(nameof(AutoStart), 0.5f);
        }

        private void OnDestroy()
        {
            if (questionDisplay != null) questionDisplay.OnAnswerSubmitted -= OnAnswer;
            if (timerController != null) timerController.OnTimeUp -= OnTimeUp;
            if (answerHandler != null) answerHandler.OnResult -= OnResult;
            if (Instance == this) Instance = null;
        }

        public void Restart() => AutoStart();

        private void AutoStart()
        {
            int qCount = PlayerPrefs.GetInt("Diff_QuestionCount", 5);
            int bossHP = PlayerPrefs.GetInt("Diff_BossHP", qCount);
            int playerHP = PlayerPrefs.GetInt("Diff_PlayerHP", 3);
            int timeLmt = PlayerPrefs.GetInt("Diff_TimeLimit", 10);
            int expR = PlayerPrefs.GetInt("Diff_ExpReward", 100);
            int goldR = PlayerPrefs.GetInt("Diff_GoldReward", 50);
            string name = PlayerPrefs.GetString("Diff_Name", "普通");

            _stageData = new StageData { name = $"{name}难度", bossName = "恶龙", bossHP = bossHP, questionCount = qCount, timePerQuestion = timeLmt, rewardExp = expR, rewardGold = goldR };
            StartBattle();
        }

        private void StartBattle()
        {
            if (_delayRoutine != null) { StopCoroutine(_delayRoutine); _delayRoutine = null; }
            StopAllCoroutines(); _busy = false;
            Score = 0; Combo = 0; MaxCombo = 0; Correct = 0; QuestionIndex = -1;
            PlayerMaxHP = PlayerPrefs.GetInt("Diff_PlayerHP", 3); PlayerHP = PlayerMaxHP;
            BossHP = _stageData.bossHP; BossMaxHP = BossHP;
            OnPlayerHPChanged?.Invoke(PlayerHP, PlayerMaxHP);
            OnBossHPChanged?.Invoke(BossHP, BossMaxHP);
            OnComboChanged?.Invoke(0); OnScoreUpdated?.Invoke();

            var qm = QuestionManager.Instance;
            if (qm != null && qm.GetTotalCount() > 0)
            {
                int minD = PlayerPrefs.GetInt("Diff_MinDifficulty", 1);
                int maxD = PlayerPrefs.GetInt("Diff_MaxDifficulty", 3);
                QuestionList = qm.GetQuestionsByDifficulty(_stageData.questionCount, minD, maxD);
                if (QuestionList.Count < _stageData.questionCount)
                    QuestionList.AddRange(qm.GetRandomQuestions(_stageData.questionCount - QuestionList.Count));
            }
            else QuestionList = MakeFallback(_stageData.questionCount);

            Debug.Log($"[Battle] {_stageData.name} | {QuestionList.Count}题 | HP={PlayerHP} | BossHP={BossHP}");
            NextQuestion();
        }

        private void NextQuestion()
        {
            QuestionIndex++;
            if (QuestionIndex >= Total) { EndBattle(true); return; }
            CurrentState = BattleState.Question;
            var q = QuestionList[QuestionIndex];
            questionDisplay?.Show(q, QuestionIndex + 1, Total);
            timerController?.StartCountdown(q.timeLimit > 0 ? q.timeLimit : 10f);
        }

        private void OnAnswer(string option) { if (_busy) return; timerController?.Pause(); _busy = true; answerHandler?.Submit(QuestionList[QuestionIndex], option); }
        private void OnTimeUp() { if (!_busy) OnAnswer(""); }

        private void OnResult(bool isCorrect, string correctAns, string explanation, int pts)
        {
            _busy = false;
            if (isCorrect)
            {
                Correct++; Score += pts; UpdateCombo(true);
                BossHP = Mathf.Max(0, BossHP - 1); OnBossHPChanged?.Invoke(BossHP, BossMaxHP); OnScoreUpdated?.Invoke();
                questionDisplay?.ShowResult(true, correctAns, explanation);
                _delayRoutine = StartCoroutine(DelayCall(0.4f, NextQuestion));
            }
            else
            {
                UpdateCombo(false); PlayerHP--; OnPlayerHPChanged?.Invoke(PlayerHP, PlayerMaxHP);
                questionDisplay?.ShowResult(false, correctAns, explanation); bossController?.Attack();
                if (PlayerHP <= 0) { if (_delayRoutine != null) StopCoroutine(_delayRoutine); _delayRoutine = StartCoroutine(DelayCall(1.5f, () => EndBattle(false))); return; }
                _delayRoutine = StartCoroutine(DelayCall(1.5f, NextQuestion));
            }
        }

        private IEnumerator DelayCall(float d, Action cb) { yield return new WaitForSeconds(d); cb?.Invoke(); }

        private void EndBattle(bool win)
        {
            int wrong = Total - Correct;
            int stars = wrong == 0 ? 3 : wrong == 1 ? 2 : wrong == 2 ? 1 : 0;
            var result = new StageResultData
            {
                stageName = _stageData?.name ?? "", score = Score, correctCount = Correct, totalCount = Total,
                maxCombo = MaxCombo, starsEarned = stars,
                expGained = win ? (_stageData?.rewardExp ?? 100) : 0, goldGained = win ? (_stageData?.rewardGold ?? 50) : 0,
                isBossDefeated = win
            };
            CurrentState = win ? BattleState.Win : BattleState.Lose;
            OnBattleFinished?.Invoke(result);
        }

        private void UpdateCombo(bool correct) { if (correct) { Combo++; if (Combo > MaxCombo) MaxCombo = Combo; comboEffect?.Add(); } else { Combo = 0; comboEffect?.Reset(); } OnComboChanged?.Invoke(Combo); }

        private List<QuestionData> MakeFallback(int count)
        {
            var list = new List<QuestionData>();
            for (int i = 0; i < count; i++)
            {
                string c = ((char)('A' + (i % 4))).ToString();
                list.Add(new QuestionData { id = $"fb_{i}", type = QuestionType.SingleChoice, content = $"模拟题目 {i + 1}", timeLimit = 10, devCorrectAnswer = c, difficulty = 1, options = new List<OptionData> { new OptionData { key = "A", content = "正确答案" }, new OptionData { key = "B", content = "错误选项" }, new OptionData { key = "C", content = "错误选项" }, new OptionData { key = "D", content = "错误选项" } }, explanation = "题目解析" });
            }
            return list;
        }
    }
}
