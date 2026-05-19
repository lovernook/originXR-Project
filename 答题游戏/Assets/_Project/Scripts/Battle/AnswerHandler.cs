using UnityEngine;
using System;
using System.Collections;
using OriginXR.Data;

namespace OriginXR.Battle
{
    /// <summary>
    /// 答题处理器
    /// 负责：
    /// 1. 接收玩家选择的答案
    /// 2. 发送答案至服务端进行校验（不本地判断对错，防止作弊）
    /// 3. 接收服务端返回的答题结果（正确/错误/得分/解析）
    /// 4. 触发答题结果展示流程（通知 BattleManager -> QuestionDisplay 显示反馈）
    ///
    /// 防作弊设计原则：
    ///   1. 客户端不存储正确答案
    ///   2. 每次答题由服务端实时判定
    ///   3. 服务端记录答题时间戳，异常快速作答标记
    ///   4. 服务端验证答题时序（是否在题目展示后作答）
    ///
    /// API: POST /api/v1/game/stages/:id/answer
    ///   Body: { questionId, selectedOption, timestampMs }
    ///   Response: { isCorrect, correctAnswer, explanation, scoreGained, comboBonus }
    /// </summary>
    public class AnswerHandler : MonoBehaviour
    {
        // === 状态 ===
        private bool _isWaitingForServer;        // 等待服务端响应中
        private string _pendingQuestionId;        // 待判定的题目ID
        private string _pendingAnswer;            // 待判定的答案

        // === Unity 生命周期 ===
        private void Start() { }

        // === 公共方法 ===

        /// <summary>提交答案至服务端</summary>
        /// <param name="questionId">题目ID</param>
        /// <param name="selectedOption">选择的选项（A/B/C/D）</param>
        /// <param name="onResult">结果回调（isCorrect, correctAnswer, explanation, scoreGained）</param>
        public void SubmitAnswer(string questionId, string selectedOption, Action<bool, string, string, int> onResult) { }

        /// <summary>取消等待（超时/退出）</summary>
        public void CancelPending() { }

        /// <summary>是否正在等待服务端响应</summary>
        public bool IsWaiting() { return _isWaitingForServer; }

        // === 私有方法 ===
        private IEnumerator SendAnswerCoroutine(string questionId, string selectedOption, Action<bool, string, string, int> onResult) { yield return null; }
        private void HandleServerError(string errorMsg) { }

        // === 事件 ===
        /// <summary>服务端响应超时事件</summary>
        public event Action<string> OnServerTimeout;
    }
}
