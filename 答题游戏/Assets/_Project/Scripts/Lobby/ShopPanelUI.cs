using UnityEngine;
using UnityEngine.UI;
using TMPro;
using OriginXR.Data;

namespace OriginXR.Home
{
    public class ShopPanelUI : MonoBehaviour
    {
        public TextMeshProUGUI goldText;
        public TextMeshProUGUI diamondText;
        public Button closeButton;

        private void Start()
        {
            if (closeButton != null) closeButton.onClick.AddListener(Hide);
        }

        private void OnEnable() => UpdateDisplay();
        public void Show() { gameObject.SetActive(true); UpdateDisplay(); }
        public void Hide() { gameObject.SetActive(false); }

        private void UpdateDisplay()
        {
            if (goldText != null) goldText.text = $" {CurrencyManager.Gold}";
            if (diamondText != null) diamondText.text = $" {CurrencyManager.Diamond}";
        }

        public void BuySkipCard()     => DoBuy("跳过卡", 100, false);
        public void BuyDoubleCard()   => DoBuy("加倍卡", 5, true);
        public void BuyFreezeCard()   => DoBuy("冻结卡", 10, true);
        public void BuyEnergyPotion() => DoBuy("体力药水", 200, false);

        private void DoBuy(string itemName, int price, bool useDiamond)
        {
            bool ok = useDiamond ? CurrencyManager.SpendDiamond(price) : CurrencyManager.SpendGold(price);
            if (ok)
            {
                UpdateDisplay();
                UI.ToastManager.Instance?.ShowSuccess($"购买了 {itemName}！");
            }
            else
            {
                UI.ToastManager.Instance?.ShowWarning($"{(useDiamond ? "钻石" : "金币")}不足！");
            }
        }
    }
}
