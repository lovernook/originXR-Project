using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace OriginXR.Knowledge
{
    /// <summary>
    /// 知识点详情面板（UI）
    /// 负责：
    /// 1. 显示所选知识点的详细信息（概念卡片形式）
    /// 2. 支持多种展示形式：概念卡片、动画演示、记忆闪卡、思维导图、进度热力图
    /// 3. 展示关联题目列表（正确率/掌握度）
    /// 4. 提供"开始复习"按钮入口，跳转到对应关卡或练习题
    /// 5. 展示前置依赖知识点路径
    ///
    /// 知识展示形式（七种）由后端数据中的 displayType 决定：
    ///   concept_card     -> UGUI ScrollView + 富文本
    ///   animation_demo   -> Timeline 动画 + 图文解说
    ///   memory_flashcard -> 卡片翻转动画 + 间隔重复
    ///   mind_map         -> 树形 UI + LineRenderer 连线
    ///   heat_map         -> Shader 渲染的日历热力图
    ///   comparison_table -> UGUI 动态表格布局
    ///   3d_demo          -> 3D 模型演示
    /// </summary>
    public class KnowledgeDetailPanel : MonoBehaviour
    {
        // === UI 组件 ===
        [SerializeField] private Canvas _panelCanvas;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _descriptionText;
        [SerializeField] private TextMeshProUGUI _masteryText;       // 掌握度百分比
        [SerializeField] private RectTransform _contentContainer;     // 内容容器（不同展示形式挂载于此）
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _startReviewButton;          // 开始复习按钮
        [SerializeField] private Button _prevButton;                 // 上一个知识点
        [SerializeField] private Button _nextButton;                 // 下一个知识点

        // === 内容预制体（七种展示形式） ===
        [SerializeField] private GameObject _conceptCardPrefab;
        [SerializeField] private GameObject _animationDemoPrefab;
        [SerializeField] private GameObject _memoryFlashcardPrefab;
        [SerializeField] private GameObject _mindMapPrefab;
        [SerializeField] private GameObject _heatMapPrefab;
        [SerializeField] private GameObject _comparisonTablePrefab;
        [SerializeField] private GameObject _3dDemoPrefab;

        // === 状态 ===
        private string _currentNodeId;
        private StarNodeData _currentNodeData;
        private bool _isPanelOpen;

        // === Unity 生命周期 ===
        private void Start() { }
        private void OnEnable() { }
        private void OnDisable() { }

        // === 公共方法 ===

        /// <summary>打开面板并显示知识点详情</summary>
        /// <param name="nodeId">知识点节点ID</param>
        public void Show(string nodeId) { }

        /// <summary>关闭面板</summary>
        public void Hide() { }

        /// <summary>切换上一个知识点</summary>
        public void NavigatePrev() { }

        /// <summary>切换下一个知识点</summary>
        public void NavigateNext() { }

        /// <summary>刷新面板内容（掌握度变化时）</summary>
        public void Refresh() { }

        /// <summary>开始复习此知识点（跳转到对应关卡）</summary>
        public void StartReview() { }

        // === 私有方法 ===
        private void LoadKnowledgeDetail(string nodeId) { }
        private void SwitchContentView(string displayType) { }
        private void SetupConceptCard(string dataJson) { }
        private void SetupAnimationDemo(string dataJson) { }
        private void SetupMemoryFlashcard(string dataJson) { }
        private void SetupMindMap(string dataJson) { }
        private void SetupHeatMap(string dataJson) { }
        private void SetupComparisonTable(string dataJson) { }
        private void Setup3DDemo(string dataJson) { }
        private void PlayOpenAnimation() { }
        private void PlayCloseAnimation() { }
    }
}
