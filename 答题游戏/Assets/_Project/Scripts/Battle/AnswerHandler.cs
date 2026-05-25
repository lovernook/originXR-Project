using UnityEngine;
using System;
using System.Collections;
using OriginXR.Core;

namespace OriginXR.Battle
{
    /// <summary>
    /// 答题处理器
    /// 开发阶段：使用题目内置的 devCorrectAnswer 进行本地判定
    /// 生产环境：通过 HttpManager 提交服务端判定
    /// </summary>
    public class AnswerHandler : MonoBehaviour
    {
        [Header("配置")]
        [SerializeField] private float _serverTimeout = 5f;
        [SerializeField] private bool _useLocalJudge = true;

        // === 状态 ===
        private bool _isWaiting;
        private string _pendingQuestionId;
        private Coroutine _pendingCoroutine;

        // === 事件 ===
        /// <summary>答题结果回调</summary>
        public event Action<bool, string, string, int> OnResultReceived;
        /// <summary>服务端超时</summary>
        public event Action<string> OnServerTimeout;

        private void Start()
        {
            Debug.Log($"[AnswerHandler] 答题处理器已初始化 (本地判定: {_useLocalJudge})");
        }

        private void OnDestroy()
        {
            CancelPending();
        }

        /// <summary>提交答案</summary>
        /// <param name="question">题目数据（含 devCorrectAnswer）</param>
        /// <param name="selectedOption">玩家选择的选项</param>
        /// <param name="usedTime">答题耗时</param>
        public void SubmitAnswer(Data.QuestionData question, string selectedOption, float usedTime)
        {
            if (_isWaiting)
            {
                Debug.LogWarning("[AnswerHandler] 正在等待判定结果");
                return;
            }

            _isWaiting = true;
            _pendingQuestionId = question.id;

            if (_useLocalJudge)
            {
                _pendingCoroutine = StartCoroutine(LocalJudgeRoutine(question, selectedOption, usedTime));
            }
            else
            {
                _pendingCoroutine = StartCoroutine(SendToServerRoutine(question.id, selectedOption, usedTime));
            }
        }

        public void CancelPending()
        {
            if (_pendingCoroutine != null) StopCoroutine(_pendingCoroutine);
            _isWaiting = false;
        }

        public bool IsWaiting() => _isWaiting;

        // === 本地判定 ===

        private IEnumerator LocalJudgeRoutine(Data.QuestionData question, string selectedOption, float usedTime)
        {
            yield return new WaitForSeconds(UnityEngine.Random.Range(0.1f, 0.3f));

            string correctAnswer = question.devCorrectAnswer;
            bool isCorrect;

            if (string.IsNullOrEmpty(correctAnswer))
            {
                // 没有正确答案数据，暂时任何非空回答都算对
                Debug.LogWarning($"[AnswerHandler] 题目 {question.id} 缺少正确答案，暂判为正确");
                isCorrect = !string.IsNullOrEmpty(selectedOption);
            }
            else
            {
                isCorrect = !string.IsNullOrEmpty(selectedOption)
                    && selectedOption.Trim().Equals(correctAnswer.Trim(), StringComparison.OrdinalIgnoreCase);
            }

            int baseScore = question.difficulty * 100;
            int scoreGained = isCorrect ? baseScore : 0;

            _isWaiting = false;
            OnResultReceived?.Invoke(isCorrect, correctAnswer, question.explanation ?? "", scoreGained);

            Debug.Log($"[AnswerHandler] 判定: 选={selectedOption} 正确答案={correctAnswer} → {(isCorrect ? "✓正确" : "✗错误")} +{scoreGained}分");
        }

        // === 服务端判定（待对接） ===

        private IEnumerator SendToServerRoutine(string questionId, string selectedOption, float usedTime)
        {
            bool hasResponded = false;
            float elapsed = 0f;

            // TODO: HttpManager.Instance.Post(...)
            while (!hasResponded && elapsed < _serverTimeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (!hasResponded)
            {
                _isWaiting = false;
                OnServerTimeout?.Invoke(questionId);
                OnResultReceived?.Invoke(false, "A", "判定超时", 0);
            }
        }
    }
}
