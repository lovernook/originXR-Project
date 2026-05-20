using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace OriginXR.Battle
{
    /// <summary>
    /// PVP 状态
    /// </summary>
    public enum PVPState
    {
        Idle,           // 空闲
        Matching,       // 匹配中
        Ready,          // 准备开始
        InProgress,     // 对战中
        Finished,       // 对局结束
        Disconnected    // 断线重连中
    }

    /// <summary>
    /// PVP 对战逻辑控制器（预留模块）
    /// 负责：
    /// 1. 管理 PVP 实时对战流程（1v1 知识竞技场）
    /// 2. WebSocket 帧同步：pvp:* 事件
    /// 3. 道具使用（跳过卡/加倍卡/冻结卡）
    /// 4. ELO 评分结算
    ///
    /// 当前状态：接口已定义，具体实现待 PVP 功能启动时完成。
    ///           开发阶段 BattleManager 默认使用 PVE 模式。
    /// </summary>
    public class PVPBattleController : MonoBehaviour
    {
        // === 单例 ===
        public static PVPBattleController Instance { get; private set; }

        [Header("配置")]
        [SerializeField] private float _timePerQuestion = 10f;
        [SerializeField] private int _questionsPerMatch = 5;

        // === 属性 ===
        public PVPState CurrentState { get; private set; } = PVPState.Idle;
        public string MatchId { get; private set; }
        public int MyScore { get; private set; }
        public int OpponentScore { get; private set; }
        public OpponentInfo Opponent { get; private set; }
        public Dictionary<string, int> RemainingItems { get; private set; } = new Dictionary<string, int>();
        public int CurrentFrameIndex { get; private set; }

        // === 内部状态 ===
        private Coroutine _matchTimeoutRoutine;
        private const float MatchTimeout = 60f;

        [Serializable]
        public class OpponentInfo
        {
            public string playerId;
            public string username;
            public string rankTier;
            public int eloScore;
            public string avatarId;
        }

        // === 事件 ===
        public event Action<OpponentInfo> OnMatchFoundEvent;
        public event Action<int, int, int> OnMatchEndEvent;      // myScore, opponentScore, eloChange
        public event Action<string, string> OnItemUsedOnMe;       // itemId, fromPlayerId
        public event Action<PVPState> OnStateChanged;

        // === Unity 生命周期 ===

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            RegisterPVPEvents();
        }

        private void OnDestroy()
        {
            UnregisterPVPEvents();
            if (Instance == this) Instance = null;
        }

        // === 公共方法（接口已定义，具体实现待开发） ===

        /// <summary>加入匹配队列</summary>
        public void JoinMatchQueue()
        {
            Debug.Log("[PVPBattle] 加入匹配队列...");
            ChangeState(PVPState.Matching);

            // TODO: POST /api/v1/game/pvp/join-queue
            // TODO: 启动匹配超时协程

            // 模拟匹配超时降级（开发阶段）
            _matchTimeoutRoutine = StartCoroutine(MatchTimeoutRoutine());
        }

        /// <summary>取消匹配</summary>
        public void CancelMatchQueue()
        {
            Debug.Log("[PVPBattle] 取消匹配");
            if (_matchTimeoutRoutine != null) StopCoroutine(_matchTimeoutRoutine);
            ChangeState(PVPState.Idle);

            // TODO: POST /api/v1/game/pvp/cancel-queue
        }

        /// <summary>提交答案（含时间戳）</summary>
        public void SubmitAnswer(string questionId, string selectedOption)
        {
            if (CurrentState != PVPState.InProgress) return;

            long timestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            string jsonData = $"{{\"questionId\":\"{questionId}\",\"selectedOption\":\"{selectedOption}\",\"timestampMs\":{timestampMs}}}";
            Core.NetworkManager.Instance?.Send("pvp:answer", jsonData);
        }

        /// <summary>使用道具</summary>
        public void UseItem(string itemId, string targetPlayerId = "")
        {
            if (CurrentState != PVPState.InProgress) return;

            if (RemainingItems.ContainsKey(itemId) && RemainingItems[itemId] > 0)
            {
                RemainingItems[itemId]--;
                string jsonData = $"{{\"itemId\":\"{itemId}\",\"targetPlayerId\":\"{targetPlayerId}\"}}";
                Core.NetworkManager.Instance?.Send("pvp:use_item", jsonData);
            }
        }

        /// <summary>发送准备信号</summary>
        public void SendReady()
        {
            Core.NetworkManager.Instance?.Send("pvp:ready", "{\"ready\":true}");
        }

        /// <summary>断线重连</summary>
        public void Reconnect(string matchId)
        {
            MatchId = matchId;
            ChangeState(PVPState.Disconnected);
            StartCoroutine(ReconnectRoutine());
        }

        /// <summary>主动放弃</summary>
        public void Forfeit()
        {
            Core.NetworkManager.Instance?.Send("pvp:forfeit", "{}");
            ChangeState(PVPState.Finished);
        }

        // === WebSocket 事件处理（接口定义） ===

        private void RegisterPVPEvents()
        {
            var nm = Core.NetworkManager.Instance;
            if (nm == null) return;

            // 以下事件处理预留，PVP 功能启动时实现
            // nm.RegisterHandler("pvp:match_found", OnMatchFound);
            // nm.RegisterHandler("pvp:question", OnQuestionReceived);
            // nm.RegisterHandler("pvp:opponent_status", OnOpponentStatus);
            // nm.RegisterHandler("pvp:result", OnResultReceived);
            // nm.RegisterHandler("pvp:match_end", OnMatchEnd);
            // nm.RegisterHandler("pvp:item_effect", OnItemEffect);
        }

        private void UnregisterPVPEvents()
        {
            var nm = Core.NetworkManager.Instance;
            if (nm == null) return;
            // nm.UnregisterHandler("pvp:match_found", OnMatchFound);
            // nm.UnregisterHandler("pvp:question", OnQuestionReceived);
            // ... etc
        }

        private void ChangeState(PVPState newState)
        {
            CurrentState = newState;
            OnStateChanged?.Invoke(newState);
        }

        private IEnumerator MatchTimeoutRoutine()
        {
            yield return new WaitForSeconds(MatchTimeout);
            if (CurrentState == PVPState.Matching)
            {
                Debug.Log("[PVPBattle] 匹配超时，使用 Bot 对手");
                // TODO: 降级为 Bot 对战或提示用户
                CancelMatchQueue();
            }
        }

        private IEnumerator ReconnectRoutine()
        {
            float reconnectTimeout = 30f;
            float elapsed = 0f;

            while (elapsed < reconnectTimeout)
            {
                // TODO: 尝试重新建立 PVP 会话
                elapsed += Time.deltaTime;
                yield return null;
            }

            // 超时判定失败
            ChangeState(PVPState.Finished);
        }
    }
}
