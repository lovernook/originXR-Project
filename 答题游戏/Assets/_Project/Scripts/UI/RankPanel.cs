using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using OriginXR.Core;

namespace OriginXR.UI
{
    /// <summary>
    /// 排行榜面板
    /// 负责：
    /// 1. 展示多维度排行榜数据（全服 / 好友 / 公会）
    /// 2. 时间维度切换（日榜 / 周榜 / 赛季榜）
    /// 3. 显示排行条目：排名 / 头像 / 昵称 / 分数 / 等级
    /// 4. 自己的排名高亮显示 + 底部固定在当前位置
    /// 5. 支持无限滚动（VirtualListView 优化大批量数据）
    ///
    /// 排行榜维度：
    ///   全服榜  -> 按总积分排名（所有玩家）
    ///   好友榜  -> 仅好友中排名
    ///   公会榜  -> 公会成员中排名
    ///
    /// 时间维度：
    ///   日榜   -> 今日数据
    ///   周榜   -> 本周数据
    ///   赛季榜 -> 当前赛季数据
    ///
    /// API 接口：
    ///   GET /api/v1/rank/global?type=score&period=daily
    ///   GET /api/v1/rank/friends?period=weekly
    ///   GET /api/v1/rank/guild?period=season
    /// </summary>
    public class RankPanel : MonoBehaviour
    {
        // === UI 组件 ===
        [SerializeField] private Canvas _panelCanvas;
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private Button _closeButton;

        // === 维度Tab ===
        [SerializeField] private Button _globalTab;
        [SerializeField] private Button _friendsTab;
        [SerializeField] private Button _guildTab;

        // === 时间Tab ===
        [SerializeField] private Button _dailyTab;
        [SerializeField] private Button _weeklyTab;
        [SerializeField] private Button _seasonTab;

        // === 排行列表 ===
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private RectTransform _contentRoot;        // 列表内容根节点
        [SerializeField] private GameObject _rankItemPrefab;        // 排行条目预制体

        // === 自己的排名 ===
        [SerializeField] private GameObject _myRankPanel;           // 底部固定：我的排名
        [SerializeField] private TextMeshProUGUI _myRankText;
        [SerializeField] private TextMeshProUGUI _myScoreText;

        // === 状态 ===
        private RankDimension _currentDimension = RankDimension.Global;
        private RankPeriod _currentPeriod = RankPeriod.Daily;
        private List<RankItemData> _rankDataList;

        [Serializable]
        public class RankItemData
        {
            public int Rank;
            public string PlayerId;
            public string Username;
            public string AvatarId;
            public int Level;
            public long Score;
            public bool IsMe;
        }

        public enum RankDimension { Global, Friends, Guild }
        public enum RankPeriod { Daily, Weekly, Season }

        // === Unity 生命周期 ===
        private void OnEnable() { }
        private void OnDisable() { }

        // === 公共方法 ===

        /// <summary>打开排行榜面板</summary>
        public void Show() { }

        /// <summary>关闭排行榜面板</summary>
        public void Hide() { }

        /// <summary>切换排行维度</summary>
        public void SwitchDimension(RankDimension dimension) { }

        /// <summary>切换时间维度</summary>
        public void SwitchPeriod(RankPeriod period) { }

        /// <summary>刷新排行数据</summary>
        public void RefreshData() { }

        // === 私有方法 ===
        private IEnumerator<Coroutine> FetchRankData() { yield return null; }
        private void PopulateRankList(List<RankItemData> dataList) { }
        private void UpdateMyRankDisplay() { }
        private void HighlightTabButton(Button activeButton) { }
        private void SetupRankItem(GameObject item, RankItemData data, int index) { }

        // === 事件 ===
        public event Action OnPanelClosed;

        // === 按钮回调 ===
        private void OnGlobalTabClicked() { }
        private void OnFriendsTabClicked() { }
        private void OnGuildTabClicked() { }
        private void OnDailyTabClicked() { }
        private void OnWeeklyTabClicked() { }
        private void OnSeasonTabClicked() { }
        private void OnCloseClicked() { }
    }
}
