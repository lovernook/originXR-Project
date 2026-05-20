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
    /// 1. 展示成就列表（50+ 成就，分青铜/白银/黄金/钻石四档）
    /// 2. 分类Tab + 已解锁/未解锁状态展示
    /// 3. 成就进度条与解锁条件提示
    /// 4. 成就奖励领取
    /// </summary>
    public class AchievementPanel : MonoBehaviour
    {
        [Header("主面板")]
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private Button _closeButton;
        [SerializeField] private TextMeshProUGUI _completionSummary;  // "已解锁 12/50"
        [SerializeField] private Image _completionBar;

        [Header("分类Tab")]
        [SerializeField] private Button _allTab;
        [SerializeField] private Button _studyTab;
        [SerializeField] private Button _battleTab;
        [SerializeField] private Button _socialTab;
        [SerializeField] private Button _collectTab;

        [Header("成就列表")]
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private RectTransform _contentRoot;
        [SerializeField] private GameObject _achievementItemPrefab;

        [Header("详情弹窗")]
        [SerializeField] private GameObject _detailPanel;
        [SerializeField] private TextMeshProUGUI _detailTitle;
        [SerializeField] private TextMeshProUGUI _detailDescription;
        [SerializeField] private TextMeshProUGUI _detailProgress;
        [SerializeField] private Image _detailProgressBar;
        [SerializeField] private Button _detailClaimButton;

        // === 状态 ===
        private AchievementCategory _currentCategory = AchievementCategory.All;
        private List<AchievementData> _allAchievements = new List<AchievementData>();
        private AchievementData _selectedAchievement;

        [Serializable]
        public class AchievementData
        {
            public string id;
            public string title;
            public string description;
            public string iconId;
            public AchievementCategory category;
            public AchievementTier tier;         // 青铜/白银/黄金/钻石
            public bool isUnlocked;
            public bool isClaimed;
            public int currentProgress;
            public int targetProgress;
            public string rewardDescription;
            public long unlockedAt;
        }

        public enum AchievementCategory { All, Study, Battle, Social, Collect }
        public enum AchievementTier { Bronze, Silver, Gold, Diamond }

        // === Unity 生命周期 ===

        private void Awake()
        {
            if (_closeButton != null) _closeButton.onClick.AddListener(Hide);
            if (_allTab != null) _allTab.onClick.AddListener(() => SwitchCategory(AchievementCategory.All));
            if (_studyTab != null) _studyTab.onClick.AddListener(() => SwitchCategory(AchievementCategory.Study));
            if (_battleTab != null) _battleTab.onClick.AddListener(() => SwitchCategory(AchievementCategory.Battle));
            if (_socialTab != null) _socialTab.onClick.AddListener(() => SwitchCategory(AchievementCategory.Social));
            if (_collectTab != null) _collectTab.onClick.AddListener(() => SwitchCategory(AchievementCategory.Collect));
            if (_detailClaimButton != null) _detailClaimButton.onClick.AddListener(ClaimReward);

            if (_detailPanel != null) _detailPanel.SetActive(false);
        }

        private void Start()
        {
            if (_panelRoot != null) _panelRoot.SetActive(false);
        }

        // === 公共方法 ===

        public void Show()
        {
            if (_panelRoot != null) _panelRoot.SetActive(true);
            RefreshAchievements();
        }

        public void Hide()
        {
            if (_panelRoot != null) _panelRoot.SetActive(false);
        }

        public void SwitchCategory(AchievementCategory category)
        {
            _currentCategory = category;
            PopulateList();
        }

        public void RefreshAchievements()
        {
            StartCoroutine(FetchAchievements());
        }

        public void ClaimReward()
        {
            if (_selectedAchievement == null || !_selectedAchievement.isUnlocked || _selectedAchievement.isClaimed) return;

            // TODO: POST /api/v1/users/:id/achievements/claim
            _selectedAchievement.isClaimed = true;
            if (_detailClaimButton != null) _detailClaimButton.interactable = false;
            ToastManager.Instance?.ShowSuccess($"领取了成就奖励: {_selectedAchievement.rewardDescription}");
        }

        // === 私有方法 ===

        private System.Collections.IEnumerator FetchAchievements()
        {
            // TODO: GET /api/v1/users/:id/achievements
            yield return new WaitForSeconds(0.2f);
            _allAchievements = CreateMockData();
            UpdateSummary();
            PopulateList();
        }

        private void UpdateSummary()
        {
            int unlocked = 0;
            foreach (var a in _allAchievements)
                if (a.isUnlocked) unlocked++;

            if (_completionSummary != null)
                _completionSummary.text = $"已解锁 {unlocked}/{_allAchievements.Count}";

            if (_completionBar != null)
                _completionBar.fillAmount = (float)unlocked / Mathf.Max(_allAchievements.Count, 1);
        }

        private void PopulateList()
        {
            if (_contentRoot == null || _achievementItemPrefab == null) return;

            foreach (Transform child in _contentRoot)
                Destroy(child.gameObject);

            var filtered = FilterByCategory(_allAchievements, _currentCategory);
            foreach (var achievement in filtered)
            {
                GameObject item = Instantiate(_achievementItemPrefab, _contentRoot);
                SetupAchievementItem(item, achievement);
            }
        }

        private void SetupAchievementItem(GameObject item, AchievementData data)
        {
            var texts = item.GetComponentsInChildren<TextMeshProUGUI>();
            foreach (var t in texts)
            {
                if (t.name.Contains("Title")) t.text = data.title;
                else if (t.name.Contains("Progress")) t.text = data.isUnlocked ? "✓ 已完成" : $"{data.currentProgress}/{data.targetProgress}";
                else if (t.name.Contains("Tier")) t.text = GetTierLabel(data.tier);
            }

            // 已解锁 vs 未解锁视觉效果
            var bg = item.GetComponent<Image>();
            if (bg != null)
                bg.color = data.isUnlocked ? new Color(0.3f, 0.6f, 0.3f, 0.5f) : new Color(0.3f, 0.3f, 0.3f, 0.5f);

            var btn = item.GetComponent<Button>();
            if (btn != null) btn.onClick.AddListener(() => ShowDetail(data));
        }

        private void ShowDetail(AchievementData data)
        {
            _selectedAchievement = data;
            if (_detailPanel == null) return;

            _detailPanel.SetActive(true);
            if (_detailTitle != null) _detailTitle.text = data.title;
            if (_detailDescription != null) _detailDescription.text = data.description;
            if (_detailProgress != null) _detailProgress.text = $"{data.currentProgress}/{data.targetProgress}";
            if (_detailProgressBar != null) _detailProgressBar.fillAmount = (float)data.currentProgress / Mathf.Max(data.targetProgress, 1);
            if (_detailClaimButton != null) _detailClaimButton.interactable = data.isUnlocked && !data.isClaimed;
        }

        private List<AchievementData> FilterByCategory(List<AchievementData> list, AchievementCategory cat)
        {
            if (cat == AchievementCategory.All) return list;
            var result = new List<AchievementData>();
            foreach (var a in list)
                if (a.category == cat) result.Add(a);
            return result;
        }

        private string GetTierLabel(AchievementTier tier)
        {
            return tier switch
            {
                AchievementTier.Bronze => "🥉 青铜",
                AchievementTier.Silver => "🥈 白银",
                AchievementTier.Gold => "🥇 黄金",
                AchievementTier.Diamond => "💎 钻石",
                _ => ""
            };
        }

        private List<AchievementData> CreateMockData()
        {
            return new List<AchievementData>
            {
                new AchievementData { id="a1", title="初次答题", description="完成第一次答题", category=AchievementCategory.Study, tier=AchievementTier.Bronze, isUnlocked=true, isClaimed=true, currentProgress=1, targetProgress=1, rewardDescription="金币×100" },
                new AchievementData { id="a2", title="百题斩", description="累计答对100题", category=AchievementCategory.Study, tier=AchievementTier.Silver, isUnlocked=true, isClaimed=false, currentProgress=100, targetProgress=100, rewardDescription="钻石×10" },
                new AchievementData { id="a3", title="知识渊博", description="掌握50个知识点", category=AchievementCategory.Study, tier=AchievementTier.Gold, isUnlocked=false, currentProgress=32, targetProgress=50, rewardDescription="称号: 知识渊博" },
                new AchievementData { id="a4", title="连胜之王", description="PVP取得10连胜", category=AchievementCategory.Battle, tier=AchievementTier.Diamond, isUnlocked=false, currentProgress=3, targetProgress=10, rewardDescription="限定头像框" },
            };
        }
    }
}
