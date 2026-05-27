using UnityEngine;
using System;
using System.Collections;

namespace OriginXR.Battle
{
    public class AnswerHandler : MonoBehaviour
    {
        public event Action<bool, string, string, int> OnResult;

        public void Submit(Data.QuestionData q, string selected, string stageId = "")
        {
            StartCoroutine(Judge(q, selected, stageId));
        }

        private IEnumerator Judge(Data.QuestionData q, string selected, string stageId)
        {
            // 优先 API 判定
            var api = ApiClient.Instance;
            if (api != null && !string.IsNullOrEmpty(stageId))
            {
                bool done = false;
                bool apiOk = false;
                string apiAns = "";
                string apiExp = "";
                int apiScore = 0;

                StartCoroutine(api.SubmitAnswer(stageId, q.id, selected,
                    (correct, ans, exp, score) =>
                    { apiOk = correct; apiAns = ans; apiExp = exp; apiScore = score; done = true; }));

                float t = 0;
                while (!done && t < 3f) { t += Time.deltaTime; yield return null; }

                if (done && !string.IsNullOrEmpty(apiAns))
                {
                    Debug.Log($"[Judge] API: {(apiOk ? "✓" : "✗")} 正解={apiAns}");
                    OnResult?.Invoke(apiOk, apiAns, apiExp, apiScore);
                    yield break;
                }
                Debug.Log("[Judge] API无结果，降级本地");
            }

            // 本地判定
            yield return new WaitForSeconds(0.1f);
            string correct = q.devCorrectAnswer ?? "";
            if (string.IsNullOrEmpty(correct))
            {
                Debug.LogWarning("[Judge] 无本地答案, API未连, 跳过本题");
                OnResult?.Invoke(true, "", "API未连接", 0); // = 不扣血不给分
                yield break;
            }
            bool ok = selected.Trim().Equals(correct.Trim(), StringComparison.OrdinalIgnoreCase);
            Debug.Log($"[Judge] 本地: {(ok ? "✓" : "✗")} 选={selected} 正解={correct}");
            OnResult?.Invoke(ok, correct, q.explanation ?? "", ok ? 100 : 0);
        }
    }
}
