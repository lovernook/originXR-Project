using UnityEngine;

namespace OriginXR.Battle
{
    /// <summary>
    /// 开发阶段测试脚本 — 从 PlayerPrefs 读取难度参数启动战斗
    /// </summary>
    public class TestBattleStarter : MonoBehaviour
    {
        private void Start()
        {
            Invoke(nameof(StartTestBattle), 0.5f);
        }

        private void StartTestBattle()
        {
            var battleMgr = BattleManager.Instance;
            if (battleMgr == null) { Debug.LogError("[Starter] BattleManager 未找到！"); return; }

            bool isDailyMode = PlayerPrefs.GetInt("IsDailyMode", 0) == 1;
            PlayerPrefs.SetInt("IsDailyMode", 0);
            PlayerPrefs.Save();

            OriginXR.Data.StageData stageData;

            if (isDailyMode)
            {
                stageData = new OriginXR.Data.StageData
                {
                    id = "daily", name = "每日挑战", bossName = "今日BOSS",
                    bossHP = 10, questionCount = 10, timePerQuestion = 10,
                    rewardExp = 200, rewardGold = 100
                };
            }
            else
            {
                // 从 PlayerPrefs 读取难度参数
                int qCount   = PlayerPrefs.GetInt("Diff_QuestionCount", 8);
                int minDiff  = PlayerPrefs.GetInt("Diff_MinDifficulty", 1);
                int maxDiff  = PlayerPrefs.GetInt("Diff_MaxDifficulty", 3);
                int bossHP   = PlayerPrefs.GetInt("Diff_BossHP", qCount);
                int playerHP = PlayerPrefs.GetInt("Diff_PlayerHP", 3);
                int timeLmt  = PlayerPrefs.GetInt("Diff_TimeLimit", 10);
                int expR     = PlayerPrefs.GetInt("Diff_ExpReward", 200);
                int goldR    = PlayerPrefs.GetInt("Diff_GoldReward", 80);
                string name  = PlayerPrefs.GetString("Diff_Name", "普通");

                stageData = new OriginXR.Data.StageData
                {
                    id = $"diff_{name}",
                    name = $"{name}难度",
                    bossName = $"恶龙 Lv.{name}",
                    bossHP = bossHP,
                    questionCount = qCount,
                    timePerQuestion = timeLmt,
                    rewardExp = expR,
                    rewardGold = goldR
                };
            }

            battleMgr.StartPVEBattle(stageData);
            Destroy(this);
        }
    }
}
