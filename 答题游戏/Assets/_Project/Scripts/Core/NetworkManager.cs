using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using System.Text;

namespace OriginXR.Core
{
    /// <summary>
    /// WebSocket 连接管理器（单例）
    /// 负责：
    /// 1. 基于 System.Net.WebSockets.ClientWebSocket 实现长连接
    /// 2. 消息收发（文本 JSON + 二进制 Protobuf）
    /// 3. 事件路由：根据 eventName 前缀分发到注册的处理器
    /// 4. 心跳检测（每15秒 ping）与指数退避断线重连
    /// 5. 线程安全的消息收发队列
    ///
    /// WebSocket 事件格式：{ "event": "eventName", "data": {...} }
    /// </summary>
    public class NetworkManager : MonoBehaviour
    {
        [Header("连接配置")]
        [SerializeField] private float _heartbeatInterval = 15f;
        [SerializeField] private float _heartbeatTimeout = 30f;
        [SerializeField] private int _maxReconnectAttempts = 5;
        [SerializeField] private float _baseReconnectDelay = 2f;
        [SerializeField] private float _maxReconnectDelay = 30f;
        [SerializeField] private int _receiveBufferSize = 4096;

        // === 单例 ===
        public static NetworkManager Instance { get; private set; }

        // === 属性 ===
        public bool IsConnected { get; private set; }
        public string ServerUrl { get; private set; }

        // === 内部状态 ===
        private ClientWebSocket _webSocket;
        private CancellationTokenSource _cancellationTokenSource;
        private string _authToken;
        private int _reconnectAttempts;
        private DateTime _lastHeartbeatResponse;
        private bool _isReconnecting;

        // === 消息处理器注册表 ===
        private Dictionary<string, List<Action<string>>> _jsonHandlers = new Dictionary<string, List<Action<string>>>();
        private Dictionary<string, List<Action<byte[]>>> _protoHandlers = new Dictionary<string, List<Action<byte[]>>>();

        // === 线程安全发送队列 ===
        private Queue<byte[]> _sendQueue = new Queue<byte[]>();
        private readonly object _sendLock = new object();
        private bool _isSendPending;

        // === 事件 ===
        public event Action OnConnected;
        public event Action<string> OnDisconnected;
        public event Action<string> OnError;
        public event Action OnReconnecting;

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
        }

        private void Update()
        {
            // 处理主线程发送队列
            ProcessSendQueue();
        }

        private void OnDestroy()
        {
            Disconnect();
            if (Instance == this) Instance = null;
        }

        private void OnApplicationQuit()
        {
            Disconnect();
        }

        // === 公共方法 ===

        public void Initialize()
        {
            Debug.Log("[NetworkManager] 网络管理器已初始化");
        }

        /// <summary>
        /// 建立 WebSocket 连接
        /// </summary>
        /// <param name="url">WebSocket 地址，如 ws://localhost:3000/ws</param>
        /// <param name="token">JWT 认证令牌</param>
        public void Connect(string url, string token)
        {
            if (_webSocket != null && _webSocket.State == WebSocketState.Open)
            {
                Debug.LogWarning("[NetworkManager] 已有活跃连接，先断开旧连接");
                Disconnect();
            }

            ServerUrl = url;
            _authToken = token;
            _reconnectAttempts = 0;
            _cancellationTokenSource = new CancellationTokenSource();

            StartCoroutine(ConnectRoutine());
        }

        /// <summary>
        /// 断开 WebSocket 连接（不触发重连）
        /// </summary>
        public void Disconnect()
        {
            _isReconnecting = false;
            _cancellationTokenSource?.Cancel();

            if (_webSocket != null)
            {
                try
                {
                    if (_webSocket.State == WebSocketState.Open ||
                        _webSocket.State == WebSocketState.CloseReceived)
                    {
                        _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client closing", CancellationToken.None).Wait(3000);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[NetworkManager] 关闭连接异常: {ex.Message}");
                }
                finally
                {
                    _webSocket.Dispose();
                    _webSocket = null;
                }
            }

            IsConnected = false;
            Debug.Log("[NetworkManager] 连接已断开");
        }

        /// <summary>
        /// 发送 JSON 文本消息
        /// 格式：{ "event": "pvp:answer", "data": { ... } }
        /// </summary>
        public void Send(string eventName, string jsonData)
        {
            if (!IsConnected || _webSocket == null)
            {
                Debug.LogWarning($"[NetworkManager] 未连接，无法发送消息: {eventName}");
                return;
            }

            string msg = $"{{\"event\":\"{eventName}\",\"data\":{jsonData}}}";
            byte[] bytes = Encoding.UTF8.GetBytes(msg);

            lock (_sendLock)
            {
                _sendQueue.Enqueue(bytes);
                _isSendPending = true;
            }
        }

