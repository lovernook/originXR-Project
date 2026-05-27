#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace OriginXR.EditorTools
{
    /// <summary>
    /// 题目预览工具（Unity Editor 窗口）
    /// 负责：
    /// 1. 在 Unity Editor 窗口可视化预览题目展示效果
    /// 2. 从后端 API 拉取题目数据供预览
    /// 3. 模拟答题流程（选择选项 → 查看结果）
    ///
    /// 使用方式：菜单栏 OriginXR → Question Preview Tool
    /// </summary>
    public class QuestionPreviewTool : EditorWindow
    {
        // === 窗口状态 ===
        private Vector2 _scrollPosition;
        private string _apiBaseUrl = "http://10.19.89.160:3002/api/v1";
        private string _authToken = "";
        private int _page = 1;
        private int _pageSize = 20;

        // === 题目数据 ===
        private List<QuestionPreviewData> _questions = new List<QuestionPreviewData>();
        private int _selectedIndex = -1;
        private int _selectedOptionIndex = -1;
        private bool _hasSubmitted;
        private bool _showResult;
        private bool _isLoading;

        // === 选项A/B/C/D颜色 ===
        private Color[] _optionColors = { Color.cyan, Color.yellow, Color.green, Color.magenta };

        [Serializable]
        private class QuestionPreviewData
        {
            public string id;
            public int type;       // 0=单选, 1=多选, 2=判断, 3=填空
            public string content;
            public string correctAnswer;
            public string explanation;
            public int difficulty;
            public string status;  // draft/pending/published/archived
            public List<OptionPreviewData> options;
        }

        [Serializable]
        private class OptionPreviewData
        {
            public string key;
            public string content;
        }

        // === 窗口入口 ===

        [MenuItem("OriginXR/Question Preview Tool")]
        public static void ShowWindow()
        {
            var window = GetWindow<QuestionPreviewTool>("题目预览工具");
            window.minSize = new Vector2(800, 600);
            window.Show();
        }

        private void OnEnable()
        {
            LoadMockQuestions();
        }

        private void OnGUI()
        {
            DrawToolbar();
            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            DrawQuestionList();
            DrawQuestionPreview();
            EditorGUILayout.EndHorizontal();
        }

        // === 工具栏 ===

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            EditorGUILayout.LabelField("API:", GUILayout.Width(30));
            _apiBaseUrl = EditorGUILayout.TextField(_apiBaseUrl, GUILayout.Width(250));

            EditorGUILayout.LabelField("Token:", GUILayout.Width(40));
            _authToken = EditorGUILayout.TextField(_authToken, GUILayout.Width(200));

            if (GUILayout.Button("拉取题目", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                FetchQuestionsFromAPI();
            }

            if (GUILayout.Button("加载模拟数据", EditorStyles.toolbarButton, GUILayout.Width(100)))
            {
                LoadMockQuestions();
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("在浏览器中打开管理后台", EditorStyles.toolbarButton, GUILayout.Width(160)))
            {
                Application.OpenURL(_apiBaseUrl + "/admin/questions");
            }

            EditorGUILayout.EndHorizontal();
        }

        // === 左侧：题目列表 ===

        private void DrawQuestionList()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(250));

            EditorGUILayout.LabelField("题目列表", EditorStyles.boldLabel);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            for (int i = 0; i < _questions.Count; i++)
            {
                var q = _questions[i];
                string typeIcon = q.type switch { 0 => "[单]", 1 => "[多]", 2 => "[判]", 3 => "[填]", _ => "[?]" };
                string label = $"{typeIcon} {q.content}";

                if (q.content.Length > 20) label = label.Substring(0, 20) + "...";

                Color bgColor = _selectedIndex == i ? new Color(0.3f, 0.5f, 1f) : GUI.backgroundColor;
                GUI.backgroundColor = bgColor;

                if (GUILayout.Button($"{i + 1}. {label}", GUILayout.Height(40)))
                {
                    SelectQuestion(i);
                }
            }

            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        // === 右侧：题目预览 ===

        private void DrawQuestionPreview()
        {
            EditorGUILayout.BeginVertical();

            if (_selectedIndex < 0 || _selectedIndex >= _questions.Count)
            {
                EditorGUILayout.HelpBox("请从左侧列表选择一道题目进行预览", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            var question = _questions[_selectedIndex];

            // 题目元信息
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"ID: {question.id}", EditorStyles.miniLabel, GUILayout.Width(120));
            EditorGUILayout.LabelField($"难度: {new string('★', question.difficulty)}", EditorStyles.miniLabel, GUILayout.Width(80));
            string statusText = question.status switch
            {
                "draft" => "草稿",
                "pending" => "待审",
                "published" => "已发布",
                "archived" => "已废弃",
                _ => question.status
            };
            EditorGUILayout.LabelField($"状态: {statusText}", EditorStyles.miniLabel, GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // 题目内容
            GUIStyle questionStyle = new GUIStyle(EditorStyles.wordWrappedLabel)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };
            EditorGUILayout.LabelField($"【题目】{question.content}", questionStyle, GUILayout.Height(60));

            EditorGUILayout.Space(10);

            // 选项按钮
            EditorGUILayout.LabelField("选项:", EditorStyles.boldLabel);
            for (int i = 0; i < question.options.Count; i++)
            {
                var option = question.options[i];

                Color originalBg = GUI.backgroundColor;
                if (_selectedOptionIndex == i)
                    GUI.backgroundColor = new Color(0.27f, 0.53f, 1f);
                else if (_showResult && option.key == question.correctAnswer)
                    GUI.backgroundColor = Color.green;
                else if (_showResult && _selectedOptionIndex == i && option.key != question.correctAnswer)
                    GUI.backgroundColor = Color.red;

                EditorGUILayout.BeginHorizontal("box");

                if (GUILayout.Button($"{option.key}", GUILayout.Width(40), GUILayout.Height(40)))
                {
                    SelectOption(i);
                }

                EditorGUILayout.LabelField(option.content, GUILayout.Height(40));
                EditorGUILayout.EndHorizontal();

                GUI.backgroundColor = originalBg;
            }

            EditorGUILayout.Space(10);

            // 操作按钮
            EditorGUILayout.BeginHorizontal();

            GUI.enabled = _selectedOptionIndex >= 0 && !_hasSubmitted;
            if (GUILayout.Button("提交答案", GUILayout.Height(35), GUILayout.Width(100)))
            {
                SubmitAnswer();
            }
            GUI.enabled = true;

            if (GUILayout.Button("重置", GUILayout.Height(35), GUILayout.Width(80)))
            {
                ResetQuestion();
            }

            EditorGUILayout.EndHorizontal();

            // 答题结果
            if (_showResult)
            {
                EditorGUILayout.Space(10);
                bool isCorrect = question.options[_selectedOptionIndex].key == question.correctAnswer;
                EditorGUILayout.HelpBox(
                    isCorrect ? "✓ 回答正确！" : $"✗ 回答错误！正确答案: {question.correctAnswer}",
                    isCorrect ? MessageType.Info : MessageType.Error
                );

                if (!string.IsNullOrEmpty(question.explanation))
                {
                    EditorGUILayout.LabelField("解析:", EditorStyles.boldLabel);
                    EditorGUILayout.HelpBox(question.explanation, MessageType.None);
                }
            }

            EditorGUILayout.EndVertical();
        }

        // === 操作方法 ===

        private void SelectQuestion(int index)
        {
            _selectedIndex = index;
            ResetQuestion();
        }

        private void SelectOption(int index)
        {
            if (_hasSubmitted) return;
            _selectedOptionIndex = index;
        }

        private void SubmitAnswer()
        {
            if (_hasSubmitted || _selectedIndex < 0 || _selectedOptionIndex < 0) return;
            _hasSubmitted = true;
            _showResult = true;

            var question = _questions[_selectedIndex];
            bool isCorrect = question.options[_selectedOptionIndex].key == question.correctAnswer;
            Debug.Log($"[QuestionPreview] 提交答案: {question.options[_selectedOptionIndex].key}, 正确: {isCorrect}");
        }

        private void ResetQuestion()
        {
            _selectedOptionIndex = -1;
            _hasSubmitted = false;
            _showResult = false;
        }

        private void LoadMockQuestions()
        {
            _questions = new List<QuestionPreviewData>
            {
                new QuestionPreviewData
                {
                    id = "preview_001", type = 0, content = "Unity中，以下哪个组件用于控制3D角色的移动？", correctAnswer = "A", explanation = "CharacterController是Unity专门用于角色移动控制的组件，提供Move()和SimpleMove()方法。",
                    difficulty = 2, status = "published",
                    options = new List<OptionPreviewData> {
                        new OptionPreviewData { key = "A", content = "CharacterController" },
                        new OptionPreviewData { key = "B", content = "BoxCollider" },
                        new OptionPreviewData { key = "C", content = "Rigidbody" },
                        new OptionPreviewData { key = "D", content = "MeshRenderer" }
                    }
                },
                new QuestionPreviewData
                {
                    id = "preview_002", type = 0, content = "C#中，interface关键字用于定义？", correctAnswer = "B", explanation = "interface关键字用于定义接口。接口仅包含方法/属性/事件的声明，不包含实现。",
                    difficulty = 1, status = "published",
                    options = new List<OptionPreviewData> {
                        new OptionPreviewData { key = "A", content = "抽象类" },
                        new OptionPreviewData { key = "B", content = "接口" },
                        new OptionPreviewData { key = "C", content = "枚举" },
                        new OptionPreviewData { key = "D", content = "结构体" }
                    }
                },
                new QuestionPreviewData
                {
                    id = "preview_003", type = 2, content = "Unity的Time.deltaTime表示当前帧与上一帧之间的时间间隔（秒）。", correctAnswer = "T", explanation = "Time.deltaTime确实是每帧的时间间隔，常用于平滑运动计算如: position += velocity * Time.deltaTime。",
                    difficulty = 1, status = "published",
                    options = new List<OptionPreviewData> {
                        new OptionPreviewData { key = "T", content = "正确" },
                        new OptionPreviewData { key = "F", content = "错误" }
                    }
                }
            };

            _selectedIndex = -1;
            ResetQuestion();
            Debug.Log($"[QuestionPreview] 已加载 {_questions.Count} 道模拟题目");
        }

        private void FetchQuestionsFromAPI()
        {
            Debug.Log("[QuestionPreview] 正在从 API 拉取题目...");
            // TODO: 使用 UnityWebRequest GET /api/v1/admin/questions
            LoadMockQuestions(); // 暂时使用模拟数据
        }
    }
}
#endif
