using UnityEngine;
using System.Collections.Generic;

namespace OriginXR.Knowledge
{
    /// <summary>
    /// 知识星图生成器（3D 力导向图）
    /// 负责：
    /// 1. 根据知识点树数据生成 3D 星图布局（学科→星系，章节→星球，知识点→卫星）
    /// 2. 力导向算法计算节点位置（排斥力 + 吸引力迭代）
    /// 3. LineRenderer 绘制前置依赖关系连线（箭头）
    /// 4. 缩放/旋转/平移交互响应
    /// </summary>
    public class StarMapGenerator : MonoBehaviour
    {
        [Header("生成配置")]
        [SerializeField] private GameObject _starNodePrefab;
        [SerializeField] private Material _edgeMaterial;
        [SerializeField] private float _edgeWidth = 0.05f;
        [SerializeField] private float _galaxyRadius = 10f;
        [SerializeField] private float _planetRadius = 5f;
        [SerializeField] private float _satelliteRadius = 3f;

        [Header("力导向算法参数")]
        [SerializeField] private int _forceIterations = 100;
        [SerializeField] private float _repulsionForce = 500f;
        [SerializeField] private float _attractionForce = 0.01f;
        [SerializeField] private float _dampingFactor = 0.9f;
        [SerializeField] private float _minDistance = 2f;
        [SerializeField] private float _maxVelocity = 5f;

        [Header("交互")]
        [SerializeField] private float _rotateSpeed = 100f;
        [SerializeField] private float _panSpeed = 5f;
        [SerializeField] private float _zoomSpeed = 50f;
        [SerializeField] private float _minZoom = 2f;
        [SerializeField] private float _maxZoom = 30f;

        // === 运行时数据 ===
        private Dictionary<string, StarNodeController> _nodeMap = new Dictionary<string, StarNodeController>();
        private Dictionary<string, Transform> _subjectRoots = new Dictionary<string, Transform>();
        private List<LineRenderer> _edgeLines = new List<LineRenderer>();
        private List<KnowledgeEdgeData> _edges = new List<KnowledgeEdgeData>();
        private List<StarNodeData> _nodeDataList = new List<StarNodeData>();

        // === 相机交互 ===
        private Camera _mainCamera;
        private Vector3 _lastMousePosition;
        private float _currentZoom = 10f;
        private Vector3 _panOffset;

        // === Unity 生命周期 ===

        private void Start()
        {
            _mainCamera = Camera.main;
            _currentZoom = _mainCamera != null ? _mainCamera.transform.position.magnitude : 10f;
        }

        private void Update()
        {
            HandleInteraction();
        }

        // === 公共方法 ===

        /// <summary>根据知识点树 JSON 生成星图</summary>
        public void GenerateStarMap(string knowledgeTreeJson)
        {
            Clear();
            _nodeDataList = ParseKnowledgeTree(knowledgeTreeJson);
            if (_nodeDataList.Count == 0) return;

            // 力导向布局计算位置
            ApplyForceDirectedLayout(_nodeDataList, _edges);

            // 创建节点实例
            foreach (var nodeData in _nodeDataList)
            {
                StarNodeController node = CreateNodeObject(nodeData);
                if (node != null)
                    _nodeMap[nodeData.id] = node;
            }

            // 绘制连线
            foreach (var edge in _edges)
            {
                if (_nodeMap.TryGetValue(edge.fromNodeId, out var from) &&
                    _nodeMap.TryGetValue(edge.toNodeId, out var to))
                {
                    CreateEdgeLine(from.transform.position, to.transform.position);
                }
            }
        }

        /// <summary>刷新所有节点的掌握状态</summary>
        public void RefreshMasteryStatus(Dictionary<string, float> masteryData)
        {
            foreach (var kvp in masteryData)
            {
                if (_nodeMap.TryGetValue(kvp.Key, out var node))
                {
                    node.NodeData.masteryLevel = kvp.Value;
                    node.SetState(GetStateFromMastery(kvp.Value));
                }
            }
        }

        /// <summary>聚焦到指定节点</summary>
        public void FocusOnNode(string nodeId)
        {
            if (!_nodeMap.TryGetValue(nodeId, out var node)) return;

            Vector3 targetPos = node.transform.position - _mainCamera.transform.forward * 5f;
            _panOffset = targetPos;
        }

        /// <summary>高亮指定知识点及其前置路径</summary>
        public void HighlightPath(string targetNodeId)
        {
            ClearHighlight();

            // BFS 追溯前置路径
            HashSet<string> pathNodes = new HashSet<string>();
            FindPrerequisitePath(targetNodeId, pathNodes);

            foreach (var nodeId in pathNodes)
            {
                if (_nodeMap.TryGetValue(nodeId, out var node))
                    node.SetHighlight(true);
            }
        }

