using UnityEngine;
using TMPro;
using System;
using System.Collections;

namespace OriginXR.Battle
{
    public class TimerController : MonoBehaviour
    {
        public TextMeshProUGUI timerText;
        private float _remain, _limit;
        private bool _running, _paused;
        private Coroutine _routine;

        public event Action OnTimeUp;

        public void StartCountdown(float limit)
        {
            _limit = limit; _remain = limit; _running = true; _paused = false;
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(Run());
        }

        public void Pause() => _paused = true;
        public void Resume() => _paused = false;
        public void Stop() { _running = false; if (_routine != null) StopCoroutine(_routine); }

        private IEnumerator Run()
        {
            while (_remain > 0f && _running)
            {
                if (!_paused) { _remain -= Time.deltaTime; UpdateUI(); }
                yield return null;
            }
            _running = false; UpdateUI();
            OnTimeUp?.Invoke();
        }

        private void UpdateUI()
        {
            if (timerText == null) return;
            float r = Mathf.Max(0, _remain);
            timerText.text = $"⏱ {r:F1}s";
            timerText.color = r <= 3f ? Color.red : r <= 5f ? Color.yellow : Color.green;
        }
    }
}
