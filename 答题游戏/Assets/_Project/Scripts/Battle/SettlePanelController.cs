using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace OriginXR.Battle
{
    /// <summary>
    /// 战斗结算面板
    /// </summary>
    public class SettlePanelController : MonoBehaviour
    {
        [Header("UI")]
        public TextMeshProUGUI titleText;        // "胜利!" / "失败"
        public TextMeshProUGUI scoreText;
        public TextMeshProUGUI accuracyText;
        public TextMeshProUGUI comboText;
        public TextMeshProUGUI starsText;
        public StarAnimator starAnimator;

        [Header("按钮")]
        public Button retryButton;               // 再来一次
        public Button homeButton;                 // 返回主页

        private void Start()
        {
            if (homeButton != null) homeButton.onClick.AddListener(() => Core.SceneLoader.Instance?.LoadScene("HomeScene"));
            if (retryButton != null) retryButton.onClick.AddListener(() =>
            {
                BattleManager.Instance?.StartPVEBattle(BattleManager.Instance.CurrentStageData);
                gameObject.SetActive(false);
            });
        }

        public void Show(Data.StageResultData result)
        {
            gameObject.SetActive(true);

            bool isWin = result.isBossDefeated;

            if (titleText != null)
            {
                titleText.text = isWin ? "🏆 胜利!" : "💀 失败";
                titleText.color = isWin ? new Color(1f, 0.85f, 0.2f) : new Color(1f, 0.4f, 0.4f);
            }

            if (scoreText != null) scoreText.text = $"得分: {result.score}";
            if (accuracyText != null) accuracyText.text = $"正确: {result.correctCount}/{result.totalCount}";
            if (comboText != null) comboText.text = $"最高连击: {result.maxCombo}";
            if (starsText != null) starsText.text = result.GetStarText();

            if (retryButton != null) retryButton.gameObject.SetActive(!isWin);
            if (homeButton != null) homeButton.gameObject.SetActive(true);

            if (starAnimator != null)
                starAnimator.Play(result.starsEarned);
        }
    }
}
