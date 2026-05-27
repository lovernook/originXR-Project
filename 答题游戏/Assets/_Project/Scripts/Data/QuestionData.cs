using System;
using System.Collections.Generic;

namespace OriginXR.Data
{
    /// <summary>
    /// 题目类型枚举（与 Protobuf QuestionType 对齐）
    /// </summary>
    public enum QuestionType
    {
        SingleChoice = 0,   // 单选题（4选1）
        MultiChoice = 1,    // 多选题（多选 + 确认）
        TrueFalse = 2,      // 判断题（对/错）
        FillBlank = 3,      // 填空题（文本输入）
        Sorting = 4,        // 排序题（拖拽排序）
        Matching = 5        // 连线题（拖拽连线）
    }

    /// <summary>
    /// 题目数据模型
    /// 负责：
    /// 1. 定义完整题目数据结构（对应 Protobuf Question 消息）
    /// 2. 包含题目内容、选项、媒体素材、难度、限时、解析
    /// 3. 答题结果字段（服务端校验后填充）
    ///
    /// 安全注意：
    ///   correctAnswer 字段仅服务端持有，客户端不存储。
    ///   答题结果通过 AnswerHandler 从服务端获取后填入 IsCorrect/Explanation。
    /// </summary>
    [Serializable]
    public class QuestionData
    {
        // === 基础信息 ===
        public string id;
        public QuestionType type;
        public string content;               // 题目文本（支持简单HTML标签）
        public string mediaUrl;              // 题目配图/视频/音频 URL
        public string mediaType;             // "image" / "video" / "audio" / "none"

        // === 选项列表 ===
        public List<OptionData> options;

        // === 配置 ===
        public int difficulty;               // 1=简单 ~ 5=困难
        public int timeLimit;                // 答题限时（秒）
        public List<string> knowledgePointIds; // 关联知识点ID列表
        public string subjectId;             // 所属学科ID
        public string explanation;           // 题目解析（答错后展示）
        [NonSerialized] public string devCorrectAnswer;  // 开发阶段使用，生产环境删除

        // === 答题结果（服务端返回后填充） ===
        [NonSerialized] public string selectedAnswer;
        [NonSerialized] public bool isCorrect;
        [NonSerialized] public float usedTime;          // 答题耗时（秒）
        [NonSerialized] public int scoreGained;          // 本题得分

        // === 方法 ===

        /// <summary>获取选项数量</summary>
        public int GetOptionCount() => options?.Count ?? 0;

        /// <summary>根据 key 获取选项</summary>
        public OptionData GetOption(string key)
        {
            if (options == null) return null;
            return options.Find(o => o.key == key);
        }

        /// <summary>获取难度文字描述</summary>
        public string GetDifficultyText()
        {
            return difficulty switch
            {
                1 => "★☆☆☆☆",
                2 => "★★☆☆☆",
                3 => "★★★☆☆",
                4 => "★★★★☆",
                5 => "★★★★★",
                _ => "未知"
            };
        }

        /// <summary>获取题型中文名称</summary>
        public string GetTypeName()
        {
            return type switch
            {
                QuestionType.SingleChoice => "单选题",
                QuestionType.MultiChoice => "多选题",
                QuestionType.TrueFalse => "判断题",
                QuestionType.FillBlank => "填空题",
                QuestionType.Sorting => "排序题",
                QuestionType.Matching => "连线题",
                _ => "未知题型"
            };
        }

        /// <summary>克隆题目数据（不含答题结果）</summary>
        public QuestionData Clone()
        {
            return new QuestionData
            {
                id = this.id,
                type = this.type,
                content = this.content,
                mediaUrl = this.mediaUrl,
                mediaType = this.mediaType,
                options = this.options?.ConvertAll(o => o.Clone()),
                difficulty = this.difficulty,
                timeLimit = this.timeLimit,
                knowledgePointIds = this.knowledgePointIds != null ? new List<string>(this.knowledgePointIds) : null,
                subjectId = this.subjectId,
                explanation = this.explanation
            };
        }
    }

    /// <summary>
    /// 选项数据模型
    /// </summary>
    [Serializable]
    public class OptionData
    {
        public string key;        // 前端用
        public string optionKey;  // 后端API用
        public string content;
        public string mediaUrl;

        /// <summary>获取有效的选项标识（兼容前后端字段名差异）</summary>
        public string GetKey() => !string.IsNullOrEmpty(key) ? key : optionKey;

        public OptionData Clone()
        {
            return new OptionData { key = this.key, optionKey = this.optionKey, content = this.content, mediaUrl = this.mediaUrl };
        }
    }
}
