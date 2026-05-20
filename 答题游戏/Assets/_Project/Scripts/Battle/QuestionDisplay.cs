using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using OriginXR.Data;

namespace OriginXR.Battle
{
    /// <summary>
    /// 题目展示控制器
    /// 负责：
    /// 1. 在 BattleScene 展示题目文本、图片/视频、选项按钮
    /// 2. 根据不同题型切换 UI 布局（单选/多选/判断/填空）
    /// 3. 选项点击高亮 + 选中状态
    /// 4. 答题后展示结果反馈（正确绿色 / 错误红色 + 正确答案 + 解析）
    /// </summary>
    public class QuestionDisplay : MonoBehaviour
    {
        [Header("题目 UI")]
        [SerializeField] private TextMeshProUGUI _questionText;
        [SerializeField] private TextMeshProUGUI _questionIndexText;    // "第3/10题"
        [SerializeField] private RawImage _mediaImage;
        [SerializeField] private GameObject _mediaVideoContainer;

        [Header("单选题 UI")]
        [SerializeField] private GameObject _singleChoiceContainer;
        [SerializeField] private Button[] _singleButtons;
        [SerializeField] private Image[] _singleButtonBg;
        [SerializeField] private TextMeshProUGUI[] _singleButtonTexts;

        [Header("多选题 UI")]
        [SerializeField] private GameObject _multiChoiceContainer;
        [SerializeField] private Toggle[] _multiToggles;
        [SerializeField] private TextMeshProUGUI[] _multiToggleTexts;
        [SerializeField] private Button _multiConfirmButton;

        [Header("判断题 UI")]
        [SerializeField] private GameObject _trueFalseContainer;
        [SerializeField] private Button _trueButton;
        [SerializeField] private Button _falseButton;

        [Header("填空题 UI")]
        [SerializeField] private GameObject _fillBlankContainer;
        [SerializeField] private TMP_InputField _fillBlankInput;
        [SerializeField] private Button _fillBlankSubmitButton;

        [Header("结果反馈")]
        [SerializeField] private GameObject _resultPanel;
        [SerializeField] private TextMeshProUGUI _resultText;           // "正确！" / "错误"
        [SerializeField] private TextMeshProUGUI _correctAnswerText;
        [SerializeField] private TextMeshProUGUI _explanationText;
        [SerializeField] private Image _resultBackground;
        [SerializeField] private CanvasGroup _resultCanvasGroup;

        [Header("颜色")]
        [SerializeField] private Color _correctColor = new Color(0.2f, 0.8f, 0.3f);
        [SerializeField] private Color _wrongColor = new Color(1f, 0.3f, 0.3f);
        [SerializeField] private Color _selectedColor = new Color(0.27f, 0.53f, 1f);
        [SerializeField] private Color _defaultColor = new Color(0.9f, 0.9f, 0.9f);

        // === 状态 ===
        private QuestionData _currentQuestion;
        private string _selectedAnswer = "";
        private HashSet<string> _multiSelectedAnswers = new HashSet<string>();
        private bool _hasSubmitted;
        private bool _isShowingResult;
        private Action<string> _submitCallback;

        // === 事件 ===
        public event Action<string> OnAnswerSubmitted;

        // === Unity 生命周期 ===

        private void Awake()
        {
            // 绑定选项按钮事件
            if (_singleButtons != null)
            {
                for (int i = 0; i < _singleButtons.Length; i++)
                {
                    int index = i;
                    if (_singleButtons[i] != null)
                        _singleButtons[i].onClick.AddListener(() => OnSingleOptionClicked(index));
                }
            }

            if (_trueButton != null) _trueButton.onClick.AddListener(() => OnSingleOptionClicked(0));
            if (_falseButton != null) _falseButton.onClick.AddListener(() => OnSingleOptionClicked(1));

            if (_fillBlankSubmitButton != null)
                _fillBlankSubmitButton.onClick.AddListener(OnFillBlankSubmit);

            if (_multiConfirmButton != null)
                _multiConfirmButton.onClick.AddListener(OnMultiConfirm);

            // 初始隐藏所有题型容器
            HideAllContainers();
            if (_resultPanel != null) _resultPanel.SetActive(false);
        }

