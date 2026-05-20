using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

namespace OriginXR.UI
{
    /// <summary>
    /// 设置面板
    /// 负责：
    /// 1. 音量设置（BGM/SFX/UI 滑块）
    /// 2. 画质设置（分辨率/帧率/画质档位/阴影/后处理）
    /// 3. 控制设置（摇杆/相机灵敏度）
    /// 4. 语言切换
    /// 5. 账号管理（修改密码/注销）
    /// 6. 所有设置持久化到 PlayerPrefs
    /// </summary>
    public class SettingsPanel : MonoBehaviour
    {
        [Header("主面板")]
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private Button _closeButton;

        [Header("音量")]
        [SerializeField] private Slider _bgmVolumeSlider;
        [SerializeField] private Slider _sfxVolumeSlider;
        [SerializeField] private Slider _uiVolumeSlider;
        [SerializeField] private TextMeshProUGUI _bgmVolumeText;
        [SerializeField] private TextMeshProUGUI _sfxVolumeText;
        [SerializeField] private TextMeshProUGUI _uiVolumeText;

        [Header("画质")]
        [SerializeField] private TMP_Dropdown _qualityDropdown;
        [SerializeField] private TMP_Dropdown _framerateDropdown;
        [SerializeField] private Toggle _vSyncToggle;
        [SerializeField] private Toggle _postProcessingToggle;
        [SerializeField] private Toggle _shadowToggle;

        [Header("控制")]
        [SerializeField] private Slider _moveSensitivitySlider;
        [SerializeField] private Slider _cameraSensitivitySlider;
        [SerializeField] private Toggle _invertYToggle;

        [Header("语言")]
        [SerializeField] private TMP_Dropdown _languageDropdown;

        [Header("账号")]
        [SerializeField] private Button _logoutButton;
        [SerializeField] private Button _changePasswordButton;

        [Header("关于")]
        [SerializeField] private TextMeshProUGUI _versionText;
        [SerializeField] private Button _userAgreementButton;
        [SerializeField] private Button _privacyPolicyButton;

        // === PlayerPrefs 键 ===
        private const string PP_BGM_VOL = "Audio_BGMVolume";
        private const string PP_SFX_VOL = "Audio_SFXVolume";
        private const string PP_UI_VOL = "Audio_UIVolume";
        private const string PP_QUALITY = "Graphics_QualityLevel";
        private const string PP_FRAMERATE = "Graphics_Framerate";
        private const string PP_VSYNC = "Graphics_VSync";
        private const string PP_POSTFX = "Graphics_PostProcessing";
        private const string PP_SHADOWS = "Graphics_Shadows";
        private const string PP_MOVE_SENS = "Control_MoveSensitivity";
        private const string PP_CAM_SENS = "Control_CameraSensitivity";
        private const string PP_INVERT_Y = "Control_InvertY";
        private const string PP_LANGUAGE = "System_Language";

        // === Unity 生命周期 ===

        private void Awake()
        {
            if (_closeButton != null) _closeButton.onClick.AddListener(Hide);
            if (_logoutButton != null) _logoutButton.onClick.AddListener(OnLogout);
            if (_changePasswordButton != null) _changePasswordButton.onClick.AddListener(OnChangePassword);
            if (_userAgreementButton != null) _userAgreementButton.onClick.AddListener(() => OpenUrl("https://example.com/agreement"));
            if (_privacyPolicyButton != null) _privacyPolicyButton.onClick.AddListener(() => OpenUrl("https://example.com/privacy"));

            // 绑定 UI 事件
            if (_bgmVolumeSlider != null) _bgmVolumeSlider.onValueChanged.AddListener(OnBGMVolumeChanged);
            if (_sfxVolumeSlider != null) _sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
            if (_uiVolumeSlider != null) _uiVolumeSlider.onValueChanged.AddListener(OnUIVolumeChanged);
            if (_qualityDropdown != null) _qualityDropdown.onValueChanged.AddListener(OnQualityChanged);
            if (_framerateDropdown != null) _framerateDropdown.onValueChanged.AddListener(OnFramerateChanged);
            if (_vSyncToggle != null) _vSyncToggle.onValueChanged.AddListener(OnVSyncToggled);
            if (_postProcessingToggle != null) _postProcessingToggle.onValueChanged.AddListener(OnPostProcessingToggled);
            if (_shadowToggle != null) _shadowToggle.onValueChanged.AddListener(OnShadowToggled);
            if (_moveSensitivitySlider != null) _moveSensitivitySlider.onValueChanged.AddListener(OnMoveSensitivityChanged);
            if (_cameraSensitivitySlider != null) _cameraSensitivitySlider.onValueChanged.AddListener(OnCameraSensitivityChanged);
            if (_invertYToggle != null) _invertYToggle.onValueChanged.AddListener(OnInvertYToggled);
            if (_languageDropdown != null) _languageDropdown.onValueChanged.AddListener(OnLanguageChanged);
        }

