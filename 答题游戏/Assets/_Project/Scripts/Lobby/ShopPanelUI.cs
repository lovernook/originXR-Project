using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace OriginXR.Home
{
    /// <summary>
    /// 商店面板控制器
    /// </summary>
    public class ShopPanelUI : MonoBehaviour
    {
        [Header("UI")]
        public TextMeshProUGUI goldText;
        public Button closeButton;

        [Header("道具按钮")]
        public Button[] itemButtons;
        public string[] itemNames = { "跳过卡", "加倍卡", "冻结卡", "体力药水" };
        public int[] itemPrices = { 100, 5, 10, 200 };
        public string[] itemCurrencies = { "gold", "diamond", "diamond", "gold" };

        private void Start()
        {
            if (closeButton != null) closeButton.onClick.AddListener(Hide);

            for (int i = 0; i < itemButtons.Length; i++)
            {
                int index = i;
                itemButtons[i].onClick.AddListener(() => BuyItem(index));
            }
        }

        public void Show()
        {
            gameObject.SetActive(true);
            UpdateGoldDisplay();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void BuyItem(int index)
        {
            string name = itemNames[index];
            int price = itemPrices[index];
            string currency = itemCurrencies[index];

            UI.PopupManager.Instance?.ShowConfirm("购买确认",
                $"花费 {price} {(currency == "gold" ? "金币" : "钻石")} 购买 {name}？",
                () =>
                {
                    UI.ToastManager.Instance?.ShowSuccess($"购买了 {name}！");
                    Debug.Log($"[Shop] 购买: {name} x1, 花费 {currency}{price}");
                });
        }

        private void UpdateGoldDisplay()
        {
            if (goldText != null)
                goldText.text = " 1280    ";
        }
    }
}