        /// <summary>
        /// 发送 Protobuf 二进制消息
        /// </summary>
        public void SendProtobuf(string eventName, byte[] protobufData)
        {
            if (!IsConnected || _webSocket == null)
            {
                Debug.LogWarning($"[NetworkManager] 未连接，无法发送 Protobuf 消息: {eventName}");
                return;
            }

            // 简单协议：前2字节=事件名长度，接着是事件名UTF8，再是Protobuf数据
            byte[] eventBytes = Encoding.UTF8.GetBytes(eventName);
            if (eventBytes.Length > 255)
            {
                Debug.LogError($"[NetworkManager] 事件名过长: {eventName}");
                return;
            }

            byte[] packet = new byte[1 + eventBytes.Length + protobufData.Length];
            packet[0] = (byte)eventBytes.Length;
            Array.Copy(eventBytes, 0, packet, 1, eventBytes.Length);
            Array.Copy(protobufData, 0, packet, 1 + eventBytes.Length, protobufData.Length);

            lock (_sendLock)
            {
                _sendQueue.Enqueue(packet);
                _isSendPending = true;
            }
        }

        /// <summary>
        /// 注册 JSON 消息处理器
        /// </summary>
        public void RegisterHandler(string eventName, Action<string> handler)
        {
            if (!_jsonHandlers.ContainsKey(eventName))
                _jsonHandlers[eventName] = new List<Action<string>>();
            _jsonHandlers[eventName].Add(handler);
        }

        /// <summary>
        /// 注销 JSON 消息处理器
        /// </summary>
        public void UnregisterHandler(string eventName, Action<string> handler)
        {
            if (_jsonHandlers.ContainsKey(eventName))
                _jsonHandlers[eventName].Remove(handler);
        }

        /// <summary>
        /// 注册 Protobuf 消息处理器
        /// </summary>
        public void RegisterProtoHandler(string eventName, Action<byte[]> handler)
        {
            if (!_protoHandlers.ContainsKey(eventName))
                _protoHandlers[eventName] = new List<Action<byte[]>>();
            _protoHandlers[eventName].Add(handler);
        }

        /// <summary>
        /// 注销 Protobuf 消息处理器
        /// </summary>
        public void UnregisterProtoHandler(string eventName, Action<byte[]> handler)
        {
            if (_protoHandlers.ContainsKey(eventName))
                _protoHandlers[eventName].Remove(handler);
        }

        // === 私有：连接协程 ===

        private IEnumerator ConnectRoutine()
        {
            Debug.Log($"[NetworkManager] 正在连接: {ServerUrl}");
            bool hasError = false;
            string errorMsg = "";

            try
            {
                _webSocket = new ClientWebSocket();
                _webSocket.Options.AddSubProtocol("json");

                if (!string.IsNullOrEmpty(_authToken))
                {
                    _webSocket.Options.SetRequestHeader("Authorization", $"Bearer {_authToken}");
                }
            }
            catch (Exception ex)
            {
                HandleConnectionError(ex.Message);
                yield break;
            }

            // 异步连接
            Task connectTask = null;
            try
            {
                connectTask = _webSocket.ConnectAsync(new Uri(ServerUrl), _cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                HandleConnectionError(ex.Message);
                yield break;
            }

            yield return new WaitUntil(() => connectTask.IsCompleted);

            if (connectTask.IsFaulted)
            {
                HandleConnectionError(connectTask.Exception?.InnerException?.Message ?? "连接失败");
                yield break;
            }

            if (_webSocket.State == WebSocketState.Open)
            {
                IsConnected = true;
                _reconnectAttempts = 0;
                _lastHeartbeatResponse = DateTime.UtcNow;

                Debug.Log("[NetworkManager] WebSocket 连接成功");
                OnConnected?.Invoke();

                // 启动接收和心跳协程
                StartCoroutine(ReceiveRoutine());
                StartCoroutine(HeartbeatRoutine());
            }
        }

        // === 私有：接收协程 ===

        private IEnumerator ReceiveRoutine()
        {
            byte[] buffer = new byte[_receiveBufferSize];
            List<byte> messageBuffer = new List<byte>();

            while (_webSocket != null && _webSocket.State == WebSocketState.Open)
            {
                Task<WebSocketReceiveResult> receiveTask = null;
                bool taskStarted = false;

                try
                {
                    receiveTask = _webSocket.ReceiveAsync(
                        new ArraySegment<byte>(buffer), _cancellationTokenSource.Token);
                    taskStarted = true;
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (WebSocketException ex)
                {
                    Debug.LogWarning($"[NetworkManager] 接收异常: {ex.Message}");
                    break;
                }

                if (!taskStarted) break;

                yield return new WaitUntil(() => receiveTask.IsCompleted);

                if (receiveTask.IsFaulted || receiveTask.IsCanceled)
                    break;

                WebSocketReceiveResult result = receiveTask.Result;

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    Debug.Log("[NetworkManager] 服务端关闭连接");
                    break;
                }

                // 累加数据
                messageBuffer.AddRange(new ArraySegment<byte>(buffer, 0, result.Count));

                if (result.EndOfMessage)
                {
                    byte[] completeMessage = messageBuffer.ToArray();
                    messageBuffer.Clear();

                    // 根据消息类型分发
                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        string json = Encoding.UTF8.GetString(completeMessage);
                        DispatchJsonMessage(json);
                    }
                    else if (result.MessageType == WebSocketMessageType.Binary)
                    {
                        DispatchProtoMessage(completeMessage);
                    }
                }
            }

            // 连接断开处理
            bool wasConnected = IsConnected;
            IsConnected = false;

            if (wasConnected && !_isReconnecting)
            {
                string reason = "连接意外断开";
                OnDisconnected?.Invoke(reason);
                Debug.LogWarning($"[NetworkManager] {reason}");

                if (_reconnectAttempts < _maxReconnectAttempts)
                {
                    StartCoroutine(ReconnectRoutine());
                }
            }

            // 清理
            if (_webSocket != null)
            {
                _webSocket.Dispose();
                _webSocket = null;
            }
        }

