using UnityEngine;
using System;
using System.Collections.Generic;
using OriginXR.Data;

namespace OriginXR.Guild
{
    /// <summary>
    /// 公会管理器（预留 - 当前阶段暂不开发）
    /// 负责：
    /// 1. 公会的创建/加入/退出/升级管理
    /// 2. 公会 BOSS 的生成与血量同步（WebSocket guild:boss_hp_sync）
    /// 3. 公会成员伤害贡献累积
    /// 4. 公会科技树管理（经验/金币加成等）
    /// 5. 公会战引导
    ///
    /// 解锁条件：玩家等级 >= 5
    ///
    /// 当前状态：暂不开发，仅保留接口定义。
    /// 待公会功能启动时实现具体逻辑。
    ///
    /// WebSocket 事件：
    ///   guild:boss_hp_sync -> 公会 BOSS 血量同步
    ///
    /// API 接口：
    ///   POST /api/v1/game/guilds           -> 创建公会
    ///   POST /api/v1/game/guilds/join      -> 加入公会
    ///   POST /api/v1/game/guilds/leave     -> 退出公会
    ///   GET  /api/v1/game/guilds/:id       -> 公会详情
    ///   POST /api/v1/game/guilds/boss/attack -> 攻击公会BOSS
    /// </summary>
    public class GuildManager : MonoBehaviour
    {
        // === 单例 ===
        public static GuildManager Instance { get; private set; }

        // === 属性 ===
        /// <summary>当前公会ID（null表示无公会）</summary>
        public string MyGuildId { get; private set; }

        /// <summary>公会名称</summary>
        public string GuildName { get; private set; }

        /// <summary>公会等级</summary>
        public int GuildLevel { get; private set; }

        /// <summary>公会成员上限</summary>
        public int MaxMembers { get; private set; }

        /// <summary>公会资金</summary>
        public int GuildFunds { get; private set; }

        /// <summary>成员列表</summary>
        public List<GuildMemberData> Members { get; private set; }

        /// <summary>今日剩余BOSS挑战次数</summary>
        public int RemainingBossChallenges { get; private set; }

        /// <summary>公会科技树激活项</summary>
        public List<string> ActiveTechSkills { get; private set; }

        /// <summary>公会创建所需最低等级</summary>
        public const int MinCreateLevel = 5;

        /// <summary>初始成员上限</summary>
        public const int InitialMaxMembers = 4;

        // === Unity 生命周期 ===
        private void Start() { }
        private void OnDestroy() { }

        // === 公共方法 ===

        /// <summary>创建公会</summary>
        /// <param name="name">公会名称</param>
        /// <param name="icon">公会图标ID</param>
        public void CreateGuild(string name, string icon) { }

        /// <summary>加入公会</summary>
        /// <param name="guildId">目标公会ID</param>
        public void JoinGuild(string guildId) { }

        /// <summary>退出公会</summary>
        public void LeaveGuild() { }

        /// <summary>升级公会（需会长权限）</summary>
        public void UpgradeGuild() { }

        /// <summary>攻击公会 BOSS</summary>
        /// <param name="damageDealt">造成伤害值</param>
        public void AttackBoss(int damageDealt) { }

        /// <summary>捐献金币给公会</summary>
        public void DonateGold(int amount) { }

        /// <summary>激活公会科技</summary>
        public void ActivateTechSkill(string skillId) { }

        /// <summary>获取公会科技加成（经验倍率）</summary>
        public float GetExpBonus() { return 1f; }

        /// <summary>获取公会科技加成（金币倍率）</summary>
        public float GetGoldBonus() { return 1f; }

        // === 私有方法 ===
        private IEnumerator<Coroutine> FetchGuildInfo() { yield return null; }
        private IEnumerator<Coroutine> FetchMembers() { yield return null; }
        private IEnumerator<Coroutine> FetchBossStatus() { yield return null; }
        private void RegisterGuildEvents() { }
        private void UnregisterGuildEvents() { }
        private void HandleBossHPSync(string jsonData) { }

        // === 事件 ===
        /// <summary>公会信息更新事件</summary>
        public event Action OnGuildInfoUpdated;

        /// <summary>加入/离开公会事件</summary>
        public event Action<bool> OnGuildMembershipChanged;  // true=加入, false=离开

        /// <summary>公会 BOSS 血量变化事件</summary>
        public event Action<float, float> OnBossHPChanged;   // currentHP, maxHP
    }

    /// <summary>
    /// 公会成员数据
    /// </summary>
    [Serializable]
    public class GuildMemberData
    {
        public string PlayerId;
        public string Username;
        public int Level;
        public string Role;               // "leader" / "elder" / "member"
        public int WeeklyContribution;    // 本周贡献
        public int TotalContribution;     // 总贡献
        public long LastActiveAt;         // 最后活跃时间戳
    }
}
