using System;
using UnityEngine;
using OriginXR.Core;

public enum GameState { Boot, Login, Lobby, Battle, Loading }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public GameState CurrentState { get; private set; }
    public bool IsInitialized { get; private set; }
    public event Action<GameState> OnGameStateChanged;

    [Header("子系统")]
    [SerializeField] private AudioManager _audioManager;
    [SerializeField] private NetworkManager _networkManager;
    [SerializeField] private HttpManager _httpManager;
    [SerializeField] private SceneLoader _sceneLoader;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start() { Initialize(); }

    private void OnDestroy() { if (Instance == this) Instance = null; }

    public void Initialize()
    {
        if (IsInitialized) return;
        Debug.Log("[GameManager] 初始化...");

        if (_audioManager != null) _audioManager.Initialize();
        if (_networkManager != null) _networkManager.Initialize();
        if (_httpManager != null) _httpManager.Initialize();
        if (_sceneLoader != null) _sceneLoader.Initialize();

        IsInitialized = true;
        ChangeGameState(GameState.Boot);
        Debug.Log("[GameManager] 初始化完成");
    }

    public void ChangeGameState(GameState s)
    {
        if (CurrentState == s) return;
        CurrentState = s;
        OnGameStateChanged?.Invoke(s);
    }

    public void PauseGame() => Time.timeScale = 0f;
    public void ResumeGame() => Time.timeScale = 1f;

    public void QuitGame()
    {
        _networkManager?.Disconnect();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
