using UnityEngine;
using TMPro;
using System;
using System.Collections.Generic;

namespace OriginXR.Lobby
{
    /// <summary>
    /// 其他玩家 Avatar 同步控制器
    /// 负责：
    /// 1. 管理 LobbyScene 中其他在线玩家的 3D Avatar 的创建、展示、移除
    /// 2. 通过 WebSocket 事件同步位置/旋转/表情
    /// 3. 插值平滑移动（位置 Lerp，旋转 Slerp）
    /// 4. 对象池复用 Avatar GameObject
    /// 5. 显示玩家头顶信息（昵称 + 等级）
    /// </summary>
    public class PlayerAvatarSync : MonoBehaviour
    {
        [Header("配置")]
        [SerializeField] private int _maxVisiblePlayers = 30;
        [SerializeField] private float _positionLerpSpeed = 10f;
        [SerializeField] private float _rotationLerpSpeed = 15f;
        [SerializeField] private GameObject _avatarPrefab;            // 远程玩家预制体
        [SerializeField] private GameObject _nameplatePrefab;        // 头顶名称预制体
        [SerializeField] private Vector3 _nameplateOffset = new Vector3(0f, 2.5f, 0f);

        // === 运行时数据 ===
        private Dictionary<string, RemotePlayerData> _remotePlayers = new Dictionary<string, RemotePlayerData>();
        private Queue<GameObject> _avatarPool = new Queue<GameObject>();

        // === Unity 生命周期 ===

        private void Awake()
        {
            // 初始化对象池
            for (int i = 0; i < _maxVisiblePlayers; i++)
            {
                GameObject avatar = Instantiate(_avatarPrefab, transform);
                avatar.name = $"Avatar_Pool_{i}";
                avatar.SetActive(false);
                _avatarPool.Enqueue(avatar);
            }
        }

        private void Start()
        {
            RegisterNetworkEvents();
        }

        private void Update()
        {
            SmoothUpdateRemotePlayers();
        }

        private void OnDestroy()
        {
            UnregisterNetworkEvents();
        }

        // === 公共方法 ===

        /// <summary>注册 WebSocket 事件监听</summary>
        public void RegisterNetworkEvents()
        {
            var nm = Core.NetworkManager.Instance;
            if (nm == null) return;

            nm.RegisterHandler("lobby:player_enter", HandlePlayerEnter);
            nm.RegisterHandler("lobby:player_move", HandlePlayerMove);
            nm.RegisterHandler("lobby:player_leave", HandlePlayerLeave);
            nm.RegisterHandler("lobby:player_emote", HandlePlayerEmote);
        }

        /// <summary>注销 WebSocket 事件监听</summary>
        public void UnregisterNetworkEvents()
        {
            var nm = Core.NetworkManager.Instance;
            if (nm == null) return;

            nm.UnregisterHandler("lobby:player_enter", HandlePlayerEnter);
            nm.UnregisterHandler("lobby:player_move", HandlePlayerMove);
            nm.UnregisterHandler("lobby:player_leave", HandlePlayerLeave);
            nm.UnregisterHandler("lobby:player_emote", HandlePlayerEmote);
        }

        /// <summary>清除所有远程玩家</summary>
        public void ClearAllRemotePlayers()
        {
            foreach (var kvp in _remotePlayers)
            {
                ReturnAvatarToPool(kvp.Value);
            }
            _remotePlayers.Clear();
        }

        /// <summary>设置最大可见玩家数</summary>
        public void SetMaxVisible(int count) => _maxVisiblePlayers = Mathf.Max(count, 1);

        // === WebSocket 事件处理 ===

        private void HandlePlayerEnter(string jsonData)
        {
            try
            {
                string playerId = ExtractJsonString(jsonData, "playerId");
                string username = ExtractJsonString(jsonData, "username");
                int level = ExtractJsonInt(jsonData, "level");
                string avatarId = ExtractJsonString(jsonData, "avatarId");
                string title = ExtractJsonString(jsonData, "title");

                float px = ExtractJsonFloat(jsonData, "px");
                float py = ExtractJsonFloat(jsonData, "py");
                float pz = ExtractJsonFloat(jsonData, "pz");

                if (_remotePlayers.ContainsKey(playerId)) return;

                GameObject avatarObj = GetAvatarFromPool();
                if (avatarObj == null) return;

                avatarObj.SetActive(true);
                avatarObj.transform.position = new Vector3(px, py, pz);

                Animator animator = avatarObj.GetComponentInChildren<Animator>();

                // 创建头顶信息
                GameObject nameplate = null;
                if (_nameplatePrefab != null)
                {
                    nameplate = Instantiate(_nameplatePrefab, avatarObj.transform);
                    nameplate.transform.localPosition = _nameplateOffset;
                    var tmpText = nameplate.GetComponentInChildren<TextMeshProUGUI>();
                    if (tmpText != null) tmpText.text = $"{username} Lv.{level}";
                }

                RemotePlayerData data = new RemotePlayerData
                {
                    playerId = playerId,
                    username = username,
                    level = level,
                    title = title,
                    avatarId = avatarId,
                    avatarObject = avatarObj,
                    animator = animator,
                    nameplate = nameplate,
                    targetPosition = new Vector3(px, py, pz),
                    targetRotation = Quaternion.identity
                };

                _remotePlayers[playerId] = data;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AvatarSync] 处理玩家进入事件异常: {ex.Message}");
            }
        }

