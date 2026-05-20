using UnityEngine;
using UnityEngine.Audio;
using System;
using System.Collections;
using System.Collections.Generic;

namespace OriginXR.Core
{
    /// <summary>
    /// 音频管理器（单例）
    /// 负责：
    /// 1. BGM 背景音乐播放（单轨，支持淡入淡出切换）
    /// 2. SFX 游戏音效播放（2D 全局 / 3D 空间定位）
    /// 3. UI 交互音效播放（独立音轨）
    /// 4. 基于 AudioMixer 的分组音量控制
    /// 5. 音量设置 PlayerPrefs 持久化
    ///
    /// AudioMixer 分组参数名（需在 Mixer 中暴露）：
    ///   MasterVolume, BGMVolume, SFXVolume, UIVolume
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        [Header("AudioMixer")]
        [SerializeField] private AudioMixer _audioMixer;

        [Header("音频源")]
        [SerializeField] private AudioSource _bgmSource;       // BGM 专用（2D，Loop）
        [SerializeField] private AudioSource _sfxSourceA;      // SFX 池 A（2D，用于UI音效）
        [SerializeField] private AudioSource _sfxSourceB;      // SFX 池 B（2D，避免音效重叠）
        [SerializeField] private int _sfx3DPoolSize = 10;      // 3D 音效对象池大小

        [Header("默认音量（0~1）")]
        [SerializeField] private float _defaultBGMVolume = 0.8f;
        [SerializeField] private float _defaultSFXVolume = 1f;
        [SerializeField] private float _defaultUIVolume = 1f;

        // === 单例 ===
        public static AudioManager Instance { get; private set; }

        // === 属性 ===
        public float BGMVolume { get; private set; } = 0.8f;
        public float SFXVolume { get; private set; } = 1f;
        public float UIVolume { get; private set; } = 1f;
        public bool IsMuted { get; private set; }

        // === 内部状态 ===
        private Dictionary<string, AudioClip> _audioClipCache = new Dictionary<string, AudioClip>();
        private Queue<AudioSource> _sfx3DPool = new Queue<AudioSource>();
        private Transform _sfx3DPoolRoot;
        private Coroutine _bgmFadeCoroutine;

        // === PlayerPrefs 键 ===
        private const string PP_BGM_VOLUME = "Audio_BGMVolume";
        private const string PP_SFX_VOLUME = "Audio_SFXVolume";
        private const string PP_UI_VOLUME = "Audio_UIVolume";
        private const string PP_MUTED = "Audio_Muted";

        // === 事件 ===
        public event Action<float, float, float> OnVolumeChanged;

