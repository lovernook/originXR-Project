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
    /// 1. 展示可购买的道具/皮肤/体力等商品列表
    /// 2. 商品分类Tab（道具 / 皮肤 / 体力 / 礼包）
    /// 3. 商品卡片展示（图标 / 名称 / 价格 / 限购信息）
    /// 4. 购买流程：点击购买 -> 确认弹窗 -> 扣除货币 -> 发放道具
    /// 5. 货币余额显示（金币 / 钻石）
    ///
    /// 注意：
    ///   Web 端可浏览商城，实际购买需在 Unity 客户端完成
    ///
    /// API 接口：
    ///   GET  /api/v1/shop/items          -> 获取商品列表
    ///   POST /api/v1/shop/purchase       -> 购买商品
    /// </summary>
    public class ShopPanel : MonoBehaviour
    {
        // === UI 组件 ===
        [SerializeField] private Canvas _panelCanvas;
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private Button _closeButton;

        // === 货币显示 ===
        [SerializeField] private TextMeshProUGUI _goldText;        // 金币余额
        [SerializeField] private TextMeshProUGUI _diamondText;     // 钻石余额

        // === 分类Tab ===
        [SerializeField] private Button _itemTab;                  // 道具
        [SerializeField] private Button _skinTab;                  // 皮肤
        [SerializeField] private Button _energyTab;                // 体力
        [SerializeField] private Button _packTab;                  // 礼包

        // === 商品列表 ===
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private GridLayoutGroup _itemGrid;        // 商品网格布局
        [SerializeField] private GameObject _shopItemPrefab;       // 商品卡片预制体
        [SerializeField] private RectTransform _itemContentRoot;

        // === 商品详情弹窗 ===
        [SerializeField] private GameObject _itemDetailPanel;
        [SerializeField] private Image _detailIcon;
        [SerializeField] private TextMeshProUGUI _detailName;
        [SerializeField] private TextMeshProUGUI _detailDescription;
        [SerializeField] private TextMeshProUGUI _detailPrice;
        [SerializeField] private Button _detailBuyButton;
        [SerializeField] private Button _detailCloseButton;

        // === 状态 ===
        private ShopCategory _currentCategory = ShopCategory.Item;
        private List<ShopItemData> _currentItemList;
        private ShopItemData _selectedItem;

        [Serializable]
        public class ShopItemData
        {
            public string Id;
            public string Name;
            public string Description;
            public string IconId;
            public ShopCategory Category;
            public string CurrencyType;         // "gold" / "diamond"
            public int Price;
            public int DailyLimit;
            public int RemainingQuantity;
            public ShopItemEffect Effect;       // 购买后的效果
        }

        [Serializable]
        public class ShopItemEffect
        {
            public string EffectType;           // "add_energy" / "add_gold" / "unlock_skin" / "add_item"
            public string TargetId;             // 目标道具/皮肤ID
            public int Amount;                  // 数量
        }

        public enum ShopCategory { Item, Skin, Energy, Pack }

        // === Unity 生命周期 ===
        private void OnEnable() { }
        private void OnDisable() { }

        // === 公共方法 ===

        public void Show() { }
        public void Hide() { }

        /// <summary>切换商品分类</summary>
        public void SwitchCategory(ShopCategory category) { }

        /// <summary>刷新商品列表和货币余额</summary>
        public void RefreshShop() { }

        /// <summary>显示商品详情</summary>
        public void ShowItemDetail(ShopItemData item) { }

        /// <summary>购买选中商品</summary>
        public void BuySelectedItem() { }

        // === 私有方法 ===
        private IEnumerator<Coroutine> FetchShopItems() { yield return null; }
        private void PopulateItemGrid(List<ShopItemData> items) { }
        private void SetupShopItemCard(GameObject card, ShopItemData data) { }
        private IEnumerator<Coroutine> SendPurchaseRequest(string itemId) { yield return null; }
        private void UpdateCurrencyDisplay() { }

        // === 事件 ===
        public event Action OnPanelClosed;
        public event Action<ShopItemData> OnItemPurchased;

        // === 按钮回调 ===
        private void OnItemTabClicked() { }
        private void OnSkinTabClicked() { }
        private void OnEnergyTabClicked() { }
        private void OnPackTabClicked() { }
        private void OnCloseClicked() { }
        private void OnDetailBuyClicked() { }
        private void OnDetailCloseClicked() { }
    }
}
