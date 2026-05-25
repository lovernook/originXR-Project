using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace OriginXR.Battle
{
    /// <summary>
    /// BattleScene UI 绑定器
    /// </summary>
    public class BattleSceneSetup : MonoBehaviour
    {
        [Header("题目 UI")]
        public TextMeshProUGUI questionText;
        public Button[] optionButtons;
        public Image[] optionBackgrounds;
        public TextMeshProUGUI[] optionTexts;

        [Header("计时器")]
        public TextMeshProUGUI timerText;

        [Header("结果面板")]
        public GameObject resultPanel;
        public TextMeshProUGUI resultText;
        public TextMeshProUGUI explanationText;

        [Header("得分 & 连击")]
        public TextMeshProUGUI scoreText;
        public TextMeshProUGUI comboText;

        [Header("BOSS")]
        public BossController bossController;
        public Slider bossHpSlider;

        [Header("玩家血量")]
        public Image[] playerHeartIcons;        // 3个心形Image
        public Sprite heartFull;
        public Sprite heartEmpty;

        [Header("结算")]
        public SettlePanelController settlePanel;

        private QuestionDisplay _display;
        private TimerController _timer;

        private void Start()
        {
            _display = GetComponent<QuestionDisplay>();
            _timer = GetComponent<TimerController>();
            InjectReferences();
            RegisterEvents();
        }

        private void InjectReferences()
        {
            if (_display != null)
            {
                _display.questionText = questionText;
                _display.optionButtons = optionButtons;
                _display.optionBackgrounds = optionBackgrounds;
                _display.optionTexts = optionTexts;
                _display.resultPanel = resultPanel;
                _display.resultText = resultText;
                _display.explanationText = explanationText;
                _display.BindButtons();
            }
            if (_timer != null) _timer.timerText = timerText;
        }

        private void RegisterEvents()
        {
            var mgr = BattleManager.Instance;
            if (mgr == null) return;

            mgr.OnComboChanged += UpdateComboUI;
            mgr.OnQuestionChanged += UpdateScoreUI;
            mgr.OnBattleFinished += ShowSettlement;
            mgr.OnPlayerHPChanged += UpdatePlayerHP;
            mgr.OnBossHPChanged += UpdateBossHP;
        }

        private void UpdateScoreUI(OriginXR.Data.QuestionData q, int i)
        {
            if (scoreText != null && BattleManager.Instance != null)
                scoreText.text = $"得分: {BattleManager.Instance.CurrentScore}";
        }

        private void UpdateComboUI(int combo)
        {
            if (comboText != null)
            {
                comboText.gameObject.SetActive(combo >= 3);
                comboText.text = $" {combo}连击!";
            }
        }

        private void UpdatePlayerHP(int current, int max)
        {
            if (playerHeartIcons == null) return;
            for (int i = 0; i < playerHeartIcons.Length; i++)
            {
                if (playerHeartIcons[i] == null) continue;
                playerHeartIcons[i].sprite = i < current ? heartFull : heartEmpty;
            }
        }

        private void UpdateBossHP(int current, int max)
        {
            if (bossHpSlider != null)
                bossHpSlider.value = max > 0 ? (float)current / max : 0f;
        }

        private void ShowSettlement(OriginXR.Data.StageResultData r)
        {
            if (settlePanel != null) settlePanel.Show(r);
        }

        private void OnDestroy()
        {
            var mgr = BattleManager.Instance;
            if (mgr != null)
            {
                mgr.OnComboChanged -= UpdateComboUI;
                mgr.OnQuestionChanged -= UpdateScoreUI;
                mgr.OnBattleFinished -= ShowSettlement;
                mgr.OnPlayerHPChanged -= UpdatePlayerHP;
                mgr.OnBossHPChanged -= UpdateBossHP;
            }
        }
    }
}
