using UnityEngine;
using UnityEngine.UI;
using TMPro;
using OriginXR.Core;

namespace OriginXR.Home
{
    /// <summary>
    /// 难度选择面板
    /// </summary>
    public class DifficultyPanel : MonoBehaviour
    {
        public Button closeButton;

        [System.Serializable]
        public class DifficultyConfig
        {
            public string name;          // "新手"
            public int questionCount;    // 该难度题目数
            public int minDiff;          // 题目最低难度 1~5
            public int maxDiff;          // 题目最高难度 1~5
            public int bossHP;           // BOSS 血量（=答对题数）
            public int playerHP;         // 玩家血量（答错上限）
            public int timeLimit;        // 每题限时
            public int expReward;
            public int goldReward;
        }

        [SerializeField] private string _battleSceneName = "BattleScene";

        // 五个难度
        private DifficultyConfig[] _difficulties = new DifficultyConfig[]
        {
            new DifficultyConfig { name="新手", questionCount=5,  minDiff=1, maxDiff=1, bossHP=5,  playerHP=5, timeLimit=10, expReward=100, goldReward=50 },
            new DifficultyConfig { name="简单", questionCount=8,  minDiff=1, maxDiff=2, bossHP=8,  playerHP=4, timeLimit=10, expReward=200, goldReward=80 },
            new DifficultyConfig { name="一般", questionCount=12, minDiff=2, maxDiff=3, bossHP=12, playerHP=3, timeLimit=10, expReward=350, goldReward=120 },
            new DifficultyConfig { name="困难", questionCount=15, minDiff=3, maxDiff=4, bossHP=15, playerHP=3, timeLimit=10, expReward=500, goldReward=180 },
            new DifficultyConfig { name="噩梦", questionCount=20, minDiff=4, maxDiff=5, bossHP=20, playerHP=2, timeLimit=10, expReward=800, goldReward=300 },
        };

        private void Start()
        {
            if (closeButton != null) closeButton.onClick.AddListener(Hide);
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        /// <summary>按钮点击：选择难度</summary>
        public void SelectDifficulty(int index)
        {
            if (index < 0 || index >= _difficulties.Length) return;

            var diff = _difficulties[index];

            // 通过 PlayerPrefs 传递难度参数
            PlayerPrefs.SetInt("Diff_QuestionCount", diff.questionCount);
            PlayerPrefs.SetInt("Diff_MinDifficulty", diff.minDiff);
            PlayerPrefs.SetInt("Diff_MaxDifficulty", diff.maxDiff);
            PlayerPrefs.SetInt("Diff_BossHP", diff.bossHP);
            PlayerPrefs.SetInt("Diff_PlayerHP", diff.playerHP);
            PlayerPrefs.SetInt("Diff_TimeLimit", diff.timeLimit);
            PlayerPrefs.SetInt("Diff_ExpReward", diff.expReward);
            PlayerPrefs.SetInt("Diff_GoldReward", diff.goldReward);
            PlayerPrefs.SetString("Diff_Name", diff.name);
            PlayerPrefs.Save();

            Debug.Log($"[DifficultyPanel] 选择难度: {diff.name}, {diff.questionCount}题, BossHP={diff.bossHP}, PlayerHP={diff.playerHP}");

            SceneLoader.Instance?.LoadScene(_battleSceneName);
            Hide();
        }
    }
}
