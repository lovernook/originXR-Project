using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

namespace OriginXR.UI
{
    /// <summary>
    /// 成就面板
    /// 负责：
    /// 1. 展示玩家的成就列表（50+成就）
    /// 2. 成就分类Tab（全部 / 学习 / 战斗 / 社交 / 收集 / 隐藏）
    /// 3. 成就档位：青铜 / 白银 / 黄金 / 钻石
    /// 4. 已解锁成就显示完成详情（完成时间 / 称号）
    /// 5. 未解锁成就显示进度条 + 解锁条件提示
    /// 6. 成就奖励领取（未领取的红点提示）
    ///
    /// API 接口：
    ///   GET /api/v1/users/:id/achievements -> 获取成就列表
    ///   POST /api/v1/users/:id/achievements/claim -> 领取成就奖励
    ///
    /// 成就场景联动：
    ///   AchievementScene (3D 奖杯陈列室) 与此面板数据共享
    /// </summary>
    public class AchievementPanel : MonoBehaviour
    {
        // === UI 组件 ===
        [SerializeField] private Canvas _panelCanvas;
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private Button _closeButton;

        // === 统计摘要 ===
        [SerializeField] private TextMeshProUGUI _totalAchievementText;  // "已解锁 12/50"
        [SerializeField] private TextMeshProUGUI _completionRateText;    // "完成率 24%"
        [SerializeField] private Image _progressBar;                     // 总进度条

        // === 分类Tab ===
        [SerializeField] private Button _allTab;
        [SerializeField] private Button _studyTab;
        [SerializeField] private Button _battleTab;
        [SerializeField] private Button _socialTab;
        [SerializeField] private Button _collectTab;
        [SerializeField] private Button _hiddenTab;

        // === 成就列表 ===
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private RectTransform _contentRoot;
        [SerializeField] private GameObject _achievementItemPrefab;   // 成就条目预制体

        // === 成就详情弹窗 ===
        [SerializeField] private GameObject _detailPanel;
        [SerializeField] private Image _detailBadge;
        [SerializeField] private TextMeshProUGUI _detailTitle;
        [SerializeField] private TextMeshProUGUI _detailDescription;
        [SerializeField] private TextMeshProUGUI _detailProgress;     // "10/100"
        [SerializeField] private Image _detailProgressBar;
        [SerializeField] private Button _detailClaimButton;           // 领取奖励按钮
        [SerializeField] private TextMeshProUGUI _detailRewardText;   // 奖励描述

        // === 状态 ===
        private AchievementCategory _currentCategory = AchievementCategory.All;
        private List<AchievementData> _allAchievements;
        private List<AchievementData> _filteredAchievements;
        private AchievementData _selectedAchievement;
        private int _totalAchievements;
        private int _unlockedAchievements;

        [Serializable]
        public class AchievementData
        {
            public string Id;
            public string Title;
            public string Description;
            public string IconId;
            public AchievementCategory Category;
            public AchievementTier Tier;               // 青铜/白银/黄金/钻石
            public bool IsUnlocked;
            public bool IsClaimed;                     // 奖励是否已领取
            public int CurrentProgress;                // 当前进度
            public int TargetProgress;                 // 目标进度
            public string RewardDescription;           // 奖励描述（金币/钻石/称号/头像框）
            public long UnlockedAt;                    // 解锁时间戳
        }

        public enum AchievementCategory { All, Study, Battle, Social, Collect, Hidden }
        public enum AchievementTier { Bronze, Silver, Gold, Diamond }

        // === Unity 生命周期 ===
        private void OnEnable() { }
        private void OnDisable() { }

        // === 公共方法 ===

        public void Show() { }
        public void Hide() { }
        public void SwitchCategory(AchievementCategory category) { }
        public void RefreshAchievements() { }
        public void ShowDetail(AchievementData achievement) { }
        public void ClaimReward(string achievementId) { }

        // === 私有方法 ===
        private IEnumerator<Coroutine> FetchAchievements() { yield return null; }
        private void PopulateAchievementList(List<AchievementData> achievements) { }
        private void SetupAchievementItem(GameObject item, AchievementData data) { }
        private List<AchievementData> FilterByCategory(AchievementCategory category) { return null; }
        private void UpdateSummary() { }
        private Color GetTierColor(AchievementTier tier) { return Color.white; }
        private string GetTierName(AchievementTier tier) { return ""; }

        // === 事件 ===
        public event Action OnPanelClosed;
        public event Action<AchievementData> OnRewardClaimed;

        // === 按钮回调 ===
        private void OnCategoryTabClicked(int tabIndex) { }
        private void OnCloseClicked() { }
        private void OnDetailClaimClicked() { }
    }
}