        // === 公共方法 ===

        /// <summary>显示题目</summary>
        public void DisplayQuestion(QuestionData question, int questionIndex, int totalCount)
        {
            _currentQuestion = question;
            _selectedAnswer = "";
            _multiSelectedAnswers.Clear();
            _hasSubmitted = false;
            _isShowingResult = false;

            // 隐藏结果面板
            if (_resultPanel != null) _resultPanel.SetActive(false);

            // 设置题目文本
            if (_questionText != null)
                _questionText.text = question.content;

            // 设置题号
            if (_questionIndexText != null)
                _questionIndexText.text = $"第 {questionIndex}/{totalCount} 题";

            // 根据题型切换 UI
            HideAllContainers();
            switch (question.type)
            {
                case QuestionType.SingleChoice:
                    SetupSingleChoice(question);
                    break;
                case QuestionType.MultiChoice:
                    SetupMultiChoice(question);
                    break;
                case QuestionType.TrueFalse:
                    SetupTrueFalse(question);
                    break;
                case QuestionType.FillBlank:
                    SetupFillBlank(question);
                    break;
            }

            // 清除选项高亮
            ResetOptionHighlights();

            // 加载媒体素材
            LoadQuestionMedia(question.mediaUrl);
        }

        /// <summary>显示答题结果</summary>
        public void ShowResult(bool isCorrect, string correctAnswer, string explanation)
        {
            _isShowingResult = true;

            if (_resultPanel == null) return;
            _resultPanel.SetActive(true);

            if (_resultText != null)
                _resultText.text = isCorrect ? "✓ 回答正确！" : "✗ 回答错误";

            if (_resultText != null)
                _resultText.color = isCorrect ? _correctColor : _wrongColor;

            if (_resultBackground != null)
                _resultBackground.color = isCorrect
                    ? new Color(0.1f, 0.3f, 0.1f, 0.9f)
                    : new Color(0.3f, 0.1f, 0.1f, 0.9f);

            if (_correctAnswerText != null && !isCorrect)
                _correctAnswerText.text = $"正确答案：{correctAnswer}";

            if (_explanationText != null)
                _explanationText.text = explanation;

            // 高亮正确答案
            if (!isCorrect)
                HighlightCorrectOption(correctAnswer);

            // 淡入动画
            if (_resultCanvasGroup != null)
            {
                _resultCanvasGroup.alpha = 0f;
                StartCoroutine(FadeCanvasGroup(_resultCanvasGroup, 0f, 1f, 0.2f));
            }

            // 播放音效
            Core.AudioManager.Instance?.PlayUISFX(isCorrect ? "correct" : "wrong");
        }

        /// <summary>清空显示</summary>
        public void ClearDisplay()
        {
            _currentQuestion = null;
            HideAllContainers();
        }

        /// <summary>启用/禁用选项交互</summary>
        public void SetOptionsInteractable(bool interactable)
        {
            if (_singleButtons != null)
                foreach (var btn in _singleButtons)
                    if (btn != null) btn.interactable = interactable;

            if (_trueButton != null) _trueButton.interactable = interactable;
            if (_falseButton != null) _falseButton.interactable = interactable;
            if (_fillBlankSubmitButton != null) _fillBlankSubmitButton.interactable = interactable;
            if (_multiConfirmButton != null) _multiConfirmButton.interactable = interactable;
        }

        // === 私有：题型设置 ===

        private void SetupSingleChoice(QuestionData question)
        {
            if (_singleChoiceContainer != null) _singleChoiceContainer.SetActive(true);

            for (int i = 0; i < _singleButtonTexts.Length && i < question.options.Count; i++)
            {
                if (_singleButtonTexts[i] != null)
                    _singleButtonTexts[i].text = $"{question.options[i].key}. {question.options[i].content}";
            }
        }

        private void SetupMultiChoice(QuestionData question)
        {
            if (_multiChoiceContainer != null) _multiChoiceContainer.SetActive(true);

            for (int i = 0; i < _multiToggleTexts.Length && i < question.options.Count; i++)
            {
                if (_multiToggleTexts[i] != null)
                    _multiToggleTexts[i].text = $"{question.options[i].key}. {question.options[i].content}";
                if (_multiToggles[i] != null)
                    _multiToggles[i].isOn = false;
            }
        }