        private void HandlePlayerMove(string jsonData)
        {
            try
            {
                string playerId = ExtractJsonString(jsonData, "playerId");
                if (!_remotePlayers.TryGetValue(playerId, out var data)) return;

                float px = ExtractJsonFloat(jsonData, "px");
                float py = ExtractJsonFloat(jsonData, "py");
                float pz = ExtractJsonFloat(jsonData, "pz");
                float ry = ExtractJsonFloat(jsonData, "ry");

                data.targetPosition = new Vector3(px, py, pz);
                data.targetRotation = Quaternion.Euler(0f, ry, 0f);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AvatarSync] 处理玩家移动事件异常: {ex.Message}");
            }
        }

        private void HandlePlayerLeave(string jsonData)
        {
            try
            {
                string playerId = ExtractJsonString(jsonData, "playerId");
                if (_remotePlayers.TryGetValue(playerId, out var data))
                {
                    ReturnAvatarToPool(data);
                    _remotePlayers.Remove(playerId);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AvatarSync] 处理玩家离开事件异常: {ex.Message}");
            }
        }

        private void HandlePlayerEmote(string jsonData)
        {
            try
            {
                string playerId = ExtractJsonString(jsonData, "playerId");
                string emoteId = ExtractJsonString(jsonData, "emoteId");

                if (_remotePlayers.TryGetValue(playerId, out var data) && data.animator != null)
                {
                    data.animator.SetTrigger(emoteId);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AvatarSync] 处理玩家表情事件异常: {ex.Message}");
            }
        }

        // === 私有方法 ===

        /// <summary>平滑插值更新所有远程玩家的位置与旋转</summary>
        private void SmoothUpdateRemotePlayers()
        {
            float dt = Time.deltaTime;
            foreach (var kvp in _remotePlayers)
            {
                RemotePlayerData data = kvp.Value;
                if (data.avatarObject == null || !data.avatarObject.activeSelf) continue;

                Transform t = data.avatarObject.transform;
                t.position = Vector3.Lerp(t.position, data.targetPosition, _positionLerpSpeed * dt);
                t.rotation = Quaternion.Slerp(t.rotation, data.targetRotation, _rotationLerpSpeed * dt);
            }
        }

        private GameObject GetAvatarFromPool()
        {
            if (_avatarPool.Count == 0) return null;
            return _avatarPool.Dequeue();
        }

        private void ReturnAvatarToPool(RemotePlayerData data)
        {
            if (data.avatarObject != null)
            {
                // 清理头顶名称
                if (data.nameplate != null)
                    Destroy(data.nameplate);

                data.avatarObject.SetActive(false);
                data.avatarObject.transform.SetParent(transform);
                _avatarPool.Enqueue(data.avatarObject);
            }
        }

        // === JSON 简易解析（避免 Newtonsoft.Json 依赖） ===

        private string ExtractJsonString(string json, string key)
        {
            string search = $"\"{key}\":\"";
            int start = json.IndexOf(search);
            if (start < 0) return "";
            start += search.Length;
            int end = json.IndexOf("\"", start);
            if (end < 0) return "";
            return json.Substring(start, end - start);
        }

        private int ExtractJsonInt(string json, string key)
        {
            string search = $"\"{key}\":";
            int start = json.IndexOf(search);
            if (start < 0) return 0;
            start += search.Length;
            int end = start;
            while (end < json.Length && (char.IsDigit(json[end]) || json[end] == '-'))
                end++;
            if (end == start) return 0;
            int.TryParse(json.Substring(start, end - start), out int val);
            return val;
        }

        private float ExtractJsonFloat(string json, string key)
        {
            string search = $"\"{key}\":";
            int start = json.IndexOf(search);
            if (start < 0) return 0f;
            start += search.Length;
            int end = start;
            while (end < json.Length && (char.IsDigit(json[end]) || json[end] == '.' || json[end] == '-'))
                end++;
            if (end == start) return 0f;
            float.TryParse(json.Substring(start, end - start), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float val);
            return val;
        }
    }

    /// <summary>
    /// 远程玩家运行时数据
    /// </summary>
    [Serializable]
    public class RemotePlayerData
    {
        public string playerId;
        public string username;
        public int level;
        public string title;
        public string avatarId;
        public GameObject avatarObject;
        public GameObject nameplate;
        public Animator animator;
        public Vector3 targetPosition;
        public Quaternion targetRotation;
    }
}
