using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Text;

namespace OriginXR.Core
{
    /// <summary>
    /// HTTP 请求管理器（与 ApiClient 共存，处理需要 Token 的请求）
    /// </summary>
    public class HttpManager : MonoBehaviour
    {
        public static HttpManager Instance { get; private set; }

        public string BaseUrl { get; private set; } = "http://10.19.89.160:3002/api/v1";
        public string AuthToken { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }

        public void Initialize()
        {
            Debug.Log($"[HttpManager] 已初始化, BaseUrl={BaseUrl}");
        }

        public void SetBaseUrl(string url) { BaseUrl = url.TrimEnd('/'); }

        // === REST 方法 ===

        public void Get<T>(string endpoint, Action<T> onSuccess, Action<string> onError = null)
        {
            StartCoroutine(SendRequest<T>(UnityWebRequest.kHttpVerbGET, endpoint, null, onSuccess, onError));
        }

        public void Post<T>(string endpoint, object body, Action<T> onSuccess, Action<string> onError = null)
        {
            StartCoroutine(SendRequest<T>(UnityWebRequest.kHttpVerbPOST, endpoint, body, onSuccess, onError));
        }

        public void Put<T>(string endpoint, object body, Action<T> onSuccess, Action<string> onError = null)
        {
            StartCoroutine(SendRequest<T>(UnityWebRequest.kHttpVerbPUT, endpoint, body, onSuccess, onError));
        }

        public void Delete<T>(string endpoint, Action<T> onSuccess, Action<string> onError = null)
        {
            StartCoroutine(SendRequest<T>(UnityWebRequest.kHttpVerbDELETE, endpoint, null, onSuccess, onError));
        }

        private IEnumerator SendRequest<T>(string method, string endpoint, object body, Action<T> onSuccess, Action<string> onError)
        {
            string url = $"{BaseUrl}{endpoint}";
            using (var req = new UnityWebRequest(url, method))
            {
                if (body != null)
                {
                    string jsonBody = JsonUtility.ToJson(body);
                    req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonBody));
                }
                req.downloadHandler = new DownloadHandlerBuffer();
                req.timeout = 30;
                req.SetRequestHeader("Content-Type", "application/json");

                if (!string.IsNullOrEmpty(AuthToken))
                    req.SetRequestHeader("Authorization", $"Bearer {AuthToken}");

                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    try { onSuccess?.Invoke(JsonUtility.FromJson<T>(req.downloadHandler.text)); }
                    catch (Exception e) { onError?.Invoke(e.Message); }
                }
                else onError?.Invoke(req.error);
            }
        }
    }
}
