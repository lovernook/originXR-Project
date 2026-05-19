using UnityEngine;
using System.Collections.Generic;

namespace OriginXR.Knowledge
{
    /// <summary>
    /// 知识星图生成器（3D 力导向图）
    /// 负责：
    /// 1. 根据后端返回的知识点树形数据生成 3D 星图布局
    /// 2. 使用力导向算法（Force-Directed Layout）计算节点位置
    /// 3. 学科 -> 星系，章节 -> 星球，知识点 -> 卫星 的三级层级
    /// 4. 用 LineRenderer 绘制知识点之间的前置依赖连线（箭头）
    /// 5. 支持缩放、旋转、平移交互
    ///
    /// 数据来源：GET /api/v1/admin/knowledge-points（管理端）
    ///           GET /api/v1/users/:id/progress（学员知识点掌握度）
    ///
    /// 视觉表现：
    ///   已掌握知识点：绿色发光 + 对勾标记
    ///   薄弱知识点：红色闪烁 + 推荐复习入口
    ///   未解锁知识点：灰色节点 + 解锁条件提示
    /// </summary>
    public class StarMapGenerator : MonoBehaviour
    {
        // === 配置 ===
        /// <summary>节点预制体</summary>
        [SerializeField] private GameObject _starNodePrefab;

        /// <summary>连线材质</summary>
        [SerializeField] private Material _lineMaterial;

        /// <summary>星系半径</summary>
        [SerializeField] private float _galaxyRadius = 10f;

        /// <summary>卫星与行星的距离</summary>
        [SerializeField] private float _satelliteDistance = 3f;

        /// <summary>力导向算法迭代次数</summary>
        [SerializeField] private int _forceIterations = 100;

        /// <summary>排斥力系数</summary>
        [SerializeField] private float _repulsionForce = 500f;

        /// <summary>吸引力系数</summary>
        [SerializeField] private float _attractionForce = 0.01f;

        // === 运行时数据 ===
        private Dictionary<string, StarNodeController> _nodeMap;   // 节点ID -> 节点控制器
        private Dictionary<string, GameObject> _subjectRoots;      // 学科根节点（星系中心）
        private List<LineRenderer> _edgeLines;                     // 所有连线渲染器
        private List<KnowledgeEdgeData> _edges;                    // 边数据

        // === Unity 生命周期 ===
        private void Start() { }

        // === 公共方法 ===

        /// <summary>根据知识点树数据生成星图</summary>
        /// <param name="knowledgeTree">知识点树 JSON 数据</param>
        public void GenerateStarMap(string knowledgeTreeJson) { }

        /// <summary>刷新所有节点的掌握状态</summary>
        /// <param name="masteryData">掌握度数据 { nodeId: masteryLevel }</param>
        public void RefreshMasteryStatus(Dictionary<string, float> masteryData) { }

        /// <summary>聚焦到指定节点（相机飞到该节点附近）</summary>
        public void FocusOnNode(string nodeId) { }

        /// <summary>高亮显示指定知识点及其前置路径</summary>
        public void HighlightPath(string targetNodeId) { }

        /// <summary>清除所有高亮</summary>
        public void ClearHighlight() { }

        /// <summary>重置星图到默认视角</summary>
        public void ResetView() { }

        // === 私有方法 ===

        /// <summary>力导向布局算法</summary>
        private void ApplyForceDirectedLayout(List<StarNodeData> nodes, List<KnowledgeEdgeData> edges) { }

        /// <summary>从节点数据创建 3D 节点实例</summary>
        private StarNodeController CreateNodeObject(StarNodeData nodeData) { return null; }

        /// <summary>绘制两个节点之间的连线（带箭头）</summary>
        private LineRenderer CreateEdgeLine(Vector3 from, Vector3 to) { return null; }

        /// <summary>刷新连线位置</summary>
        private void UpdateEdgeLines() { }

        /// <summary>解析 JSON 数据为节点数据列表</summary>
        private List<StarNodeData> ParseKnowledgeTree(string json) { return null; }
    }

    /// <summary>
    /// 星图节点数据（纯数据结构，非 MonoBehaviour）
    /// </summary>
    [System.Serializable]
    public class StarNodeData
    {
        public string Id;
        public string Name;
        public string ParentId;          // 父节点ID
        public string SubjectName;       // 所属学科
        public string ChapterName;       // 所属章节
        public int Depth;                // 层级深度（0=学科, 1=章节, 2=知识点）
        public float MasteryLevel;       // 掌握度（0~1）
        public StarNodeState State;      // 掌握状态
        public Vector3 Position;         // 力导向算法计算后的位置
    }

    public enum StarNodeState
    {
        Locked,        // 未解锁
        Unlocked,      // 已解锁但未掌握
        Mastered,      // 已掌握
        Weak           // 薄弱（需复习）
    }

    /// <summary>
    /// 知识边数据（前置依赖关系）
    /// </summary>
    [System.Serializable]
    public class KnowledgeEdgeData
    {
        public string FromNodeId;
        public string ToNodeId;
        public string DependencyType;   // "prerequisite" 前置依赖
    }
}
