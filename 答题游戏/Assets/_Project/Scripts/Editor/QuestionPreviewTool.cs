using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace OriginXR.EditorTools
{
    /// <summary>
    /// 题目预览工具（Unity Editor 扩展窗口）
    /// 负责：
    /// 1. 在 Unity Editor 中打开一个可视化窗口，用于预览题目展示效果
    /// 2. 从后端 API 拉取题目数据（需配置 API 地址和 Token）
    /// 3. 支持预览不同题型：单选题/多选题/判断题/填空题
    /// 4. 模拟 BattleScene 中的题目展示布局效果
    /// 5. 支持在 Editor 中测试答题流程（选答案 -> 查看结果 -> 下一题）
    /// 6. 展示题目元信息（ID/难度/知识点/审核状态）
    ///
    /// 使用方式：
    ///   菜单栏 -> OriginXR -> Question Preview Tool 打开窗口
    ///
    /// API 对接：
    ///   GET /api/v1/admin/questions?page=1&pageSize=50
    /// </summary>
    public class QuestionPreviewTool : EditorWindow
    {
        // === 窗口状态 ===
        private Vector2 _scrollPosition;
        private string _apiBaseUrl = "http://localhost:3000/api/v1";
        private string _authToken = "";
        private int _currentPage = 1;
        private int _pageSize = 20;

        // === 题目数据 ===
        private List<QuestionPreviewData> _questionList;
        private int _selectedQuestionIndex = -1;
        private int _selectedOptionIndex = -1;
        private bool _hasSubmitted;
        private bool _showResult;

        [Serializable]
        private class QuestionPreviewData
        {
            public string Id;
            public int Type;            // 0=单选, 1=多选, 2=判断, 3=填空
            public string Content;
            public string MediaUrl;
            public string CorrectAnswer;
            public string Explanation;
            public int Difficulty;
            public List<string> KnowledgePointNames;
            public List<OptionPreviewData> Options;
            public string Status;       // draft/pending/published/archived
        }

        [Serializable]
        private class OptionPreviewData
        {
            public string Key;
            public string Content;
            public string MediaUrl;
        }

        // === EditorWindow 生命周期 ===
        private void OnEnable() { }
        private void OnGUI() { }
        private void OnDisable() { }

        // === 公共方法 ===

        /// <summary>打开窗口（菜单入口）</summary>
        [MenuItem("OriginXR/Question Preview Tool")]
        public static void ShowWindow()
        {
            GetWindow<QuestionPreviewTool>("Question Preview");
        }

        /// <summary>从后端 API 拉取题目列表</summary>
        private void FetchQuestions() { }

        /// <summary>渲染窗口主 UI</summary>
        private void DrawMainGUI() { }

        /// <summary>渲染顶部工具栏（API配置 + 刷新按钮）</summary>
        private void DrawToolbar() { }

        /// <summary>渲染左侧题目列表</summary>
        private void DrawQuestionList() { }

        /// <summary>渲染右侧题目预览区</summary>
        private void DrawQuestionPreview() { }

        /// <summary>渲染题目预览（模拟实际答题界面）</summary>
        private void DrawQuestionContent(QuestionPreviewData question) { }

        /// <summary>渲染选项按钮</summary>
        private void DrawOptionButtons(List<OptionPreviewData> options, int questionType) { }

        /// <summary>渲染答题结果</summary>
        private void DrawResult(string correctAnswer, string explanation) { }

        /// <summary>渲染题目元信息（ID/难度/知识点/状态）</summary>
        private void DrawQuestionMeta(QuestionPreviewData question) { }

        /// <summary>渲染题目类型对应的UI差异</summary>
        private void DrawSingleChoiceOptions(List<OptionPreviewData> options) { }
        private void DrawMultiChoiceOptions(List<OptionPreviewData> options) { }
        private void DrawTrueFalseOptions() { }
        private void DrawFillBlankInput() { }

        /// <summary>切换到上一题 / 下一题</summary>
        private void NavigateToPrevQuestion() { }
        private void NavigateToNextQuestion() { }

        /// <summary>提交选中答案并显示结果</summary>
        private void SubmitAnswer() { }

        /// <summary>重置当前题目状态</summary>
        private void ResetQuestion() { }

        /// <summary>在浏览器中打开题目编辑页</summary>
        private void OpenInWebEditor(string questionId) { }
    }
}
