using UnityEngine;
using System.Collections.Generic;

namespace OriginXR.Data
{
    /// <summary>
    /// 题库 ScriptableObject
    /// 右键 Project 窗口 → Create → OriginXR → QuestionBank 创建
    /// 可在 Inspector 中直接编辑题目
    /// </summary>
    [CreateAssetMenu(fileName = "QuestionBank", menuName = "OriginXR/QuestionBank")]
    public class QuestionBankSO : ScriptableObject
    {
        [TextArea(2, 5)]
        public string bankDescription = "默认题库";

        public List<QuestionEntry> questions = new List<QuestionEntry>();
    }

    /// <summary>
    /// 单道题目数据（Inspector 可编辑版）
    /// </summary>
    [System.Serializable]
    public class QuestionEntry
    {
        public QuestionType questionType = QuestionType.SingleChoice;

        [TextArea(2, 5)]
        public string content = "";             // 题目文本
        public string mediaUrl = "";            // 配图URL（可选）

        public string optionA = "";
        public string optionB = "";
        public string optionC = "";
        public string optionD = "";

        public string correctAnswer = "";       // "A" / "B" / "C" / "D" / "T" / "F"
        public int difficulty = 1;              // 1~5
        public int timeLimit = 10;              // 限时秒数

        [TextArea(2, 4)]
        public string explanation = "";         // 题目解析

        /// <summary>转为运行时 QuestionData</summary>
        public QuestionData ToQuestionData()
        {
            var data = new QuestionData
            {
                id = System.Guid.NewGuid().ToString(),
                type = questionType,
                content = content,
                mediaUrl = mediaUrl,
                difficulty = difficulty,
                timeLimit = timeLimit,
                explanation = explanation,
                devCorrectAnswer = correctAnswer,
                options = new List<OptionData>()
            };

            // 判断题特殊处理
            if (questionType == QuestionType.TrueFalse)
            {
                data.options.Add(new OptionData { key = "T", content = "正确" });
                data.options.Add(new OptionData { key = "F", content = "错误" });
                return data;
            }

            // 添加非空选项
            AddOption(data, "A", optionA);
            AddOption(data, "B", optionB);
            AddOption(data, "C", optionC);
            AddOption(data, "D", optionD);

            return data;
        }

        private void AddOption(QuestionData data, string key, string text)
        {
            if (!string.IsNullOrEmpty(text))
                data.options.Add(new OptionData { key = key, content = text });
        }
    }
}
