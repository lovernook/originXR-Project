using UnityEngine;
using UnityEngine.Audio;
using System;
using System.Collections;

namespace OriginXR.Core
{
    /// <summary>
    /// 音频管理器
    /// 负责：
    /// 1. 背景音乐（BGM）的播放/停止/淡入淡出/切换
    /// 2. 游戏音效（SFX）的 2D/3D 空间播放
    /// 3. UI 交互音效播放
    /// 4. 基于 AudioMixer 的音量分组控制（Master/BGM/SFX/UI）
    /// 5. 音量设置的持久化存储（PlayerPrefs）
    ///
    /// AudioMixer 分组：
    ///   Master -> BGM Group / SFX Group / UI Group
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        // === 单例 ===
        public static AudioManager Instance { get; private set; }

        // === 属性 ===
        /// <summary>AudioMixer 资源引用</summary>
        [SerializeField] private AudioMixer _audioMixer;

        /// <summary>当前 BGM 音量（0~1）</summary>
        public float BGMVolume { get; private set; } = 0.8f;

        /// <summary>当前 SFX 音量（0~1）</summary>
        public float SFXVolume { get; private set; } = 1f;

        /// <summary>当前 UI 音效音量（0~1）</summary>
        public float UIVolume { get; private set; } = 1f;

        /// <summary>是否全局静音</summary>
        public bool IsMuted { get; private set; }

        // === 配置 ===
        [SerializeField] private AudioSource _bgmSource;
        [SerializeField] private AudioSource _sfxSource;
        [SerializeField] private AudioSource _uiSource;

        // === Unity 生命周期 ===
        private void Awake() { }
        private void Start() { }

        // === 公共方法 ===

        /// <summary>播放背景音乐（支持淡入）</summary>
        /// <param name="clipName">音频资源名称（Resources/Audio/BGM/ 下）</param>
        /// <param name="fadeInTime">淡入时长（秒），0为立即播放</param>
        public void PlayBGM(string clipName, float fadeInTime = 1f) { }

        /// <summary>停止背景音乐（支持淡出）</summary>
        /// <param name="fadeOutTime">淡出时长（秒）</param>
        public void StopBGM(float fadeOutTime = 1f) { }

        /// <summary>在指定世界坐标播放 3D 音效</summary>
        /// <param name="clipName">音频资源名称（Resources/Audio/SFX/ 下）</param>
        /// <param name="position">世界坐标位置</param>
        public void PlaySFX(string clipName, Vector3 position) { }

        /// <summary>播放 2D UI 音效</summary>
        /// <param name="clipName">音频资源名称（Resources/Audio/UI/ 下）</param>
        public void PlayUISFX(string clipName) { }

        /// <summary>设置 BGM 音量并应用至 AudioMixer</summary>
        public void SetBGMVolume(float volume)
        {
            BGMVolume = Mathf.Clamp01(volume);
        }

        /// <summary>设置 SFX 音量</summary>
        public void SetSFXVolume(float volume)
        {
            SFXVolume = Mathf.Clamp01(volume);
        }

        /// <summary>设置 UI 音效音量</summary>
        public void SetUIVolume(float volume)
        {
            UIVolume = Mathf.Clamp01(volume);
        }

        /// <summary>全局静音/取消静音</summary>
        public void MuteAll(bool mute) { IsMuted = mute; }

        /// <summary>加载音频资源</summary>
        /// <returns>AudioClip 实例，失败返回 null</returns>
        public AudioClip LoadAudioClip(string clipName) { return null; }

        /// <summary>保存音量设置到 PlayerPrefs</summary>
        public void SaveVolumeSettings() { }

        /// <summary>从 PlayerPrefs 加载音量设置</summary>
        public void LoadVolumeSettings() { }

        // === 私有方法 ===
        private IEnumerator FadeBGM(float targetVolume, float duration) { yield return null; }
        private IEnumerator FadeOutBGM(float duration) { yield return null; }
        private void ApplyVolumeToMixer(string parameter, float volume) { }

        // === 事件 ===
        /// <summary>音量设置变更事件</summary>
        public event Action<float, float, float> OnVolumeChanged;
    }
}
