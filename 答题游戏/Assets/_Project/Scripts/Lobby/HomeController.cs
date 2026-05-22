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
        [SerializeField] private StageSelectPanel _stageSelectPanel;
        [SerializeField] private string _shopPanelName = "ShopPanel";
        [SerializeField] private string _settingsPanelName = "SettingsPanel";

        private void Start()
        {
            Debug.Log("[HomeController] 主页已加载");
        }

        public void OnPVEClicked()
        {
            Core.AudioManager.Instance?.PlayUISFX("button_click");
            if (_stageSelectPanel != null)
                _stageSelectPanel.Show();
            else
                SceneLoader.Instance?.LoadScene(_battleSceneName);
        }

       

        /// <summary>点击每日挑战按钮</summary>
        public void OnDailyClick()
        {
            Core.AudioManager.Instance?.PlayUISFX("button_click");
            // TODO: 加载每日挑战场景或显示每日挑战面板
            SceneLoader.Instance?.LoadScene(_battleSceneName);
        }

        /// <summary>点击商店按钮</summary>
        public void OnShopClicked()
        {
            Core.AudioManager.Instance?.PlayUISFX("button_click");
            UI.UIManager.Instance?.ShowPanel(_shopPanelName);
        }

        /// <summary>点击设置按钮</summary>
        public void OnSettingsClicked()
        {
            Core.AudioManager.Instance?.PlayUISFX("button_click");
            UI.UIManager.Instance?.ShowPanel(_settingsPanelName);
        }

        /// <summary>返回主页</summary>
        public void BackToHome()
        {
            SceneLoader.Instance?.LoadScene(_homeSceneName);
        }
    }
}
