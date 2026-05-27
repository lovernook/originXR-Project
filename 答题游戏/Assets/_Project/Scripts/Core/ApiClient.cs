using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using OriginXR.Data;

public class ApiClient : MonoBehaviour
{
    public static ApiClient Instance { get; private set; }
    public string baseUrl = "http://10.19.89.160:3002/api/v1";
    private HttpClient _http;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        Debug.Log($"[ApiClient] HttpClient初始化, baseUrl={baseUrl}");
    }

    private void OnDestroy() { _http?.Dispose(); if (Instance == this) Instance = null; }

    private IEnumerator Send(string path, string method, object body, Action<string, long> cb)
    {
        string url = $"{baseUrl}{path}";
        Task<HttpResponseMessage> task = null;

        if (method == "GET")
            task = _http.GetAsync(url);
        else
        {
            string json = body != null ? JsonUtility.ToJson(body) : "{}";
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            if (method == "POST") task = _http.PostAsync(url, content);
            else if (method == "PUT") task = _http.PutAsync(url, content);
            else { var req = new HttpRequestMessage(HttpMethod.Delete, url); task = _http.SendAsync(req); }
        }

        while (!task.IsCompleted) yield return null;

        long code = 0;
        string resp = "";
        if (task.IsCompletedSuccessfully)
        {
            code = (long)task.Result.StatusCode;
            resp = task.Result.Content.ReadAsStringAsync().Result;
        }
        else
        {
            code = 502;
            resp = task.Exception?.InnerException?.Message ?? "请求失败";
        }

        Debug.Log($"[API] {method} {path} → {code}");
        cb?.Invoke(resp, code);
    }

    public IEnumerator Health(Action<bool> cb)
    {
        var task = _http.GetAsync($"{baseUrl}/health");
        while (!task.IsCompleted) yield return null;
        cb(task.IsCompletedSuccessfully && task.Result.IsSuccessStatusCode);
    }

    public IEnumerator GetAllQuestions(int page, int pageSize, Action<List<QuestionData>> cb)
    {
        yield return StartCoroutine(Send($"/admin/questions?page={page}&pageSize={pageSize}", "GET", null, (json, code) =>
        {
            try
            {
                // 后端返回 {"items":[...], "total":10, ...}
                var r = JsonUtility.FromJson<QWrap>(json);
                cb(r?.items ?? new List<QuestionData>());
            }
            catch (Exception e) { Debug.LogError($"[API] GetAllQuestions解析: {e.Message}"); cb(new List<QuestionData>()); }
        }));
    }

    public IEnumerator SmartPick(int count, int minDiff, int maxDiff, Action<List<QuestionData>> cb)
    {
        var body = new SmartPickReq { count = count, difficultyMin = minDiff, difficultyMax = maxDiff, knowledgePointIds = new string[0] };
        yield return StartCoroutine(Send("/admin/questions/smart-pick", "POST", body, (json, code) =>
        {
            try
            {
                if (json.TrimStart().StartsWith("[")) json = $"{{\"questions\":{json}}}";
                var r = JsonUtility.FromJson<QWrap>(json);
                cb(r?.questions ?? r?.items ?? new List<QuestionData>());
            }
            catch { cb(new List<QuestionData>()); }
        }));
    }

    public IEnumerator SubmitAnswer(string stageId, string questionId, string selectedOption, Action<bool, string, string, int> cb)
    {
        var body = new SubmitReq { questionId = questionId, selectedAnswer = selectedOption, timeSpent = 5f };
        Debug.Log($"[API] SubmitAnswer body: {JsonUtility.ToJson(body)} stageId={stageId}");
        yield return StartCoroutine(Send($"/game/stages/{stageId}/answer", "POST", body, (json, code) =>
        {
            Debug.Log($"[API] SubmitAnswer response: code={code} json={json}");
            try
            {
                if (string.IsNullOrEmpty(json) || code >= 400)
                {
                    cb(false, "", "判定失败", 0);
                    return;
                }
                var r = JsonUtility.FromJson<AnswerResultData>(json);
                cb(r.isCorrect, r.correctAnswer, r.explanation, r.scoreGained);
            }
            catch (Exception e) { Debug.LogError($"[API] SubmitAnswer解析: {e.Message}"); cb(false, "", "异常", 0); }
        }));
    }

    public IEnumerator GetStages(Action<List<StageData>> cb)
    {
        yield return StartCoroutine(Send("/game/stages", "GET", null, (json, code) =>
        {
            try { cb(JsonUtility.FromJson<SWrap>($"{{\"items\":{json}}}")?.items ?? new List<StageData>()); }
            catch { cb(new List<StageData>()); }
        }));
    }

    public IEnumerator StartStage(string stageId, Action<bool> cb)
    {
        yield return StartCoroutine(Send($"/game/stages/{stageId}/start", "POST", null, (json, code) =>
        {
            Debug.Log($"[API] StartStage → {code}");
            cb(code < 400);
        }));
    }

    [Serializable] class QWrap { public List<QuestionData> items; public List<QuestionData> questions; }
    [Serializable] class SWrap { public List<StageData> items; }
    [Serializable] class SmartPickReq { public int count; public int difficultyMin; public int difficultyMax; public string[] knowledgePointIds; }
    [Serializable] class SubmitReq { public string questionId; public string selectedAnswer; public float timeSpent; }
}
