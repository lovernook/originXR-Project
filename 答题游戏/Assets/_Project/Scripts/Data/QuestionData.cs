using System;
using System.Collections.Generic;

namespace OriginXR.Data
{
    /// <summary>
    /// 题目数据模型
    /// 负责：
    /// 1. 定义一道题目的完整数据结构（对应 Protobuf Question 消息）
    /// 2. 包含题目内容、选项、正确答案（服务端校验后下发结果）、难度、限时等
    /// 3. 作为对战（PVE/PVP）中题目展示的数据源
    ///
    /// 注意：correct_answer 由服务端保管，不在此模型中传输；
    ///       客户端仅收到服务端下发的题目内容和选项，答后收到对/错判定结果。
    ///
    /// Protobuf 对应：message Question in game.proto
    /// </summary>

    /// <summary>题目类型枚举</summary>
    public enum QuestionType
    {
        SingleChoice = 0,   // 单选题
        MultiChoice = 1,    // 多选题
        TrueFalse = 2,      // 判断题
        FillBlank = 3,      // 填空题
        Sorting = 4,        // 排序题
        Matching = 5        // 连线题
    }

    [Serializable]
    public class QuestionData
    {
        // === 基础信息 ===
        /// <summary>题目唯一ID</summary>
        public string Id;

        /// <summary>题目类型</summary>
        public QuestionType Type;

        /// <summary>题目内容（支持富文本/HTML）</summary>
        public string Content;

        /// <summary>题目配图或视频 URL</summary>
        public string MediaUrl;

        /// <summary>媒体类型：image / video / none</summary>
        public string MediaType;

        // === 选项 ===
        /// <summary>选项列表（A/B/C/D...）</summary>
        public List<OptionData> Options;

        // === 配置 ===
        /// <summary>难度（1~5）</summary>
        public int Difficulty;

        /// <summary>答题限时（秒）</summary>
        public int TimeLimit;

        /// <summary>所属知识点ID列表</summary>
        public List<string> KnowledgePointIds;

        /// <summary>来源学科</summary>
        public string SubjectId;

        /// <summary>题目解析（答错后展示）</summary>
        public string Explanation;

        // === 答题结果（答题后填充） ===
        /// <summary>用户选择的答案</summary>
        public string SelectedAnswer;

        /// <summary>是否正确（由服务端返回）</summary>
        public bool IsCorrect;

        /// <summary>答题耗时（秒）</summary>
        public float UsedTime;
    }

    /// <summary>
    /// 选项数据模型
    /// </summary>
    [Serializable]
    public class OptionData
    {
        /// <summary>选项标识（A/B/C/D）</summary>
        public string Key;

        /// <summary>选项内容</summary>
        public string Content;

        /// <summary>选项配图 URL（如需要）</summary>
        public string MediaUrl;
    }
}
