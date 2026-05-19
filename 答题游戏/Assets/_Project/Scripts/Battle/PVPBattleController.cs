using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using OriginXR.Data;
using OriginXR.Core;

namespace OriginXR.Battle
{
    /// <summary>
    /// PVP 对战逻辑控制器（预留 - 当前阶段暂不开发）
    /// 负责：
    /// 1. 管理 PVP 实时对战逻辑（1v1 知识竞技场）
    /// 2. 基于 WebSocket 帧同步：PVPSyncFrame (Protobuf)
    /// 3. 双方同屏答题，限时10秒，答对得分，答错扣分
    /// 4. 道具系统集成：跳过卡/加倍卡/冻结卡
    /// 5. ELO 评分结算
    ///
    /// WebSocket 事件流：
    ///   pvp:match_found      -> 匹配成功
    ///   pvp:question         -> 推送题目
    ///   pvp:answer           -> 提交答案
    ///   pvp:opponent_status  -> 对手答题状态
    ///   pvp:result           -> 单题结果
    ///   pvp:match_end        -> 对局结束
    ///   pvp:use_item         -> 使用道具
    ///   pvp:item_effect      -> 道具效果通知
    ///
    /// 匹配流程：
    ///   1. POST /api/v1/game/pvp/join-queue  -> 加入匹配队列
    ///   2. WebSocket pvp:match_found         -> 匹配成功
    ///   3. 逐题对战 (WebSocket 帧同步)
    ///   4. 服务端判定胜负 -> 更新 ELO
    ///
    /// 当前状态：暂不开发，仅保留接口定义。
    /// 待 PVP 功能启动时实现具体逻辑。
    /// </summary>
    public class PVPBattleController : MonoBehaviour
    {
        // === 单例 ===
        public static PVPBattleController Instance { get; private set; }

        // === 属性 ===
        /// <summary>PVP 对战状态</summary>
        public PVPState CurrentState { get; private set; }

        /// <summary>当前对局ID</summary>
        public string MatchId { get; private set; }

        /// <summary>我的得分</summary>
        public int MyScore { get; private set; }

        /// <summary>对手得分</summary>
        public int OpponentScore { get; private set; }

        /// <summary>对手信息</summary>
        public OpponentInfo Opponent { get; private set; }

        /// <summary>剩余道具数量 { 跳过卡: n, 加倍卡: n, 冻结卡: n }</summary>
        public Dictionary<string, int> RemainingItems { get; private set; }

        /// <summary>当前帧索引（用于帧同步）</summary>
        public int CurrentFrameIndex { get; private set; }

        // === 配置 ===
        [SerializeField] private float _timePerQuestion = 10f;     // 每题限时10秒

        public enum PVPState
        {
            Idle,           // 空闲
            Matching,       // 匹配中
            Ready,          // 准备开始
            InProgress,     // 对战中
            Finished,       // 对局结束
            Disconnected    // 断线重连中
        }

        [Serializable]
        public class OpponentInfo
        {
            public string PlayerId;
            public string Username;
            public string RankTier;
            public int EloScore;
            public string AvatarId;
        }

        // === Unity 生命周期 ===
        private void Awake() { }
        private void Start() { }
        private void OnDestroy() { }

        // === 公共方法 ===

        /// <summary>加入匹配队列</summary>
        public void JoinMatchQueue() { }

        /// <summary>取消匹配</summary>
        public void CancelMatchQueue() { }

        /// <summary>提交答案（含时间戳用于判定先后）</summary>
        public void SubmitAnswer(string questionId, string selectedOption) { }

        /// <summary>使用道具</summary>
        /// <param name="itemId">道具ID</param>
        /// <param name="targetPlayerId">目标玩家ID</param>
        public void UseItem(string itemId, string targetPlayerId) { }

        /// <summary>发送准备信号</summary>
        public void SendReady() { }

        /// <summary>断线重连</summary>
        public void Reconnect(string matchId) { }

        /// <summary>主动放弃对战</summary>
        public void Forfeit() { }

        // === WebSocket 事件处理（由 NetworkManager 回调） ===

        private void OnMatchFound(string jsonData) { }
        private void OnQuestionReceived(string jsonData) { }
        private void OnOpponentStatus(string jsonData) { }
        private void OnResultReceived(string jsonData) { }
        private void OnMatchEnd(string jsonData) { }
        private void OnItemEffect(string jsonData) { }

        // === 私有方法 ===
        private void RegisterPVPEvents() { }
        private void UnregisterPVPEvents() { }
        private void UpdateScores(bool isMineCorrect, int scoreDelta) { }
        private void ShowMatchEndPanel(int myFinalScore, int opponentFinalScore, int eloChange) { }
        private IEnumerator ReconnectRoutine(string matchId) { yield return null; }

        // === 事件 ===
        /// <summary>匹配成功事件</summary>
        public event Action<OpponentInfo> OnMatchFoundEvent;

        /// <summary>对局结束事件</summary>
        public event Action<int, int, int> OnMatchEndEvent;

        /// <summary>被道具攻击事件</summary>
        public event Action<string, string> OnItemUsedOnMe;
    }
}
