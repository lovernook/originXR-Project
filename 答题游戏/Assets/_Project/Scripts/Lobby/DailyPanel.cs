using UnityEngine;
using UnityEngine.UI;
using TMPro;
using OriginXR.Core;

namespace OriginXR.Home
{
    /// <summary>
    /// 每日挑战面板控制器
    /// </summary>
    public class DailyPanel : MonoBehaviour
    {
        [Header("UI")]
        public TextMeshProUGUI ruleText;
        public TextMeshProUGUI remainingText;
        public Button startButton;
        public Button closeButton;

        [SerializeField] private string _battleSceneName = "BattleScene";
        private int _remainingChallenges = 3;

        private void Start()
        {
            if (closeButton != null) closeButton.onClick.AddListener(Hide);
            if (startButton != null) startButton.onClick.AddListener(StartChallenge);
        }

        public void Show()
        {
            // 每日规则随机
            string[] rules = {
                "今日BUFF：全部答对双倍积分！",
                "今日BUFF：连击加成+50%",
                "今日BUFF：每题时间+3秒",
                "今日DEBUFF：答错扣双倍分",
                "今日BUFF：初始生命+1"
            };
            if (ruleText != null)
                ruleText.text = rules[Random.Range(0, rules.Length)];

            if (remainingText != null)
                remainingText.text = $"剩余挑战次数：{_remainingChallenges}/3";

            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void StartChallenge()
        {
            if (_remainingChallenges <= 0)
            {
                UI.ToastManager.Instance?.ShowWarning("今日挑战次数已用完！");
                return;
            }

            _remainingChallenges--;

            // 标记为每日挑战模式
            PlayerPrefs.SetInt("IsDailyMode", 1);
            PlayerPrefs.Save();

            SceneLoader.Instance?.LoadScene(_battleSceneName);
        }
    }
}
