using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using OriginXR.Core;

namespace OriginXR.Home
{
    /// <summary>
    /// 关卡选择面板控制器
    /// 显示关卡列表，点击跳转到 BattleScene 并传入关卡数据
    /// </summary>
    public class StageSelectPanel : MonoBehaviour
    {
        [Header("UI 引用")]
        public GameObject stageItemPrefab;       // 关卡条目预制体
        public Transform contentRoot;             // ScrollView 的 Content
        public Button closeButton;

        [Header("关卡数量")]
        [SerializeField] private string _battleSceneName = "BattleScene";

        // === 模拟关卡数据（后续从服务端获取）===
        private List<StageInfo> _stages = new List<StageInfo>();

        [System.Serializable]
        public class StageInfo
        {
            public int id;
            public string name;
            public int difficulty;     // 1~5
            public int bestStars;      // 0~3
            public bool unlocked;
        }

        private void Start()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(Hide);

            CreateMockData();
            PopulateStageList();
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void CreateMockData()
        {
            _stages = new List<StageInfo>();
            string[] names = {
                "变量入门", "数据类型", "条件判断", "循环结构",
                "数组基础", "函数入门", "面向对象", "继承多态",
                "接口抽象", "异常处理", "泛型集合", "文件操作"
            };

            for (int i = 0; i < 12; i++)
            {
                _stages.Add(new StageInfo
                {
                    id = i + 1,
                    name = names[i],
                    difficulty = Mathf.Min(i / 3 + 1, 5),
                    bestStars = i == 0 ? 3 : (i < 3 ? Random.Range(0, 3) : 0),
                    unlocked = i == 0 || (i < 6)
                });
            }
        }

        private void PopulateStageList()
        {
            if (contentRoot == null || stageItemPrefab == null) return;

            // 清除旧条目
            foreach (Transform child in contentRoot)
                Destroy(child.gameObject);

            foreach (var stage in _stages)
            {
                GameObject item = Instantiate(stageItemPrefab, contentRoot);
                SetupStageItem(item, stage);
            }
        }

        private void SetupStageItem(GameObject item, StageInfo stage)
        {
            // 文字
            string starsStr = new string('★', stage.bestStars) + new string('☆', 3 - stage.bestStars);
            string lockStr = stage.unlocked ? "" : " 🔒";
            string difficultyStr = new string('⭐', stage.difficulty);

            var label = item.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
                label.text = $"第{stage.id}关 · {stage.name} · {starsStr} {lockStr}";

            // 未解锁变灰
            if (!stage.unlocked)
            {
                var img = item.GetComponent<Image>();
                if (img != null) img.color = new Color(0.3f, 0.3f, 0.3f, 1f);
            }

            // 点击事件
            var btn = item.GetComponent<Button>();
            if (btn != null)
            {
                int stageId = stage.id;
                btn.interactable = stage.unlocked;
                btn.onClick.AddListener(() => StartStage(stageId));
            }
        }

        private void StartStage(int stageId)
        {
            Debug.Log($"[StageSelect] 选择关卡 {stageId}，正在进入战斗...");

            // 将关卡ID存到 PlayerPrefs，BattleScene 启动时读取
            PlayerPrefs.SetInt("CurrentStageId", stageId);
            PlayerPrefs.Save();

            SceneLoader.Instance?.LoadScene(_battleSceneName);
        }
    }
}
