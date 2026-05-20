using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using System.Collections.Generic;
using static OriginXR.Lobby.BuildingEntry;

namespace OriginXR.Lobby
{
    /// <summary>
    /// 建筑交互控制器
    /// 负责：
    /// 1. 检测玩家与 LobbyScene 中各建筑入口的距离
    /// 2. 显示交互提示 UI（按键提示 + 建筑名称）
    /// 3. 处理交互触发（切换场景或打开功能面板）
    ///
    /// 交互流程：
    ///   PC端：靠近建筑 → 显示"按 E 进入" → 按 E 触发
    ///   移动端：靠近建筑 → 显示"点击进入"按钮 → 点击触发
    /// </summary>
    public class BuildingInteraction : MonoBehaviour
    {
        [Header("检测配置")]
        [SerializeField] private Transform _playerTransform;
        [SerializeField] private float _interactionRange = 3f;
        [SerializeField] private LayerMask _buildingLayer;

        [Header("提示 UI")]
        [SerializeField] private Canvas _promptCanvas;
        [SerializeField] private TextMeshProUGUI _promptText;
        [SerializeField] private TextMeshProUGUI _buildingNameText;
        [SerializeField] private Button _mobileInteractButton;     // 移动端交互按钮

        [Header("输入")]
        [SerializeField] private bool _useMobileInput;
        [SerializeField] private KeyCode _interactKey = KeyCode.E;

        // === 状态 ===
        private BuildingEntry _currentTarget;
        private BuildingEntry _lastTarget;
        private bool _isPromptVisible;

        // === Unity 生命周期 ===

        private void Start()
        {
            if (_playerTransform == null)
            {
                // 尝试自动查找玩家
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) _playerTransform = player.transform;
            }

            if (_promptCanvas != null)
                _promptCanvas.gameObject.SetActive(false);

            if (_mobileInteractButton != null)
            {
                _mobileInteractButton.onClick.AddListener(Interact);
                _mobileInteractButton.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            CheckNearbyBuildings();
            HandleInteractInput();
        }

        // === 公共方法 ===

        /// <summary>手动触发当前建筑的交互</summary>
        public void Interact()
        {
            if (_currentTarget != null && _isPromptVisible)
            {
                PerformBuildingAction(_currentTarget);
            }
        }

        /// <summary>显示交互提示</summary>
        public void ShowPrompt(string buildingName, string actionText)
        {
            if (_promptCanvas != null)
            {
                _promptCanvas.gameObject.SetActive(true);
                if (_buildingNameText != null) _buildingNameText.text = buildingName;
            }

            if (_promptText != null)
            {
                string prompt = _useMobileInput ? "点击进入" : $"按 [{_interactKey}] {actionText}";
                _promptText.text = prompt;
            }

            if (_mobileInteractButton != null)
                _mobileInteractButton.gameObject.SetActive(_useMobileInput);

            _isPromptVisible = true;
        }

        /// <summary>隐藏交互提示</summary>
        public void HidePrompt()
        {
            if (_promptCanvas != null)
                _promptCanvas.gameObject.SetActive(false);

            if (_mobileInteractButton != null)
                _mobileInteractButton.gameObject.SetActive(false);

            _isPromptVisible = false;
        }

        // === 私有方法 ===

        private void CheckNearbyBuildings()
        {
            if (_playerTransform == null) return;

            // 球形检测范围内的建筑
            Collider[] colliders = Physics.OverlapSphere(_playerTransform.position, _interactionRange, _buildingLayer);

            _currentTarget = null;
            float closestDist = float.MaxValue;

            foreach (Collider col in colliders)
            {
                BuildingEntry entry = col.GetComponentInParent<BuildingEntry>();
                if (entry == null) continue;

                float dist = Vector3.Distance(_playerTransform.position, col.ClosestPoint(_playerTransform.position));
                if (dist < closestDist && dist <= _interactionRange)
                {
                    closestDist = dist;
                    _currentTarget = entry;
                }
            }

            // 更新提示
            if (_currentTarget != null)
            {
                if (_currentTarget != _lastTarget)
                {
                    ShowPrompt(_currentTarget.buildingName, _currentTarget.actionText);
                    _lastTarget = _currentTarget;
                }
            }
            else
            {
                if (_lastTarget != null)
                {
                    HidePrompt();
                    _lastTarget = null;
                }
            }
        }

