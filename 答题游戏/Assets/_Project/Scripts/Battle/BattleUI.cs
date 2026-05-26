using UnityEngine;
using UnityEngine.UI;
using TMPro;
using OriginXR.Data;

namespace OriginXR.Battle
{
    public class BattleUI : MonoBehaviour
    {
        [Header("题目")]
        public TextMeshProUGUI questionText;
        public Button[] optionButtons;
        public Image[] optionBg;
        public TextMeshProUGUI[] optionTexts;
        public TextMeshProUGUI resultText;
        public TextMeshProUGUI explanationText;

        [Header("计时 + 分数")]
        public TextMeshProUGUI timerText;
        public TextMeshProUGUI scoreText;

        [Header("BOSS")]
        public BossController bossController;
        public Slider bossHpSlider;

        [Header("血量")]
        public Image[] hearts;
        public Sprite heartFull;
        public Sprite heartEmpty;

        [Header("结算")]
        public SettlePanelController settlePanel;

        private void Start()
        {
            var bm = BattleManager.Instance;
            if (bm == null) return;

            var qd = GetComponent<QuestionDisplay>();
            if (qd != null)
            {
                qd.questionText = questionText; qd.optionButtons = optionButtons;
                qd.optionBg = optionBg; qd.optionTexts = optionTexts;
                qd.resultText = resultText; qd.explanationText = explanationText;
                qd.BindButtons();
            }

            var tc = GetComponent<TimerController>();
            if (tc != null) tc.timerText = timerText;

            var ce = GetComponent<ComboEffectController>();
            if (ce != null) ce.comboText = scoreText;

            var bc = GetComponent<BossController>();
            if (bc != null) bc.hpSlider = bossHpSlider;

            bm.OnPlayerHPChanged += OnHP;
            bm.OnBossHPChanged += OnBossHP;
            bm.OnScoreUpdated += OnScore;
            bm.OnBattleFinished += OnFinish;
        }

        private void OnHP(int cur, int max)
        {
            if (hearts == null) return;
            for (int i = 0; i < hearts.Length; i++)
                if (hearts[i] != null) hearts[i].sprite = i < cur ? heartFull : heartEmpty;
        }

        private void OnBossHP(int cur, int max)
        {
            if (bossHpSlider != null) bossHpSlider.value = max > 0 ? (float)cur / max : 0f;
        }

        private void OnScore()
        {
            var bm = BattleManager.Instance;
            if (bm != null && scoreText != null) scoreText.text = $"得分: {bm.Score}";
        }

        private void OnFinish(StageResultData r)
        {
            if (settlePanel != null) settlePanel.Show(r);
        }

        private void OnDestroy()
        {
            var bm = BattleManager.Instance;
            if (bm != null) { bm.OnPlayerHPChanged -= OnHP; bm.OnBossHPChanged -= OnBossHP; bm.OnScoreUpdated -= OnScore; bm.OnBattleFinished -= OnFinish; }
        }
    }
}