        private void Start()
        {
            if (_panelRoot != null) _panelRoot.SetActive(false);

            // 设置版本号
            if (_versionText != null)
                _versionText.text = $"版本: {Application.version}";

            // 初始化下拉列表
            PopulateDropdowns();
        }

        // === 公共方法 ===

        public void Show()
        {
            if (_panelRoot != null) _panelRoot.SetActive(true);
            LoadSettings();
        }

        public void Hide()
        {
            if (_panelRoot != null) _panelRoot.SetActive(false);
            SaveSettings();
        }

        /// <summary>从 PlayerPrefs 加载当前设置</summary>
        public void LoadSettings()
        {
            _bgmVolumeSlider?.SetValueWithoutNotify(PlayerPrefs.GetFloat(PP_BGM_VOL, 0.8f));
            _sfxVolumeSlider?.SetValueWithoutNotify(PlayerPrefs.GetFloat(PP_SFX_VOL, 1f));
            _uiVolumeSlider?.SetValueWithoutNotify(PlayerPrefs.GetFloat(PP_UI_VOL, 1f));
            _qualityDropdown?.SetValueWithoutNotify(PlayerPrefs.GetInt(PP_QUALITY, 2));
            _framerateDropdown?.SetValueWithoutNotify(PlayerPrefs.GetInt(PP_FRAMERATE, 1));
            _vSyncToggle?.SetIsOnWithoutNotify(PlayerPrefs.GetInt(PP_VSYNC, 1) == 1);
            _postProcessingToggle?.SetIsOnWithoutNotify(PlayerPrefs.GetInt(PP_POSTFX, 1) == 1);
            _shadowToggle?.SetIsOnWithoutNotify(PlayerPrefs.GetInt(PP_SHADOWS, 1) == 1);
            _moveSensitivitySlider?.SetValueWithoutNotify(PlayerPrefs.GetFloat(PP_MOVE_SENS, 1f));
            _cameraSensitivitySlider?.SetValueWithoutNotify(PlayerPrefs.GetFloat(PP_CAM_SENS, 1f));
            _invertYToggle?.SetIsOnWithoutNotify(PlayerPrefs.GetInt(PP_INVERT_Y, 0) == 1);
            _languageDropdown?.SetValueWithoutNotify(PlayerPrefs.GetInt(PP_LANGUAGE, 0));

            UpdateVolumeTexts();
        }

        /// <summary>保存设置到 PlayerPrefs</summary>
        public void SaveSettings()
        {
            PlayerPrefs.SetFloat(PP_BGM_VOL, _bgmVolumeSlider?.value ?? 0.8f);
            PlayerPrefs.SetFloat(PP_SFX_VOL, _sfxVolumeSlider?.value ?? 1f);
            PlayerPrefs.SetFloat(PP_UI_VOL, _uiVolumeSlider?.value ?? 1f);

            PlayerPrefs.SetInt(PP_QUALITY, _qualityDropdown?.value ?? 2);
            PlayerPrefs.SetInt(PP_FRAMERATE, _framerateDropdown?.value ?? 1);
            PlayerPrefs.SetInt(PP_VSYNC, (_vSyncToggle?.isOn ?? true) ? 1 : 0);
            PlayerPrefs.SetInt(PP_POSTFX, (_postProcessingToggle?.isOn ?? true) ? 1 : 0);
            PlayerPrefs.SetInt(PP_SHADOWS, (_shadowToggle?.isOn ?? true) ? 1 : 0);

            PlayerPrefs.SetFloat(PP_MOVE_SENS, _moveSensitivitySlider?.value ?? 1f);
            PlayerPrefs.SetFloat(PP_CAM_SENS, _cameraSensitivitySlider?.value ?? 1f);
            PlayerPrefs.SetInt(PP_INVERT_Y, (_invertYToggle?.isOn ?? false) ? 1 : 0);
            PlayerPrefs.SetInt(PP_LANGUAGE, _languageDropdown?.value ?? 0);

            PlayerPrefs.Save();
            ApplySettings();
        }