        private void HandleInteractInput()
        {
            if (!_isPromptVisible || _currentTarget == null) return;

            if (!_useMobileInput && Input.GetKeyDown(_interactKey))
            {
                Interact();
            }
        }

        private void PerformBuildingAction(BuildingEntry building)
        {
            Debug.Log($"[BuildingInteraction] 触发建筑交互: {building.buildingName} (类型: {building.type})");

            // 触发自定义事件
            building.onInteract?.Invoke();

            // 根据建筑类型执行不同逻辑
            switch (building.type)
            {
                case BuildingType.TeachingBuilding:
                case BuildingType.Arena:
                case BuildingType.GuildHall:
                case BuildingType.KnowledgeTower:
                    // 切换场景
                    if (!string.IsNullOrEmpty(building.targetSceneName))
                    {
                        Core.SceneLoader sceneLoader = Core.SceneLoader.Instance;
                        if (sceneLoader != null)
                            sceneLoader.LoadScene(building.targetSceneName);
                    }
                    break;

                case BuildingType.BulletinBoard:
                case BuildingType.Shop:
                case BuildingType.PersonalCenter:
                    // 打开 UI 面板
                    if (!string.IsNullOrEmpty(building.targetPanelName))
                    {
                        UI.UIManager uiManager = UI.UIManager.Instance;
                        if (uiManager != null)
                            uiManager.ShowPanel(building.targetPanelName);
                    }
                    break;
            }

            // 隐藏提示
            HidePrompt();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, _interactionRange);
        }
    }

    /// <summary>
    /// 建筑入口组件（挂载到场景建筑 GameObject 上）
    /// </summary>
    public class BuildingEntry : MonoBehaviour
    {
        [Header("建筑信息")]
        public string buildingId = "";
        public string buildingName = "";
        public BuildingType type;
        public string actionText = "进入";
        public string description;

        [Header("目标配置")]
        public string targetSceneName = "";      // 跳转的目标场景名
        public string targetPanelName = "";      // 打开的目标面板名
        public Vector3 teleportTargetPosition;    // 传送后玩家的目标位置

        [Header("事件")]
        public UnityEvent onInteract;

        public enum BuildingType
        {
            TeachingBuilding,   // 教学大楼 → 关卡选择 / BattleScene
            Arena,              // 竞技场   → PVP 匹配
            GuildHall,          // 公会大厅 → GuildScene
            KnowledgeTower,     // 知识塔   → 无尽模式
            BulletinBoard,      // 布告栏   → 每日挑战
            Shop,               // 商店     → 道具购买面板
            PersonalCenter      // 个人中心 → 背包/成就/设置
        }

        private void OnDrawGizmos()
        {
            // 绘制建筑入口标记
            Gizmos.color = type switch
            {
                BuildingType.TeachingBuilding => Color.blue,
                BuildingType.Arena => Color.red,
                BuildingType.GuildHall => Color.magenta,
                BuildingType.KnowledgeTower => Color.yellow,
                BuildingType.BulletinBoard => Color.cyan,
                BuildingType.Shop => Color.green,
                BuildingType.PersonalCenter => Color.white,
                _ => Color.gray
            };

            Gizmos.DrawSphere(transform.position, 0.5f);
            Gizmos.DrawWireCube(transform.position + Vector3.up * 1.5f, new Vector3(1f, 2f, 0.2f));

#if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2.5f, $"[{type}]\n{buildingName}");
#endif
        }
    }
}
