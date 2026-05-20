using UnityEngine;
using TMPro;
using System;

namespace OriginXR.Knowledge
{
    /// <summary>
    /// 星图节点掌握状态
    /// </summary>
    public enum StarNodeState
    {
        Locked,      // 未解锁（灰色）
        Unlocked,    // 已解锁但未掌握（蓝色）
        Mastered,    // 已掌握（绿色 + 对勾）
        Weak         // 薄弱（红色脉冲）
    }

    /// <summary>
    /// 星图节点交互控制器
    /// 负责：
    /// 1. 挂载到每个 3D 知识星图节点上
    /// 2. 响应悬停/点击交互
    /// 3. 根据掌握状态切换视觉外观（材质颜色、发光、动画）
    /// 4. 点击通知打开知识详情面板
    /// </summary>
    public class StarNodeController : MonoBehaviour
    {
        [Header("组件")]
        [SerializeField] private MeshRenderer _meshRenderer;
        [SerializeField] private Light _nodeLight;
        [SerializeField] private GameObject _checkMark;
        [SerializeField] private GameObject _lockIcon;
        [SerializeField] private TextMeshPro _nameLabel;
        [SerializeField] private ParticleSystem _orbitParticles;

        [Header("状态颜色")]
        [SerializeField] private Color _lockedColor = new Color(0.5f, 0.5f, 0.5f);
        [SerializeField] private Color _unlockedColor = new Color(0.27f, 0.53f, 1f);
        [SerializeField] private Color _masteredColor = new Color(0.2f, 0.8f, 0.3f);
        [SerializeField] private Color _weakColor = new Color(1f, 0.3f, 0.3f);

        [Header("动画参数")]
        [SerializeField] private float _pulseSpeed = 2f;
        [SerializeField] private float _hoverScaleAmount = 0.15f;
        [SerializeField] private float _hoverDuration = 0.2f;

        // === 属性 ===
        public StarNodeData NodeData { get; private set; }
        public StarNodeState CurrentState { get; private set; }

        // === 内部状态 ===
        private Vector3 _originalScale;
        private bool _isHighlighted;
        private float _pulseTimer;
        private bool _isHovered;
        private float _hoverTimer;

        // === 事件 ===
        public static event Action<string> OnNodeClickedEvent;

        // === Unity 生命周期 ===

        private void Awake()
        {
            _originalScale = transform.localScale;
        }

        private void Start()
        {
            if (_meshRenderer == null)
                _meshRenderer = GetComponentInChildren<MeshRenderer>();
        }

        private void Update()
        {
            // 薄弱状态脉冲动画
            if (CurrentState == StarNodeState.Weak)
            {
                UpdateWeakPulse();
            }

            // 悬停缩放动画
            if (_isHovered)
            {
                _hoverTimer += Time.deltaTime;
                float t = Mathf.Clamp01(_hoverTimer / _hoverDuration);
                transform.localScale = Vector3.Lerp(_originalScale, _originalScale * (1f + _hoverScaleAmount), t);
            }
            else if (_hoverTimer > 0f && transform.localScale != _originalScale)
            {
                _hoverTimer -= Time.deltaTime;
                float t = Mathf.Clamp01(_hoverTimer / _hoverDuration);
                transform.localScale = Vector3.Lerp(_originalScale, _originalScale * (1f + _hoverScaleAmount), t);
            }
        }

        private void OnMouseEnter()
        {
            _isHovered = true;
            _hoverTimer = 0f;
        }

        private void OnMouseExit()
        {
            _isHovered = false;
        }

        private void OnMouseDown()
        {
            if (NodeData != null)
            {
                OnNodeClickedEvent?.Invoke(NodeData.id);
            }
        }

        // === 公共方法 ===

        /// <summary>初始化节点数据</summary>
        public void Initialize(StarNodeData data)
        {
            NodeData = data;

            if (_nameLabel != null)
                _nameLabel.text = data.name;

            // 根据深度调整大小
            float scale = data.depth == 0 ? 1.5f : (data.depth == 1 ? 1f : 0.6f);
            _originalScale = Vector3.one * scale;
            transform.localScale = _originalScale;

            SetState(GetStateFromMastery(data.masteryLevel));
        }

