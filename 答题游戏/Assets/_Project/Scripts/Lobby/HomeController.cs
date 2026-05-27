using UnityEngine;
using TMPro;
using OriginXR.Core;
using OriginXR.Data;

namespace OriginXR.Home
{
    public class HomeController : MonoBehaviour
    {
        [Header("场景")]
        [SerializeField] private string _battleSceneName = "BattleScene";

        [Header("面板")]
        [SerializeField] private DifficultyPanel _difficultyPanel;
        [SerializeField] private DailyPanel _dailyPanel;
        [SerializeField] private ShopPanelUI _shopPanel;
        [SerializeField] private SimpleSettingsPanel _settingsPanel;

        [Header("顶部货币")]
        public TextMeshProUGUI goldText;
        public TextMeshProUGUI diamondText;

        private void Start()
        {
            UpdateCurrencyUI();
        }

        private void OnEnable()
        {
            UpdateCurrencyUI();
        }

        public void UpdateCurrencyUI()
        {
            if (goldText != null) goldText.text = $" {CurrencyManager.Gold}";
            if (diamondText != null) diamondText.text = $" {CurrencyManager.Diamond}";
        }

        public void OnPVEClicked()
        {
            Core.AudioManager.Instance?.PlayUISFX("button_click");
            if (_difficultyPanel != null)
                _difficultyPanel.Show();
            else
                SceneLoader.Instance?.LoadScene(_battleSceneName);
        }

       

        /// <summary>点击每日挑战按钮</summary>
        public void OnDailyClick()
        {
            Core.AudioManager.Instance?.PlayUISFX("button_click");
            if (_dailyPanel != null)
                _dailyPanel.Show();
            else
                SceneLoader.Instance?.LoadScene(_battleSceneName);
        }

        /// <summary>点击商店按钮</summary>
        public void OnShopClicked()
        {
            Core.AudioManager.Instance?.PlayUISFX("button_click");
            if (_shopPanel != null)
                _shopPanel.Show();
        }

        /// <summary>点击设置按钮</summary>
        public void OnSettingsClicked()
        {
            Core.AudioManager.Instance?.PlayUISFX("button_click");
            if (_settingsPanel != null)
                _settingsPanel.Show();
        }

        /// <summary>返回主页</summary>
        public void BackToHome()
        {
            SceneLoader.Instance?.LoadScene("HomeScene");
        }
    }
}
