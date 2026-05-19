using System;
using System.Collections.Generic;

namespace OriginXR.Data
{
    /// <summary>
    /// 用户数据模型
    /// 负责：
    /// 1. 客户端本地缓存当前登录用户的完整信息
    /// 2. 包含基础信息、经济数值、学习进度、统计数据
    /// 3. 数据同步：登录时从服务端拉取，变更时本地更新 + 异步上报服务端
    /// 4. 提供对 UserService (后端) 接口返回数据的反序列化容器
    ///
    /// API 对应接口：GET /api/v1/users/:id
    /// </summary>
    [Serializable]
    public class UserData
    {
        // === 基础信息 ===
        /// <summary>用户唯一ID</summary>
        public string UserId;

        /// <summary>用户名（游戏内昵称）</summary>
        public string Username;

        /// <summary>Avatar 形象ID（对应模型资源）</summary>
        public string AvatarId;

        /// <summary>当前等级</summary>
        public int Level;

        /// <summary>当前经验值</summary>
        public int Experience;

        /// <summary>下一级所需经验</summary>
        public int ExperienceToNextLevel;

        /// <summary>总经验值</summary>
        public int TotalExperience;

        // === 经济系统 ===
        /// <summary>金币数量</summary>
        public int Gold;

        /// <summary>钻石数量</summary>
        public int Diamond;

        /// <summary>当前体力值</summary>
        public int Energy;

        /// <summary>体力上限</summary>
        public int MaxEnergy;

        /// <summary>声望值</summary>
        public int Reputation;

        // === PVP 段位 ===
        /// <summary>当前段位名称（青铜/白银/黄金/铂金/钻石/王者）</summary>
        public string RankTier;

        /// <summary>ELO 评分</summary>
        public int EloScore;

        // === 统计数据 ===
        /// <summary>总答题数</summary>
        public int TotalQuestionsAnswered;

        /// <summary>总答题正确数</summary>
        public int TotalCorrectAnswers;

        /// <summary>总学习时长（分钟）</summary>
        public int TotalStudyMinutes;

        /// <summary>连续签到天数</summary>
        public int ConsecutiveSignInDays;

        /// <summary>累计登录天数</summary>
        public int TotalLoginDays;

        // === 方法 ===
        /// <summary>获取正确率百分比</summary>
        public float GetAccuracyRate()
        {
            if (TotalQuestionsAnswered == 0) return 0f;
            return (float)TotalCorrectAnswers / TotalQuestionsAnswered * 100f;
        }

        /// <summary>获取当前经验百分比进度（用于经验条UI）</summary>
        public float GetExpProgress()
        {
            if (ExperienceToNextLevel == 0) return 1f;
            return (float)Experience / ExperienceToNextLevel;
        }

        /// <summary>是否有足够的指定货币</summary>
        public bool HasEnoughCurrency(string currencyType, int amount) { return false; }
    }
}