        /// <summary>应用设置到运行中的系统</summary>
        public void ApplySettings()
        {
            // 音量
            var audioMgr = Core.AudioManager.Instance;
            if (audioMgr != null)
            {
                audioMgr.SetBGMVolume(_bgmVolumeSlider?.value ?? 0.8f);
                audioMgr.SetSFXVolume(_sfxVolumeSlider?.value ?? 1f);
                audioMgr.SetUIVolume(_uiVolumeSlider?.value ?? 1f);
            }

            // 画质
            QualitySettings.SetQualityLevel(_qualityDropdown?.value ?? 2);
            Application.targetFrameRate = (_framerateDropdown?.value ?? 1) switch
            {
                0 => 30,
                1 => 60,
                2 => 120,
                _ => 60
            };
            QualitySettings.vSyncCount = (_vSyncToggle?.isOn ?? true) ? 1 : 0;
        }

        // === UI 事件处理 ===

        private void OnBGMVolumeChanged(float value) { UpdateVolumeTexts(); }
        private void OnSFXVolumeChanged(float value) { UpdateVolumeTexts(); }
        private void OnUIVolumeChanged(float value) { UpdateVolumeTexts(); }
        private void OnQualityChanged(int index) { }
        private void OnFramerateChanged(int index) { }
        private void OnVSyncToggled(bool on) { }
        private void OnPostProcessingToggled(bool on) { }
        private void OnShadowToggled(bool on) { }
        private void OnMoveSensitivityChanged(float value) { }
        private void OnCameraSensitivityChanged(float value) { }
        private void OnInvertYToggled(bool on) { }
        private void OnLanguageChanged(int index) { }

        private void UpdateVolumeTexts()
        {
            if (_bgmVolumeText != null) _bgmVolumeText.text = $"{(int)((_bgmVolumeSlider?.value ?? 0) * 100)}%";
            if (_sfxVolumeText != null) _sfxVolumeText.text = $"{(int)((_sfxVolumeSlider?.value ?? 0) * 100)}%";
            if (_uiVolumeText != null) _uiVolumeText.text = $"{(int)((_uiVolumeSlider?.value ?? 0) * 100)}%";
        }

        private void OnLogout()
        {
            PopupManager.Instance?.ShowConfirm("注销登录", "确定要退出当前账号吗？",
                () =>
                {
                    PlayerPrefs.DeleteAll();
                    Core.SceneLoader.Instance?.LoadScene("LoginScene");
                });
        }

        private void OnChangePassword()
        {
            // TODO: 打开修改密码 UI
            ToastManager.Instance?.ShowInfo("修改密码功能开发中...");
        }

        private void OpenUrl(string url)
        {
            Application.OpenURL(url);
        }

        private void PopulateDropdowns()
        {
            // 画质选项
            if (_qualityDropdown != null)
            {
                _qualityDropdown.ClearOptions();
                _qualityDropdown.AddOptions(new List<string> { "低", "中", "高" });
            }

            // 帧率选项
            if (_framerateDropdown != null)
            {
                _framerateDropdown.ClearOptions();
                _framerateDropdown.AddOptions(new List<string> { "30 FPS", "60 FPS", "120 FPS" });
            }

            // 语言选项
            if (_languageDropdown != null)
            {
                _languageDropdown.ClearOptions();
                _languageDropdown.AddOptions(new List<string> { "中文", "English" });
            }
        }
    }
}
