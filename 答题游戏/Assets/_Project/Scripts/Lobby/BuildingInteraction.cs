using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

namespace OriginXR.Lobby
{
    /// <summary>
    /// 建筑交互控制器（2D 版）
    /// 负责：
    /// 1. 检测玩家与场景中各建筑入口的接近距离（2D 碰撞检测）
    /// 2. 显示交互提示 UI（按键提示 + 建筑名称）
    /// 3. 处理交互触发（切换场景或打开功能面板）
    /// </summary>
    public class BuildingInteraction : MonoBehaviour
    {
        [Header("检测配置")]
        [SerializeField] private Transform _playerTransform;
        [SerializeField] private float _interactionRange = 2f;
        [SerializeField] private LayerMask _buildingLayer;

        [Header("提示 UI")]
        [SerializeField] private Canvas _promptCanvas;
        [SerializeField] private TextMeshProUGUI _promptText;
        [SerializeField] private TextMeshProUGUI _buildingNameText;
        [SerializeField] private Button _mobileInteractButton;

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

        public void Interact()
        {
            if (_currentTarget != null && _isPromptVisible)
            {
                PerformBuildingAction(_currentTarget);
            }
        }

        public void ShowPrompt(string buildingName, string actionText)
        {
            if (_promptCanvas != null)
                _promptCanvas.gameObject.SetActive(true);
            if (_buildingNameText != null) _buildingNameText.text = buildingName;

            if (_promptText != null)
            {
                string prompt = _useMobileInput ? "点击进入" : $"按 [{_interactKey}] {actionText}";
                _promptText.text = prompt;
            }

            if (_mobileInteractButton != null)
                _mobileInteractButton.gameObject.SetActive(_useMobileInput);

            _isPromptVisible = true;
        }

        public void HidePrompt()
        {
            if (_promptCanvas != null) _promptCanvas.gameObject.SetActive(false);
            if (_mobileInteractButton != null) _mobileInteractButton.gameObject.SetActive(false);
            _isPromptVisible = false;
        }

        // === 私有方法 ===

        private void CheckNearbyBuildings()
        {
            if (_playerTransform == null) return;

            // 2D 球形检测
            Collider2D[] colliders = Physics2D.OverlapCircleAll(_playerTransform.position, _interactionRange, _buildingLayer);

            _currentTarget = null;
            float closestDist = float.MaxValue;

            foreach (var col in colliders)
            {
                BuildingEntry entry = col.GetComponentInParent<BuildingEntry>();
                if (entry == null) continue;

                float dist = Vector2.Distance(_playerTransform.position, col.ClosestPoint(_playerTransform.position));
                if (dist < closestDist && dist <= _interactionRange)
                {
                    closestDist = dist;
                    _currentTarget = entry;
                }
            }

            if (_currentTarget != null)
            {
                if (_currentTarget != _lastTarget)
                {
                    ShowPrompt(_currentTarget.buildingName, _currentTarget.interactActionText);
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
                Interact();
        }

        private void PerformBuildingAction(BuildingEntry building)
        {
            Debug.Log($"[BuildingInteraction] 交互: {building.buildingName}");

            building.onInteract?.Invoke();

            if (!string.IsNullOrEmpty(building.targetSceneName))
            {
                Core.SceneLoader.Instance?.LoadScene(building.targetSceneName);
            }

            if (!string.IsNullOrEmpty(building.targetPanelName))
            {
                UI.UIManager.Instance?.ShowPanel(building.targetPanelName);
            }

            HidePrompt();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, _interactionRange);
        }
    }

    /// <summary>
    /// 建筑入口组件（2D 版）
    /// 挂载到场景建筑 GameObject 上（需有 Collider2D）
    /// </summary>
    public class BuildingEntry : MonoBehaviour
    {
        [Header("建筑信息")]
        public string buildingId = "";
        public string buildingName = "";
        public BuildingType type;
        public string interactActionText = "进入";

        [Header("目标配置")]
        public string targetSceneName = "";
        public string targetPanelName = "";

        [Header("事件")]
        public UnityEvent onInteract;

        public enum BuildingType
        {
            TeachingBuilding,   // 教学大楼 → 关卡选择
            Arena,              // 竞技场   → PVP
            GuildHall,          // 公会大厅
            KnowledgeTower,     // 知识塔   → 无尽模式
            BulletinBoard,      // 布告栏   → 每日挑战
            Shop,               // 商店     → 商城面板
            PersonalCenter      // 个人中心 → 设置/成就
        }

        private void OnDrawGizmos()
        {
            // Scene 视图绘制入口标记
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
            Gizmos.DrawWireCube(transform.position, new Vector3(1.5f, 1.5f, 0f));

#if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 1.5f, $"{buildingName}\n[{type}]");
#endif
        }
    }
}
