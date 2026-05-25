using UnityEngine;

namespace OriginXR.Battle
{
    /// <summary>
    /// 开发阶段测试脚本
    /// 挂到 BattleSystem 上，场景启动后自动开始一场战斗
    /// 正式版本删除此脚本
    /// </summary>
    public class TestBattleStarter : MonoBehaviour
    {
        [Header("测试用关卡数据")]
        [SerializeField] private string _testStageId = "test_stage_001";
        [SerializeField] private string _testStageName = "第一章：编程入门";
        [SerializeField] private string _testBossName = "初级BUG怪";
        [SerializeField] private int _testBossHP = 1000;
        [SerializeField] private int _testQuestionCount = 5;

        private void Start()
        {
            Debug.Log("=== TestBattleStarter Start 执行 ===");
            // 延迟0.5秒确保所有管理器初始化完成
            Invoke(nameof(StartTestBattle), 0.5f);
        }

        private void StartTestBattle()
        {
            var battleMgr = BattleManager.Instance;
            if (battleMgr == null)
            {
                Debug.LogError("[TestBattleStarter] BattleManager 未找到！");
                return;
            }

            bool isDailyMode = PlayerPrefs.GetInt("IsDailyMode", 0) == 1;
            PlayerPrefs.SetInt("IsDailyMode", 0);  // 用完重置
            PlayerPrefs.Save();

            OriginXR.Data.StageData stageData;

            if (isDailyMode)
            {
                stageData = new OriginXR.Data.StageData
                {
                    id = "daily_challenge",
                    name = "每日挑战",
                    bossName = "今日BOSS",
                    bossHP = 800,
                    questionCount = 10,
                    timePerQuestion = 10,
                    rewardExp = 200,
                    rewardGold = 100
                };
                Debug.Log("[TestBattleStarter] 开始每日挑战模式");
            }
            else
            {
                int stageId = PlayerPrefs.GetInt("CurrentStageId", 1);
                string[] stageNames = {
                    "变量入门", "数据类型", "条件判断", "循环结构",
                    "数组基础", "函数入门", "面向对象", "继承多态",
                    "接口抽象", "异常处理", "泛型集合", "文件操作"
                };
                string stageName = stageId <= stageNames.Length ? stageNames[stageId - 1] : $"第{stageId}关";

                stageData = new OriginXR.Data.StageData
                {
                    id = $"stage_{stageId:D3}",
                    name = $"第{stageId}关 · {stageName}",
                    bossName = $"知识守卫 Lv.{stageId}",
                    bossHP = 500 + stageId * 250,
                    questionCount = 3 + stageId,
                    timePerQuestion = 10,
                    rewardExp = 50 + stageId * 25,
                    rewardGold = 30 + stageId * 10
                };
                Debug.Log($"[TestBattleStarter] 开始关卡: {stageData.name}");
            }

            battleMgr.StartPVEBattle(stageData);
            Destroy(this);
        }
    }
}
