using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace OriginXR.Knowledge
{
    /// <summary>
    /// 星图节点交互控制器
    /// 负责：
    /// 1. 挂载到每个知识星图的 3D 节点 GameObject 上
    /// 2. 响应鼠标悬停/点击/拖拽交互
    /// 3. 根据掌握状态切换视觉效果（材质颜色/发光/动画）
    /// 4. 点击时通知 KnowledgeDetailPanel 展示详情
    /// 5. 播放状态切换动画（解锁/掌握/薄弱闪烁）
    ///
    /// 节点层级与外观：
    ///   学科节点（星系中心）：大型球体 + 光环粒子效果
    ///   章节节点（星球）：中型球体 + 轨道环
    ///   知识点节点（卫星）：小型球体
    ///
    /// 状态颜色：
    ///   Locked   -> 灰色 #888888
    ///   Unlocked -> 蓝色 #4488FF
    ///   Mastered -> 绿色 #44FF44 + 对勾标记
    ///   Weak     -> 红色脉冲 #FF4444
    /// </summary>
    public class StarNodeController : MonoBehaviour
    {
        // === 组件引用 ===
        [SerializeField] private MeshRenderer _meshRenderer;
        [SerializeField] private Light _nodeLight;             // 节点自发光
        [SerializeField] private GameObject _checkMark;        // 对勾标记（Mastered状态）
        [SerializeField] private GameObject _lockIcon;          // 锁定图标
        [SerializeField] private TextMeshPro _nameLabel;       // 节点名称
        [SerializeField] private ParticleSystem _orbitParticles; // 轨道粒子（星系/行星）

        // === 数据 ===
        public StarNodeData NodeData { get; private set; }
        public StarNodeState CurrentState { get; private set; }

        // === 动画参数 ===
        [SerializeField] private float _pulseSpeed = 2f;       // 薄弱闪烁速度
        [SerializeField] private float _hoverScale = 1.2f;     // 悬停放大倍数
        [SerializeField] private float _animDuration = 0.3f;   // 动画过渡时长

        // === Unity 生命周期 ===
        private void Start() { }
        private void Update() { }
        private void OnMouseEnter() { }
        private void OnMouseExit() { }
        private void OnMouseDown() { }
        private void OnMouseDrag() { }

        // === 公共方法 ===

        /// <summary>初始化节点数据，设置初始状态和外观</summary>
        public void Initialize(StarNodeData data) { NodeData = data; }

        /// <summary>更新掌握状态并切换视觉效果</summary>
        public void SetState(StarNodeState state) { CurrentState = state; }

        /// <summary>设置节点位置（用于力导向布局更新）</summary>
        public void SetPosition(Vector3 position) { }

        /// <summary>播放节点解锁动画</summary>
        public void PlayUnlockAnimation() { }

        /// <summary>播放节点掌握动画（粒子爆发 + 对勾出现）</summary>
        public void PlayMasteredAnimation() { }

        /// <summary>设置名称标签显示</summary>
        public void SetName(string name) { }

        /// <summary>显示/隐藏节点</summary>
        public void SetVisible(bool visible) { }

        // === 私有方法 ===
        private void UpdateVisualByState(StarNodeState state) { }
        private void UpdateWeakPulseEffect() { }
        private void OnNodeClicked() { }

        // === 事件 ===
        /// <summary>节点被点击事件，参数为节点ID</summary>
        public static event Action<string> OnNodeClickedEvent;
    }
}