        // === 私有：心跳协程 ===

        private IEnumerator HeartbeatRoutine()
        {
            while (IsConnected && _webSocket != null && _webSocket.State == WebSocketState.Open)
            {
                yield return new WaitForSeconds(_heartbeatInterval);

                // 检查上次心跳响应是否超时
                if ((DateTime.UtcNow - _lastHeartbeatResponse).TotalSeconds > _heartbeatTimeout)
                {
                    Debug.LogWarning("[NetworkManager] 心跳超时，触发重连");
                    IsConnected = false;
                    StartCoroutine(ReconnectRoutine());
                    yield break;
                }

                // 发送 ping（发送空文本帧作为 ping）
                try
                {
                    Send("ping", "{}");
                    _lastHeartbeatResponse = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[NetworkManager] 心跳发送失败: {ex.Message}");
                }
            }
        }

        // === 私有：重连协程 ===

        private IEnumerator ReconnectRoutine()
        {
            if (_isReconnecting) yield break;
            _isReconnecting = true;

            while (_reconnectAttempts < _maxReconnectAttempts)
            {
                _reconnectAttempts++;
                float delay = Mathf.Min(_baseReconnectDelay * Mathf.Pow(2, _reconnectAttempts - 1), _maxReconnectDelay);

                Debug.Log($"[NetworkManager] 第 {_reconnectAttempts}/{_maxReconnectAttempts} 次重连，等待 {delay:F1}s");
                OnReconnecting?.Invoke();

                yield return new WaitForSeconds(delay);

                // 清理旧连接
                if (_webSocket != null)
                {
                    try { _webSocket.Dispose(); } catch { }
                    _webSocket = null;
                }

                // 尝试重新连接
                _cancellationTokenSource = new CancellationTokenSource();
                yield return StartCoroutine(ConnectRoutine());

                if (IsConnected)
                {
                    Debug.Log("[NetworkManager] 重连成功");
                    _isReconnecting = false;
                    yield break;
                }
            }

            Debug.LogError("[NetworkManager] 重连次数用尽，放弃重连");
            OnError?.Invoke("重连失败，已达最大尝试次数");
            _isReconnecting = false;
        }

        // === 私有：消息分发 ===