        /// <summary>设置掌握状态并刷新视觉</summary>
        public void SetState(StarNodeState state)
        {
            CurrentState = state;
            UpdateVisualByState(state);
        }

        /// <summary>设置节点位置</summary>
        public void SetPosition(Vector3 position)
        {
            transform.position = position;
        }

        /// <summary>设置高亮状态</summary>
        public void SetHighlight(bool highlight)
        {
            _isHighlighted = highlight;
            if (_nodeLight != null)
                _nodeLight.intensity = highlight ? 3f : 1f;
        }

        /// <summary>设置名称标签</summary>
        public void SetName(string name)
        {
            NodeData.name = name;
            if (_nameLabel != null) _nameLabel.text = name;
        }

        /// <summary>设置可见性</summary>
        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        /// <summary>播放节点解锁动画</summary>
        public void PlayUnlockAnimation()
        {
            // 缩放弹入 + 粒子
            transform.localScale = Vector3.zero;
            StartCoroutine(ScaleToSize(_originalScale, 0.5f, EaseOutBack));
            if (_orbitParticles != null) _orbitParticles.Play();
        }

        // === 私有方法 ===

        private void UpdateVisualByState(StarNodeState state)
        {
            if (_meshRenderer == null) return;

            Color targetColor = state switch
            {
                StarNodeState.Locked => _lockedColor,
                StarNodeState.Unlocked => _unlockedColor,
                StarNodeState.Mastered => _masteredColor,
                StarNodeState.Weak => _weakColor,
                _ => _unlockedColor
            };

            _meshRenderer.material.color = targetColor;

            // 对勾标记
            if (_checkMark != null)
                _checkMark.SetActive(state == StarNodeState.Mastered);

            // 锁定图标
            if (_lockIcon != null)
                _lockIcon.SetActive(state == StarNodeState.Locked);

            // 自发光
            if (_nodeLight != null)
            {
                _nodeLight.color = targetColor;
                _nodeLight.intensity = state == StarNodeState.Mastered ? 2f : 1f;
            }
        }

        private void UpdateWeakPulse()
        {
            _pulseTimer += Time.deltaTime * _pulseSpeed;
            float intensity = 1f + Mathf.Sin(_pulseTimer * Mathf.PI * 2f) * 0.4f;
            if (_nodeLight != null)
                _nodeLight.intensity = intensity;
        }

        private StarNodeState GetStateFromMastery(float masteryLevel)
        {
            if (masteryLevel >= 0.8f) return StarNodeState.Mastered;
            if (masteryLevel > 0f) return StarNodeState.Weak;
            return StarNodeState.Unlocked;
        }

        private System.Collections.IEnumerator ScaleToSize(Vector3 target, float duration, Func<float, float> easeFunc)
        {
            float elapsed = 0f;
            Vector3 start = transform.localScale;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                transform.localScale = Vector3.Lerp(start, target, easeFunc(t));
                yield return null;
            }
            transform.localScale = target;
        }

        private float EaseOutBack(float t)
        {
            float c1 = 1.70158f;
            float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }
    }

    /// <summary>
    /// 星图节点纯数据结构
    /// </summary>
    [Serializable]
    public class StarNodeData
    {
        public string id;
        public string name;
        public string parentId;
        public string subjectName;
        public string chapterName;
        public int depth;               // 0=学科, 1=章节, 2=知识点
        public float masteryLevel;      // 掌握度 0~1
        public Vector3 position;        // 力导向算法计算后位置
    }

    /// <summary>
    /// 知识边（前置依赖关系）
    /// </summary>
    [Serializable]
    public class KnowledgeEdgeData
    {
        public string fromNodeId;
        public string toNodeId;
        public string dependencyType;   // "prerequisite" 前置依赖
    }
}
