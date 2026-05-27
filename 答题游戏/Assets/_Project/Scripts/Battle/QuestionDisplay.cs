using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using OriginXR.Data;

namespace OriginXR.Battle
{
    public class QuestionDisplay : MonoBehaviour
    {
        public TextMeshProUGUI questionText;
        public Button[] optionButtons;
        public Image[] optionBg;
        public TextMeshProUGUI[] optionTexts;
        public TextMeshProUGUI resultText;
        public TextMeshProUGUI explanationText;

        private QuestionData _q;
        private bool _submitted;
        private string _answer;

        public event Action<string> OnAnswerSubmitted;

        public void Show(QuestionData question, int index, int total)
        {
            _q = question; _answer = ""; _submitted = false;
            if (questionText != null) questionText.text = $"Q{index}/{total}  {question.content}";
            for (int i = 0; i < optionTexts?.Length && i < question.options.Count; i++)
                if (optionTexts[i] != null)
                    optionTexts[i].text = $"{question.options[i].GetKey()}. {question.options[i].content}";
            if (resultText != null) resultText.gameObject.SetActive(false);
            if (explanationText != null) explanationText.gameObject.SetActive(false);
            ResetBg(); SetBtns(true);
        }

        public void ShowResult(bool correct, string correctAns, string explanation)
        {
            if (resultText != null) { resultText.gameObject.SetActive(true); resultText.text = correct ? "✓ 正确!" : "✗ 错误!"; resultText.color = correct ? new Color(0.2f, 0.8f, 0.3f) : new Color(1f, 0.3f, 0.3f); }
            if (explanationText != null && !string.IsNullOrEmpty(explanation)) { explanationText.gameObject.SetActive(true); explanationText.text = explanation; }
            if (optionBg != null && _q != null)
                for (int i = 0; i < _q.options.Count && i < optionBg.Length; i++)
                    if (optionBg[i] != null && _q.options[i].GetKey() == correctAns)
                        optionBg[i].color = new Color(0.2f, 0.8f, 0.3f, 0.7f);
        }

        public void OnBtnClick(int index)
        {
            if (_submitted || _q == null || index >= _q.options.Count) return;
            _answer = _q.options[index].GetKey(); _submitted = true;
            if (optionBg != null) for (int i = 0; i < optionBg.Length; i++) if (optionBg[i] != null) optionBg[i].color = (i == index) ? new Color(0.27f, 0.53f, 1f, 0.7f) : Color.white;
            SetBtns(false);
            OnAnswerSubmitted?.Invoke(_answer);
        }

        public void BindButtons()
        {
            if (optionButtons == null) return;
            for (int i = 0; i < optionButtons.Length; i++)
            {
                if (optionButtons[i] == null) continue;
                optionButtons[i].onClick.RemoveAllListeners();
                int idx = i; optionButtons[i].onClick.AddListener(() => OnBtnClick(idx));
            }
        }

        private void ResetBg() { if (optionBg != null) foreach (var b in optionBg) if (b != null) b.color = Color.white; }
        private void SetBtns(bool en) { if (optionButtons != null) foreach (var b in optionButtons) if (b != null) b.interactable = en; }
    }
}
