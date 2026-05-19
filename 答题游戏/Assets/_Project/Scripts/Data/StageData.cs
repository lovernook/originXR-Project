using System;
using System.Collections.Generic;

namespace OriginXR.Data
{
    /// <summary>
    /// 关卡数据模型
    /// 负责：
    /// 1. 定义关卡（Stage）的完整配置数据结构
    /// 2. 包含关卡元信息、BOSS配置、题目池配置、奖励配置、解锁条件
    /// 3. 从服务端 API (GET /api/v1/game/stages) 获取后缓存于本地
    ///
    /// 注意：关卡数据由 Web 后台配置，Unity 端为只读消费
    /// </summary>

    [Serializable]
    public class StageData
    {
        // === 关卡信息 ===
        /// <summary>关卡唯一ID</summary>
        public string Id;

        /// <summary>关卡序号（1~40）</summary>
        public int StageNumber;

        /// <summary>关卡名称，如 "第一章：变量入门"</summary>
        public string Name;

        /// <summary>关卡描述</summary>
        public string Description;

        /// <summary>章节名称</summary>
        public string ChapterName;

        /// <summary>关卡场景背景ID</summary>
        public string BackgroundId;

        // === 状态 ===
        /// <summary>关卡状态：Locked / Unlocked / Completed</summary>
        public StageState State;

        /// <summary>最佳得分</summary>
        public int BestScore;

        /// <summary>最高星数（1~3）</summary>
        public int BestStars;

        /// <summary>挑战次数</summary>
        public int AttemptCount;

        // === BOSS 配置 ===
        /// <summary>BOSS 名称</summary>
        public string BossName;

        /// <summary>BOSS 3D模型资源ID</summary>
        public string BossModelId;

        /// <summary>BOSS 生命值</summary>
        public int BossHP;

        /// <summary>BOSS 攻击力（答错扣血量）</summary>
        public int BossAttack;

        // === 题目配置 ===
        /// <summary>每关题目数量</summary>
        public int QuestionCount;

        /// <summary>题目池：知识点ID + 难度范围 + 抽取数量</summary>
        public List<QuestionPoolItem> QuestionPool;

        /// <summary>每题限时（秒）</summary>
        public int TimePerQuestion;

        // === 奖励配置 ===
        /// <summary>通关经验值奖励</summary>
        public int RewardExp;

        /// <summary>通关金币奖励</summary>
        public int RewardGold;

        /// <summary>三星星数所需条件</summary>
        public List<StarCondition> StarConditions;

        // === 解锁条件 ===
        /// <summary>前置关卡ID</summary>
        public string PrerequisiteStageId;

        /// <summary>所需等级</summary>
        public int RequiredLevel;

        /// <summary>所需消耗体力</summary>
        public int EnergyCost;
    }

    /// <summary>关卡状态</summary>
    public enum StageState
    {
        Locked,      // 未解锁
        Unlocked,    // 已解锁可挑战
        Completed    // 已完成
    }

    /// <summary>
    /// 题目池配置项
    /// 定义某知识点下抽取多少道题
    /// </summary>
    [Serializable]
    public class QuestionPoolItem
    {
        /// <summary>知识点ID</summary>
        public string KnowledgePointId;

        /// <summary>最低难度（1~5）</summary>
        public int MinDifficulty;

        /// <summary>最高难度（1~5）</summary>
        public int MaxDifficulty;

        /// <summary>抽取题目数量</summary>
        public int Count;
    }

    /// <summary>
    /// 星数条件
    /// 三星星 = 满足所有条件
    /// </summary>
    [Serializable]
    public class StarCondition
    {
        /// <summary>条件类型：Accuracy / Time / Combo / NoMistake</summary>
        public string ConditionType;

        /// <summary>条件阈值（如正确率 >= 80）</summary>
        public float Threshold;

        /// <summary>条件描述文本</summary>
        public string Description;
    }

    /// <summary>
    /// 关卡通关结算数据（答题结束后生成）
    /// </summary>
    [Serializable]
    public class StageResultData
    {
        public string StageId;
        public int Score;
        public int CorrectCount;
        public int TotalCount;
        public int MaxCombo;
        public float TotalTime;
        public int StarsEarned;
        public int ExpGained;
        public int GoldGained;
        public List<string> WeakKnowledgePoints;
    }
}
