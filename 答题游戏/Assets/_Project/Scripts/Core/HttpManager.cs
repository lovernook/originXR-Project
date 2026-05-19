using UnityEngine;
using System;
using System.Collections;

namespace OriginXR.Core
{
    /// <summary>
    /// HTTP 请求管理器
    /// 负责：
    /// 1. 封装 UnityWebRequest 的 GET/POST/PUT/PATCH/DELETE 请求
    /// 2. 统一注入 JWT Token 鉴权头
    /// 3. 统一错误处理（网络错误、HTTP状态码错误、业务错误）
    /// 4. 自动重试机制（网络超时等临时错误）
    /// 5. 泛型 JSON 反序列化
    ///
    /// API 基础路径配置：
    ///   开发环境: http://localhost:3000/api/v1
    ///   生产环境: https://api.originxr.com/api/v1
    /// </summary>
    public class HttpManager : MonoBehaviour
    {
        // === 单例 ===
        public static HttpManager Instance { get; private set; }

        // === 属性 ===
        /// <summary>API 服务器基础 URL</summary>
        public string BaseUrl { get; private set; } = "http://localhost:3000/api/v1";

        /// <summary>JWT 认证 Token</summary>
        public string AuthToken { get; private set; }

        /// <summary>请求超时时间（秒）</summary>
        public int RequestTimeout { get; set; } = 30;

        /// <summary>最大自动重试次数</summary>
        public int MaxRetries { get; set; } = 2;

        // === Unity 生命周期 ===
        private void Awake() { }
        private void OnDestroy() { }

        // === 公共方法 ===

        /// <summary>设置 API 基础 URL</summary>
        public void SetBaseUrl(string url) { BaseUrl = url; }

        /// <summary>设置认证 Token，后续所有请求自动携带</summary>
        public void SetAuthToken(string token) { AuthToken = token; }

        /// <summary>清除认证 Token</summary>
        public void ClearAuthToken() { AuthToken = null; }

        /// <summary>GET 请求</summary>
        /// <typeparam name="T">响应数据模型类型</typeparam>
        /// <param name="endpoint">接口路径，如 "/users/profile"</param>
        /// <param name="onSuccess">成功回调</param>
        /// <param name="onError">错误回调，参数为错误信息</param>
        public IEnumerator Get<T>(string endpoint, Action<T> onSuccess, Action<string> onError) { yield return null; }

        /// <summary>POST 请求</summary>
        public IEnumerator Post<T>(string endpoint, object body, Action<T> onSuccess, Action<string> onError) { yield return null; }

        /// <summary>PUT 请求</summary>
        public IEnumerator Put<T>(string endpoint, object body, Action<T> onSuccess, Action<string> onError) { yield return null; }

        /// <summary>PATCH 请求</summary>
        public IEnumerator Patch<T>(string endpoint, object body, Action<T> onSuccess, Action<string> onError) { yield return null; }

        /// <summary>DELETE 请求</summary>
        public IEnumerator Delete<T>(string endpoint, Action<T> onSuccess, Action<string> onError) { yield return null; }

        /// <summary>文件上传</summary>
        /// <param name="endpoint">上传接口路径</param>
        /// <param name="fileData">文件二进制数据</param>
        /// <param name="fileName">文件名</param>
        public IEnumerator UploadFile<T>(string endpoint, byte[] fileData, string fileName, Action<T> onSuccess, Action<string> onError) { yield return null; }

        // === 私有方法 ===
        private IEnumerator SendRequest<T>(string method, string endpoint, object body, Action<T> onSuccess, Action<string> onError) { yield return null; }
        private void HandleHttpError(long statusCode, string responseBody, Action<string> onError) { }
    }
}
