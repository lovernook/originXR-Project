using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using OriginXR.Data;

namespace OriginXR.Battle
{
    /// <summary>
    /// 题目展示控制器（UI）
    /// 负责：
    /// 1. 在 BattleScene 中展示题目内容和选项
    /// 2. 显示题目配图/视频素材（通过 MediaUrl 加载）
    /// 3. 选项按钮交互（点击选中 + 高亮 + 提交）
    /// 4. 展示题型相关UI（单选圆形、多选方形、判断对错按钮、填空输入框等）
    /// 5. 答题后展示结果反馈（正确绿色闪动、错误红色闪动 + 显示正确答案）
    ///
    /// 题型UI差异：
    ///   SingleChoice -> 4个圆形选项按钮，单选
    ///   MultiChoice  -> 4个方形选项按钮，多选 + 确认按钮
    ///   TrueFalse    -> 对/错两个大按钮
    ///   FillBlank    -> 输入框 + 提交按钮
    ///
    /// 界面布局：
    ///   上方：题目文本 + 图片/视频素材
    ///   中间：4个选项按钮(ABCD) + 倒计时圆环
    ///   左侧：玩家Avatar + 血条 + 连击计数器
    ///   右侧：对手/BOSS Avatar + 血条
    /// </summary>
    public class QuestionDisplay : MonoBehaviour
    {
        // === 题目UI组件 ===
        [SerializeField] private TextMeshProUGUI _questionText;       // 题目内容
        [SerializeField] private RawImage _mediaImage;                // 题目配图
        [SerializeField] private GameObject _mediaVideoContainer;     // 题目视频容器
        [SerializeField] private TextMeshProUGUI _questionIndexText;  // "第 3/10 题"

        // === 选项UI组件 ===
        [SerializeField] private Button[] _optionButtons;             // 4个选项按钮
        [SerializeField] private TextMeshProUGUI[] _optionTexts;      // 选项文本
        [SerializeField] private Image[] _optionBackgrounds;          // 选项背景
        [SerializeField] private GameObject[] _optionCheckMarks;      // 选中标记

        // === 不同题型容器 ===
        [SerializeField] private GameObject _singleChoiceContainer;
        [SerializeField] private GameObject _multiChoiceContainer;
        [SerializeField] private GameObject _trueFalseContainer;
        [SerializeField] private GameObject _fillBlankContainer;
        [SerializeField] private TMP_InputField _fillBlankInput;

        // === 结果反馈 ===
        [SerializeField] private GameObject _correctFeedback;         // 正确反馈动画
        [SerializeField] private GameObject _wrongFeedback;           // 错误反馈动画
        [SerializeField] private TextMeshProUGUI _correctAnswerText;  // 正确答案显示
        [SerializeField] private TextMeshProUGUI _explanationText;    // 题目解析

        // === 颜色配置 ===
        [SerializeField] private Color _correctColor = Color.green;
        [SerializeField] private Color _wrongColor = Color.red;
        [SerializeField] private Color _selectedColor = new Color(0.27f, 0.53f, 1f);
        [SerializeField] private Color _defaultColor = Color.white;

        // === 状态 ===
        private QuestionData _currentQuestion;
        private string _selectedAnswer;
        private bool _isAnswerSubmitted;            // 是否已提交答案（防止重复提交）
        private bool _isShowingResult;              // 是否正在展示结果

        // === Unity 生命周期 ===
        private void Awake() { }
        private void Start() { }

        // === 公共方法 ===

        /// <summary>显示题目</summary>
        /// <param name="question">题目数据</param>
        /// <param name="questionIndex">题目序号（1-based）</param>
        /// <param name="totalCount">总题目数</param>
        public void DisplayQuestion(QuestionData question, int questionIndex, int totalCount) { }

        /// <summary>显示答题结果反馈</summary>
        /// <param name="isCorrect">是否正确</param>
        /// <param name="correctAnswer">正确答案</param>
        /// <param name="explanation">题目解析</param>
        public void ShowResult(bool isCorrect, string correctAnswer, string explanation) { }

        /// <summary>清空题目显示（进入下一题前）</summary>
        public void ClearDisplay() { }

        /// <summary>获取当前选择的答案</summary>
        public string GetSelectedAnswer() { return _selectedAnswer; }

        /// <summary>是否已提交答案</summary>
        public bool HasSubmitted() { return _isAnswerSubmitted; }

        /// <summary>启用/禁用选项按钮交互</summary>
        public void SetOptionsInteractable(bool interactable) { }

        /// <summary>设置提交按钮回调</summary>
        public void SetSubmitCallback(Action<string> onSubmit) { }

        // === 私有方法 ===
        private void SwitchQuestionTypeUI(QuestionType type) { }
        private void OnOptionClicked(string key) { }
        private void OnSubmitButtonClicked() { }
        private void HighlightSelectedOption(string key) { }
        private void HighlightCorrectOption(string key) { }
        private void LoadQuestionMedia(string url) { }
        private IEnumerator PlayResultAnimation(bool isCorrect) { yield return null; }

        // === 事件 ===
        /// <summary>答案提交事件，参数为选项 key</summary>
        public event Action<string> OnAnswerSubmitted;
    }
}
