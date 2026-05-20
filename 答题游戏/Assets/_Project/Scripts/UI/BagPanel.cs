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
    /// 1. 展示玩家拥有的道具/物品（网格布局）
    /// 2. 物品分类Tab + 详情弹窗
    /// 3. 使用/出售物品操作
    /// </summary>
    public class BagPanel : MonoBehaviour
    {
        [Header("主面板")]
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private Button _closeButton;
        [SerializeField] private TextMeshProUGUI _capacityText;

        [Header("分类Tab")]
        [SerializeField] private Button _allTab;
        [SerializeField] private Button _consumableTab;
        [SerializeField] private Button _skinTab;
        [SerializeField] private Button _otherTab;

        [Header("物品网格")]
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private GridLayoutGroup _itemGrid;
        [SerializeField] private GameObject _bagItemPrefab;

        [Header("详情弹窗")]
        [SerializeField] private GameObject _detailPanel;
        [SerializeField] private Image _detailIcon;
        [SerializeField] private TextMeshProUGUI _detailName;
        [SerializeField] private TextMeshProUGUI _detailDescription;
        [SerializeField] private TextMeshProUGUI _detailQuantity;
        [SerializeField] private Button _detailUseButton;
        [SerializeField] private Button _detailSellButton;
        [SerializeField] private Button _detailCloseButton;

        // === 状态 ===
        private BagCategory _currentCategory = BagCategory.All;
        private List<BagItemData> _allItems = new List<BagItemData>();
        private BagItemData _selectedItem;

        [Serializable]
        public class BagItemData
        {
            public string itemId;
            public string name;
            public string description;
            public string iconId;
            public BagCategory category;
            public int quantity;
            public bool isUsable;
            public int sellPrice;
            public ItemRarity rarity;
        }

        public enum BagCategory { All, Consumable, Skin, Other }
        public enum ItemRarity { Common, Rare, Epic, Legendary }

        // === Unity 生命周期 ===

        private void Awake()
        {
            if (_closeButton != null) _closeButton.onClick.AddListener(Hide);
            if (_allTab != null) _allTab.onClick.AddListener(() => SwitchCategory(BagCategory.All));
            if (_consumableTab != null) _consumableTab.onClick.AddListener(() => SwitchCategory(BagCategory.Consumable));
            if (_skinTab != null) _skinTab.onClick.AddListener(() => SwitchCategory(BagCategory.Skin));
            if (_otherTab != null) _otherTab.onClick.AddListener(() => SwitchCategory(BagCategory.Other));

            if (_detailUseButton != null) _detailUseButton.onClick.AddListener(OnUseClicked);
            if (_detailSellButton != null) _detailSellButton.onClick.AddListener(OnSellClicked);
            if (_detailCloseButton != null) _detailCloseButton.onClick.AddListener(CloseDetail);

            if (_detailPanel != null) _detailPanel.SetActive(false);
        }

        private void Start()
        {
            if (_panelRoot != null) _panelRoot.SetActive(false);
        }

        // === 公共方法 ===

        public void Show()
        {
            if (_panelRoot != null) _panelRoot.SetActive(true);
            RefreshBag();
        }

        public void Hide()
        {
            if (_panelRoot != null) _panelRoot.SetActive(false);
            CloseDetail();
        }

        public void SwitchCategory(BagCategory category)
        {
            _currentCategory = category;
            PopulateGrid();
        }

        public void RefreshBag()
        {
            StartCoroutine(FetchBagItems());
        }

        public void ShowDetail(BagItemData item)
        {
            _selectedItem = item;
            if (_detailPanel == null) return;

            _detailPanel.SetActive(true);
            if (_detailName != null) _detailName.text = item.name;
            if (_detailDescription != null) _detailDescription.text = item.description;
            if (_detailQuantity != null) _detailQuantity.text = $"数量: {item.quantity}";
            if (_detailUseButton != null) _detailUseButton.gameObject.SetActive(item.isUsable);
            if (_detailSellButton != null) _detailSellButton.gameObject.SetActive(item.sellPrice > 0);
        }

        public void CloseDetail()
        {
            if (_detailPanel != null) _detailPanel.SetActive(false);
            _selectedItem = null;
        }

        // === 私有方法 ===

        private System.Collections.IEnumerator FetchBagItems()
        {
            // TODO: GET /api/v1/users/:id/items
            yield return new WaitForSeconds(0.2f);
            _allItems = CreateMockItems();
            UpdateCapacityText();
            PopulateGrid();
        }

        private void PopulateGrid()
        {
            if (_itemGrid == null || _bagItemPrefab == null) return;

            foreach (Transform child in _itemGrid.transform)
                Destroy(child.gameObject);

            var filtered = FilterByCategory(_allItems, _currentCategory);
            foreach (var item in filtered)
            {
                GameObject card = Instantiate(_bagItemPrefab, _itemGrid.transform);
                SetupCard(card, item);
            }
        }

        private void SetupCard(GameObject card, BagItemData item)
        {
            var texts = card.GetComponentsInChildren<TextMeshProUGUI>();
            foreach (var t in texts)
            {
                if (t.name.Contains("Name")) t.text = item.name;
                else if (t.name.Contains("Quantity")) t.text = $"x{item.quantity}";
            }

            var btn = card.GetComponentInChildren<Button>();
            if (btn != null) btn.onClick.AddListener(() => ShowDetail(item));
        }

        private void OnUseClicked()
        {
            if (_selectedItem == null) return;
            // TODO: POST /api/v1/users/items/use { itemId, quantity: 1 }
            ToastManager.Instance?.ShowSuccess($"使用了 {_selectedItem.name}");
            RefreshBag();
            CloseDetail();
        }

        private void OnSellClicked()
        {
            if (_selectedItem == null) return;
            PopupManager.Instance?.ShowConfirm("出售确认",
                $"确定出售 {_selectedItem.name}，获得 {_selectedItem.sellPrice} 金币？",
                () =>
                {
                    // TODO: POST /api/v1/users/items/sell
                    ToastManager.Instance?.ShowSuccess($"出售成功，获得 {_selectedItem.sellPrice} 金币");
                    RefreshBag();
                    CloseDetail();
                });
        }

        private void UpdateCapacityText()
        {
            if (_capacityText != null)
                _capacityText.text = $"{_allItems.Count} / 100";
        }

        private List<BagItemData> FilterByCategory(List<BagItemData> items, BagCategory category)
        {
            if (category == BagCategory.All) return items;
            var result = new List<BagItemData>();
            foreach (var item in items)
                if (item.category == category) result.Add(item);
            return result;
        }

        private List<BagItemData> CreateMockItems()
        {
            return new List<BagItemData>
            {
                new BagItemData { itemId = "skip_card", name = "跳过卡", category = BagCategory.Consumable, quantity = 3, isUsable = true, sellPrice = 50, rarity = ItemRarity.Common },
                new BagItemData { itemId = "double_card", name = "加倍卡", category = BagCategory.Consumable, quantity = 1, isUsable = true, sellPrice = 100, rarity = ItemRarity.Rare },
                new BagItemData { itemId = "freeze_card", name = "冻结卡", category = BagCategory.Consumable, quantity = 2, isUsable = true, sellPrice = 80, rarity = ItemRarity.Rare },
                new BagItemData { itemId = "energy_potion", name = "体力药水", category = BagCategory.Consumable, quantity = 5, isUsable = true, sellPrice = 30, rarity = ItemRarity.Common },
            };
        }
    }
}
