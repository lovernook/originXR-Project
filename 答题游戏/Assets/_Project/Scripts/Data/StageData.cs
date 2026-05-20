using System;
using System.Collections.Generic;
using UnityEngine;

namespace OriginXR.Data
{
    /// <summary>
    /// 关卡状态
    /// </summary>
    public enum StageState
    {
        Locked,      // 未解锁
        Unlocked,    // 已解锁可挑战
        Completed    // 已通关完成
    }

    /// <summary>
    /// 关卡数据模型
    /// 负责：
    /// 1. 定义关卡完整配置数据（40关主线）
    /// 2. 包含 BOSS、题目池、奖励、解锁条件
    /// 3. 数据来源：GET /api/v1/game/stages 服务端接口
    ///    Unity 端只读消费，配置由 Web 后台管理
    /// </summary>
    [Serializable]
    public class StageData
    {
        // === 关卡信息 ===
        public string id;
        public int stageNumber;          // 关卡序号 1~40
        public string name;              // 关卡名称，如"第一章：变量入门"
        public string description;       // 关卡描述
        public string chapterName;       // 所属章节名称
        public string backgroundId;      // 场景背景资源ID

        // === 状态 ===
        public StageState state;
        public int bestScore;            // 最佳得分
        public int bestStars;            // 最高星数（0~3）
        public int attemptCount;         // 挑战次数

        // === BOSS 配置 ===
        public string bossName;          // BOSS 名称
        public string bossModelId;       // BOSS 3D 模型资源ID
        public int bossHP;               // BOSS 生命值
        public int bossAttack;           // BOSS 攻击力（答错扣血值）

        // === 题目配置 ===
        public int questionCount;                 // 每关题目数
        public List<QuestionPoolItem> questionPool; // 题目池配置
        public int timePerQuestion;               // 每题限时（秒，默认10）

        // === 奖励配置 ===
        public int rewardExp;                     // 通关经验值
        public int rewardGold;                    // 通关金币
        public List<StarCondition> starConditions; // 三星条件列表

        // === 解锁条件 ===
        public string prerequisiteStageId;        // 前置关卡ID（null=第一关）
        public int requiredLevel;                 // 所需等级
        public int energyCost;                    // 消耗体力

        // === 方法 ===

        /// <summary>获取星级的文字说明</summary>
        public string GetStarConditionText()
        {
            if (starConditions == null || starConditions.Count == 0) return "无特殊条件";
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (var cond in starConditions)
            {
                sb.AppendLine($"• {cond.description}");
            }
            return sb.ToString().TrimEnd();
        }

        /// <summary>获取 BOSS 信息摘要</summary>
        public string GetBossSummary()
        {
            return $"{bossName} | HP:{bossHP} | 攻击:{bossAttack}";
        }

        /// <summary>是否可挑战</summary>
        public bool IsPlayable()
        {
            return state == StageState.Unlocked || state == StageState.Completed;
        }
    }

    /// <summary>
    /// 题目池配置项
    /// 定义从某个知识点 + 难度范围中抽取多少道题
    /// </summary>
    [Serializable]
    public class QuestionPoolItem
    {
        public string knowledgePointId;    // 知识点ID
        public int minDifficulty = 1;      // 最低难度（1~5）
        public int maxDifficulty = 5;      // 最高难度（1~5）
        public int count = 3;              // 抽取题目数量

        /// <summary>难度范围描述</summary>
        public string GetDifficultyRange()
        {
            if (minDifficulty == maxDifficulty) return $"难度{minDifficulty}";
            return $"难度{minDifficulty}~{maxDifficulty}";
        }
    }

    /// <summary>
    /// 星数评定条件
    /// 三星需同时满足所有条件
    /// </summary>
    [Serializable]
    public class StarCondition
    {
        public string conditionType;   // "accuracy" / "time" / "combo" / "no_mistake"
        public float threshold;        // 阈值（如正确率>=80%）
        public string description;     // 条件描述，如"正确率 >= 80%"

        /// <summary>判断是否满足条件</summary>
        public bool IsMet(StageResultData result)
        {
            return conditionType switch
            {
                "accuracy" => result.GetAccuracy() >= threshold,
                "time" => result.totalTime <= threshold,
                "combo" => result.maxCombo >= (int)threshold,
                "no_mistake" => result.correctCount == result.totalCount,
                _ => false
            };
        }
    }

    /// <summary>
    /// 关卡通关结算数据
    /// 答题全部结束后生成，用于结算面板展示和服务端上报
    /// </summary>
    [Serializable]
    public class StageResultData
    {
        public string stageId;
        public string stageName;
        public int score;                // 总得分
        public int correctCount;         // 正确题数
        public int totalCount;           // 总题数
        public int maxCombo;             // 最大连击
        public float totalTime;          // 总用时（秒）
        public int starsEarned;          // 获得星数
        public int expGained;            // 获得经验
        public int goldGained;           // 获得金币
        public bool isBossDefeated;      // 是否击败 BOSS
        public List<string> weakKnowledgePoints; // 薄弱知识点ID列表
        public DateTime completedAt;     // 完成时间

        /// <summary>获取正确率（0~1）</summary>
        public float GetAccuracy()
        {
            if (totalCount == 0) return 0f;
            return (float)correctCount / totalCount;
        }

        /// <summary>获取正确率百分比文本</summary>
        public string GetAccuracyText()
        {
            return $"{(GetAccuracy() * 100f):F1}%";
        }

        /// <summary>获取平均每题用时</summary>
        public float GetAverageTimePerQuestion()
        {
            if (totalCount == 0) return 0f;
            return totalTime / totalCount;
        }

        /// <summary>获取星级文本（★/☆）</summary>
        public string GetStarText()
        {
            return new string('★', starsEarned) + new string('☆', 3 - Mathf.Min(starsEarned, 3));
        }
    }
}
