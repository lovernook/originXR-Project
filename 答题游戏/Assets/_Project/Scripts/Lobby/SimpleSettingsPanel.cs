using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace OriginXR.Home
{
    /// <summary>
    /// 设置面板精简版
    /// 负责音量调整和退出登录
    /// </summary>
    public class SimpleSettingsPanel : MonoBehaviour
    {
        [Header("音量")]
        public Slider bgmSlider;
        public Slider sfxSlider;

        [Header("按钮")]
        public Button logoutButton;
        public Button closeButton;

        private void Start()
        {
            if (closeButton != null) closeButton.onClick.AddListener(Hide);
            if (logoutButton != null) logoutButton.onClick.AddListener(OnLogout);

            if (bgmSlider != null)
            {
                bgmSlider.value = PlayerPrefs.GetFloat("Audio_BGMVolume", 0.8f);
                bgmSlider.onValueChanged.AddListener(OnBGMChanged);
            }

            if (sfxSlider != null)
            {
                sfxSlider.value = PlayerPrefs.GetFloat("Audio_SFXVolume", 1f);
                sfxSlider.onValueChanged.AddListener(OnSFXChanged);
            }
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void OnBGMChanged(float value)
        {
            PlayerPrefs.SetFloat("Audio_BGMVolume", value);
            Core.AudioManager.Instance?.SetBGMVolume(value);
        }

        private void OnSFXChanged(float value)
        {
            PlayerPrefs.SetFloat("Audio_SFXVolume", value);
            Core.AudioManager.Instance?.SetSFXVolume(value);
        }

        private void OnLogout()
        {
            UI.PopupManager.Instance?.ShowConfirm("退出登录",
                "确定要退出当前账号吗？",
                () =>
                {
                    Core.SceneLoader.Instance?.LoadScene("LoginScene");
                });
        }
    }
}
