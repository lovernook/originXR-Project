using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

namespace OriginXR.UI
{
    /// <summary>
    /// 背包面板
    /// 负责：
    /// 1. 展示玩家拥有的所有道具/物品（网格布局）
    /// 2. 物品分类Tab（全部 / 消耗品 / 材料 / 碎片 / 皮肤 / 其他）
    /// 3. 物品卡片展示（图标 / 名称 / 数量）
    /// 4. 物品详情弹窗（描述 / 使用按钮 / 出售按钮）
    /// 5. 支持拖拽排序（预留）
    ///
    /// 道具类型示例：
    ///   跳过卡、加倍卡、冻结卡、体力药水、经验药水、皮肤碎片等
    ///
    /// API 接口：
    ///   GET /api/v1/users/:id/items -> 获取背包列表
    /// </summary>
    public class BagPanel : MonoBehaviour
    {
        // === UI 组件 ===
        [SerializeField] private Canvas _panelCanvas;
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private Button _closeButton;

        // === 顶部信息 ===
        [SerializeField] private TextMeshProUGUI _bagCapacityText;  // "32 / 100"

        // === 分类Tab ===
        [SerializeField] private Button _allTab;
        [SerializeField] private Button _consumableTab;
        [SerializeField] private Button _materialTab;
        [SerializeField] private Button _fragmentTab;
        [SerializeField] private Button _skinTab;
        [SerializeField] private Button _otherTab;

        // === 物品网格 ===
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private GridLayoutGroup _itemGrid;
        [SerializeField] private GameObject _bagItemPrefab;         // 物品卡片预制体
        [SerializeField] private RectTransform _itemContentRoot;

        // === 物品详情弹窗 ===
        [SerializeField] private GameObject _itemDetailPanel;
        [SerializeField] private Image _detailIcon;
        [SerializeField] private TextMeshProUGUI _detailName;
        [SerializeField] private TextMeshProUGUI _detailDescription;
        [SerializeField] private TextMeshProUGUI _detailQuantity;
        [SerializeField] private Button _detailUseButton;
        [SerializeField] private Button _detailSellButton;
        [SerializeField] private Button _detailCloseButton;

        // === 状态 ===
        private BagCategory _currentCategory = BagCategory.All;
        private List<BagItemData> _allItems;
        private List<BagItemData> _filteredItems;
        private BagItemData _selectedItem;
        private int _maxCapacity;

        [Serializable]
        public class BagItemData
        {
            public string ItemId;
            public string Name;
            public string Description;
            public string IconId;
            public BagCategory Category;
            public int Quantity;
            public bool IsUsable;
            public int SellPrice;       // 出售金币价格
            public ItemRarity Rarity;
        }

        public enum BagCategory { All, Consumable, Material, Fragment, Skin, Other }
        public enum ItemRarity { Common, Rare, Epic, Legendary }

        // === Unity 生命周期 ===
        private void OnEnable() { }
        private void OnDisable() { }

        // === 公共方法 ===

        public void Show() { }
        public void Hide() { }

        /// <summary>切换物品分类</summary>
        public void SwitchCategory(BagCategory category) { }

        /// <summary>刷新背包数据</summary>
        public void RefreshBag() { }

        /// <summary>显示物品详情</summary>
        public void ShowItemDetail(BagItemData item) { }

        /// <summary>使用物品</summary>
        public void UseItem(string itemId) { }

        /// <summary>出售物品</summary>
        public void SellItem(string itemId, int quantity = 1) { }

        // === 私有方法 ===
        private IEnumerator<Coroutine> FetchBagItems() { yield return null; }
        private void PopulateItemGrid(List<BagItemData> items) { }
        private void SetupBagItemCard(GameObject card, BagItemData data) { }
        private List<BagItemData> FilterItemsByCategory(BagCategory category) { return null; }
        private void UpdateBagCapacityDisplay() { }
        private Color GetRarityColor(ItemRarity rarity) { return Color.white; }

        // === 事件 ===
        public event Action OnPanelClosed;
        public event Action<BagItemData> OnItemUsed;

        // === 按钮回调 ===
        private void OnCategoryTabClicked(int tabIndex) { }
        private void OnCloseClicked() { }
        private void OnDetailUseClicked() { }
        private void OnDetailSellClicked() { }
        private void OnDetailCloseClicked() { }
    }
}
