using UnityEngine;
using UnityEngine.UI;
using TMPro;
using OriginXR.Core;

namespace OriginXR.Home
{
    public class DifficultyPanel : MonoBehaviour
    {
        public Button closeButton;

        [System.Serializable]
        public class DifficultyConfig
        {
            public string name; public int questionCount; public int minDiff; public int maxDiff;
            public int bossHP; public int playerHP; public int timeLimit;
            public int expReward; public int goldReward;
            public int goldCost;     // 入场费
            public bool costsDiamond; // true=花钻石, false=花金币
        }

        [SerializeField] private string _battleSceneName = "BattleScene";

        private DifficultyConfig[] _difficulties = new DifficultyConfig[]
        {
            new DifficultyConfig { name="新手", questionCount=5,  minDiff=1, maxDiff=1, bossHP=5,  playerHP=5, timeLimit=10, expReward=100, goldReward=50,  goldCost=0,   costsDiamond=false },
            new DifficultyConfig { name="简单", questionCount=8,  minDiff=1, maxDiff=2, bossHP=8,  playerHP=4, timeLimit=10, expReward=200, goldReward=80,  goldCost=50,  costsDiamond=false },
            new DifficultyConfig { name="一般", questionCount=12, minDiff=2, maxDiff=3, bossHP=12, playerHP=3, timeLimit=10, expReward=350, goldReward=120, goldCost=100, costsDiamond=false },
            new DifficultyConfig { name="困难", questionCount=15, minDiff=3, maxDiff=4, bossHP=15, playerHP=3, timeLimit=10, expReward=500, goldReward=180, goldCost=200, costsDiamond=false },
            new DifficultyConfig { name="噩梦", questionCount=20, minDiff=4, maxDiff=5, bossHP=20, playerHP=2, timeLimit=10, expReward=800, goldReward=300, goldCost=10,  costsDiamond=true },
        };

        private void Start()
        {
            if (closeButton != null) closeButton.onClick.AddListener(Hide);
        }

        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);

        public void SelectDifficulty(int index)
        {
            if (index < 0 || index >= _difficulties.Length) return;
            var diff = _difficulties[index];

            // 扣钱
            if (diff.goldCost > 0)
            {
                if (diff.costsDiamond)
                {
                    if (!Data.CurrencyManager.SpendDiamond(diff.goldCost))
                    {
                        UI.ToastManager.Instance?.ShowWarning($"💎不足! 需要{diff.goldCost}");
                        return;
                    }
                }
                else
                {
                    if (!Data.CurrencyManager.SpendGold(diff.goldCost))
                    {
                        UI.ToastManager.Instance?.ShowWarning($"💰不足! 需要{diff.goldCost}");
                        return;
                    }
                }
            }

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

            SceneLoader.Instance?.LoadScene(_battleSceneName);
            Hide();
        }
    }
}
