using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace OriginXR.Battle
{
    /// <summary>
    /// BattleScene UI 绑定器
    /// 把这个挂到 BattleSystem 上，在 Inspector 中拖好所有引用，
    /// 启动时自动注入到 QuestionDisplay、TimerController 等管理器
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

        [Header("结算面板")]
        public SettlePanelController settlePanel;

        // === 缓存 ===
        private QuestionDisplay _display;
        private TimerController _timer;

        private void Start()
        {
            _display = GetComponent<QuestionDisplay>();
            _timer = GetComponent<TimerController>();

            InjectReferences();
            RegisterEvents();

            Debug.Log("[BattleSceneSetup] UI 绑定完成");
        }

        private void InjectReferences()
        {
            // --- QuestionDisplay ---
            if (_display != null)
            {
                _display.questionText = questionText;
                _display.optionButtons = optionButtons;
                _display.optionBackgrounds = optionBackgrounds;
                _display.optionTexts = optionTexts;
                _display.resultPanel = resultPanel;
                _display.resultText = resultText;
                _display.explanationText = explanationText;
            }

            // --- TimerController ---
            if (_timer != null)
            {
                _timer.timerText = timerText;
            }
        }

        private void RegisterEvents()
        {
            var battleMgr = BattleManager.Instance;
            if (battleMgr != null)
            {
                battleMgr.OnComboChanged += UpdateComboUI;
                battleMgr.OnQuestionChanged += UpdateScoreUI;
                battleMgr.OnBattleFinished += ShowSettlement;
            }
        }

        private void UpdateScoreUI(OriginXR.Data.QuestionData question, int index)
        {
            var mgr = BattleManager.Instance;
            if (mgr != null && scoreText != null)
                scoreText.text = $"得分: {mgr.CurrentScore}";
        }

        private void UpdateComboUI(int combo)
        {
            if (comboText != null)
            {
                comboText.gameObject.SetActive(combo >= 3);
                comboText.text = $"🔥 {combo}连击!";
            }
        }

        private void ShowSettlement(OriginXR.Data.StageResultData result)
        {
            if (settlePanel != null)
                settlePanel.Show(result);
        }

        private void OnDestroy()
        {
            var mgr = BattleManager.Instance;
            if (mgr != null)
            {
                mgr.OnComboChanged -= UpdateComboUI;
                mgr.OnQuestionChanged -= UpdateScoreUI;
                mgr.OnBattleFinished -= ShowSettlement;
            }
        }
    }
}
