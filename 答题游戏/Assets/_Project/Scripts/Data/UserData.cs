using System;
using System.Collections.Generic;
using UnityEngine;

namespace OriginXR.Data
{
    /// <summary>
    /// 用户数据模型
    /// 负责：
    /// 1. 客户端本地缓存当前登录用户信息
    /// 2. 包含基础信息、经济数值、学习进度、统计数据
    /// 3. 数据同步：登录时从 API 拉取，变更时本地更新
    /// 4. 作为 API GET /api/v1/users/:id 的响应容器
    /// </summary>
    [Serializable]
    public class UserData
    {
        // === 基础信息 ===
        public string userId;
        public string username;
        public string avatarId;
        public int level;
        public int experience;
        public int experienceToNextLevel;
        public int totalExperience;

        // === 经济系统 ===
        public int gold;
        public int diamond;
        public int energy;
        public int maxEnergy;
        public int reputation;

        // === PVP 段位 ===
        public string rankTier;
        public int eloScore;

        // === 统计数据 ===
        public int totalQuestionsAnswered;
        public int totalCorrectAnswers;
        public int totalStudyMinutes;
        public int consecutiveSignInDays;
        public int totalLoginDays;

        // === 方法 ===

        /// <summary>获取答题正确率（百分比 0~100）</summary>
        public float GetAccuracyRate()
        {
            if (totalQuestionsAnswered == 0) return 0f;
            return (float)totalCorrectAnswers / totalQuestionsAnswered * 100f;
        }

        /// <summary>获取经验条进度（0~1），用于 UI 进度条</summary>
        public float GetExpProgress()
        {
            if (experienceToNextLevel <= 0) return 1f;
            return Mathf.Clamp01((float)experience / experienceToNextLevel);
        }

        /// <summary>判断是否有足够的货币</summary>
        /// <param name="currencyType">"gold" / "diamond" / "energy"</param>
        /// <param name="amount">所需数量</param>
        public bool HasEnoughCurrency(string currencyType, int amount)
        {
            switch (currencyType.ToLower())
            {
                case "gold": return gold >= amount;
                case "diamond": return diamond >= amount;
                case "energy": return energy >= amount;
                default: return false;
            }
        }

        /// <summary>增加金币（返回变更后的数量）</summary>
        public int AddGold(int amount)
        {
            gold = Mathf.Max(0, gold + amount);
            return gold;
        }

        /// <summary>增加钻石</summary>
        public int AddDiamond(int amount)
        {
            diamond = Mathf.Max(0, diamond + amount);
            return diamond;
        }

        /// <summary>消耗体力（返回是否成功）</summary>
        public bool ConsumeEnergy(int amount)
        {
            if (energy < amount) return false;
            energy -= amount;
            return true;
        }

        /// <summary>增加经验值（自动处理升级）</summary>
        /// <returns>升级了几级</returns>
        public int AddExperience(int amount)
        {
            experience += amount;
            totalExperience += amount;
            int levelsGained = 0;

            while (experience >= experienceToNextLevel && experienceToNextLevel > 0)
            {
                experience -= experienceToNextLevel;
                level++;
                levelsGained++;
                experienceToNextLevel = CalculateNextLevelExp(level);
            }

            return levelsGained;
        }

        /// <summary>计算下一级所需经验值</summary>
        /// <param name="currentLevel">当前等级</param>
        /// <returns>升级所需经验</returns>
        public static int CalculateNextLevelExp(int currentLevel)
        {
            // 经验曲线: 100 * level * 1.5^(level-1)
            return Mathf.RoundToInt(100f * currentLevel * Mathf.Pow(1.5f, currentLevel - 1));
        }

        /// <summary>获取用户等级称号</summary>
        public string GetLevelTitle()
        {
            if (level >= 50) return "传奇学者";
            if (level >= 30) return "博学大师";
            if (level >= 20) return "资深研究员";
            if (level >= 10) return "进阶学员";
            if (level >= 5) return "见习学者";
            return "初学者";
        }
    }

    /// <summary>
    /// API 通用响应包装
    /// </summary>
    [Serializable]
    public class ApiResponse<T>
    {
        public int code;             // 0=成功
        public string message;
        public T data;
        public long timestamp;

        public bool IsSuccess => code == 0;
    }

    /// <summary>
    /// API 分页数据包装
    /// </summary>
    [Serializable]
    public class PaginatedData<T>
    {
        public List<T> items;
        public int total;
        public int page;
        public int pageSize;
        public int totalPages;
    }
}
