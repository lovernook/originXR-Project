using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace OriginXR.Knowledge
{
    /// <summary>
    /// 知识点详情面板 UI
    /// 负责：
    /// 1. 显示知识点概念卡片（名称/描述/掌握度/关联题目）
    /// 2. 支持多种展示形式切换（概念卡片/动画演示/记忆闪卡/思维导图/热力图/对比表）
    /// 3. 提供"开始复习"入口跳转到对应关卡
    /// 4. 导航上一项/下一项知识点
    /// </summary>
    public class KnowledgeDetailPanel : MonoBehaviour
    {
        [Header("主 UI")]
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _descriptionText;
        [SerializeField] private TextMeshProUGUI _masteryText;
        [SerializeField] private Image _masteryProgressBar;
        [SerializeField] private TextMeshProUGUI _subjectText;

        [Header("内容展示区域")]
        [SerializeField] private RectTransform _contentContainer;      // 不同展示形式挂载于此

        [Header("按钮")]
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _startReviewButton;
        [SerializeField] private Button _prevButton;
        [SerializeField] private Button _nextButton;

        // === 状态 ===
        private StarNodeData _currentNode;
        private string[] _siblingNodeIds = System.Array.Empty<string>();
        private int _currentIndex;

        // === Unity 生命周期 ===

        private void Awake()
        {
            if (_closeButton != null) _closeButton.onClick.AddListener(Hide);
            if (_startReviewButton != null) _startReviewButton.onClick.AddListener(StartReview);
            if (_prevButton != null) _prevButton.onClick.AddListener(NavigatePrev);
            if (_nextButton != null) _nextButton.onClick.AddListener(NavigateNext);

            if (_panelRoot != null)
                _panelRoot.SetActive(false);
        }

        private void OnEnable()
        {
            StarNodeController.OnNodeClickedEvent += OnNodeClicked;
        }

        private void OnDisable()
        {
            StarNodeController.OnNodeClickedEvent -= OnNodeClicked;
        }

        // === 公共方法 ===

        /// <summary>打开面板显示知识点详情</summary>
        public void Show(string nodeId)
        {
            // TODO: 从后端拉取知识点详情数据
            LoadKnowledgeDetail(nodeId);
        }

        /// <summary>关闭面板</summary>
        public void Hide()
        {
            if (_panelRoot != null)
                _panelRoot.SetActive(false);
        }

        /// <summary>上一个知识点</summary>
        public void NavigatePrev()
        {
            if (_siblingNodeIds.Length == 0) return;
            _currentIndex = (_currentIndex - 1 + _siblingNodeIds.Length) % _siblingNodeIds.Length;
            LoadKnowledgeDetail(_siblingNodeIds[_currentIndex]);
        }

        /// <summary>下一个知识点</summary>
        public void NavigateNext()
        {
            if (_siblingNodeIds.Length == 0) return;
            _currentIndex = (_currentIndex + 1) % _siblingNodeIds.Length;
            LoadKnowledgeDetail(_siblingNodeIds[_currentIndex]);
        }

        /// <summary>开始复习此知识点</summary>
        public void StartReview()
        {
            if (_currentNode == null) return;

            // 跳转到对应关卡
            // TODO: 根据知识点ID查找对应的关卡并跳转
            Debug.Log($"[KnowledgeDetailPanel] 开始复习知识点: {_currentNode.name}");

            // 示例：直接跳转到 BattleScene
            Core.SceneLoader.Instance?.LoadScene("BattleScene");
        }

        // === 私有方法 ===

        private void OnNodeClicked(string nodeId)
        {
            Show(nodeId);
        }

        private void LoadKnowledgeDetail(string nodeId)
        {
            // TODO: HTTP 请求知识点详情
            // 开发阶段使用模拟数据
            if (_panelRoot != null)
                _panelRoot.SetActive(true);

            if (_titleText != null)
                _titleText.text = $"知识点: {nodeId}";

            if (_descriptionText != null)
                _descriptionText.text = "这是一个知识点的详细描述。包含概念解释、示例代码、注意事项等。";

            if (_masteryText != null)
                _masteryText.text = "掌握度: 75%";

            if (_masteryProgressBar != null)
                _masteryProgressBar.fillAmount = 0.75f;

            if (_subjectText != null)
                _subjectText.text = "所属学科: 计算机科学";

            // 设置导航按钮状态
            if (_prevButton != null)
                _prevButton.interactable = _siblingNodeIds.Length > 1;

            if (_nextButton != null)
                _nextButton.interactable = _siblingNodeIds.Length > 1;
        }
    }
}
