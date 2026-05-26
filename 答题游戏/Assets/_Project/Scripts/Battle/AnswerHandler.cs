using UnityEngine;
using System;
using System.Collections;
using OriginXR.Data;

namespace OriginXR.Battle
{
    public class AnswerHandler : MonoBehaviour
    {
        public event Action<bool, string, string, int> OnResult;

        public void Submit(QuestionData q, string selected)
        {
            StartCoroutine(Judge(q, selected));
        }

        private IEnumerator Judge(QuestionData q, string selected)
        {
            yield return new WaitForSeconds(0.15f);
            string correct = q.devCorrectAnswer ?? "";
            bool ok = !string.IsNullOrEmpty(selected) && !string.IsNullOrEmpty(correct)
                && selected.Trim().Equals(correct.Trim(), StringComparison.OrdinalIgnoreCase);
            OnResult?.Invoke(ok, correct, q.explanation ?? "", ok ? 100 : 0);
        }
    }
}