        // === Unity 生命周期 ===

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // 创建 3D SFX 对象池根节点
            _sfx3DPoolRoot = new GameObject("SFX_3D_Pool").transform;
            _sfx3DPoolRoot.SetParent(transform);
            _sfx3DPoolRoot.localPosition = Vector3.zero;
        }

        private void Start()
        {
            Initialize();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // === 公共方法 ===

        /// <summary>
        /// 初始化：加载音量设置 + 初始化 3D SFX 对象池
        /// </summary>
        public void Initialize()
        {
            LoadVolumeSettings();
            Initialize3DPool();
            Debug.Log("[AudioManager] 音频管理器已初始化");
        }

        // --- BGM 控制 ---

        /// <summary>
        /// 播放或切换背景音乐（支持淡入）
        /// </summary>
        /// <param name="clipName">音频资源名称（Resources/Audio/BGM/ 目录下）</param>
        /// <param name="fadeInTime">淡入时长（秒），0 为立即播放</param>
        public void PlayBGM(string clipName, float fadeInTime = 1f)
        {
            AudioClip clip = LoadAudioClip(clipName);
            if (clip == null)
            {
                Debug.LogWarning($"[AudioManager] BGM 资源未找到: {clipName}");
                return;
            }

            // 如果正在播放相同 BGM，忽略
            if (_bgmSource.clip == clip && _bgmSource.isPlaying)
                return;

            // 停止之前的淡入淡出协程
            if (_bgmFadeCoroutine != null)
                StopCoroutine(_bgmFadeCoroutine);

            if (fadeInTime > 0f && _bgmSource.isPlaying)
            {
                // 淡出旧 BGM 后再淡入新 BGM
                _bgmFadeCoroutine = StartCoroutine(CrossfadeBGM(clip, fadeInTime));
            }
            else
            {
                _bgmSource.clip = clip;
                _bgmSource.loop = true;
                _bgmSource.Play();

                if (fadeInTime > 0f)
                    _bgmFadeCoroutine = StartCoroutine(FadeInBGM(fadeInTime));
            }
        }

        /// <summary>
        /// 停止背景音乐（支持淡出）
        /// </summary>
        /// <param name="fadeOutTime">淡出时长（秒）</param>
        public void StopBGM(float fadeOutTime = 1f)
        {
            if (_bgmFadeCoroutine != null)
                StopCoroutine(_bgmFadeCoroutine);

            if (fadeOutTime > 0f)
            {
                _bgmFadeCoroutine = StartCoroutine(FadeOutBGM(fadeOutTime));
            }
            else
            {
                _bgmSource.Stop();
                _bgmSource.clip = null;
            }
        }

        // --- SFX 控制 ---

        /// <summary>
        /// 播放 2D 全局音效（如战斗音效、环境音效）
        /// </summary>
        public void PlaySFX(string clipName)
        {
            AudioClip clip = LoadAudioClip(clipName);
            if (clip == null)
            {
                Debug.LogWarning($"[AudioManager] SFX 资源未找到: {clipName}");
                return;
            }

            // 使用音频源池避免音效重叠
            if (!_sfxSourceA.isPlaying)
                _sfxSourceA.PlayOneShot(clip);
            else if (!_sfxSourceB.isPlaying)
                _sfxSourceB.PlayOneShot(clip);
            else
                _sfxSourceA.PlayOneShot(clip); // 池已满则覆盖
        }

        /// <summary>
        /// 在指定世界坐标播放 3D 空间音效（随距离衰减）
        /// </summary>
        public void PlaySFX3D(string clipName, Vector3 position)
        {
            AudioClip clip = LoadAudioClip(clipName);
            if (clip == null) return;

            AudioSource source = Get3DSourceFromPool();
            if (source == null) return;

            source.transform.position = position;
            source.clip = clip;
            source.spatialBlend = 1f; // 完全 3D
            source.Play();

            // 播放完毕后归还对象池
            StartCoroutine(Return3DSourceAfterPlay(source, clip.length));
        }

        // --- UI 音效 ---

        /// <summary>
        /// 播放 UI 交互音效（按钮点击、弹窗等）
        /// </summary>
        public void PlayUISFX(string clipName)
        {
            AudioClip clip = LoadAudioClip(clipName);
            if (clip == null) return;

            // UI 音效通过 BGM 源播放（OneShot 不打断 BGM）
            _bgmSource.PlayOneShot(clip);
        }

        // --- 音量控制 ---

        /// <summary>设置 BGM 音量（0~1），自动转换为 dB 并写入 AudioMixer</summary>
        public void SetBGMVolume(float volume)
        {
            BGMVolume = Mathf.Clamp01(volume);
            ApplyVolumeToMixer("BGMVolume", BGMVolume);
            SaveVolumeSettings();
            OnVolumeChanged?.Invoke(BGMVolume, SFXVolume, UIVolume);
        }

        /// <summary>设置 SFX 音量</summary>
        public void SetSFXVolume(float volume)
        {
            SFXVolume = Mathf.Clamp01(volume);
            ApplyVolumeToMixer("SFXVolume", SFXVolume);
            SaveVolumeSettings();
            OnVolumeChanged?.Invoke(BGMVolume, SFXVolume, UIVolume);
        }

        /// <summary>设置 UI 音效音量</summary>
        public void SetUIVolume(float volume)
        {
            UIVolume = Mathf.Clamp01(volume);
            ApplyVolumeToMixer("UIVolume", UIVolume);
            SaveVolumeSettings();
            OnVolumeChanged?.Invoke(BGMVolume, SFXVolume, UIVolume);
        }

        /// <summary>全局静音 / 取消静音</summary>
        public void MuteAll(bool mute)
        {
            IsMuted = mute;
            float db = mute ? -80f : 0f;
            _audioMixer?.SetFloat("MasterVolume", db);
            PlayerPrefs.SetInt(PP_MUTED, mute ? 1 : 0);
            PlayerPrefs.Save();
        }

        // --- 音量持久化 ---

        /// <summary>从 PlayerPrefs 加载音量设置</summary>
        public void LoadVolumeSettings()
        {
            BGMVolume = PlayerPrefs.GetFloat(PP_BGM_VOLUME, _defaultBGMVolume);
            SFXVolume = PlayerPrefs.GetFloat(PP_SFX_VOLUME, _defaultSFXVolume);
            UIVolume = PlayerPrefs.GetFloat(PP_UI_VOLUME, _defaultUIVolume);
            IsMuted = PlayerPrefs.GetInt(PP_MUTED, 0) == 1;

            ApplyVolumeToMixer("BGMVolume", BGMVolume);
            ApplyVolumeToMixer("SFXVolume", SFXVolume);
            ApplyVolumeToMixer("UIVolume", UIVolume);

            if (IsMuted)
                _audioMixer?.SetFloat("MasterVolume", -80f);

            Debug.Log($"[AudioManager] 音量设置已加载: BGM={BGMVolume:F1}, SFX={SFXVolume:F1}, UI={UIVolume:F1}, Muted={IsMuted}");
        }

        /// <summary>保存音量设置到 PlayerPrefs</summary>
        public void SaveVolumeSettings()
        {
            PlayerPrefs.SetFloat(PP_BGM_VOLUME, BGMVolume);
            PlayerPrefs.SetFloat(PP_SFX_VOLUME, SFXVolume);
            PlayerPrefs.SetFloat(PP_UI_VOLUME, UIVolume);
            PlayerPrefs.SetInt(PP_MUTED, IsMuted ? 1 : 0);
            PlayerPrefs.Save();
        }

        // --- 资源加载 ---

        /// <summary>
        /// 从 Resources 目录加载音频资源（带缓存）
        /// 查找路径：Audio/{clipName}
        /// </summary>
        public AudioClip LoadAudioClip(string clipName)
        {
            if (string.IsNullOrEmpty(clipName)) return null;

            if (_audioClipCache.TryGetValue(clipName, out AudioClip cached))
                return cached;

            AudioClip clip = Resources.Load<AudioClip>($"Audio/{clipName}");
            if (clip != null)
            {
                _audioClipCache[clipName] = clip;
            }
            return clip;
        }

        /// <summary>清除音频缓存</summary>
        public void ClearCache()
        {
            _audioClipCache.Clear();
            Resources.UnloadUnusedAssets();
        }

        // === 私有方法 ===

        /// <summary>将 0~1 音量转换为 dB 并应用到 AudioMixer</summary>
        private void ApplyVolumeToMixer(string paramName, float volume)
        {
            if (_audioMixer == null)
            {
                Debug.LogWarning("[AudioManager] AudioMixer 未配置");
                return;
            }

            // 0 -> -80dB (静音), 1 -> 0dB (最大)
            float db = volume > 0.001f ? 20f * Mathf.Log10(volume) : -80f;
            _audioMixer.SetFloat(paramName, db);
        }

        /// <summary>初始化 3D SFX 音频源对象池</summary>
        private void Initialize3DPool()
        {
            for (int i = 0; i < _sfx3DPoolSize; i++)
            {
                GameObject obj = new GameObject($"SFX_3D_{i}");
                obj.transform.SetParent(_sfx3DPoolRoot);
                AudioSource source = obj.AddComponent<AudioSource>();
                source.spatialBlend = 1f;
                source.rolloffMode = AudioRolloffMode.Linear;
                source.minDistance = 3f;
                source.maxDistance = 30f;
                source.playOnAwake = false;
                source.outputAudioMixerGroup = GetMixerGroup("SFX");
                _sfx3DPool.Enqueue(source);
            }
        }

        /// <summary>获取 AudioMixer 分组（通过名称匹配）</summary>
        private AudioMixerGroup GetMixerGroup(string groupName)
        {
            if (_audioMixer == null) return null;
            AudioMixerGroup[] groups = _audioMixer.FindMatchingGroups(groupName);
            return groups.Length > 0 ? groups[0] : null;
        }

        private AudioSource Get3DSourceFromPool()
        {
            return _sfx3DPool.Count > 0 ? _sfx3DPool.Dequeue() : null;
        }

        private void Return3DSourceToPool(AudioSource source)
        {
            source.Stop();
            source.clip = null;
            source.transform.localPosition = Vector3.zero;
            _sfx3DPool.Enqueue(source);
        }

        private IEnumerator Return3DSourceAfterPlay(AudioSource source, float delay)
        {
            yield return new WaitForSeconds(delay);
            Return3DSourceToPool(source);
        }

        // === BGM 淡入淡出协程 ===

        private IEnumerator FadeInBGM(float duration)
        {
            float targetVolume = _bgmSource.volume;
            _bgmSource.volume = 0f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _bgmSource.volume = Mathf.Lerp(0f, targetVolume, elapsed / duration);
                yield return null;
            }

            _bgmSource.volume = targetVolume;
            _bgmFadeCoroutine = null;
        }

        private IEnumerator FadeOutBGM(float duration)
        {
            float startVolume = _bgmSource.volume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _bgmSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
                yield return null;
            }

            _bgmSource.Stop();
            _bgmSource.clip = null;
            _bgmSource.volume = 1f;
            _bgmFadeCoroutine = null;
        }

        private IEnumerator CrossfadeBGM(AudioClip newClip, float duration)
        {
            // 淡出旧 BGM
            float startVolume = _bgmSource.volume;
            float halfDuration = duration * 0.5f;
            float elapsed = 0f;

            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                _bgmSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / halfDuration);
                yield return null;
            }

            // 切换 BGM
            _bgmSource.Stop();
            _bgmSource.clip = newClip;
            _bgmSource.loop = true;
            _bgmSource.volume = 0f;
            _bgmSource.Play();

            // 淡入新 BGM
            elapsed = 0f;
            float targetVolume = 1f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                _bgmSource.volume = Mathf.Lerp(0f, targetVolume, elapsed / halfDuration);
                yield return null;
            }

            _bgmSource.volume = targetVolume;
            _bgmFadeCoroutine = null;
        }
    }
}
