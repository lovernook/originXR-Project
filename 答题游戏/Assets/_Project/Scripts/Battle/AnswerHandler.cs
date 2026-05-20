using UnityEngine;
using System;
using System.Collections;
using OriginXR.Core;

namespace OriginXR.Battle
{
    /// <summary>
    /// 答题处理器
    /// 负责：
    /// 1. 将玩家答案提交至服务端进行校验（服务端判定对错，防作弊）
    /// 2. 接收服务端返回的答题结果（正确/错误/得分/正确答案/解析）
    /// 3. 超时处理（服务端响应超时视为答错）
    ///
    /// API 接口：POST /api/v1/game/stages/:id/answer
    ///   请求体：{ questionId, selectedOption, usedTime, timestampMs }
    ///   响应体：{ isCorrect, correctAnswer, explanation, scoreGained, comboBonus }
    /// </summary>
    public class AnswerHandler : MonoBehaviour
    {
        [Header("配置")]
        [SerializeField] private float _serverTimeout = 5f;        // 服务端响应超时（秒）
        [SerializeField] private bool _useLocalJudge = true;       // 开发阶段使用本地判定（生产环境为 false）

        // === 状态 ===
        private bool _isWaiting;
        private string _pendingQuestionId;
        private Coroutine _pendingCoroutine;

        // === 事件 ===
        /// <summary>答题结果回调：isCorrect, correctAnswer, explanation, scoreGained</summary>
        public event Action<bool, string, string, int> OnResultReceived;
        /// <summary>服务端响应超时</summary>
        public event Action<string> OnServerTimeout;

        // === Unity 生命周期 ===

        private void Start()
        {
            Debug.Log($"[AnswerHandler] 答题处理器已初始化 (本地判定: {_useLocalJudge})");
        }

        private void OnDestroy()
        {
            CancelPending();
        }

        // === 公共方法 ===

        /// <summary>提交答案到服务端校验</summary>
        /// <param name="questionId">题目ID</param>
        /// <param name="selectedOption">玩家选择的选项（A/B/C/D 或 ""=超时未答）</param>
        /// <param name="usedTime">答题耗时（秒）</param>
        public void SubmitAnswer(string questionId, string selectedOption, float usedTime)
        {
            if (_isWaiting)
            {
                Debug.LogWarning("[AnswerHandler] 正在等待上一题的判定结果");
                return;
            }

            _isWaiting = true;
            _pendingQuestionId = questionId;

            if (_useLocalJudge)
            {
                // 开发阶段：本地判定（实际应该由服务端判定）
                _pendingCoroutine = StartCoroutine(LocalJudgeRoutine(questionId, selectedOption, usedTime));
            }
            else
            {
                // 生产环境：发送到服务端判定
                _pendingCoroutine = StartCoroutine(SendToServerRoutine(questionId, selectedOption, usedTime));
            }
        }

        /// <summary>取消等待中的判定</summary>
        public void CancelPending()
        {
            if (_pendingCoroutine != null)
            {
                StopCoroutine(_pendingCoroutine);
                _pendingCoroutine = null;
            }
            _isWaiting = false;
        }

        /// <summary>是否正在等待服务端响应</summary>
        public bool IsWaiting() => _isWaiting;

        // === 私有：本地判定（开发阶段） ===

        /// <summary>
        /// 本地判定逻辑（仅开发阶段使用，生产环境删除）
        /// 规则：
        ///   - 答案非空且以 A/B/C/D 开头 → 正确
        ///   - 判断题 T → 正确
        ///   - 不匹配 → 错误
        /// </summary>
        private IEnumerator LocalJudgeRoutine(string questionId, string selectedOption, float usedTime)
        {
            // 模拟网络延迟
            yield return new WaitForSeconds(UnityEngine.Random.Range(0.1f, 0.3f));

            // 简易判定逻辑
            bool isCorrect = false;
            string correctAnswer = "A";
            string explanation = "这是题目的详细解析。请仔细阅读，理解知识点。";
            int scoreGained = 100;

            if (!string.IsNullOrEmpty(selectedOption))
            {
                // 模拟：答案以 A/B/C/D/T 开头为正确
                char firstChar = selectedOption[0];
                isCorrect = (firstChar >= 'A' && firstChar <= 'D') || firstChar == 'T';
                correctAnswer = "A";
                scoreGained = isCorrect ? 100 : 0;
            }
            else
            {
                // 超时未答 = 错误
                isCorrect = false;
                scoreGained = 0;
            }

            _isWaiting = false;
            OnResultReceived?.Invoke(isCorrect, correctAnswer, explanation, scoreGained);
        }

        // === 私有：服务端判定（生产环境） ===

        private IEnumerator SendToServerRoutine(string questionId, string selectedOption, float usedTime)
        {
            // 构造请求体
            var body = new AnswerSubmitRequest
            {
                questionId = questionId,
                selectedOption = selectedOption,
                usedTime = usedTime,
                timestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            bool hasResponded = false;

            // 发送 POST 请求
            HttpManager.Instance?.Post<ApiAnswerResponse>(
                "/game/stages/answer",
                body,
                (response) =>
                {
                    hasResponded = true;
                    _isWaiting = false;
                    OnResultReceived?.Invoke(
                        response.data.isCorrect,
                        response.data.correctAnswer,
                        response.data.explanation,
                        response.data.scoreGained
                    );
                },
                (error) =>
                {
                    hasResponded = true;
                    _isWaiting = false;
                    Debug.LogError($"[AnswerHandler] 服务端判定失败: {error}");
                    // 失败时视为答错
                    OnResultReceived?.Invoke(false, "A", "服务端判定异常，请重试", 0);
                }
            );

            // 等待超时
            float elapsed = 0f;
            while (!hasResponded && elapsed < _serverTimeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (!hasResponded)
            {
                CancelPending();
                _isWaiting = false;
                Debug.LogWarning("[AnswerHandler] 服务端判定超时");
                OnServerTimeout?.Invoke(questionId);
                OnResultReceived?.Invoke(false, "A", "判定超时", 0);
            }
        }

        // === 内部数据类型 ===

        [Serializable]
        private class AnswerSubmitRequest
        {
            public string questionId;
            public string selectedOption;
            public float usedTime;
            public long timestampMs;
        }

        [Serializable]
        private class ApiAnswerResponse
        {
            public AnswerResultData data;
        }

        [Serializable]
        private class AnswerResultData
        {
            public bool isCorrect;
            public string correctAnswer;
            public string explanation;
            public int scoreGained;
            public int comboBonus;
        }
    }
}
