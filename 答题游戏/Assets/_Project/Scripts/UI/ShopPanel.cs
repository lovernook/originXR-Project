using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

namespace OriginXR.UI
{
    /// <summary>
    /// 商城面板
    /// 负责：
    /// 1. 展示可购买道具/皮肤/体力/礼包商品列表
    /// 2. 分类Tab切换 + 商品网格展示
    /// 3. 购买流程：点击 → 确认弹窗 → 扣除货币 → 发放道具
    /// </summary>
    public class ShopPanel : MonoBehaviour
    {
        [Header("主面板")]
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private Button _closeButton;

        [Header("货币")]
        [SerializeField] private TextMeshProUGUI _goldText;
        [SerializeField] private TextMeshProUGUI _diamondText;

        [Header("分类Tab")]
        [SerializeField] private Button _itemTab;
        [SerializeField] private Button _skinTab;
        [SerializeField] private Button _energyTab;
        [SerializeField] private Button _packTab;

        [Header("商品网格")]
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private GridLayoutGroup _itemGrid;
        [SerializeField] private GameObject _shopItemPrefab;

        // === 状态 ===
        private ShopCategory _currentCategory = ShopCategory.Item;
        private List<ShopItemData> _items = new List<ShopItemData>();

        [Serializable]
        public class ShopItemData
        {
            public string id;
            public string name;
            public string description;
            public string iconId;
            public ShopCategory category;
            public string currencyType;    // "gold" / "diamond"
            public int price;
            public int dailyLimit;
            public int remainingQuantity;
        }

        public enum ShopCategory { Item, Skin, Energy, Pack }

        // === Unity 生命周期 ===

        private void Awake()
        {
            if (_closeButton != null) _closeButton.onClick.AddListener(Hide);
            if (_itemTab != null) _itemTab.onClick.AddListener(() => SwitchCategory(ShopCategory.Item));
            if (_skinTab != null) _skinTab.onClick.AddListener(() => SwitchCategory(ShopCategory.Skin));
            if (_energyTab != null) _energyTab.onClick.AddListener(() => SwitchCategory(ShopCategory.Energy));
            if (_packTab != null) _packTab.onClick.AddListener(() => SwitchCategory(ShopCategory.Pack));
        }

        private void Start()
        {
            if (_panelRoot != null) _panelRoot.SetActive(false);
        }

        // === 公共方法 ===

        public void Show()
        {
            if (_panelRoot != null) _panelRoot.SetActive(true);
            RefreshShop();
        }

        public void Hide()
        {
            if (_panelRoot != null) _panelRoot.SetActive(false);
        }

        public void SwitchCategory(ShopCategory category)
        {
            _currentCategory = category;
            RefreshShop();
        }

        public void RefreshShop()
        {
            StartCoroutine(FetchShopItems());
        }

        public void BuyItem(ShopItemData item)
        {
            PopupManager.Instance?.ShowConfirm("购买确认",
                $"确定花费 {item.price} {item.currencyType} 购买 {item.name} 吗？",
                () => StartCoroutine(SendPurchaseRequest(item.id)),
                null, "购买", "取消");
        }

        // === 私有方法 ===

        private System.Collections.IEnumerator FetchShopItems()
        {
            // TODO: GET /api/v1/shop/items?category={category}
            yield return new WaitForSeconds(0.2f);
            _items = CreateMockItems(_currentCategory);
            PopulateGrid();
            UpdateCurrencyDisplay();
        }

        private void PopulateGrid()
        {
            if (_itemGrid == null || _shopItemPrefab == null) return;

            foreach (Transform child in _itemGrid.transform)
                Destroy(child.gameObject);

            foreach (var item in _items)
            {
                GameObject card = Instantiate(_shopItemPrefab, _itemGrid.transform);
                SetupCard(card, item);
            }
        }

        private void SetupCard(GameObject card, ShopItemData item)
        {
            var texts = card.GetComponentsInChildren<TextMeshProUGUI>();
            foreach (var t in texts)
            {
                if (t.name.Contains("Name")) t.text = item.name;
                else if (t.name.Contains("Price")) t.text = $"{item.currencyType}: {item.price}";
                else if (t.name.Contains("Desc")) t.text = item.description;
            }

            var btn = card.GetComponentInChildren<Button>();
            if (btn != null) btn.onClick.AddListener(() => BuyItem(item));
        }

        private System.Collections.IEnumerator SendPurchaseRequest(string itemId)
        {
            // TODO: POST /api/v1/shop/purchase { itemId, quantity: 1 }
            yield return new WaitForSeconds(0.5f);
            ToastManager.Instance?.ShowSuccess("购买成功！");
            RefreshShop();
        }

        private void UpdateCurrencyDisplay()
        {
            var userData = GetUserData();
            if (_goldText != null) _goldText.text = $"💰 {userData?.gold ?? 0}";
            if (_diamondText != null) _diamondText.text = $"💎 {userData?.diamond ?? 0}";
        }

        private Data.UserData GetUserData()
        {
            // TODO: 从 Data 层获取当前用户数据
            return null;
        }

        private List<ShopItemData> CreateMockItems(ShopCategory category)
        {
            var list = new List<ShopItemData>();
            if (category == ShopCategory.Item || category == ShopCategory.Pack)
            {
                list.Add(new ShopItemData { id = "skip_card", name = "跳过卡", currencyType = "gold", price = 100, description = "跳过当前题目" });
                list.Add(new ShopItemData { id = "double_card", name = "加倍卡", currencyType = "diamond", price = 5, description = "本题得分×2" });
                list.Add(new ShopItemData { id = "freeze_card", name = "冻结卡", currencyType = "diamond", price = 10, description = "冻结对手5秒" });
                list.Add(new ShopItemData { id = "energy_potion", name = "体力药水", currencyType = "gold", price = 200, description = "恢复30点体力" });
            }
            return list;
        }
    }
}