        /// <summary>
        /// 解析 JSON 消息并分发到注册的处理器
        /// </summary>
        private void DispatchJsonMessage(string json)
        {
            try
            {
                // 简易 JSON 解析（避免依赖 Newtonsoft.Json）
                string eventName = ExtractJsonField(json, "event");
                string data = ExtractJsonField(json, "data");

                if (string.IsNullOrEmpty(eventName))
                {
                    Debug.LogWarning($"[NetworkManager] 无法解析消息事件名: {json}");
                    return;
                }

                // 心跳响应
                if (eventName == "pong")
                {
                    _lastHeartbeatResponse = DateTime.UtcNow;
                    return;
                }

                // 分发给注册的处理器
                if (_jsonHandlers.TryGetValue(eventName, out var handlers))
                {
                    foreach (var handler in handlers)
                    {
                        try { handler?.Invoke(data ?? "{}"); }
                        catch (Exception ex) { Debug.LogError($"[NetworkManager] 处理器异常 [{eventName}]: {ex.Message}"); }
                    }
                }
                else
                {
                    Debug.Log($"[NetworkManager] 未注册的事件: {eventName}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NetworkManager] 消息分发异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 分发 Protobuf 二进制消息
        /// 协议：第1字节=事件名长度，接着是事件名UTF8，再是Protobuf数据
        /// </summary>
        private void DispatchProtoMessage(byte[] data)
        {
            try
            {
                if (data.Length < 2) return;

                int nameLen = data[0];
                if (data.Length < 1 + nameLen) return;

                string eventName = Encoding.UTF8.GetString(data, 1, nameLen);
                int protoOffset = 1 + nameLen;
                int protoLen = data.Length - protoOffset;
                byte[] protoData = new byte[protoLen];
                Array.Copy(data, protoOffset, protoData, 0, protoLen);

                if (_protoHandlers.TryGetValue(eventName, out var handlers))
                {
                    foreach (var handler in handlers)
                    {
                        try { handler?.Invoke(protoData); }
                        catch (Exception ex) { Debug.LogError($"[NetworkManager] Proto处理器异常 [{eventName}]: {ex.Message}"); }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NetworkManager] Proto消息分发异常: {ex.Message}");
            }
        }

        // === 私有：发送队列处理（主线程） ===

        private async void ProcessSendQueue()
        {
            if (!_isSendPending || _webSocket == null || _webSocket.State != WebSocketState.Open) return;

            byte[] dataToSend = null;
            lock (_sendLock)
            {
                if (_sendQueue.Count > 0)
                {
                    dataToSend = _sendQueue.Dequeue();
                }
                _isSendPending = _sendQueue.Count > 0;
            }

            if (dataToSend != null)
            {
                try
                {
                    await _webSocket.SendAsync(
                        new ArraySegment<byte>(dataToSend),
                        WebSocketMessageType.Text,
                        true,
                        _cancellationTokenSource.Token);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[NetworkManager] 发送失败: {ex.Message}");
                }
            }
        }

        // === 私有：工具方法 ===

        private void HandleConnectionError(string errorMessage)
        {
            Debug.LogError($"[NetworkManager] 连接错误: {errorMessage}");
            IsConnected = false;
            OnError?.Invoke(errorMessage);

            if (_reconnectAttempts < _maxReconnectAttempts)
            {
                StartCoroutine(ReconnectRoutine());
            }
        }

        /// <summary>
        /// 简易 JSON 字段提取（不依赖第三方库）
        /// 从 {"key":"value","key2":{...}} 中提取 key 对应的值
        /// </summary>
        private string ExtractJsonField(string json, string fieldName)
        {
            string searchKey = $"\"{fieldName}\":";
            int startIndex = json.IndexOf(searchKey, StringComparison.Ordinal);
            if (startIndex < 0) return null;

            startIndex += searchKey.Length;

            // 跳过空白
            while (startIndex < json.Length && char.IsWhiteSpace(json[startIndex]))
                startIndex++;
            if (startIndex >= json.Length) return null;

            char firstChar = json[startIndex];

            if (firstChar == '{' || firstChar == '[')
            {
                // 提取嵌套对象/数组：计数括号匹配
                char openChar = firstChar;
                char closeChar = (firstChar == '{') ? '}' : ']';
                int depth = 1;
                int endIndex = startIndex + 1;
                while (endIndex < json.Length && depth > 0)
                {
                    if (json[endIndex] == openChar) depth++;
                    else if (json[endIndex] == closeChar) depth--;
                    endIndex++;
                }
                return json.Substring(startIndex, endIndex - startIndex);
            }
            else if (firstChar == '"')
            {
                // 提取字符串值
                int endIndex = startIndex + 1;
                while (endIndex < json.Length)
                {
                    if (json[endIndex] == '"' && json[endIndex - 1] != '\\')
                        break;
                    endIndex++;
                }
                return json.Substring(startIndex + 1, endIndex - startIndex - 1);
            }
            else
            {
                // 数字或布尔值
                int endIndex = startIndex;
                while (endIndex < json.Length && !(json[endIndex] == ',' || json[endIndex] == '}' || json[endIndex] == ']'))
                    endIndex++;
                return json.Substring(startIndex, endIndex - startIndex).Trim();
            }
        }
    }
}