        /// <summary>清除所有高亮</summary>
        public void ClearHighlight()
        {
            foreach (var kvp in _nodeMap)
                kvp.Value.SetHighlight(false);
        }

        /// <summary>重置视图</summary>
        public void ResetView()
        {
            _panOffset = Vector3.zero;
            _currentZoom = 10f;
        }

        /// <summary>清除所有节点和连线</summary>
        public void Clear()
        {
            foreach (var node in _nodeMap.Values)
                if (node != null) Destroy(node.gameObject);

            foreach (var line in _edgeLines)
                if (line != null) Destroy(line.gameObject);

            foreach (var root in _subjectRoots.Values)
                if (root != null) Destroy(root.gameObject);

            _nodeMap.Clear();
            _subjectRoots.Clear();
            _edgeLines.Clear();
            _edges.Clear();
            _nodeDataList.Clear();
        }

        // === 私有：力导向布局算法 ===

        private void ApplyForceDirectedLayout(List<StarNodeData> nodes, List<KnowledgeEdgeData> edges)
        {
            // 初始化位置（按学科分组）
            Dictionary<string, List<StarNodeData>> subjectGroups = new Dictionary<string, List<StarNodeData>>();
            Dictionary<string, int> subjectIndices = new Dictionary<string, int>();
            Dictionary<string, int> subjectCounts = new Dictionary<string, int>();

            foreach (var node in nodes)
            {
                if (!subjectGroups.ContainsKey(node.subjectName))
                {
                    subjectGroups[node.subjectName] = new List<StarNodeData>();
                    subjectIndices[node.subjectName] = subjectIndices.Count;
                }
                subjectGroups[node.subjectName].Add(node);
            }

            int totalSubjects = subjectGroups.Count;
            foreach (var kvp in subjectGroups)
            {
                float angle = (float)subjectIndices[kvp.Key] / totalSubjects * Mathf.PI * 2f;
                Vector3 center = new Vector3(Mathf.Cos(angle) * _galaxyRadius, 0f, Mathf.Sin(angle) * _galaxyRadius);

                var group = kvp.Value;
                int count = group.Count;
                for (int i = 0; i < count; i++)
                {
                    float nodeAngle = (float)i / count * Mathf.PI * 2f;
                    group[i].position = center + new Vector3(
                        Mathf.Cos(nodeAngle) * _planetRadius * (0.5f + group[i].depth * 0.5f),
                        0f,
                        Mathf.Sin(nodeAngle) * _planetRadius * (0.5f + group[i].depth * 0.5f)
                    );
                }
            }

            // 力导向迭代
            for (int iter = 0; iter < _forceIterations; iter++)
            {
                // 计算每个节点的受力
                Vector3[] forces = new Vector3[nodes.Count];

                // 排斥力（所有节点对之间）
                for (int i = 0; i < nodes.Count; i++)
                {
                    for (int j = i + 1; j < nodes.Count; j++)
                    {
                        Vector3 dir = nodes[i].position - nodes[j].position;
                        float dist = dir.magnitude;
                        if (dist < _minDistance) dist = _minDistance;
                        Vector3 repulsion = dir.normalized * (_repulsionForce / (dist * dist));
                        forces[i] += repulsion;
                        forces[j] -= repulsion;
                    }
                }

                // 吸引力（有连线关系的节点之间）
                foreach (var edge in edges)
                {
                    var from = nodes.Find(n => n.id == edge.fromNodeId);
                    var to = nodes.Find(n => n.id == edge.toNodeId);
                    if (from == null || to == null) continue;

                    Vector3 dir = to.position - from.position;
                    float dist = dir.magnitude;
                    Vector3 attraction = dir * (_attractionForce * dist);
                    int fromIdx = nodes.IndexOf(from);
                    int toIdx = nodes.IndexOf(to);
                    forces[fromIdx] += attraction;
                    forces[toIdx] -= attraction;
                }

                // 应用力
                for (int i = 0; i < nodes.Count; i++)
                {
                    forces[i] *= _dampingFactor;
                    if (forces[i].magnitude > _maxVelocity)
                        forces[i] = forces[i].normalized * _maxVelocity;
                    nodes[i].position += forces[i];
                }
            }
        }

        // === 私有：节点和连线创建 ===

