using UnityEngine;
using UnityEngine.UI;
using OriginXR.Core;

namespace OriginXR.Home
{
    /// <summary>
    /// HomeScene 主页控制器
    /// 管理主页按钮点击事件：PVE/每日挑战/商店/设置
    /// </summary>
    public class HomeController : MonoBehaviour
    {
        [Header("场景名称")]
        [SerializeField] private string _homeSceneName = "HomeScene";
        [SerializeField] private string _battleSceneName = "BattleScene";

        [Header("UI 面板")]
        [SerializeField] private DifficultyPanel _difficultyPanel;
        [SerializeField] private DailyPanel _dailyPanel;
        [SerializeField] private ShopPanelUI _shopPanel;
        [SerializeField] private SimpleSettingsPanel _settingsPanel;

        private void Start()
        {
            Debug.Log("[HomeController] 主页已加载");
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
            SceneLoader.Instance?.LoadScene(_homeSceneName);
        }
    }
}
