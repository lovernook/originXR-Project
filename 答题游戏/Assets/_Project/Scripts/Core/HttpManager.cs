using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace OriginXR.Core
{
    /// <summary>
    /// HTTP 请求管理器（单例）
    /// 负责：
    /// 1. 封装 UnityWebRequest 的 REST 请求（GET/POST/PUT/PATCH/DELETE）
    /// 2. 统一注入 JWT Token 到 Authorization 头
    /// 3. 泛型 JSON 反序列化（基于 JsonUtility）
    /// 4. 网络超时自动重试（最多2次）
    /// 5. 统一错误码处理（401=Token过期, 5xx=服务端错误）
    /// </summary>
    public class HttpManager : MonoBehaviour
    {
        [Header("请求配置")]
        [SerializeField] private int _requestTimeout = 30;
        [SerializeField] private int _maxRetries = 2;
        [SerializeField] private float _retryDelay = 1f;

        // === 单例 ===
        public static HttpManager Instance { get; private set; }

        // === 属性 ===
        public string BaseUrl { get; private set; } = "http://localhost:3000/api/v1";
        public string AuthToken { get; private set; }

        /// <summary>Token 过期回调，用于触发重新登录</summary>
        public event Action OnTokenExpired;

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

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // === 公共方法 ===

        public void Initialize()
        {
            Debug.Log($"[HttpManager] 已初始化，BaseUrl={BaseUrl}");
        }

        /// <summary>设置 API 基础地址</summary>
        public void SetBaseUrl(string url)
        {
            BaseUrl = url.TrimEnd('/');
            Debug.Log($"[HttpManager] BaseUrl 已更新: {BaseUrl}");
        }

        /// <summary>设置认证令牌</summary>
        public void SetAuthToken(string token)
        {
            AuthToken = token;
        }

        /// <summary>清除认证令牌</summary>
        public void ClearAuthToken()
        {
            AuthToken = null;
        }

        // === REST 请求方法 ===

        /// <summary>GET 请求</summary>
        /// <typeparam name="T">响应数据类型（需标记 [Serializable]）</typeparam>
        /// <param name="endpoint">接口路径，如 "/users/profile"</param>
        /// <param name="onSuccess">成功回调</param>
        /// <param name="onError">失败回调，参数为错误信息</param>
        public void Get<T>(string endpoint, Action<T> onSuccess, Action<string> onError = null)
        {
            StartCoroutine(SendRequest<T>(UnityWebRequest.kHttpVerbGET, endpoint, null, onSuccess, onError, 0));
        }

        /// <summary>POST 请求</summary>
        public void Post<T>(string endpoint, object body, Action<T> onSuccess, Action<string> onError = null)
        {
            StartCoroutine(SendRequest<T>(UnityWebRequest.kHttpVerbPOST, endpoint, body, onSuccess, onError, 0));
        }

        /// <summary>PUT 请求</summary>
        public void Put<T>(string endpoint, object body, Action<T> onSuccess, Action<string> onError = null)
        {
            StartCoroutine(SendRequest<T>(UnityWebRequest.kHttpVerbPUT, endpoint, body, onSuccess, onError, 0));
        }

        /// <summary>PATCH 请求</summary>
        public void Patch<T>(string endpoint, object body, Action<T> onSuccess, Action<string> onError = null)
        {
            StartCoroutine(SendRequest<T>("PATCH", endpoint, body, onSuccess, onError, 0));
        }

        /// <summary>DELETE 请求</summary>
        public void Delete<T>(string endpoint, Action<T> onSuccess, Action<string> onError = null)
        {
            StartCoroutine(SendRequest<T>(UnityWebRequest.kHttpVerbDELETE, endpoint, null, onSuccess, onError, 0));
        }

        /// <summary>文件上传（multipart/form-data）</summary>
        public void UploadFile<T>(string endpoint, byte[] fileData, string fileName, string fieldName = "file",
            Action<T> onSuccess = null, Action<string> onError = null)
        {
            StartCoroutine(UploadFileRoutine<T>(endpoint, fileData, fileName, fieldName, onSuccess, onError));
        }

        // === 核心请求协程 ===

        private IEnumerator SendRequest<T>(string method, string endpoint, object body,
            Action<T> onSuccess, Action<string> onError, int retryCount)
        {
            string url = $"{BaseUrl}{endpoint}";
            using (UnityWebRequest request = new UnityWebRequest(url, method))
            {
                // 设置请求体
                if (body != null)
                {
                    string jsonBody = JsonUtility.ToJson(body);
                    byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                    request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                }
                else
                {
                    request.uploadHandler = new UploadHandlerRaw(null);
                }

                request.downloadHandler = new DownloadHandlerBuffer();
                request.timeout = _requestTimeout;
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Accept", "application/json");

                // 注入认证令牌
                if (!string.IsNullOrEmpty(AuthToken))
                {
                    request.SetRequestHeader("Authorization", $"Bearer {AuthToken}");
                }

                yield return request.SendWebRequest();

                // 处理响应
                if (request.result == UnityWebRequest.Result.Success)
                {
                    string responseText = request.downloadHandler.text;
                    try
                    {
                        T result = JsonUtility.FromJson<T>(responseText);
                        onSuccess?.Invoke(result);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[HttpManager] JSON 解析失败: {ex.Message}\n原始数据: {responseText}");
                        onError?.Invoke($"数据解析错误: {ex.Message}");
                    }
                }
                else
                {
                    long statusCode = request.responseCode;
                    string errorBody = request.downloadHandler?.text ?? request.error;

                    // 401 Token 过期
                    if (statusCode == 401)
                    {
                        Debug.LogWarning("[HttpManager] Token 已过期");
                        ClearAuthToken();
                        OnTokenExpired?.Invoke();
                        onError?.Invoke("登录已过期，请重新登录");
                        yield break;
                    }

                    // 网络超时或5xx服务端错误 → 自动重试
                    if (retryCount < _maxRetries &&
                        (request.result == UnityWebRequest.Result.ConnectionError ||
                         request.result == UnityWebRequest.Result.ProtocolError &&
                         statusCode >= 500))
                    {
                        Debug.LogWarning($"[HttpManager] 请求失败，第 {retryCount + 1} 次重试... [{method} {endpoint}]");
                        yield return new WaitForSeconds(_retryDelay);
                        StartCoroutine(SendRequest<T>(method, endpoint, body, onSuccess, onError, retryCount + 1));
                        yield break;
                    }

                    Debug.LogError($"[HttpManager] 请求失败 [{method} {endpoint}] Status={statusCode}: {errorBody}");
                    onError?.Invoke($"请求失败 ({statusCode}): {request.error}");
                }
            }
        }

        /// <summary>文件上传协程</summary>
        private IEnumerator UploadFileRoutine<T>(string endpoint, byte[] fileData, string fileName, string fieldName,
            Action<T> onSuccess, Action<string> onError)
        {
            string url = $"{BaseUrl}{endpoint}";
            List<IMultipartFormSection> formData = new List<IMultipartFormSection>
            {
                new MultipartFormFileSection(fieldName, fileData, fileName, "application/octet-stream")
            };

            using (UnityWebRequest request = UnityWebRequest.Post(url, formData))
            {
                request.timeout = _requestTimeout * 2; // 上传超时更长

                if (!string.IsNullOrEmpty(AuthToken))
                {
                    request.SetRequestHeader("Authorization", $"Bearer {AuthToken}");
                }

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        T result = JsonUtility.FromJson<T>(request.downloadHandler.text);
                        onSuccess?.Invoke(result);
                    }
                    catch (Exception ex)
                    {
                        onError?.Invoke($"解析错误: {ex.Message}");
                    }
                }
                else
                {
                    onError?.Invoke($"上传失败: {request.error}");
                }
            }
        }

        // === 便捷方法 ===

        /// <summary>GET 请求，返回原始 JSON 字符串</summary>
        public void GetRaw(string endpoint, Action<string> onSuccess, Action<string> onError = null)
        {
            StartCoroutine(SendRawRequest(UnityWebRequest.kHttpVerbGET, endpoint, null, onSuccess, onError));
        }

        /// <summary>POST 请求，返回原始 JSON 字符串</summary>
        public void PostRaw(string endpoint, object body, Action<string> onSuccess, Action<string> onError = null)
        {
            StartCoroutine(SendRawRequest(UnityWebRequest.kHttpVerbPOST, endpoint, body, onSuccess, onError));
        }

        private IEnumerator SendRawRequest(string method, string endpoint, object body,
            Action<string> onSuccess, Action<string> onError)
        {
            string url = $"{BaseUrl}{endpoint}";
            using (UnityWebRequest request = new UnityWebRequest(url, method))
            {
                if (body != null)
                {
                    string jsonBody = JsonUtility.ToJson(body);
                    request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonBody));
                }
                else
                {
                    request.uploadHandler = new UploadHandlerRaw(null);
                }

                request.downloadHandler = new DownloadHandlerBuffer();
                request.timeout = _requestTimeout;
                request.SetRequestHeader("Content-Type", "application/json");

                if (!string.IsNullOrEmpty(AuthToken))
                    request.SetRequestHeader("Authorization", $"Bearer {AuthToken}");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                    onSuccess?.Invoke(request.downloadHandler.text);
                else
                    onError?.Invoke(request.error);
            }
        }
    }
}
