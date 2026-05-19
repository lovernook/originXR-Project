using UnityEngine;
using UnityEngine.Events;
using System;

namespace OriginXR.Lobby
{
    /// <summary>
    /// 建筑交互控制器
    /// 负责：
    /// 1. 检测玩家与 LobbyScene 中各建筑入口的接近距离
    /// 2. 显示交互提示 UI（"按 E 进入 / 点击进入"）
    /// 3. 处理交互触发（进入子场景或打开功能面板）
    ///
    /// 建筑入口映射：
    ///   教学大楼 (TeachingBuilding)  -> 打开关卡选择面板 / 切换到 BattleScene
    ///   竞技场   (Arena)           -> PVP 匹配（预留）
    ///   公会大厅 (GuildHall)       -> GuildScene（预留）
    ///   知识塔   (KnowledgeTower)  -> 知识塔无尽模式
    ///   布告栏   (BulletinBoard)   -> 每日挑战
    ///   商店     (Shop)            -> 打开商店面板
    ///   个人中心 (PersonalCenter)  -> 打开个人中心面板
    ///
    /// 交互方式：
    ///   PC端：靠近后按 E 键
    ///   移动端：靠近后点击交互按钮
    /// </summary>
    public class BuildingInteraction : MonoBehaviour
    {
        // === 配置 ===
        /// <summary>交互检测距离</summary>
        [SerializeField] private float _interactionRange = 3f;

        /// <summary>交互提示 Canvas</summary>
        [SerializeField] private Canvas _interactionPromptCanvas;

        /// <summary>交互按键提示文本</summary>
        [SerializeField] private UnityEngine.UI.Text _promptText;

        // === 组件引用 ===
        [SerializeField] private Transform _playerTransform;

        // === 状态 ===
        private BuildingEntry _currentNearbyBuilding;   // 当前可交互的建筑
        private bool _isPromptVisible;

        // === Unity 生命周期 ===
        private void Start() { }
        private void Update() { }
        private void OnDrawGizmosSelected() { }

        // === 公共方法 ===

        /// <summary>手动触发当前建筑的交互</summary>
        public void Interact() { }

        /// <summary>根据建筑配置决定交互行为</summary>
        /// <param name="building">建筑入口数据</param>
        private void PerformBuildingAction(BuildingEntry building) { }

        /// <summary>显示交互提示</summary>
        /// <param name="buildingName">建筑名称</param>
        /// <param name="actionText">交互行为描述</param>
        public void ShowPrompt(string buildingName, string actionText) { }

        /// <summary>隐藏交互提示</summary>
        public void HidePrompt() { }

        // === 私有方法 ===
        private void CheckNearbyBuildings() { }
        private BuildingEntry RaycastForBuilding() { return null; }
    }

    /// <summary>
    /// 建筑入口数据定义
    /// 在 Unity Editor 中通过 BuildingEntry 组件挂载到场景中的建筑GameObject上
    /// </summary>
    [Serializable]
    public class BuildingEntry : MonoBehaviour
    {
        /// <summary>建筑ID</summary>
        public string BuildingId;

        /// <summary>建筑名称（中文）</summary>
        public string BuildingName;

        /// <summary>建筑类型</summary>
        public BuildingType Type;

        /// <summary>目标场景名称（如果跳转到新场景）</summary>
        public string TargetSceneName;

        /// <summary>目标UI面板名称（如果打开UI面板）</summary>
        public string TargetPanelName;

        /// <summary>传送后玩家的目标位置</summary>
        public Vector3 TeleportTargetPosition;

        /// <summary>交互触发事件</summary>
        public UnityEvent OnInteract;

        public enum BuildingType
        {
            TeachingBuilding,   // 教学大楼 -> 关卡选择
            Arena,              // 竞技场 -> PVP匹配
            GuildHall,          // 公会大厅 -> 公会功能
            KnowledgeTower,     // 知识塔 -> 无尽模式
            BulletinBoard,      // 布告栏 -> 每日挑战
            Shop,               // 商店 -> 道具购买
            PersonalCenter      // 个人中心 -> 背包/成就/设置
        }
    }
}
