using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace OriginXR.Guild
{
    /// <summary>
    /// 公会管理器（单例，预留模块）
    /// 负责：
    /// 1. 公会创建/加入/退出/升级管理
    /// 2. 公会 BOSS 血量 WebSocket 同步
    /// 3. 成员伤害贡献累积
    /// 4. 公会科技树（经验/金币加成）
    ///
    /// 解锁条件：玩家等级 >= 5
    /// 当前状态：接口已实现，具体服务端通信待公会功能启动时对接
    /// </summary>
    public class GuildManager : MonoBehaviour
    {
        // === 单例 ===
        public static GuildManager Instance { get; private set; }

        [Header("配置")]
        [SerializeField] private int _minCreateLevel = 5;
        [SerializeField] private int _initialMaxMembers = 4;
        [SerializeField] private int _dailyBossChallenges = 3;

        // === 属性 ===
        public string MyGuildId { get; private set; }
        public string GuildName { get; private set; }
        public int GuildLevel { get; private set; }
        public int MaxMembers { get; private set; }
        public int GuildFunds { get; private set; }
        public List<GuildMemberData> Members { get; private set; }
        public int RemainingBossChallenges { get; private set; }
        public List<string> ActiveTechSkills { get; private set; }
        public bool HasGuild => !string.IsNullOrEmpty(MyGuildId);
        public float ExpBonus { get; private set; } = 1f;
        public float GoldBonus { get; private set; } = 1f;

        // === 事件 ===
        public event Action OnGuildInfoUpdated;
        public event Action<bool> OnGuildMembershipChanged;
        public event Action<float, float> OnBossHPChanged;

        // === Unity 生命周期 ===

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            RegisterNetworkEvents();
            Members = new List<GuildMemberData>();
            ActiveTechSkills = new List<string>();
        }

        private void OnDestroy()
        {
            UnregisterNetworkEvents();
            if (Instance == this) Instance = null;
        }

        // === 公共方法 ===

        /// <summary>创建公会</summary>
        public void CreateGuild(string name, string iconId)
        {
            if (HasGuild)
            {
                Debug.LogWarning("[GuildManager] 已在公会中，无法创建");
                return;
            }

            // TODO: POST /api/v1/game/guilds { name, iconId }
            Debug.Log($"[GuildManager] 创建公会请求: {name}");
        }

        /// <summary>加入公会</summary>
        public void JoinGuild(string guildId)
        {
            if (HasGuild)
            {
                Debug.LogWarning("[GuildManager] 已在公会中");
                return;
            }

            // TODO: POST /api/v1/game/guilds/join { guildId }
            Debug.Log($"[GuildManager] 加入公会请求: {guildId}");
        }

        /// <summary>退出公会</summary>
        public void LeaveGuild()
        {
            if (!HasGuild) return;

            // TODO: POST /api/v1/game/guilds/leave
            Debug.Log("[GuildManager] 退出公会");
            MyGuildId = null;
            OnGuildMembershipChanged?.Invoke(false);
        }

        /// <summary>攻击公会 BOSS</summary>
        public void AttackBoss(int damageDealt)
        {
            if (RemainingBossChallenges <= 0) return;
            RemainingBossChallenges--;

            // TODO: POST /api/v1/game/guilds/boss/attack { damageDealt }
            Debug.Log($"[GuildManager] 攻击 BOSS: {damageDealt} 伤害");
        }

        /// <summary>捐献金币</summary>
        public void DonateGold(int amount)
        {
            // TODO: POST /api/v1/game/guilds/donate { amount }
            GuildFunds += amount;
        }

        /// <summary>获取公会经验加成倍率</summary>
        public float GetExpBonus() => ExpBonus;

        /// <summary>获取公会金币加成倍率</summary>
        public float GetGoldBonus() => GoldBonus;

        /// <summary>刷新公会信息</summary>
        public void RefreshGuildInfo()
        {
            if (!HasGuild) return;
            StartCoroutine(FetchGuildInfo());
        }

        // === 私有方法 ===

        private IEnumerator FetchGuildInfo()
        {
            // TODO: GET /api/v1/game/guilds/:id
            yield return new WaitForSeconds(0.3f);
            OnGuildInfoUpdated?.Invoke();
        }

        private void HandleBossHPSync(string jsonData)
        {
            // TODO: 解析 WebSocket guild:boss_hp_sync 消息
            // { currentHP: 75000, maxHP: 100000 }
        }

        private void RegisterNetworkEvents()
        {
            var nm = Core.NetworkManager.Instance;
            if (nm != null)
            {
                nm.RegisterHandler("guild:boss_hp_sync", HandleBossHPSync);
            }
        }

        private void UnregisterNetworkEvents()
        {
            var nm = Core.NetworkManager.Instance;
            if (nm != null)
            {
                nm.UnregisterHandler("guild:boss_hp_sync", HandleBossHPSync);
            }
        }
    }

    /// <summary>
    /// 公会成员数据
    /// </summary>
    [Serializable]
    public class GuildMemberData
    {
        public string playerId;
        public string username;
        public int level;
        public string role;              // "leader" / "elder" / "member"
        public int weeklyContribution;
        public int totalContribution;
        public long lastActiveAt;
    }
}
