using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;

namespace OriginXR.UI
{
    /// <summary>
    /// 排行榜面板
    /// 负责：
    /// 1. 多维度排行（全服/好友/公会）和时间维度（日/周/赛季）切换
    /// 2. 排行条目列表（排名/头像/昵称/分数/等级）
    /// 3. 自己的排名固定在底部高亮显示
    /// 4. 数据从 API 拉取
    /// </summary>
    public class RankPanel : MonoBehaviour
    {
        [Header("主面板")]
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private Button _closeButton;

        [Header("维度Tab")]
        [SerializeField] private Button _globalTab;
        [SerializeField] private Button _friendsTab;
        [SerializeField] private Button _guildTab;

        [Header("时间Tab")]
        [SerializeField] private Button _dailyTab;
        [SerializeField] private Button _weeklyTab;
        [SerializeField] private Button _seasonTab;

        [Header("排行列表")]
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private RectTransform _contentRoot;
        [SerializeField] private GameObject _rankItemPrefab;

        [Header("我的排名")]
        [SerializeField] private GameObject _myRankPanel;
        [SerializeField] private TextMeshProUGUI _myRankText;
        [SerializeField] private TextMeshProUGUI _myScoreText;
        [SerializeField] private TextMeshProUGUI _myUsernameText;

        // === 状态 ===
        private RankDimension _currentDimension = RankDimension.Global;
        private RankPeriod _currentPeriod = RankPeriod.Daily;
        private List<RankItemData> _rankData = new List<RankItemData>();

        [Serializable]
        public class RankItemData
        {
            public int rank;
            public string playerId;
            public string username;
            public string avatarId;
            public int level;
            public long score;
            public bool isMe;
        }

        public enum RankDimension { Global, Friends, Guild }
        public enum RankPeriod { Daily, Weekly, Season }

        // === Unity 生命周期 ===

        private void Awake()
        {
            if (_closeButton != null) _closeButton.onClick.AddListener(Hide);

            if (_globalTab != null) _globalTab.onClick.AddListener(() => SwitchDimension(RankDimension.Global));
            if (_friendsTab != null) _friendsTab.onClick.AddListener(() => SwitchDimension(RankDimension.Friends));
            if (_guildTab != null) _guildTab.onClick.AddListener(() => SwitchDimension(RankDimension.Guild));

            if (_dailyTab != null) _dailyTab.onClick.AddListener(() => SwitchPeriod(RankPeriod.Daily));
            if (_weeklyTab != null) _weeklyTab.onClick.AddListener(() => SwitchPeriod(RankPeriod.Weekly));
            if (_seasonTab != null) _seasonTab.onClick.AddListener(() => SwitchPeriod(RankPeriod.Season));
        }

        private void Start()
        {
            if (_panelRoot != null)
                _panelRoot.SetActive(false);
        }

        // === 公共方法 ===

        public void Show()
        {
            if (_panelRoot != null) _panelRoot.SetActive(true);
            RefreshData();
        }

        public void Hide()
        {
            if (_panelRoot != null) _panelRoot.SetActive(false);
        }

        public void SwitchDimension(RankDimension dimension)
        {
            _currentDimension = dimension;
            HighlightTab(new Button[] { _globalTab, _friendsTab, _guildTab }, (int)dimension);
            RefreshData();
        }

        public void SwitchPeriod(RankPeriod period)
        {
            _currentPeriod = period;
            HighlightTab(new Button[] { _dailyTab, _weeklyTab, _seasonTab }, (int)period);
            RefreshData();
        }

        public void RefreshData()
        {
            StartCoroutine(FetchRankData());
        }

        // === 私有方法 ===

        private IEnumerator FetchRankData()
        {
            // TODO: 请求 API GET /api/v1/rank/{dimension}?period={period}
            // 开发阶段使用模拟数据
            yield return new WaitForSeconds(0.3f);

            _rankData = CreateMockData();
            PopulateRankList();
            UpdateMyRankDisplay();
        }

        private void PopulateRankList()
        {
            if (_contentRoot == null || _rankItemPrefab == null) return;

            // 清除旧条目
            foreach (Transform child in _contentRoot)
                Destroy(child.gameObject);

            // 创建新条目
            for (int i = 0; i < _rankData.Count; i++)
            {
                GameObject item = Instantiate(_rankItemPrefab, _contentRoot);
                SetupRankItem(item, _rankData[i]);
            }
        }

        private void SetupRankItem(GameObject item, RankItemData data)
        {
            var texts = item.GetComponentsInChildren<TextMeshProUGUI>();
            foreach (var text in texts)
            {
                if (text.name.Contains("Rank")) text.text = data.rank <= 3 ? $"#{data.rank}" : $"{data.rank}";
                else if (text.name.Contains("Name")) text.text = data.username;
                else if (text.name.Contains("Score")) text.text = data.score.ToString("N0");
                else if (text.name.Contains("Level")) text.text = $"Lv.{data.level}";
            }

            // 高亮自己的条目
            if (data.isMe)
            {
                var bg = item.GetComponent<Image>();
                if (bg != null) bg.color = new Color(1f, 1f, 0.2f, 0.3f);
            }
        }

        private void UpdateMyRankDisplay()
        {
            var myData = _rankData.Find(r => r.isMe);
            if (myData == null) return;

            if (_myRankText != null) _myRankText.text = $"#{myData.rank}";
            if (_myScoreText != null) _myScoreText.text = myData.score.ToString("N0");
            if (_myUsernameText != null) _myUsernameText.text = myData.username;

            if (_myRankPanel != null) _myRankPanel.SetActive(true);
        }

        private List<RankItemData> CreateMockData()
        {
            var data = new List<RankItemData>();
            string[] names = { "学霸小明", "星空行者", "知识猎人", "学习者一号", "答题达人", "星际学者", "逻辑大师", "智慧女神", "百科王", "快枪手" };
            for (int i = 0; i < Mathf.Min(names.Length, 10); i++)
            {
                data.Add(new RankItemData
                {
                    rank = i + 1,
                    playerId = $"player_{i}",
                    username = names[i],
                    level = UnityEngine.Random.Range(5, 50),
                    score = UnityEngine.Random.Range(1000, 50000),
                    isMe = i == 2
                });
            }
            return data;
        }

        private void HighlightTab(Button[] tabs, int activeIndex)
        {
            for (int i = 0; i < tabs.Length; i++)
            {
                if (tabs[i] == null) continue;
                var colors = tabs[i].colors;
                colors.normalColor = i == activeIndex ? new Color(1f, 1f, 1f, 1f) : new Color(0.7f, 0.7f, 0.7f, 1f);
                tabs[i].colors = colors;
            }
        }
    }
}
