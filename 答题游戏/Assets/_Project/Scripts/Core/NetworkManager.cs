using UnityEngine;
using System;
using System.Collections;

namespace OriginXR.Core
{
    /// <summary>
    /// WebSocket 连接管理器
    /// 负责：
    /// 1. 与后端建立、维护、重连 WebSocket 长连接
    /// 2. 消息收发（JSON 和 Protobuf 两种格式）
    /// 3. 消息路由：根据 eventName 分发到已注册的处理器
    /// 4. 心跳检测与断线重连机制
    ///
    /// WebSocket 事件流向：
    ///   pvp:*        -> PVPBattleController
    ///   chat:*       -> ChatSystem（预留）
    ///   lobby:*      -> PlayerAvatarSync
    ///   guild:*      -> GuildManager（预留）
    ///   notify:*     -> UIManager / ToastManager
    /// </summary>
    public class NetworkManager : MonoBehaviour
    {
        // === 单例 ===
        public static NetworkManager Instance { get; private set; }

        // === 属性 ===
        /// <summary>WebSocket 是否已连接</summary>
        public bool IsConnected { get; private set; }

        /// <summary>重连间隔（秒）</summary>
        public float ReconnectInterval { get; set; } = 3f;

        /// <summary>最大重连次数</summary>
        public int MaxReconnectAttempts { get; set; } = 5;

        /// <summary>当前重连次数</summary>
        private int _reconnectAttempts;
        private string _serverUrl;
        private string _authToken;

        // === Unity 生命周期 ===
        private void Awake() { }
        private void OnDestroy() { }
        private void OnApplicationPause(bool pause) { }

        // === 公共方法 ===

        /// <summary>建立 WebSocket 连接</summary>
        /// <param name="url">服务器地址 ws:// 或 wss://</param>
        /// <param name="token">JWT 认证 Token</param>
        public void Connect(string url, string token) { }

        /// <summary>主动断开 WebSocket 连接</summary>
        public void Disconnect() { }

        /// <summary>发送 JSON 格式消息</summary>
        /// <param name="eventName">事件名，如 "pvp:answer"</param>
        /// <param name="jsonData">JSON 字符串数据</param>
        public void Send(string eventName, string jsonData) { }

        /// <summary>发送 Protobuf 二进制消息</summary>
        /// <param name="eventName">事件名</param>
        /// <param name="data">Protobuf 序列化后的字节数组</param>
        public void SendProtobuf(string eventName, byte[] data) { }

        /// <summary>注册消息处理器</summary>
        /// <param name="eventName">事件名</param>
        /// <param name="handler">回调函数（接收 JSON 字符串）</param>
        public void RegisterHandler(string eventName, Action<string> handler) { }

        /// <summary>注销消息处理器</summary>
        public void UnregisterHandler(string eventName, Action<string> handler) { }

        /// <summary>断线重连协程：指数退避重连，超过最大次数后放弃</summary>
        private IEnumerator ReconnectCoroutine() { yield return null; }

        /// <summary>心跳检测协程：每15秒发送 ping，超过30秒无响应则触发重连</summary>
        private IEnumerator HeartbeatCoroutine() { yield return null; }

        // === 私有方法 ===
        private void HandleMessageReceived(string eventName, string rawData) { }
        private void HandleConnectionClosed() { }
        private void HandleConnectionError(string errorMsg) { }

        // === 事件 ===
        /// <summary>连接成功回调</summary>
        public event Action OnConnected;

        /// <summary>断开连接回调，参数为断开原因</summary>
        public event Action<string> OnDisconnected;

        /// <summary>连接错误回调，参数为错误信息</summary>
        public event Action<string> OnError;
    }
}