        private StarNodeController CreateNodeObject(StarNodeData nodeData)
        {
            if (_starNodePrefab == null)
            {
                Debug.LogWarning("[StarMapGenerator] 节点预制体未配置");
                return null;
            }

            GameObject obj = Instantiate(_starNodePrefab, nodeData.position, Quaternion.identity, transform);
            obj.name = $"Node_{nodeData.name}";
            StarNodeController controller = obj.GetComponent<StarNodeController>();
            if (controller == null)
                controller = obj.AddComponent<StarNodeController>();

            controller.Initialize(nodeData);
            controller.SetState(GetStateFromMastery(nodeData.masteryLevel));
            return controller;
        }

        private void CreateEdgeLine(Vector3 from, Vector3 to)
        {
            GameObject lineObj = new GameObject("EdgeLine", typeof(LineRenderer));
            lineObj.transform.SetParent(transform);
            LineRenderer lr = lineObj.GetComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.SetPosition(0, from);
            lr.SetPosition(1, to);
            lr.startWidth = _edgeWidth;
            lr.endWidth = _edgeWidth;
            lr.material = _edgeMaterial != null ? _edgeMaterial : new Material(Shader.Find("Sprites/Default"));
            lr.startColor = Color.gray;
            lr.endColor = Color.gray;
            _edgeLines.Add(lr);
        }

        // === 私有：交互 ===

        private void HandleInteraction()
        {
            if (_mainCamera == null) return;

            // 鼠标滚轮缩放
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.001f)
            {
                _currentZoom -= scroll * _zoomSpeed;
                _currentZoom = Mathf.Clamp(_currentZoom, _minZoom, _maxZoom);
            }

            // 双指缩放（移动端）
            if (Input.touchCount == 2)
            {
                Touch t0 = Input.GetTouch(0);
                Touch t1 = Input.GetTouch(1);
                Vector2 prev0 = t0.position - t0.deltaPosition;
                Vector2 prev1 = t1.position - t1.deltaPosition;
                float prevDist = (prev0 - prev1).magnitude;
                float currDist = (t0.position - t1.position).magnitude;
                _currentZoom -= (currDist - prevDist) * 0.1f;
                _currentZoom = Mathf.Clamp(_currentZoom, _minZoom, _maxZoom);
            }

            // 鼠标拖拽旋转/平移
            if (Input.GetMouseButton(0) || Input.GetMouseButton(1))
            {
                Vector3 delta = Input.mousePosition - _lastMousePosition;
                if (Input.GetMouseButton(0))
                {
                    // 左键旋转
                    transform.Rotate(Vector3.up, -delta.x * _rotateSpeed * Time.deltaTime, Space.World);
                    transform.Rotate(Vector3.right, delta.y * _rotateSpeed * Time.deltaTime, Space.World);
                }
                else if (Input.GetMouseButton(1))
                {
                    // 右键平移
                    _panOffset += new Vector3(-delta.x, -delta.y, 0f) * _panSpeed * Time.deltaTime;
                }
            }

            _lastMousePosition = Input.mousePosition;
        }

        // === 私有：工具方法 ===

        private void FindPrerequisitePath(string nodeId, HashSet<string> pathNodes)
        {
            pathNodes.Add(nodeId);
            foreach (var edge in _edges)
            {
                if (edge.toNodeId == nodeId && !pathNodes.Contains(edge.fromNodeId))
                {
                    FindPrerequisitePath(edge.fromNodeId, pathNodes);
                }
            }
        }

        private StarNodeState GetStateFromMastery(float masteryLevel)
        {
            if (masteryLevel >= 0.8f) return StarNodeState.Mastered;
            if (masteryLevel > 0f) return StarNodeState.Weak;
            return StarNodeState.Unlocked;
        }

        private List<StarNodeData> ParseKnowledgeTree(string json)
        {
            // TODO: 实际解析后端返回的知识点树 JSON
            // 开发阶段返回模拟数据
            return new List<StarNodeData>
            {
                new StarNodeData { id = "cs", name = "计算机科学", subjectName = "计算机科学", depth = 0, masteryLevel = 1f },
                new StarNodeData { id = "lang", name = "编程语言", subjectName = "计算机科学", parentId = "cs", chapterName = "编程语言", depth = 1, masteryLevel = 0.9f },
                new StarNodeData { id = "cpp", name = "C++", subjectName = "计算机科学", parentId = "lang", chapterName = "编程语言", depth = 2, masteryLevel = 0.5f },
                new StarNodeData { id = "csharp", name = "C#", subjectName = "计算机科学", parentId = "lang", chapterName = "编程语言", depth = 2, masteryLevel = 0.8f },
                new StarNodeData { id = "ds", name = "数据结构", subjectName = "计算机科学", parentId = "cs", chapterName = "数据结构", depth = 1, masteryLevel = 0.3f },
            };
        }
    }
}