        private void SetupTrueFalse(QuestionData question)
        {
            if (_trueFalseContainer != null) _trueFalseContainer.SetActive(true);
        }

        private void SetupFillBlank(QuestionData question)
        {
            if (_fillBlankContainer != null) _fillBlankContainer.SetActive(true);
            if (_fillBlankInput != null) _fillBlankInput.text = "";
        }

        private void HideAllContainers()
        {
            if (_singleChoiceContainer != null) _singleChoiceContainer.SetActive(false);
            if (_multiChoiceContainer != null) _multiChoiceContainer.SetActive(false);
            if (_trueFalseContainer != null) _trueFalseContainer.SetActive(false);
            if (_fillBlankContainer != null) _fillBlankContainer.SetActive(false);
        }

        // === 私有：选项点击 ===

        private void OnSingleOptionClicked(int index)
        {
            if (_hasSubmitted || _currentQuestion == null) return;

            _selectedAnswer = _currentQuestion.GetOptionCount() > index
                ? _currentQuestion.options[index].key : "";

            HighlightSelectedButton(index);
            SubmitCurrentAnswer();
        }

        private void OnMultiConfirm()
        {
            if (_hasSubmitted) return;

            _multiSelectedAnswers.Clear();
            if (_multiToggles != null)
            {
                for (int i = 0; i < _multiToggles.Length && i < _currentQuestion.options.Count; i++)
                {
                    if (_multiToggles[i] != null && _multiToggles[i].isOn)
                        _multiSelectedAnswers.Add(_currentQuestion.options[i].key);
                }
            }

            _selectedAnswer = string.Join(",", _multiSelectedAnswers);
            SubmitCurrentAnswer();
        }

        private void OnFillBlankSubmit()
        {
            if (_hasSubmitted) return;
            _selectedAnswer = _fillBlankInput != null ? _fillBlankInput.text.Trim() : "";
            SubmitCurrentAnswer();
        }

        private void SubmitCurrentAnswer()
        {
            if (string.IsNullOrEmpty(_selectedAnswer)) return;

            _hasSubmitted = true;
            SetOptionsInteractable(false);
            OnAnswerSubmitted?.Invoke(_selectedAnswer);
        }

        // === 私有：高亮 ===

        private void HighlightSelectedButton(int index)
        {
            if (_singleButtonBg != null && index < _singleButtonBg.Length)
            {
                ResetOptionHighlights();
                _singleButtonBg[index].color = _selectedColor;
            }
        }

        private void HighlightCorrectOption(string correctAnswer)
        {
            if (_currentQuestion == null) return;

            for (int i = 0; i < _currentQuestion.GetOptionCount(); i++)
            {
                if (_currentQuestion.options[i].key == correctAnswer)
                {
                    if (_singleButtonBg != null && i < _singleButtonBg.Length)
                        _singleButtonBg[i].color = _correctColor;
                    break;
                }
            }
        }

        private void ResetOptionHighlights()
        {
            if (_singleButtonBg != null)
                foreach (var bg in _singleButtonBg)
                    if (bg != null) bg.color = _defaultColor;
        }

        // === 私有：资源加载 ===

        private void LoadQuestionMedia(string url)
        {
            if (string.IsNullOrEmpty(url)) return;
            // TODO: 使用 UnityWebRequest 或 Addressables 异步加载媒体
            StartCoroutine(LoadMediaCoroutine(url));
        }

        private IEnumerator LoadMediaCoroutine(string url)
        {
            if (_mediaImage != null)
            {
                using (var request = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url))
                {
                    yield return request.SendWebRequest();
                    if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                    {
                        Texture2D texture = UnityEngine.Networking.DownloadHandlerTexture.GetContent(request);
                        _mediaImage.texture = texture;
                        _mediaImage.gameObject.SetActive(true);
                    }
                }
            }
        }

        private IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                cg.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }
            cg.alpha = to;
        }
    }
}
